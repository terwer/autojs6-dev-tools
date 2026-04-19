## 0. 前置准备：完整理解现有项目与 AutoJS6 生态

- [x] 0.1 阅读 C:\Users\Administrator\Documents\myscripts\yxs-day-task\AGENTS.md（AutoJS6 项目上下文、API 规则、开发约束）
- [x] 0.2 阅读 yxs-day-task 项目所有文档（README.md、openspec/project.md、task-config.json 等）
- [x] 0.3 分析 yxs-day-task\capture-current.cmd（截图拉取逻辑）
- [x] 0.4 分析 yxs-day-task\capture-loop.cmd（循环截图逻辑）
- [x] 0.5 分析 yxs-day-task\generate-region-ref（区域生成算法、regionRef 计算规则）
- [x] 0.6 分析 yxs-day-task\matchReferenceTemplate（OpenCV 匹配参数、阈值、orientation 处理）
- [x] 0.7 查阅 AutoJS6 文档：C:\Users\Administrator\Documents\opensouce\AutoJs6-Documentation（json/、api/、docs/）
- [x] 0.8 查阅 AutoJS6 源码：C:\Users\Administrator\Documents\opensouce\AutoJs6（runtime/api/、core/）
- [x] 0.9 提取核心业务逻辑：坐标计算公式、路径处理规则、匹配算法参数、横竖屏处理、regionRef 生成规则
- [x] 0.10 理解 AutoJS6 API 约束：images.findImage()、images.matchTemplate()、UiSelector、requestScreenCapture()、线程限制、权限要求
- [x] 0.11 确认已完整理解 AutoJS6 文档和源码，生成 PHASE0_REFERENCE.md 作为 API 权威参考
- [x] 0.12 MVP 验证完成确认：所有 4 个 MVP 项目已通过编译和运行测试，核心技术栈验证通过，可直接用于实施指导
- [x] 0.13 **架构原则确立**：禁止使用 ADB 命令，必须使用 AdvancedSharpAdbClient 底层 API（已记录至 feedback_adb_api_only.md 和 project_architecture_principles.md 第 8 节）

**MVP 验证项目参考路径：**
- MVP1.AdbScreencap: `C:\Users\Administrator\Documents\myproject\autojs6-dev-tools-mvp\MVP1.AdbScreencap\Program.cs`
  - 技术验证：AdvancedSharpAdbClient.GetFrameBufferAsync（流式截图）、SixLabors.ImageSharp（图像处理）
  - 性能指标：271-333ms（接收 + 解码）、0 警告 0 错误
  - 实施参考：异步截图拉取、Framebuffer stride 处理、uint 类型安全、方法复用模式
  
- MVP2.UiDumpParser: `C:\Users\Administrator\Documents\myproject\autojs6-dev-tools-mvp\MVP2.UiDumpParser\Program.cs`
  - 技术验证：AdvancedSharpAdbClient.DeviceClient.DumpScreenAsync（UI 树抓取）、System.Xml.Linq（XML 解析）
  - 性能指标：~1270ms（抓取 + 解析）、0 警告 0 错误
  - 实施参考：异步 UI Dump 拉取、布局容器过滤、bounds 坐标解析、XML 文件保存（带时间戳）
  
