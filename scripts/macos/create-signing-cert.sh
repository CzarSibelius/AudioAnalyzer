#!/usr/bin/env bash
#
# Create a stable, self-signed code-signing identity in the login keychain so that macOS TCC
# (Microphone / System Audio Recording / Camera) consent for the AudioAnalyzer .app bundle PERSISTS
# across rebuilds. See ADR-0091.
#
# Background: pack-bundle.sh ad-hoc signs the bundle by default (codesign --sign -). An ad-hoc
# signature's designated requirement is tied to the cdhash, which changes on every rebuild, so macOS
# treats each rebuild as a new app and the previous consent is lost. Signing with a stable self-signed
# certificate gives a stable designated requirement (anchored to the certificate), so the grant sticks.
#
# Usage:
#   scripts/macos/create-signing-cert.sh ["Identity Common Name"]
#
# After running, opt into stable signing for run.sh / FinalizeMacOsAppBundle by exporting:
#   export AUDIOANALYZER_CODESIGN_IDENTITY="AudioAnalyzer Local Signing"

set -euo pipefail

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "create-signing-cert.sh only runs on macOS." >&2
  exit 1
fi

CERT_NAME="${1:-AudioAnalyzer Local Signing}"
KEYCHAIN="$HOME/Library/Keychains/login.keychain-db"

if security find-identity -p codesigning 2>/dev/null | grep -qF "$CERT_NAME"; then
  echo "Code-signing identity already present: $CERT_NAME"
  echo "Enable it with: export AUDIOANALYZER_CODESIGN_IDENTITY=\"$CERT_NAME\""
  exit 0
fi

TMP="$(mktemp -d)"
trap 'rm -rf "$TMP"' EXIT

cat > "$TMP/openssl.cnf" <<EOF
[req]
distinguished_name = dn
x509_extensions = v3
prompt = no
[dn]
CN = $CERT_NAME
[v3]
basicConstraints = critical,CA:false
keyUsage = critical,digitalSignature
extendedKeyUsage = critical,codeSigning
EOF

openssl req -x509 -newkey rsa:2048 -nodes -days 3650 \
  -keyout "$TMP/key.pem" -out "$TMP/cert.pem" -config "$TMP/openssl.cnf" >/dev/null 2>&1

# Apple's Security framework cannot verify PKCS#12 files that use OpenSSL 3's default MAC/PBE (and it
# rejects an empty PKCS#12 password), so use a throwaway password and the legacy SHA1/3DES algorithms
# that `security import` understands.
P12_PASS="audioanalyzer"
openssl pkcs12 -export -inkey "$TMP/key.pem" -in "$TMP/cert.pem" \
  -out "$TMP/cert.p12" -passout "pass:$P12_PASS" -name "$CERT_NAME" \
  -certpbe PBE-SHA1-3DES -keypbe PBE-SHA1-3DES -macalg sha1 >/dev/null 2>&1

# Import the cert + private key; -T allows codesign to use the key. May prompt for the keychain password.
security import "$TMP/cert.p12" -k "$KEYCHAIN" -P "$P12_PASS" -A -T /usr/bin/codesign

# Mark the leaf as trusted for code signing in the user domain so `codesign --sign` accepts it.
security add-trusted-cert -r trustAsRoot -p codeSign -k "$KEYCHAIN" "$TMP/cert.pem" >/dev/null 2>&1 \
  || echo "Note: could not set trust automatically; codesign may still work, or trust it via Keychain Access." >&2

# Let codesign use the key non-interactively in this keychain (best effort; ignores password mismatch).
security set-key-partition-list -S apple-tool:,apple: -k "" "$KEYCHAIN" >/dev/null 2>&1 || true

echo "Created code-signing identity: $CERT_NAME"
echo "Enable it with: export AUDIOANALYZER_CODESIGN_IDENTITY=\"$CERT_NAME\""
echo "Then run scripts/macos/run.sh (or dotnet run) and grant System Audio Recording once; it will persist across rebuilds."
