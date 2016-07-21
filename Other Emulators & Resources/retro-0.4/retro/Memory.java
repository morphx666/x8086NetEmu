/*
 *  Memory.java
 *  Joris van Rantwijk
 */

package retro;

/**
 * Emulation of 1MB memory in a flat 20-bit address space.
 *
 * The top 64k (0xf0000 and above) are assumed to be ROM memory;
 * writes into this area are ignored.
 */
public class Memory
{

    static final int MEMSIZE = 0x100000;
    static final int ADDRMASK = 0xfffff;
    static final int MAPBASE =  0xa0000;
    static final int ROMBASE =  0xf0000;
    static final int ROMSIZE =  0x10000;
    static final int PAGESHIFT = 8;

    /** Flat array with memory contents; <b>read only</b>. */
    public byte[] mem;

    /**
     * Bitmap of dirty flags.
     * A dirty bit is raised on a write into the corresponding
     * 256-byte page. The dirty bits are updated only for addresses
     * 0xa0000 and above.
     */
    public int[] dirty;


    /** Construct memory, emptied to zero bytes. */
    public Memory()
    {
        mem = new byte[MEMSIZE];
        dirty = new int[MEMSIZE >> (PAGESHIFT + 5)];
    }


    /** Clear memory to zero bytes; ROM area is not modified. */
    public void reset()
    {
        for (int i = 0; i < ROMBASE; i++)
            mem[i] = 0;
        for (int i = 0; i < dirty.length; i++)
            dirty[i] = 0;
    }


    /** Copy a block of data into memory; dirty flags are not updated. */
    public void loadData(int addr, byte[] data)
    {
        System.arraycopy(data, 0, mem, addr, data.length);
    }


    /** Initialize the contents of the ROM area. */
    public void loadRom(byte[] rom)
    {
        System.arraycopy(rom, 0, mem, ROMBASE, rom.length);
    }


    /** Load a byte from memory. */
    public final int loadByte(int addr)
    {
        return mem[addr & ADDRMASK] & 0xff;
    }


    /** Load a word from memory. */
    public final int loadWord(int addr)
    {
        return (mem[addr & ADDRMASK] & 0xff) |
               ((mem[(addr + 1) & ADDRMASK] & 0xff) << 8);
    }


    /** Store a byte into memory. */
    public final void storeByte(int addr, int v)
    {
        addr &= ADDRMASK;
        if (addr < MAPBASE) {
            // write into normal RAM
            mem[addr] = (byte)v;
        } else if (addr < ROMBASE) {
            // write into mapped area
            mem[addr] = (byte)v;
            dirty[addr >>> (PAGESHIFT + 5)] |=
              1 << ((addr >>> PAGESHIFT) & 31);
        }
    }


    /** Store a word into memory. */
    public final void storeWord(int addr, int v)
    {
        addr &= ADDRMASK;
        if (addr < MAPBASE - 1) {
            // write into normal RAM
            mem[addr] = (byte)v;
            mem[addr+1] = (byte)(v >> 8);
        } else if (((addr + 1) & ((1 << PAGESHIFT) - 1)) != 0) {
            // single page write into mapped area or ROM
            if (addr < ROMBASE) {
                mem[addr] = (byte)v;
                mem[addr+1] = (byte)(v >> 8);
                dirty[addr >>> (PAGESHIFT + 5)] |=
                  1 << ((addr >>> PAGESHIFT) & 31);
            }
        } else {
            // cross-page write into mapped area or ROM
            if (addr < ROMBASE) {
                mem[addr] = (byte)v;
                dirty[addr >>> (PAGESHIFT + 5)] |=
                  1 << ((addr >>> PAGESHIFT) & 31);
            }
            addr = (addr + 1) & ADDRMASK;
            if (addr < ROMBASE) {
                mem[addr] = (byte)(v >> 8);
                dirty[addr >>> (PAGESHIFT + 5)] |=
                  1 << ((addr >>> PAGESHIFT) & 31);
            }
        }
    }

}

/* end */
