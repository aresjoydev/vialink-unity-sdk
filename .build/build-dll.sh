#!/bin/bash
# ViaLink Unity SDK DLL 빌드 스크립트
# C# 소스를 netstandard2.1 DLL로 컴파일하여 소스코드를 숨김
#
# 사전 요구사항: dotnet SDK (brew install dotnet)
# 사용법: .build/build-dll.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
SDK_ROOT="$(dirname "$SCRIPT_DIR")"
BUILD_DIR="$SCRIPT_DIR"
STUBS_DIR="$BUILD_DIR/stubs"
OUTPUT_DLL="$SDK_ROOT/Runtime/Plugins/ViaLinkSDK.dll"

# dotnet 경로 설정 (Homebrew)
export DOTNET_ROOT="${DOTNET_ROOT:-/opt/homebrew/opt/dotnet/libexec}"
export PATH="$DOTNET_ROOT:$PATH"

echo "[1/4] dotnet CLI 확인..."
if ! command -v dotnet &> /dev/null; then
    echo "dotnet CLI가 설치되어 있지 않습니다."
    echo "설치: brew install dotnet"
    exit 1
fi
echo "  dotnet $(dotnet --version)"

echo "[2/4] Unity 엔진 stub DLL 빌드..."
cd "$STUBS_DIR"
dotnet build -c Release -o bin/ --nologo -v quiet
echo "  UnityEngine.dll stub 생성 완료"

echo "[3/4] ViaLinkSDK DLL 빌드..."
cd "$BUILD_DIR"
dotnet build -c Release --nologo -v quiet
echo "  ViaLinkSDK.dll 빌드 완료"

echo "[4/4] DLL 배포..."
mkdir -p "$SDK_ROOT/Runtime/Plugins"
cp "$BUILD_DIR/bin/ViaLinkSDK.dll" "$OUTPUT_DLL"
echo "  $OUTPUT_DLL"

# DLL 정보 출력
DLL_SIZE=$(wc -c < "$OUTPUT_DLL" | tr -d ' ')
echo ""
echo "빌드 완료:"
echo "  파일: $OUTPUT_DLL"
echo "  크기: ${DLL_SIZE} bytes"
echo "  타겟: netstandard2.1 (Unity 2021.3+)"
