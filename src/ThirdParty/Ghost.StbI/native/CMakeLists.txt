cmake_minimum_required(VERSION 3.20)
project(stbi LANGUAGES C)

set(STB_SOURCE_DIR "" CACHE PATH "Path to stb source directory containing stb_image.h and stb_image_write.h")
if(NOT EXISTS "${STB_SOURCE_DIR}/stb_image.h" OR NOT EXISTS "${STB_SOURCE_DIR}/stb_image_write.h")
    message(FATAL_ERROR "STB_SOURCE_DIR must contain stb_image.h and stb_image_write.h")
endif()

add_library(stbi SHARED
    stbi.c
)

set_target_properties(stbi PROPERTIES
    C_STANDARD 11
    C_STANDARD_REQUIRED ON
    OUTPUT_NAME "stbi"
)

target_include_directories(stbi PRIVATE "${STB_SOURCE_DIR}")

if(WIN32)
    target_compile_definitions(stbi PRIVATE
        "STBIDEF=__declspec(dllexport)"
        "STBIWDEF=__declspec(dllexport)"
    )
else()
    target_compile_definitions(stbi PRIVATE
        "STBIDEF=__attribute__((visibility(\"default\")))"
        "STBIWDEF=__attribute__((visibility(\"default\")))"
    )
endif()
