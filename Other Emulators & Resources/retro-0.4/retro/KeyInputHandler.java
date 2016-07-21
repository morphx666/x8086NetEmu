/*
 *  KeyInputHandler.java
 *  Joris van Rantwijk
 */

package retro;
import java.util.EventObject;
import java.awt.event.KeyEvent;

/**
 * The KeyInputHandler handles key press and key release events.
 * Key events are converted to scan code sequences, which are sent
 * to the keyboard controller component.
 */
public class KeyInputHandler implements ExternalInputHandler
{

    private KeyboardController keyboardController;
    private KeyboardMapXT keymap;

    public KeyInputHandler(KeyboardController kc)
    {
        keyboardController = kc;
        keymap = new KeyboardMapXT();
    }

    public void handleInput(ExternalInputEvent evt)
    {
        KeyInputEvent kevt = (KeyInputEvent)evt;
        byte[] scan = null;
        switch (kevt.id) {
            case KeyEvent.KEY_PRESSED:
                // send make scan code to keyboard controller
                scan = keymap.getMakeCode(kevt.keyCode,
                  kevt.modifiers, kevt.keyLocation);
                break;
            case KeyEvent.KEY_RELEASED:
                // send break scan code to keyboard controller
                scan = keymap.getBreakCode(kevt.keyCode,
                  kevt.modifiers, kevt.keyLocation);
                break;
        }
        if (scan != null)
            keyboardController.putKeyData(scan);
    }

}

/* end */
