/*
 *  KeyboardExt.java
 *  Joris van Rantwijk
 */

/*
 *  This implementation has been validated on Sun JRE 1.4.2 / Linux i386 /
 *  X.Org 6.9.0 with 104-key US PS/2 keyboard, against a real IBM PC/XT
 *  with 101-key extended keyboard. It produces the same scan code sequences,
 *  except for the issue that Java does not report numeric keypad '5' when
 *  numlock is off.
 */

package retro;
import java.awt.event.KeyEvent;

/**
 * KeyboardExt simulates a 101-key extended PC keyboard.
 *
 * A KeyboardExt object accepts key up/down events using Java VK_xxx-style
 * key codes, and sends keyboard scan codes to the keyboard controller.
 * <p>
 * This object produces XT scan codes for the 83 keys of the original
 * PC keyboard, and e0-style extended scan codes for the new keys of
 * the 101-key keyboard. This is commonly known as scan code set 1.
 * <p>
 * Inferring the correct escape sequence for extended keys requires
 * knowledge of the state of modifier (Shift, Ctrl, Alt) keys and NumLock.
 * This implementation keeps track of these states by looking at the
 * corresponding key up/down events.
 * <p>
 * Tracking the NumLock state on a PC/XT is especially problematic
 * because there is no feedback. So the keyboard and software may get
 * different ideas about the numlock state, and there is no way to recover
 * from that.
 * This is not the simulation's fault; physical extended XT keyboards
 * have exactly the same problem.
 */
public class KeyboardExt implements ExternalInputHandler
{

    /** Enables sending virtual shift up/down events to deal with
        non-XT keys. */
    private static final boolean useVirtualShift = true;

    /** Map Java key code to PC scan code and extra flags. */
    private int[] keytbl;

    /** Must send E0 escape with this scancode */
    private static final int KEY_EXTEND = 0x100;

    /** Need negative shift/numlock state */
    private static final int KEY_NONUM = 0x200;

    /** Need negative shift state */
    private static final int KEY_NOSHIFT = 0x400;

    /** Could be numpad or edit block depending on key location */
    private static final int KEY_EDIT = 0x800;

    /** Scan code of left Shift key */
    public static final int SCAN_LSHIFT = 42;

    /** Scan code of right Shift key */
    public static final int SCAN_RSHIFT = 54;

    /** Scan code of Ctrl key */
    public static final int SCAN_CTRL = 29;

    /** Scan code of Alt key */
    public static final int SCAN_ALT = 56;

    /** The keyboard controller that we send scancodes to. */
    private KeyboardController keyboardController;

    /** True if we inverted the shift state by sending a virtual shift code. */
    private boolean virtualShiftState;

    /** True if PrintScreen is down in its SysRq role. */
    private boolean isSysRq;

    /** Mask of state keys that we believe to be physically down. */
    private int stateKeyMask;
    private static int MASK_LSHIFT = 1;
    private static int MASK_RSHIFT = 2;
    private static int MASK_LALT = 4;
    private static int MASK_RALT = 8;
    private static int MASK_LCTRL = 16;
    private static int MASK_RCTRL = 32;
    private static int MASK_NUMLOCK = 64;


