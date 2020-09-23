cmake_minimum_required(VERSION 3.12.0)
project(libspeechmac)

add_library(libspeechmac STATIC
    libspeechmac.cpp
    synthesizer.h
    synthesizer.m
)

target_include_directories(libspeechmac PUBLIC "${CMAKE_SOURCE_DIR}/../../src/include")
set_target_properties(libspeechmac PROPERTIES PREFIX "")
