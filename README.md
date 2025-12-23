# RunCat-Lite

**A lightweight, customizable running cat animation on your Windows Taskbar.**

基于 [Kyome22/RunCat365](https://github.com/Kyome22/RunCat365) 的精简版本。

---

## ✨ 与原版的区别

| 特性 | RunCat 365 (原版) | RunCat-Lite |
|------|------------------|-------------|
| **目标框架** | .NET 9.0 | .NET 8.0 |
| **分发方式** | Microsoft Store | 独立可执行文件 |
| **部署类型** | 依赖运行时 | 自包含 (Single-File) |
| **角色资源** | 内嵌 | 外挂 (可自定义) |
| **Endless Game** | ✅ | ❌ (已移除) |
| **WinRT 依赖** | 需要 | 不需要 |
| **界面语言** | English | 中文 |

### 主要改动

- 🎯 **精简体积** - 移除 Endless Game 功能和相关资源
- 🔧 **外挂角色** - `runners/` 目录可自由添加/替换角色动画
- 🐧 **Linux 构建** - 使用 Podman 容器化构建，零宿主机依赖
- 🇨🇳 **中文界面** - 菜单和提示信息汉化
- ⚡ **低内存** - 使用 NtQuerySystemInformation 替代 PerformanceCounter

---

## 📦 安装使用

### 下载

从 [Releases](../../releases) 下载最新版本，解压后直接运行 `RunCat-Lite.exe`。

### 系统要求

- Windows 10 version 19041.0 或更高
- 自包含版本无需安装 .NET 运行时

---

## 🎨 自定义角色

在 `runners/` 目录下创建新文件夹，放入图标文件即可：

```
runners/
├── cat/                    # 内置角色
│   ├── light_0.ico
│   ├── light_1.ico
│   ├── ...
│   ├── dark_0.ico
│   └── ...
├── myrunner/               # 自定义角色
│   ├── light_0.ico         # 浅色主题帧 0
│   ├── light_1.ico         # 浅色主题帧 1
│   ├── dark_0.ico          # 深色主题帧 0
│   └── dark_1.ico          # 深色主题帧 1
```

**命名格式**: `{themeName}_{frameIndex}.ico`
- `themeName`: `light` 或 `dark`
- `frameIndex`: 从 0 开始的帧序号

程序会在打开"角色"菜单时自动扫描并加载新角色。

---

## 🔨 从源码构建

### 前提条件

- Linux 系统 + Podman
- 或 Windows + .NET 8.0 SDK

### 使用构建脚本 (推荐)

```bash
# 构建 Windows x64 版本
./build.sh win-x64

# 构建所有平台
./build.sh all

# 查看帮助
./build.sh --help
```

构建产物输出到 `dist/` 目录。

### 支持的目标平台

- `win-x64` - Windows x64
- `win-x86` - Windows x86
- `win-arm64` - Windows ARM64

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
