#!/usr/bin/env bash

set echo off
set -e
osName="$(uname -s)"

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

echo ------ cleaning googletest ------
pushd libs/googletest
git reset --hard
rm -rf build
popd

echo ------ cleaning PPL ------
pushd libs/PPL
git reset --hard
rm -rf build
popd

echo ------ cleaning openal ------
pushd libs/PPL/include/openal-soft
git reset --hard
rm -rf build
popd

echo ------ cleaning XPMP2 ------
pushd libs/XPMP2
git reset --hard
rm -rf build
popd

case "${osName}" in
    Darwin*)
        echo ------ cleaning libspeechmac ------
        pushd libs/libspeechmac
        # here we don't git reset --hard!
        rm -rf build
        popd
        ;;
esac

echo ------ cleaning plugin build ------
# here we don't git reset --hard!
rm -rf build

popd
echo ------ clean-all: success ------
