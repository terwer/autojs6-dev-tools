## Context

现有项目已完成 WinUI 3 脚手架初始化（VS 2026），基础工程结构就绪。

**现有 cmd 脚本参考路径：** C:\Users\Administrator\Documents\myscripts\yxs-day-task  
该目录包含完整的命令行工作流（capture-current.cmd / capture-loop.cmd / generate-region-ref / matchReferenceTemplate），其截图、区域生成、匹配逻辑必须作为核心参考，直接复用业务逻辑与算法流程。

**核心受益项目：** yxs-day-task（英雄杀日常任务自动化脚本）  
该项目是本工具的直接使用方，其 AGENTS.md 文档详细定义了 AutoJS6 开发约束、API 使用规则、模板裁剪规则、横竖屏处理、regionRef 生成规则、图像识别 OOM 预防等关键业务逻辑，实施前必须完整理解。

**AutoJS6 生态参考资源：**
- 官方文档：C:\Users\Administrator\Documents\opensouce\AutoJs6-Documentation（json/ 用于 API 发现，api/ 用于引用，docs/ 用于辅助阅读）
- 源码：C:\Users\Administrator\Documents\opensouce\AutoJs6（runtime/api/augment/、core/accessibility/、core/activity/ 用于最终定论）
- 强制规则：文档与源码冲突时以源码为准，涉及 images API、UiSelector、前台应用检测、权限、线程限制时必须查源码

**前置分析报告（任务 0.1-0.11）：**
- **PHASE0_REFERENCE.md**（最高优先级）：AutoJS6 API 权威参考，定义所有技术约束和 API 边界
  - 图像生命周期管理（recycle() 规则）
  - Rhino 引擎限制（循环体内禁止 const/let）
  - 核心 API 签名和参数（images.findImage、images.matchTemplate、UiSelector）
  - 源码常量（DEFAULT_COLOR_THRESHOLD=4、DEFAULT_IMAGE_SIMILARITY_METRIC="mssim"）
  - C# 实现映射表（AutoJS6 API → C# 类/方法）
- **PHASE0_ANALYSIS.md**（次高优先级）：yxs-day-task 业务逻辑分析，提供算法和设计参考
  - 锚点构建算法（16 个分散锚点、局部对比度排序）
  - 多容差搜索策略（[24, 40, 64] 步进）
  - regionRef 生成规则（padding=20、参考分辨率 1280x720/720x1280）
  - 双引擎独立架构原则
- **冲突处理**：API 约束优先于业务逻辑，所有实现必须符合 PHASE0_REFERENCE.md 定义的技术限制

约束条件：
- 必须严格遵循"双核独立架构"：图像处理引擎与 UI 图层分析引擎完全解耦
- 项目层依赖关系强制单向：App → Infrastructure → Core ← Infrastructure
- 技术栈锁定：C# 12 / .NET 8 / WinUI 3 / Win2D / OpenCvSharp4 / SharpAdbClient
- 性能要求：60FPS 渲染、5000+ 节点控件树、异步非阻塞 UI

## Goals / Non-Goals

**Goals:**
- 提供像素级图像处理与控件级 UI 图层分析的双引擎并行能力
- 实现交互式裁剪、坐标拾取、模板匹配、控件树解析的可视化操作
- 生成可直接替换现有 cmd 工作流的 AutoJS6 代码
- 确保 60FPS 流畅渲染与异步非阻塞架构
- 复用现有 cmd 脚本的核心业务逻辑与算法流程

**Non-Goals:**
- 不支持 iOS 设备或非 Android 平台
- 不实现 AutoJS6 脚本运行时或调试器
- 不提供云端存储或多设备协同功能
- 不修改现有 cmd 脚本文件

## Decisions

### 1. 双引擎严格独立架构

**决策：** 图像处理引擎与 UI 图层分析引擎完全解耦，数据源、处理管线、渲染逻辑与代码生成路径严禁耦合。

**理由：**
- 图像引擎基于像素/位图（PNG 截图 + OpenCV），输出绝对像素坐标 (x, y, w, h)
- UI 引擎基于控件树（uiautomator dump + XML 解析），输出 UiSelector 选择器链
- 两者数据源、坐标系、匹配算法完全不同，强制解耦避免逻辑混乱

**替代方案：** 统一引擎处理图像与控件 → 被拒绝，因坐标系转换复杂且违背单一职责原则

### 2. 分层渲染管线（Win2D）

**决策：** 采用 CanvasImageLayer（底层位图）+ CanvasOverlayLayer（上层控件边界框）双图层架构。

**理由：**
- 图像缩放/平移/旋转仅影响 ImageLayer，无需重绘 Overlay
- 控件边界框绘制独立于图像纹理，支持透明度/颜色/显示开关
- 分层渲染确保 60FPS 无撕裂，阈值滑动时仅重算匹配层

**替代方案：** 单层混合渲染 → 被拒绝，因每次交互需重建完整纹理，性能不可接受

### 3. 控件树冗余布局容器过滤规则

