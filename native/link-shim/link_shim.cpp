// SPDX-License-Identifier: GPL-2.0-or-later
// Thin C ABI over Ableton Link for AudioAnalyzer managed interop.

#include <ableton/Link.hpp>

#include <cmath>
#include <cstdlib>

#if defined(_WIN32)
#define LINK_SHIM_API __declspec(dllexport)
#else
#define LINK_SHIM_API __attribute__((visibility("default")))
#endif

struct LinkShimContext
{
    ableton::Link link;
    explicit LinkShimContext(double bpm) : link(bpm) {}
};

extern "C" {

LINK_SHIM_API void* link_shim_create(double initial_bpm)
{
    if (initial_bpm <= 0.0 || !std::isfinite(initial_bpm))
    {
        initial_bpm = 120.0;
    }
    return new LinkShimContext(initial_bpm);
}

LINK_SHIM_API void link_shim_destroy(void* handle)
{
    delete static_cast<LinkShimContext*>(handle);
}

LINK_SHIM_API void link_shim_set_enabled(void* handle, int enabled)
{
    if (handle == nullptr)
    {
        return;
    }
    static_cast<LinkShimContext*>(handle)->link.enable(enabled != 0);
}

LINK_SHIM_API int link_shim_capture(void* handle, double quantum, double* out_tempo, int* out_peers, double* out_beat)
{
    if (handle == nullptr)
    {
        return 0;
    }
    if (quantum <= 0.0 || !std::isfinite(quantum))
    {
        quantum = 4.0;
    }

    auto* ctx = static_cast<LinkShimContext*>(handle);
    auto& link = ctx->link;
    auto state = link.captureAppSessionState();
    auto t = link.clock().micros();
    const double tempo = state.tempo();
    const double beat = state.beatAtTime(t, quantum);
    const int peers = static_cast<int>(link.numPeers());

    if (out_tempo != nullptr)
    {
        *out_tempo = tempo;
    }
    if (out_peers != nullptr)
    {
        *out_peers = peers;
    }
    if (out_beat != nullptr)
    {
        *out_beat = beat;
    }
    return 1;
}

} // extern "C"
