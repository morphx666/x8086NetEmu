
JAVASOURCES = $(wildcard retro/*.java)
JAVACLASSES = $(patsubst %.java,%.class,$(JAVASOURCES))
CLASSFILES = $(wildcard retro/*.class)
BIOSBIN = bios/rombios.bin

JAVAC = jikes-classpath
JAVACFLAGS = --source 1.4
JAR = fastjar

JAVADOC = javadoc

.PHONY: all clean

all : $(JAVACLASSES) $(BIOSBIN)

%.class : %.java
	$(JAVAC) $(JAVACFLAGS) $<

bios/rombios.bin : bios/rombios.asm bios/vga-rom.f08
	cd bios ; nasm -o $(@F) $(<F)

retro.jar : manifest.txt $(JAVACLASSES) $(wildcard retro/*.class) $(BIOSBIN)
	$(JAR) -cmf manifest.txt $@ retro/*.class $(BIOSBIN)

javadoc : $(JAVASOURCES)
	mkdir -p javadoc
	$(JAVADOC) -d javadoc retro

clean:
	$(RM) retro/*.class bios/rombios.bin *.jar
	$(RM) -r javadoc

