/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:     contrib/ihex/ihex.c                                        *
 * Created:       2004-06-09 by Hampa Hug <hampa@hampa.ch>                   *
 * Copyright:     (C) 2004-2006 Hampa Hug <hampa@hampa.ch>                   *
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

/* $Id$ */


#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "strarg.h"
#include "strtoint.h"
#include "ihex.h"


static unsigned      par_enc = 1;
static unsigned long par_addr = 0;
static unsigned      par_recsize = 16;
static char          *par_fname1 = NULL;
static char          *par_fname2 = NULL;
static int           par_drop = 0;


static
void prt_help (void)
{
	fputs (
		"Usage: ihex [options]\n"
		"  -i string              Set input file name [stdin]\n"
		"  -o string              Set output file name [stdout]\n"
		"  -c string              Set an ihex input file name\n"
		"  -e, --encode           Encode from binary to intel hex [default]\n"
		"  -d, --decode           Decode from intel hex to binary\n"
		"  -n, --record-size int  Set the record size [16]\n"
		"  -a, --address int      Set the base address\n",
		stdout
	);

	fflush (stdout);
}

void prt_version (const char *app)
{
	fputs (app, stdout);

	fputs (
		" (pce) version 2004-08-02\n\n"
		"Copyright (C) 2000-2004 by Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
int ihex_skip_line (FILE *fp)
{
	int c;

	while (1) {
		c = fgetc (fp);

		if (c == EOF) {
			return (1);
		}

		if ((c == 0x0a) || (c == 0x0d)) {
			return (0);
		}
	}

	return (0);
}

static
int ihex_get_hex8 (FILE *fp, unsigned char *val)
{
	unsigned i;
	int      c;

	*val = 0;

	for (i = 0; i < 2; i++) {
		c = fgetc (fp);

		if ((c >= '0') && (c <= '9')) {
			*val = (*val << 4) | (c - '0');
		}
		else if ((c >= 'A') && (c <= 'F')) {
			*val = (*val << 4) | (c - 'A' + 10);
		}
		else if ((c >= 'a') && (c <= 'f')) {
			*val = (*val << 4) | (c - 'a' + 10);
		}
		else {
			return (1);
		}
	}

	return (0);
}

static
unsigned char ihex_get_cksum (record_t *rec)
{
	unsigned      i;
	unsigned char ck;

	ck = rec->cnt & 0xff;
	ck += rec->addr & 0xff;
	ck += (rec->addr >> 8) & 0xff;
	ck += rec->type & 0xff;

	for (i = 0; i < rec->cnt; i++) {
		ck += rec->data[i];
	}

	ck = (~ck + 1) & 0xff;

	return (ck);
}

static
int ihex_get_record (FILE *fp, record_t *rec)
{
	unsigned      i;
	int           c;
	unsigned char a1, a2;

	while (1) {
		c = fgetc (fp);

		if (c == EOF) {
			return (1);
		}

		if (c == ':') {
			break;
		}

		if ((c != 0x0a) && (c != 0x0d)) {
			ihex_skip_line (fp);
		}
	}

	if (ihex_get_hex8 (fp, &rec->cnt)) {
		return (1);
	}

	if (ihex_get_hex8 (fp, &a1) || ihex_get_hex8 (fp, &a2)) {
		return (1);
	}

	rec->addr = (a1 << 8) | a2;

	if (ihex_get_hex8 (fp, &rec->type)) {
		return (1);
	}

	for (i = 0; i < rec->cnt; i++) {
		if (ihex_get_hex8 (fp, &rec->data[i])) {
			return (1);
		}
	}

	if (ihex_get_hex8 (fp, &rec->cksum)) {
		return (1);
	}

	ihex_skip_line (fp);

	return (0);
}

static
void ihex_set_hex8 (FILE *fp, unsigned char c)
{
	int tmp;

	tmp = (c >> 4) & 0x0f;
	tmp += (tmp <= 9) ? '0' : ('A' - 10);
	fputc (tmp, fp);

	tmp = c & 0x0f;
	tmp += (tmp <= 9) ? '0' : ('A' - 10);
	fputc (tmp, fp);
}

static
void ihex_set_record (FILE *fp, record_t *rec)
{
	unsigned i;

	rec->cksum = ihex_get_cksum (rec);

	fprintf (fp, ":%02X%04X%02X",
		(unsigned) rec->cnt, (unsigned) rec->addr & 0xffffU, (unsigned) rec->type
	);

	for (i = 0; i < rec->cnt; i++) {
		ihex_set_hex8 (fp, rec->data[i]);
	}

	ihex_set_hex8 (fp, rec->cksum);

	fputs ("\n", fp);
}

static
void ihex_set_ulba (FILE *fp, unsigned long addr)
{
	record_t rec;

	rec.type = 0x04;
	rec.cnt = 2;
	rec.addr = 0;
	rec.data[0] = (addr >> 24) & 0xff;
	rec.data[1] = (addr >> 16) & 0xff;

	ihex_set_record (fp, &rec);
}

static
void ihex_set_end (FILE *fp)
{
	record_t rec;

	rec.type = 0x01;
	rec.cnt = 0;
	rec.addr = 0;

	ihex_set_record (fp, &rec);
}

static
FILE *ihex_open_next (unsigned long addr)
{
	unsigned i;
	char     dig;
	FILE     *fp;

	if (par_fname1 == NULL) {
		par_fname1 = "########.bin";
	}

	if (par_fname2 == NULL) {
		par_fname2 = malloc (strlen (par_fname1) + 1);
	}

	i = 0;
	while (par_fname1[i] != 0) {
		par_fname2[i] = par_fname1[i];
		i += 1;
	}

	par_fname2[i] = 0;

	while (i > 0) {
		i -= 1;

		if (par_fname2[i] == '#') {
			dig = addr & 0x0f;
			dig = (dig <= 9) ? ('0' + dig) : ('A' + dig - 10);
			par_fname2[i] = dig;
			addr = (addr >> 4);
		}
	}

	fp = fopen (par_fname2, "wb");

	return (fp);
}

static
int ihex_decode_fp (FILE *inp)
{
	int           r;
	unsigned long addr, raddr, ulba;
	record_t      rec;
	FILE          *fp;

	r = 1;
	addr = 0;
	ulba = 0;
	fp = NULL;

	while (1) {
		if (ihex_get_record (inp, &rec)) {
			break;
		}

		if (rec.cksum != ihex_get_cksum (&rec)) {
			fprintf (stderr, "ihex: bad checksum T=%02X L=%04X CK=%02X/%02X",
				(unsigned) rec.type & 0xff, (unsigned) rec.addr & 0xffffU,
				(unsigned) rec.cksum, (unsigned) ihex_get_cksum (&rec)
			);

			if (par_drop) {
				fprintf (stderr, " (skipping)\n");
				continue;
			}

			fprintf (stderr, " (using anyway)\n");
		}

		if (rec.type == IHEX_REC_EOFR) {
			r = 0;
			break;
		}
		else if (rec.type == IHEX_REC_ELAR) {
			if (rec.cnt == 2) {
				ulba = (rec.data[0] << 8) | rec.data[1];
				ulba = ulba << 16;
			}
			else {
				fprintf (stderr, "ihex: bad extended linear address record (ignoring)\n");
			}
		}
		else if (rec.type == IHEX_REC_ESAR) {
			if (rec.cnt == 2) {
				ulba = (rec.data[0] << 8) | rec.data[1];
				ulba = ulba << 4;
			}
			else {
				fprintf (stderr, "ihex: bad extended segment address record (ignoring)\n");
			}
		}
		else if (rec.type == IHEX_REC_DATA) {
			raddr = ulba + (rec.addr & 0xffff);

			if ((fp != NULL) && (raddr != addr)) {
				fclose (fp);
				fp = NULL;
			}

			if (fp == NULL) {
				fp = ihex_open_next (raddr);
				if (fp == NULL) {
					break;
				}
			}

			if (fwrite (rec.data, 1, rec.cnt, fp) != rec.cnt) {
				break;
			}

			addr = raddr + rec.cnt;
		}
		else {
			fprintf (stderr, "ihex: unknown record type %02X at %04X (ignoring)\n",
				(unsigned) rec.type, (unsigned) rec.addr
			);
		}
	}

	if (fp != NULL) {
		fclose (fp);
	}

	return (r);
}

static
int ihex_decode (const char *fname)
{
	int  r;
	FILE *fp;

	if (fname == NULL) {
		r = ihex_decode_fp (stdin);
	}
	else {
		fp = fopen (fname, "rb");
		if (fp == NULL) {
			return (1);
		}

		r = ihex_decode_fp (fp);

		fclose (fp);
	}

	return (r);
}

static
int ihex_encode_ihex_fp (FILE *out, FILE *inp)
{
	record_t rec;

	while (1) {
		if (ihex_get_record (inp, &rec)) {
			break;
		}

		if (rec.type == IHEX_REC_EOFR) {
			break;
		}

		ihex_set_record (out, &rec);
	}

	return (0);
}

static
int ihex_encode_ihex (FILE *out, const char *inp)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = ihex_encode_ihex_fp (out, fp);

	fclose (fp);

	return (r);
}

static
int ihex_encode_fp (FILE *out, FILE *inp, unsigned long *addr, unsigned recsize)
{
	unsigned long ulba;
	record_t      rec;

	ulba = 0;

	while (1) {
		rec.type = 0x00;
		rec.addr = *addr & 0xffffU;
		rec.cnt = fread (rec.data, 1, recsize, inp);

		if (rec.cnt == 0) {
			break;
		}

		if ((ulba & 0xffff0000UL) != (*addr & 0xffff0000UL)) {
			ihex_set_ulba (out, *addr);
			ulba = *addr & 0xffff0000UL;
		}

		ihex_set_record (out, &rec);

		*addr += rec.cnt;
	}

	return (0);
}

static
int ihex_encode (FILE *out, const char *inp, unsigned long *addr, unsigned recsize)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = ihex_encode_fp (out, fp, addr, recsize);

	fclose (fp);

	return (r);
}

