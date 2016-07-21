/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/casdec.c                                           *
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
#include "util.h"


static
void print_help (void)
{
	fputs (
		"casdec: decode cassette files\n"
		"\n"
		"usage: casdec [options] [file...]\n"
		"  -i, --input string  Set an input file [stdin]\n"
		"  -l, --list          List the files, don't extract [default]\n"
		"  -x, --extract       Extract the files [no]\n",
		stdout
	);

	fflush (stdout);
}

static
void print_version (void)
{
	fputs (
		"casdec version 0.0.1\n\n"
		"Copyright (C) 2008-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
int cas_read_text_fp (cas_file_t *cf, FILE *fp, unsigned *size)
{
	unsigned      i, n;
	int           ok;
	unsigned long fpos;
	unsigned char buf[256];

	*size = 0;

	while (1) {
		if (cas_read_leader (cf, &fpos)) {
			return (1);
		}

		if (cas_read_block (cf, buf, 0, &ok)) {
			return (1);
		}

		n = buf[0];

		if (n == 0) {
			n = 256;
		}

		*size += n - 1;

		if (fp != NULL) {
			for (i = 1; i < n; i++) {
				fputc (buf[i], fp);
			}
		}

		if (n < 256) {
			break;
		}
	}

	return (0);
}

static
int cas_read_text (cas_file_t *cf, const char *fname, unsigned *size)
{
	int  r;
	FILE *fp;

	fp = fopen (fname, "wb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_read_text_fp (cf, fp, size);

	fclose (fp);

	return (r);
}

static
int cas_read_binary_fp (cas_file_t *cf, FILE *fp, unsigned size)
{
	unsigned      i, n;
	int           ok;
	unsigned long fpos;
	unsigned char buf[256];

	if (cas_read_leader (cf, &fpos)) {
		return (1);
	}

	while (size > 0) {
		if (cas_read_block (cf, buf, 0, &ok)) {
			return (1);
		}

		n = (size < 256) ? size : 256;

		if (fp != NULL) {
			for (i = 0; i < n; i++) {
				fputc (buf[i], fp);
			}
		}

		size -= n;
	}

	return (0);
}

static
int cas_read_binary (cas_file_t *cf, const char *fname, unsigned size)
{
	int  r;
	FILE *fp;

	fp = fopen (fname, "wb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_read_binary_fp (cf, fp, size);

	fclose (fp);

	return (r);
}

static
int cas_read_blocks_fp (cas_file_t *cf, FILE *fp, unsigned char *buf, unsigned *size)
{
	int i;
	int ok;

	*size = 0;

	while (1) {
		if (fp != NULL) {
			for (i = 0; i < 256; i++) {
				fputc (buf[i], fp);
			}
		}

		*size += 256;

		if (cas_read_block (cf, buf, 0, &ok)) {
			return (0);
		}
	}

	return (1);
}

static
int cas_read_blocks (cas_file_t *cf, const char *fname, unsigned char *buf, unsigned *size)
{
	int  r;
	FILE *fp;

	if (fname == NULL) {
		return (cas_read_blocks_fp (cf, NULL, buf, size));
	}

	fp = fopen (fname, "wb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_read_blocks_fp (cf, fp, buf, size);

	fclose (fp);

	return (r);
}

static
int cas_read_file (cas_file_t *cf, int list, unsigned *idx)
{
	unsigned      i;
	int           r, ok;
	int           text, binary;
	unsigned      seg, ofs, size;
	unsigned long fpos;
	unsigned char buf[256];
	char          mode;
	char          name[64];
	char          fname[64];

	if (cas_read_leader (cf, &fpos)) {
		return (1);
	}

	if (cas_read_block (cf, buf, 0, &ok)) {
		return (0);
	}

	if (buf[0] != 0xa5) {
		sprintf (fname, "%03u_BLOCKS", *idx);
		strcpy (name, "BLOCKS");

		r = cas_read_blocks (cf, list ? NULL : fname, buf, &size);

		fprintf (list ? stdout : stderr,
			"%03u  %-8s  %c  %04X:%04X  %6lu  %5u  %s\n",
			*idx, name, '-', 0, 0, fpos, size,
			r ? "NOT OK" : "OK"
		);

		*idx += 1;

		return (0);
	}

	for (i = 0; i < 8; i++) {
		name[i] = buf[i + 1];
	}

	i = 8;
	while ((i > 0) && (name[i - 1] == 0x20)) {
		i -= 1;
	}

	name[i] = 0;

	fname[0] = '0' + (*idx / 100) % 10;
	fname[1] = '0' + (*idx / 10) % 10;
	fname[2] = '0' + *idx % 10;
	fname[3] = '_';

	for (i = 0; i < 8; i++) {
		fname[i + 4] = buf[i + 1];

		if (fname[i + 4] == 0x20) {
			fname[i + 4] = '_';
		}
	}

	fname[12] = '_';
	fname[14] = 0;

	text = 0;
	binary = 0;

	switch (buf[9]) {
	case 0x00:
		text = 1;
		mode = 'D';
		break;

	case 0x01:
		binary = 1;
		mode = 'M';
		break;

	case 0x40:
		text = 1;
		mode = 'A';
		break;

	case 0x80:
		binary = 1;
		mode = 'B';
		break;

	case 0xA0:
		binary = 1;
		mode = 'P';
		break;

	default:
		mode = 'X';
		break;
	}

	fname[13] = mode;

	size = ((unsigned) buf[11] << 8) | buf[10];
	seg = ((unsigned) buf[13] << 8) | buf[12];
	ofs = ((unsigned) buf[15] << 8) | buf[14];

	if (list) {
		if (text) {
			r = cas_read_text_fp (cf, NULL, &size);
		}
		else if (binary) {
			r = cas_read_binary_fp (cf, NULL, size);
		}
		else {
			r = 1;
		}
	}
	else {
		if (text) {
			r = cas_read_text (cf, fname, &size);
		}
		else if (binary) {
			r = cas_read_binary (cf, fname, size);
		}
		else {
			r = 1;
		}
	}

	fprintf (list ? stdout : stderr,
		"%03u  %-8s  %c  %04X:%04X  %6lu  %5u  %s\n",
		*idx, name, mode, seg, ofs, fpos, size,
		r ? "NOT OK" : "OK"
	);

	*idx += 1;

	return (r);
}

static
int cas_decode_fp (FILE *inp, FILE *out, int list, unsigned *idx)
{
	cas_file_t cf;

	cas_open_fp (&cf, inp);

	while (cas_read_file (&cf, list, idx) == 0) {
		;
	}

	return (0);
}

static
int cas_decode (const char *inp, FILE *out, int list, unsigned *idx)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_decode_fp (fp, out, list, idx);

	fclose (fp);

	return (r);
}

int main (int argc, char **argv)
{
	int      i;
	int      list;
	int      done;
	unsigned idx;

	if (argc == 2) {
		if (str_isarg (argv[1], "h", "help")) {
			print_help();
			return (0);
		}
		else if (str_isarg (argv[1], "V", "version")) {
			print_version();
		}
	}

	done = 0;
	list = 1;

	idx = 1;

	i = 1;
	while (i < argc) {
		if (str_isarg (argv[i], "i", "input")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			cas_decode (argv[i], stdout, list, &idx);
			done = 1;
		}
		else if (str_isarg (argv[i], "l", "list")) {
			list = 1;
		}
		else if (str_isarg (argv[i], "x", "extract")) {
			list = 0;
		}
		else if (argv[i][0] == '-') {
			fprintf (stderr, "%s: unknown option (%s)\n",
				argv[0], argv[i]
			);
		}
		else {
			cas_decode (argv[i], stdout, list, &idx);
			done = 1;
		}

		i += 1;
	}

	if (done == 0) {
		cas_decode_fp (stdin, stdout, list, &idx);
	}

	return (0);
}
