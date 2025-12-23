#!/bin/bash
# RunCat-Lite Podman Build Script
# 零宿主机依赖，零污染，编译产物输出到 dist/
#
# 支持三种构建模式：
# 1. portable (默认): 自包含 + 配置文件在程序运行目录
# 2. installed: 依赖系统 .NET Desktop Runtime + 配置在 AppData
# 3. installed-self: 自包含 + 配置在 AppData

set -e

# 配置
DOTNET_VERSION="9.0"
PROJECT_NAME="RunCat-Lite"
PROJECT_DIR="RunCatLite"
OUTPUT_BASE="dist"

# 目标平台配置
declare -A PLATFORMS=(
    ["win-x64"]="Windows x64"
    ["win-x86"]="Windows x86"
    ["win-arm64"]="Windows ARM64"
)

# 构建模式配置
declare -A BUILD_MODES=(
    ["portable"]="自包含，配置在程序目录"
    ["installed"]="依赖系统 .NET，配置在 AppData"
    ["installed-self"]="自包含，配置在 AppData"
)

# 颜色输出
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

log_step() {
    echo -e "${CYAN}[STEP]${NC} $1"
}

# 检查 podman 是否可用
check_podman() {
    if ! command -v podman &> /dev/null; then
        log_error "podman 未安装，请先安装 podman"
        exit 1
    fi
    log_info "Podman version: $(podman --version)"
}

# 构建函数
build_platform() {
    local RID=$1
    local BUILD_MODE=$2
    local PLATFORM_DESC="${PLATFORMS[$RID]}"

    # 从 csproj 中提取版本号
    local VERSION=$(grep -oP '(?<=<Version>)[^<]+' "${PROJECT_DIR}/${PROJECT_DIR}.csproj" | head -1)
    if [[ -z "$VERSION" ]]; then
        VERSION="1.0.0"
    fi

    # 根据构建模式确定输出目录名称
    local MODE_SUFFIX=""
    case "${BUILD_MODE}" in
        "portable")
            MODE_SUFFIX="_portable"
            ;;
        "installed")
            MODE_SUFFIX="_installed"
            ;;
        "installed-self")
            MODE_SUFFIX="_installed-self"
            ;;
    esac

    local OUTPUT_DIR="${OUTPUT_BASE}/${PROJECT_NAME}_${RID}_net${DOTNET_VERSION}_v${VERSION}${MODE_SUFFIX}"

    log_step "开始构建: ${PLATFORM_DESC} (${RID}) - ${BUILD_MODES[$BUILD_MODE]}"

    # 创建输出目录
    mkdir -p "${OUTPUT_DIR}"

    # 创建持久化的 NuGet 缓存目录
    local CACHE_DIR="${PWD}/.build-cache"
    mkdir -p "${CACHE_DIR}/nuget" "${CACHE_DIR}/dotnet"

    # 根据构建模式确定参数
    local SELF_CONTAINED="true"
    local PORTABLE_MODE="true"

    case "${BUILD_MODE}" in
        "portable")
            SELF_CONTAINED="true"
            PORTABLE_MODE="true"
            ;;
        "installed")
            SELF_CONTAINED="false"
            PORTABLE_MODE="false"
            ;;
        "installed-self")
            SELF_CONTAINED="true"
            PORTABLE_MODE="false"
            ;;
    esac

    # 使用 podman 运行 .NET SDK 容器进行编译
    # 使用 --userns=keep-id 保持当前用户权限，避免产物归root所有
    # 挂载持久化缓存目录以加速后续构建
    podman run --rm \
        --network=host \
        --userns=keep-id \
        -e HOME=/tmp \
        -e DOTNET_CLI_HOME=/cache/dotnet \
        -e NUGET_PACKAGES=/cache/nuget \
        -e DOTNET_NOLOGO=1 \
        -e DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1 \
        -e DOTNET_CLI_TELEMETRY_OPTOUT=1 \
        -e DOTNET_SKIP_WORKLOAD_INTEGRITY_CHECK=1 \
        -v "$(pwd):/src:Z" \
        -v "${CACHE_DIR}:/cache:Z" \
        -w /src \
        "mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}" \
        dotnet publish "${PROJECT_DIR}/${PROJECT_DIR}.csproj" \
            -c Release \
            -r "${RID}" \
            --self-contained "${SELF_CONTAINED}" \
            -o "/src/${OUTPUT_DIR}" \
            -p:PublishSingleFile=true \
            -p:IncludeNativeLibrariesForSelfExtract=true \
            -p:PublishReadyToRun=false \
            -p:EnableWindowsTargeting=true \
            -p:DebugType=None \
            -p:DebugSymbols=false \
            -p:PortableMode="${PORTABLE_MODE}" \
            -p:BaseIntermediateOutputPath=/tmp/obj/ \
            -p:BaseOutputPath=/tmp/bin/

    # 创建版本信息文件
    cat > "${OUTPUT_DIR}/BUILD_INFO.txt" <<EOF
