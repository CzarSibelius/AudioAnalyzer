# Builds link_shim.dll (Windows) / liblink_shim.so against vendored Ableton Link.
cmake_minimum_required(VERSION 3.16)
project(LinkShim LANGUAGES CXX)

set(CMAKE_CXX_STANDARD 17)

set(LINK_ROOT "${CMAKE_CURRENT_SOURCE_DIR}/../third_party/link" CACHE PATH "Path to Ableton Link repository root")

if(NOT EXISTS "${LINK_ROOT}/include/ableton/Link.hpp")
  message(FATAL_ERROR "Ableton Link not found at LINK_ROOT=${LINK_ROOT}. Clone: git clone https://github.com/Ableton/link.git native/third_party/link")
endif()

list(APPEND CMAKE_MODULE_PATH "${LINK_ROOT}/cmake_include")
# Included config already runs ConfigureAbletonLink; a second call duplicates target Ableton::Link.
include("${LINK_ROOT}/AbletonLinkConfig.cmake")

add_library(link_shim SHARED link_shim.cpp)
target_link_libraries(link_shim PRIVATE Ableton::Link)
target_compile_definitions(link_shim PRIVATE LINK_SHIM_EXPORTS)

if(MSVC)
  target_compile_options(link_shim PRIVATE /W3 /EHsc)
endif()
