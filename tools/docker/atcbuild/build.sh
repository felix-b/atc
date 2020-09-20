#!/usr/bin/env bash

set -e

function build_platform() {
  local src_dir="$1"
  local platform="$2"
  
  echo "----------------- Building for $platform -----------------"
  echo "src_dir=$src_dir"
  ls -la $src_dir

  local build_dir="$src_dir/build-$platform"

  local flags=("-DATCBUILD_PLATFORM_BIN=build-$platform")
  local cmake="cmake"
  #local generator=""
  case "$platform" in
    lin)
      ;;
    win)
      flags+=('-DCMAKE_TOOLCHAIN_FILE=/build/toolchain-mingw-w64-x86-64.cmake')
      #generator="-G 'MinGW Makefiles'"
      ;;
    mac)
      #export CXX=/clang_9.0.0/bin/clang++
      #export CC=/clang_9.0.0/bin/clang
      flags+=('-DCMAKE_TOOLCHAIN_FILE=/build/toolchain-ubuntu-osxcross-10.11.cmake')
      flags+=('-DCMAKE_FIND_ROOT_PATH=/usr/osxcross/SDK/MacOSX10.11.sdk/')
      ;;
    *)
      echo "Platform $platform is not supported, skipping..."
      return
  esac

  (
    export PATH="$PATH:/usr/osxcross/bin"
    mkdir -p "$build_dir" && cd "$build_dir"
    #rm -rf *
    "$cmake" "${flags[@]}" ..
    make

    case "$platform" in 
      lin)
        make test
        ;;
    esac
  )
}

src_dir="$(pwd)"

for platform in $@; do
  build_platform "$src_dir" "$platform"
done
