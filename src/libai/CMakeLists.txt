cmake_minimum_required(VERSION 3.0.0)
project(libai VERSION 0.1.0)

add_library(libai STATIC 
    libai.hpp
    libai.cpp
    aiController.hpp
    aiPilot.hpp
    aiAircraft.hpp
    maneuverFactory.hpp
    maneuverFactory.cpp
    clearanceFactory.hpp
)

set_property(TARGET libai PROPERTY CXX_STANDARD 14)
target_include_directories(libai PUBLIC ../libworld)
target_link_libraries(libai libworld)

if (UNIX)
    target_compile_options(libai PUBLIC -fPIC)
endif()
