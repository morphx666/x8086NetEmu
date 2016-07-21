/*
 *  Retro.java
 *  Joris van Rantwijk
 */

package retro;
import java.io.*;
import java.net.URL;
import java.util.Properties;
import java.util.Enumeration;
import java.awt.event.WindowAdapter;
import java.awt.event.WindowEvent;


/**
 * Application main class.
 */
public class Retro
  implements Cpu.InterruptHook, Cpu.TraceHook, ExternalInputHandler
{

    protected static Logger log = Logger.getLogger("main");
    protected static Logger logdisk = Logger.getLogger("disk");

    Scheduler sched;
    Cpu cpu;
    Memory mem;
    IOPorts io;
    IOMisc iomisc;
    Cga cga;
    I8259 intctl;
    I8237 dmactl;
    I8254 timer;
    FloppyController fdctl;
    KeyboardController kbdctl;
    ExternalInputHandler keyboard;

    DiskImage floppyaImage;
    DiskImage floppybImage;

    TerminalFrame frame;

    private int tracecount = -1;
    private OutputStream dumpf;

    
    /** Convert string to boolean value. */
    public static final boolean strToBoolean(String s, boolean d)
    {
        if (s == null || s.length() == 0) return d;
        s = s.toLowerCase();
        if (s.equals("true")) return true;
        if (s.equals("false")) return false;
        throw new IllegalArgumentException(
          "Expected 'true' or 'false'; got '" + s + "'");
    }


    /** Handle intercepted interrupts. */
    public int interruptHook(int intno, int[] reg, int[] sreg, int[] flags)
    {
        if (intno != 0xe6) {
            log.info("int" + Misc.byteToHex(intno) + ": ax=" + Misc.wordToHex(reg[Cpu.regAX]) + " cx=" + Misc.wordToHex(reg[Cpu.regCX]) + " dx=" + Misc.wordToHex(reg[Cpu.regDX]));
            return intno;
        }

        int ax = reg[Cpu.regAX];
        if ((ax >> 8) == 0xfe) {
            int[] fakeflags = new int[1];
            reg[Cpu.regAX] = mem.loadWord(
              (sreg[Cpu.sregSS] << 4) + reg[Cpu.regSP]);
			  
            fakeflags[0] = mem.loadWord(
              (sreg[Cpu.sregSS] << 4) + ((reg[Cpu.regSP] + 6) & 0xffff));
			  
            switch (ax & 0xff) {
              case 0x13: diskIntHook(reg, sreg, fakeflags); break;
              default: log.warn("Intercepted unknown interrupt 0x" + Misc.byteToHex(intno)); break;
            }
            mem.storeWord(
              (sreg[Cpu.sregSS] << 4) + reg[Cpu.regSP],
              reg[Cpu.regAX]);
            mem.storeWord(
              (sreg[Cpu.sregSS] << 4) + ((reg[Cpu.regSP] + 6) & 0xffff),
              fakeflags[0]);
            reg[Cpu.regAX] = ax;
        } else {
            log.warn("Unknown function int 0xe6 ax=" + Misc.wordToHex(ax));
        }
        return -1;
    }


    /** Write instruction trace to dump file. */
    public void traceHook()
    {
        if (tracecount < 0) return;
        try {
            if (tracecount == 1) {
                dumpf = new FileOutputStream("dump.start");
                byte[] state = cpu.getStateData();
                dumpf.write(state);
                dumpf.write(mem.mem);
                dumpf.close();
                dumpf = new BufferedOutputStream(
                  new FileOutputStream("dump.trace"));
            }
            if (tracecount > 1) {
                byte[] state = cpu.getStateData();
                dumpf.write(state);
            }
            tracecount++;
        } catch (IOException e) {
            e.printStackTrace();
            System.exit(1);
        }
    }


    /** Handle interrupt 0x13 (soft-emulation of floppy drives) */
    void diskIntHook(int[] reg, int[] sreg, int[] flags)
    {
        int ret;
        int ax = reg[Cpu.regAX];
        int bx = reg[Cpu.regBX];
        int cx = reg[Cpu.regCX];
        int dx = reg[Cpu.regDX];
        int es = sreg[Cpu.sregES];

        // Select floppy drive
        DiskImage diskimage = null;
        if ((dx & 0xff) == 0) diskimage = floppyaImage; // Drive A:
        if ((dx & 0xff) == 1) diskimage = floppybImage; // Drive B:

        // Branch on function code
        switch ((ax >> 8) & 0xff) {
            case 0x00: // reset drive
                logdisk.info("reset drive (dx=" + Misc.wordToHex(dx) + ")");
                ret = 0;
                break;
            case 0x01: // get last operation status
                logdisk.info("get status (dx=" + Misc.wordToHex(dx) + ")");
                ret = mem.loadByte(0x441);
                if (ret != 0) ret |= 0x100;
                break;
            case 0x02: { // read sectors
                int track = cx >> 8;
                int sect = cx & 0xff;
                int head = dx >> 8;
                int n = ax & 0xff;
                int addr = (es << 4) + bx;
                logdisk.info("read sectors (drive=" + (dx & 0xff) + ", track=" + track + ", sect=" + sect + ", head=" + head + ", n=" + n + ")");
                if (diskimage == null) {
                    ret = 0x180; // drive not ready
                    break;
                }
                long offset = diskimage.mapChsToOffset(track, head, sect);
                if (offset < 0) {
                    ret = 0x140; // seek failed
                    break;
                }
                byte[] buf = new byte[n * 512];
                ret = diskimage.read(offset, buf);
                if (ret == DiskImage.EIO) {
                    ret = 0x110; // CRC error
                    break;
                } else if (ret == DiskImage.EOF) {
                    ret = 0x104; // sector not found
                    break;
                }
                mem.loadData(addr, buf);
                ret = 0;
                break;
            }
            case 0x03: { // write sectors
                int track = cx >> 8;
                int sect = cx & 0xff;
                int head = dx >> 8;
                int n = ax & 0xff;
                int addr = (es << 4) + bx;
                logdisk.info("write sectors (drive=" + (dx & 0xff) + ", track=" + track + ", sect=" + sect + ", head=" + head + ", n=" + n + ")");
                if (diskimage == null) {
                    ret = 0x180; // drive not ready
                    break;
                }
                if (diskimage.readOnly()) {
                    log.warn("rejecting write to readonly disk image");
                    ret = 0x103; // write protected
                    break;
                }
                long offset = diskimage.mapChsToOffset(track, head, sect);
                if (offset < 0) {
                    ret = 0x140; // seek failed
                    break;
                }
                byte[] buf = new byte[n * 512];
                System.arraycopy(mem.mem, addr, buf, 0, n * 512);
                ret = diskimage.write(offset, buf);
                if (ret == DiskImage.EIO) {
                    ret = 0x110; // CRC error
                    break;
                } else if (ret == DiskImage.EOF) {
                    ret = 0x104; // sector not found
                    break;
                }
                ret = 0;
                break;
            }
            case 0x08: { // get drive parameters
                // Note: this function is all wrong; we return disk geometry,
                // while we should return drive geometry. This will matter
                // once we support run-time ejecting/changing of disks.
                logdisk.info("get drive parameters (dx=" + Misc.wordToHex(dx) + ")");
                if (diskimage == null) {
                    ret = 0x180; // no such drive
                    break;
                }
                // get disk geometry
                int ndrives =
                  (floppyaImage != null && floppybImage != null) ? 2 : 1;
                long size = diskimage.getSize();
                int maxtrack = diskimage.getNumCylinders() - 1;
                int maxhead = diskimage.getNumHeads() - 1;
                int nsect = diskimage.getNumSectors();
                if (maxtrack < 0) {
                    // disk geometry is unknown
                    logdisk.error("Unknown disk geometry");
                    ret = 0x107;
                } else {
                    // return drive parameters
                    dx = ((maxhead << 8) & 0xff00) | (ndrives & 0x00ff);
                    cx = ((maxtrack << 8) * 0xff00) |
                         ((maxtrack >> 2) & 0x00c0) |
                         (nsect & 0x007f);
                    bx &= 0xff00;
                    if (size == 360 * 1024)       bx |= 0x0001;
                    else if (size == 1200 * 1024) bx |= 0x0002;
                    else if (size == 720 * 1024)  bx |= 0x0003;
                    else if (size == 1440 * 1024) bx |= 0x0004;
                    else if (size == 2880 * 1024) bx |= 0x0006;
                    reg[Cpu.regBX] = bx;
                    reg[Cpu.regCX] = cx;
                    reg[Cpu.regDX] = dx;
                    // point ES:DI to disk parameter table at F000:EFC7
                    reg[Cpu.regDI] = 0xefc7;
                    sreg[Cpu.sregES] = 0xf000;
                    ret = 0;
                }
                break;
            }
            default:
                logdisk.info("unknown function ax=" + Misc.wordToHex(ax));
                ret = 0x101;
                break;
        }

        // Store return status
        mem.storeByte(0x441, ret);
        if ((ret & 0xff00) != 0) {
            flags[0] |= Cpu.flCF; // set cary flag
            reg[Cpu.regAX] = (ret << 8);
        } else {
            flags[0] &= ~ Cpu.flCF; // clear cary flag
            reg[Cpu.regAX] = (ax & 0x00ff) | ((ret << 8) & 0xff00);
        }
    }


    /** Dispatch external input events (currently only keyboard). */
    public void handleInput(ExternalInputEvent evt)
    {
        if (evt instanceof KeyInputEvent)
            keyboard.handleInput(evt);
    }


    /** Setup simulation with given configuration. */
    public void initvm(Properties cfg)
    {
        // Create simulation scheduler
        sched = new Scheduler();
        Logger.setScheduler(sched);

        // Create core simulation objects
        io = new IOPorts();
        mem = new Memory();
        cpu = new Cpu(sched, mem, io);
        sched.setCpu(cpu);

        // Configure CPU speed
        String cpuspeed = cfg.getProperty("cpuspeed");
        if (cpuspeed != null)
            cpu.setCyclesPerSecond(Integer.parseInt(cpuspeed));

        // Configure synchronization parameters
        boolean syncScheduler =
          strToBoolean(cfg.getProperty("syncScheduler"), true);
        double syncQuantum = cfg.containsKey("syncQuantum") ?
          Double.parseDouble(cfg.getProperty("syncQuantum")) : 0.05;
        double syncSimulationSpeed = cfg.containsKey("syncSimulationSpeed") ? 
          Double.parseDouble(cfg.getProperty("syncSimulationSpeed")) : 1.0;
        sched.setSynchronization(syncScheduler,
          (long)(Scheduler.CLOCKRATE * syncQuantum),
          (long)(Scheduler.CLOCKRATE * syncSimulationSpeed / 1000.0));
        
        // Create GUI window
        frame = new TerminalFrame("Retro - Initializing");
        frame.show();

        // Stop simulation when user closes the window
        frame.addWindowListener(new WindowAdapter() {
            public void windowClosing(WindowEvent e) {
                log.info("Stopping simulation");
                sched.stopSimulation();
            }
        });

        // Interrupt controller
        intctl = new I8259(sched);
        io.registerHandler(intctl, 0x20, 2);
        cpu.setInterruptController(intctl);

        // DMA controller
        dmactl = new I8237(sched, mem);
        io.registerHandler(dmactl, 0x00, 16);
        io.registerHandler(dmactl, 0x80, 8);

        // Timer
        timer = new I8254(sched, intctl.getIrqLine(0), dmactl);
        io.registerHandler(timer, 0x40, 4);

        // CGA card
        String cgaupdatefreq = cfg.getProperty("cgaupdatefreq");
        if (cgaupdatefreq != null && cgaupdatefreq.length() > 0) {
            long updateperiod =
              (long) (Scheduler.CLOCKRATE / Double.parseDouble(cgaupdatefreq));
            cga = new Cga(sched, mem, frame.getDisplay(), updateperiod);
        } else {
            cga = new Cga(sched, mem, frame.getDisplay());
        }
        io.registerHandler(cga, 0x3d0, 16);

        // Floppy controller
        fdctl = new FloppyController(sched, intctl.getIrqLine(6), dmactl.getChannel(2));
        dmactl.bindChannel(2, fdctl);
        io.registerHandler(fdctl, 0x3f0, 8);

        // Keyboard controller
        kbdctl = new KeyboardController(sched, intctl.getIrqLine(1));
        kbdctl.setTimer(timer);
        io.registerHandler(kbdctl, 0x60, 4);

        // Input handling
        keyboard = new KeyboardExt(kbdctl);
        sched.setInputHandler(this);
        frame.getDisplay().setInputHandler(sched);

        // Register I/O handlers
        iomisc = new IOMisc();
        io.registerHandler(iomisc, 0xe600, 4);

        // Suppress warnings for unimplemented I/O on well known ports
        io.registerHandler(iomisc, 0x201, 1); // joystick

        // Register interrupt hooks
        cpu.setInterruptHook(this, 0xe6);

        try {
            // Load ROM image into memory
            String romfile = cfg.getProperty("romfile", "bios/rombios.bin");
            InputStream fin =
              Retro.class.getClassLoader().getResourceAsStream(romfile);
            byte[] buf = new byte[65536];
            int k = 0;
            while (k < buf.length) {
                int t = fin.read(buf, k, buf.length - k);
                if (t < 0)
                    break;
                k += t;
            }
            fin.close();
            byte[] xbuf = new byte[k];
            System.arraycopy(buf, 0, xbuf, 0, k);
            mem.loadData(0x100000 - k, xbuf);
            log.info("Loading ROM: " + k + " bytes from " + romfile);

            // Load/prepare disk image
            String diskfile = cfg.getProperty("floppyaimg");
            boolean readonly =
              strToBoolean(cfg.getProperty("floppyareadonly"), true);
            if (diskfile != null && diskfile.length() > 0) {
                log.info("Opening floppy A: image " + diskfile);
                floppyaImage = new DiskImage(diskfile, readonly);
            } else if (cfg.containsKey("floppyaurl") &&
                       cfg.getProperty("floppyaurl").length() > 0) {
                diskfile = cfg.getProperty("floppyaurl");
                log.info("Loading floppy A: from URL " + diskfile);
                fin = new URL(diskfile).openStream();
                floppyaImage = new DiskImageBuf(fin, readonly);
                fin.close();
            }
            diskfile = cfg.getProperty("floppybimg");
            readonly = strToBoolean(cfg.getProperty("floppybreadonly"), true);
            if (diskfile != null && diskfile.length()>0) {
                log.info("Opening floppy B: image " + diskfile);
                floppybImage = new DiskImage(diskfile, readonly);
            } else if (cfg.containsKey("floppybresource") &&
                       cfg.getProperty("floppybresource").length() > 0) {
                diskfile = cfg.getProperty("floppybresource");
                log.info("Loading floppy B: from URL " + diskfile);
                fin = new URL(diskfile).openStream();
                floppybImage = new DiskImageBuf(fin, readonly);
                fin.close();
            }

        } catch (IOException e) {
            e.printStackTrace();
            System.exit(1);
        }

        // Attach floppy images to floppy controller
        fdctl.attachImage(0, floppyaImage);
        fdctl.attachImage(1, floppybImage);

        // Set hardware configuration switches
        kbdctl.setSwitchData((floppybImage == null) ? 0x2d : 0x6d);

        // Set frame title
        String appname = cfg.getProperty("name");
        frame.setTitle((appname != null && appname.length() > 0) ?
                       ("Retro - " + appname) : "Retro");
    }


    /** Run simulation (previously prepared through initvm). */
    public void runvm() {
        try {
            sched.run();
        } catch (Cpu.InvalidOpcodeException e) {
            e.printStackTrace();
        }
    }


    /** Stop a running simulation. */
    public void stopvm()
    {
        if (sched != null) {
            log.info("Stopping simulation");
            sched.stopSimulation();
        }
    }


    /** Clean up after a simulation run. */
    public void cleanvm() {
        if (frame != null) {
            frame.dispose();
            frame = null;
        }
    }


    /** Print usage instructions and exit. */
    static void usage(String errmsg)
    {
        System.err.println("Usage: java Retro [--config configfile] [--floppyaimg diskimage] [options ...]");
        System.err.println();
        if (errmsg != null)
            System.err.println("ERROR: " + errmsg);
        System.exit(1);
    }


    /** Application main method. */
    public static void main(String[] args)
    {
        // Find configuration file
        String configfile = null;
        for (int i = 0; i < args.length; i += 2) {
            if (!args[i].startsWith("--"))
                usage("Invalid option syntax: '" + args[i] + "'");
            if (i + 1 >= args.length)
                usage("Missing argument for option " + args[i]);
            if (args[i].equals("--config"))
                configfile = args[i+1];
        }

        // Read configuration file
        Properties cfgprops = new Properties();
        if (configfile != null) {
            log.info("Reading configuration from '" + configfile + "'");
            try {
                InputStream inf = new FileInputStream(configfile);
                cfgprops.load(inf);
                inf.close();
            } catch (IOException e) {
                log.fatal("ERROR: Can not read configuration file.", e);
                System.exit(1);
            }
        }

        // Add configuration options from command line
        for (int i = 0; i < args.length; i += 2) {
            if (!args[i].startsWith("--"))
                usage("Invalid option syntax: '" + args[i] + "'");
            if (i + 1 >= args.length)
                usage("Missing argument for option " + args[i]);
            cfgprops.put(args[i].substring(2), args[i+1]);
        }

        // Configure logging system
        if (cfgprops.containsKey("loglevel")) {
            String cfgval = cfgprops.getProperty("loglevel");
            int level = Logger.getLogLevelByName(cfgval.toUpperCase());
            if (level == 0) {
                log.fatal("Unknown default log level '" + cfgval + "'");
                System.exit(1);
            } else {
                Logger.setDefaultLevel(level);
            }
        }
        for (Enumeration e = cfgprops.propertyNames(); e.hasMoreElements(); ) {
            String cfgkey = (String) e.nextElement();
            if (cfgkey.startsWith("loglevel.")) {
                String cfgval = cfgprops.getProperty(cfgkey);
                int level = Logger.getLogLevelByName(cfgval.toUpperCase());
                if (level == 0) {
                    log.fatal(
                      "Unknown log level '" + cfgval + "' for " + cfgval);
                    System.exit(1);
                } else {
                    Logger.getLogger(
                      cfgkey.substring("loglevel.".length())).setLevel(level);
                }
            }
        }

        // Run simulation
        Retro app = new Retro();
        app.initvm(cfgprops);
        app.runvm();
    }

}

/* end */
