/*****************************************************************************
 * misc-utils                                                                *
 *****************************************************************************/

/*****************************************************************************
 * File name:     strtoint.c                                                 *
 * Created:       2001-04-21 by Hampa Hug <hampa@hampa.ch>                   *
 * Last modified: 2004-04-12 by Hampa Hug <hampa@hampa.ch>                   *
 * Copyright:     (C) 2001-2004 Hampa Hug <hampa@hampa.ch>                   *
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

/* $Id: strtoint.c 251 2004-04-12 19:40:42Z hampa $ */


#include <limits.h>

#include "strtoint.h"


int str_get_integer (const char *str, int *sign, unsigned long *ret)
{
	unsigned      base, dig;
	unsigned long tmp;

	while ((*str == ' ') || (*str == '\t')) {
		str += 1;
	}

	switch (*str) {
		case '-':
			str += 1;
			*sign = 1;
			break;

		case '+':
			str += 1;
			*sign = 0;
			break;

		default:
			*sign = 0;
			break;
	}

	base = 10;

	if (*str == '0') {
		switch (str[1]) {
			case 'x':
			case 'X':
				str += 2;
				base = 16;
				break;

			case 'b':
			case 'B':
				str += 2;
				base = 2;
				break;

			case 'o':
			case 'O':
				str += 2;
				base = 2;
				break;

			case 'd':
			case 'D':
				str += 2;
				base = 2;
				break;

			default:
				base = 10;
				break;
		}
	}

	*ret = 0;

	while (1) {
		if ((*str >= '0') && (*str <= '9')) {
			dig = (unsigned) (*str - '0');
		}
		else if ((*str >= 'a') && (*str <= 'f')) {
			dig = (unsigned) (*str - 'a' + 10);
		}
		else if ((*str >= 'A') && (*str <= 'F')) {
			dig = (unsigned) (*str - 'A' + 10);
		}
		else {
			break;
		}

		if (dig >= base) {
			return (1);
		}

		tmp = base * *ret + dig;

		if (tmp < *ret) {
			return (1);
		}

		*ret = tmp;

		str += 1;
	}

	switch (*str) {
		case 'k':
		case 'K':
			str += 1;
			*ret *= 1024UL;
			break;

		case 'm':
		case 'M':
			str += 1;
			*ret *= 1024UL * 1024UL;
			break;

		case 'g':
		case 'G':
			str += 1;
			*ret *= 1024UL * 1024UL * 1024UL;
			break;

		default:
			break;
	}

	while ((*str == ' ') || (*str == '\t')) {
		str += 1;
	}

	if (*str != 0) {
		return (1);
	}

	return (0);
}

int str_get_ulng (const char *str, unsigned long *ret)
{
	int s;

	if (str_get_integer (str, &s, ret)) {
		return (1);
	}

	if (s) {
		*ret = -(*ret);
	}

	return (0);
}

int str_get_slng (const char *str, long *ret)
{
	int           s;
	unsigned long tmp;

	if (str_get_integer (str, &s, &tmp)) {
		return (1);
	}

	if (tmp > LONG_MAX) {
		return (1);
	}

	*ret = (long) tmp;

	if (s) {
		*ret = -(*ret);
	}

	return (0);
}

int str_get_uint (const char *str, unsigned *ret)
{
	int           s;
	unsigned long tmp;

	if (str_get_integer (str, &s, &tmp)) {
		return (1);
	}

	if (s) {
		tmp = -tmp;
	}

	*ret = (unsigned) tmp;

	return (0);
}

int str_get_sint (const char *str, int *ret)
{
	int           s;
	unsigned long tmp;

	if (str_get_integer (str, &s, &tmp)) {
		return (1);
	}

	if (tmp > INT_MAX) {
		return (1);
	}

	*ret = (int) tmp;

	if (s) {
		*ret = -(*ret);
	}

	return (0);
}
