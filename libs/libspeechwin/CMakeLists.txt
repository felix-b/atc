cmake_minimum_required(VERSION 3.12.0)
project(libspeechwin CXX)

set(CMAKE_WINDOWS_EXPORT_ALL_SYMBOLS 1)
set(BUILD_SHARED_LIBS 1)

find_library(SAPI_LIB SAPI.lib PATHS "${CMAKE_CURRENT_SOURCE_DIR}\\sdk" NO_CMAKE_SYSTEM_PATH NO_SYSTEM_ENVIRONMENT_PATH)
message ("SAPI_LIB" = ${SAPI_LIB})
add_library(sapi STATIC IMPORTED)
set_target_properties(sapi PROPERTIES IMPORTED_LOCATION "${SAPI_LIB}")

add_library(libspeechwin MODULE 
    dllmain.cpp
    framework.h
    libspeechwin.cpp
    pch.cpp
    pch.h
)

target_compile_definitions(libspeechwin PUBLIC -D_CRT_SECURE_NO_WARNINGS -DIBM=1 /bigobj)
target_include_directories(libspeechwin PUBLIC "${CMAKE_SOURCE_DIR}\\..\\..\\src\\include")
target_link_libraries(libspeechwin sapi)
set_property(TARGET libspeechwin PROPERTY CXX_STANDARD 14)
set_property(TARGET libspeechwin PROPERTY MSVC_RUNTIME_LIBRARY "MultiThreaded")

add_custom_command(TARGET libspeechwin POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}\\build\\Debug\\libspeechwin.dll ${CMAKE_CURRENT_SOURCE_DIR}\\..\\..\\build\\lib\\
)
add_custom_command(TARGET libspeechwin POST_BUILD
    COMMAND ${CMAKE_COMMAND} -E copy ${CMAKE_CURRENT_SOURCE_DIR}\\build\\Debug\\libspeechwin.lib ${CMAKE_CURRENT_SOURCE_DIR}\\..\\..\\build\\lib\\
)