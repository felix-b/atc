rm -rf build
mkdir build
cd build

g++ -c -ObjC ../synthesizer.m -m64
g++ -c -std=c++11 -I../../../src/pluginxp ../libspeechmac.cpp -m64
# g++ synthesize.o libspeechmac.o -o libspeechmac.dylib -shared -framework Cocoa -m64
ar ru libspeechmac.a synthesizer.o libspeechmac.o

g++ -c -std=c++11 ../test.cpp -m64
g++ -o test test.o synthesizer.o libspeechmac.o -framework Cocoa
