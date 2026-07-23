#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32)
#if defined(AUDIO_TAP_SHIM_EXPORTS)
#define AUDIO_TAP_SHIM_API __declspec(dllexport)
#else
#define AUDIO_TAP_SHIM_API __declspec(dllimport)
#endif
#else
#define AUDIO_TAP_SHIM_API __attribute__((visibility("default")))
#endif

typedef struct audio_tap_config {
    int capture_all_processes;
    const int* process_ids;
    int process_id_count;
    int mono;
    int sample_rate;
    const char* device_uid;
    int stream_index;
} audio_tap_config;

typedef struct audio_tap_format {
    double sample_rate;
    uint32_t channels;
    uint32_t bits_per_sample;
    uint32_t is_float;
} audio_tap_format;

typedef void (*audio_tap_pcm_callback)(
    void* user_data,
    const void* buffer,
    uint32_t byte_count,
    const audio_tap_format* format);

AUDIO_TAP_SHIM_API int audio_tap_is_supported(void);

AUDIO_TAP_SHIM_API int audio_tap_start(
    const audio_tap_config* config,
    audio_tap_pcm_callback callback,
    void* user_data,
    char* error_out,
    size_t error_out_size);

AUDIO_TAP_SHIM_API void audio_tap_stop(void);

AUDIO_TAP_SHIM_API int audio_tap_is_running(void);

// Non-prompting System Audio Recording (TCC) status. Uses TCCAccessPreflight only; never calls
// TCCAccessRequest, so it cannot raise a consent prompt. Returns 1 when authorized, 0 when not
// authorized, -1 when the status cannot be determined (TCC private API unavailable).
AUDIO_TAP_SHIM_API int audio_tap_permission_status(void);

#ifdef __cplusplus
}
#endif
