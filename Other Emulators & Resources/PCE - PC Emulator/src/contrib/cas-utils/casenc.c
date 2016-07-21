/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/casenc.c                                           *
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
#include <stdlib.h>
#include <string.h>

#include "crc16.h"
#include "cassette.h"
#include "util.h"


static unsigned par_seg = 0x0060;
static unsigned par_ofs = 0x081e;

static unsigned par_motor_delay = 64;

static int      par_verbose = 0;


static
void print_help (void)
{
	fputs (
		"casenc: encode cassette files to stdout\n"
		"\n"
		"usage: casenc [options] [file...]\n"
		"  -d, --motor-delay int              Set the motor delay in bytes [64]\n"
		"  -f, --file filename tapename type  Add a file\n"
		"  -S, --segment int                  Set the segment for following files [0x0060]\n"
		"  -O, --offset int                   Set the offset for following files [0x081e]\n"
		"  -v, --verbose                      Verbose operation [no]\n"
		"types are:\n"
		"  a  An ASCII BASIC program (save \"file\",a)\n"
		"  b  A tokenized BASIC program (save \"file\")\n"
		"  d  A sequential file (open \"file\" for output)\n"
		"  m  A binary file (bsave \"file\")\n"
		"  p  A protected BASIC program (save \"file\",p)\n"
		"  -  A sequence of raw blocks\n",
		stdout
	);

	fflush (stdout);
}

