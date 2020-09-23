cmake_minimum_required(VERSION 3.9)
project(libdataxp VERSION 0.1.0)

add_library(libdataxp STATIC 
    libdataxp.h
    xpAirportReader.cpp
)

set_property(TARGET libdataxp PROPERTY CXX_STANDARD 14)
target_include_directories(libdataxp PUBLIC ../libworld)
target_link_libraries(libdataxp libworld) 
