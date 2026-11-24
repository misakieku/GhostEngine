cmake_minimum_required(VERSION 3.20)
project(d3d12ma LANGUAGES CXX)

set(D3D12_INCLUDE_DIR "" CACHE PATH "Path to Microsoft.Direct3D.D3D12 build/native/include")
if(NOT EXISTS "${D3D12_INCLUDE_DIR}/d3d12.h")
    message(FATAL_ERROR "D3D12_INCLUDE_DIR must point to a directory containing d3d12.h")
endif()

add_library(d3d12ma SHARED
    D3D12MemAlloc.cpp
    D3D12MemAlloc.h
    d3d12ma_c.cpp
    d3d12ma_c.h
)

set_target_properties(d3d12ma PROPERTIES
    CXX_STANDARD 17
    CXX_STANDARD_REQUIRED ON
    CXX_EXTENSIONS OFF
    OUTPUT_NAME "d3d12ma"
)

target_include_directories(d3d12ma BEFORE PRIVATE "${D3D12_INCLUDE_DIR}")
target_include_directories(d3d12ma PRIVATE "${CMAKE_CURRENT_SOURCE_DIR}")

target_compile_definitions(d3d12ma PRIVATE
    D3D12MA_USE_AGILITY_SDK=1
    WIN32_LEAN_AND_MEAN
    NOMINMAX
    WINVER=0x0A00
    _WIN32_WINNT=0x0A00
)

target_link_libraries(d3d12ma PRIVATE
    d3d12.lib
    dxgi.lib
    dxguid.lib
)
