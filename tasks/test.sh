#!/usr/bin/env bash

set echo off
set -e

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

echo "------ building the plugin ------"
mkdir build || true && cd build
cmake ..
make
echo "------ running unit tests ------"
make test

popd
echo "------ build: success ------"
