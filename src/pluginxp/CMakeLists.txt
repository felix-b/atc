cmake_minimum_required(VERSION 3.9)
project(atc VERSION 0.1.0 DESCRIPTION "AT&C X-Plane plugin")
cmake_policy(SET CMP0079 NEW)

# Set up the imported targets for X-Plane plugin libraries:

IF(APPLE)
    SET(XPLANE_LIBRARY_PATH "${ATC_LIBS_PATH}/XPSDK/Libraries/Mac/")
ELSEIF(WIN32)
    SET(XPLANE_LIBRARY_PATH "${ATC_LIBS_PATH}/XPSDK/Libraries/Win/")
ENDIF()

SET(XPLANE_INCLUDES_PATH "${ATC_LIBS_PATH}/XPSDK/CHeaders/" CACHE STRING PATH)

IF(APPLE)
	FIND_LIBRARY(XPLM_LIBRARY XPLM "${XPLANE_LIBRARY_PATH}")
	FIND_LIBRARY(XPWIDGETS_LIBRARY XPWidgets "${XPLANE_LIBRARY_PATH}")
	add_library(xplm STATIC IMPORTED GLOBAL)
	add_library(xpwidgets STATIC IMPORTED GLOBAL)
	SET_PROPERTY(TARGET xplm PROPERTY IMPORTED_LOCATION "${XPLM_LIBRARY}")
	SET_PROPERTY(TARGET xpwidgets PROPERTY IMPORTED_LOCATION "${XPWIDGETS_LIBRARY}")
ELSEIF(WIN32)
	FIND_LIBRARY(XPLM_LIBRARY XPLM_64 "${XPLANE_LIBRARY_PATH}")
	FIND_LIBRARY(XPWIDGETS_LIBRARY XPWidgets_64 "${XPLANE_LIBRARY_PATH}")
	add_library(xplm SHARED IMPORTED GLOBAL)
	add_library(xpwidgets SHARED IMPORTED GLOBAL)
	SET_PROPERTY(TARGET xplm PROPERTY IMPORTED_IMPLIB "${XPLM_LIBRARY}")
	SET_PROPERTY(TARGET xpwidgets PROPERTY IMPORTED_IMPLIB "${XPWIDGETS_LIBRARY}")
ELSEIF(UNIX)
	message(STATUS linux target made)
	add_library(xplm INTERFACE)
	add_library(xpwidgets INTERFACE)
ENDIF()

message("XPLM_LIBRARY" = ${XPLM_LIBRARY})
message("XPWIDGETS_LIBRARY" = ${XPWIDGETS_LIBRARY})

#SET(XPLM_LIBRARY "${XPLM_LIBRARY}" PARENT_SCOPE)
#SET(XPWIDGETS_LIBRARY "${XPWIDGETS_LIBRARY}" PARENT_SCOPE)

# For some reason this doesn't work. It's not worth wasting more time on fixing it, so
# I'll manually include the directories below
#SET_PROPERTY(TARGET xplm APPEND PROPERTY INTERFACE_INCLUDE_DIRECTORIES "${ATC_LIBS_PATH}/XPSDK/CHeaders/XPLM" "${ATC_LIBS_PATH}/XPSDK/CHeaders/Wrappers")
#SET_PROPERTY(TARGET xpwidgets APPEND PROPERTY INTERFACE_INCLUDE_DIRECTORIES "${ATC_LIBS_PATH}/XPSDK/CHeaders/XPLM" "${ATC_LIBS_PATH}/XPSDK/CHeaders/Widgets" "${ATC_LIBS_PATH}/XPSDK/CHeaders/Wrappers")

FUNCTION(add_xplane_sdk_definitions library_name library_version) 
    IF(APPLE)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DAPL=1 -DIBM=0 -DLIN=0)
    ELSEIF(UNIX)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DAPL=0 -DIBM=0 -DLIN=1)
    ELSEIF(WIN32)
        # TARGET_COMPILE_OPTIONS(${library_name} PRIVATE "/MD$<$<CONFIG:Debug>:d>")
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DAPL=0 -DIBM=1 -DLIN=0)
    ENDIF()

    IF(library_version EQUAL 200)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DXPLM200=1)
    ELSEIF(library_version EQUAL 210)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DXPLM200=1 -DXPLM210=1)
    ELSEIF(library_version EQUAL 300)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DXPLM200=1 -DXPLM210=1 -DXPLM300=1)
    ELSEIF(library_version EQUAL 301)
        TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -DXPLM200=1 -DXPLM210=1 -DXPLM300=1 -DXPLM301=1)
    ELSE()
        MESSAGE(FATAL_ERROR "Library version one of: 200, 210, 300, 301")
    ENDIF()
    TARGET_INCLUDE_DIRECTORIES(${library_name} PUBLIC "${XPLANE_INCLUDES_PATH}/XPLM" "${XPLANE_INCLUDES_PATH}/Widgets" "${XPLANE_INCLUDES_PATH}/Wrappers")
