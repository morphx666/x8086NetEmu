/*
 *  DiskImage.java
 *  Joris van Rantwijk
 */

package retro;
import java.io.*;

/**
 * Disk image access.
 *
 * This class provides access to an open disk image file, translating
 * read and write requests directly into read/write operations on the file.
 */
public class DiskImage
{

    protected static Logger log = Logger.getLogger("disk");

    public static final int EOF = -1;
    public static final int EIO = -2;

    protected static final int[][] geometryTable = {
        { 40, 1,  8,  160 * 1024 },
        { 40, 2,  8,  320 * 1024 },
        { 40, 1,  9,  180 * 1024 },
        { 40, 2,  9,  360 * 1024 },
        { 80, 2,  9,  720 * 1024 },
        { 80, 2, 15, 1200 * 1024 },
        { 80, 2, 18, 1440 * 1024 },
        { 80, 2, 36, 2880 * 1024 } };

    protected RandomAccessFile file;
    protected boolean readonly;
    protected int ntrack, nhead, nsect, sectsize;


    /** Default constructor to allow subclass constructors. */
    protected DiskImage() { }


    /** Construct a disk image backed by a local file. */
    public DiskImage(String filename, boolean readonly) throws IOException
    {
        File path = new File(filename);
        if (!path.isFile()) {
            log.error("disk image file not found '" + filename + "'");
            throw new FileNotFoundException();
        }
        this.readonly = readonly;
        if (!this.readonly) {
            try {
                file = new RandomAccessFile(path, "rw");
            } catch (IOException e) {
                log.warn("error opening '" + filename + "'" +
                         ", retrying in readonly mode");
                this.readonly = true;
            }
        }
        if (this.readonly) {
            file = new RandomAccessFile(path, "r");
        }
        matchGeometry();
        if (ntrack < 0) {
            log.error("unsupported disk image '" + filename + "'" +
                      " (file size not recognized)");
            throw new IOException("unsupported file format");
        }
        log.info("opened disk image '" + filename + "'" +
                 " (ntrack=" + ntrack + ", nhead=" + nhead +
                 ", nsect=" + nsect + ", sectsize=" + sectsize +
                 (readonly ? ", readonly" : "") + ")");
    }


    /** Guess disk geometry of the image based on its size. */
    protected void matchGeometry()
    {
        ntrack = -1;
        nhead = -1;
        nsect = -1;
        long size = getSize();
        for (int i = 0; i < geometryTable.length; i++) {
            if (size == geometryTable[i][3]) {
                ntrack = geometryTable[i][0];
                nhead = geometryTable[i][1];
                nsect = geometryTable[i][2];
                sectsize = 512;
                break;
            }
        }
    }


    /**
     * Map disk CHS location to linear offset in image.
     * @return byte offset, or -1 for failure.
     * */
    public long mapChsToOffset(int track, int head, int sect)
    {
        if (track < 0 || track >= ntrack ||
            sect <= 0 || sect > nsect ||
            head < 0 || head > nhead) {
            log.warn("can not map CHS to offset (track=" + track +
                     ", head=" +head + ", sector=" + sect + ")");
            return -1;
        }
        long s = (long)((track * nhead) + head) * nsect + sect - 1;
        return s * sectsize;
    }


    /** Close the disk image file. */
    public void close()
    {
        try {
            file.close();
        } catch (IOException e) {
            log.error("error closing disk image", e);
        }
    }


    /** Return true iff the disk image is opened for read-only access. */
    public boolean readOnly()
    {
        return readonly;
    }


    /** Return the disk image size in bytes. */
    public long getSize()
    {
        try {
            return file.length();
        } catch (IOException e) {
            e.printStackTrace();
            return 0;
        }
    }


    /** Read data from the image. Return 0 on success, EOF or EIO on error. */
    public int read(long offs, byte[] data)
    {
        long size = getSize();
        if (offs < 0 || offs > size || offs + data.length > size) {
            log.warn("out-of-range read from disk image");
            return EOF;
        }
        try {
            file.seek(offs);
            file.readFully(data);
            return 0;
        } catch (IOException e) {
            log.error("error reading from disk image", e);
            return EIO;
        }
    }


    /** Write data to the image. Return 0 on success, EOF or EIO on error. */
    public int write(long offs, byte[] data)
    {
        long size = getSize();
        if (offs < 0 || offs > size || offs + data.length > size) {
            log.warn("out-of-range write to disk image");
            return EOF;
        }
        try {
            file.seek(offs);
            file.write(data);
            return 0;
        } catch (IOException e) {
            log.error("error writing to disk image", e);
            return EIO;
        }
    }


    /** Returns the number of cylinders, or returns -1 for unknown geometry. */
    public int getNumCylinders() {
        return ntrack;
    }


    /** Returns the number of heads, or returns -1 for unknown geometry. */
    public int getNumHeads() {
        return nhead;
    }


    /** Returns the number of sectors per track, or returns -1 for unknown geometry. */
    public int getNumSectors() {
        return nsect;
    }

}

/* end */
