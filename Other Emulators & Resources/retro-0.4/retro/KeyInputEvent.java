/*
 *  KeyInputEvent.java
 *  Joris van Rantwijk
 */

package retro;
import java.lang.reflect.Method;
import java.lang.reflect.InvocationTargetException;
import java.awt.event.KeyEvent;

/**
 * A KeyInputEvent represents a key press or key release event.
 */
public class KeyInputEvent extends ExternalInputEvent
{

    public int id;

    /** Key code as a Java KeyEvent.VK_xxx value. */
    public int keyCode;

    /** Key modifier mask at time of event. */
    public int modifiers;

    /** Optional key location as a Java KeyEvent.KEY_LOCATION_xxx value. */
    public int keyLocation;

    /** Reference to KeyEvent.getKeyLocation() if that method is supported. */
    private static Method getKeyLocationMethod;

    static {
        try {
            getKeyLocationMethod =
              KeyEvent.class.getMethod("getKeyLocation", new Class[0]);
            Logger.getLogger("main").debug(
              "Using KeyEvent.getKeyLocation()");
        } catch (NoSuchMethodException e) {
            getKeyLocationMethod = null;
            Logger.getLogger("main").debug(
              "No support for KeyEvent.getKeyLocation()");
        }
    }

   
    public KeyInputEvent(KeyEvent e)
    {
        super(e.getSource());
        id = e.getID();
        modifiers = e.getModifiers();
        timestamp = e.getWhen();
        keyCode = e.getKeyCode();
        if (getKeyLocationMethod == null) {
            // no support for key locations
            keyLocation = 0;
        } else {
            try {
                keyLocation = ((Integer)
                  getKeyLocationMethod.invoke(e, new Object[0])).intValue();
            } catch (InvocationTargetException err) {
                throw new Error(err.getTargetException().getMessage());
            } catch (IllegalAccessException err) {
                throw new Error(err.getMessage());
            }
            
        }
    }

}

/* end */