int main (int argc, char *argv[])
{
	int  i;
	char *inp;
	FILE *out;

	if (argc == 2) {
		if (str_isarg2 (argv[1], "?", "help")) {
			prt_help();
			return (EXIT_SUCCESS);
		}
		else if (str_isarg2 (argv[1], NULL, "version")) {
			prt_version ("ihex");
			return (EXIT_SUCCESS);
		}
	}

	inp = NULL;
	out = stdout;

	i = 1;
	while (i < argc) {
		if (str_isarg1 (argv[i], "i")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing input file name\n", argv[0]);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (ihex_encode (out, argv[i], &par_addr, par_recsize)) {
					return (EXIT_FAILURE);
				}
			}
			else {
				inp = argv[i];
			}
		}
		else if (str_isarg1 (argv[i], "o")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing output file name\n", argv[0]);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (out != stdout) {
					fclose (out);
				}

				out = fopen (argv[i], "w");
				if (out == NULL) {
					return (EXIT_FAILURE);
				}
			}
			else {
				par_fname1 = argv[i];
				free (par_fname2);
				par_fname2 = NULL;
			}
		}
		else if (str_isarg1 (argv[i], "c")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing input file name\n", argv[0]);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (ihex_encode_ihex (out, argv[i])) {
					return (EXIT_FAILURE);
				}
			}
			else {
				fprintf (stderr, "%s: invalid option (-c)\n", argv[0]);
				return (EXIT_FAILURE);
			}
		}
		else if (str_isarg2 (argv[i], "e", "encode")) {
			par_enc = 1;
		}
		else if (str_isarg2 (argv[i], "d", "decode")) {
			par_enc = 0;
		}
		else if (str_isarg2 (argv[i], "n", "record-size")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing record size\n", argv[0]);
				return (EXIT_FAILURE);
			}

			if (str_get_uint (argv[i], &par_recsize)) {
				fprintf (stderr, "%s: bad record size (%s)\n", argv[0], argv[i]);
				return (EXIT_FAILURE);
			}

			if (par_recsize < 1) {
				par_recsize = 1;
			}
		}
		else if (str_isarg2 (argv[i], "a", "address")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing address\n", argv[0]);
				return (EXIT_FAILURE);
			}

			if (str_get_ulng (argv[i], &par_addr)) {
				fprintf (stderr, "%s: bad address (%s)\n", argv[0], argv[i]);
				return (EXIT_FAILURE);
			}
		}
		else if (argv[i][0] != '-') {
			if (par_enc) {
				if (ihex_encode (out, argv[i], &par_addr, par_recsize)) {
					return (EXIT_FAILURE);
				}
			}
			else {
				if (inp == NULL) {
					inp = argv[i];
				}
				else if (par_fname1 == NULL) {
					par_fname1 = argv[i];
					free (par_fname2);
					par_fname2 = NULL;
				}
				else {
					return (EXIT_FAILURE);
				}
			}
		}
		else {
			fprintf (stderr, "%s: unknown option (%s)\n", argv[0], argv[i]);
			return (EXIT_FAILURE);
		}

		i += 1;
	}

	if (par_enc) {
		ihex_set_end (out);

		if (out != stdout) {
			fclose (out);
		}
	}
	else {
		if (ihex_decode (inp)) {
			return (EXIT_FAILURE);
		}
	}

	return (EXIT_SUCCESS);
}
