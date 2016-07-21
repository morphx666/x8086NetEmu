/*
 *  RetroApplet.java
 *  Joris van Rantwijk
 */

package retro;
import java.applet.*;
import java.awt.*;
import java.awt.event.ActionEvent;
import java.awt.event.ActionListener;
import java.util.Properties;
import java.awt.event.WindowEvent;


/**
 * Applet version of Retro.
 */
public class RetroApplet extends Applet implements ActionListener, Runnable
{

    private Button startButton;
    private Choice configChoice;
    private Thread simThread;
    private Retro simApp;
    private volatile boolean stopSim;

    // stupid applet interface cannot enumerate its parameters
    private String[] paramNames = {
        "name",
        "cpuspeed",
        "floppyaimg", "floppyareadonly", "floppyaurl",
        "floppybimg", "floppybreadonly", "floppyburl",
        "cgaupdatefreq",
        "romfile",
        "syncScheduler", "syncSimulationSpeed", "syncQuantum" };


    /** Return applet name and short description. */
    public String getAppletInfo()
    {
        return "RetroApplet\nApplet version of the Retro PC simulator.\n";
    }


    /** Return list of supported parameter names. */
    public String[][] getParameterInfo()
    {
        String[][] info = new String[paramNames.length][3];
        for (int i = 0; i < paramNames.length; i++) {
            info[i][0] = paramNames[i];
            info[i][1] = "String";
            info[i][2] = "";
        }
        return info;
    }
    

    /** Initialize applet; show launch button. */
    public void init()
    {
        // List of configurations to choose from
        configChoice = new Choice();
        for (int i = 1; ; i++) {
            String s = getParameter("cfg" + i + "_name");
            if (s == null) {
                if (i == 0)
                    configChoice.add("Default");
                break;
            }
            configChoice.add(s);
        }
        add(configChoice);

        // Start button
        startButton = new Button("Start simulation");
        add(startButton);
        startButton.addActionListener(this);
    }


    /** Handle button clicks. */
    public void actionPerformed(ActionEvent e)
    {
        if (e.getSource() == startButton)
            startRetro();
    }


    /** Start the simulation. */
    private synchronized void startRetro()
    {
        if (simThread != null && simThread.isAlive())
            return; // already running
        startButton.setEnabled(false);
        simThread = new Thread(this);
        simThread.start();
        stopSim = false;
    }


    /** Stop simulation if it was running. */
    public synchronized void stop()
    {
        if (simApp != null)
            simApp.stopvm();
        stopSim = true;
    }


    /** Main method for the simulation thread. */
    public void run()
    {
        int cfgindex = configChoice.getSelectedIndex() + 1;

        // Pass applet parameters as configuration properties
        Properties cfgprops = new Properties();
        for (int i = 0; i < paramNames.length; i++) {
            String s = getParameter("cfg" + cfgindex + "_" + paramNames[i]);
            if (s == null)
                s = getParameter(paramNames[i]);
            if (s != null)
                cfgprops.put(paramNames[i], s);
        }

        // Run simulation
        simApp = new Retro();
        simApp.initvm(cfgprops);
        if (!stopSim)
            simApp.runvm();
        simApp.cleanvm();

        // Cleanup
        synchronized (this) { simApp = null; }
        startButton.setEnabled(true);
    }
        
}

/* end */
