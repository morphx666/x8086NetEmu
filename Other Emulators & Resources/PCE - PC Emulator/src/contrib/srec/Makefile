# Makefile

prefix = /usr/local
bindir = $(prefix)/bin
mandir = $(prefix)/man

distdir = dist

#CROSS = i686-mingw32-

CC    = $(CROSS)gcc
LD    = $(CROSS)gcc

STRIP     = $(CROSS)strip
STRIP_OPT = -p

SRC	:= srec.c strarg.c strtoint.c
OBJ	:= srec.o strarg.o strtoint.o
HDR	:= srec.h strarg.h strtoint.h
MAN1	:= srec.1
BIN	:= srec$(EXEEXT)

SDP	:= Makefile $(HDR)
BDP	:= Makefile $(OBJ)


all: $(BIN)


srec$(EXEEXT): $(BDP)
	$(LD) $(LDFLAGS) $(OBJ) -o $@


clean:
	rm -f $(OBJ) $(BIN)


install: all
	mkdir -p $(DESTDIR)$(bindir)
	mkdir -p $(DESTDIR)$(mandir)/man1
	for f in $(BIN) ; do \
		install -m 755 "$$f" "$(DESTDIR)$(bindir)/$$f" ; \
	done
	for f in $(MAN1) ; do \
		install -m 644 "$$f" "$(DESTDIR)$(mandir)/man1/$$f" ; \
	done


dist: all
	mkdir -p $(distdir)
	for f in $(BIN) ; do \
		install -m 755 "$$f" "$(distdir)/$$f" ; \
		$(STRIP) $(STRIP_OPT) "$(distdir)/$$f" ; \
	done
	for f in $(MAN1) ; do \
		install -m 644 "$$f" "$(distdir)/$$f" ; \
	done


%.o: %.c
	$(CC) $(CFLAGS) -c $< -o $@


srec.o:		srec.c $(SDP)
strarg.o:	strarg.c $(SDP)
strtoint.o:	strtoint.c $(SDP)
