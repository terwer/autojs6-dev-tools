# AutoJS6 Visual Development Toolkit

[English](README.md) | [简体中文](README_zh_CN.md)

<p align="center">
  <img src="docs/images/software-logo.png" alt="AutoJS6 Visual Development Toolkit logo" width="720"/>
</p>

🎯 A development toolkit for AutoJS6 script developers, with visual screenshot analysis, UI widget parsing, image matching preview, and AutoJS6 script code generation.

> **Before you try it, install .NET 8 first**  
> Download: [https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0](https://dotnet.microsoft.com/zh-cn/download/dotnet/8.0)  
> Without .NET 8, the app may fail to start or run correctly on some machines.

## 🎥 Bilibili Quick Start

If you want to understand this tool faster, start with the Bilibili quick-start video. It will walk through .NET 8 installation, first launch, screenshot loading, template cropping, match preview, and AutoJS6 code generation in one complete flow, so you can get from “just downloaded” to “actually usable” with much less trial and error.

**Bilibili quick-start video:** [AutoJS6 Visual Development Toolkit Quick Start](https://space.bilibili.com/)

## 🧭 Which Edition Should You Choose?

> `autojs6-dev-tools` is the Windows-native edition, optimized for a performance-first Windows experience.  
> For the Cross-platform edition, see [`autojs6-dev-tools-plus`](https://github.com/terwer/autojs6-dev-tools-plus).  
> Both projects are original works that I design and maintain as part of the same AutoJS6 toolkit family.

## 😫 Why You May Need It

> **Image recognition takes 20 tries to work? Breaks on different devices?**  
> Use this tool: Real-time match preview • Visual threshold and region adjustment • Auto-generate AutoJS6 code

[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![WinUI 3](https://img.shields.io/badge/WinUI-3-0078D4?logo=windows)](https://microsoft.github.io/microsoft-ui-xaml/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE.txt)

---

## ⚡ See the Toolkit in Action

Bring screenshot analysis, widget inspection, and AutoJS6 code generation into one native Windows workbench. Tune match regions visually, validate selectors against the current UI tree, and export runnable code without bouncing between crop tools, terminals, and repeated device-side trial and error.

<p align="center">
  <img src="docs/images/workbench-demo.gif" alt="Animated demo of the AutoJS6 Visual Workbench switching between image mode and control mode" width="100%"/>
</p>

<p align="center">
  <sub>Live template cropping · Threshold tuning · Widget boundary inspection · AutoJS6-ready code generation</sub>
</p>

## 🖥️ Two Focused Workspaces

<table>
  <tr>
    <td width="50%" align="center" valign="top">
      <img src="docs/images/image-mode.png" alt="Image mode workspace for cropping templates, tuning threshold, and previewing OpenCV matches"/><br/>
      <sub><b>Image Mode</b> · Crop templates, preview OpenCV matches, and export <code>images.findImage()</code>-based AutoJS6 flows.</sub>
    </td>
    <td width="50%" align="center" valign="top">
      <img src="docs/images/control-mode.png" alt="Control mode workspace for parsing UI tree, highlighting widgets, and generating UiSelector code"/><br/>
      <sub><b>Control Mode</b> · Inspect the Android UI hierarchy, highlight widget bounds, and generate selector-based AutoJS6 actions.</sub>
    </td>
  </tr>
</table>

---

## 😫 The Pain You Know Too Well

**Developing AutoJS6 scripts without this tool:**

1. 📸 Screenshot → Manually crop template → Save → Write code → Run on device
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
- ✅ Test scripts on multiple Android devices with different resolutions
- ✅ Use image recognition features frequently
- ✅ Need to manually search UI tree for widget attributes
- ✅ Want to preview matching results without running on device

**You DON'T need this if:**
- ❌ You only use simple fixed-coordinate clicks
- ❌ You never use image matching or widget selectors
- ❌ You enjoy manually debugging 20 times per feature

---

## 🚀 Quick Start

### Prerequisites

- **💻 OS**: Windows 10/11 (Build 22621.0+)
- **⚙️ Runtime**: .NET 8 SDK
- **🛠️ IDE**: Visual Studio 2022/2026 with WinUI 3 workload
- **📱 Tools**: Android Debug Bridge (ADB) in PATH

### Extra tools for local release validation

- **MSBuild + SignTool**: install Visual Studio 2022/2026 or Build Tools with Windows 10/11 SDK
- **Inno Setup 6**: required for building the EXE installer (`ISCC.exe`)
- **Tip**: the local release scripts auto-detect `ISCC.exe`, `msbuild.exe`, and `signtool.exe`, but they now fail with clear prerequisite messages if a tool is missing

### GitHub / proxy note (if GitHub is not directly reachable)

If your network cannot reach GitHub directly, read:

- [`PROXY.md`](PROXY.md)

before trying to:

- `git clone`
- `git push`
- push `.github/workflows/*` and validate Actions

> Important:  
> If `origin` still uses an SSH remote like `git@github.com:...`, setting only `HTTP_PROXY` / `HTTPS_PROXY` is often **not enough**.  
> The simplest default fix is usually: **switch the remote to HTTPS, then configure a Git proxy**.

### 1️⃣ Clone the Repository

```bash
git clone https://github.com/terwer/autojs6-dev-tools.git
cd autojs6-dev-tools
```

### 2️⃣ Install Dependencies

```bash
# Restore NuGet packages
dotnet restore
```

### 3️⃣ Configure Optional Reference Paths

If you need local AutoJS6 API/source lookup, edit `AGENTS.md` to set your local paths:

```bash
AUTOJS6_DOCS_ROOT="C:\path\to\AutoJs6-Documentation"
AUTOJS6_SOURCE_ROOT="C:\path\to\AutoJs6"
```

### 4️⃣ Build and Run

```bash
# Restore solution packages
dotnet restore autojs6-dev-tools.slnx

# Build solution
dotnet build autojs6-dev-tools.slnx

# Run application
dotnet run --project App/App.csproj
```

Or open `autojs6-dev-tools.slnx` in Visual Studio and press F5.

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

## 📁 Project Structure

```
autojs6-dev-tools/
├── App/                        # WinUI 3 desktop application
│   ├── Views/                  # Pages and custom controls
│   ├── ViewModels/             # MVVM view models
│   ├── Services/               # App-layer orchestration services
│   ├── Models/                 # UI-facing models
│   ├── Resources/              # Styles and resource dictionaries
│   ├── CodeTemplates/          # AutoJS6 code generation templates
│   └── App.csproj
├── Core/                       # Pure business logic (no UI dependencies)
│   ├── Abstractions/           # Service interfaces
│   ├── Models/                 # Domain models
│   ├── Services/               # Core business services
│   ├── Helpers/                # Utility classes
│   └── Core.csproj
├── Infrastructure/             # External dependency adapters
│   ├── Adb/                    # ADB communication
│   ├── Imaging/                # OpenCV / imaging wrappers
│   └── Infrastructure.csproj
├── App.Tests/                  # UI/app-level tests
├── Core.Tests/                 # Core unit tests
├── docs/
│   └── images/                 # README screenshots and demo assets
├── openspec/                   # OpenSpec change proposals
├── AGENTS.md                   # Core design principles (AI agent context)
├── autojs6-dev-tools.slnx      # Solution entry
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
3. Review the current repository implementation, tests, and code templates
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

### 🚢 Before Release

- ✅ Run `dotnet build autojs6-dev-tools.slnx -c Release`
- ✅ Run `dotnet test autojs6-dev-tools.slnx -c Release`
- ✅ Run the `manual-release-test` workflow with release upload turned off before merging the release PR
- ✅ Verify the local portable smoke check on `win-x64` before trusting a full release candidate
- ✅ Verify `win-x64` and `win-arm64` both produce ZIP and EXE installer
- ✅ Smoke test the ZIP or EXE on a clean Windows machine before publishing
- ✅ Confirm the generated app name, package identity, and publisher are correct
- ✅ If packaging fails after a release is created, use `manual-release-test` to rebuild and re-upload assets instead of guessing fixes
- ✅ If a production package is broken, prefer a forward fix and a new patch release over mutating an existing production tag

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

## 🎯 Target Users

This toolkit is designed for AutoJS6 developers who need visual screenshot analysis, widget inspection, template matching, and code generation.

---

## 📚 Documentation

- **📘 AGENTS.md**: Core design principles and constraints (read first)
- **📗 openspec/project.md**: Development checklist and verification rules
- **📙 DEVELOPMENT.md**: Release automation, manual packaging test, recovery, and rollback guide
- **📕 DEVELOPMENT_zh_CN.md**: Chinese version of the release and recovery guide
- **📂 openspec/changes/**: OpenSpec change proposals

---

## 📚Realase Test Documentation Entry

If what you need right now is **release validation / GitHub Actions / package verification / release repair**, start with:

- [`RELEASE_TEST.md`](RELEASE_TEST.md)

That page will point you to:

- `manual.md`
- `checklist.md`
- `PROXY.md`
- `DEVELOPMENT.md`

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
- 🐛 [Issue Tracker](https://github.com/terwer/autojs6-dev-tools/issues)
- 💬 [Discussions](https://github.com/terwer/autojs6-dev-tools/discussions)

---

## ☕ Buy Me a Coffee

If this tool saves you hours of tedious work, consider buying me a coffee! Your support keeps this project alive and helps me dedicate more time to adding new features.

**Support via:**

<table>
  <tr>
    <td align="center">
      <img src="https://img.shields.io/badge/WeChat-09B83E?logo=wechat&logoColor=white" alt="WeChat Pay"/><br/>
      <b>WeChat Pay</b><br/>
      <img src="https://static-rs-terwer.oss-cn-beijing.aliyuncs.com/donate/wechat.jpg" alt="wechat" style="width:280px;height:375px;" /><br/>
      <sub>Scan to donate</sub>
    </td>
    <td align="center">
      <img src="https://img.shields.io/badge/Alipay-1677FF?logo=alipay&logoColor=white" alt="Alipay"/><br/>
      <b>Alipay</b><br/>
      <img src="https://static-rs-terwer.oss-cn-beijing.aliyuncs.com/donate/alipay.jpg" alt="alipay" style="width:280px;height:375px;" /><br/>
      <sub>Scan to donate</sub>
    </td>
    <td align="center">
      <img src="https://img.shields.io/badge/爱发电-946CE6?logo=data:image/svg+xml;base64,PHN2ZyB3aWR0aD0iMTYiIGhlaWdodD0iMTYiIHZpZXdCb3g9IjAgMCAxNiAxNiIgZmlsbD0ibm9uZSIgeG1sbnM9Imh0dHA6Ly93d3cudzMub3JnLzIwMDAvc3ZnIj4KPHBhdGggZD0iTTggMEw5LjUgNS41TDE1IDdMOS41IDguNUw4IDE1TDYuNSA4LjVMMSA3TDYuNSA1LjVMOCAwWiIgZmlsbD0id2hpdGUiLz4KPC9zdmc+&logoColor=white" alt="爱发电"/><br/>
      <b>爱发电 (afdian)</b><br/>
      <a href="https://afdian.com/a/terwer">
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

Wait for you

---

**Built with ❤️ for AutoJS6 developers**
