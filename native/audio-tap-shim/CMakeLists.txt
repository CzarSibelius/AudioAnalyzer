cmake_minimum_required(VERSION 3.16)
project(audio_tap_shim LANGUAGES CXX OBJCXX)

add_library(audio_tap_shim SHARED audio_tap_shim.mm)
target_compile_definitions(audio_tap_shim PRIVATE AUDIO_TAP_SHIM_EXPORTS)
target_compile_features(audio_tap_shim PRIVATE cxx_std_17)
# The .mm source is written for ARC (no manual retain/release); match the documented clang++ build.
target_compile_options(audio_tap_shim PRIVATE -fobjc-arc)
target_link_libraries(
  audio_tap_shim
  PRIVATE
    "-framework CoreAudio"
    "-framework AudioToolbox"
    "-framework Foundation")
set_target_properties(audio_tap_shim PROPERTIES
  CXX_VISIBILITY_PRESET hidden
  VISIBILITY_INLINES_HIDDEN ON
  MACOSX_RPATH ON)
