/*
 *  KeyboardMapXT.java
 *  Joris van Rantwijk
 */

package retro;
import java.awt.event.KeyEvent;

/**
 * KeyboardMapXT translates keyboard input events from Java VK_xxx-style
 * key codes into XT keyboard scan codes.
 */
public class KeyboardMapXT
{

    // Map Java key code to PC scan code
    private byte[] scantbl;


    // Create and initialize key translator
    public KeyboardMapXT()
    {
        scantbl = new byte[256];
        scantbl[KeyEvent.VK_ESCAPE] = 1;
        scantbl[KeyEvent.VK_1] = 2;
        scantbl[KeyEvent.VK_2] = 3;
        scantbl[KeyEvent.VK_3] = 4;
        scantbl[KeyEvent.VK_4] = 5;
        scantbl[KeyEvent.VK_5] = 6;
        scantbl[KeyEvent.VK_6] = 7;
        scantbl[KeyEvent.VK_7] = 8;
        scantbl[KeyEvent.VK_8] = 9;
       	scantbl[KeyEvent.VK_9] = 10;
        scantbl[KeyEvent.VK_0] = 11;
        scantbl[KeyEvent.VK_MINUS] = 12;
        scantbl[KeyEvent.VK_EQUALS] = 13;
        scantbl[KeyEvent.VK_BACK_SPACE] = 14;
        scantbl[KeyEvent.VK_TAB] = 15;
        scantbl[KeyEvent.VK_Q] = 16;
        scantbl[KeyEvent.VK_W] = 17;
        scantbl[KeyEvent.VK_E] = 18;
        scantbl[KeyEvent.VK_R] = 19;
        scantbl[KeyEvent.VK_T] = 20;
        scantbl[KeyEvent.VK_Y] = 21;
        scantbl[KeyEvent.VK_U] = 22;
        scantbl[KeyEvent.VK_I] = 23;
        scantbl[KeyEvent.VK_O] = 24;
        scantbl[KeyEvent.VK_P] = 25;
        scantbl[KeyEvent.VK_OPEN_BRACKET] = 26;
        scantbl[KeyEvent.VK_CLOSE_BRACKET] = 27;
        scantbl[KeyEvent.VK_ENTER] = 28;
        scantbl[KeyEvent.VK_CONTROL] = 29;
        scantbl[KeyEvent.VK_A] = 30;
        scantbl[KeyEvent.VK_S] = 31;
        scantbl[KeyEvent.VK_D] = 32;
        scantbl[KeyEvent.VK_F] = 33;
        scantbl[KeyEvent.VK_G] = 34;
        scantbl[KeyEvent.VK_H] = 35;
        scantbl[KeyEvent.VK_J] = 36;
        scantbl[KeyEvent.VK_K] = 37;
        scantbl[KeyEvent.VK_L] = 38;
        scantbl[KeyEvent.VK_SEMICOLON] = 39;
        scantbl[KeyEvent.VK_QUOTE] = 40;
        scantbl[KeyEvent.VK_BACK_QUOTE] = 41;
        scantbl[KeyEvent.VK_SHIFT] = 42; // left shift
        scantbl[KeyEvent.VK_BACK_SLASH] = 43;
        scantbl[KeyEvent.VK_BACK_SLASH] = 43;
        scantbl[KeyEvent.VK_Z] = 44;
        scantbl[KeyEvent.VK_X] = 45;
        scantbl[KeyEvent.VK_C] = 46;
        scantbl[KeyEvent.VK_V] = 47;
        scantbl[KeyEvent.VK_B] = 48;
        scantbl[KeyEvent.VK_N] = 49;
        scantbl[KeyEvent.VK_M] = 50;
        scantbl[KeyEvent.VK_COMMA] = 51;
        scantbl[KeyEvent.VK_PERIOD] = 52;
        scantbl[KeyEvent.VK_SLASH] = 53;
        scantbl[KeyEvent.VK_MULTIPLY] = 55;
        scantbl[KeyEvent.VK_ALT] = 56;
        scantbl[KeyEvent.VK_SPACE] = 57;
        scantbl[KeyEvent.VK_CAPS_LOCK] = 58;
        scantbl[KeyEvent.VK_F1] = 59;
        scantbl[KeyEvent.VK_F2] = 60;
        scantbl[KeyEvent.VK_F3] = 61;
        scantbl[KeyEvent.VK_F4] = 62;
        scantbl[KeyEvent.VK_F5] = 63;
        scantbl[KeyEvent.VK_F6] = 64;
        scantbl[KeyEvent.VK_F7] = 65;
        scantbl[KeyEvent.VK_F8] = 66;
        scantbl[KeyEvent.VK_F9] = 67;
        scantbl[KeyEvent.VK_F10] = 68;
        scantbl[KeyEvent.VK_NUM_LOCK] = 69;
        scantbl[KeyEvent.VK_SCROLL_LOCK] = 70;
        scantbl[KeyEvent.VK_NUMPAD7] = 71;
        scantbl[KeyEvent.VK_HOME] = 71;
        scantbl[KeyEvent.VK_NUMPAD8] = 72;
        scantbl[KeyEvent.VK_UP] = 72;
        scantbl[KeyEvent.VK_KP_UP] = 72;
        scantbl[KeyEvent.VK_NUMPAD9] = 73;
        scantbl[KeyEvent.VK_PAGE_UP] = 73;
        scantbl[KeyEvent.VK_SUBTRACT] = 74;
        scantbl[KeyEvent.VK_NUMPAD4] = 75;
        scantbl[KeyEvent.VK_LEFT] = 75;
        scantbl[KeyEvent.VK_KP_LEFT] = 75;
        scantbl[KeyEvent.VK_NUMPAD5] = 76;
        scantbl[KeyEvent.VK_NUMPAD6] = 77;
        scantbl[KeyEvent.VK_RIGHT] = 77;
        scantbl[KeyEvent.VK_KP_RIGHT] = 77;
        scantbl[KeyEvent.VK_ADD] = 78;
        scantbl[KeyEvent.VK_NUMPAD1] = 79;
        scantbl[KeyEvent.VK_END] = 79;
        scantbl[KeyEvent.VK_NUMPAD2] = 80;
        scantbl[KeyEvent.VK_DOWN] = 80;
        scantbl[KeyEvent.VK_KP_DOWN] = 80;
        scantbl[KeyEvent.VK_NUMPAD3] = 81;
        scantbl[KeyEvent.VK_PAGE_DOWN] = 81;
        scantbl[KeyEvent.VK_NUMPAD0] = 82;
        scantbl[KeyEvent.VK_INSERT] = 82;
        scantbl[KeyEvent.VK_DECIMAL] = 83;
        scantbl[KeyEvent.VK_DELETE] = 83;
        scantbl[KeyEvent.VK_PRINTSCREEN] = 84; // sysreq
        scantbl[KeyEvent.VK_F11] = 86;
        scantbl[KeyEvent.VK_F12] = 87;
    }

