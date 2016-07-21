/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:   strtoint.h                                                   *
 * Created:     2001-04-21 by Hampa Hug <hampa@hampa.ch>                     *
 * Copyright:   (C) 2001-2003 by Hampa Hug <hampa@hampa.ch>                  *
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


#ifndef MISC_STRTOINT_H
#define MISC_STRTOINT_H 1


int str_get_ulng (const char *str, unsigned long *ret);
int str_get_slng (const char *str, long *ret);
int str_get_uint (const char *str, unsigned *ret);
int str_get_sint (const char *str, int *ret);


#endif
