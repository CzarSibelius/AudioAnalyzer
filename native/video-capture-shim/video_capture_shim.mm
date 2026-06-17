#import "video_capture_shim.h"

#import <AVFoundation/AVFoundation.h>
#import <CoreMedia/CoreMedia.h>
#import <CoreVideo/CoreVideo.h>
#import <Foundation/Foundation.h>

#include <string.h>

namespace {

void SetError(char* error_out, size_t error_out_size, const char* message)
{
    if (error_out == nullptr || error_out_size == 0)
    {
        return;
    }

    strncpy(error_out, message, error_out_size - 1);
    error_out[error_out_size - 1] = '\0';
}

// Discovery device types in a stable order. video_capture_device_count/name and video_capture_start
// must agree on ordering so a settings index maps to the same physical camera.
NSArray<AVCaptureDeviceType>* DiscoveryDeviceTypes()
{
    NSMutableArray<AVCaptureDeviceType>* types = [NSMutableArray array];
    [types addObject:AVCaptureDeviceTypeBuiltInWideAngleCamera];
    if (@available(macOS 14.0, *))
    {
        [types addObject:AVCaptureDeviceTypeExternal];
        [types addObject:AVCaptureDeviceTypeContinuityCamera];
    }
    if (@available(macOS 13.0, *))
    {
        [types addObject:AVCaptureDeviceTypeDeskViewCamera];
    }

    return types;
}

NSArray<AVCaptureDevice*>* EnumerateDevices()
{
    AVCaptureDeviceDiscoverySession* session =
        [AVCaptureDeviceDiscoverySession discoverySessionWithDeviceTypes:DiscoveryDeviceTypes()
                                                              mediaType:AVMediaTypeVideo
                                                               position:AVCaptureDevicePositionUnspecified];
    return session.devices;
}

// Candidate presets from largest to smallest, with nominal pixel dimensions used for cap filtering.
struct PresetCandidate {
    AVCaptureSessionPreset preset;
    int width;
    int height;
};

NSString* ChooseSessionPreset(AVCaptureSession* session, AVCaptureDevice* device, int max_width, int max_height)
{
    const PresetCandidate candidates[] = {
        { AVCaptureSessionPreset1920x1080, 1920, 1080 },
        { AVCaptureSessionPreset1280x720, 1280, 720 },
        { AVCaptureSessionPreset640x480, 640, 480 },
        { AVCaptureSessionPreset352x288, 352, 288 },
        { AVCaptureSessionPresetLow, 192, 144 },
    };

    // No caps: prefer a modest resolution (terminal ASCII downsamples heavily) that the device supports.
    if (max_width <= 0 && max_height <= 0)
    {
        const AVCaptureSessionPreset defaults[] = {
            AVCaptureSessionPreset640x480,
            AVCaptureSessionPreset1280x720,
            AVCaptureSessionPresetHigh,
        };
        for (AVCaptureSessionPreset preset : defaults)
        {
            if ([device supportsAVCaptureSessionPreset:preset] && [session canSetSessionPreset:preset])
            {
                return preset;
            }
        }

        return AVCaptureSessionPresetHigh;
    }

    for (const PresetCandidate& candidate : candidates)
    {
        if (max_width > 0 && candidate.width > max_width)
        {
            continue;
        }

        if (max_height > 0 && candidate.height > max_height)
        {
            continue;
        }

        if ([device supportsAVCaptureSessionPreset:candidate.preset] &&
            [session canSetSessionPreset:candidate.preset])
        {
            return candidate.preset;
        }
    }

    // Caps smaller than the smallest preset (or unsupported): fall back to the lowest usable resolution.
    if ([device supportsAVCaptureSessionPreset:AVCaptureSessionPresetLow] &&
        [session canSetSessionPreset:AVCaptureSessionPresetLow])
    {
        return AVCaptureSessionPresetLow;
    }

    return AVCaptureSessionPresetHigh;
}

} // namespace

