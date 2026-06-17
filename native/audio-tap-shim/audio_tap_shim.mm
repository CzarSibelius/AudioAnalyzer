#import "audio_tap_shim.h"

#import <AudioToolbox/AudioToolbox.h>
#import <CoreAudio/AudioHardware.h>
#import <CoreAudio/CATapDescription.h>
#import <CoreAudio/AudioHardwareTapping.h>
#import <CoreFoundation/CoreFoundation.h>
#import <Foundation/Foundation.h>

#include <dlfcn.h>
#include <pthread.h>
#include <string.h>
#include <unistd.h>
#include <vector>

namespace {

constexpr UInt32 kNoErr = 0;
constexpr AudioObjectID kAudioObjectSystemObject = 1;

struct TapState {
    pthread_mutex_t mutex = PTHREAD_MUTEX_INITIALIZER;
    bool running = false;
    AudioObjectID tap_id = kAudioObjectUnknown;
    AudioObjectID aggregate_id = kAudioObjectUnknown;
    AudioDeviceIOProcID io_proc_id = nullptr;
    AudioStreamBasicDescription asbd {};
    audio_tap_format delivery_format {};
    audio_tap_pcm_callback callback = nullptr;
    void* user_data = nullptr;
};

TapState g_state;

void SetError(char* error_out, size_t error_out_size, const char* message)
{
    if (error_out == nullptr || error_out_size == 0)
    {
        return;
    }

    strncpy(error_out, message, error_out_size - 1);
    error_out[error_out_size - 1] = '\0';
}

bool IsMacOs142OrLater()
{
    if (@available(macOS 14.2, *))
    {
        return true;
    }

    return false;
}

// Core Audio process taps have no public authorization API. Consent is only implicit (AudioDeviceStart
// is meant to trigger the System Audio Recording prompt), and Core Audio returns noErr even when access
// is denied, so a console/TUI host gets silently denied. Mirror the camera path
// (AVCaptureDevice requestAccessForMediaType) by explicitly requesting the TCC service via the private
// TCC API, blocking briefly for the user's response. If the private symbols are unavailable, fall back
// to the implicit consent path (return true so AudioDeviceStart still attempts it).
//
// Private TCC API signatures (TCC.framework):
//   int  TCCAccessPreflight(CFStringRef service, CFDictionaryRef options);  // 0 == authorized
//   void TCCAccessRequest(CFStringRef service, CFDictionaryRef options, void(^)(Boolean granted));
bool EnsureSystemAudioRecordingAuthorized(char* error_out, size_t error_out_size)
{
    using PreflightFn = int (*)(CFStringRef, CFDictionaryRef);
    using RequestFn = void (*)(CFStringRef, CFDictionaryRef, void (^)(Boolean));

    static CFStringRef const kAudioCaptureService = CFSTR("kTCCServiceAudioCapture");

    void* tcc = dlopen("/System/Library/PrivateFrameworks/TCC.framework/TCC", RTLD_LAZY);
    if (tcc == nullptr)
    {
        return true;
    }

    auto preflight = reinterpret_cast<PreflightFn>(dlsym(tcc, "TCCAccessPreflight"));
    auto request = reinterpret_cast<RequestFn>(dlsym(tcc, "TCCAccessRequest"));

    if (preflight != nullptr && preflight(kAudioCaptureService, nullptr) == 0)
    {
        return true;
    }

    if (request == nullptr)
    {
        return true;
    }

    __block bool granted = false;
    dispatch_semaphore_t semaphore = dispatch_semaphore_create(0);
    request(kAudioCaptureService, nullptr, ^(Boolean allowed) {
        granted = allowed != 0;
        dispatch_semaphore_signal(semaphore);
    });

    // Wait up to 30s for the user to respond to the system prompt.
    if (dispatch_semaphore_wait(semaphore, dispatch_time(DISPATCH_TIME_NOW, (int64_t)(30 * NSEC_PER_SEC))) != 0)
    {
        SetError(error_out, error_out_size, "Timed out waiting for System Audio Recording authorization.");
        return false;
    }

    if (!granted)
    {
        SetError(
            error_out,
            error_out_size,
            "System Audio Recording access was not granted. Enable AudioAnalyzer in "
            "System Settings > Privacy & Security > Screen & System Audio Recording.");
    }

    return granted;
}

AudioObjectPropertyAddress PropertyAddress(AudioObjectPropertySelector selector)
{
    AudioObjectPropertyAddress address {};
    address.mSelector = selector;
    address.mScope = kAudioObjectPropertyScopeGlobal;
    address.mElement = kAudioObjectPropertyElementMain;
    return address;
}

bool TranslatePidToProcessObject(pid_t pid, AudioObjectID* out_process_id)
{
    AudioObjectPropertyAddress address =
        PropertyAddress(kAudioHardwarePropertyTranslatePIDToProcessObject);
    UInt32 size = sizeof(AudioObjectID);
    OSStatus status = AudioObjectGetPropertyData(
        kAudioObjectSystemObject,
        &address,
        sizeof(pid),
        &pid,
        &size,
        out_process_id);
    return status == kNoErr && *out_process_id != kAudioObjectUnknown;
}

NSArray<NSNumber*>* ProcessObjectIdsFromPids(const int* process_ids, int count, char* error_out, size_t error_out_size)
{
    NSMutableArray<NSNumber*>* objects = [NSMutableArray arrayWithCapacity:count];
    for (int i = 0; i < count; ++i)
    {
        pid_t pid = static_cast<pid_t>(process_ids[i]);
        AudioObjectID process_object = kAudioObjectUnknown;
        if (!TranslatePidToProcessObject(pid, &process_object))
        {
            SetError(error_out, error_out_size, "Could not translate process id to Core Audio process object.");
            return nil;
        }

        [objects addObject:@(process_object)];
    }

    return objects;
}

CATapDescription* BuildTapDescription(const audio_tap_config* config, char* error_out, size_t error_out_size)
{
    if (config == nullptr)
    {
        SetError(error_out, error_out_size, "config is null.");
        return nil;
    }

    CATapDescription* description = nil;
    if (config->capture_all_processes)
    {
        if (config->mono)
        {
            description = [[CATapDescription alloc] initMonoGlobalTapButExcludeProcesses:@[]];
        }
        else
        {
            description = [[CATapDescription alloc] initStereoGlobalTapButExcludeProcesses:@[]];
        }
    }
    else if (config->process_ids != nullptr && config->process_id_count > 0)
    {
        NSArray<NSNumber*>* processes = ProcessObjectIdsFromPids(
            config->process_ids,
            config->process_id_count,
            error_out,
            error_out_size);
        if (processes == nil)
        {
            return nil;
        }

        if (config->mono)
        {
            description = [[CATapDescription alloc] initMonoMixdownOfProcesses:processes];
        }
        else
        {
            description = [[CATapDescription alloc] initStereoMixdownOfProcesses:processes];
        }
    }
    else
    {
        SetError(error_out, error_out_size, "No capture target: set capture_all_processes or process_ids.");
        return nil;
    }

    description.name = @"AudioAnalyzer System Audio Tap";
    description.muteBehavior = CATapUnmuted;
    description.privateTap = NO;

    if (config->device_uid != nullptr && config->device_uid[0] != '\0')
    {
        NSString* uid = [NSString stringWithUTF8String:config->device_uid];
        description.deviceUID = uid;
        description.stream = @(config->stream_index);
    }

    return description;
}

bool CopyTapUid(AudioObjectID tap_id, CFStringRef* out_uid)
{
    AudioObjectPropertyAddress address = PropertyAddress(kAudioTapPropertyUID);
    UInt32 size = sizeof(CFStringRef);
    OSStatus status = AudioObjectGetPropertyData(tap_id, &address, 0, nullptr, &size, out_uid);
    return status == kNoErr && *out_uid != nullptr;
}

// Returns a +1 CFStringRef UID of the current default output device (caller releases), or nullptr.
// A tap-backed aggregate needs a real audio sub-device as its main/clock device; without one the
// aggregate has no IO cycle and the IOProc never fires (AudioDeviceStart still returns noErr).
CFStringRef CopyDefaultOutputDeviceUid()
{
    AudioObjectPropertyAddress device_address = PropertyAddress(kAudioHardwarePropertyDefaultOutputDevice);
    AudioObjectID output_device = kAudioObjectUnknown;
    UInt32 size = sizeof(output_device);
    OSStatus status = AudioObjectGetPropertyData(
        kAudioObjectSystemObject,
        &device_address,
        0,
        nullptr,
        &size,
        &output_device);
    if (status != kNoErr || output_device == kAudioObjectUnknown)
    {
        return nullptr;
    }

    AudioObjectPropertyAddress uid_address = PropertyAddress(kAudioDevicePropertyDeviceUID);
    CFStringRef uid = nullptr;
    size = sizeof(uid);
    status = AudioObjectGetPropertyData(output_device, &uid_address, 0, nullptr, &size, &uid);
    if (status != kNoErr)
    {
        return nullptr;
    }

    return uid;
}

bool ReadDeviceInputStreamFormat(AudioObjectID device_id, AudioStreamBasicDescription* out_asbd)
{
    AudioObjectPropertyAddress address = PropertyAddress(kAudioDevicePropertyStreamFormat);
    address.mScope = kAudioObjectPropertyScopeInput;
    UInt32 size = sizeof(AudioStreamBasicDescription);
    OSStatus status = AudioObjectGetPropertyData(device_id, &address, 0, nullptr, &size, out_asbd);
    return status == kNoErr;
}

bool TryReadVirtualOrPhysicalStreamFormat(AudioStreamID stream_id, AudioStreamBasicDescription* out_asbd)
{
    constexpr AudioObjectPropertySelector kSelectors[] = {
        kAudioStreamPropertyVirtualFormat,
        kAudioStreamPropertyPhysicalFormat,
    };
    for (AudioObjectPropertySelector selector : kSelectors)
    {
        AudioObjectPropertyAddress address = PropertyAddress(selector);
        address.mScope = kAudioObjectPropertyScopeGlobal;
        UInt32 size = sizeof(AudioStreamBasicDescription);
        OSStatus status = AudioObjectGetPropertyData(stream_id, &address, 0, nullptr, &size, out_asbd);
        if (status == kNoErr)
        {
            return true;
        }
    }

    return false;
}

bool TryReadTapStreamFormat(AudioObjectID tap_id, AudioStreamBasicDescription* out_asbd)
{
    if (tap_id == kAudioObjectUnknown)
    {
        return false;
    }

    // The tap object exposes the format it delivers; this is available right after the tap is created and is
    // more reliable than the aggregate device's input StreamFormat, which can be missing briefly (or entirely
    // on some macOS versions) for a freshly created tap-backed aggregate.
    AudioObjectPropertyAddress address = PropertyAddress(kAudioTapPropertyFormat);
    UInt32 size = sizeof(AudioStreamBasicDescription);
    OSStatus status = AudioObjectGetPropertyData(tap_id, &address, 0, nullptr, &size, out_asbd);
    return status == kNoErr;
}

bool TryResolveAggregateTapInputFormat(
    AudioObjectID aggregate_device_id,
    AudioObjectID tap_id,
    AudioStreamBasicDescription* out_asbd)
{
    if (ReadDeviceInputStreamFormat(aggregate_device_id, out_asbd))
    {
        return true;
    }

    if (TryReadTapStreamFormat(tap_id, out_asbd))
    {
        return true;
    }

    AudioObjectPropertyAddress streams_address {};
    streams_address.mSelector = kAudioDevicePropertyStreams;
    streams_address.mScope = kAudioObjectPropertyScopeInput;
    streams_address.mElement = kAudioObjectPropertyElementMain;

    if (!AudioObjectHasProperty(aggregate_device_id, &streams_address))
    {
        return false;
    }

    UInt32 streams_byte_size = 0;
    OSStatus status = AudioObjectGetPropertyDataSize(
        aggregate_device_id,
        &streams_address,
        0,
        nullptr,
        &streams_byte_size);
    if (status != kNoErr || streams_byte_size < sizeof(AudioStreamID))
    {
        return false;
    }

    const UInt32 stream_count = streams_byte_size / sizeof(AudioStreamID);
    std::vector<AudioStreamID> streams(stream_count);
    status = AudioObjectGetPropertyData(
        aggregate_device_id,
        &streams_address,
        0,
        nullptr,
        &streams_byte_size,
        streams.data());
    if (status != kNoErr)
    {
        return false;
    }

    for (UInt32 i = 0; i < stream_count; ++i)
    {
        if (TryReadVirtualOrPhysicalStreamFormat(streams[i], out_asbd))
        {
            return true;
        }
    }

    return false;
}

void FillDeliveryFormat(const AudioStreamBasicDescription& asbd, audio_tap_format* out_format)
{
    out_format->sample_rate = asbd.mSampleRate;
    out_format->channels = asbd.mChannelsPerFrame;
    out_format->bits_per_sample = asbd.mBitsPerChannel;
    out_format->is_float = (asbd.mFormatFlags & kAudioFormatFlagIsFloat) != 0 ? 1u : 0u;
}

// Realtime device IOProc on the tap-backed aggregate device. Must be lock-free: AudioDeviceStop
// (called under g_state.mutex during teardown) blocks until the in-flight IOProc returns, so taking
// g_state.mutex here would deadlock. Reads are safe because callback/user_data/format are written
// before AudioDeviceStart and only cleared after AudioDeviceStop guarantees this proc is no longer running.
OSStatus DeviceIOProc(
    AudioObjectID device,
    const AudioTimeStamp* now,
    const AudioBufferList* input_data,
    const AudioTimeStamp* input_time,
    AudioBufferList* output_data,
    const AudioTimeStamp* output_time,
    void* client_data)
{
    (void)device;
    (void)now;
    (void)input_time;
    (void)output_data;
    (void)output_time;

    TapState* state = static_cast<TapState*>(client_data);
    if (state == nullptr || input_data == nullptr || input_data->mNumberBuffers == 0)
    {
        return kNoErr;
    }

    audio_tap_pcm_callback callback = state->callback;
    if (callback == nullptr)
    {
        return kNoErr;
    }

    const AudioBuffer& buffer = input_data->mBuffers[0];
    const void* data = buffer.mData;
    uint32_t byte_count = buffer.mDataByteSize;
    if (data != nullptr && byte_count > 0)
    {
        audio_tap_format format = state->delivery_format;
        callback(state->user_data, data, byte_count, &format);
    }

    return kNoErr;
}

bool StartAggregateIoProc(char* error_out, size_t error_out_size)
{
    // Tap-backed aggregates sometimes omit device-level StreamFormat briefly; enumeration + a few short
    // retries avoids failing while avoiding long mutex-held sleeps (audio_tap_start holds g_state.mutex).
    constexpr int kMaxFormatAttempts = 10;
    bool resolved_format = false;
    for (int attempt = 0; attempt < kMaxFormatAttempts; ++attempt)
    {
        if (TryResolveAggregateTapInputFormat(g_state.aggregate_id, g_state.tap_id, &g_state.asbd))
        {
            resolved_format = true;
            break;
        }

        if (attempt + 1 < kMaxFormatAttempts)
        {
            usleep(5000);
        }
    }

    if (!resolved_format)
    {
        SetError(error_out, error_out_size, "Could not read aggregate device input stream format.");
        return false;
    }

    FillDeliveryFormat(g_state.asbd, &g_state.delivery_format);

    // Drive the aggregate device with an IOProc instead of an Audio Queue: AudioQueue resolves its
    // CurrentDevice through the public HAL device list and fails to start on a private tap-backed
    // aggregate, whereas an IOProc binds to the AudioObjectID directly.
    OSStatus status = AudioDeviceCreateIOProcID(
        g_state.aggregate_id,
        DeviceIOProc,
        &g_state,
        &g_state.io_proc_id);
    if (status != kNoErr || g_state.io_proc_id == nullptr)
    {
        char message[128];
        snprintf(message, sizeof(message), "AudioDeviceCreateIOProcID failed (status=%d).", static_cast<int>(status));
        SetError(error_out, error_out_size, message);
        return false;
    }

    status = AudioDeviceStart(g_state.aggregate_id, g_state.io_proc_id);
    if (status != kNoErr)
    {
        char message[128];
        snprintf(message, sizeof(message), "AudioDeviceStart failed (status=%d).", static_cast<int>(status));
        SetError(error_out, error_out_size, message);
        return false;
    }

    return true;
}

void TearDownLocked()
{
    if (g_state.io_proc_id != nullptr)
    {
        if (g_state.aggregate_id != kAudioObjectUnknown)
        {
            AudioDeviceStop(g_state.aggregate_id, g_state.io_proc_id);
            AudioDeviceDestroyIOProcID(g_state.aggregate_id, g_state.io_proc_id);
        }

        g_state.io_proc_id = nullptr;
    }

    if (g_state.aggregate_id != kAudioObjectUnknown)
    {
        AudioHardwareDestroyAggregateDevice(g_state.aggregate_id);
        g_state.aggregate_id = kAudioObjectUnknown;
    }

    if (g_state.tap_id != kAudioObjectUnknown)
    {
        AudioHardwareDestroyProcessTap(g_state.tap_id);
        g_state.tap_id = kAudioObjectUnknown;
    }

    g_state.callback = nullptr;
    g_state.user_data = nullptr;
    g_state.running = false;
}

} // namespace

