#!/bin/bash
# ============================================================
# 未尽之路 - Unity 项目自动化设置脚本
# 功能：检查环境 → 查找Unity → 批处理模式初始化 → 验证结果
# 用法：./setup.sh
# ============================================================

set -euo pipefail

# ==================== 颜色定义 ====================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # 无颜色

# ==================== 全局变量 ====================
# 项目根目录（脚本所在目录）
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT="$SCRIPT_DIR"
LOG_FILE="$PROJECT/setup.log"
UNITY_PATH=""
UNITY_VERSION=""

# 最小磁盘空间要求（10GB，单位KB）
MIN_DISK_SPACE_KB=$((10 * 1024 * 1024))

# ==================== 工具函数 ====================

# 打印信息
info()    { echo -e "${BLUE}[信息]${NC} $1"; }
success() { echo -e "${GREEN}[成功]${NC} $1"; }
warn()    { echo -e "${YELLOW}[警告]${NC} $1"; }
error()   { echo -e "${RED}[错误]${NC} $1"; }
step()    { echo -e "${CYAN}==== $1 ====${NC}"; }

# 分隔线
separator() { echo -e "${CYAN}────────────────────────────────────────${NC}"; }

# ==================== 第一步：检查前置条件 ====================
check_prerequisites() {
    step "第一步：检查前置条件"
    local has_error=0

    # 1. 检查项目目录是否存在
    if [[ ! -d "$PROJECT" ]]; then
        error "项目目录不存在: $PROJECT"
        has_error=1
    else
        success "项目目录存在: $PROJECT"
    fi

    # 2. 检查关键项目文件
    if [[ -f "$PROJECT/ProjectSettings/ProjectVersion.txt" ]]; then
        local current_version
        current_version=$(head -1 "$PROJECT/ProjectSettings/ProjectVersion.txt")
        info "当前项目版本: $current_version"
    else
        warn "未找到 ProjectVersion.txt"
    fi

    # 3. 检查可用磁盘空间
    local disk_info
    disk_info=$(df -k "$PROJECT" | tail -1)
    local available_kb
    available_kb=$(echo "$disk_info" | awk '{print $4}')

    local available_gb=$((available_kb / 1024 / 1024))
    if [[ $available_kb -lt $MIN_DISK_SPACE_KB ]]; then
        warn "可用磁盘空间不足: ${available_gb}GB（建议至少10GB）"
        warn "Unity项目编译和资源缓存可能需要大量空间"
    else
        success "可用磁盘空间: ${available_gb}GB（满足要求）"
    fi

    # 4. 检查 macOS 基本工具
    for cmd in df awk sed; do
        if ! command -v "$cmd" &>/dev/null; then
            error "缺少必要命令: $cmd"
            has_error=1
        fi
    done

    if [[ $has_error -eq 1 ]]; then
        error "前置条件检查未通过，请修复上述问题后重试"
        exit 1
    fi

    success "前置条件检查通过"
    echo ""
}