// AVCaptureVideoDataOutput delegate that forwards locked BGRA pixels to the managed callback.
@interface VideoCaptureDelegate : NSObject <AVCaptureVideoDataOutputSampleBufferDelegate>
@property (atomic, assign) video_capture_frame_callback callback;
@property (atomic, assign) void* userData;
@end

@implementation VideoCaptureDelegate

- (void)captureOutput:(AVCaptureOutput*)output
    didOutputSampleBuffer:(CMSampleBufferRef)sampleBuffer
           fromConnection:(AVCaptureConnection*)connection
{
    (void)output;
    (void)connection;

    video_capture_frame_callback callback = self.callback;
    if (callback == nullptr)
    {
        return;
    }

    CVImageBufferRef imageBuffer = CMSampleBufferGetImageBuffer(sampleBuffer);
    if (imageBuffer == nullptr)
    {
        return;
    }

    CVPixelBufferLockBaseAddress(imageBuffer, kCVPixelBufferLock_ReadOnly);
    const void* base = CVPixelBufferGetBaseAddress(imageBuffer);
    size_t width = CVPixelBufferGetWidth(imageBuffer);
    size_t height = CVPixelBufferGetHeight(imageBuffer);
    size_t bytes_per_row = CVPixelBufferGetBytesPerRow(imageBuffer);
    if (base != nullptr && width > 0 && height > 0)
    {
        callback(self.userData, base, static_cast<int>(width), static_cast<int>(height), static_cast<int>(bytes_per_row));
    }

    CVPixelBufferUnlockBaseAddress(imageBuffer, kCVPixelBufferLock_ReadOnly);
}

@end

namespace {

struct CaptureState {
    AVCaptureSession* session = nil;
    VideoCaptureDelegate* delegate = nil;
    dispatch_queue_t queue = nullptr;
    bool running = false;
};

CaptureState g_state;
NSObject* g_lock = nil;

NSObject* StateLock()
{
    static dispatch_once_t once;
    dispatch_once(&once, ^{
        g_lock = [[NSObject alloc] init];
    });
    return g_lock;
}

// Requests camera access, blocking briefly when the status is undetermined. Returns true if authorized.
bool EnsureCameraAuthorized(char* error_out, size_t error_out_size)
{
    AVAuthorizationStatus status = [AVCaptureDevice authorizationStatusForMediaType:AVMediaTypeVideo];
    if (status == AVAuthorizationStatusAuthorized)
    {
        return true;
    }

    if (status == AVAuthorizationStatusDenied || status == AVAuthorizationStatusRestricted)
    {
        SetError(error_out, error_out_size, "Camera access denied. Enable it in System Settings > Privacy & Security > Camera.");
        return false;
    }

    __block bool granted = false;
    dispatch_semaphore_t semaphore = dispatch_semaphore_create(0);
    [AVCaptureDevice requestAccessForMediaType:AVMediaTypeVideo completionHandler:^(BOOL allowed) {
        granted = allowed;
        dispatch_semaphore_signal(semaphore);
    }];

    // Wait up to 30s for the user to respond to the system prompt.
    if (dispatch_semaphore_wait(semaphore, dispatch_time(DISPATCH_TIME_NOW, (int64_t)(30 * NSEC_PER_SEC))) != 0)
    {
        SetError(error_out, error_out_size, "Timed out waiting for camera authorization.");
        return false;
    }

    if (!granted)
    {
        SetError(error_out, error_out_size, "Camera access was not granted.");
    }

    return granted;
}

void TearDownLocked()
{
    if (g_state.session != nil)
    {
        if (g_state.running)
        {
            [g_state.session stopRunning];
        }

        // stopRunning blocks until in-flight delegate callbacks finish; clear after so none fire late.
        if (g_state.delegate != nil)
        {
            g_state.delegate.callback = nullptr;
            g_state.delegate.userData = nullptr;
        }

        g_state.session = nil;
    }

    g_state.delegate = nil;
    g_state.queue = nullptr;
    g_state.running = false;
}

} // namespace

