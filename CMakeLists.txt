# ATC build script.
#
# Useful command line flags:
#   * -DCMAKE_BUILD_TYPE=XXX, where XXX is one of Debug, Release,
#     RelWithDebInfo and MinSizeRel. Default is Release.
#   

cmake_minimum_required(VERSION 3.9)

include(CTest)
enable_testing()

set(ATC_LIBS_PATH "${CMAKE_CURRENT_SOURCE_DIR}/libs")
set(GTEST_INCLUDE_DIR "${ATC_LIBS_PATH}/googletest/googletest/include")
set(GTEST_LIBRARY_PATH "${ATC_LIBS_PATH}/googletest/build/lib/")

# add_subdirectory(libs/googletest)
add_subdirectory(src)
