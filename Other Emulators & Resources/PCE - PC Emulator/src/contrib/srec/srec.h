/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:   srec.h                                                       *
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


typedef struct {
	unsigned char  type;
	unsigned char  cnt;
	unsigned long  addr;
	unsigned       asize;
	unsigned char  data[256];
	unsigned char  cksum;
} record_t;


#define IHEX_REC_DATA 0x00
#define IHEX_REC_EOFR 0x01
#define IHEX_REC_ESAR 0x02
#define IHEX_REC_SSAR 0x03
#define IHEX_REC_ELAR 0x04
#define IHEX_REC_SLAR 0x05
