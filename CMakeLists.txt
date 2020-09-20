# ATC build script.
#
# Useful command line flags:
#   * -DCMAKE_BUILD_TYPE=XXX, where XXX is one of Debug, Release,
#     RelWithDebInfo and MinSizeRel. Default is Release.
#   

cmake_minimum_required(VERSION 3.9)
project(atcbuild)

include(CTest)
enable_testing()

message ("------ atcbuild: environment ------")

if (NOT DEFINED ATCBUILD_PLATFORM_BIN)
    set(ATCBUILD_PLATFORM_BIN "build")
    message ("Warning: ATCBUILD_PLATFORM_BIN not specified, using 'build'")
endif()

if (CMAKE_SYSTEM_NAME STREQUAL CMAKE_HOST_SYSTEM_NAME)
    set(ATCBUILD_CAN_RUN_TESTS TRUE)
else()
    set(ATCBUILD_CAN_RUN_TESTS FALSE)
endif()

message ("ATCBUILD_PLATFORM_BIN" = ${ATCBUILD_PLATFORM_BIN})
message ("ATCBUILD_CAN_RUN_TESTS" = ${ATCBUILD_CAN_RUN_TESTS})
message ("CMAKE_SYSTEM_NAME" = ${CMAKE_SYSTEM_NAME})
message ("CMAKE_HOST_SYSTEM_NAME" = ${CMAKE_HOST_SYSTEM_NAME})
message ("CMAKE_CURRENT_BINARY_DIR" = ${CMAKE_CURRENT_BINARY_DIR})

set(ATC_LIBS_PATH "${CMAKE_CURRENT_SOURCE_DIR}/libs")
set(GTEST_INCLUDE_DIR "${ATC_LIBS_PATH}/googletest/googletest/include")
set(GTEST_LIBRARY_PATH "${ATC_LIBS_PATH}/googletest/${ATCBUILD_PLATFORM_BIN}/lib/")

message ("ATC_LIBS_PATH" = ${ATC_LIBS_PATH})
message ("GTEST_INCLUDE_DIR" = ${GTEST_INCLUDE_DIR})
message ("GTEST_LIBRARY_PATH" = ${GTEST_LIBRARY_PATH})

message ("------ atcbuild: end of environment ------")

add_subdirectory(src)
