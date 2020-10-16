cmake_minimum_required(VERSION 3.0.0)
project(libworld VERSION 0.1.0)

add_library(libworld STATIC 
    actor.cpp
    aircraft.cpp
    airport.cpp
    airspaceClass.cpp
    altitude.cpp
    basicManeuverTypes.hpp
    clearanceTypes.hpp
    controlFacility.cpp
    controllerPosition.cpp
    flight.cpp
    frequency.cpp
    geoMath.cpp
    geoPoint.cpp
    intent.cpp
    intentFactory.hpp
    intentTypes.cpp
    intentTypes.hpp
    libworld.cpp
    libworld.h
    maneuver.cpp
    runway.cpp
    simplePhraseologyService.hpp
    state.h
    stlhelpers.cpp
    stlhelpers.h
    taxiEdge.cpp
    taxiNet.cpp
    taxiNode.cpp
    taxiPath.cpp
    uniPoint.cpp
    utteranceBuilder.cpp
    world.cpp
    worldBuilder.cpp
    worldHelper.hpp
    stateMachine.hpp
    hostServices.cpp
)

set_property(TARGET libworld PROPERTY CXX_STANDARD 14)

if (UNIX)
    target_compile_options(libworld PUBLIC -fPIC)
endif()