# ==================== 第二步：查找 Unity 编辑器 ====================
find_unity() {
    step "第二步：查找 Unity 编辑器"

    # 搜索路径列表（按优先级排列）
    local search_paths=(
        "/Applications/Unity/Hub/Editor"          # Unity Hub 标准安装路径
        "$HOME/Applications/Unity/Hub/Editor"      # 用户目录安装
        "/Applications/Unity"                       # 非Hub安装
        "$HOME/Applications/Unity"                  # 用户目录非Hub安装
    )

    # 支持的版本模式：2022.3.x 或 6000.x
    local version_patterns=(
        "6000."    # Unity 6 系列
        "2022.3"   # Unity 2022.3 LTS
    )

    # 遍历搜索路径
    for search_path in "${search_paths[@]}"; do
        if [[ ! -d "$search_path" ]]; then
            continue
        fi

        info "搜索路径: $search_path"

        # 按版本模式查找，优先匹配高版本
        for pattern in "${version_patterns[@]}"; do
            # 查找匹配的版本目录，按版本号降序排列
            local found_versions=()
            while IFS= read -r dir; do
                if [[ -d "$dir" ]]; then
                    found_versions+=("$dir")
                fi
            done < <(find "$search_path" -maxdepth 1 -type d -name "${pattern}*" 2>/dev/null | sort -rV)

            for version_dir in "${found_versions[@]}"; do
                local unity_app="$version_dir/Unity.app"
                if [[ -d "$unity_app" ]]; then
                    UNITY_PATH="$unity_app/Contents/MacOS/Unity"
                    # 从目录名提取版本号
                    UNITY_VERSION="$(basename "$version_dir")"
                    success "找到 Unity $UNITY_VERSION"
                    success "路径: $UNITY_PATH"
                    return 0
                fi
            done
        done
    done

    # 如果标准路径未找到，尝试用 mdfind 全局搜索
    info "标准路径未找到，尝试全局搜索..."
    local mdfind_result
    mdfind_result=$(mdfind "kMDItemCFBundleIdentifier == 'com.unity3d.UnityEditor5.x'" 2>/dev/null | head -5)

    if [[ -n "$mdfind_result" ]]; then
        while IFS= read -r app_path; do
            if [[ -d "$app_path" ]]; then
                UNITY_PATH="$app_path/Contents/MacOS/Unity"
                # 尝试从 Info.plist 获取版本号
                local plist_version
                plist_version=$(/usr/libexec/PlistBuddy -c "Print CFBundleShortVersionString" "$app_path/Contents/Info.plist" 2>/dev/null || echo "未知")
                UNITY_VERSION="$plist_version"
                success "通过全局搜索找到 Unity $UNITY_VERSION"
                success "路径: $UNITY_PATH"
                return 0
            fi
        done <<< "$mdfind_result"
    fi

    # 尝试通过 Unity Hub CLI 安装
    warn "未找到已安装的 Unity 编辑器"
    info "尝试通过 Unity Hub CLI 安装..."

    if try_install_via_hub; then
        return 0
    fi

    # 所有方法都失败，打印手动安装说明
    print_manual_install_instructions
    exit 1
}

# ==================== 尝试通过 Unity Hub CLI 安装 ====================
try_install_via_hub() {
    local hub_path="/Applications/Unity Hub.app"
    local hub_cli="$hub_path/Contents/MacOS/Unity Hub"

    if [[ ! -f "$hub_cli" ]]; then
        warn "未找到 Unity Hub CLI"
        return 1
    fi

    info "检测到 Unity Hub，尝试安装推荐版本..."

    # 优先安装 2022.3 LTS（项目兼容性更好）
    local install_version="2022.3.0f1"

    # Unity Hub CLI 安装编辑器
    if "$hub_cli" --headless install --version "$install_version" --changeset "default" 2>/dev/null; then
        # 安装后重新搜索
        local new_unity_dir="/Applications/Unity/Hub/Editor/$install_version/Unity.app"
        if [[ -d "$new_unity_dir" ]]; then
            UNITY_PATH="$new_unity_dir/Contents/MacOS/Unity"
            UNITY_VERSION="$install_version"
            success "通过 Unity Hub 安装成功: Unity $UNITY_VERSION"
            return 0
        fi
    fi

    warn "Unity Hub CLI 安装失败"
    return 1
}

# ==================== 打印手动安装说明 ====================
print_manual_install_instructions() {
    separator
    error "未能自动安装 Unity 编辑器，请手动安装："
    echo ""
    echo -e "${YELLOW}方法一：通过 Unity Hub 安装（推荐）${NC}"
    echo "  1. 打开 Unity Hub"
    echo "  2. 点击左侧 'Installs'"
    echo "  3. 点击 'Install Editor'"
    echo "  4. 选择 '2022.3 LTS' 或 'Unity 6 (6000.x)' 标签页"
    echo "  5. 选择一个版本并安装（建议包含：WebGL、Windows/Mac 构建支持）"
    echo "  6. 安装完成后重新运行此脚本"
    echo ""
    echo -e "${YELLOW}方法二：从官网下载${NC}"
    echo "  1. 访问 https://unity.com/download"
    echo "  2. 下载 Unity Hub"
    echo "  3. 通过 Hub 安装编辑器"
    echo ""
    echo -e "${YELLOW}方法三：手动指定 Unity 路径${NC}"
    echo "  如果你已安装 Unity 但脚本未检测到，请设置环境变量："
    echo "  export UNITY_PATH_OVERRIDE=\"/你的Unity路径/Unity.app/Contents/MacOS/Unity\""
    echo "  然后重新运行此脚本"
    separator
}

