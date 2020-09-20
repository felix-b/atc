cmake_minimum_required(VERSION 3.9)
project(libai VERSION 0.1.0)

add_library(libai STATIC 
    libai.h
    libai.cpp    
)

set_property(TARGET libai PROPERTY CXX_STANDARD 14)
target_include_directories(libai PUBLIC ../libworld)
target_link_libraries(libai libworld) 
