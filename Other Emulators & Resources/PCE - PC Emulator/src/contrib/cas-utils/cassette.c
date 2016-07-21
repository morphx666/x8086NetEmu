/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/cassette.c                                         *
 * Created:     2009-02-18 by Hampa Hug <hampa@hampa.ch>                     *
 * Copyright:   (C) 2008-2009 Hampa Hug <hampa@hampa.ch>                     *
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


#include <stdio.h>
#include <string.h>

#include "crc16.h"
#include "cassette.h"


int cas_open_fp (cas_file_t *cf, FILE *fp)
{
	cf->fp = fp;
	cf->buf = 0;
	cf->cnt = 0;

	cf->ofs = 0;

	return (0);
}

int cas_read_bit (cas_file_t *cf, int *val)
{
	int c;

	if (cf->cnt == 0) {
		c = fgetc (cf->fp);
		if (c == EOF) {
			return (1);
		}

		cf->ofs += 1;

		cf->buf = c & 0xff;
		cf->cnt = 8;
	}

	*val = ((cf->buf & 0x80) != 0);

	cf->buf = (cf->buf << 1) & 0xff;
	cf->cnt -= 1;

	return (0);
}

int cas_read_byte (cas_file_t *cf, unsigned char *val)
{
	unsigned i;
	int      bit;

	*val = 0;

	for (i = 0; i < 8; i++) {
		if (cas_read_bit (cf, &bit)) {
			return (1);
		}

		*val = (*val << 1) | (bit != 0);
	}

	return (0);
}

int cas_read_leader (cas_file_t *cf, unsigned long *start)
{
	unsigned      bitcnt;
	int           bit;
	unsigned char byte;

	*start = cf->ofs;

	bitcnt = 0;

	while (1) {
		if (cas_read_bit (cf, &bit)) {
			return (1);
		}

		if (bit) {
			bitcnt += 1;

			if (bitcnt == 0) {
				bitcnt -= 1;
			}
		}
		else {
			if (bitcnt < (64 * 8)) {
				bitcnt = 0;
			}
			else {
				if (cas_read_byte (cf, &byte)) {
					return (1);
				}

				if (byte == 0x16) {
					return (0);
				}

				bitcnt = 0;
			}

			*start = cf->ofs;
		}
	}

	return (1);
}

int cas_read_block (cas_file_t *cf, void *buf, unsigned maxff, int *ok)
{
	unsigned      i, n;
	unsigned      crc1, crc2;
	unsigned char val[2];
	unsigned char *blk;

	blk = buf;

	for (i = 0; i < 256; i++) {
		if (cas_read_byte (cf, blk + i)) {
			return (1);
		}
	}

	if (cas_read_byte (cf, &val[0])) {
		return (1);
	}

	if (cas_read_byte (cf, &val[1])) {
		return (1);
	}

	crc1 = (val[0] << 8) | val[1];
	crc2 = crc16_crc (blk, 256, 0x1021, CRC_INV_V0 | CRC_INV_V1);

	if (crc1 != crc2) {
		n = 0;
		for (i = 0; i < 256; i++) {
			if (blk[i] == 0xff) {
				n += 1;
			}
		}
		if (n < maxff) {
			*ok = 0;
			return (0);
		}
		if (fseek (cf->fp, -258, SEEK_CUR) == 0) {
			cf->ofs -= 258;
		}
		return (1);
	}

	*ok = 1;

	return (0);
}


int cas_flush_out (cas_file_t *cf)
{
	if (cf->cnt > 0) {
		fputc ((cf->buf << (8 - cf->cnt)) & 0xff, cf->fp);
		cf->ofs += 1;
	}

	cf->buf = 0;
	cf->cnt = 0;

	return (0);
}

int cas_write_bit (cas_file_t *cf, int val)
{
	cf->buf = (cf->buf << 1) | (val != 0);
	cf->cnt += 1;

	if (cf->cnt >= 8) {
		fputc (cf->buf & 0xff, cf->fp);

		cf->ofs += 1;

		cf->buf = 0;
		cf->cnt = 0;
	}

	return (0);
}

int cas_write_byte (cas_file_t *cf, unsigned char val)
{
	unsigned i;

	for (i = 0; i < 8; i++) {
		if (cas_write_bit (cf, val & 0x80)) {
			return (1);
		}

		val <<= 1;
	}

	return (0);
}

int cas_write_leader (cas_file_t *cf, unsigned cnt)
{
	unsigned i;

	for (i = 2; i < cnt; i++) {
		if (cas_write_byte (cf, 0xff)) {
			return (1);
		}
	}

	if (cas_write_byte (cf, 0xfe)) {
		return (1);
	}

	if (cas_write_byte (cf, 0x16)) {
		return (1);
	}

	return (0);
}

int cas_write_trailer (cas_file_t *cf)
{
	unsigned i;

	for (i = 0; i < 32; i++) {
		if (cas_write_bit (cf, 1)) {
			return (1);
		}
	}

	return (0);
}

int cas_write_block (cas_file_t *cf, const void *buf, unsigned cnt)
{
	unsigned      i;
	unsigned      crc;
	unsigned char blk[258];

	if (cnt > 256) {
		cnt = 256;
	}

	memcpy (blk, buf, cnt);

	for (i = cnt; i < 256; i++) {
		blk[i] = 0;
	}

	crc = crc16_crc (blk, 256, 0x1021, CRC_INV_V0 | CRC_INV_V1);

	blk[256] = (crc >> 8) & 0xff;
	blk[257] = crc & 0xff;

	for (i = 0; i < 258; i++) {
		if (cas_write_byte (cf, blk[i])) {
			return (1);
		}
	}

	return (0);
}

int cas_write_header (cas_file_t *cf, const char *name,
	unsigned type, unsigned size, unsigned seg, unsigned ofs)
{
	unsigned      i, n;
	unsigned char blk[256];

	memset (blk, 0, 256);

	if (name != NULL) {
		n = strlen (name);
	}
	else {
		n = 0;
	}

	blk[0] = 0xa5;

	for (i = 0; i < 8; i++) {
		blk[i + 1] = (i < n) ? name[i] : 0x20;
	}

	blk[9] = type & 0xff;

	blk[10] = size & 0xff;
	blk[11] = (size >> 8) & 0xff;

	blk[12] = seg & 0xff;
	blk[13] = (seg >> 8) & 0xff;

	blk[14] = ofs & 0xff;
	blk[15] = (ofs >> 8) & 0xff;

	blk[16] = 0;

	return (cas_write_block (cf, blk, 256));
}
