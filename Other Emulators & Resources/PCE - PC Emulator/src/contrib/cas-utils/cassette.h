/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/cassette.h                                         *
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


#ifndef CASSETTE_H
#define CASSETTE_h 1


#include <stdio.h>


typedef struct {
	FILE          *fp;
	unsigned char buf;
	unsigned      cnt;

	unsigned long ofs;
} cas_file_t;


int cas_open_fp (cas_file_t *cf, FILE *fp);

int cas_read_bit (cas_file_t *cf, int *val);

int cas_read_byte (cas_file_t *cf, unsigned char *val);

int cas_read_leader (cas_file_t *cf, unsigned long *start);

int cas_read_block (cas_file_t *cf, void *buf, unsigned maxff, int *ok);


int cas_flush_out (cas_file_t *cf);

int cas_write_bit (cas_file_t *cf, int val);

int cas_write_byte (cas_file_t *cf, unsigned char val);

int cas_write_leader (cas_file_t *cf, unsigned cnt);

int cas_write_trailer (cas_file_t *cf);

int cas_write_block (cas_file_t *cf, const void *buf, unsigned cnt);

int cas_write_header (cas_file_t *cf, const char *name,
	unsigned type, unsigned size, unsigned seg, unsigned ofs);


#endif