Project: ${PROJECT_NAME}
Build Mode: ${BUILD_MODE} (${BUILD_MODES[$BUILD_MODE]})
Target Runtime: ${RID}
.NET Version: ${DOTNET_VERSION}
Self-Contained: ${SELF_CONTAINED}
Portable Mode: ${PORTABLE_MODE}
Build Time: $(date -Iseconds)
Build Host: $(hostname)
EOF

    # 复制 runners 目录到输出
    cp -r "${PROJECT_DIR}/resources/runners" "${OUTPUT_DIR}/"
    log_info "已复制 runners 目录"

    # 使用 podman unshare 修复文件权限
    # 在 unshare 命名空间中，UID 0 映射回宿主用户
    podman unshare chown -R 0:0 "${OUTPUT_DIR}"
    log_info "已修复文件权限"

    log_info "构建完成: ${OUTPUT_DIR}"

    # 显示产物信息
    log_info "产物文件:"
    ls -lh "${OUTPUT_DIR}/"*.exe 2>/dev/null || ls -lh "${OUTPUT_DIR}/"*.dll | head -5

    # 如果是非自包含版本，提示依赖信息
    if [[ "${SELF_CONTAINED}" == "false" ]]; then
        log_warn "此版本需要目标系统安装 .NET Desktop Runtime ${DOTNET_VERSION}"
        echo ""
        echo "  下载地址: https://dotnet.microsoft.com/download/dotnet/${DOTNET_VERSION}"
        echo ""
    fi
}

# 清理旧的构建产物
clean_build() {
    log_warn "清理 ${PROJECT_DIR}/bin 和 ${PROJECT_DIR}/obj 目录..."
    rm -rf "${PROJECT_DIR}/bin" "${PROJECT_DIR}/obj"
    log_info "清理完成"
}

# 主函数
main() {
    echo "=========================================="
    echo "  ${PROJECT_NAME} Podman Build Script"
    echo "=========================================="
    echo ""

    check_podman

    # 解析参数
    local PLATFORM="${1:-win-x64}"
    local BUILD_MODE="${2:-portable}"
    local DO_CLEAN="${3:-}"

    # 检查是否是清理命令
    if [[ "${PLATFORM}" == "--clean" ]]; then
        clean_build
        exit 0
    fi

    if [[ "${DO_CLEAN}" == "--clean" ]] || [[ "${BUILD_MODE}" == "--clean" ]]; then
        clean_build
        if [[ "${BUILD_MODE}" == "--clean" ]]; then
            BUILD_MODE="portable"
        fi
    fi

    # 验证构建模式
    if [[ -z "${BUILD_MODES[$BUILD_MODE]}" ]]; then
        log_error "不支持的构建模式: ${BUILD_MODE}"
        log_info "支持的模式: ${!BUILD_MODES[*]}"
        exit 1
    fi

    if [[ "${PLATFORM}" == "all" ]]; then
        for RID in "${!PLATFORMS[@]}"; do
            build_platform "${RID}" "${BUILD_MODE}"
            echo ""
        done
    else
        if [[ -z "${PLATFORMS[$PLATFORM]}" ]]; then
            log_error "不支持的平台: ${PLATFORM}"
            log_info "支持的平台: ${!PLATFORMS[*]}"
            exit 1
        fi
        build_platform "${PLATFORM}" "${BUILD_MODE}"
    fi

    echo ""
    log_info "所有构建完成！"
    log_info "产物目录: ${OUTPUT_BASE}/"
    ls -la "${OUTPUT_BASE}/"
}

# 显示帮助
if [[ "${1:-}" == "-h" ]] || [[ "${1:-}" == "--help" ]]; then
    echo "Usage: $0 [PLATFORM] [BUILD_MODE] [--clean]"
    echo ""
    echo "PLATFORM:"
    echo "  win-x64    Windows x64 (默认)"
    echo "  win-x86    Windows x86"
    echo "  win-arm64  Windows ARM64"
    echo "  all        构建所有平台"
    echo ""
    echo "BUILD_MODE:"
    echo "  portable       自包含 + 配置在程序目录 (默认/绿色版)"
    echo "  installed      依赖系统 .NET + 配置在 AppData (需安装 .NET Runtime)"
    echo "  installed-self 自包含 + 配置在 AppData (安装版)"
    echo ""
    echo "Options:"
    echo "  --clean    清理构建目录"
    echo ""
    echo "Examples:"
    echo "  $0                           # 构建 win-x64 portable"
    echo "  $0 win-x64 portable          # 构建 win-x64 portable (绿色版)"
    echo "  $0 win-x64 installed         # 构建 win-x64 installed (需系统 .NET)"
    echo "  $0 win-x64 installed-self    # 构建 win-x64 installed-self (安装版)"
    echo "  $0 all portable              # 构建所有平台 portable"
    echo "  $0 --clean                   # 仅清理"
    echo "  $0 win-x64 portable --clean  # 清理后构建"
    echo ""
    echo "配置文件位置:"
    echo "  portable:       <程序目录>/config.json"
    echo "  installed/self: %APPDATA%/RunCat-Lite/<版本>/config.json"
    exit 0
fi

main "$@"