**决策：** 完全忽略布局容器（LinearLayout/ConstraintLayout/FrameLayout/RelativeLayout），仅提取最底层具备特征的控件节点（TextView/ImageView/Button/Switch/RecyclerView Item）。

**理由：**
- AutoJS6 UiSelector 不依赖布局容器，仅需可交互/可识别控件
- 布局容器无 text/content-desc/resource-id，过滤后可减少 70%+ 冗余节点
- 过滤规则：class 包含 Layout 且无 clickable/text/content-desc 属性 → 跳过

**替代方案：** 保留完整控件树 → 被拒绝，因 TreeView 渲染性能不可接受且无业务价值

### 4. 坐标系对齐策略

**决策：** 图像坐标系与 Dump 坐标系均采用左上角原点，bounds="[x1,y1][x2,y2]" 直接映射为 Rect(x1, y1, x2-x1, y2-y1)。

**理由：**
- Android bounds 格式固定为左上角+右下角坐标
- Win2D 画布默认左上角原点，无需坐标系转换
- 像素坐标与控件坐标直接对应，简化双向联动逻辑

**替代方案：** 中心点坐标系 → 被拒绝，因增加转换复杂度且与 Android 原生格式不一致

### 5. 双路径代码生成参数映射

**决策：** 图像模式生成 images.findImage(template, {threshold, region}) + click()，控件模式生成 id().text().findOne() + click()。

**理由：**
- 图像模式依赖像素坐标，需 requestScreenCapture() + images.read() + matchTemplate()
- 控件模式依赖 UiSelector，优先 id()，降级 text()/desc()，补充 boundsInside()
- 两者 API 完全不同，必须独立生成路径
- 生成代码必须严格遵循 yxs-day-task\AGENTS.md 中的 AutoJS6 API 约束（如 Rhino 引擎循环体内禁止 const/let、图像识别 OOM 预防规则）

**替代方案：** 混合模式（图像+控件） → 被拒绝，因 AutoJS6 不支持同时使用两种匹配方式

### 6. 异步架构与内存优化

**决策：** ADB 拉取、OpenCV 计算、Dump 解析、纹理上传全部 async/await，Win2D 使用 CanvasBitmap 缓存池。

**理由：**
- ADB 截图拉取耗时 200-500ms，必须异步避免 UI 冻结
- OpenCV 模板匹配耗时 50-200ms，必须后台线程计算
- Win2D CanvasBitmap 支持 GPU 加速，缓存池避免重复创建纹理

**替代方案：** 同步阻塞 → 被拒绝，因违背 WinUI 3 响应式设计原则

### 7. 项目层依赖关系

**决策：** 强制单向依赖 App → Infrastructure → Core ← Infrastructure，Core 为纯类库无项目内部依赖。

**理由：**
- Core 包含纯业务逻辑（AdbService/UiDumpParser/OpenCVMatchService/CodeGenerator），可独立测试
- Infrastructure 封装外部依赖（SharpAdbClient/OpenCvSharp4），隔离技术细节
- App 仅负责 UI 与 MVVM，不直接依赖外部库

**替代方案：** 单体项目 → 被拒绝，因测试困难且违背清晰架构原则

## Risks / Trade-offs

### [风险] ADB 连接不稳定导致截图/Dump 拉取失败
**缓解措施：** 实现重试机制（最多 3 次），超时设置 5 秒，异常捕获后 Toast 提示用户检查设备连接

### [风险] OpenCV 模板匹配误报/漏报
**缓解措施：** 提供阈值滑块（0.50~0.95）实时调节，画布绘制置信度数值，支持多模板匹配降低误报

### [风险] 控件树解析失败（XML 格式异常/节点缺失）
**缓解措施：** 容错解析器（跳过无效节点），日志记录解析错误，提供原始 Dump 文本查看面板

### [风险] Win2D 渲染性能不足（大图/高分辨率设备）
**缓解措施：** 图像降采样（最大 1920x1080），分层渲染仅重绘变化图层，启用 GPU 加速

### [风险] 生成代码与现有 cmd 脚本行为不一致
**缓解措施：** 严格复用现有脚本的坐标计算/匹配算法/路径处理逻辑，提供代码预览与手动编辑功能。实施前必须完整理解 yxs-day-task 项目的业务逻辑（AGENTS.md、README.md、openspec/project.md）与现有脚本实现（capture-current.cmd、generate-region-ref、matchReferenceTemplate），确保生成代码符合 AutoJS6 API 约束（Rhino 引擎限制、图像识别 OOM 预防、模板裁剪规则、横竖屏处理、regionRef 生成规则）。

### [权衡] 双引擎独立架构增加代码复杂度
**接受理由：** 解耦带来的可维护性与扩展性收益远大于复杂度成本，且符合单一职责原则

### [权衡] 仅支持 Windows 平台
**接受理由：** WinUI 3 与 Win2D 为 Windows 独占技术，跨平台需重写 UI 层，不在当前目标范围
