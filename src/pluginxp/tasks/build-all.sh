#!/usr/bin/env bash

set echo off
set -e
osName="$(uname -s)"

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

echo ------ building googletest ------
pushd libs/googletest
mkdir build || true && cd build
cmake ..
make
popd

echo ------ building PPL ------
pushd libs/PPL
git apply ../../tools/patches/ppl-0001-adapt-cmakelists.patch
cp -R ../../tools/patches/glew/include include/glew/
mkdir build || true && cd build
cmake ..
make
popd

# echo ------ building openal ------
# pushd libs/PPL/include/openal-soft
# mkdir build || true && cd build
# cmake ..
# make
# ls -la
# popd

echo ------ building XPMP2 ------
pushd libs/XPMP2
mkdir build || true && cd build
cmake ..
make
popd

# case "${osName}" in
#    Linux*)
#        echo ------ building libspeechlin ------
#        pushd libs/libspeechlin
#        mkdir build || true && cd build
#        cmake ..
#        make
#        ls -la 
#        popd
#        ;;
#    Darwin*)
#        echo ------ building libspeechmac ------
#        pushd libs/libspeechmac
#        mkdir build || true && cd build
#        cmake ..
#        make
#        popd
#        ;;
# esac

echo ------ building the plugin ------
mkdir build || true && cd build
cmake ..
make
echo ------ running unit tests ------
make test

popd
echo ------ build-all: success ------