- MVP3.Win2DCoordinate: `C:\Users\Administrator\Documents\myproject\autojs6-dev-tools-mvp\MVP3.Win2DCoordinate\`
  - 技术验证：Microsoft.Graphics.Win2D（画布渲染）、WinUI 3（桌面应用）
  - 测试通过：滚轮缩放、右键拖拽、坐标拾取、视图重置
  - 实施参考：CanvasControl 初始化、PointerPressed/PointerMoved/PointerWheelChanged 事件处理、坐标系转换
  
- MVP4.OpenCVBenchmark: `C:\Users\Administrator\Documents\myproject\autojs6-dev-tools-mvp\MVP4.OpenCVBenchmark\MainWindow.xaml.cs`
  - 技术验证：OpenCvSharp4（模板匹配）、TM_CCOEFF_NORMED 算法
  - 性能指标：160-214ms（匹配计算）、置信度 1.0000、0 警告 0 错误
  - 实施参考：异步模板匹配（Task.Run）、实时阈值调整、控件初始化空检查、DispatcherQueue.TryEnqueue

**关键技术要点总结：**
1. **ADB 底层 API 强制原则**（最高优先级）：禁止使用 ADB 命令，必须使用 AdvancedSharpAdbClient 底层 API（参考 feedback_adb_api_only.md）
2. 所有 ADB 操作必须使用异步 API（GetFrameBufferAsync、DumpScreenAsync、Connect）
3. 类型安全优先：使用 uint 匹配原生类型，禁止强制转换
4. 代码复用：方法返回值设计支持组合调用（SingleCapture 返回 Image，ContinuousCapture 复用）
5. 性能优化：移除反射、直接属性访问、后台线程计算（Task.Run）
6. WinUI 3 控件初始化：必须添加空检查防止 NullReferenceException
7. XML 文档注释：所有公共方法必须添加完整的 `<summary>` 和用法示例
8. **坐标系统简化**：直接使用 Framebuffer 实际宽高（横屏 1280x720，竖屏 720x1280），UI Dump 坐标直接匹配，无需旋转转换

## 1. 项目结构与依赖配置

- [x] 1.1 创建 src/Core 类库项目（.NET 8，无 UI 依赖）
- [x] 1.2 创建 src/Infrastructure 类库项目（.NET 8）
- [x] 1.3 配置项目依赖关系：App → Infrastructure → Core
- [x] 1.4 添加 NuGet 依赖到 Core 项目：OpenCvSharp4.Windows、SixLabors.ImageSharp
- [x] 1.5 添加 NuGet 依赖到 Infrastructure 项目：SharpAdbClient
- [x] 1.6 添加 NuGet 依赖到 App 项目：Microsoft.Graphics.Win2D、CommunityToolkit.Mvvm
- [x] 1.7 创建目录结构：Core/{Abstractions,Models,Services,Helpers}、Infrastructure/{Adb,Imaging}、App/{Views,ViewModels,Resources}

## 2. Core 层：数据模型定义

- [x] 2.1 创建 Models/AdbDevice.cs（设备序列号、型号、状态、连接类型）
- [x] 2.2 创建 Models/WidgetNode.cs（控件节点：resource-id、text、content-desc、class、clickable、bounds、package、子节点列表）
- [x] 2.3 创建 Models/CropRegion.cs（裁剪区域：x、y、width、height）
- [x] 2.4 创建 Models/MatchResult.cs（匹配结果：位置、置信度、耗时）
- [x] 2.5 创建 Models/AutoJS6CodeOptions.cs（代码生成选项：模式、阈值、重试次数、超时时间、变量名）

## 3. Core 层：服务接口定义

**核心约束：接口设计必须基于底层 API，不暴露命令执行方法（参考 feedback_adb_api_only.md）**

- [x] 3.1 创建 Abstractions/IAdbService.cs（设备扫描、截图拉取返回实际宽高、UI Dump 拉取、TCP/IP 连接，禁止命令执行方法）
- [x] 3.2 创建 Abstractions/IUiDumpParser.cs（XML 解析、节点过滤、坐标映射，坐标直接匹配 Framebuffer 无需转换）
- [x] 3.3 创建 Abstractions/IOpenCVMatchService.cs（模板匹配、阈值过滤）
- [x] 3.4 创建 Abstractions/ICodeGenerator.cs（图像模式代码生成、控件模式代码生成）

## 4. Infrastructure 层：ADB 通信实现

**核心约束：禁止使用 ADB 命令，必须使用 AdvancedSharpAdbClient 底层 API（参考 feedback_adb_api_only.md）**

- [x] 4.1 创建 Adb/AdbServiceImpl.cs 实现 IAdbService
- [x] 4.2 实现设备扫描功能（使用 `AdbClient.GetDevices()` 底层 API，禁止 `adb devices` 命令）
- [x] 4.3 实现异步截图拉取（使用 `AdbClient.GetFrameBufferAsync()` 底层 API，返回实际宽高，禁止 `adb shell screencap` 命令）
- [x] 4.4 实现异步 UI Dump 拉取（使用 `DeviceClient.DumpScreenAsync()` 底层 API，禁止 `adb shell uiautomator dump` 命令）
- [x] 4.5 实现 TCP/IP 设备连接（使用 `AdbClient.Connect(address)` 底层 API，禁止 `adb connect` 命令）
- [x] 4.6 实现日志输出流（记录 API 调用和结果，不记录命令文本）

## 5. Infrastructure 层：图像处理封装

- [x] 5.1 创建 Imaging/ImageProcessor.cs（PNG 解码、降采样、裁剪、导出）
- [x] 5.2 实现 PNG 解码为 byte[] 或 Stream
- [x] 5.3 实现高分辨率图像降采样（最大 1920x1080，保持宽高比）
- [x] 5.4 实现裁剪区域导出为独立 PNG 文件
- [x] 5.5 实现 JSON 元数据生成（记录偏移量与原图尺寸）

## 6. Core 层：UI 树解析实现

**核心约束：坐标系统基于 Framebuffer 实际宽高，无需旋转转换（参考 project_architecture_principles.md 第 5 节）**

- [x] 6.1 创建 Services/UiDumpParser.cs 实现 IUiDumpParser
- [x] 6.2 实现 XML 解析（System.Xml.Linq 或轻量级解析器）
- [x] 6.3 实现布局容器过滤规则（class 包含 Layout 且无 clickable/text/content-desc → 跳过）
- [x] 6.4 实现 bounds 坐标解析（"[x1,y1][x2,y2]" → Rect(x1, y1, x2-x1, y2-y1)，坐标直接匹配 Framebuffer，无需旋转转换）
- [x] 6.5 实现控件节点树构建（递归解析子节点）
- [x] 6.6 实现容错解析（跳过无效节点、记录警告日志）

## 7. Core 层：OpenCV 模板匹配实现

- [x] 7.1 创建 Services/OpenCVMatchService.cs 实现 IOpenCVMatchService
- [x] 7.2 实现 TM_CCOEFF_NORMED 算法封装
- [x] 7.3 实现异步模板匹配（Task.Run 后台线程计算）
- [x] 7.4 实现多模板匹配（返回所有置信度高于阈值的结果）
- [x] 7.5 实现阈值过滤（仅返回置信度 ≥ 阈值的匹配结果）
- [x] 7.6 实现匹配耗时统计

## 8. Core 层：AutoJS6 代码生成实现

- [x] 8.1 创建 Services/AutoJS6CodeGenerator.cs 实现 ICodeGenerator
- [x] 8.2 实现图像模式代码生成（requestScreenCapture + images.read + images.findImage + click）
- [x] 8.3 实现控件模式代码生成（UiSelector 链：优先 id()，降级 text()/desc()，补充 boundsInside()）
- [x] 8.4 实现路径兼容处理（assets/相对/绝对路径自动切换）
- [x] 8.5 实现重试机制代码生成（for 循环 + sleep）
- [x] 8.6 实现超时机制代码生成（setTimeout）
- [x] 8.7 实现 JS 代码格式化（缩进、换行、注释）

## 9. App 层：Win2D 画布控件实现

- [x] 9.1 创建 Views/CanvasView.xaml 与 CanvasView.xaml.cs
- [x] 9.2 实现 CanvasControl 初始化（Win2D 设备创建）
- [x] 9.3 实现分层渲染管线（CanvasImageLayer + CanvasOverlayLayer）
- [x] 9.4 实现 CanvasBitmap 缓存池（避免重复创建纹理）
- [x] 9.5 实现图像层渲染（位图缩放/平移/旋转）
- [x] 9.6 实现 Overlay 层渲染（控件边界框、匹配结果框、裁剪区域框）
- [x] 9.7 实现 60FPS 渲染优化（仅重绘变化图层）

## 10. App 层：画布交互实现

- [x] 10.1 实现滚轮缩放（以光标为中心，范围 10%-500%）
- [x] 10.2 实现拖拽平移（鼠标右键拖拽）
- [x] 10.3 实现惯性滑动（释放鼠标后继续滑动并衰减）
- [ ] 10.4 实现边界阻尼回弹（超出边界时施加阻尼并回弹）
- [x] 10.5 实现交互式矩形裁剪（拖拽创建、调整顶点/边、Shift 锁定宽高比）
- [ ] 10.6 实现像素坐标拾取（鼠标悬停实时显示坐标）
- [ ] 10.7 实现十字准线锁定（Ctrl 键绘制十字准线）
- [ ] 10.8 实现辅助网格显示（10x10 像素网格，随缩放调整密度）
- [ ] 10.9 实现 90° 步进旋转（R 键旋转，坐标系保持一致）

## 11. App 层：控件边界框渲染与联动

- [x] 11.1 实现控件边界框绘制（按类型着色：蓝色=Text、绿色=Button、橙色=Image、灰色=其他）
- [x] 11.2 实现透明度调节（滑块控制边界框透明度）
- [x] 11.3 实现显示过滤开关（按控件类型显示/隐藏边界框）
- [ ] 11.4 实现 TreeView 点击联动画布（高亮对应控件框、自动滚动）
- [ ] 11.5 实现画布点击联动 TreeView（自动展开并定位节点）
- [ ] 11.6 实现多层嵌套节点展开（自动展开所有父节点）

## 12. App 层：设备管理 UI

**核心约束：所有 ADB 操作必须使用底层 API，日志记录 API 调用而非命令文本（参考 feedback_adb_api_only.md）**

- [x] 12.1 创建 Views/AdbDeviceList.xaml 与 AdbDeviceList.xaml.cs
- [x] 12.2 实现设备列表显示（序列号、型号、状态、连接类型）
- [x] 12.3 实现刷新设备按钮（触发 `AdbClient.GetDevices()` API 调用）
- [x] 12.4 实现设备选择（点击高亮、设置为默认设备）
- [x] 12.5 实现 TCP/IP 连接输入框（IP 地址 + 端口，使用 `AdbClient.Connect()` API）
- [ ] 12.6 实现日志面板（实时流式输出 API 调用结果，不记录命令文本）

## 13. App 层：属性面板与代码预览

- [x] 13.1 创建 Views/PropertyPanel.xaml（显示选中控件完整属性）
- [x] 13.2 实现一键复制坐标按钮
- [x] 13.3 实现一键复制 XPath 按钮
- [x] 13.4 创建 Views/CodePreviewView.xaml（代码预览面板）
- [ ] 13.5 实现 JavaScript 语法高亮
- [x] 13.6 实现手动编辑代码功能
- [x] 13.7 实现一键复制代码按钮（Ctrl+S）
- [ ] 13.8 实现导出为 JS 文件功能（TODO: 需要窗口句柄）

## 14. App 层：MVVM 视图模型实现

- [ ] 14.1 创建 ViewModels/MainViewModel.cs（主窗口状态管理）
- [ ] 14.2 创建 ViewModels/AdbDeviceListViewModel.cs（设备列表状态）
- [ ] 14.3 创建 ViewModels/CanvasViewModel.cs（画布状态：缩放、偏移、旋转、模式）
- [ ] 14.4 创建 ViewModels/PropertyPanelViewModel.cs（属性面板状态）
- [ ] 14.5 实现 INotifyPropertyChanged 绑定
- [ ] 14.6 实现 RelayCommand 命令绑定（截图、拉取 UI 树、生成代码、匹配测试）

## 15. App 层：主窗口与页面布局

- [x] 15.1 创建 Views/MainPage.xaml 主页面布局
- [x] 15.2 实现左侧设备列表区域
- [x] 15.3 实现中央画布区域
- [x] 15.4 实现右侧 TreeView + 属性面板区域
- [x] 15.5 实现底部状态栏（缩放比例、视口偏移、原始尺寸、当前模式）
- [x] 15.6 实现顶部工具栏（截图、拉取 UI 树、生成代码、匹配测试、导出模板）
- [x] 15.7 实现 Mica 背景与 Fluent Design 样式

## 16. App 层：实时匹配测试 UI

- [ ] 16.1 实现阈值滑块（范围 0.50~0.95，实时更新匹配结果）
- [ ] 16.2 实现匹配结果可视化（绘制绿色/黄色/橙色矩形框，标注置信度）
- [ ] 16.3 实现匹配测试报告输出（耗时、坐标、状态、置信度）
- [ ] 16.4 实现选择器验证功能（验证 UiSelector 有效性）
- [ ] 16.5 实现坐标系对齐验证（对比控件边界框与截图位置）
- [ ] 16.6 实现批量模板匹配测试（加载多个模板、生成汇总报告）

## 17. App 层：全局快捷键与状态管理

- [ ] 17.1 实现全局快捷键注册（Ctrl+S、Ctrl+Z、Space、R、F）
- [ ] 17.2 实现模式切换（Space 键切换图像模式/图层模式）
- [ ] 17.3 实现还原视图（R 键重置缩放/平移/旋转）
- [ ] 17.4 实现适配窗口（F 键自动缩放）
- [ ] 17.5 实现撤销操作（Ctrl+Z 撤销裁剪区域）
- [ ] 17.6 实现状态栏实时更新

## 18. 性能优化与异步架构

**核心约束：所有 ADB 操作必须异步执行，使用 CancellationToken 支持超时控制（参考 project_architecture_principles.md 第 4 节）**

- [ ] 18.1 确保所有 ADB 操作使用 async/await（`GetFrameBufferAsync`、`DumpScreenAsync`、`Connect` 等底层 API 调用）
- [ ] 18.2 确保所有 OpenCV 计算使用 Task.Run 后台线程
- [ ] 18.3 确保所有 UI 树解析使用异步解析
- [ ] 18.4 实现 CanvasBitmap 缓存池（复用纹理）
- [ ] 18.5 实现 TreeView UI 虚拟化（仅渲染可见节点）
- [ ] 18.6 实现属性面板懒加载（仅加载当前节点）
- [ ] 18.7 优化分层渲染（阈值滑动仅重算匹配层，不重建图像纹理）

## 19. 错误处理与日志

**核心约束：异常处理基于 API 调用失败，使用 CancellationToken 实现超时控制（参考 feedback_adb_api_only.md）**

- [ ] 19.1 实现 ADB 连接异常捕获与 Toast 提示（捕获 API 调用异常，如 `AdbClient.Connect()` 失败）
- [ ] 19.2 实现操作超时控制（通过 CancellationToken 实现 10 秒超时，参考 adb-device-management/spec.md）
- [ ] 19.3 实现重试机制（最多 3 次）
- [ ] 19.4 实现 UI 树解析容错（跳过无效节点、记录警告日志）
- [ ] 19.5 实现 OpenCV 匹配异常捕获
- [ ] 19.6 实现日志面板输出（记录所有 API 调用结果、执行耗时、错误信息，不记录命令文本）

## 20. 测试与验证

**核心约束：测试必须验证底层 API 调用的正确性，不测试命令执行（参考 feedback_adb_api_only.md）**

- [ ] 20.1 创建 Core.Tests 单元测试项目
- [ ] 20.2 测试 UiDumpParser 解析逻辑（布局容器过滤、坐标映射，验证坐标直接匹配 Framebuffer）
- [ ] 20.3 测试 OpenCVMatchService 匹配算法
- [ ] 20.4 测试 AutoJS6CodeGenerator 代码生成逻辑
- [ ] 20.5 真机联调测试（USB 连接使用 `GetDevices()`、TCP/IP 连接使用 `Connect(address)`）
- [ ] 20.6 测试截图拉取（验证 `GetFrameBufferAsync()` 返回实际宽高）与 UI Dump 拉取（验证 `DumpScreenAsync()` 返回 XML）
- [ ] 20.7 测试交互式裁剪与坐标拾取
- [ ] 20.8 测试控件边界框渲染与双向联动（验证坐标系对齐，横屏 1280x720，竖屏 720x1280）
- [ ] 20.9 测试代码生成与匹配测试
- [ ] 20.10 性能测试（5000+ 节点控件树、60FPS 渲染）

## 21. 文档与部署

- [ ] 21.1 编写 ADB 环境配置文档
- [ ] 21.2 编写编译运行步骤文档
- [ ] 21.3 编写真机联调指南
- [ ] 21.4 编写 Dump 调试技巧文档
- [ ] 21.5 配置 VS 2026 发布配置（Release 模式、自包含部署）
- [ ] 21.6 生成安装包或便携版
