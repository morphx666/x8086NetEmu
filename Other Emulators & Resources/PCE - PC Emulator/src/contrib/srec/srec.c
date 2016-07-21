/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:   srec.c                                                       *
 * Created:     2005-03-28 by Hampa Hug <hampa@hampa.ch>                     *
 * Copyright:   (C) 2005 Hampa Hug <hampa@hampa.ch>                          *
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
#include <stdlib.h>
#include <string.h>

#include "strarg.h"
#include "strtoint.h"

#include "srec.h"


static const char    *argv0 = NULL;

static unsigned      par_enc = 1;

static unsigned long par_addr = 0;
static unsigned long par_saddr = 0;
static char          *par_name = "";

static unsigned      par_recsize = 16;
static char          *par_fname1 = NULL;
static char          *par_fname2 = NULL;

static int           par_drop = 0;


static unsigned srec_asize[16] = {
	2, 2, 3, 4, 2, 2, 2, 4,
	3, 2, 2, 2, 2, 2, 2, 2
};


static
void prt_version (const char *app)
{
	fputs (app, stdout);

	fputs (
		" (pce) version 2009-02-11\n\n"
		"Copyright (C) 2000-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
void prt_help (void)
{
	fputs (
		"Usage: srec [options]\n"
		"  -i string                Set input file name [stdin]\n"
		"  -o string                Set output file name [stdout]\n"
		"  -c string                Set an srec input file name\n"
		"  -f int string            Set an input file name with address\n"
		"  -e, --encode             Encode from binary to srec [default]\n"
		"  -d, --decode             Decode from srec to binary\n"
		"  -n, --record-size int    Set the record size in bytes [16]\n"
		"  -a, --address int        Set the address for the next input file [0]\n"
		"  -p, --start-address int  Set the start address\n",
		stdout
	);

	fflush (stdout);
}

static
int srec_skip_line (FILE *fp)
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
int srec_get_hex4 (FILE *fp, unsigned char *val)
{
	int c;

	c = fgetc (fp);

	if ((c >= '0') && (c <= '9')) {
		*val = c - '0';
	}
	else if ((c >= 'A') && (c <= 'F')) {
		*val = c - 'A' + 10;
	}
	else if ((c >= 'a') && (c <= 'f')) {
		*val = c - 'a' + 10;
	}
	else {
		return (1);
	}

	return (0);
}

static
int srec_get_hex8 (FILE *fp, unsigned char *val)
{
	unsigned char v1, v2;

	if (srec_get_hex4 (fp, &v1) || srec_get_hex4 (fp, &v2)) {
		return (1);
	}

	*val = ((v1 & 0x0f) << 4) | (v2 & 0x0f);

	return (0);
}

static
void srec_set_hex4 (FILE *fp, unsigned val)
{
	val &= 0x0f;
	val += (val <= 9) ? '0' : ('A' - 10);

	fputc (val, fp);
}

static
void srec_set_hex8 (FILE *fp, unsigned val)
{
	srec_set_hex4 (fp, (val >> 4) & 0x0f);
	srec_set_hex4 (fp, val & 0x0f);
}

static
unsigned char srec_get_cksum (record_t *rec)
{
	unsigned      i;
	unsigned char ck;

	ck = (rec->cnt + rec->asize + 1) & 0xff;
	ck += rec->addr & 0xff;
	ck += (rec->addr >> 8) & 0xff;
	ck += (rec->addr >> 16) & 0xff;
	ck += (rec->addr >> 24) & 0xff;

	for (i = 0; i < rec->cnt; i++) {
		ck += rec->data[i];
	}

	ck = ~ck & 0xff;

	return (ck);
}

static
void srec_init_record (record_t *rec, unsigned type, unsigned cnt, unsigned long addr)
{
	rec->type = type;
	rec->cnt = cnt;
	rec->addr = addr;

	if (type < 16) {
		rec->asize = srec_asize[type];
	}
	else {
		rec->asize = 2;
	}
}

static
int srec_get_record (FILE *fp, record_t *rec)
{
	unsigned      i;
	int           c;
	unsigned char addr;
	unsigned char type;

	while (1) {
		c = fgetc (fp);

		if (c == EOF) {
			return (1);
		}

		if (c == 'S') {
			if (srec_get_hex4 (fp, &type) == 0) {
				break;
			}
		}

		if ((c != 0x0a) && (c != 0x0d)) {
			srec_skip_line (fp);
		}
	}

	rec->type = type;
	rec->asize = srec_asize[type];

	if (srec_get_hex8 (fp, &rec->cnt)) {
		return (1);
	}

	if (rec->cnt < (rec->asize + 1)) {
		return (1);
	}

	rec->cnt -= rec->asize + 1;
	rec->addr = 0;

	for (i = 0; i < rec->asize; i++) {
		if (srec_get_hex8 (fp, &addr)) {
			return (1);
		}
		rec->addr = (rec->addr << 8) | (addr & 0xff);
	}

	for (i = 0; i < rec->cnt; i++) {
		if (srec_get_hex8 (fp, &rec->data[i])) {
			return (1);
		}
	}

	if (srec_get_hex8 (fp, &rec->cksum)) {
		return (1);
	}

	srec_skip_line (fp);

	return (0);
}

static
void srec_set_record (FILE *fp, record_t *rec)
{
	unsigned i;

	rec->cksum = srec_get_cksum (rec);

	fputc ('S', fp);

	srec_set_hex4 (fp, rec->type);
	srec_set_hex8 (fp, rec->cnt + rec->asize + 1);

	for (i = 0; i < rec->asize; i++) {
		srec_set_hex8 (fp,
			(rec->addr >> (8 * (rec->asize - i - 1))) & 0xff
		);
	}

	for (i = 0; i < rec->cnt; i++) {
		srec_set_hex8 (fp, rec->data[i]);
	}

	srec_set_hex8 (fp, rec->cksum);

	fputs ("\n", fp);
}

static
void srec_set_hdr (FILE *fp, const char *name)
{
	unsigned cnt;
	record_t rec;

	cnt = strlen (name);

	if (cnt > 20) {
		cnt = 20;
	}

	srec_init_record (&rec, 0, cnt, 0);

	memcpy (rec.data, name, cnt);

	srec_set_record (fp, &rec);
}

static
void srec_set_end (FILE *fp, unsigned long saddr)
{
	record_t rec;

	if (saddr & 0xff000000) {
		srec_init_record (&rec, 7, 0, saddr);
	}
	else if (saddr & 0xffff0000) {
		srec_init_record (&rec, 8, 0, saddr);
	}
	else {
		srec_init_record (&rec, 9, 0, saddr);
	}

	srec_set_record (fp, &rec);
}

static
FILE *srec_open_next (unsigned long addr)
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
int srec_decode_fp (FILE *inp)
{
	int           r;
	unsigned long addr;
	record_t      rec;
	FILE          *fp;

	r = 1;
	addr = 0;
	fp = NULL;

	while (1) {
		if (srec_get_record (inp, &rec)) {
			break;
		}

		if (rec.cksum != srec_get_cksum (&rec)) {
			fprintf (stderr, "%s: bad checksum in S%X ADDR=%08lX CK=%02X/%02X",
				argv0,
				(unsigned) rec.type & 0xff, (unsigned long) rec.addr,
				(unsigned) rec.cksum, (unsigned) srec_get_cksum (&rec)
			);

			if (par_drop) {
				fprintf (stderr, " (skipping)\n");
				continue;
			}

			fprintf (stderr, " (using anyway)\n");
		}

		if (rec.type == 0) {
			/* header record */
			;
		}
		else if ((rec.type == 1) || (rec.type == 2) || (rec.type == 3)) {
			if ((fp != NULL) && (rec.addr != addr)) {
				fclose (fp);
				fp = NULL;
			}

			if (fp == NULL) {
				fp = srec_open_next (rec.addr);
				if (fp == NULL) {
					break;
				}
			}

			if (fwrite (rec.data, rec.cnt, 1, fp) != 1) {
				break;
			}

			addr = (rec.addr + rec.cnt) & 0xffffffff;
		}
		else if (rec.type == 5) {
			/* record count record */
			;
		}
		else if ((rec.type == 7) || (rec.type == 8) || (rec.type == 9)) {
			/* end record */
			break;
		}
		else {
			fprintf (stderr, "%s: unknown record type S%X at %08lX (ignoring)\n",
				argv0, (unsigned) rec.type, (unsigned long) rec.addr
			);
		}
	}

	if (fp != NULL) {
		fclose (fp);
	}

	return (r);
}

static
int srec_decode (const char *fname)
{
	int  r;
	FILE *fp;

	if (fname == NULL) {
		r = srec_decode_fp (stdin);
	}
	else {
		fp = fopen (fname, "rb");
		if (fp == NULL) {
			return (1);
		}

		r = srec_decode_fp (fp);

		fclose (fp);
	}

	return (r);
}

static
int srec_encode_srec_fp (FILE *out, FILE *inp)
{
	record_t rec;

	while (1) {
		if (srec_get_record (inp, &rec)) {
			break;
		}

		if (rec.type == 0) {
			continue;
		}

		if ((rec.type == 7) || (rec.type == 8) || (rec.type == 9)) {
			continue;
		}

		srec_set_record (out, &rec);
	}

	return (0);
}

static
int srec_encode_srec (FILE *out, const char *inp)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = srec_encode_srec_fp (out, fp);

	fclose (fp);

	return (r);
}

static
int srec_encode_fp (FILE *out, FILE *inp, unsigned long *addr, unsigned recsize)
{
	unsigned long ulba;
	record_t      rec;

	ulba = 0;

	while (1) {
		if (*addr & 0xff000000) {
			rec.type = 3;
			rec.asize = 4;
		}
		else if (*addr & 0xffff0000) {
			rec.type = 2;
			rec.asize = 3;
		}
		else {
			rec.type = 1;
			rec.asize = 2;
		}

		rec.addr = *addr & 0xffffffff;
		rec.cnt = fread (rec.data, 1, recsize, inp);

		if (rec.cnt == 0) {
			break;
		}

		srec_set_record (out, &rec);

		*addr += rec.cnt;
	}

	return (0);
}

static
int srec_encode (FILE *out, const char *inp, unsigned long *addr, unsigned recsize)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = srec_encode_fp (out, fp, addr, recsize);

	fclose (fp);

	return (r);
}

