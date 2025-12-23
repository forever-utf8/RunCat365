#!/bin/bash
# RunCat-Lite Podman Build Script
# 零宿主机依赖，零污染，编译产物输出到 dist/

set -e

# 配置
DOTNET_VERSION="9.0"
PROJECT_NAME="RunCat-Lite"
PROJECT_DIR="RunCat365"
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
    local PLATFORM_DESC="${PLATFORMS[$RID]}"
    local TIMESTAMP=$(date +%Y%m%d%H%M%S)
    local OUTPUT_DIR="${OUTPUT_BASE}/${PROJECT_NAME}_${RID}_net${DOTNET_VERSION}_${TIMESTAMP}"

    log_info "开始构建: ${PLATFORM_DESC} (${RID})"

    # 创建输出目录
    mkdir -p "${OUTPUT_DIR}"

    # 使用 podman 运行 .NET SDK 容器进行编译
    # 使用 --userns=keep-id 保持当前用户权限，避免产物归root所有
    # 设置环境变量将所有缓存重定向到容器内 /tmp，避免污染宿主机
    podman run --rm \
        --network=host \
        --userns=keep-id \
        -e HOME=/tmp \
        -e DOTNET_CLI_HOME=/tmp/.dotnet \
        -e NUGET_PACKAGES=/tmp/.nuget/packages \
        -e DOTNET_NOLOGO=1 \
        -v "$(pwd):/src:Z" \
        -w /src \
        "mcr.microsoft.com/dotnet/sdk:${DOTNET_VERSION}" \
        dotnet publish "${PROJECT_DIR}/${PROJECT_DIR}.csproj" \
            -c Release \
            -r "${RID}" \
            --self-contained true \
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
    cat > "${OUTPUT_DIR}/BUILD_INFO.txt" << EOF
Project: ${PROJECT_NAME}
Target Runtime: ${RID}
.NET Version: ${DOTNET_VERSION}
Self-Contained: Yes
Build Time: $(date -Iseconds)
Build Host: $(hostname)
EOF

    # 复制 runners 目录到输出
    cp -r "${PROJECT_DIR}/resources/runners" "${OUTPUT_DIR}/"
    log_info "已复制 runners 目录"

    # 删除多余的 .dll.config 文件（默认值已嵌入到代码中）
    # rm -f "${OUTPUT_DIR}"/*.dll.config
    # log_info "已清理多余的配置文件"

    # 使用 podman unshare 修复文件权限
    # 在 unshare 命名空间中，UID 0 映射回宿主用户
    podman unshare chown -R 0:0 "${OUTPUT_DIR}"
    log_info "已修复文件权限"

    log_info "构建完成: ${OUTPUT_DIR}"

    # 显示产物信息
    log_info "产物文件:"
    ls -lh "${OUTPUT_DIR}/"*.exe 2>/dev/null || ls -lh "${OUTPUT_DIR}/"*.dll | head -5
}

# 清理旧的构建产物
clean_build() {
    log_warn "清理 ${PROJECT_DIR}/bin 和 ${PROJECT_DIR}/obj 目录..."
    rm -rf "${PROJECT_DIR}/bin" "${PROJECT_DIR}/obj"
    log_info "清理完成"
}

# 主函数
main() {
    echo "========================================"
    echo "  ${PROJECT_NAME} Podman Build Script"
    echo "========================================"
    echo ""

    check_podman

    # 解析参数
    local PLATFORM="${1:-win-x64}"
    local DO_CLEAN="${2:-}"

    if [[ "${DO_CLEAN}" == "--clean" ]] || [[ "${PLATFORM}" == "--clean" ]]; then
        clean_build
        if [[ "${PLATFORM}" == "--clean" ]]; then
            exit 0
        fi
    fi

    if [[ "${PLATFORM}" == "all" ]]; then
        for RID in "${!PLATFORMS[@]}"; do
            build_platform "${RID}"
            echo ""
        done
    else
        if [[ -z "${PLATFORMS[$PLATFORM]}" ]]; then
            log_error "不支持的平台: ${PLATFORM}"
            log_info "支持的平台: ${!PLATFORMS[*]}"
            exit 1
        fi
        build_platform "${PLATFORM}"
    fi

    echo ""
    log_info "所有构建完成！"
    log_info "产物目录: ${OUTPUT_BASE}/"
    ls -la "${OUTPUT_BASE}/"
}

# 显示帮助
if [[ "${1:-}" == "-h" ]] || [[ "${1:-}" == "--help" ]]; then
    echo "Usage: $0 [PLATFORM] [--clean]"
    echo ""
    echo "PLATFORM:"
    echo "  win-x64    Windows x64 (默认)"
    echo "  win-x86    Windows x86"
    echo "  win-arm64  Windows ARM64"
    echo "  all        构建所有平台"
    echo ""
    echo "Options:"
    echo "  --clean    清理构建目录"
    echo ""
    echo "Examples:"
    echo "  $0                    # 构建 win-x64"
    echo "  $0 win-x86            # 构建 win-x86"
    echo "  $0 all                # 构建所有平台"
    echo "  $0 --clean            # 仅清理"
    echo "  $0 win-x64 --clean    # 清理后构建"
    exit 0
fi

main "$@"
