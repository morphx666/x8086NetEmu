/*
 *  Logger.java
 *  Joris van Rantwijk
 */

package retro;

import java.io.PrintStream;
import java.util.HashMap;

/**
 * Message logging system.
 *
 * Why write a logging system from scratch?
 * Because it is the easiest way.
 *
 * Why not log4j?
 * Complex configuration, external JAR file, license issues.
 *
 * Why not java.util.logging?
 * Complex, strange log levels, depends on 1.4, applet security issues.
 *
 * So there.
 */
public final class Logger
{

    /* Log levels */
    public static final int DEBUG = 1;
    public static final int INFO = 2;
    public static final int WARN = 3;
    public static final int ERROR = 4;
    public static final int FATAL = 5;

    /** Map log level to string. */
    private static final String[] logLevelName = {
      "?", "DEBUG", "INFO", "WARN", "ERROR", "FATAL" };

    /** Default log level for unconfigured loggers. */
    private static volatile int defaultLogLevel = INFO;

    /** Log stream. */
    private static PrintStream logstream = System.err;

    /** Map names to logger objects. */
    private static HashMap loggerTable = new HashMap();

    /** Scheduler object for log timestamps. */
    private static Scheduler sched;
    
    private final String name;
    private volatile int logLevel;

    /** Return the logger object with the given name. */
    public static synchronized Logger getLogger(String name)
    {
        Logger log = (Logger) loggerTable.get(name);
        if (log == null && name != null) {
            log = new Logger(name);
            loggerTable.put(name, log);
        }
        return log;
    }

    /** Set the scheduler object to consult for log timestamps. */
    public static synchronized void setScheduler(Scheduler sched)
    {
        Logger.sched = sched;
    }

    /** Return the default log level. */
    public static int getDefaultLevel()
    {
        return defaultLogLevel;
    }

    /**
     * Change the default log level.
     * The default log level is used by Logger objects that have no explicitly
     * configured log level.
     */
    public static void setDefaultLevel(int level)
    {
        defaultLogLevel = level;
    }

    /** Return named log level, or 0 if no such log level exists. */
    public static int getLogLevelByName(String levelName)
    {
        for (int i = 1; i <= FATAL; i++)
            if (levelName.equals(logLevelName[i]))
                return i;
        return 0;
    }
    
    /** Construct a named logger object with default log level. */
    protected Logger(String name)
    {
        this.name = name;
        this.logLevel = 0;
    }

    /** Return the current log level. */
    public int getLevel()
    {
        return this.logLevel;
    }

    /** Change the log level for this Logger object. */
    public void setLevel(int level)
    {
        this.logLevel = level;
    }

    /** Return true if messages at the given log level are enabled. */
    public final boolean isEnabledFor(int level)
    {
        // Let's hope that the compiler will inline these calls
        int m = logLevel;
        if (m == 0)
            m = defaultLogLevel;
        return (level >= m);
    }

    /** Log a message. */
    public void log(int level, String msg)
    {
        if (isEnabledFor(level))
            doLog(name, level, msg, null);
    }

    /** Log an exception. */
    public void log(int level, String msg, Throwable e)
    {
        if (isEnabledFor(level))
            doLog(name, level, msg, e);
    }

    /** Log a failed assertion. */
    public void assertLog(boolean cond, String msg)
    {
        if (!cond && isEnabledFor(ERROR)) {
            Exception e = new Exception("Assertion failed");
            e.fillInStackTrace();
            doLog(name, ERROR, msg, e);
        }
    }

    /** Log a debug message. */
    public void debug(String msg)
    {
        if (isEnabledFor(DEBUG))
            doLog(name, DEBUG, msg, null);
    }

    /** Log an informational message. */
    public void info(String msg)
    {
        if (isEnabledFor(INFO))
            doLog(name, INFO, msg, null);
    }

    /** Log a warning message. */
    public void warn(String msg)
    {
        if (isEnabledFor(WARN))
            doLog(name, WARN, msg, null);
    }

    /** Log an error message. */
    public void error(String msg)
    {
        if (isEnabledFor(ERROR))
            doLog(name, ERROR, msg, null);
    }

    /** Log an error exception. */
    public void error(String msg, Throwable e)
    {
        if (isEnabledFor(ERROR))
            doLog(name, ERROR, msg, e);
    }

    /** Log a fatal error message. */
    public void fatal(String msg)
    {
        if (isEnabledFor(FATAL))
            doLog(name, FATAL, msg, null);
    }

    /** Log a fatal exception. */
    public void fatal(String msg, Throwable e)
    {
        if (isEnabledFor(FATAL))
            doLog(name, FATAL, msg, e);
    }

    /** Return true if debug messages are enabled. */
    public boolean isDebugEnabled() { return isEnabledFor(DEBUG); }

    /** Return true if informational messages are enabled. */
    public boolean isInfoEnabled() { return isEnabledFor(INFO); }

    /** Return true if warning messages are enabled. */
    public boolean isWarnEnabled() { return isEnabledFor(WARN); }

    /** Return true if error messages are enabled. */
    public boolean isErrorEnabled() { return isEnabledFor(ERROR); }

    /** Return true if fatal error messages are enabled. */
    public boolean isFatalEnabled() { return isEnabledFor(FATAL); }

    /** Send a logged message to the log stream. */
    protected static synchronized void doLog(String name, int level, String msg, Throwable e)
    {
        StringBuffer sbuf = new StringBuffer(256);
        if (sched != null) {
            sbuf.append("00000000000000");
            sbuf.append(sched.getCurrentTime());
            sbuf.delete(0, (sbuf.length()<28) ? (sbuf.length()-14) : 14);
            sbuf.append('\t');
        }
        sbuf.append(logLevelName[level]);
        sbuf.append('\t');
        sbuf.append(name);
        sbuf.append('\t');
        sbuf.append(msg);
        logstream.println(sbuf.toString());
        if (e != null)
            e.printStackTrace(logstream);
    }

}

/* end */
