#!/usr/bin/env bash
# Prepare PICO SDK copies for abxrlib-for-unity-demo-app (dual package: Integration + OpenXR).
#
# 1) Copies pristine extracted SDKs into *-demo-app folders (fresh copy each run).
# 2) Deduplicates assemblies on the OpenXR copy only (removes Enterprise/ + Platform/).
# 3) Patches the Integration copy for CS0433 (DllImport + asmdef edits).
# 4) On macOS only: clears Gatekeeper quarantine on SpatialAudio OSX dylibs.
#
# Layout (default): workspace root = parent of this repo (e.g. .../arborxr/)
#   abxrlib-for-unity-demo-app/          ← this repo
#   abxrlib-for-unity/                   ← file:../abxrlib-for-unity in manifest
#   PICO Unity Integration SDK-<version>/  ← source (unzip from PICO)
#   Unity OpenXR IntegrationSDK-<version>/ ← source
#   PICO Unity Integration SDK-demo-app/   ← generated; referenced by manifest
#   Unity OpenXR IntegrationSDK-demo-app/  ← generated; referenced by manifest
#
# Usage:
#   bash tools/pico-setup-demo-sdks.sh
#
# Override paths (optional):
#   WORKSPACE_ROOT=/path/to/workspace \
#   PICO_INTEGRATION_SOURCE="..." \
#   OPENXR_SOURCE="..." \
#   PICO_INTEGRATION_DEMO="..." \
#   OPENXR_DEMO="..." \
#   bash tools/pico-setup-demo-sdks.sh
#
# Windows: use Git Bash (bundled with Git for Windows) so bash/cp/rm/python3 work.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEMO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
WORKSPACE_ROOT="${WORKSPACE_ROOT:-$(cd "$DEMO_ROOT/.." && pwd)}"

# Default source folder names (change if you use a different SDK drop)
PICO_INTEGRATION_SOURCE="${PICO_INTEGRATION_SOURCE:-$WORKSPACE_ROOT/PICO Unity Integration SDK-3.2.0-20250707}"
OPENXR_SOURCE="${OPENXR_SOURCE:-$WORKSPACE_ROOT/Unity OpenXR IntegrationSDK-1.4.0-20250407}"

# Demo-app copies (patched; safe to delete and recreate)
PICO_INTEGRATION_DEMO="${PICO_INTEGRATION_DEMO:-$WORKSPACE_ROOT/PICO Unity Integration SDK-demo-app}"
OPENXR_DEMO="${OPENXR_DEMO:-$WORKSPACE_ROOT/Unity OpenXR IntegrationSDK-demo-app}"

copy_tree_fresh() {
  local src="$1"
  local dst="$2"
  if [[ ! -d "$src" ]]; then
    echo "ERROR: Source folder not found: $src"
    exit 1
  fi
  if [[ -e "$dst" ]]; then
    echo "Removing existing: $dst"
    rm -rf "$dst"
  fi
  echo "Copying:"
  echo "  $src"
  echo "  -> $dst"
  mkdir -p "$(dirname "$dst")"
  cp -R "$src" "$dst"
}

dedupe_openxr_package() {
  local OPENXR_PKG="$1"
  echo ""
  echo "Deduplicating OpenXR PICO package (drop duplicate Enterprise + Platform; Integration supplies them):"
  echo "  $OPENXR_PKG"

  remove_if_present() {
    local path="$1"
    if [[ -e "$path" ]]; then
      echo "  Removing: $path"
      rm -rf "$path"
    fi
  }

  remove_if_present "$OPENXR_PKG/Enterprise"
  remove_if_present "$OPENXR_PKG/Enterprise.meta"
  remove_if_present "$OPENXR_PKG/Platform"
  remove_if_present "$OPENXR_PKG/Platform.meta"
}

patch_integration_for_dual_package() {
  local ROOT="$1"
  local PLUGIN="$ROOT/Enterprise/Scripts/Plugin/PXR_EnterprisePlugin.cs"
  local ASMDEF_EDITOR="$ROOT/Platform/Editor/PICO.Platform.Editor.asmdef"
  local TOB_ASMDEF="$ROOT/Enterprise/PICOXR.TobSupport.asmdef"

  if [[ ! -f "$PLUGIN" ]]; then
    echo "ERROR: Not found: $PLUGIN"
    exit 1
  fi

  echo ""
  echo "Patching Integration SDK for dual-package (CS0433): $ROOT"

  echo "  DllImport in PXR_EnterprisePlugin.cs"
  PATCH_FILE="$PLUGIN" python3 <<'PY'
import os
path = os.environ["PATCH_FILE"]
old = "[DllImport(PXR_Plugin.PXR_PLATFORM_DLL, CallingConvention = CallingConvention.Cdecl)]"
new = '[DllImport("PxrPlatform", CallingConvention = CallingConvention.Cdecl)]'
with open(path, encoding="utf-8") as f:
    data = f.read()
if old not in data:
    if new in data:
        print("    (already patched)")
    else:
        print("    WARNING: expected DllImport pattern not found; file may differ from supported SDK")
else:
    data = data.replace(old, new)
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(data)
PY

  if [[ -f "$ASMDEF_EDITOR" ]]; then
    echo "  PICO.Platform.Editor.asmdef (drop PICOSupport ref)"
    PATCH_FILE="$ASMDEF_EDITOR" python3 <<'PY'
import json, os
path = os.environ["PATCH_FILE"]
with open(path, encoding="utf-8-sig") as f:
    d = json.load(f)
refs = d.get("references", [])
d["references"] = [r for r in refs if r != "Unity.XR.OpenXR.Features.PICOSupport"]
with open(path, "w", encoding="utf-8") as f:
    json.dump(d, f, indent=2)
    f.write("\n")
PY
  fi

  if [[ -f "$TOB_ASMDEF" ]]; then
    echo "  PICOXR.TobSupport.asmdef (drop PICOSupport assembly refs)"
    PATCH_FILE="$TOB_ASMDEF" python3 <<'PY'
import json, os
path = os.environ["PATCH_FILE"]
with open(path, encoding="utf-8-sig") as f:
    d = json.load(f)
refs = d.get("references", [])
d["references"] = [r for r in refs if "PICOSupport" not in r]
with open(path, "w", encoding="utf-8") as f:
    json.dump(d, f, indent=4)
    f.write("\n")
PY
  fi
}

