cmake_minimum_required(VERSION 3.9)
project(libworld_test CXX)

message("GTEST_INCLUDE_DIR = ${GTEST_INCLUDE_DIR}")

include(CTest)
include(GoogleTest)
enable_testing()

add_executable(libworld_test 
    libworld_test.h
    taxiEdgeTest.cpp
    worldBuilderTest.cpp
    worldTest.cpp
    airportTest.cpp
    taxiPathTest.cpp
    uniPointTest.cpp
    geoMathTest.cpp
    hostServicesTest.cpp
    airportTest.h
    taxiNodeTest.cpp
    taxiNetTest.cpp
    stateMachineTest.cpp
    airlineReferenceTableTest.cpp
    unit_testable_world.hpp
    openflightsTest.cpp
)

set_property(TARGET libworld_test PROPERTY CXX_STANDARD 14)
target_include_directories(libworld_test PUBLIC ../libworld)
target_link_libraries(libworld_test libworld GTest::GTest GTest::Main)

message("CMAKE_CURRENT_SOURCE_DIR = ${CMAKE_CURRENT_SOURCE_DIR}")

if (ATCBUILD_CAN_RUN_TESTS)
    gtest_discover_tests(
        libworld_test
        PROPERTIES WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )
endif()
