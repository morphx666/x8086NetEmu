using System;
using System.Collections.Generic;
using Eto;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    internal sealed class EtoVideoInputController {
        private static readonly Cursor HiddenCursor = new(new Bitmap(16, 16, PixelFormat.Format32bppRgba, new int[16 * 16]), PointF.Empty);

        // Mouse speed multiplier. At 1.0 the guest pointer moves the same distance on screen as the host
        // pointer would (with the DOS driver's default mickey/pixel ratios); raise or lower to taste.
        private const float MOUSE_SPEED = 1.0f;

        // How far (in host pixels, per axis, as a fraction of the surface's smaller side) the pointer must
        // be pushed past a corner of the emulator surface to release the mouse capture.
        private const float CORNER_ESCAPE_PUSH = 1f / 12;

        private readonly Drawable renderControl;
        private readonly CGAAdapter videoAdapter;
        private PointF lastMouseLocation;
        // Sub-pixel movement not yet sent (Eto reports fractional logical coordinates on scaled displays)
        private float pendingDeltaX;
        private float pendingDeltaY;
        // Where the host pointer would be over the surface if it weren't being re-centered; used to detect
        // the user pushing into a corner to escape the capture
        private PointF virtualPointer;
        private float pushX, pushY;  // Accumulated push beyond the left/right and top/bottom edges
        private int pushDirX, pushDirY; // -1/+1 = pushing past the left/top or right/bottom edge, 0 = not
        private bool isMouseOver;
        private MouseButtons pressedMouseButtons;
        private MouseButtons lastClickedMouseButtons;
        private readonly HashSet<Keys> heldKeys = [];
        private bool suspended;

        // Raised (on the UI thread) for the frontend's Shift+Alt+<key> hotkeys other than the menu (C, V)
        public event Action<Keys> HotKeyPressed;

        public EtoVideoInputController(Drawable renderControl, CGAAdapter videoAdapter) {
            this.renderControl = renderControl;
            this.videoAdapter = videoAdapter;
        }

        public void Attach() {
            renderControl.KeyDown += OnKeyDown;
            renderControl.KeyUp += OnKeyUp;
            renderControl.LostFocus += OnLostFocus;
            renderControl.MouseEnter += OnMouseEnter;
            renderControl.MouseDown += OnMouseDown;
            renderControl.MouseDoubleClick += OnMouseDoubleClick;
            renderControl.MouseMove += OnMouseMove;
            renderControl.MouseUp += OnMouseUp;
        }

        public void Detach() {
            renderControl.KeyDown -= OnKeyDown;
            renderControl.KeyUp -= OnKeyUp;
            renderControl.LostFocus -= OnLostFocus;
            renderControl.MouseEnter -= OnMouseEnter;
            renderControl.MouseDown -= OnMouseDown;
            renderControl.MouseDoubleClick -= OnMouseDoubleClick;
            renderControl.MouseMove -= OnMouseMove;
            renderControl.MouseUp -= OnMouseUp;
        }

        // While suspended (e.g. selecting text to copy) no keyboard or mouse input reaches the guest and the
        // mouse is not captured, so the frontend can use the surface itself
        public bool Suspended {
            get => suspended;
            set {
                if(suspended == value) return;
                suspended = value;
                if(value) {
                    ReleaseKeys();
                    ReleaseMouse();
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if(suspended) return;

            if(e.Shift && e.Alt && (e.Key == Keys.C || e.Key == Keys.V)) {
                ReleaseKeys();
                Keys key = e.Key;
                Application.Instance.AsyncInvoke(() => HotKeyPressed?.Invoke(key));
                e.Handled = true;
                return;
            }

            if(e.KeyData == (Keys.Shift | Keys.Alt | Keys.Home)
                || e.KeyData == (Keys.Shift | Keys.Alt | Keys.Keypad7)
                || (e.Shift && e.Alt && (e.Key == Keys.Home || e.Key == Keys.Keypad7))) {
                // The menu takes keyboard focus, so the key-ups for Shift/Alt would never reach
                // the guest, leaving them logically held down (every later keystroke then arrives
                // as Shift+Alt+key and the machine appears frozen).
                ReleaseKeys();
                ReleaseMouse();
                Application.Instance.AsyncInvoke(() => renderControl.ParentWindow?.ContextMenu?.Show(renderControl));
                e.Handled = true;
                return;
            }

            heldKeys.Add(e.Key);
            videoAdapter.HandleKeyDown(videoAdapter, new Adapter.XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
            e.Handled = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs e) {
            if(suspended) return;
            heldKeys.Remove(e.Key);
            videoAdapter.HandleKeyUp(videoAdapter, new Adapter.XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
            e.Handled = true;
        }

        private void OnLostFocus(object sender, EventArgs e) {
            // Any key still down when focus leaves (menu, Alt+Tab, dialog) will never get its
            // key-up delivered here, so release it in the guest now.
            ReleaseKeys();
        }

        public void ReleaseKeys() {
            if(heldKeys.Count == 0) return;

            Keys[] keys = [.. heldKeys];
            heldKeys.Clear();

            foreach(Keys k in keys) {
                videoAdapter.HandleKeyUp(videoAdapter, new Adapter.XKeyEventArgs(KeyToInt(k), 0));
            }
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) {
            CaptureInput(e.Location);
        }

        private void CaptureInput(PointF location) {
            if(suspended || videoAdapter.CPU.Mouse == null) {
                return;
            }

            isMouseOver = true;
            lastMouseLocation = location;
            pendingDeltaX = 0;
            pendingDeltaY = 0;
            virtualPointer = location;
            pushX = pushY = 0;
            pushDirX = pushDirY = 0;
            videoAdapter.CPU.Mouse.MidPointOffset = new Adapter.XPoint(0, 0);
            videoAdapter.CPU.Mouse.IsCaptured = true;
            renderControl.Cursor = HiddenCursor;
            renderControl.Focus();
            renderControl.CaptureMouse();
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            if(isMouseOver && !suspended) {
                SendMouseDown(e.Buttons);
                e.Handled = true;
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e) {
            if(isMouseOver && !suspended) {
                MouseButtons buttons = e.Buttons != MouseButtons.None
                    ? e.Buttons
                    : lastClickedMouseButtons;

                if(buttons == MouseButtons.None) {
                    buttons = MouseButtons.Primary;
                }

                // Eto delivers the second press of a double-click as this event *instead of*
                // a second MouseDown/MouseUp pair. Emit a full press+release cycle here so the
                // guest's serial mouse driver sees two complete click cycles (matching the
                // WinForms renderer, where a physical double-click arrives as two down/up pairs).
                // The guest keys double-click detection off the spacing between the two button-down
                // transitions, which real event timing already preserves; the release only needs
                // to happen so the button does not stay logically held down.
                SendMouseDown(buttons);
                SendMouseUp(buttons);
                e.Handled = true;
            }
        }

        private void SendMouseDown(MouseButtons buttons) {
            pressedMouseButtons |= buttons;
            lastClickedMouseButtons = buttons;
            videoAdapter.OnMouseDown(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), 0, 0));
        }

        private void SendMouseUp(MouseButtons buttons) {
            pressedMouseButtons &= ~buttons;
            videoAdapter.OnMouseUp(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), 0, 0));
        }

        private void OnMouseMove(object sender, MouseEventArgs e) {
            // After the emulator menu closes the pointer may already be over the canvas, in which
            // case no MouseEnter is raised; re-capture on the first move instead.
            if(suspended) return;
            if(!isMouseOver) {
                CaptureInput(e.Location);
                e.Handled = true;
                return;
            }

            if(isMouseOver && videoAdapter.CPU.Mouse?.IsCaptured == true) {
                float hostDeltaX = e.Location.X - lastMouseLocation.X;
                float hostDeltaY = e.Location.Y - lastMouseLocation.Y;
                lastMouseLocation = e.Location;

                if(TryCornerEscape(hostDeltaX, hostDeltaY)) {
                    e.Handled = true;
                    return;
                }

                // Host pixels -> mickeys. The surface is the guest screen scaled by Zoom, and DOS drivers map
                // mickeys onto a 640x200 virtual screen (8 mickeys per 8 pixels horizontally, 16 per 8
                // vertically), so dividing by Zoom makes the guest pointer track the host pointer's on-screen
                // speed in every video mode (text included). Without it everything moved Zoom times too fast.
                double zoom = videoAdapter.Zoom > 0 ? videoAdapter.Zoom : 1;
                float scale = (float)(MOUSE_SPEED / zoom);

                // Keep the fractional part for the next event instead of truncating it away, otherwise
                // slow movements (and scaled displays) never add up to a single mickey
                pendingDeltaX += hostDeltaX * scale;
                pendingDeltaY += hostDeltaY * scale;

                int deltaX = (int)pendingDeltaX;
                int deltaY = (int)pendingDeltaY;
                pendingDeltaX -= deltaX;
                pendingDeltaY -= deltaY;

                if(deltaX != 0 || deltaY != 0) {
                    videoAdapter.OnMouseMove(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), deltaX, deltaY));
                }

                RecenterHostCursor(e.Location);
                e.Handled = true;
            }
        }

        // Tracks a virtual host pointer clamped to the surface and releases the capture when it is pushed
        // firmly past any of the four corners. The real cursor is then placed just outside that corner so
        // the user can carry on moving out of the window (moving back over the surface captures again).
        private bool TryCornerEscape(float dx, float dy) {
            SizeF size = renderControl.Size;
            if(size.Width <= 0 || size.Height <= 0) return false;

            float maxX = size.Width - 1;
            float maxY = size.Height - 1;
            float threshold = Math.Min(size.Width, size.Height) * CORNER_ESCAPE_PUSH;

            virtualPointer.X = TrackEdge(virtualPointer.X + dx, dx, maxX, ref pushX, ref pushDirX);
            virtualPointer.Y = TrackEdge(virtualPointer.Y + dy, dy, maxY, ref pushY, ref pushDirY);

            if(pushDirX == 0 || pushDirY == 0 || pushX < threshold || pushY < threshold) return false;

            int dirX = pushDirX;
            int dirY = pushDirY;
            ReleaseMouse();

            try {
                const float outside = 8;
                PointF corner = new(dirX < 0 ? -outside : size.Width + outside,
                                    dirY < 0 ? -outside : size.Height + outside);
                Mouse.Position = renderControl.PointToScreen(corner);
            } catch {
                // Pointer warping not supported on this platform; the capture is released anyway
            }

            return true;
        }

        // Clamps one axis of the virtual pointer to [0, max] and accumulates how far it is being pushed past
        // an edge. Moving back inwards (or away from the edge) resets the push.
        private static float TrackEdge(float position, float delta, float max, ref float push, ref int pushDir) {
            if(position < 0) {
                push = (pushDir == -1 ? push : 0) - position;
                pushDir = -1;
                return 0;
            }
            if(position > max) {
                push = (pushDir == 1 ? push : 0) + (position - max);
                pushDir = 1;
                return max;
            }

            // Still resting on the edge with no movement along this axis: keep the push
            bool resting = delta == 0 && ((pushDir == -1 && position == 0) || (pushDir == 1 && position == max));
            if(!resting) {
                push = 0;
                pushDir = 0;
            }
            return position;
        }

        // While captured the host cursor is hidden; if it were allowed to reach the edge of the screen,
        // movement in that direction would silently stop. Warp it back to the center of the surface
        // whenever it drifts too far (the warp's own move event then yields a zero delta).
        private void RecenterHostCursor(PointF location) {
            SizeF size = renderControl.Size;
            if(size.Width <= 0 || size.Height <= 0) return;

            PointF center = new(size.Width / 2, size.Height / 2);
            float margin = Math.Min(size.Width, size.Height) / 4;
            if(Math.Abs(location.X - center.X) < margin && Math.Abs(location.Y - center.Y) < margin) return;

            try {
                Mouse.Position = renderControl.PointToScreen(center);
                lastMouseLocation = center;
            } catch {
                // Some platforms (e.g. Wayland) don't allow moving the pointer; keep the plain behavior
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e) {
            if(isMouseOver && !suspended) {
                MouseButtons releasedButtons = e.Buttons != MouseButtons.None
                    ? e.Buttons
                    : pressedMouseButtons;

                pressedMouseButtons &= ~releasedButtons;
                videoAdapter.OnMouseUp(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), 0, 0));
                e.Handled = true;
            }
        }

        // Eto keys whose emulator (Windows virtual-key) name differs. Eto's Slash is the main keyboard's '/' while
        // the emulator's Slash is the keypad's (an alias of Divide).
        private static readonly Dictionary<Keys, Adapter.XEventArgs.Keys> keyTranslation = new() {
            [Keys.Slash] = Adapter.XEventArgs.Keys.OemQuestion,
            [Keys.ScrollLock] = Adapter.XEventArgs.Keys.Scroll,
            [Keys.ContextMenu] = Adapter.XEventArgs.Keys.Apps,
        };

        // Eto key (and/or modifiers) -> emulator key value. Keys the PC keyboard doesn't have (the Mac's Command key,
        // the keypad '=', ...) translate to no key, which the keyboard ignores; they must never throw: this runs
        // inside the native key event handler, where an exception takes the whole application down.
        private static int KeyToInt(Keys k) {
            int result = 0;
            if((k & Keys.Shift) != 0) result |= (int)Adapter.XEventArgs.Keys.Shift;
            if((k & Keys.Control) != 0) result |= (int)Adapter.XEventArgs.Keys.Control;
            if((k & Keys.Alt) != 0) result |= (int)Adapter.XEventArgs.Keys.Alt;

            Keys key = k & Keys.KeyMask;
            if(key == Keys.None) return result;
            if(keyTranslation.TryGetValue(key, out Adapter.XEventArgs.Keys translated)) return result | (int)translated;
            string name = key.ToString();
            if(!char.IsDigit(name[0]) && Enum.TryParse(name, out Adapter.XEventArgs.Keys parsed)) return result | (int)parsed;
            return result;
        }

        private int MouseButtonsToInt(MouseButtons buttons) {
            int mappedButtons = 0;

            if((buttons & MouseButtons.Primary) == MouseButtons.Primary) mappedButtons |= (int)Adapter.XEventArgs.MouseButtons.Left;
            if((buttons & MouseButtons.Alternate) == MouseButtons.Alternate) mappedButtons |= (int)Adapter.XEventArgs.MouseButtons.Right;
            if((buttons & MouseButtons.Middle) == MouseButtons.Middle) mappedButtons |= (int)Adapter.XEventArgs.MouseButtons.Middle;

            return mappedButtons;
        }

        public void ReleaseMouse() {
            if(pressedMouseButtons != MouseButtons.None && videoAdapter.CPU.Mouse != null) {
                pressedMouseButtons = MouseButtons.None;
                videoAdapter.OnMouseUp(videoAdapter, new Adapter.XMouseEventArgs(0, 0, 0));
            }

            isMouseOver = false;

            if(videoAdapter.CPU.Mouse != null) {
                videoAdapter.CPU.Mouse.IsCaptured = false;
            }

            Application.Instance.Invoke(() => {
                if(renderControl.IsMouseCaptured) {
                    renderControl.ReleaseMouseCapture();
                }

                renderControl.Cursor = Cursors.Default;
            });
        }
    }
}