# ==================== 第三步：运行 Unity 批处理模式 ====================
run_unity_batchmode() {
    step "第三步：运行 Unity 批处理模式初始化"

    # 检查环境变量覆盖
    if [[ -n "${UNITY_PATH_OVERRIDE:-}" ]] && [[ -f "$UNITY_PATH_OVERRIDE" ]]; then
        UNITY_PATH="$UNITY_PATH_OVERRIDE"
        info "使用环境变量指定的 Unity 路径: $UNITY_PATH"
    fi

    # 验证 Unity 可执行文件
    if [[ ! -f "$UNITY_PATH" ]]; then
        error "Unity 可执行文件不存在: $UNITY_PATH"
        exit 1
    fi

    if [[ ! -x "$UNITY_PATH" ]]; then
        warn "Unity 可执行文件无执行权限，尝试修复..."
        chmod +x "$UNITY_PATH" 2>/dev/null || {
            error "无法设置执行权限，请手动执行: chmod +x \"$UNITY_PATH\""
            exit 1
        }
    fi

    # 清理旧日志
    if [[ -f "$LOG_FILE" ]]; then
        rm -f "$LOG_FILE"
    fi

    info "Unity 版本: $UNITY_VERSION"
    info "项目路径: $PROJECT"
    info "日志文件: $LOG_FILE"
    echo ""

    # 构建 Unity 命令
    local unity_cmd=(
        "$UNITY_PATH"
        -batchmode
        -projectPath "$PROJECT"
        -executeMethod WeiJinRoad.Editor.ProjectSetup.SetupAll
        -quit
        -logFile "$LOG_FILE"
    )

    info "正在启动 Unity 编辑器（批处理模式）..."
    info "命令: ${unity_cmd[*]}"
    echo ""

    # 启动 Unity 进程
    local unity_pid
    # shellcheck disable=SC2068
    ${unity_cmd[@]} &
    unity_pid=$!

    info "Unity 进程 PID: $unity_pid"

    # 监控进度：实时显示日志
    local last_log_size=0
    local spin_chars=('⠋' '⠙' '⠹' '⠸' '⠼' '⠴' '⠦' '⠧' '⠇' '⠏')
    local spin_idx=0
    local elapsed=0
    local max_wait=600  # 最大等待10分钟

    info "正在监控进度（最长等待${max_wait}秒）..."
    echo ""

    while kill -0 "$unity_pid" 2>/dev/null; do
        # 检查超时
        if [[ $elapsed -ge $max_wait ]]; then
            warn "等待超时（${max_wait}秒），终止 Unity 进程..."
            kill "$unity_pid" 2>/dev/null || true
            error "Unity 批处理超时"
            show_log_tail
            exit 1
        fi

        # 显示旋转动画和日志尾部
        if [[ -f "$LOG_FILE" ]]; then
            local current_size
            current_size=$(wc -c < "$LOG_FILE" 2>/dev/null || echo 0)

            if [[ $current_size -gt $last_log_size ]]; then
                # 有新日志输出，显示最新几行
                local new_lines
                new_lines=$(tail -3 "$LOG_FILE" 2>/dev/null)
                printf "\r${spin_chars[$((spin_idx % 10))]} 处理中... [%ds] %s   " "$elapsed" "${new_lines//$'\n'/ | }"
                last_log_size=$current_size
            else
                printf "\r${spin_chars[$((spin_idx % 10))]} 等待中... [%ds]   " "$elapsed"
            fi
        else
            printf "\r${spin_chars[$((spin_idx % 10))]} 启动中... [%ds]   " "$elapsed"
        fi

        spin_idx=$((spin_idx + 1))
        sleep 2
        elapsed=$((elapsed + 2))
    done

    echo ""

    # 等待进程结束并获取退出码
    wait "$unity_pid" 2>/dev/null
    local exit_code=$?

    if [[ $exit_code -eq 0 ]]; then
        success "Unity 批处理模式执行完成（退出码: 0）"
    else
        error "Unity 批处理模式执行失败（退出码: $exit_code）"
        show_log_tail
        echo ""
        warn "常见原因："
        warn "  - 项目存在编译错误，请检查 C# 脚本"
        warn "  - Unity 版本不兼容"
        warn "  - 缺少必要的包或依赖"
        warn "  - WeiJinRoad.Editor.ProjectSetup.SetupAll 方法不存在"
        echo ""
        info "请查看完整日志: $LOG_FILE"
        exit 1
    fi

    echo ""
}

