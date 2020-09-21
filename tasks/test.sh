#!/usr/bin/env bash

set echo off
set -e

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

tasks/build.sh
echo "------ running unit tests ------"
make test

popd
echo "------ test: success ------"
