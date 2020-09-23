#!/usr/bin/env bash

set echo off
set -e

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

rm -rf build

popd
echo "------ clean: success ------"
