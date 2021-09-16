#!/usr/bin/env bash

set echo off
set -e

thisScriptDir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
pushd "$thisScriptDir/.."

echo "------ deep-cloning libs ------"
git submodule update --init --recursive

tasks/clean-all.sh && tasks/build-all.sh

popd
echo "------ init-all: success ------"
