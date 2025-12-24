#!/bin/bash
# RunCat-Lite Podman Build Script
# 零宿主机依赖，零污染，编译产物输出到 dist/
#
# 默认构建为自包含 (static)，指定 --dynamic 则依赖外部 .NET Runtime

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

# 显示帮助信息
show_help() {
    echo "Usage: $0 <PLATFORM> [--dynamic|--static]"
    echo ""
    echo "PLATFORM (必选):"
    echo "  win-x64    Windows x64 (Intel/AMD)"
    echo "  win-x86    Windows x86 (32位)"
    echo "  win-arm64  Windows ARM64"
    echo "  all        构建所有平台"
    echo ""
    echo "OPTIONS:"
    echo "  --static   自包含编译 (默认，无需安装 .NET Runtime)"
    echo "  --dynamic  依赖外部 .NET Desktop Runtime ${DOTNET_VERSION}"
    echo ""
    echo "当 PLATFORM=all 时："
    echo "  不指定模式  → 构建所有平台 × 所有模式（笛卡尔积）"
    echo "  指定 --static  → 仅构建所有平台的自包含版本"
    echo "  指定 --dynamic → 仅构建所有平台的依赖运行时版本"
    echo ""
    echo "Examples:"
    echo "  $0 win-x64              # 自包含 win-x64"
    echo "  $0 win-x64 --dynamic    # 依赖运行时 win-x64"
    echo "  $0 all                  # 所有平台 × 所有模式"
    echo "  $0 all --static         # 所有平台，仅自包含"
    echo "  $0 all --dynamic        # 所有平台，仅依赖运行时"
    echo ""
    echo "配置文件位置: <程序目录>/config.json"
    exit 0
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
# $1: RID (runtime identifier)
# $2: SELF_CONTAINED (true/false)
build_platform() {
    local RID=$1
    local SELF_CONTAINED=$2
    local PLATFORM_DESC="${PLATFORMS[$RID]}"

    # 从 csproj 中提取版本号
    local VERSION=$(grep -oP '(?<=<Version>)[^<]+' "${PROJECT_DIR}/${PROJECT_DIR}.csproj" | head -1)
    if [[ -z "$VERSION" ]]; then
        VERSION="1.0.0"
    fi

    # 根据自包含模式确定输出目录后缀
    local MODE_SUFFIX=""
    local MODE_DESC=""
    if [[ "${SELF_CONTAINED}" == "true" ]]; then
        MODE_SUFFIX="_static"
        MODE_DESC="自包含"
    else
        MODE_SUFFIX="_dynamic"
        MODE_DESC="依赖 .NET Runtime"
    fi

    local OUTPUT_DIR="${OUTPUT_BASE}/${PROJECT_NAME}_${RID}_net${DOTNET_VERSION}_v${VERSION}${MODE_SUFFIX}"

    log_step "开始构建: ${PLATFORM_DESC} (${RID}) - ${MODE_DESC}"

    # 创建输出目录
    mkdir -p "${OUTPUT_DIR}"

    # 创建持久化的 NuGet 缓存目录
    local CACHE_DIR="${PWD}/.build-cache"
    mkdir -p "${CACHE_DIR}/nuget" "${CACHE_DIR}/dotnet"

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
            -p:BaseIntermediateOutputPath=/tmp/obj/ \
            -p:BaseOutputPath=/tmp/bin/

    # 创建版本信息文件
    cat > "${OUTPUT_DIR}/BUILD_INFO.txt" <<EOF
Project: ${PROJECT_NAME}
Target Runtime: ${RID}
.NET Version: ${DOTNET_VERSION}
Self-Contained: ${SELF_CONTAINED}
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

# 主函数
main() {
    echo "=========================================="
    echo "  ${PROJECT_NAME} Podman Build Script"
    echo "=========================================="
    echo ""

    # 无参数时显示帮助
    if [[ $# -eq 0 ]]; then
        show_help
    fi

    # 检查帮助参数
    if [[ "${1:-}" == "-h" ]] || [[ "${1:-}" == "--help" ]]; then
        show_help
    fi

    check_podman

    # 解析参数
    local PLATFORM="$1"
    local MODE="${2:-}"

    # 验证平台参数
    if [[ "${PLATFORM}" != "all" ]] && [[ -z "${PLATFORMS[$PLATFORM]:-}" ]]; then
        log_error "不支持的平台: ${PLATFORM}"
        log_info "支持的平台: ${!PLATFORMS[*]} all"
        exit 1
    fi

    # 验证模式参数
    if [[ -n "${MODE}" ]] && [[ "${MODE}" != "--static" ]] && [[ "${MODE}" != "--dynamic" ]]; then
        log_error "不支持的模式: ${MODE}"
        log_info "支持的模式: --static (默认), --dynamic"
        exit 1
    fi

    # 执行构建
    if [[ "${PLATFORM}" == "all" ]]; then
        if [[ -z "${MODE}" ]]; then
            # 笛卡尔积：所有平台 × 所有模式
            log_info "构建所有平台 × 所有模式 (笛卡尔积)"
            for RID in "${!PLATFORMS[@]}"; do
                build_platform "${RID}" "true"   # static
                echo ""
                build_platform "${RID}" "false"  # dynamic
                echo ""
            done
        elif [[ "${MODE}" == "--static" ]]; then
            # 所有平台，仅自包含
            log_info "构建所有平台 (仅自包含)"
            for RID in "${!PLATFORMS[@]}"; do
                build_platform "${RID}" "true"
                echo ""
            done
        else
            # 所有平台，仅依赖运行时
            log_info "构建所有平台 (仅依赖运行时)"
            for RID in "${!PLATFORMS[@]}"; do
                build_platform "${RID}" "false"
                echo ""
            done
        fi
    else
        # 单平台构建
        if [[ "${MODE}" == "--dynamic" ]]; then
            build_platform "${PLATFORM}" "false"
        else
            # 默认自包含
            build_platform "${PLATFORM}" "true"
        fi
    fi

    echo ""
    log_info "所有构建完成！"
    log_info "产物目录: ${OUTPUT_BASE}/"
    ls -la "${OUTPUT_BASE}/"
}

main "$@"
