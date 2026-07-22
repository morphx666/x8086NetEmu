using System;
using Eto.Drawing;
using Eto.Forms;
using x8086NetEmu;

namespace x8086NetEmuEto.Renderers {
    internal sealed class EtoVideoInputController {
        private static readonly Cursor HiddenCursor = new(new Bitmap(16, 16, PixelFormat.Format32bppRgba, new int[16 * 16]), PointF.Empty);

        private readonly Drawable renderControl;
        private readonly CGAAdapter videoAdapter;
        private PointF lastMouseLocation;
        private bool isMouseOver;
        private MouseButtons pressedMouseButtons;
        private MouseButtons lastClickedMouseButtons;

        public EtoVideoInputController(Drawable renderControl, CGAAdapter videoAdapter) {
            this.renderControl = renderControl;
            this.videoAdapter = videoAdapter;
        }

        public void Attach() {
            renderControl.KeyDown += OnKeyDown;
            renderControl.KeyUp += OnKeyUp;
            renderControl.MouseEnter += OnMouseEnter;
            renderControl.MouseDown += OnMouseDown;
            renderControl.MouseDoubleClick += OnMouseDoubleClick;
            renderControl.MouseMove += OnMouseMove;
            renderControl.MouseUp += OnMouseUp;
        }

        public void Detach() {
            renderControl.KeyDown -= OnKeyDown;
            renderControl.KeyUp -= OnKeyUp;
            renderControl.MouseEnter -= OnMouseEnter;
            renderControl.MouseDown -= OnMouseDown;
            renderControl.MouseDoubleClick -= OnMouseDoubleClick;
            renderControl.MouseMove -= OnMouseMove;
            renderControl.MouseUp -= OnMouseUp;
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if(e.KeyData == (Keys.Shift | Keys.Alt | Keys.Home)
                || e.KeyData == (Keys.Shift | Keys.Alt | Keys.Keypad7)
                || (e.Shift && e.Alt && (e.Key == Keys.Home || e.Key == Keys.Keypad7))) {
                ReleaseMouse();
                Application.Instance.AsyncInvoke(() => renderControl.ParentWindow?.ContextMenu?.Show(renderControl));
                e.Handled = true;
                return;
            }

            videoAdapter.HandleKeyDown(videoAdapter, new Adapter.XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
            e.Handled = true;
        }

        private void OnKeyUp(object sender, KeyEventArgs e) {
            videoAdapter.HandleKeyUp(videoAdapter, new Adapter.XKeyEventArgs(KeyToInt(e.Key), KeyToInt(e.Modifiers)));
            e.Handled = true;
        }

        private void OnMouseEnter(object sender, MouseEventArgs e) {
            if(videoAdapter.CPU.Mouse == null) {
                return;
            }

            isMouseOver = true;
            lastMouseLocation = e.Location;
            videoAdapter.CPU.Mouse.MidPointOffset = new Adapter.XPoint(0, 0);
            videoAdapter.CPU.Mouse.IsCaptured = true;
            renderControl.Cursor = HiddenCursor;
            renderControl.Focus();
            renderControl.CaptureMouse();
        }

        private void OnMouseDown(object sender, MouseEventArgs e) {
            if(isMouseOver) {
                SendMouseDown(e.Buttons);
                e.Handled = true;
            }
        }

        private void OnMouseDoubleClick(object sender, MouseEventArgs e) {
            if(isMouseOver) {
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
            if(isMouseOver && videoAdapter.CPU.Mouse?.IsCaptured == true) {
                int deltaX = (int)(e.Location.X - lastMouseLocation.X);
                int deltaY = (int)(e.Location.Y - lastMouseLocation.Y);
                lastMouseLocation = e.Location;

                videoAdapter.OnMouseMove(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), deltaX, deltaY));
                e.Handled = true;
            }
        }

        private void OnMouseUp(object sender, MouseEventArgs e) {
            if(isMouseOver) {
                MouseButtons releasedButtons = e.Buttons != MouseButtons.None
                    ? e.Buttons
                    : pressedMouseButtons;

                pressedMouseButtons &= ~releasedButtons;
                videoAdapter.OnMouseUp(videoAdapter, new Adapter.XMouseEventArgs(MouseButtonsToInt(pressedMouseButtons), 0, 0));
                e.Handled = true;
            }
        }

        private int KeyToInt(Keys k) {
            return (int)(Adapter.XEventArgs.Keys)Enum.Parse(typeof(Adapter.XEventArgs.Keys), k.ToString());
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