int main (int argc, char *argv[])
{
	int  i;
	char *inp;
	FILE *out;
	int  first;

	argv0 = argv[0];

	if (argc == 2) {
		if (str_isarg2 (argv[1], "?", "help")) {
			prt_help();
			return (EXIT_SUCCESS);
		}
		else if (str_isarg2 (argv[1], NULL, "version")) {
			prt_version ("srec");
			return (EXIT_SUCCESS);
		}
	}

	inp = NULL;
	out = stdout;

	first = 1;

	i = 1;
	while (i < argc) {
		if (str_isarg1 (argv[i], "i")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing input file name\n", argv0);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (srec_encode (out, argv[i], &par_addr, par_recsize)) {
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
				fprintf (stderr, "%s: missing output file name\n", argv0);
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

				srec_set_hdr (out, par_name);

				first = 0;
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
				fprintf (stderr, "%s: missing input file name\n", argv0);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (first) {
					srec_set_hdr (out, par_name);
					first = 0;
				}

				if (srec_encode_srec (out, argv[i])) {
					return (EXIT_FAILURE);
				}
			}
			else {
				fprintf (stderr, "%s: invalid option (-c)\n", argv0);
				return (EXIT_FAILURE);
			}
		}
		else if (str_isarg1 (argv[i], "f")) {
			if ((i + 2) >= argc) {
				fprintf (stderr, "%s: missing address or file name\n", argv0);
				return (EXIT_FAILURE);
			}

			if (str_get_ulng (argv[i + 1], &par_addr)) {
				fprintf (stderr, "%s: bad address (%s)\n", argv0, argv[i + 1]);
				return (EXIT_FAILURE);
			}

			if (par_enc) {
				if (first) {
					srec_set_hdr (out, par_name);
					first = 0;
				}

				if (srec_encode (out, argv[i + 2], &par_addr, par_recsize)) {
					return (EXIT_FAILURE);
				}
			}
			else {
				inp = argv[i];
			}

			i += 2;
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
				fprintf (stderr, "%s: missing record size\n", argv0);
				return (EXIT_FAILURE);
			}

			if (str_get_uint (argv[i], &par_recsize)) {
				fprintf (stderr, "%s: bad record size (%s)\n", argv0, argv[i]);
				return (EXIT_FAILURE);
			}

			if (par_recsize < 1) {
				par_recsize = 1;
			}
		}
		else if (str_isarg2 (argv[i], "a", "address")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing address\n", argv0);
				return (EXIT_FAILURE);
			}

			if (str_get_ulng (argv[i], &par_addr)) {
				fprintf (stderr, "%s: bad address (%s)\n", argv0, argv[i]);
				return (EXIT_FAILURE);
			}
		}
		else if (str_isarg2 (argv[i], "m", "module-name")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing module name\n", argv0);
				return (EXIT_FAILURE);
			}

			par_name = argv[i];
		}
		else if (str_isarg2 (argv[i], "p", "start-address")) {
			i += 1;
			if (i >= argc) {
				fprintf (stderr, "%s: missing start address\n", argv0);
				return (EXIT_FAILURE);
			}

			if (str_get_ulng (argv[i], &par_saddr)) {
				fprintf (stderr, "%s: bad start address (%s)\n", argv0, argv[i]);
				return (EXIT_FAILURE);
			}
		}
		else if (argv[i][0] != '-') {
			if (par_enc) {
				if (first) {
					srec_set_hdr (out, par_name);
					first = 0;
				}

				if (srec_encode (out, argv[i], &par_addr, par_recsize)) {
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
			fprintf (stderr, "%s: unknown option (%s)\n", argv0, argv[i]);
			return (EXIT_FAILURE);
		}

		i += 1;
	}

	if (par_enc) {
		srec_set_end (out, par_saddr);

		if (out != stdout) {
			fclose (out);
		}
	}
	else {
		if (srec_decode (inp)) {
			return (EXIT_FAILURE);
		}
	}

	return (EXIT_SUCCESS);
}
