#!/usr/bin/env bash

set echo off
set -e

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

echo ------ building openal ------
pushd libs/PPL/include/openal-soft
mkdir build || true && cd build
cmake ..
make
popd

echo ------ building XPMP2 ------
pushd libs/XPMP2
# git apply ../../tools/xpmp2-0001-adapt-cmakelists-to-mingw.patch
mkdir build || true && cd build
cmake ..
make
popd

echo ------ building the plugin ------
mkdir build || true && cd build
cmake ..
make
echo ------ running unit tests ------
make test

popd
echo ------ build-all: success ------
