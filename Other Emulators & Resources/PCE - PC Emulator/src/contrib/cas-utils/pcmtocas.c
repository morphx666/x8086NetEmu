/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/pcmtocas.c                                         *
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

#include "util.h"


static int      par_bias = 0;
static int      par_volume = 1;
static int      par_unsigned = 0;
static int      par_lowpass = 0;
static unsigned par_shift = 0;


static
void print_help (void)
{
	fputs (
		"pcmtocas: convert PCM to cassette files\n"
		"\n"
		"usage: pcmtocas [options] [file...] > out\n"
		"  -b, --bias int    Add a value to samples [0]\n"
		"  -f, --shift int   Insert bits at start of output [0]\n"
		"  -l, --lowpass     Use a simple lowpass filter [no]\n"
		"  -u, --unsigned    Input is unsigned PCM [no]\n"
		"  -v, --volume int  Adjust the volume [1]\n"
		"\n"
		"The input is 8-bit signed PCM at 44100 Hz\n",
		stdout
	);

	fflush (stdout);
}

static
void print_version (void)
{
	fputs (
		"pcmtocas version 0.0.1\n\n"
		"Copyright (C) 2008-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

/*
 * Write bit val to fp using buf and cnt as a bit buffer
 */
static
void write_bit (FILE *fp, unsigned *buf, unsigned *cnt, unsigned long *ofs, int val)
{
	*buf = (*buf << 1) | (val != 0);
	*cnt += 1;

	if (*cnt >= 8) {
		fputc (*buf & 0xff, fp);
		*buf = 0;
		*cnt = 0;
		*ofs += 1;
	}
}

/*
 * Read a sample from the source file
 */
static
int get_sample (FILE *inp, int *val)
{
	int v;

	v = fgetc (inp);

	if (v == EOF) {
		return (1);
	}

	if (par_unsigned) {
		v -= 128;
	}
	else {
		if (v & 0x80) {
			v -= 256;
		}
	}

	*val = par_volume * v + par_bias;

	return (0);
}

/*
 * Convert 44100 Hz, 8 bit, signed PCM to raw bits
 */
static
int pcm_to_cas_fp (FILE *inp, FILE *out)
{
	unsigned      i;
	int           x1, x2, x3, x4, x5;
	int           v1, v2;
	unsigned      cnt;
	unsigned      bbuf, bcnt;
	unsigned long spos, dpos;

	x2 = 0;
	x3 = 0;
	x4 = 0;
	x5 = 0;

	v1 = 0;
	v2 = 0;

	cnt = 0;

	bbuf = 0;
	bcnt = 0;

	spos = 0;
	dpos = 0;

	for (i = 0; i < par_shift; i++) {
		write_bit (out, &bbuf, &bcnt, &dpos, 0);
	}

	while (1) {
		if (get_sample (inp, &x1)) {
			break;
		}

		if (par_lowpass) {
			/* a crude low-pass filter */
			v1 = (((1*x1 + 2*x2 + 4*x3 + 2*x4 + 1*x5 + 5) / 10) > 0);
		}
		else {
			v1 = (x3 > 0);
		}

		x5 = x4;
		x4 = x3;
		x3 = x2;
		x2 = x1;

		cnt += 1;

		if ((v1 != v2) && (v1 == 0)) {
			/* falling edge */

			/*
			 * 1.0ms -> 44 samples @ 44.1 kHz
			 * 0.5ms -> 22 samples @ 44.1 kHz
			 */
			if ((cnt > (22 - 8)) && (cnt < (22 + 8))) {
				write_bit (out, &bbuf, &bcnt, &dpos, 0);
			}
			else if ((cnt > (44 - 8)) && (cnt < (44 + 12))) {
				write_bit (out, &bbuf, &bcnt, &dpos, 1);
			}
			else {
				fprintf (stderr, "%08lX %08lX  %u\n", spos, dpos, cnt);
			}

			cnt = 0;
		}

		v2 = v1;

		spos += 1;
	}

	return (0);
}

static
int pcm_to_cas (const char *src)
{
	FILE *fp;

	fp = fopen (src, "rb");
	if (fp == NULL) {
		return (1);
	}

	pcm_to_cas_fp (fp, stdout);

	fclose (fp);

	return (0);
}

int main (int argc, char **argv)
{
	int i;
	int done;

	if (argc >= 2) {
		if (str_isarg (argv[1], "h", "help")) {
			print_help();
			return (0);
		}
		else if (str_isarg (argv[1], "V", "version")) {
			print_version();
			return (0);
		}
	}

	done = 0;

	i = 1;
	while (i < argc) {
		if (str_isarg (argv[i], "b", "bias")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_bias = strtol (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "f", "shift")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_shift = strtoul (argv[i], NULL, 0);
		}
		else if (str_isarg (argv[i], "l", "lowpass")) {
			par_lowpass = 1;
		}
		else if (str_isarg (argv[i], "u", "unsigned")) {
			par_unsigned = 1;
		}
		else if (str_isarg (argv[i], "v", "volume")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			par_volume = strtol (argv[i], NULL, 0);
		}
		else if (argv[i][0] == '-') {
			fprintf (stderr, "%s: unknown option (%s)\n",
				argv[0], argv[i]
			);
			return (1);
		}
		else {
			if (pcm_to_cas (argv[i])) {
				return (1);
			}

			done = 1;
		}

		i += 1;
	}

	if (done == 0) {
		if (pcm_to_cas_fp (stdin, stdout)) {
			return (1);
		}
	}

	return (0);
}