# ==================== 显示日志尾部 ====================
show_log_tail() {
    if [[ -f "$LOG_FILE" ]]; then
        echo ""
        warn "日志最后30行："
        separator
        tail -30 "$LOG_FILE" 2>/dev/null
        separator
    fi
}

# ==================== 第四步：设置后验证 ====================
post_setup_verification() {
    step "第四步：设置后验证"

    local pass_count=0
    local fail_count=0

    # 1. 检查 MainScene.unity 是否创建
    local scene_found=0
    # 搜索所有可能的场景位置
    local scene_search_paths=(
        "$PROJECT/Assets/Scenes/MainScene.unity"
        "$PROJECT/Assets/Scenes/main.unity"
        "$PROJECT/Assets/MainScene.unity"
    )

    for scene_path in "${scene_search_paths[@]}"; do
        if [[ -f "$scene_path" ]]; then
            success "场景文件已创建: $scene_path"
            scene_found=1
            pass_count=$((pass_count + 1))
            break
        fi
    done

    if [[ $scene_found -eq 0 ]]; then
        # 搜索更广泛的场景文件
        local any_scene
        any_scene=$(find "$PROJECT/Assets" -name "*.unity" -type f 2>/dev/null | head -1)
        if [[ -n "$any_scene" ]]; then
            success "找到场景文件: $any_scene"
            pass_count=$((pass_count + 1))
        else
            warn "未找到场景文件（.unity）"
            fail_count=$((fail_count + 1))
        fi
    fi

    # 2. 检查 .meta 文件是否生成
    local meta_count
    meta_count=$(find "$PROJECT/Assets" -name "*.meta" -type f 2>/dev/null | wc -l | tr -d ' ')

    if [[ $meta_count -gt 0 ]]; then
        success ".meta 文件已生成: ${meta_count} 个"
        pass_count=$((pass_count + 1))
    else
        warn "未找到 .meta 文件（Unity 可能未正确初始化项目）"
        fail_count=$((fail_count + 1))
    fi

    # 3. 检查 Library 目录（Unity 编译缓存）
    if [[ -d "$PROJECT/Library" ]]; then
        success "Library 目录已创建（Unity 编译缓存）"
        pass_count=$((pass_count + 1))
    else
        warn "Library 目录未创建"
        fail_count=$((fail_count + 1))
    fi

    # 4. 检查 Temp 目录
    if [[ -d "$PROJECT/Temp" ]]; then
        success "Temp 目录已创建"
        pass_count=$((pass_count + 1))
    else
        info "Temp 目录未创建（非关键）"
    fi

    # 5. 检查 ProjectSettings 更新
    if [[ -f "$PROJECT/ProjectSettings/ProjectSettings.asset" ]]; then
        local settings_size
        settings_size=$(wc -c < "$PROJECT/ProjectSettings/ProjectSettings.asset" 2>/dev/null || echo 0)
        if [[ $settings_size -gt 100 ]]; then
            success "ProjectSettings 已更新（${settings_size} 字节）"
            pass_count=$((pass_count + 1))
        fi
    fi

    # 6. 检查 Editor 脚本程序集
    local editor_asmdef
    editor_asmdef=$(find "$PROJECT/Assets" -name "WeiJinRoad.Editor.asmdef" -type f 2>/dev/null | head -1)
    if [[ -n "$editor_asmdef" ]]; then
        success "Editor 程序集定义已找到: $editor_asmdef"
        pass_count=$((pass_count + 1))
    else
        info "未找到 WeiJinRoad.Editor.asmdef（可能需要手动创建 Editor 程序集）"
    fi

    echo ""
    info "验证结果: ${pass_count} 通过, ${fail_count} 未通过"
}

