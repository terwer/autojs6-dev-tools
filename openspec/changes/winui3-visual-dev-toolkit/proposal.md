## Why

现有 AutoJS6 开发工具依赖低效的命令行脚本工作流（capture-current.cmd / capture-loop.cmd / generate-region-ref / matchReferenceTemplate），缺乏可视化交互界面，导致截图裁剪、坐标拾取、模板匹配调试效率极低。需要构建 Windows 原生可视化开发工具包，提供像素级图像处理与控件级 UI 图层分析的双引擎并行能力，彻底替代手动调试流程。

**核心受益项目：** C:\Users\Administrator\Documents\myscripts\yxs-day-task（英雄杀日常任务自动化脚本），该项目是本工具的直接使用方，其开发流程、模板管理、坐标调试需求直接驱动本工具设计。

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

**关键参考资源（实施前必须完整理解）：**
- 现有脚本工作流：C:\Users\Administrator\Documents\myscripts\yxs-day-task（capture-current.cmd、capture-loop.cmd、generate-region-ref、matchReferenceTemplate）
- 受益项目文档：C:\Users\Administrator\Documents\myscripts\yxs-day-task\AGENTS.md（AutoJS6 开发约束、API 规则、业务逻辑）
- AutoJS6 官方文档：C:\Users\Administrator\Documents\opensouce\AutoJs6-Documentation（json/、api/、docs/）
- AutoJS6 源码：C:\Users\Administrator\Documents\opensouce\AutoJs6（runtime/api/、core/）

**前置分析报告参考优先级：**

本项目在前置准备阶段（任务 0.1-0.11）生成了两份关键分析报告，实施时必须严格遵守以下优先级：

1. **PHASE0_REFERENCE.md（最高优先级 - API 定义层）**
   - 来源：AutoJS6 官方文档和源码的第一手信息
   - 内容：API 级别的技术约束、函数签名、参数规则、生命周期管理、默认常量
   - 用途：定义 WinUI 3 工具必须遵守的 API 边界和技术限制
   - 示例：图像对象回收规则、Rhino 引擎限制、坐标系统约定、OpenCV 算法参数

2. **PHASE0_ANALYSIS.md（次高优先级 - 业务实现层）**
   - 来源：yxs-day-task 项目的业务逻辑分析
   - 内容：现有实现的算法流程、参数选择、工作流设计、业务规则
   - 用途：指导工具设计的架构原则和功能需求
   - 示例：锚点构建算法、多容差搜索策略、regionRef 生成规则、双引擎独立架构

**冲突处理原则：**
- 当 API 约束与业务逻辑冲突时，以 PHASE0_REFERENCE.md 的 API 约束为准
- 业务逻辑需要调整以符合 API 技术限制
- 例如：Rhino 引擎禁止循环体内使用 const/let 是 API 约束，代码生成器必须强制遵守，即使业务代码希望使用现代 ES6 语法

**实施建议：**
1. 先读 PHASE0_REFERENCE.md，理解 AutoJS6 API 的技术边界
2. 再读 PHASE0_ANALYSIS.md，理解业务需求和算法设计
3. 实施时，API 约束优先，业务逻辑次之
4. 所有代码生成、坐标计算、图像处理必须符合 API 约束
