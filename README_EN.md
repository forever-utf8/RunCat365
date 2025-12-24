# RunCat-Lite

**English** | [简体中文](README.md)

**A lightweight running cat animation on your Windows Taskbar that adjusts speed based on CPU usage.**

A streamlined fork of [Kyome22/RunCat365](https://github.com/Kyome22/RunCat365).

<p align="center">
  <img src="docs/images/demo.gif" alt="RunCat-Lite Demo" width="400">
</p>

---

## ✨ Features

- 🐱 **Taskbar Animation** - Cute running cat that speeds up with CPU load
- 🎨 **Windows 11 Style Menu** - Modern rounded menu with auto light/dark theme
- 📊 **System Monitoring** - Shows CPU, Memory, Storage, Network usage
- 🔄 **Hot-reload Characters** - Add APNG/GIF to `runners/` folder, no restart needed
- 🎯 **Smart Coloring** - Monochrome icons auto-adapt to system theme
- 📦 **Portable** - Single executable, config stored in app directory
- ⚡ **Low Resource Usage** - Native APIs for minimal memory footprint

---

## 📦 Installation

### Download

Download the latest version from [Releases](../../releases), extract and run `RunCat-Lite.exe`.

### System Requirements

- Windows 10 version 19041.0 or higher
- `static`: No .NET runtime required (self-contained)
- `dynamic`: Requires [.NET Desktop Runtime 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)

### Version Comparison

| Version | Size | Runtime | Use Case |
|---------|------|---------|----------|
| `static` | ~110MB | None | Ready to use, recommended |
| `dynamic` | ~1MB | System .NET | For .NET users |

> All versions are portable - config files stored in app directory.

---

## 🎨 Custom Characters

Place animation files in the `runners/` directory (Right-click menu → Settings → Open Runners Folder):

```
runners/
├── 00_cat.png              # APNG animation
├── 01_cat_b.png
├── 02_cat_c.png
└── my_custom.gif           # GIF also supported
```

### Supported Formats

| Format | Description |
|--------|-------------|
| **APNG** | Animated PNG, recommended, supports transparency |
| **GIF** | GIF animation, all frames extracted |
| **PNG** | Static PNG, displays as single frame |

### Smart Coloring

For **monochrome animations**, the app auto-colors based on system theme:

| System Theme | Icon Color |
|--------------|------------|
| Light | Dark icons |
| Dark | Light icons |

Colorful animations keep their original colors.

---

## 🔨 Build from Source

### Prerequisites

- Linux / macOS / WSL2 + Podman
- Or Windows + .NET 9.0 SDK

### Build Commands

```bash
# Show help
./build.sh --help

# Self-contained (default, recommended)
./build.sh win-x64

# Framework-dependent
./build.sh win-x64 --dynamic

# Build all platforms (self-contained only)
./build.sh all --static

# Build all platforms (framework-dependent only)
./build.sh all --dynamic

# Build all platforms × all modes (Cartesian product)
./build.sh all
```

### Supported Platforms

| RID | Description |
|-----|-------------|
| `win-x64` | Windows x64 (Intel/AMD) |
| `win-x86` | Windows x86 (32-bit) |
| `win-arm64` | Windows ARM64 |

---

## 🆚 Differences from Original

| Feature | RunCat 365 | RunCat-Lite |
|---------|------------|-------------|
| Distribution | Microsoft Store | Standalone |
| Deployment | Runtime dependent | Self-contained |
| Characters | Embedded | External hot-load |
| Mini-game | ✅ | ❌ |
| Language | English | Chinese |
| Context Menu | System style | Win11 style |

---

## 📄 License

[Apache License 2.0](LICENSE)

---

## 🙏 Credits

This project is based on **[Kyome22/RunCat365](https://github.com/Kyome22/RunCat365)**.

- Original Project: [RunCat 365](https://github.com/Kyome22/RunCat365)
- Developer: [Kyome22](https://github.com/Kyome22)
