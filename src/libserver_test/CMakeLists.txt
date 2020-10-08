cmake_minimum_required(VERSION 3.9)
project(libserver_test CXX)

include(CTest)
include(GoogleTest)
enable_testing()

if(WIN32)
    find_file(WS2_DLL NAMES ws2_32.dll PATHS "C:\\Windows\\System32")
    find_file(MSWSOCK_DLL NAMES mswsock.dll PATHS "C:\\Windows\\System32")
endif()

add_executable(libserver_test
    e2eTest.cpp
    testClient.hpp
)

set_property(TARGET libserver_test PROPERTY CXX_STANDARD 14)

target_compile_definitions (libserver_test PUBLIC
    _WEBSOCKETPP_CPP11_THREAD_
    ASIO_STANDALONE
)

target_include_directories(libserver_test PUBLIC
    ../libserver
    ../libserver/proto
    ../libworld
    ../libworld_test
    ${ATC_LIBS_PATH}/websocketpp
    ${ATC_LIBS_PATH}/asio/asio/include
    ${ATC_LIBS_PATH}/concurrentqueue
    ${ATC_LIBS_PATH}/protobuf/include
    ${ATC_LIBS_PATH}/googletest/googletest/include
)

target_link_libraries(libserver_test
    libserver
    libworld
    GTest::GTest
    GTest::Main
    ${PROTOBUF_LIBRARY}
    ${WS2_DLL}
    ${MSWSOCK_DLL}
)

if (ATCBUILD_CAN_RUN_TESTS)
    gtest_discover_tests(
        libserver_test
        PROPERTIES WORKING_DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR}
    )
endif()
