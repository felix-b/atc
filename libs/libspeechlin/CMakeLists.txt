cmake_minimum_required(VERSION 3.12.0)
project(libspeechlin)

add_library(libspeechlin STATIC
    libspeechlin.cpp
)

target_include_directories(libspeechlin PUBLIC "${CMAKE_SOURCE_DIR}/../../src/include")
target_include_directories(libspeechlin PUBLIC "${CMAKE_SOURCE_DIR}/../XPSDK/CHeaders/XPLM")
set_target_properties(libspeechlin PROPERTIES PREFIX "")
