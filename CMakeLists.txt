# ATC build script.
#
# Useful command line flags:
#   * -DCMAKE_BUILD_TYPE=XXX, where XXX is one of Debug, Release,
#     RelWithDebInfo and MinSizeRel. Default is Release.
#   

cmake_minimum_required(VERSION 3.9)

set(ATC_LIBS_PATH "${CMAKE_CURRENT_SOURCE_DIR}/libs")

add_subdirectory(src)
