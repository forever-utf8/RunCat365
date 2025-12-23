# RunCat-Lite

**A lightweight, customizable running cat animation on your Windows Taskbar.**

基于 [Kyome22/RunCat365](https://github.com/Kyome22/RunCat365) 的精简版本。

---

## ✨ 与原版的区别

| 特性 | RunCat 365 (原版) | RunCat-Lite |
|------|------------------|-------------|
| **分发方式** | Microsoft Store | 独立可执行文件 |
| **部署类型** | 依赖外部运行时 | 自包含 Single-File |
| **角色资源** | 内嵌到程序 | 外挂 (可热加载) |
| **Endless Game** | ✅ 内置小游戏 | ❌ 已移除 |
| **WinRT/UWP** | 需要 StartupTask | 纯 Win32 注册表 |
| **界面语言** | English | 简体中文 |
| **右键菜单** | 系统默认样式 | Windows 11 风格 |

### 主要改动

- 🎯 **精简体积** - 移除 Endless Game（`EndlessGameForm.cs`、`Cat.cs`、`Road.cs`、`GameStatus.cs`）及相关游戏资源
- 🔧 **外挂角色** - `runners/` 目录运行时动态扫描，无需重新编译即可添加角色
- 🐧 **容器化构建** - `build.sh` 使用 Podman 在 Linux 下交叉编译，零宿主机污染
- 🇨🇳 **中文本地化** - 右键菜单、系统信息指示器（CPU/内存/存储/网络）全部汉化
- ⚡ **低内存占用** - `CPURepository.cs` 改用 `NtQuerySystemInformation` 替代 `PerformanceCounter`
- 🎨 **现代化 UI** - `ModernMenuRenderer.cs` 实现 Windows 11 风格圆角菜单，自动适配亮/暗主题
- 🚀 **简化启动项** - `LaunchAtStartupManager.cs` 移除 UWP `StartupTask` 依赖，仅使用注册表
- 📦 **移除 UWP 打包** - 删除 `WapForStore` 项目及 MSIX 相关配置

---

## 📦 安装使用

### 下载

从 [Releases](../../releases) 下载最新版本，解压后直接运行 `RunCat-Lite.exe`。

### 系统要求

- Windows 10 version 19041.0 或更高
- 自包含版本无需安装 .NET 运行时

---

## 🎨 自定义角色

在 `runners/` 目录下放入动画文件即可：

```
runners/
├── 00_cat.png              # APNG 动画文件
├── 01_cat_b.png
├── 02_cat_c.png
├── 03_cat_tail.png
├── 10_mock_nyan_cat.png
└── my_custom.gif           # 也支持 GIF 格式
```

### 支持格式

| 格式 | 说明 |
|------|------|
| **APNG** | 动画 PNG，推荐格式，支持透明背景和无损压缩 |
| **GIF** | 传统 GIF 动画，自动提取所有帧 |
| **PNG** | 静态 PNG，作为单帧显示 |

### 命名规则

- 文件名（不含扩展名）即为角色名称，直接显示在菜单中
- 建议使用 `序号_名称.png` 格式便于排序，如 `00_cat.png`、`01_dog.gif`

### 动画要求

- 尺寸建议：宽度 36-112px，高度 36px（与任务栏高度匹配）
- 透明背景：支持 RGBA 透明通道
- 帧率：程序会根据 CPU 使用率动态调整播放速度

每次打开右键菜单时程序会自动扫描 `runners/` 目录，新添加的角色会立即出现。

---

## 🔨 从源码构建

### 前提条件

- Linux / macOS / WSL2 + Podman
- 或 Windows + .NET 9.0 SDK

### 使用构建脚本 (推荐)

```bash
# 构建 Windows x64 版本
./build.sh win-x64

# 构建所有平台
./build.sh all

# 清理旧构建
./build.sh --clean

# 查看帮助
./build.sh --help
```

### 构建特性

- **零宿主机污染** - `bin/`、`obj/`、NuGet 缓存全部在容器内
- **持久化缓存** - `.build-cache/` 目录加速后续构建
- **权限自动修复** - `podman unshare` 确保产物归当前用户所有

### 支持的目标平台

| RID | 描述 |
|-----|------|
| `win-x64` | Windows x64 (Intel/AMD) |
| `win-x86` | Windows x86 (32位) |
| `win-arm64` | Windows ARM64 |

产物输出到 `dist/` 目录，格式：`RunCat-Lite_{RID}_net9.0_{TIMESTAMP}/`

---

## 📄 许可证

本项目遵循 [Apache License 2.0](LICENSE)。

---

## 🙏 致谢

### 原作者

本项目基于 **[Kyome22/RunCat365](https://github.com/Kyome22/RunCat365)** 修改。

<a href="https://github.com/Kyome22/RunCat365/graphs/contributors">
  <img src="https://contrib.rocks/image?repo=Kyome22/RunCat365" />
</a>

### 原版信息

- 原项目: [RunCat 365](https://github.com/Kyome22/RunCat365)
- Microsoft Store: https://apps.microsoft.com/detail/9nw5lpnvwfwj
- 开发者: [Kyome22](https://github.com/Kyome22)
