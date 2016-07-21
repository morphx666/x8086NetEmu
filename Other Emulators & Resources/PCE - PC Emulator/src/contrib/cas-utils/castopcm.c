/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/castopcm.c                                         *
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
#include <math.h>

#include "util.h"


static unsigned char par_bit_0[22] = {
	0xfe, 0xef, 0xd8, 0xb9, 0xa0, 0x94, 0x9f, 0xb8,
	0xd4, 0xe8, 0xf6, 0x01, 0x10, 0x27, 0x42, 0x5c,
	0x65, 0x5c, 0x45, 0x2a, 0x17, 0x09
};

static unsigned char par_bit_1[44] = {
	0xfc, 0xe8, 0xd3, 0xbc, 0xa6, 0x96, 0x9b, 0xa9,
	0xbc, 0xcd, 0xda, 0xe1, 0xe7, 0xec, 0xee, 0xf1,
	0xf2, 0xf4, 0xf7, 0xfb, 0x04, 0x0c, 0x18, 0x24,
	0x37, 0x4c, 0x60, 0x68, 0x66, 0x59, 0x4a, 0x38,
	0x2c, 0x25, 0x21, 0x1e, 0x1c, 0x19, 0x17, 0x14,
	0x10, 0x0c, 0x08, 0x04
};

static
void print_help (void)
{
	fputs (
		"castopcm: convert cassette files to PCM\n"
		"\n"
		"usage: castopcm [options] [file...] > out\n"
		"  -q, --square      Generate a square wave\n"
		"  -s, --sine        Generate a sine wave\n"
		"  -w, --wave        Generate original casstte waveform [default]\n"
		"  -v, --volume int  Set the volume (0..127) [96]\n"
		"\n"
		"The output is 8-bit signed PCM at 44100 Hz\n",
		stdout
	);

	fflush (stdout);
}

static
void print_version (void)
{
	fputs (
		"castopcm version 0.0.1\n\n"
		"Copyright (C) 2008-2009 Hampa Hug <hampa@hampa.ch>\n",
		stdout
	);

	fflush (stdout);
}

static
void write_bit_square (FILE *fp, int val, int volume)
{
	unsigned i, n;

	n = val ? 22 : 11;

	fputc (0x00, fp);

	for (i = 1; i < n; i++) {
		fputc (-volume & 0xff, fp);
	}

	fputc (0x00, fp);

	for (i = 1; i < n; i++) {
		fputc (volume & 0xff, fp);
	}
}

static
void write_bit_sin (FILE *fp, int val, int volume)
{
	unsigned i, n, v;

	n = val ? 44 : 22;

	for (i = 0; i < n; i++) {
		v = -volume * sin (2.0 * 3.1415926 * (double) i / (double) n);
		fputc (v & 0xff, fp);
	}
}

static
void write_bit_wave (FILE *fp, int val, int volume)
{
	unsigned      i, n;
	unsigned char *src;
	unsigned      v1, v2, v3;

	if (val) {
		n = 44;
		src = par_bit_1;
	}
	else {
		n = 22;
		src = par_bit_0;
	}

	v1 = 0x80;
	v2 = src[0] ^ 0x80;
	v3 = src[1] ^ 0x80;

	for (i = 0; i < n; i++) {
		fputc (((v1 + 2 * v2 + v3 + 2) / 4) ^ 0x80, fp);
		v1 = v2;
		v2 = v3;
		v3 = (((i + 2) < n) ? src[i + 2] : 0) ^ 0x80;
	}
}

static
int cas_to_pcm_fp (FILE *inp, FILE *out, unsigned mode, int volume)
{
	int      c;
	int      bit;
	unsigned i;

	while (1) {
		c = fgetc (stdin);
		if (c == EOF) {
			break;
		}

		for (i = 0; i < 8; i++) {
			bit = (c & (0x80 >> i)) != 0;

			if (mode == 1) {
				write_bit_square (stdout, bit, volume);
			}
			else if (mode == 2) {
				write_bit_sin (stdout, bit, volume);
			}
			else if (mode == 3) {
				write_bit_wave (stdout, bit, volume);
			}
		}
	}

	return (0);
}

static
int cas_to_pcm (const char *inp, FILE *out, unsigned mode, int volume)
{
	int  r;
	FILE *fp;

	fp = fopen (inp, "rb");
	if (fp == NULL) {
		return (1);
	}

	r = cas_to_pcm_fp (fp, out, mode, volume);

	fclose (fp);

	return (r);
}

int main (int argc, char **argv)
{
	int      i;
	int      done;
	unsigned mode;
	int      volume;

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
	mode = 1;
	volume = 96;

	i = 1;
	while (i < argc) {
		if (str_isarg (argv[i], "q", "square")) {
			mode = 1;
		}
		else if (str_isarg (argv[i], "s", "sine")) {
			mode = 2;
		}
		else if (str_isarg (argv[i], "w", "wave")) {
			mode = 3;
		}
		else if (str_isarg (argv[i], "v", "volume")) {
			i += 1;
			if (i >= argc) {
				return (1);
			}

			volume = strtol (argv[i], NULL, 0);
		}
		else if (argv[i][0] == '-') {
			fprintf (stderr, "%s: unknown option (%s)\n",
				argv[0], argv[i]
			);
			return (1);
		}
		else {
			if (cas_to_pcm (argv[i], stdout, mode, volume)) {
				return (1);
			}

			done = 1;
		}

		i += 1;
	}

	if (done == 0) {
		if (cas_to_pcm_fp (stdin, stdout, mode, volume)) {
			return (1);
		}
	}

	return (0);
}
