# AutoJS6 Visual Development Toolkit

[English](README.md) | [简体中文](README_zh_CN.md)

🎯 An ADB-based development toolkit for AutoJS6 script developers, with visual screenshot analysis, template matching preview, and code generation.

> **Stop wasting hours on manual screenshot cropping and coordinate guessing**  
> Real-time template matching preview • One-click coordinate picking • Auto-generated AutoJS6 code

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D4?logo=windows)](https://microsoft.github.io/microsoft-ui-xaml/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

---

## 😫 The Pain You Know Too Well

**Developing AutoJS6 scripts without this tool:**

1. 📸 Screenshot device → Open Photoshop/Paint → Manually crop template → Save
2. 📝 Guess coordinates (x: 500? 520? 540?) → Write code → Run on device
3. ❌ Template not found → Adjust crop by 2 pixels → Run again
4. 🔄 Repeat 20 times until it works
5. 📱 Test on another device → Different resolution → Start over
6. 🤔 Threshold 0.8 or 0.85? → Try one by one on real device
7. 🌲 Need resource-id? → Manually search through 5000 UI nodes
8. 💥 Click missed by 10 pixels → Recalculate offset → Run again

**Hours wasted. Every. Single. Day.**

---

## ✨ What This Tool Actually Does

**See template matching results BEFORE running on device:**
- Drag to crop template → Instantly see match confidence (0.95? 0.62?)
- Adjust threshold slider → Watch matches appear/disappear in real-time
- Wrong crop? Adjust 2 pixels → See result immediately
- No more "run → fail → adjust → run" loops

**Pick coordinates with mouse, not guesswork:**
- Hover over screenshot → See exact pixel coordinates (x: 523, y: 187)
- Click to mark → Coordinates copied to clipboard
- Drag rectangle → Get region [x, y, w, h] automatically
- No more "let me try x+10... no wait, x+15..."

**Generate AutoJS6 code automatically:**
- Select template → Click "Generate Code" → Get complete script
- Image mode: `images.findImage()` with correct threshold and region
- Widget mode: `id().text().findOne()` with fallback selectors
- Copy-paste ready, no manual typing

**Test on multiple resolutions without real devices:**
- Load screenshots from 3 devices → Test template on all
- See which resolution fails → Adjust crop once → Works everywhere
- No more "works on my phone but not on user's phone"

---

## 💡 Who Needs This?

**You need this if you:**
- ✅ Spend >30 minutes per day cropping screenshots and adjusting coordinates
- ✅ Test AutoJS6 scripts on multiple Android devices with different resolutions
- ✅ Use `images.findImage()` or `images.matchTemplate()` frequently
- ✅ Manually search UI dump XML for resource-id or text attributes
- ✅ Want to see template matching results without running on device every time

**You DON'T need this if:**
- ❌ You only use simple `click(x, y)` with hardcoded coordinates
- ❌ You never use image-based or selector-based automation
- ❌ You enjoy manually cropping screenshots 20 times per feature

---

## ✨ Features

### 🖼️ Image Processing Engine (Pixel-Level)

- **📸 Real-Time Screenshot Capture**: Pull device screenshots via ADB with one click
- **✂️ Interactive Cropping**: Drag vertices/edges to adjust, Shift to lock aspect ratio
- **🎯 Pixel Coordinate Picker**: Mouse hover shows exact coordinates, Ctrl for crosshair lock
- **🔍 OpenCV Template Matching**: TM_CCOEFF_NORMED algorithm with adjustable threshold (0.50-0.95)
- **💾 Template Export**: Save cropped regions as PNG with offset metadata

### 🌲 UI Layer Analysis Engine (Widget-Level)

- **📱 Android UI Tree Parsing**: Pull and parse uiautomator dump data
- **🧹 Smart Layout Filtering**: Automatically remove 70%+ redundant layout containers
- **🎨 Widget Boundary Rendering**: Color-coded by type (Blue=Text, Green=Button, Orange=Image)
- **🔗 Bidirectional Sync**: Click TreeView → highlight canvas, click canvas → expand TreeView
- **📋 Property Panel**: One-click copy coordinates, text, or XPath expressions

### 🎨 High-Performance Canvas

- **⚡ 60 FPS Rendering**: Win2D GPU-accelerated dual-layer architecture
- **🔍 Zoom & Pan**: Mouse wheel zoom (10%-500%, cursor-centered), drag to pan with inertia
- **🔄 Rotation Support**: 90° step rotation with coordinate system preservation
- **📏 Auxiliary Tools**: Pixel ruler, 10x10 grid, crosshair lock

### 🤖 AutoJS6 Code Generator

**Image Mode** (Pixel-based matching)
```javascript
// Auto-generated AutoJS6 code
requestScreenCapture();
var template = images.read("./assets/login_button.png");
var result = images.findImage(screen, template, {
    threshold: 0.85,
    region: [100, 200, 300, 400]
});
if (result) {
    click(result.x + 150, result.y + 25);
    log("Clicked login button");
}
template.recycle();
```

**Widget Mode** (Selector-based)
```javascript
// Auto-generated AutoJS6 code
var widget = id("com.example:id/login_button").findOne();
if (!widget) widget = text("Login").findOne();
if (!widget) widget = desc("Login Button").findOne();
if (widget) {
    widget.click();
    log("Clicked login button");
}
```

### ⚡ Real-Time Match Testing

- **🎚️ Live Threshold Adjustment**: Slider (0.50-0.95) with instant visual feedback
- **✅ UiSelector Validation**: Test selectors against current UI tree
- **📐 Coordinate Alignment Check**: Verify widget bounds match screenshot pixels
- **📊 Batch Testing**: Load multiple templates, generate summary report

---

## 🚀 Prerequisites

- **💻 OS**: Windows 10/11 (Build 22621.0+)
- **⚙️ Runtime**: .NET 8 SDK
- **🛠️ IDE**: Visual Studio 2022/2026 with WinUI 3 workload
- **📱 Tools**: Android Debug Bridge (ADB) in PATH

## 📦 Quick Start

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/yourusername/autojs6-dev-tools.git
cd autojs6-dev-tools
```

### 2️⃣ Install Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

### 3️⃣ Configure Environment Variables

Edit `AGENTS.md` to set your local paths:

```bash
YXS_DAY_TASK_ROOT="C:\path\to\your\yxs-day-task"
AUTOJS6_DOCS_ROOT="C:\path\to\AutoJs6-Documentation"
AUTOJS6_SOURCE_ROOT="C:\path\to\AutoJs6"
```

### 4️⃣ Build and Run

```bash
# Build solution
dotnet build

# Run application
dotnet run --project src/App
```

Or open `autojs6-dev-tools.slnx` in Visual Studio and press F5.

---

## 📁 Project Structure

```
autojs6-dev-tools/
├── src/
│   ├── App/                    # WinUI 3 UI layer
│   │   ├── Views/              # Pages and custom controls
│   │   ├── ViewModels/         # MVVM view models
│   │   └── Resources/          # Styles and resource dictionaries
│   ├── Core/                   # Pure business logic (no UI dependencies)
│   │   ├── Abstractions/       # Service interfaces
│   │   ├── Models/             # Data models
│   │   ├── Services/           # Core business services
│   │   └── Helpers/            # Utility classes
│   └── Infrastructure/         # Infrastructure layer
│       ├── Adb/                # ADB communication
│       └── Imaging/            # Image processing wrappers
├── tests/                      # Unit tests
├── openspec/                   # OpenSpec change proposals
├── AGENTS.md                   # Core design principles (AI agent context)
└── README.md                   # This file
```

---

## 🏗️ Architecture Principles

### 🔀 Dual-Engine Independence (Strict Isolation)

- **🖼️ Image Engine**: Pixel/bitmap → absolute pixel coordinates (x, y, w, h)
- **🌲 UI Engine**: Widget tree → UiSelector chains (id().text().findOne())
- **🚫 Zero Coupling**: Data sources, processing pipelines, rendering logic, and code generation paths are completely decoupled

### ⬇️ Unidirectional Dependency

```
App → Infrastructure → Core ← Infrastructure
```

- **🎯 Core**: Pure business logic, no UI dependencies, independently testable
- **🔌 Infrastructure**: External dependency wrappers (SharpAdbClient, OpenCvSharp4)
- **🎨 App**: UI and MVVM only

### ⚡ Async-First Architecture

- All I/O operations (ADB, OpenCV, XML parsing, texture upload) use `async/await`
- UI thread never blocked
- Background operations with `CancellationToken` support

---

## 🛠️ Key Technologies

| Component | Technology | Purpose |
|-----------|-----------|---------|
| 🎨 UI Framework | WinUI 3 + Windows App SDK 1.5+ | Native Windows desktop UI |
| 🖼️ Rendering | Microsoft.Graphics.Win2D | 60 FPS GPU-accelerated canvas |
| 🔍 Image Processing | OpenCvSharp4.Windows + SixLabors.ImageSharp | Template matching and image manipulation |
| 📱 ADB Communication | SharpAdbClient | Android device control |
| 🔗 MVVM | CommunityToolkit.Mvvm | View model binding and commands |
| 🏗️ Architecture | Clean Architecture | Layered separation of concerns |

---

## 👨‍💻 Development Workflow

### 📖 Before Implementation

1. Read `AGENTS.md` for core design principles
2. Read `openspec/project.md` for development checklist
3. Analyze existing cmd scripts in `$YXS_DAY_TASK_ROOT`
4. Review AutoJS6 documentation and source code

### 💻 During Implementation

- ✅ Maintain dual-engine independence
- ✅ Follow unidirectional dependency rules
- ✅ Use async/await for all I/O operations
- ✅ Keep modules under 512 lines
- ✅ Write tests for Core layer

### ✔️ Before Commit

- ✅ Verify project layer dependencies (App → Infrastructure → Core)
- ✅ Verify dual-engine isolation
- ✅ Verify async architecture
- ✅ Verify 60 FPS rendering performance
- ✅ Run unit tests

---

## ⚠️ AutoJS6 Code Generation Constraints

Generated code must comply with AutoJS6 runtime constraints:

### 🐛 Rhino Engine Limitations

```javascript
// ❌ WRONG: const/let in loop body (Rhino bug - variable won't rebind)
while (true) {
    const result = computeSomething();
    process(result);  // result keeps first iteration value!
}

// ✅ CORRECT: Use var in loop body
while (true) {
    var result = computeSomething();
    process(result);  // result correctly updates each iteration
}
```

### 💾 OOM Prevention

- **📸 Single screenshot per iteration**: Never call `captureScreen()` multiple times in one loop
- **🎯 Minimize scene detection scope**: Don't scan all templates every iteration
- **📐 Prefer region-based matching**: Use `region: [x, y, w, h]` instead of full-screen
- **♻️ Recycle ImageWrapper objects**: Call `.recycle()` immediately after use

### ✂️ Template Cropping Rules

**✅ Include**: Text, icons, fixed borders  
**❌ Exclude**: Red dots, numbers, countdowns, dynamic values

---

## 🤝 Contributing

We welcome contributions! Please:

1. 🍴 Fork the repository
2. 🌿 Create a feature branch (`git checkout -b feature/amazing-feature`)
3. 📖 Read `AGENTS.md` and `openspec/project.md` thoroughly
4. 🏗️ Follow the architecture principles
5. ✅ Write tests for Core layer changes
6. 💬 Commit with clear messages (`git commit -m 'add amazing feature'`)
7. 🚀 Push to your branch (`git push origin feature/amazing-feature`)
8. 🔀 Open a Pull Request

---

## 🎯 Reference Projects

This toolkit is designed to serve AutoJS6 automation projects, particularly:

- **🎮 yxs-day-task**: Hero Kill daily task automation (primary beneficiary)

---

## 📚 Documentation

- **📘 AGENTS.md**: Core design principles and constraints (read first)
- **📗 openspec/project.md**: Development checklist and verification rules
- **📂 openspec/changes/**: OpenSpec change proposals

---

## 📄 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

## 🙏 Acknowledgments

- [AutoJS6](https://github.com/SuperMonster003/AutoJs6) - Android automation framework
- [WinUI 3](https://microsoft.github.io/microsoft-ui-xaml/) - Modern Windows UI framework
- [Win2D](https://github.com/microsoft/Win2D) - GPU-accelerated 2D graphics
- [OpenCvSharp](https://github.com/shimat/opencvsharp) - OpenCV wrapper for .NET

---

## 💬 Support

- 📖 [Documentation](openspec/)
- 🐛 [Issue Tracker](https://github.com/yourusername/autojs6-dev-tools/issues)
- 💬 [Discussions](https://github.com/yourusername/autojs6-dev-tools/discussions)

---

## ☕ Buy Me a Coffee

If this tool saves you hours of tedious work, consider buying me a coffee! Your support keeps this project alive and helps me dedicate more time to adding new features.

**Support via:**

<table>
  <tr>
    <td align="center">
      <img src="https://img.shields.io/badge/WeChat-09B83E?logo=wechat&logoColor=white" alt="WeChat Pay"/><br/>
      <b>WeChat Pay</b><br/>
      <img src="docs/images/wechat-pay-qr.png" width="150"/><br/>
      <sub>Scan to donate</sub>
    </td>
    <td align="center">
      <img src="https://img.shields.io/badge/Alipay-1677FF?logo=alipay&logoColor=white" alt="Alipay"/><br/>
      <b>Alipay</b><br/>
      <img src="docs/images/alipay-qr.png" width="150"/><br/>
      <sub>Scan to donate</sub>
    </td>
    <td align="center">
      <img src="https://img.shields.io/badge/爱发电-946CE6?logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTggMEw5LjUgNS41TDE1IDdMOS41IDguNUw4IDE1TDYuNSA4LjVMMSA3TDYuNSA1LjVMOCAwWiIgZmlsbD0id2hpdGUiLz4KPC9zdmc+&logoColor=white" alt="爱发电"/><br/>
      <b>爱发电 (afdian)</b><br/>
      <a href="https://afdian.net/@yourusername">
        <img src="https://img.shields.io/badge/Support-爱发电-946CE6?style=for-the-badge" alt="Support on 爱发电"/>
      </a><br/>
      <sub>Monthly sponsorship</sub>
    </td>
  </tr>
</table>

**Your support enables:**
- ⚡ Continuous development and maintenance
- 🎯 New features based on community feedback
- 📚 Professional documentation and video tutorials
- 🛠️ Long-term stability and updates

**Sponsors get:**
- 🌟 Name listed in project credits
- 💬 Direct communication channel
- 🚀 Early access to new features

Every contribution matters. Thank you! 🙏

### 💖 Sponsors

Thanks to the following sponsors for their generous support:

<a href="https://github.com/supporter1"><img src="https://avatars.githubusercontent.com/u/supporter1?s=60" width="60px;" alt=""/></a>
<a href="https://github.com/supporter2"><img src="https://avatars.githubusercontent.com/u/supporter2?s=60" width="60px;" alt=""/></a>
<a href="https://github.com/supporter3"><img src="https://avatars.githubusercontent.com/u/supporter3?s=60" width="60px;" alt=""/></a>
<a href="https://github.com/supporter4"><img src="https://avatars.githubusercontent.com/u/supporter4?s=60" width="60px;" alt=""/></a>
<a href="https://github.com/supporter5"><img src="https://avatars.githubusercontent.com/u/supporter5?s=60" width="60px;" alt=""/></a>
<a href="https://github.com/supporter6"><img src="https://avatars.githubusercontent.com/u/supporter6?s=60" width="60px;" alt=""/></a>

---

**Built with ❤️ for AutoJS6 developers**