ENDFUNCTION(add_xplane_sdk_definitions)

# Set up an X-Plane plugin
FUNCTION(add_xplane_plugin library_name library_version ...)
    SET(FILES ${ARGV})
    LIST(REMOVE_AT FILES 0)
    LIST(REMOVE_AT FILES 0)
    MESSAGE(STATUS "Making X-Plane plugin called ${library_name}")

    IF(APPLE)
        ADD_EXECUTABLE(${library_name} ${FILES})
    ELSEIF(UNIX)
        ADD_LIBRARY(${library_name} SHARED ${FILES})
    ELSEIF(WIN32)
        ADD_LIBRARY(${library_name} MODULE ${FILES})
        # TARGET_COMPILE_DEFINITIONS(${library_name} PUBLIC -D_CRT_SECURE_NO_WARNINGS /bigobj)
    ENDIF()

    IF(APPLE)
        SET_TARGET_PROPERTIES(${library_name} PROPERTIES OUTPUT_NAME "mac.xpl")
    ELSEIF(UNIX)
        SET_TARGET_PROPERTIES(${library_name} PROPERTIES OUTPUT_NAME "lin.xpl")
    ELSEIF(WIN32)
        SET_TARGET_PROPERTIES(${library_name} PROPERTIES OUTPUT_NAME "win.xpl")
    ENDIF()

    add_xplane_sdk_definitions(${library_name} ${library_version})

    SET_TARGET_PROPERTIES(${library_name} PROPERTIES PREFIX "")
    SET_TARGET_PROPERTIES(${library_name} PROPERTIES SUFFIX "")

    #link flags
    IF(APPLE)
        SET_PROPERTY(TARGET ${library_name} APPEND_STRING PROPERTY LINK_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -fPIC -fvisibility=hidden -bundle")
    ELSEIF(UNIX)
        SET_PROPERTY(TARGET ${library_name} APPEND_STRING PROPERTY LINK_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -rdynamic -nodefaultlibs -undefined_warning -fPIC -fvisibility=hidden")
    ENDIF()

	TARGET_LINK_LIBRARIES(${library_name} PUBLIC ${XPLM_LIBRARY} ${XPWIDGETS_LIBRARY})
    TARGET_INCLUDE_DIRECTORIES(${library_name} PUBLIC "${XPLANE_INCLUDES_PATH}/XPLM" "${XPLANE_INCLUDES_PATH}/Widgets" "${XPLANE_INCLUDES_PATH}/Wrappers")
ENDFUNCTION(add_xplane_plugin)

add_xplane_plugin(pluginxp 301
    nativeTextToSpeechService.hpp
    xplmSpeakStringTtsService.hpp
    xpmp2AircraftObjectService.hpp
    pluginHostServices.hpp
    pluginMenu.hpp
    pluginInstance.hpp
    entryPoint.cpp
    pluginWorldLoader.hpp
    demoScheduleLoader.hpp
    speechSoundBuffer.hpp
        libdataxp_test.cpp
    utils.h
    configuration.hpp
    userPilotAssistantWorkflow.hpp
    userAircraft.hpp
    userPilot.hpp
    transcriptInterface.hpp
)

set_property(TARGET pluginxp PROPERTY CXX_STANDARD 14)

target_include_directories(pluginxp PUBLIC 
    ${ATC_LIBS_PATH}/PPL/src
    ${ATC_LIBS_PATH}/XPMP2/inc
    ${ATC_LIBS_PATH}/concurrentqueue
    ${ATC_LIBS_PATH}/PPL/include/openal-soft/include
    ../include
    ../libworld
    ../libloaders
    ../libai
    ../libserver
)

target_link_libraries(libXPMP2 PUBLIC INTERFACE ${XPLM_LIBRARY} ${XPWIDGETS_LIBRARY})

if (APPLE)
    find_library(IOKIT_LIBRARY IOKit)
    find_library(CORE_FOUNDATION_LIBRARY CoreFoundation)
    find_library(COCOA_LIBRARY Cocoa)

    target_link_libraries(pluginxp PUBLIC
        ${PPL_LIBRARY}
        ${XPMP2_LIBRARY}
        libworld
        libloaders
        libai
        ${SPEECH_LIBRARY}
        ${OPENAL_LIBRARY}
        ${IOKIT_LIBRARY}
        ${CORE_FOUNDATION_LIBRARY} 
        ${COCOA_LIBRARY} 
        ${OpenGL_LIBRARY} 
    )
else()
    target_link_libraries(pluginxp PUBLIC 
        ${PPL_LIBRARY}
        libXPMP2 #${XPMP2_LIBRARY}
        libworld
        libloaders
        libai
        ${SPEECH_LIBRARY}
        ${OPENAL_LIBRARY}
    )
endif ()

if (WIN32)
    target_link_libraries(pluginxp PUBLIC libserver)
endif ()