# PICO SDK ships Runtime/Windows/x86_64/applogrs.{dll,dll.lib} with PluginImporter "Any" enabled,
# which makes Unity try to include Windows import libs in Android builds. Disable Android; Win64 only.
patch_applogrs_windows_plugin_meta() {
  local ROOT="$1"
  local DIR="$ROOT/Runtime/Windows/x86_64"
  local LIB_META="$DIR/applogrs.dll.lib.meta"
  local DLL_META="$DIR/applogrs.dll.meta"
  if [[ ! -f "$LIB_META" ]]; then
    echo ""
    echo "Skipping applogrs plugin meta patch (not found): $LIB_META"
    return 0
  fi
  echo ""
  echo "Patching Windows applogrs plugin import (disable Android; Win64 Editor/Standalone only): $DIR"
  cat >"$LIB_META" <<'META'
fileFormatVersion: 2
guid: 5253f35a25951fe408e7ab9a267d78f6
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Android: Android
    second:
      enabled: 0
      settings:
        CPU: ARMv7
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData: 
  assetBundleName: 
  assetBundleVariant: 
META
  if [[ -f "$DLL_META" ]]; then
    cat >"$DLL_META" <<'META'
fileFormatVersion: 2
guid: 09a46495ad21dab45856411680676146
PluginImporter:
  externalObjects: {}
  serializedVersion: 2
  iconMap: {}
  executionOrder: {}
  defineConstraints: []
  isPreloaded: 0
  isOverridable: 1
  isExplicitlyReferenced: 0
  validateReferences: 1
  platformData:
  - first:
      Any: 
    second:
      enabled: 0
      settings: {}
  - first:
      Android: Android
    second:
      enabled: 0
      settings:
        CPU: ARMv7
  - first:
      Editor: Editor
    second:
      enabled: 1
      settings:
        CPU: x86_64
        DefaultValueInitialized: true
        OS: Windows
  - first:
      Standalone: Win64
    second:
      enabled: 1
      settings:
        CPU: x86_64
  userData: 
  assetBundleName: 
  assetBundleVariant: 
META
  fi
}

clear_macos_spatial_audio_quarantine() {
  local PICO_ROOT="$1"
  local OSX_PLUGINS="$PICO_ROOT/SpatialAudio/Plugins/OSX"
  if [[ "$(uname -s)" != "Darwin" ]]; then
    echo ""
    echo "Skipping macOS quarantine clear (not on macOS)."
    return 0
  fi
  if [[ ! -d "$OSX_PLUGINS" ]]; then
    echo ""
    echo "Skipping macOS quarantine: folder not found: $OSX_PLUGINS"
    return 0
  fi
  echo ""
  echo "macOS: clearing quarantine on SpatialAudio plugins:"
  echo "  $OSX_PLUGINS"
  xattr -dr com.apple.quarantine "$OSX_PLUGINS"
}

echo "Workspace: $WORKSPACE_ROOT"
echo "Demo app:  $DEMO_ROOT"
echo ""

copy_tree_fresh "$PICO_INTEGRATION_SOURCE" "$PICO_INTEGRATION_DEMO"
copy_tree_fresh "$OPENXR_SOURCE" "$OPENXR_DEMO"

dedupe_openxr_package "$OPENXR_DEMO"
patch_integration_for_dual_package "$PICO_INTEGRATION_DEMO"
patch_applogrs_windows_plugin_meta "$PICO_INTEGRATION_DEMO"
clear_macos_spatial_audio_quarantine "$PICO_INTEGRATION_DEMO"

echo ""
echo "Done. Expected Package Manager paths (see Packages/manifest.json; resolved from Packages/):"
echo "  com.unity.xr.picoxr        -> ../../$(basename "$PICO_INTEGRATION_DEMO")"
echo "  com.unity.xr.openxr.picoxr -> ../../$(basename "$OPENXR_DEMO")"
echo "Reopen Unity or reimport packages if the editor was open."
