/*
 *  DiskImageBuf.java
 *  Joris van Rantwijk
 */

package retro;
import java.io.*;

/**
 * Disk image backed by a memory buffer.
 *
 * The contents of the disk image are read into memory once during
 * initialization. Read and write requests are served from the memory buffer;
 * all writes are lost when the image is closed.
 */
public class DiskImageBuf extends DiskImage
{

    /** Memory buffer. */
    private byte[] image;


    /** Construct a disk image by reading an input stream into memory. */
    public DiskImageBuf(InputStream inf, boolean readonly) throws IOException
    {
        this.readonly = readonly;
        byte[] buf = new byte[368640];
        int n = 0;
        while (true) {
            if (n == buf.length) {
                byte[] t = buf;
                buf = new byte[t.length + 368640];
                System.arraycopy(t, 0, buf, 0, n);
                t = null;
            }
            int k = inf.read(buf, n, buf.length - n);
            if (k <= 0) break;
            n += k;
        }
        if (n == buf.length) {
            image = buf;
        } else {
            image = new byte[n];
            System.arraycopy(buf, 0, image, 0, n);
        }
        matchGeometry();
    }


    /** Release memory buffer. */
    public void close()
    {
        image = null;
    }


    /** Return the disk image size in bytes. */
    public long getSize()
    {
        return image.length;
    }


    /** Read data from the image. */
    public int read(long offs, byte[] data)
    {
        if (offs < 0 || offs > image.length ||
            offs + data.length > image.length)
            return EOF;
        System.arraycopy(image, (int)offs, data, 0, data.length);
        return 0;
    }


    /** Write data to the image. */
    public int write(long offs, byte[] data)
    {
        if (this.readonly)
            return EIO;
        if (offs < 0 || offs > image.length ||
            offs + data.length > image.length)
            return EOF;
        System.arraycopy(data, 0, image, (int)offs, data.length);
        return 0;
    }

}

/* end */