extern "C" {

int video_capture_is_supported(void)
{
    return 1;
}

int video_capture_device_count(void)
{
    @autoreleasepool
    {
        return static_cast<int>(EnumerateDevices().count);
    }
}

int video_capture_device_name(int index, char* name_out, size_t name_out_size)
{
    if (name_out == nullptr || name_out_size == 0 || index < 0)
    {
        return -1;
    }

    @autoreleasepool
    {
        NSArray<AVCaptureDevice*>* devices = EnumerateDevices();
        if (index >= static_cast<int>(devices.count))
        {
            return -1;
        }

        NSString* name = devices[static_cast<NSUInteger>(index)].localizedName;
        const char* utf8 = name.UTF8String;
        if (utf8 == nullptr)
        {
            return -1;
        }

        strncpy(name_out, utf8, name_out_size - 1);
        name_out[name_out_size - 1] = '\0';
        return 0;
    }
}

int video_capture_start(
    const video_capture_config* config,
    video_capture_frame_callback callback,
    void* user_data,
    char* error_out,
    size_t error_out_size)
{
    if (config == nullptr)
    {
        SetError(error_out, error_out_size, "config is null.");
        return -1;
    }

    if (callback == nullptr)
    {
        SetError(error_out, error_out_size, "callback is null.");
        return -1;
    }

    @autoreleasepool
    {
        if (!EnsureCameraAuthorized(error_out, error_out_size))
        {
            return -1;
        }

        @synchronized(StateLock())
        {
            if (g_state.running)
            {
                SetError(error_out, error_out_size, "video capture is already running.");
                return -1;
            }

            TearDownLocked();

            NSArray<AVCaptureDevice*>* devices = EnumerateDevices();
            if (devices.count == 0)
            {
                SetError(error_out, error_out_size, "No video capture devices found.");
                return -1;
            }

            int count = static_cast<int>(devices.count);
            int index = ((config->device_index % count) + count) % count;
            AVCaptureDevice* device = devices[static_cast<NSUInteger>(index)];

            AVCaptureSession* session = [[AVCaptureSession alloc] init];
            [session beginConfiguration];

            NSString* preset = ChooseSessionPreset(session, device, config->max_width, config->max_height);
            if ([session canSetSessionPreset:preset])
            {
                session.sessionPreset = preset;
            }

            NSError* inputError = nil;
            AVCaptureDeviceInput* input = [AVCaptureDeviceInput deviceInputWithDevice:device error:&inputError];
            if (input == nil || ![session canAddInput:input])
            {
                [session commitConfiguration];
                const char* reason = inputError != nil ? inputError.localizedDescription.UTF8String : "Could not open camera input.";
                SetError(error_out, error_out_size, reason != nullptr ? reason : "Could not open camera input.");
                return -1;
            }

            [session addInput:input];

            AVCaptureVideoDataOutput* output = [[AVCaptureVideoDataOutput alloc] init];
            output.videoSettings = @{ (id)kCVPixelBufferPixelFormatTypeKey : @(kCVPixelFormatType_32BGRA) };
            output.alwaysDiscardsLateVideoFrames = YES;

            VideoCaptureDelegate* delegate = [[VideoCaptureDelegate alloc] init];
            delegate.callback = callback;
            delegate.userData = user_data;

            dispatch_queue_t queue = dispatch_queue_create("dev.audioanalyzer.videocapture", DISPATCH_QUEUE_SERIAL);
            [output setSampleBufferDelegate:delegate queue:queue];

            if (![session canAddOutput:output])
            {
                [session commitConfiguration];
                SetError(error_out, error_out_size, "Could not add video output to session.");
                return -1;
            }

            [session addOutput:output];
            [session commitConfiguration];

            [session startRunning];

            g_state.session = session;
            g_state.delegate = delegate;
            g_state.queue = queue;
            g_state.running = true;
            return 0;
        }
    }
}

void video_capture_stop(void)
{
    @autoreleasepool
    {
        @synchronized(StateLock())
        {
            TearDownLocked();
        }
    }
}

int video_capture_is_running(void)
{
    @synchronized(StateLock())
    {
        return g_state.running ? 1 : 0;
    }
}

} // extern "C"
