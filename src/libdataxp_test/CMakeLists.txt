cmake_minimum_required(VERSION 3.9)
project(libdataxp_test CXX)

message("GTEST_INCLUDE_DIR = ${GTEST_INCLUDE_DIR}")

set(GTEST_ROOT "${GTEST_LIBRARY_PATH}")
find_package(GTest REQUIRED)

include(CTest)
include(GoogleTest)
enable_testing()

add_executable(libdataxp_test 
    dummy_test.cpp
)

set_property(TARGET libdataxp_test PROPERTY CXX_STANDARD 14)
target_include_directories(libdataxp_test PUBLIC ../libworld ../libdataxp)
target_link_libraries(libdataxp_test libworld GTest::GTest GTest::Main)

message("CMAKE_CURRENT_SOURCE_DIR = ${CMAKE_CURRENT_SOURCE_DIR}")
gtest_discover_tests(
    libdataxp_test
    PROPERTIES WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
)


# cmake_minimum_required(VERSION 3.9)
# project(libdataxp_test CXX)
# 
# include(CTest)
# enable_testing()
# 
# set(BINARY libdataxp_test)
# file(GLOB_RECURSE TEST_SOURCES LIST_DIRECTORIES false *.h *.cpp)
# set(SOURCES ${TEST_SOURCES})
# 
# add_executable(${BINARY} ${TEST_SOURCES})
# add_test(NAME ${BINARY} COMMAND ${BINARY})
# 
# set_property(TARGET ${BINARY} PROPERTY CXX_STANDARD 14)
# target_include_directories(${BINARY} PUBLIC ../libworld)
# target_link_libraries(${BINARY} PUBLIC libworld gtest)
 
