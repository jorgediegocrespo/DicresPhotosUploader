# Publica un .exe autocontenido de un solo fichero para Windows (sin necesidad de tener .NET instalado).
# Debe ejecutarse en Windows (o con el RID win-x64 desde cualquier SO, pero probarlo solo funciona en Windows).
$ErrorActionPreference = "Stop"

Set-Location (Join-Path $PSScriptRoot "..")

$outDir = "dist/windows"
Remove-Item -Recurse -Force $outDir -ErrorAction SilentlyContinue

dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o $outDir

Write-Host "Generado $outDir/GooglePhotosUploader.exe"
