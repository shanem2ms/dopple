cmake_minimum_required(VERSION 3.15)
project(ptslib LANGUAGES CXX)

# Set the library name
set(LIB_NAME ptslib)

# Collect source files
set(SOURCES
    Alignment.cpp
    Depth.cpp
    OctTree.cpp
    OctTreeLoc.cpp
    Planes.cpp
)

# Add the shared library
add_library(${LIB_NAME} STATIC ${SOURCES})

# Set C++ standard
target_compile_features(${LIB_NAME} PUBLIC cxx_std_17)

# Include directories (if needed)
target_include_directories(${LIB_NAME} PRIVATE ${CMAKE_CURRENT_SOURCE_DIR})

# Platform-specific settings
if(WIN32)
    target_compile_definitions(${LIB_NAME} PRIVATE -DWIN32_LEAN_AND_MEAN -DNOMINMAX)
    target_link_libraries(${LIB_NAME} PRIVATE ws2_32) # Example: link Windows libraries if necessary
endif()

# Export symbols for Windows DLL
if(WIN32)
    target_compile_definitions(${LIB_NAME} PRIVATE -DMYLIBRARY_EXPORTS)
endif()