static
void print_version (void)
{
	fputs (
		"casenc version 0.0.1\n\n"
		"Copyright (C) 2008-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
int cas_write_text_fp (cas_file_t *cf, FILE *inp, const char *name, unsigned type)
{
	int           c;
	int           cr;
	unsigned      n;
	unsigned char blk[256];

	if (cas_write_leader (cf, 256 + par_motor_delay)) {
		return (1);
	}

	if (cas_write_header (cf, name, type, 0, 0, 0)) {
		return (1);
	}

	if (cas_write_trailer (cf)) {
		return (1);
	}

	n = 0;
	cr = 0;

	while (1) {
		c = fgetc (inp);

		if (c == EOF) {
			break;
		}

		if (c == 0x0d) {
			cr = 1;
		}
		else if (c == 0x0a) {
			if (cr) {
				continue;
			}
			else {
				c = 0x0d;
			}

			cr = 0;
		}
		else {

			cr = 0;
		}

		n += 1;

		blk[n] = c & 0xff;

		if (n >= 255) {
			if (cas_write_leader (cf, 256 + par_motor_delay)) {
				return (1);
			}

			blk[0] = 0;

			if (cas_write_block (cf, blk, 256)) {
				return (1);
			}

			if (cas_write_trailer (cf)) {
				return (1);
			}

			n = 0;
		}
	}

	if (cas_write_leader (cf, 256 + par_motor_delay)) {
		return (1);
	}

	blk[0] = n + 1;

	if (cas_write_block (cf, blk, n + 1)) {
		return (1);
	}

	if (cas_write_trailer (cf)) {
		return (1);
	}

	return (0);
}

static
int cas_write_text (cas_file_t *cf, const char *fname, const char *name, int save)
{
	int  r;
	FILE *fp;

	fp = fopen (fname, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_write_text_fp (cf, fp, name, save ? 0x40 : 0x00);

	fclose (fp);

	return (r);
}

static
int cas_write_binary_fp (cas_file_t *cf, FILE *inp, const char *name,
	unsigned type, unsigned size, unsigned seg, unsigned ofs)
{
	unsigned      n;
	unsigned char blk[256];

	if (cas_write_leader (cf, 256 + par_motor_delay)) {
		return (1);
	}

	if (cas_write_header (cf, name, type, size, seg, ofs)) {
		return (1);
	}

	if (cas_write_trailer (cf)) {
		return (1);
	}

	if (cas_write_leader (cf, 256 + par_motor_delay)) {
		return (1);
	}

	while (size > 0) {
		n = (size < 256) ? size : 256;

		if (fread (blk, 1, n, inp) != n) {
			return (1);
		}

		if (cas_write_block (cf, blk, n)) {
			return (1);
		}

		size -= n;
	}

	if (cas_write_trailer (cf)) {
		return (1);
	}

	return (0);
}

static
int cas_write_binary (cas_file_t *cf, const char *fname, const char *name,
	unsigned seg, unsigned ofs, int save, int prot)
{
	int      r;
	unsigned type;
	unsigned size;
	FILE     *fp;

	fp = fopen (fname, "rb");
	if (fp == NULL) {
		return (1);
	}

	fseek (fp, 0, SEEK_END);
	size = ftell (fp);
	rewind (fp);

	if (size < 0) {
		fclose (fp);
		return (1);
	}

	if (size > 65535) {
		size = 65535;
	}

	if (save) {
		if (prot) {
			type = 0xa0;
		}
		else {
			type = 0x80;
		}
	}
	else {
		type = 0x01;
	}

	r = cas_write_binary_fp (cf, fp, name, type, size, seg, ofs);

	fclose (fp);

	return (r);
}

static
int cas_write_blocks_fp (cas_file_t *cf, FILE *inp, unsigned size)
{
	unsigned      n;
	unsigned char blk[256];

	if (cas_write_leader (cf, 256 + par_motor_delay)) {
		return (1);
	}

	while (size > 0) {
		n = (size < 256) ? size : 256;

		if (fread (blk, 1, n, inp) != n) {
			return (1);
		}

		if (cas_write_block (cf, blk, n)) {
			return (1);
		}

		size -= n;
	}

	if (cas_write_trailer (cf)) {
		return (1);
	}

	return (0);
}

static
int cas_write_blocks (cas_file_t *cf, const char *fname)
{
	int      r;
	unsigned size;
	FILE     *fp;

	fp = fopen (fname, "rb");
	if (fp == NULL) {
		return (1);
	}

	fseek (fp, 0, SEEK_END);
	size = ftell (fp);
	rewind (fp);

	if (size < 0) {
		fclose (fp);
		return (1);
	}

	r = cas_write_blocks_fp (cf, fp, size);

	fclose (fp);

	return (r);
}

static
int cas_write_file (cas_file_t *cf, const char *fname, const char *name, const char *mode)
{
	static unsigned idx = 1;

	if (par_verbose) {
		fprintf (stderr, "%03u %-8s %s  %04X:%04X  %6lu\n",
			idx, name, mode, par_seg, par_ofs, cf->ofs
		);
	}

	idx += 1;

	if (strcasecmp (mode, "a") == 0) {
		return (cas_write_text (cf, fname, name, 1));
	}

	if (strcasecmp (mode, "d") == 0) {
		return (cas_write_text (cf, fname, name, 0));
	}

	if (strcasecmp (mode, "b") == 0) {
		return (cas_write_binary (cf, fname, name, par_seg, par_ofs, 1, 0));
	}

	if (strcasecmp (mode, "p") == 0) {
		return (cas_write_binary (cf, fname, name, par_seg, par_ofs, 1, 1));
	}

	if (strcasecmp (mode, "m") == 0) {
		return (cas_write_binary (cf, fname, name, par_seg, par_ofs, 0, 0));
	}

	if (strcasecmp (mode, "-") == 0) {
		return (cas_write_blocks (cf, fname));
	}

	return (1);
}

int main (int argc, char **argv)
{
	int        i;
	cas_file_t cf;

	if (argc == 2) {
		if (str_isarg (argv[1], "h", "help")) {
			print_help();
			return (0);
		}
		else if (str_isarg (argv[1], "V", "version")) {
			print_version();
		}
	}

	cas_open_fp (&cf, stdout);

	i = 1;
	while (i < argc) {
		if (str_isarg (argv[i], "d", "motor-delay")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_motor_delay = strtoul (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "f", "file")) {
			if ((i + 3) >= argc) {
				return (1);
			}

			if (cas_write_file (&cf, argv[i + 1], argv[i + 2], argv[i + 3])) {
				fprintf (stderr, "%s: error (%s)\n",
					argv[0], argv[i + 1]
				);
				return (1);
			}

			i += 3;
		}
		else if (str_isarg (argv[i], "S", "segment")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_seg = strtoul (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "O", "offset")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_ofs = strtoul (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "v", "verbose")) {
			par_verbose = 1;
		}
		else {
			fprintf (stderr, "%s: unknown option (%s)\n",
				argv[0], argv[i]
			);
			return (1);
		}

		i += 1;
	}

	cas_flush_out (&cf);

	return (0);
}