    // Translate a Java keyPressed event into a make scan code sequence
    public byte[] getMakeCode(int keyCode, int modifiers, int keyLocation)
    {
        byte scancode = 0;
        if (keyCode < scantbl.length)
            scancode = scantbl[keyCode];
        if (scancode == 0) {
            System.err.println("getMakeCode: unknown keycode " + keyCode + "=" + KeyEvent.getKeyText(keyCode));
            return null;
        }
        byte[] b = new byte[1];
        b[0] = scancode;
        return b;
    }


    // Translate a Java keyReleased event into a break scan code sequence
    public byte[] getBreakCode(int keyCode, int modifiers, int keyLocation)
    {
        byte scancode = 0;
        if (keyCode < scantbl.length)
            scancode = scantbl[keyCode];
        if (scancode == 0) {
            System.err.println("getBreakCode: unknown keycode " + keyCode + "=" + KeyEvent.getKeyText(keyCode));
            return null;
        }
        byte[] b = new byte[1];
        b[0] = (byte) (scancode | 0x80);
        return b;
    }


    // Translate a Java keyPressed event into a repeat scan code sequence
    public byte[] getRepeatCode(int keyCode, int modifiers, int keyLocation)
    {
        byte scancode = 0;
        if (keyCode < scantbl.length)
            scancode = scantbl[keyCode];
        if (scancode == 0)
            return null;
        byte[] b = new byte[1];
        b[0] = scancode;
        return b;
    }

}

/* end */
