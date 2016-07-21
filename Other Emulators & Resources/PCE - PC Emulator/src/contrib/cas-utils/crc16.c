/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/crc16.c                                            *
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


#include "crc16.h"


static
void crc_uint64_add (unsigned long *cnt, unsigned long n)
{
	n &= 0xffffffff;

	cnt[0] = (cnt[0] + n) & 0xffffffff;

	if (cnt[0] < n) {
		cnt[1] = (cnt[1] + 1) & 0xffffffff;
	}
}

static
void crc_uint64_shr (unsigned long *cnt, unsigned n)
{
	if (n == 0) {
		return;
	}

	if (n < 32) {
		cnt[0] = ((cnt[0] >> n) | (cnt[1] << (32 - n))) & 0xffffffff;
		cnt[1] = cnt[1] >> n;
	}
	else if (n < 64) {
		cnt[0] = cnt[1] >> (n - 32);
		cnt[1] = 0;
	}
	else {
		cnt[0] = 0;
		cnt[1] = 0;
	}
}

static
unsigned char crc_rev8 (unsigned char x)
{
	unsigned      i;
	unsigned char y;

	y = 0;

	for (i = 0; i < 8; i++) {
		y = (y << 1) | (x & 1);
		x = x >> 1;
	}

	return (y);
}


unsigned crc16_reverse (unsigned x, unsigned size)
{
	unsigned i;
	unsigned y;

	y = 0;

	for (i = 0; i < size; i++) {
		y = (y << 1) | (x & 1);
		x = x >> 1;
	}

	return (y);
}

unsigned crc16_reverse_poly (unsigned x, unsigned size)
{
	if (size == 0) {
		return (0);
	}

	x = (x >> 1) | (1UL << (size - 1));
	x = crc16_reverse (x, size);

	return (x);
}


static
void crc16_create_tab (crc16_t *crc)
{
	unsigned i, j;
	unsigned reg;

	for (i = 0; i < 256; i++) {
		reg = i << 8;

		for (j = 0; j < 8; j++) {
			if (reg & 0x8000) {
				reg = (reg << 1) ^ crc->poly;
			}
			else {
				reg = reg << 1;
			}
		}

		crc->tab[i] = reg & 0xffff;
	}
}

void crc16_init (crc16_t *crc, unsigned poly, unsigned flags)
{
	if (poly == 0) {
		poly = CRC16_CRC16;
	}

	crc->poly = poly & 0xffff;

	crc16_reset (crc, flags);
	crc16_create_tab (crc);
}

void crc16_reset (crc16_t *crc, unsigned flags)
{
	crc->flags = flags;

	crc->reg = (flags & CRC_INV_V0) ? 0xffff : 0;
	crc->out = (flags & CRC_INV_V1) ? 0xffff : 0;

	crc->cnt[0] = 0;
	crc->cnt[1] = 0;
}

void crc16_set_params (crc16_t *crc, unsigned flags, unsigned v0, unsigned v1)
{
	crc->flags = flags;

	crc->reg = v0 & 0xffff;
	crc->out = v1 & 0xffff;

	crc->cnt[0] = 0;
	crc->cnt[1] = 0;
}

void crc16_copy (crc16_t *dst, const crc16_t *src)
{
	*dst = *src;
}

unsigned short crc16_get_crc (const crc16_t *crc)
{
	return (crc->reg);
}

void crc16_calc (crc16_t *crc, const void *buf, unsigned long cnt)
{
	unsigned             i;
	const unsigned char  *src = buf;
	const unsigned short *tab;

	crc_uint64_add (crc->cnt, cnt);

	tab = crc->tab;

	if (crc->flags & CRC_REVERSE_INP) {
		while (cnt > 0) {
			i = ((crc->reg >> 8) ^ crc_rev8 (*src)) & 0xff;

			crc->reg = (crc->reg << 8) ^ tab[i];

			src += 1;
			cnt -= 1;
		}
	}
	else {
		while (cnt > 0) {
			i = ((crc->reg >> 8) ^ *src) & 0xff;

			crc->reg = (crc->reg << 8) ^ tab[i];

			src += 1;
			cnt -= 1;
		}
	}

	crc->reg &= 0xffff;
}

void crc16_done (crc16_t *crc)
{
	unsigned long cnt[2];
	unsigned char buf;

	if (crc->flags & CRC_POSIX) {
		cnt[0] = crc->cnt[0];
		cnt[1] = crc->cnt[1];

		while ((cnt[0] != 0) || (cnt[1] != 0)) {
			buf = cnt[0] & 0xff;
			crc16_calc (crc, &buf, 1);
			crc_uint64_shr (cnt, 8);
		}

		crc->reg ^= 0xffff;
	}

	crc->reg = (crc->reg ^ crc->out) & 0xffff;

	if (crc->flags & CRC_REVERSE_OUT) {
		crc->reg = crc16_reverse (crc->reg, 16);
	}
}

unsigned short crc16_crc (const void *buf, unsigned long cnt, unsigned poly, unsigned flags)
{
	crc16_t crc;

	crc16_init (&crc, poly, flags);
	crc16_calc (&crc, buf, cnt);
	crc16_done (&crc);

	return (crc16_get_crc (&crc));
}
