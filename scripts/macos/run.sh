#!/usr/bin/env bash
#
# Build the macOS console host, wrap it in an ad-hoc code-signed .app bundle (so macOS grants
# Microphone / System Audio Recording consent), and run the inner executable in this terminal.
# See ADR-0088 and docs/getting-started.md.
#
# Usage:
#   scripts/macos/run.sh [-- <app args>]
# Environment:
#   CONFIG   Build configuration (default: Debug)
#   TFM      macOS host TFM (default: read from Directory.Build.props)

set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "run.sh only runs on macOS." >&2
  exit 1
fi

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
PROJECT="$REPO_ROOT/src/AudioAnalyzer.Console/AudioAnalyzer.Console.csproj"
INFO_PLIST="$REPO_ROOT/src/AudioAnalyzer.Console/macOS/Info.plist"

CONFIG="${CONFIG:-Debug}"
if [[ -z "${TFM:-}" ]]; then
  TFM="$(sed -n 's:.*<AudioAnalyzerMacOsHostTfm>\(.*\)</AudioAnalyzerMacOsHostTfm>.*:\1:p' "$REPO_ROOT/Directory.Build.props" | head -n1)"
fi
if [[ -z "$TFM" ]]; then
  echo "Could not resolve macOS host TFM; set TFM=net10.0-macos26.0 (or current pin)." >&2
  exit 1
fi

dotnet build "$PROJECT" -f "$TFM" -c "$CONFIG"

case "$(uname -m)" in
  arm64) RID="osx-arm64" ;;
  x86_64) RID="osx-x64" ;;
  *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac

TARGET_DIR="$REPO_ROOT/src/AudioAnalyzer.Console/bin/$CONFIG/$TFM/$RID"
APP_PATH="$TARGET_DIR/AudioAnalyzer.Console.app"
SHIM_PATH="$REPO_ROOT/native/audio-tap-shim/build/libaudio_tap_shim.dylib"
VIDEO_SHIM_PATH="$REPO_ROOT/native/video-capture-shim/build/libvideo_capture_shim.dylib"
MRA_FRAMEWORK_PATH="$REPO_ROOT/native/mediaremote-adapter/build/MediaRemoteAdapter.framework"
MRA_SCRIPT_PATH="$REPO_ROOT/native/mediaremote-adapter/bin/mediaremote-adapter.pl"
if [[ ! -d "$APP_PATH" ]]; then
  echo "App bundle not found at $APP_PATH" >&2
  exit 1
fi

INNER="$("$SCRIPT_DIR/pack-bundle.sh" "$APP_PATH" "$INFO_PLIST" "$SHIM_PATH" "$VIDEO_SHIM_PATH" "$MRA_FRAMEWORK_PATH" "$MRA_SCRIPT_PATH" | tail -n1)"

exec "$INNER" "$@"
