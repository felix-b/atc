cmake_minimum_required(VERSION 3.9)
project(libworld VERSION 0.1.0)

add_library(libworld STATIC 
    libworld.h
    libworld.cpp    
)

set_property(TARGET libworld PROPERTY CXX_STANDARD 14)