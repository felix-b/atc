cmake_minimum_required(VERSION 3.9)
project(libdataxp_test CXX)

include(CTest)
include(GoogleTest)
enable_testing()

add_executable(libdataxp_test
    libdataxp_test.h
    cppTest.cpp
    xpAirportReaderTest.cpp
    airportOpsTest.cpp
    xpFmsxReaderTest.cpp
    xpSceneryPacksIniReaderTest.cpp
    hydrationTest.cpp
)

set_property(TARGET libdataxp_test PROPERTY CXX_STANDARD 14)
target_include_directories(libdataxp_test PUBLIC ../libworld ../libdataxp ../libworld_test)
target_link_libraries(libdataxp_test libdataxp libworld GTest::GTest GTest::Main)

if (ATCBUILD_CAN_RUN_TESTS)
    gtest_discover_tests(
        libdataxp_test
        PROPERTIES WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )
endif()