    /** Create and initialize key mapper. */
    public KeyboardExt(KeyboardController kc)
    {
        keyboardController = kc;
        virtualShiftState = false;
        isSysRq = false;
        stateKeyMask = 0;
        keytbl = new int[256];
        keytbl[KeyEvent.VK_ESCAPE] = 1;
        keytbl[KeyEvent.VK_1] = 2;
        keytbl[KeyEvent.VK_2] = 3;
        keytbl[KeyEvent.VK_3] = 4;
        keytbl[KeyEvent.VK_4] = 5;
        keytbl[KeyEvent.VK_5] = 6;
        keytbl[KeyEvent.VK_6] = 7;
        keytbl[KeyEvent.VK_7] = 8;
        keytbl[KeyEvent.VK_8] = 9;
       	keytbl[KeyEvent.VK_9] = 10;
        keytbl[KeyEvent.VK_0] = 11;
        keytbl[KeyEvent.VK_MINUS] = 12;
        keytbl[KeyEvent.VK_EQUALS] = 13;
        keytbl[KeyEvent.VK_BACK_SPACE] = 14;
        keytbl[KeyEvent.VK_TAB] = 15;
        keytbl[KeyEvent.VK_Q] = 16;
        keytbl[KeyEvent.VK_W] = 17;
        keytbl[KeyEvent.VK_E] = 18;
        keytbl[KeyEvent.VK_R] = 19;
        keytbl[KeyEvent.VK_T] = 20;
        keytbl[KeyEvent.VK_Y] = 21;
        keytbl[KeyEvent.VK_U] = 22;
        keytbl[KeyEvent.VK_I] = 23;
        keytbl[KeyEvent.VK_O] = 24;
        keytbl[KeyEvent.VK_P] = 25;
        keytbl[KeyEvent.VK_OPEN_BRACKET] = 26;
        keytbl[KeyEvent.VK_CLOSE_BRACKET] = 27;
        keytbl[KeyEvent.VK_ENTER] = 28;
        keytbl[KeyEvent.VK_CONTROL] = 29;
        keytbl[KeyEvent.VK_A] = 30;
        keytbl[KeyEvent.VK_S] = 31;
        keytbl[KeyEvent.VK_D] = 32;
        keytbl[KeyEvent.VK_F] = 33;
        keytbl[KeyEvent.VK_G] = 34;
        keytbl[KeyEvent.VK_H] = 35;
        keytbl[KeyEvent.VK_J] = 36;
        keytbl[KeyEvent.VK_K] = 37;
        keytbl[KeyEvent.VK_L] = 38;
        keytbl[KeyEvent.VK_SEMICOLON] = 39;
        keytbl[KeyEvent.VK_QUOTE] = 40;
        keytbl[KeyEvent.VK_BACK_QUOTE] = 41;
        keytbl[KeyEvent.VK_SHIFT] = SCAN_LSHIFT;
        keytbl[KeyEvent.VK_BACK_SLASH] = 43;
        keytbl[KeyEvent.VK_Z] = 44;
        keytbl[KeyEvent.VK_X] = 45;
        keytbl[KeyEvent.VK_C] = 46;
        keytbl[KeyEvent.VK_V] = 47;
        keytbl[KeyEvent.VK_B] = 48;
        keytbl[KeyEvent.VK_N] = 49;
        keytbl[KeyEvent.VK_M] = 50;
        keytbl[KeyEvent.VK_COMMA] = 51;
        keytbl[KeyEvent.VK_PERIOD] = 52;
        keytbl[KeyEvent.VK_SLASH] = 53;
        keytbl[KeyEvent.VK_DIVIDE] = 53 | KEY_EXTEND | KEY_NOSHIFT;
        keytbl[KeyEvent.VK_MULTIPLY] = 55;
        keytbl[KeyEvent.VK_ALT] = 56;
        keytbl[KeyEvent.VK_SPACE] = 57;
        keytbl[KeyEvent.VK_CAPS_LOCK] = 58;
        keytbl[KeyEvent.VK_F1] = 59;
        keytbl[KeyEvent.VK_F2] = 60;
        keytbl[KeyEvent.VK_F3] = 61;
        keytbl[KeyEvent.VK_F4] = 62;
        keytbl[KeyEvent.VK_F5] = 63;
        keytbl[KeyEvent.VK_F6] = 64;
        keytbl[KeyEvent.VK_F7] = 65;
        keytbl[KeyEvent.VK_F8] = 66;
        keytbl[KeyEvent.VK_F9] = 67;
        keytbl[KeyEvent.VK_F10] = 68;
        keytbl[KeyEvent.VK_NUM_LOCK] = 69;
        keytbl[KeyEvent.VK_SCROLL_LOCK] = 70;
        keytbl[KeyEvent.VK_NUMPAD7] = 71;
        keytbl[KeyEvent.VK_HOME] = 71 | KEY_EDIT;
        keytbl[KeyEvent.VK_NUMPAD8] = 72;
        keytbl[KeyEvent.VK_UP] = 72 | KEY_EXTEND | KEY_NONUM;
        keytbl[KeyEvent.VK_KP_UP] = 72;
        keytbl[KeyEvent.VK_NUMPAD9] = 73;
        keytbl[KeyEvent.VK_PAGE_UP] = 73 | KEY_EDIT;
        keytbl[KeyEvent.VK_SUBTRACT] = 74;
        keytbl[KeyEvent.VK_NUMPAD4] = 75;
        keytbl[KeyEvent.VK_LEFT] = 75 | KEY_EXTEND | KEY_NONUM;
        keytbl[KeyEvent.VK_KP_LEFT] = 75;
        keytbl[KeyEvent.VK_NUMPAD5] = 76;
        keytbl[KeyEvent.VK_NUMPAD6] = 77;
        keytbl[KeyEvent.VK_RIGHT] = 77 | KEY_EXTEND | KEY_NONUM;
        keytbl[KeyEvent.VK_KP_RIGHT] = 77;
        keytbl[KeyEvent.VK_ADD] = 78;
        keytbl[KeyEvent.VK_NUMPAD1] = 79;
        keytbl[KeyEvent.VK_END] = 79 | KEY_EDIT;
        keytbl[KeyEvent.VK_NUMPAD2] = 80;
        keytbl[KeyEvent.VK_DOWN] = 80 | KEY_EXTEND | KEY_NONUM;
        keytbl[KeyEvent.VK_KP_DOWN] = 80;
        keytbl[KeyEvent.VK_NUMPAD3] = 81;
        keytbl[KeyEvent.VK_PAGE_DOWN] = 81 | KEY_EDIT;
        keytbl[KeyEvent.VK_NUMPAD0] = 82;
        keytbl[KeyEvent.VK_INSERT] = 82 | KEY_EDIT;
        keytbl[KeyEvent.VK_DECIMAL] = 83;
        keytbl[KeyEvent.VK_DELETE] = 83 | KEY_EDIT;
        keytbl[KeyEvent.VK_PRINTSCREEN] = 84;
        keytbl[KeyEvent.VK_F11] = 87;
        keytbl[KeyEvent.VK_F12] = 88;
    }


