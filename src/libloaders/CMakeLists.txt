cmake_minimum_required(VERSION 3.9)
project(libloaders VERSION 0.1.0)

add_library(libloaders STATIC 
    libdataxp.h
    tokenValueFileReaderBase.cpp
    xpAirportReader.cpp
    xpFmsxReader.cpp
    xpSceneryPacksIniReader.cpp
    libopenflights.hpp
)

set_property(TARGET libloaders PROPERTY CXX_STANDARD 14)
target_include_directories(libloaders PUBLIC ../libworld)
target_link_libraries(libloaders libworld) 

if (UNIX)
    target_compile_options(libloaders PUBLIC -fPIC)
endif()
