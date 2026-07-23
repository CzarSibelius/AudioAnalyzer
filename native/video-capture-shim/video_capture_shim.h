#pragma once

#include <stddef.h>
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#if defined(_WIN32)
#if defined(VIDEO_CAPTURE_SHIM_EXPORTS)
#define VIDEO_CAPTURE_SHIM_API __declspec(dllexport)
#else
#define VIDEO_CAPTURE_SHIM_API __declspec(dllimport)
#endif
#else
#define VIDEO_CAPTURE_SHIM_API __attribute__((visibility("default")))
#endif

typedef struct video_capture_config {
    // Zero-based index into the device list reported by video_capture_device_count/name.
    int device_index;
    // Optional caps; 0 means "no cap on this axis". The shim chooses the largest session
    // preset whose width/height does not exceed any non-zero cap (640x480 when both are 0).
    int max_width;
    int max_height;
} video_capture_config;

// Called on the AVFoundation sample-buffer dispatch queue (off the render thread) for each
// captured frame. Pixels are 32-bit BGRA. Rows may be padded: honor bytes_per_row (>= width*4).
typedef void (*video_capture_frame_callback)(
    void* user_data,
    const void* bgra_base,
    int width,
    int height,
    int bytes_per_row);

// 1 when AVFoundation video capture is usable on this host, otherwise 0.
VIDEO_CAPTURE_SHIM_API int video_capture_is_supported(void);

// Number of enumerated video capture devices (built-in, external, Continuity, Desk View).
VIDEO_CAPTURE_SHIM_API int video_capture_device_count(void);

// Writes the localized name of the device at index into name_out (UTF-8, null-terminated).
// Returns 0 on success, -1 when index is out of range or arguments are invalid.
VIDEO_CAPTURE_SHIM_API int video_capture_device_name(int index, char* name_out, size_t name_out_size);

// Starts a capture session. Returns 0 on success; on failure returns non-zero and writes a
// message into error_out. Camera authorization is requested if not yet determined.
VIDEO_CAPTURE_SHIM_API int video_capture_start(
    const video_capture_config* config,
    video_capture_frame_callback callback,
    void* user_data,
    char* error_out,
    size_t error_out_size);

VIDEO_CAPTURE_SHIM_API void video_capture_stop(void);

VIDEO_CAPTURE_SHIM_API int video_capture_is_running(void);

// Non-prompting AVFoundation authorization status. Uses authorizationStatusForMediaType only; never
// calls requestAccessForMediaType, so it cannot raise a consent prompt. media_is_audio selects
// Microphone (1, AVMediaTypeAudio) vs Camera (0, AVMediaTypeVideo). Returns the AVAuthorizationStatus
// value: 0 NotDetermined, 1 Restricted, 2 Denied, 3 Authorized; -1 on error.
VIDEO_CAPTURE_SHIM_API int video_capture_authorization_status(int media_is_audio);

#ifdef __cplusplus
}
#endif