# ==================== 更新 ProjectVersion.txt ====================
update_project_version() {
    step "更新 ProjectVersion.txt"

    if [[ -z "$UNITY_VERSION" ]]; then
        warn "未检测到 Unity 版本，跳过更新"
        return
    fi

    local version_file="$PROJECT/ProjectSettings/ProjectVersion.txt"

    if [[ -f "$version_file" ]]; then
        local old_version
        old_version=$(head -1 "$version_file")
        if [[ "$old_version" == "$UNITY_VERSION" ]]; then
            info "ProjectVersion.txt 已是最新版本: $UNITY_VERSION"
            return
        fi
        info "更新版本: $old_version → $UNITY_VERSION"
    else
        info "创建 ProjectVersion.txt: $UNITY_VERSION"
    fi

    # 写入版本信息
    echo "$UNITY_VERSION" > "$version_file"
    # 添加补充信息
    echo "" >> "$version_file"
    echo "m_EditorVersionWithRevision: $UNITY_VERSION (auto-detected by setup.sh)" >> "$version_file"

    success "ProjectVersion.txt 已更新为: $UNITY_VERSION"
}

# ==================== 打印总结 ====================
print_summary() {
    step "设置总结"
    separator

    echo -e "  项目路径:     ${GREEN}$PROJECT${NC}"
    echo -e "  Unity 版本:   ${GREEN}${UNITY_VERSION:-未检测到}${NC}"
    echo -e "  Unity 路径:   ${GREEN}${UNITY_PATH:-未找到}${NC}"
    echo -e "  日志文件:     ${GREEN}$LOG_FILE${NC}"

    if [[ -f "$LOG_FILE" ]]; then
        local log_size
        log_size=$(wc -c < "$LOG_FILE" 2>/dev/null || echo 0)
        echo -e "  日志大小:     ${GREEN}${log_size} 字节${NC}"
    fi

    echo ""
    echo -e "  ${YELLOW}后续步骤:${NC}"
    echo "  1. 在 Unity Hub 中添加项目: 打开 → Add → 选择项目目录"
    echo "  2. 在 Unity 编辑器中打开项目并验证"
    echo "  3. 如需手动运行设置: 菜单 → WeiJinRoad → Project Setup → Setup All"

    separator

    if [[ -n "$UNITY_PATH" ]]; then
        success "设置流程完成！"
    else
        warn "设置流程完成，但未找到 Unity 编辑器，请手动安装后重试"
    fi
}

# ==================== 主流程 ====================
main() {
    echo ""
    separator
    echo -e "${CYAN}  未尽之路 - Unity 项目自动化设置${NC}"
    echo -e "${CYAN}  日期: $(date '+%Y-%m-%d %H:%M:%S')${NC}"
    separator
    echo ""

    # 第一步：检查前置条件
    check_prerequisites

    # 第二步：查找 Unity 编辑器
    find_unity

    # 更新 ProjectVersion.txt（在找到Unity后立即更新）
    update_project_version

    # 第三步：运行 Unity 批处理模式
    run_unity_batchmode

    # 第四步：设置后验证
    post_setup_verification

    # 打印总结
    print_summary
}

# 执行主流程
main "$@"
SCRIPT_EOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-e8d9fb0471e04936b977de1ce4b5f527/cwd.txt'; exit "$__tr_native_ec"