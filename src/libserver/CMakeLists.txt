cmake_minimum_required(VERSION 3.9.0)
project(libserver VERSION 0.1.0)

if(WIN32)
    find_file(WS2_DLL NAMES ws2_32.dll PATHS "C:\\Windows\\System32")
    find_file(MSWSOCK_DLL NAMES mswsock.dll PATHS "C:\\Windows\\System32")
    set(PROTOC_PATH "${ATC_LIBS_PATH}/protobuf/win/bin/protoc")
endif()

set(PROTOC_SRC_DIR "${CMAKE_CURRENT_SOURCE_DIR}/proto")

add_custom_command(
    DEPENDS ${PROTOC_SRC_DIR}/world.proto
    OUTPUT ${PROTOC_SRC_DIR}/world.pb.cc
    COMMAND ${PROTOC_PATH} --cpp_out=${PROTOC_SRC_DIR} -I${PROTOC_SRC_DIR} -I${ATC_LIBS_PATH}/protobuf/include ${PROTOC_SRC_DIR}/world.proto
)

add_custom_target(world_proto DEPENDS ${PROTOC_SRC_DIR}/world.pb.cc)

add_library(libserver STATIC
    libserver.hpp
    serverController.cpp
    interfaces.hpp
    dispatcher.hpp
    server.hpp
    worldService.hpp
    protocolConverter.hpp
    proto/world.pb.cc
    proto/world.pb.h
    worldServiceDispatchMiddleware.hpp
)

set_property(TARGET libserver PROPERTY CXX_STANDARD 14)

target_include_directories(libserver PUBLIC
    proto
    ../libworld
    ../libdataxp
    ${ATC_LIBS_PATH}/websocketpp
    ${ATC_LIBS_PATH}/asio/asio/include
    ${ATC_LIBS_PATH}/concurrentqueue
    ${ATC_LIBS_PATH}/protobuf/include
)

add_dependencies(libserver world_proto)

target_link_libraries (libserver LINK_PUBLIC libworld ${PROTOBUF_LIBRARY})

if (WIN32)
    target_compile_definitions (libserver PUBLIC _WEBSOCKETPP_CPP11_THREAD_)
    target_link_libraries (libserver LINK_PUBLIC ${WS2_DLL} ${MSWSOCK_DLL})
endif()