extern "C" {

int audio_tap_is_supported(void)
{
    return IsMacOs142OrLater() ? 1 : 0;
}

int audio_tap_start(
    const audio_tap_config* config,
    audio_tap_pcm_callback callback,
    void* user_data,
    char* error_out,
    size_t error_out_size)
{
    if (!IsMacOs142OrLater())
    {
        SetError(error_out, error_out_size, "Core Audio process taps require macOS 14.2 or later.");
        return -1;
    }

    if (callback == nullptr)
    {
        SetError(error_out, error_out_size, "callback is null.");
        return -1;
    }

    // Runs on a .NET P/Invoke thread that may have no autorelease pool; wrap the body so autoreleased
    // Foundation/CoreAudio temporaries (CATapDescription, NSDictionary, NSUUID, ...) are drained.
    @autoreleasepool
    {
        // Explicitly obtain System Audio Recording consent before touching Core Audio (which would otherwise
        // succeed silently and deliver nothing). Done outside the lock so the prompt wait never blocks teardown.
        if (!EnsureSystemAudioRecordingAuthorized(error_out, error_out_size))
        {
            return -1;
        }

        pthread_mutex_lock(&g_state.mutex);
        if (g_state.running)
        {
            pthread_mutex_unlock(&g_state.mutex);
            SetError(error_out, error_out_size, "audio tap is already running.");
            return -1;
        }

        TearDownLocked();

        CATapDescription* description = BuildTapDescription(config, error_out, error_out_size);
        if (description == nil)
        {
            pthread_mutex_unlock(&g_state.mutex);
            return -1;
        }

        OSStatus status = AudioHardwareCreateProcessTap(description, &g_state.tap_id);
        if (status != kNoErr || g_state.tap_id == kAudioObjectUnknown)
        {
            pthread_mutex_unlock(&g_state.mutex);
            SetError(error_out, error_out_size, "AudioHardwareCreateProcessTap failed.");
            return -1;
        }

        CFStringRef tap_uid = nullptr;
        if (!CopyTapUid(g_state.tap_id, &tap_uid))
        {
            TearDownLocked();
            pthread_mutex_unlock(&g_state.mutex);
            SetError(error_out, error_out_size, "Could not read tap UID.");
            return -1;
        }

        NSString* aggregate_uid = [[NSUUID UUID] UUIDString];

        // The aggregate must be driven by a real audio device (the default output) as its main sub-device so
        // the HAL runs an IO cycle; the tap is then attached via the tap list (dictionary form with drift
        // compensation). A tap-only aggregate starts without error but never delivers buffers.
        CFStringRef output_uid = CopyDefaultOutputDeviceUid();
        NSMutableDictionary* aggregate_description = [@{
            @kAudioAggregateDeviceNameKey : @"AudioAnalyzer Tap Aggregate",
            @kAudioAggregateDeviceUIDKey : aggregate_uid,
            @kAudioAggregateDeviceTapListKey : @[@{
                @kAudioSubTapUIDKey : (__bridge NSString*)tap_uid,
                @kAudioSubTapDriftCompensationKey : @YES
            }],
            @kAudioAggregateDeviceTapAutoStartKey : @YES,
            @kAudioAggregateDeviceIsPrivateKey : @YES,
            @kAudioAggregateDeviceIsStackedKey : @NO
        } mutableCopy];

        if (output_uid != nullptr)
        {
            NSString* output_uid_string = (__bridge NSString*)output_uid;
            aggregate_description[@kAudioAggregateDeviceMainSubDeviceKey] = output_uid_string;
            aggregate_description[@kAudioAggregateDeviceSubDeviceListKey] = @[@{
                @kAudioSubDeviceUIDKey : output_uid_string
            }];
        }

        CFRelease(tap_uid);
        if (output_uid != nullptr)
        {
            CFRelease(output_uid);
        }

        status = AudioHardwareCreateAggregateDevice((__bridge CFDictionaryRef)aggregate_description, &g_state.aggregate_id);
        if (status != kNoErr || g_state.aggregate_id == kAudioObjectUnknown)
        {
            TearDownLocked();
            pthread_mutex_unlock(&g_state.mutex);
            SetError(error_out, error_out_size, "AudioHardwareCreateAggregateDevice failed.");
            return -1;
        }

        g_state.callback = callback;
        g_state.user_data = user_data;

        if (!StartAggregateIoProc(error_out, error_out_size))
        {
            TearDownLocked();
            pthread_mutex_unlock(&g_state.mutex);
            return -1;
        }

        g_state.running = true;
        pthread_mutex_unlock(&g_state.mutex);
        return 0;
    }
}

void audio_tap_stop(void)
{
    pthread_mutex_lock(&g_state.mutex);
    TearDownLocked();
    pthread_mutex_unlock(&g_state.mutex);
}

int audio_tap_is_running(void)
{
    pthread_mutex_lock(&g_state.mutex);
    int running = g_state.running ? 1 : 0;
    pthread_mutex_unlock(&g_state.mutex);
    return running;
}

} // extern "C"