    /**
     * Handles key press and key release events.
     *
     * Constructs thet scan code sequence corresponding to the key event
     * and sends it to the keyboard controller. Updates internal state
     * of the keyboard.
     */
    public void handleInput(ExternalInputEvent evt)
    {
        KeyInputEvent kevt = (KeyInputEvent) evt;
        byte[] scan = null;

        // Decode key up/down
        boolean released;
        switch (kevt.id) {
            case KeyEvent.KEY_PRESSED:
                released = false;
                break;
            case KeyEvent.KEY_RELEASED:
                released = true;
                break;
            default:
                return; // ignore
        }

        if (kevt.keyCode == KeyEvent.VK_PAUSE && !released) {
            // Pause has special behaviour:
            // Acts as extended ScrollLock if Ctrl is down (Break),
            // other wise acts as extended Ctrl-NumLock.
            // Key down and key release scan codes are always sent together.
            if ((stateKeyMask & (MASK_LCTRL|MASK_RCTRL)) != 0)
                scan = new byte[] { (byte)0xe0, 0x46,
                                    (byte)0xe0, (byte)0xc6 };
            else
                scan = new byte[] { (byte)0xe1, 0x1d, 0x45,
                                    (byte)0xe1, (byte)0x9d, (byte)0xc5 };
            if (keyboardController != null)
                keyboardController.putKeyData(scan);
            return;
        }

        // Lookup key in table
        int keyinfo = 0;
        if (kevt.keyCode < keytbl.length)
            keyinfo = keytbl[kevt.keyCode];
        if (keyinfo == 0)
            return; // ignore unknown key

        // Detect state keys (shift, ctrl, alt)
        int statebit = 0;
        if (keyinfo == SCAN_LSHIFT) statebit = MASK_LSHIFT;
        if (keyinfo == SCAN_CTRL) statebit = MASK_LCTRL;
        if (keyinfo == SCAN_ALT) statebit = MASK_LALT;

        // Distinguish left/right and numpad/editpad
        boolean extend = ((keyinfo & KEY_EXTEND) != 0);
        if (statebit != 0 &&
            kevt.keyLocation == KeyEvent.KEY_LOCATION_RIGHT) {
            if (keyinfo == SCAN_LSHIFT)
                keyinfo = SCAN_RSHIFT; // right shift
            else
                extend = true; // right ctrl or alt
            statebit <<= 1;
        }
        if ((keyinfo & KEY_EDIT) != 0 &&
            kevt.keyLocation == KeyEvent.KEY_LOCATION_STANDARD) {
            extend = true; // edit pad
            keyinfo |= KEY_NONUM;
        }
        if (kevt.keyCode == KeyEvent.VK_ENTER &&
            kevt.keyLocation == KeyEvent.KEY_LOCATION_NUMPAD)
            extend = true; // numpad Enter
        
        if (released) {

            // Undo shift state virtualization that we (may) have
            // started when this key was pressed.
            boolean undoVirtual = useVirtualShift && virtualShiftState &&
                                  ((keyinfo & (KEY_NOSHIFT | KEY_NONUM)) != 0);

            if (kevt.keyCode == KeyEvent.VK_PRINTSCREEN) {
                // PrintScreen has special behaviour
                if (!isSysRq) {
                    keyinfo = 55;
                    extend = true;
                    undoVirtual = useVirtualShift && virtualShiftState;
                }
                isSysRq = false;
            }

            // Construct key release scan code sequence
            scan = new byte[((extend) ? 2 : 1) + ((undoVirtual) ? 2 : 0)];
            int i = 0;
            if (extend)
                scan[i++] = (byte)0xe0;
            scan[i++] = (byte)(keyinfo | 0x80);
            if (undoVirtual) {
                scan[i++] = (byte)0xe0;
                scan[i++] = ((stateKeyMask & MASK_LSHIFT) != 0) ? SCAN_LSHIFT :
                            ((stateKeyMask & MASK_RSHIFT) != 0) ? SCAN_RSHIFT :
                            (byte)(0x80 | SCAN_LSHIFT);
                virtualShiftState = false;
            }

            // Update the state key mask
            stateKeyMask &= ~statebit;
            if (kevt.keyCode == KeyEvent.VK_NUM_LOCK)
                stateKeyMask ^= MASK_NUMLOCK; // flip numlock state

        } else {

            // Figure out how to manipulate the virtual shift state
            boolean flipVirtual = false;
            if ((keyinfo & (KEY_NOSHIFT | KEY_NONUM)) != 0) {
                // Key requires a particular shift state
                boolean realShiftState =
                  ((stateKeyMask & (MASK_LSHIFT|MASK_RSHIFT)) != 0);
                boolean needShiftState =
                  ((keyinfo & KEY_NOSHIFT) == 0) &&
                  ((stateKeyMask & MASK_NUMLOCK) != 0);
                flipVirtual = useVirtualShift &&
                  (virtualShiftState == (realShiftState == needShiftState));
            } else {
                // Modifier or "regular" key; release shift virtualization.
                flipVirtual = useVirtualShift && virtualShiftState;
            }

            if (kevt.keyCode == KeyEvent.VK_PRINTSCREEN) {
                // PrintScreen has special behaviour:
                // Acts as SysRq if Alt key is down, otherwise acts as
                // extended Asterisk with forced Shift unless Ctrl is down.
                isSysRq = ((stateKeyMask & (MASK_LALT|MASK_RALT)) != 0);
                if (!isSysRq) {
                    keyinfo = 55;
                    extend = true;
                    boolean needVirtualShift = ((stateKeyMask &
                      (MASK_LSHIFT|MASK_RSHIFT|MASK_LCTRL|MASK_RCTRL)) == 0);
                    flipVirtual = useVirtualShift &&
                      (virtualShiftState != needVirtualShift);
                }
            }
 
            // Construct key down scan code sequence
            scan = new byte[((extend) ? 2 : 1) + ((flipVirtual) ? 2 : 0)];
            int i = 0;
            if (flipVirtual) {
                scan[i++] = (byte)0xe0;
                scan[i++] = (byte)(
                  ( (virtualShiftState) ? 0x00 : 0x80 ) ^
                  ( ((stateKeyMask & MASK_LSHIFT) != 0) ? SCAN_LSHIFT :
                    ((stateKeyMask & MASK_RSHIFT) != 0) ? SCAN_RSHIFT :
                    (0x80 | SCAN_LSHIFT) ) );
                virtualShiftState = !virtualShiftState;
            }
            if (extend)
                scan[i++] = (byte)0xe0;
            scan[i++] = (byte)keyinfo;

            // Update the state key mask
            stateKeyMask |= statebit;

        }
 
        if (keyboardController != null)
            keyboardController.putKeyData(scan);
    }


    /** Returns the current value of the internal numlock state. */
    public boolean getNumlockState()
    {
        return ((stateKeyMask & MASK_NUMLOCK) != 0);
    }


    /** Sets the value of the internal numlock state. */
    public void setNumlockState(boolean enable)
    {
        if (enable)
            stateKeyMask |= MASK_NUMLOCK;
        else
            stateKeyMask &= ~MASK_NUMLOCK;
    }

}

/* end */
