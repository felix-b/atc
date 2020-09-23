cmake_minimum_required(VERSION 3.12.0)
project(libspeechlin)
set(CMAKE_BUILD_TYPE "RELEASE")

add_library(libspeechlin STATIC
    libspeechlin.cpp
)

target_include_directories(libspeechlin PUBLIC "${CMAKE_SOURCE_DIR}/../../src/include")
target_include_directories(libspeechlin PUBLIC "${CMAKE_SOURCE_DIR}/../XPSDK/CHeaders/XPLM")
target_compile_options(libspeechlin PUBLIC -fPIC)
set_target_properties(libspeechlin PROPERTIES PREFIX "")
