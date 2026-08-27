#!/usr/bin/env bash
# Publica un .app de macOS autocontenido (sin necesidad de tener .NET instalado en la máquina destino).
# Debe ejecutarse en un Mac. Genera un binario por cada arquitectura indicada y los empaqueta en el bundle.
set -euo pipefail

cd "$(dirname "$0")/.."

APP_NAME="DicresPhotosUploader"
BUNDLE_ID="com.jorgediegocrespo.dicresphotosuploader"
OUT_DIR="dist/macos"
RIDS=("osx-arm64" "osx-x64")

rm -rf "$OUT_DIR"

for RID in "${RIDS[@]}"; do
  echo "== Publicando $RID =="
  dotnet publish src/DicresPhotosUploader/DicresPhotosUploader.csproj -c Release -r "$RID" --self-contained true \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -o "$OUT_DIR/$RID-publish"

  APP_DIR="$OUT_DIR/$RID/$APP_NAME.app"
  mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"

  cp "$OUT_DIR/$RID-publish/$APP_NAME" "$APP_DIR/Contents/MacOS/$APP_NAME"
  chmod +x "$APP_DIR/Contents/MacOS/$APP_NAME"
  cp "src/DicresPhotosUploader/assets/AppIcon.icns" "$APP_DIR/Contents/Resources/AppIcon.icns"

  cat > "$APP_DIR/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0"><dict>
  <key>CFBundleName</key><string>$APP_NAME</string>
  <key>CFBundleDisplayName</key><string>Dicres Photos Uploader</string>
  <key>CFBundleIdentifier</key><string>$BUNDLE_ID</string>
  <key>CFBundleVersion</key><string>1.0.0</string>
  <key>CFBundleShortVersionString</key><string>1.0.0</string>
  <key>CFBundlePackageType</key><string>APPL</string>
  <key>CFBundleExecutable</key><string>$APP_NAME</string>
  <key>CFBundleIconFile</key><string>AppIcon</string>
  <key>LSMinimumSystemVersion</key><string>11.0</string>
  <key>NSHighResolutionCapable</key><true/>
</dict></plist>
PLIST

  echo "Generado $APP_DIR"
done

echo "Listo. La app no está firmada: la primera vez, clic derecho > Abrir para saltarte el aviso de Gatekeeper."
