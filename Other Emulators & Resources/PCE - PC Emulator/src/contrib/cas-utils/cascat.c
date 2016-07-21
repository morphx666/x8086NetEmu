/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/cascat.c                                           *
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


#include <stdlib.h>
#include <stdio.h>
#include <string.h>

#include "crc16.h"
#include "cassette.h"
#include "util.h"


static int      par_verbose = 0;
static int      par_drop = 0;
static unsigned par_motor_delay = 64;


static
void print_help (void)
{
	fputs (
		"cascat: concatenate cassette files\n"
		"\n"
		"usage: cascat [options] [file...]\n"
		"  -b, --drop-bad-blocks  Drop blocks with bad CRC [no]\n"
		"  -d, --motor-delay int  Set the motor delay in bytes [64]\n"
		"  -i, --input string     Set an input file\n"
		"  -v, --verbose          Verbose operation [no]\n",
		stdout
	);

	fflush (stdout);
}

static
void print_version (void)
{
	fputs (
		"cascat version 0.0.1\n\n"
		"Copyright (C) 2008-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
int cas_cat_fp (FILE *inp, cas_file_t *out)
{
	unsigned      i;
	int           ok, first;
	unsigned long lpos, spos;
	cas_file_t    src;
	unsigned char buf[256];

	cas_open_fp (&src, inp);

	spos = src.ofs;

	first = 1;

	while (cas_read_leader (&src, &lpos) == 0) {
		if (par_verbose) {
			if (first == 0) {
				fputs ("\n", stderr);
			}

			fprintf (stderr, "%lu\tleader (%lu)\n",
				out->ofs, src.ofs - spos
			);
		}

		if (cas_write_leader (out, 256 + par_motor_delay)) {
			return (1);
		}

		i = 0;

		while (cas_read_block (&src, buf, 64, &ok) == 0) {
			if (par_verbose) {
				fprintf (stderr, "%lu\tblock %u%s\n",
					out->ofs, i,
					ok ? "" : " (BAD CRC)"
				);
			}

			if ((par_drop == 0) || ok) {
				if (cas_write_block (out, buf, 256)) {
					return (1);
				}
			}

			i += 1;
		}

		if (cas_write_trailer (out)) {
			return (1);
		}

		spos = src.ofs;

		first = 0;
	}

	return (0);
}

static
int cas_cat (const char *inp, cas_file_t *out)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	if (par_verbose) {
		fprintf (stderr, "%s:\n", inp);
	}

	r = cas_cat_fp (fp, out);

	fclose (fp);

	return (r);
}

int main (int argc, char **argv)
{
	int        i;
	int        done;
	cas_file_t out;

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

	cas_open_fp (&out, stdout);

	i = 1;
	while (i < argc) {
		if (str_isarg (argv[i], "b", "drop-bad-blocks")) {
			par_drop = 1;
		}
		else if (str_isarg (argv[i], "d", "motor-delay")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_motor_delay = strtoul (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "i", "input")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			cas_cat (argv[i], &out);
			done = 1;
		}
		else if (str_isarg (argv[i], "v", "verbose")) {
			par_verbose = 1;
		}
		else if (argv[i][0] == '-') {
			fprintf (stderr, "%s: unknown option (%s)\n",
				argv[0], argv[i]
			);
		}
		else {
			cas_cat (argv[i], &out);
			done = 1;
		}

		i += 1;
	}

	if (done == 0) {
		cas_cat_fp (stdin, &out);
	}

	fflush (stdout);

	return (0);
}
