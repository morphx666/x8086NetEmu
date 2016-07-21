/*****************************************************************************
 * pce                                                                       *
 *****************************************************************************/

/*****************************************************************************
 * File name:   cas-utils/util.c                                             *
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


#include <string.h>

#include "util.h"


int str_isarg (const char *str, const char *arg1, const char *arg2)
{
	if (*str != '-') {
		return (0);
	}

	str += 1;

	if (arg1 != NULL) {
		if (strcmp (str, arg1) == 0) {
			return (1);
		}
	}

	if (*str != '-') {
		return (0);
	}

	str += 1;

	if (arg2 != NULL) {
		if (strcmp (str, arg2) == 0) {
			return (1);
		}
	}

	return (0);
}
