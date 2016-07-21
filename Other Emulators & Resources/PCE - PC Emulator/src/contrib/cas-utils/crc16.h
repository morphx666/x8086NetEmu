/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/crc16.h                                            *
 * Created:     2000-09-26 by Hampa Hug <hampa@hampa.ch>                     *
 * Copyright:   (C) 2000-2009 Hampa Hug <hampa@hampa.ch>                     *
 *****************************************************************************/

/*****************************************************************************
 * This program is free software. You can redistribute it and / or modify it *
 * under the terms of the GNU General Public License version 2 as  published *
 * by the Free Software Foundation.                                          *
 *                                                                           *
 * This program is distributed in the hope  that  it  will  be  useful,  but *
 * WITHOUT  ANY   WARRANTY,   without   even   the   implied   warranty   of *
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU  General *
 * Public License for more details.                                          *
 *****************************************************************************/


#ifndef CRC16_H
#define CRC16_H 1


#define CRC16_CRC16  0x8005
#define CRC16_CCITT  0x1021

#ifndef CRC_FLAGS_DEF
#define CRC_FLAGS_DEF   1
#define CRC_POSIX       1
#define CRC_REVERSE_INP 2
#define CRC_REVERSE_OUT 4
#define CRC_INV_V0      8
#define CRC_INV_V1      16
#endif


/*!***************************************************************************
 * @short The CRC-16 context
 *****************************************************************************/
typedef struct {
	unsigned       flags;
	unsigned short poly;
	unsigned short reg;
	unsigned short out;
	unsigned long  cnt[2];
	unsigned short tab[256];
} crc16_t;


/*!***************************************************************************
 * @short Reverse bit order
 * @param x    The value
 * @param size The number of bits to reverse
 *
 * Reverses the bit order of the low size bits of x.
 *****************************************************************************/
unsigned crc16_reverse (unsigned x, unsigned size);

/*!***************************************************************************
 * @short Calculate a reciprocal polynomial
 * @param x    The polynomial
 * @param size The size of the polynomial - 1
 *****************************************************************************/
unsigned crc16_reverse_poly (unsigned x, unsigned size);


/*!***************************************************************************
 * @short Initialize a CRC-16 context
 * @param crc   The CRC-16 context
 * @param poly  The generator polynomial
 * @param flags The CRC flags
 *
 * Initializes the context and creates the crc table.
 *****************************************************************************/
void crc16_init (crc16_t *crc, unsigned poly, unsigned flags);

/*!***************************************************************************
 * @short Reset a CRC-16 context
 *****************************************************************************/
void crc16_reset (crc16_t *crc, unsigned flags);

void crc16_set_params (crc16_t *crc, unsigned flags, unsigned v0, unsigned v1);

/*!***************************************************************************
 * @short Copy a CRC-16 context
 *****************************************************************************/
void crc16_copy (crc16_t *dst, const crc16_t *src);

/*!***************************************************************************
 * @short  Get the CRC register
 * @return The CRC16 checksum
 *
 * crc16_done() must be called before this.
 *****************************************************************************/
unsigned short crc16_get_crc (const crc16_t *crc);

/*!***************************************************************************
 * @short Calculate a CRC-16 checksum for a byte buffer.
 * @param crc The CRC-16 context
 * @param buf The buffer
 * @param cnt The buffer size
 *
 * This function can be called multiple times.
 *****************************************************************************/
void crc16_calc (crc16_t *crc, const void *buf, unsigned long cnt);

/*!***************************************************************************
 * @short Finish calculating a CRC-16 checksum
 * @param crc The CRC-16 context
 *
 * This function should be called after the last call to crc16_calc() and
 * before crc16_get_crc().
 *****************************************************************************/
void crc16_done (crc16_t *crc);

unsigned short crc16_crc (const void *buf, unsigned long cnt, unsigned poly, unsigned flags);


#endif
