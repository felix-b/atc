cmake_minimum_required(VERSION 3.9)
project(libloaders_test CXX)

include(CTest)
include(GoogleTest)
enable_testing()

add_executable(libloaders_test
    libdataxp_test.h
    cppTest.cpp
    xpAirportReaderTest.cpp
    airportOpsTest.cpp
    xpFmsxReaderTest.cpp
    hydrationTest.cpp
)

set_property(TARGET libloaders_test PROPERTY CXX_STANDARD 14)
target_include_directories(libloaders_test PUBLIC ../libworld ../libloaders ../libworld_test)
target_link_libraries(libloaders_test libloaders libworld GTest::GTest GTest::Main)

if (ATCBUILD_CAN_RUN_TESTS)
    gtest_discover_tests(
        libloaders_test
        PROPERTIES WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )
endif()
