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
message ("CMAKE_CXX_COMPILER_ID" = ${CMAKE_CXX_COMPILER_ID})

set(ATC_LIBS_PATH "${CMAKE_CURRENT_SOURCE_DIR}/libs")
set(GTEST_INCLUDE_DIR "${ATC_LIBS_PATH}/googletest/googletest/include")
set(GTEST_LIBRARY_PATH "${ATC_LIBS_PATH}/googletest/${ATCBUILD_PLATFORM_BIN}/lib/")

message ("ATC_LIBS_PATH" = ${ATC_LIBS_PATH})
message ("GTEST_INCLUDE_DIR" = ${GTEST_INCLUDE_DIR})
message ("GTEST_LIBRARY_PATH" = ${GTEST_LIBRARY_PATH})

set(CMAKE_LIBRARY_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/build/lib")
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/build/lib")
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY "${CMAKE_CURRENT_SOURCE_DIR}/build/bin")
set(CMAKE_STATIC_LIBRARY_PREFIX "")

message ("CMAKE_LIBRARY_OUTPUT_DIRECTORY" = ${CMAKE_LIBRARY_OUTPUT_DIRECTORY})
message ("CMAKE_ARCHIVE_OUTPUT_DIRECTORY" = ${CMAKE_ARCHIVE_OUTPUT_DIRECTORY})
message ("CMAKE_RUNTIME_OUTPUT_DIRECTORY" = ${CMAKE_RUNTIME_OUTPUT_DIRECTORY})

message ("------ atcbuild: end of environment ------")

#add_subdirectory(libs/PPL)
#add_subdirectory(libs/XPMP2)
add_subdirectory(src)
