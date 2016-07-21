/*
 *  TerminalFrame.java
 *  Joris van Rantwijk
 */

package retro;
import java.awt.*;
import java.awt.event.*;


/**
 * Top-level application window, containing the terminal canvas.
 */
public class TerminalFrame extends Frame
{

    protected Logger log = Logger.getLogger("TerminalFrame");

    private TerminalCanvas display;
    private boolean gotfocus;


    /** Create a TerminalFrame and its interior components. */
    public TerminalFrame()
    {
        super();
        setupFrame();
    }


    /** Create a titled TerminalFrame and its interior components. */
    public TerminalFrame(String title)
    {
        super(title);
        setupFrame();
    }


    /** Return the Terminal component. */
    public TerminalCanvas getDisplay()
    {
        return display;
    }


    /** Setup the Frame and create interior components. */
    private void setupFrame()
    {
        display = new TerminalCanvas();
        add(display);
/*
		ScrollPane scrollPane = new ScrollPane(ScrollPane.SCROLLBARS_AS_NEEDED);
		scrollPane.add(display);
		add(scrollPane);
*/

        setResizable(true);
        pack();

        display.requestFocus();
		
        addWindowListener(new WindowAdapter() {
            public void windowClosing(WindowEvent e) {
                TerminalFrame.this.dispose();
            }
        });

        // Keep track of our focus for the dirty hack below
        addFocusListener(new FocusAdapter() {
            public void focusGained(FocusEvent event) {
                gotfocus = true;
                log.debug("gotfocus");
            }
            public void focusLost(FocusEvent event) {
                gotfocus = false;
                log.debug("lostfocus");
            }
        });

        // Dirty hack to keep the canvas focused on buggy
        // Java implementations over X. Copied from TightVNC.
        display.addMouseMotionListener(new MouseMotionAdapter() {
            public void mouseMoved(MouseEvent event) {
                if (gotfocus)
                    display.requestFocus();
            }
        });
    }

}

/* end */
