/*
 *  Misc.java
 *  Joris van Rantwijk
 */

package retro;

public abstract class Misc
{

	public static String byteToHex(int v)
	{
		return Integer.toHexString((v & 0xff) | 0x100).substring(1);
	}

	public static final String wordToHex(int v)
	{
		return Integer.toHexString((v & 0xffff) | 0x10000).substring(1);
	}

}

/* end */
