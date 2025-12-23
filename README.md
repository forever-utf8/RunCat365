# RunCat-Lite

[English](README_EN.md) | **简体中文**

**轻量级任务栏奔跑猫动画，根据 CPU 使用率动态调整奔跑速度。**

基于 [Kyome22/RunCat365](https://github.com/Kyome22/RunCat365) 重构的精简版本。

<p align="center">
  <img src="docs/images/demo.gif" alt="RunCat-Lite Demo" width="400">
</p>

---

## ✨ 特性

- 🐱 **任务栏动画** - 可爱的奔跑猫根据 CPU 负载调整速度
- 🎨 **Windows 11 风格菜单** - 现代化圆角菜单，自动适配亮/暗主题
- 📊 **系统监控** - 显示 CPU、内存、存储、网络使用情况
- � **热加载角色** - 直接在 `runners/` 目录添加 APNG/GIF，无需重启
- � **智能着色** - 单色图标自动适配系统主题
- � **绿色便携** - 单文件运行，无需安装
- � **低资源占用** - 使用原生 API，内存占用极低

---

## 📦 安装使用

### 下载

从 [Releases](../../releases) 下载最新版本，解压后直接运行 `RunCat-Lite.exe`。

### 系统要求

- Windows 10 version 19041.0 或更高
- `portable` / `installed-self` 版本：无需安装 .NET 运行时（自包含）
- `installed` 版本：需要安装 [.NET Desktop Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

### 版本选择

| 版本 | 体积 | 运行时依赖 | 配置位置 | 适用场景 |
|------|------|-----------|----------|----------|
| `portable` | ~110MB | 无 | 程序目录 | U盘便携、绿色版 |
| `installed-self` | ~110MB | 无 | AppData | 固定安装 |
| `installed` | ~1MB | 需系统 .NET | AppData | 已装 .NET 用户 |

---

## 🎨 自定义角色

在 `runners/` 目录下放入动画文件即可（右键菜单 → 设置 → 打开角色文件夹）：

```
runners/
├── 00_cat.png              # APNG 动画文件
├── 01_cat_b.png
├── 02_cat_c.png
└── my_custom.gif           # 也支持 GIF 格式
```

### 支持格式

| 格式 | 说明 |
|------|------|
| **APNG** | 动画 PNG，推荐，支持透明背景 |
| **GIF** | GIF 动画，自动提取所有帧 |
| **PNG** | 静态 PNG，单帧显示 |

### 智能着色

对于**单色动画**，程序会自动根据系统主题着色：

| 系统主题 | 图标颜色 |
|----------|----------|
| 亮色主题 | 深色图标 |
| 暗色主题 | 浅色图标 |

彩色动画保持原始颜色不变。

---

## 🔨 从源码构建

### 前提条件

- Linux / macOS / WSL2 + Podman
- 或 Windows + .NET 9.0 SDK

### 构建命令

```bash
# 便携版（默认）
./build.sh win-x64 portable

# 安装版（自包含）
./build.sh win-x64 installed-self

# 安装版（需系统 .NET）
./build.sh win-x64 installed

# 构建所有平台
./build.sh all portable

# 查看帮助
./build.sh --help
```

### 支持平台

| RID | 描述 |
|-----|------|
| `win-x64` | Windows x64 (Intel/AMD) |
| `win-x86` | Windows x86 (32位) |
| `win-arm64` | Windows ARM64 |

---

## 🆚 与原版的区别

| 特性 | RunCat 365 | RunCat-Lite |
|------|------------|-------------|
| 分发方式 | Microsoft Store | 独立可执行文件 |
| 部署类型 | 依赖运行时 | 自包含单文件 |
| 角色资源 | 内嵌 | 外挂热加载 |
| 小游戏 | ✅ | ❌ |
| 界面语言 | English | 简体中文 |
| 右键菜单 | 系统样式 | Win11 风格 |

---

## 📄 许可证

[Apache License 2.0](LICENSE)

---

## 🙏 致谢

本项目基于 **[Kyome22/RunCat365](https://github.com/Kyome22/RunCat365)** 修改。

- 原项目: [RunCat 365](https://github.com/Kyome22/RunCat365)
- 开发者: [Kyome22](https://github.com/Kyome22)
