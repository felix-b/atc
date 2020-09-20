cmake_minimum_required(VERSION 3.9)

include(CTest)
enable_testing()

set(GTEST_ROOT "${GTEST_LIBRARY_PATH}")
find_package(GTest REQUIRED)

add_subdirectory(libworld)
add_subdirectory(libworld_test)
add_subdirectory(libdataxp)
add_subdirectory(libdataxp_test)
add_subdirectory(libai)
add_subdirectory(libai_test)
add_subdirectory(pluginxp)
