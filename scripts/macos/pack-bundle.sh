#!/usr/bin/env bash
#
# Finalize the macOS .app bundle that the macOS workload builds (Contents/MacOS launcher + MonoBundle +
# Resources) so macOS TCC (privacy) grants Microphone / System Audio Recording consent. See ADR-0088.
#
# The SDK generates and ad-hoc signs the bundle but does not embed our privacy usage strings, and the
# Core Audio tap / video capture shims must live next to the launcher. This script injects the usage
# strings from the project Info.plist, copies the shims into Contents/MacOS when present, copies the
# mediaremote-adapter now-playing artifacts into Contents/Resources when present, and re-signs the
# bundle ad-hoc.
#
# Callers run the inner launcher (…/AudioAnalyzer.Console.app/Contents/MacOS/AudioAnalyzer.Console)
# directly from a terminal, which keeps the interactive TUI while giving the process the bundle identity.
#
# Usage:
#   pack-bundle.sh <app-path> <source-info-plist> [audio-shim-dylib-path] [video-shim-dylib-path] \
#                  [mediaremote-framework-path] [mediaremote-script-path] [bundle-identifier]
#
# On success the inner launcher path is printed on the last stdout line.

set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "pack-bundle.sh only runs on macOS." >&2
  exit 1
fi

APP_PATH="${1:?app-path (.app bundle) is required}"
SRC_PLIST="${2:?source Info.plist path is required}"
SHIM_PATH="${3:-}"
VIDEO_SHIM_PATH="${4:-}"
MRA_FRAMEWORK_PATH="${5:-}"
MRA_SCRIPT_PATH="${6:-}"
IDENTIFIER="${7:-dev.audioanalyzer.console}"

APP_PATH="$(cd "$APP_PATH" && pwd)"
CONTENTS="$APP_PATH/Contents"
MACOS_DIR="$CONTENTS/MacOS"
APP_PLIST="$CONTENTS/Info.plist"
PB=/usr/libexec/PlistBuddy

if [[ ! -f "$APP_PLIST" ]]; then
  echo "Bundle Info.plist not found: $APP_PLIST (build the macOS .app first)" >&2
  exit 1
fi

EXE_NAME="$("$PB" -c 'Print :CFBundleExecutable' "$APP_PLIST")"
if [[ -z "$EXE_NAME" || ! -f "$MACOS_DIR/$EXE_NAME" ]]; then
  echo "Bundle launcher not found in $MACOS_DIR (CFBundleExecutable=$EXE_NAME)" >&2
  exit 1
fi

# Copy the privacy usage strings from the project Info.plist into the bundle Info.plist (idempotent).
inject_usage_string() {
  local key="$1"
  local value
  if value="$("$PB" -c "Print :$key" "$SRC_PLIST" 2>/dev/null)"; then
    "$PB" -c "Delete :$key" "$APP_PLIST" >/dev/null 2>&1 || true
    "$PB" -c "Add :$key string $value" "$APP_PLIST" >/dev/null
  fi
}

inject_usage_string NSAudioCaptureUsageDescription
inject_usage_string NSMicrophoneUsageDescription
inject_usage_string NSCameraUsageDescription

# Place the Core Audio tap shim next to the launcher (MacOsAudioTapShimNative searches Contents/MacOS).
if [[ -n "$SHIM_PATH" && -f "$SHIM_PATH" ]]; then
  cp "$SHIM_PATH" "$MACOS_DIR/libaudio_tap_shim.dylib"
fi

# Place the AVFoundation video capture shim next to the launcher (MacOsVideoCaptureShimNative searches Contents/MacOS).
if [[ -n "$VIDEO_SHIM_PATH" && -f "$VIDEO_SHIM_PATH" ]]; then
  cp "$VIDEO_SHIM_PATH" "$MACOS_DIR/libvideo_capture_shim.dylib"
fi

# Place the mediaremote-adapter now-playing artifacts under Contents/Resources/mediaremote-adapter
# (MacOsMediaRemoteAdapterAvailability resolves them via the bundle Resources content root). The
# framework is bundled, not linked: /usr/bin/perl loads it as a script argument, so --deep re-sign
# below covers it. Skip silently when not built (now-playing degrades to NullNowPlayingProvider).
if [[ -n "$MRA_FRAMEWORK_PATH" && -d "$MRA_FRAMEWORK_PATH" && -n "$MRA_SCRIPT_PATH" && -f "$MRA_SCRIPT_PATH" ]]; then
  MRA_DIR="$CONTENTS/Resources/mediaremote-adapter"
  rm -rf "$MRA_DIR"
  mkdir -p "$MRA_DIR"
  cp -R "$MRA_FRAMEWORK_PATH" "$MRA_DIR/MediaRemoteAdapter.framework"
  cp "$MRA_SCRIPT_PATH" "$MRA_DIR/mediaremote-adapter.pl"
fi

# Editing Info.plist (and copying the shims) invalidated the SDK signature; re-sign. --deep covers the
# nested runtime dylibs and the shims; managed assemblies live in MonoBundle and are sealed as resources.
# Prefer a stable self-signed identity (AUDIOANALYZER_CODESIGN_IDENTITY) so TCC consent persists across
# rebuilds (see scripts/macos/create-signing-cert.sh and ADR-0091); fall back to ad-hoc when unset/missing.
# Note: a self-signed identity is untrusted, so it only appears under `find-identity -p codesigning`
# (without -v). codesign still signs with it, and TCC persistence depends on the stable certificate, not
# on the trust chain.
SIGN_IDENTITY="${AUDIOANALYZER_CODESIGN_IDENTITY:-}"
if [[ -n "$SIGN_IDENTITY" ]] && security find-identity -p codesigning 2>/dev/null | grep -qF "$SIGN_IDENTITY"; then
  echo "Signing with stable identity: $SIGN_IDENTITY" >&2
  codesign --force --deep --timestamp=none --sign "$SIGN_IDENTITY" --identifier "$IDENTIFIER" "$APP_PATH"
else
  if [[ -n "$SIGN_IDENTITY" ]]; then
    echo "Requested identity '$SIGN_IDENTITY' not found in keychain; falling back to ad-hoc." >&2
  fi
  codesign --force --deep --timestamp=none --sign - --identifier "$IDENTIFIER" "$APP_PATH"
fi

echo "$MACOS_DIR/$EXE_NAME"
