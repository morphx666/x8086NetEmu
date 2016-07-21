/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:     strarg.c                                                   *
 * Created:       2004-04-12 by Hampa Hug <hampa@hampa.ch>                   *
 * Last modified: 2004-04-12 by Hampa Hug <hampa@hampa.ch>                   *
 * Copyright:     (C) 2004 Hampa Hug <hampa@hampa.ch>                        *
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

/* $Id: strarg.c 251 2004-04-12 19:40:42Z hampa $ */


#include "strarg.h"

#include <stdlib.h>
#include <string.h>


int str_isarg2 (const char *str, const char *arg1, const char *arg2)
{
	if ((str == NULL) || (str[0] != '-')) {
		return (0);
	}

	str += 1;

	if (*str == '-') {
		str += 1;
	}

	if (arg1 != NULL) {
		while (*arg1 == '-') {
			arg1 += 1;
		}

		if (strcmp (str, arg1) == 0) {
			return (1);
		}
	}

	if (arg2 != NULL) {
		while (*arg2 == '-') {
			arg2 += 2;
		}

		if (strcmp (str, arg2) == 0) {
			return (1);
		}
	}

	return (0);
}

int str_isarg1 (const char *str, const char *arg)
{
	return (str_isarg2 (str, arg, NULL));
}
