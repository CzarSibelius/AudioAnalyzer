cmake_minimum_required(VERSION 3.16)
project(video_capture_shim LANGUAGES CXX OBJCXX)

add_library(video_capture_shim SHARED video_capture_shim.mm)
target_compile_definitions(video_capture_shim PRIVATE VIDEO_CAPTURE_SHIM_EXPORTS)
target_compile_features(video_capture_shim PRIVATE cxx_std_17)
# The .mm source is written for ARC (no manual retain/release); match the documented clang++ build.
target_compile_options(video_capture_shim PRIVATE -fobjc-arc)
target_link_libraries(
  video_capture_shim
  PRIVATE
    "-framework AVFoundation"
    "-framework CoreMedia"
    "-framework CoreVideo"
    "-framework Foundation")
set_target_properties(video_capture_shim PROPERTIES
  CXX_VISIBILITY_PRESET hidden
  VISIBILITY_INLINES_HIDDEN ON
  MACOSX_RPATH ON)
