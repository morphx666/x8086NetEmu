# Makefile

prefix = /usr/local
bindir = $(prefix)/bin

distdir = dist

#CROSS = i686-mingw32-

CC    = $(CROSS)gcc
LD    = $(CROSS)gcc

STRIP     = $(CROSS)strip
STRIP_OPT = -p

CFLAGS = -O -Wall

SRC := cascat.c casdec.c casenc.c castopcm.c pcmtocas.c crc16.c cassette.c util.c
HDR := cascat.h casdec.h casenc.h castopcm.h pcmtocas.h crc16.h cassette.h util.h
OBJ := cascat.o casdec.o casenc.o castopcm.o pcmtocas.o crc16.o cassette.o util.o
BIN := cascat$(EXEEXT) casdec$(EXEEXT) casenc$(EXEEXT) castopcm$(EXEEXT) pcmtocas$(EXEEXT)

SDP := $(HDR) Makefile
BDP := $(OBJ) Makefile


all: $(BIN)


cascat$(EXEEXT): cascat.o cassette.o crc16.o util.o Makefile
	$(LD) $(LDFLAGS) -o $@ cascat.o cassette.o crc16.o util.o

casdec$(EXEEXT): casdec.o cassette.o crc16.o util.o Makefile
	$(LD) $(LDFLAGS) -o $@ casdec.o cassette.o crc16.o util.o

casenc$(EXEEXT): casenc.o cassette.o crc16.o util.o Makefile
	$(LD) $(LDFLAGS) -o $@ casenc.o cassette.o crc16.o util.o

castopcm$(EXEEXT): castopcm.o util.o Makefile
	$(LD) $(LDFLAGS) -o $@ castopcm.o util.o -lm

pcmtocas$(EXEEXT): pcmtocas.o util.o Makefile
	$(LD) $(LDFLAGS) -o $@ pcmtocas.o util.o -lm


%.o: %.c
	$(CC) $(CFLAGS) -c -o $@ $<


clean:
	rm -f $(BIN) $(OBJ)


install: all
	mkdir -p $(DESTDIR)$(bindir)
	for f in $(BIN) ; do \
		install -m 755 "$$f" "$(DESTDIR)$(bindir)/$$f" ; \
	done


dist: all
	mkdir -p $(distdir)
	for f in $(BIN) ; do \
		install -m 755 "$$f" "$(distdir)/$$f" ; \
		$(STRIP) $(STRIP_OPT) "$(distdir)/$$f" ; \
	done


cascat.o:	cascat.c $(SDP)
casdec.o:	casdec.c $(SDP)
casenc.o:	casenc.c $(SDP)
castopcm.o:	castopcm.c $(SDP)
pcmtocas.o:	pcmtocas.c $(SDP)

cassette.o:	cassette.c $(SDP)

crc16.o:	crc16.c crc16.h Makefile
util.o:		util.c util.h Makefile
