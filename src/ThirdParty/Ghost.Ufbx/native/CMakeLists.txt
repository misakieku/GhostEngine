cmake_minimum_required(VERSION 3.20)
project(ufbx LANGUAGES C)

set(UFBX_SOURCE_DIR "" CACHE PATH "Path to ufbx source directory containing ufbx.h and ufbx.c")
if(NOT EXISTS "${UFBX_SOURCE_DIR}/ufbx.c")
    message(FATAL_ERROR "UFBX_SOURCE_DIR must contain ufbx.c")
endif()

add_library(ufbx SHARED
    "${UFBX_SOURCE_DIR}/ufbx.c"
    "${UFBX_SOURCE_DIR}/ufbx.h"
)

set_target_properties(ufbx PROPERTIES
    C_STANDARD 11
    C_STANDARD_REQUIRED ON
    OUTPUT_NAME "ufbx"
)

target_include_directories(ufbx PRIVATE "${UFBX_SOURCE_DIR}")

if(WIN32)
    target_compile_definitions(ufbx PRIVATE "ufbx_abi=__declspec(dllexport)" UFBX_REAL_IS_FLOAT)
else()
    target_compile_definitions(ufbx PRIVATE "ufbx_abi=__attribute__((visibility(\"default\")))" UFBX_REAL_IS_FLOAT)
endif()
