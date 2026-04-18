## Why

现有 AutoJS6 开发工具依赖低效的命令行脚本工作流（capture-current.cmd / capture-loop.cmd / generate-region-ref / matchReferenceTemplate），缺乏可视化交互界面，导致截图裁剪、坐标拾取、模板匹配调试效率极低。需要构建 Windows 原生可视化开发工具包，提供像素级图像处理与控件级 UI 图层分析的双引擎并行能力，彻底替代手动调试流程。

## What Changes

- 新增 WinUI 3 桌面应用，提供 ADB 设备管理、实时截图拉取、交互式裁剪、坐标拾取功能
- 新增图像处理引擎：基于 Win2D 的高性能画布渲染、OpenCV 模板匹配、像素坐标系统
- 新增 UI 图层分析引擎：Android UI 树解析、控件边界框渲染、双向联动选择、属性面板
- 新增 AutoJS6 代码生成器：支持图像模式（images.findImage）和控件模式（UiSelector）双路径生成
- 新增实时匹配测试：免真机验证图像匹配置信度与控件选择器有效性
- 复用现有 cmd 脚本的核心业务逻辑与算法流程，确保业务一致性

## Capabilities

### New Capabilities
- `adb-device-management`: ADB 设备扫描、连接管理、命令执行、日志输出
- `image-processing-engine`: 设备截图拉取、Win2D 画布渲染、交互式裁剪、像素坐标拾取、模板导出
- `ui-layer-analysis-engine`: Android UI 树解析、控件节点过滤、边界框渲染、双向联动、属性面板
- `canvas-interaction`: 滚轮缩放、拖拽平移、旋转查看、视图控制、双图层叠加
- `autojs6-code-generator`: 图像模式代码生成、控件模式代码生成、路径兼容处理、JS 格式化
- `realtime-match-testing`: OpenCV 实时匹配计算、阈值调节、控件选择器验证、结果可视化

### Modified Capabilities
<!-- 无现有能力需要修改 -->

## Impact

- 新增 src/App/ 项目：WinUI 3 UI 层，包含 Views、ViewModels、Resources
- 新增 src/Core/ 项目：核心业务逻辑，包含 AdbService、UiDumpParser、OpenCVMatchService、CodeGenerator
- 新增 src/Infrastructure/ 项目：基础设施层，包含 ADB 通信封装、图像处理封装
- 新增 NuGet 依赖：Microsoft.Graphics.Win2D、OpenCvSharp4.Windows、SixLabors.ImageSharp、SharpAdbClient、CommunityToolkit.Mvvm
- 依赖外部工具：ADB（Android Debug Bridge）需预先配置环境变量
- 兼容性要求：Windows 10/11 (10.0.22621.0+)、.NET 8、VS 2022/2026
