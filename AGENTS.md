# AutoJS6 可视化开发工具包 - 项目上下文

## ⚠️ 用户体验优先规则（最高优先级 — 每次实施前必须先阅读并理解）

本项目是面向高频调试与可视化操作场景的桌面工具。处理任何需求、修复、重构、交互调整或代码生成时，必须先从**真实使用者**的角度理解任务，优先保证工具的实用性、稳定性、低学习成本与低操作负担，禁止仅以“功能完成”或“技术上可行”作为交付标准。

强制要求：
- 必须同时从**首次使用者**与**高频重度使用者**两个视角评估方案是否成立。
- 必须优先追求：更少步骤、更少等待、更少打断、更少猜测、更低出错成本。
- 任何设计、文案、布局、反馈、代码预览、保存逻辑与按钮行为，都必须以“帮助用户更快完成当前任务”为第一目标。
- 用户可见内容必须清晰、直接、可立即理解；禁止将内部实现思路、开发任务描述或临时指令直接暴露给用户。
- 每次开始实施前，必须先阅读本文件并理解本条规则；若实现结果与本条规则冲突，视为实现错误。

禁止事项：
- 禁止把“已经实现功能”当作“已经完成体验设计”。
- 禁止采用会明显增加用户操作成本、等待成本、理解成本或维护成本的方案。
- 禁止设计会阻塞 UI、打断连续操作、造成界面抖动、内容裁切或高频误解的交互。
- 禁止输出只能体现开发视角、不能直接帮助用户完成任务的文案、代码或界面结构。

实施时必须优先检查的问题：
- 这个功能是否一眼就能理解用途与下一步操作？
- 这个按钮或入口是否能立即产生价值，而不是制造额外步骤？
- 这个反馈是否非阻塞、可预期，并且不会打断连续操作？
- 复制、保存或导出的结果是否可以直接使用，而不是半成品？
- 当前界面是否稳定、完整、可读，不抖动、不裁切、不崩溃？
- 这个改动是否真正降低了截图、裁剪、匹配、调试与反复试错的成本？

## 目标

本项目是 **Windows 原生可视化开发工具包**，用于替代低效的命令行脚本工作流，为 AutoJS6 开发者提供像素级图像处理与控件级 UI 图层分析的双引擎并行能力。

**核心受益项目：** yxs-day-task（英雄杀日常任务自动化脚本，位于 `$YXS_DAY_TASK_ROOT`）

**相关文档：**
- `openspec/project.md`：开发清单，本文档的执行展开版，包含具体验证规则与开发步骤
- `README.md`：项目说明，包含编译运行步骤、环境配置、使用指南

---

## ⚠️ 双核独立架构死规则（最高优先级 — 高于所有其他规则）

本项目必须严格遵循"双核独立架构"，这是架构级约束，优先级高于任何临时实现或快速跑通的需求。

### 强制规则

1. **图像处理引擎与 UI 图层分析引擎完全解耦**
   - 图像引擎：基于像素/位图（PNG 截图 + OpenCV），输出绝对像素坐标 (x, y, w, h)
   - UI 引擎：基于控件树（uiautomator dump + XML 解析），输出 UiSelector 选择器链
   - 两者数据源、处理管线、渲染逻辑与代码生成路径严禁耦合

2. **分层渲染管线（Win2D）**
   - CanvasImageLayer（底层位图）+ CanvasOverlayLayer（上层控件边界框）
   - 图像缩放/平移/旋转仅影响 ImageLayer，无需重绘 Overlay
   - 控件边界框绘制独立于图像纹理，支持透明度/颜色/显示开关

3. **双路径代码生成严格独立**
   - 图像模式：生成 images.findImage(template, {threshold, region}) + click()
   - 控件模式：生成 id().text().findOne() + click()
   - 禁止混合模式（AutoJS6 不支持同时使用两种匹配方式）

禁止事项：
- 禁止统一引擎处理图像与控件
- 禁止单层混合渲染
- 禁止图像坐标与控件坐标转换耦合
- 禁止为了快速跑通而打破双核独立架构

---

## 项目层依赖关系硬规则（最高优先级）

**强制单向依赖：** App → Infrastructure → Core ← Infrastructure

### 强制要求

1. **Core 为纯类库，无项目内部依赖**
   - 包含纯业务逻辑：AdbService、UiDumpParser、OpenCVMatchService、CodeGenerator
   - 可独立测试
   - 禁止依赖 UI 框架
   - 禁止依赖外部库（外部依赖必须通过 Infrastructure 封装）

2. **Infrastructure 封装外部依赖**
   - 封装 SharpAdbClient、OpenCvSharp4、ImageSharp
   - 隔离技术细节
   - 可被 Core 依赖

3. **App 仅负责 UI 与 MVVM**
   - 不直接依赖外部库
   - 通过 Core 和 Infrastructure 访问业务逻辑

禁止事项：
- 禁止 Core 依赖 Infrastructure
- 禁止 Core 依赖 App
- 禁止循环依赖
- 禁止单体项目设计

---

## 关键参考资源（实施前必须完整理解）

### 环境变量约定（按本机环境替换）

```bash
# 受益项目根目录（英雄杀日常任务自动化脚本）
YXS_DAY_TASK_ROOT="C:\Users\Administrator\Documents\myscripts\yxs-day-task"

# AutoJS6 文档根目录
AUTOJS6_DOCS_ROOT="C:\Users\Administrator\Documents\opensouce\AutoJs6-Documentation"

# AutoJS6 源码根目录
AUTOJS6_SOURCE_ROOT="C:\Users\Administrator\Documents\opensouce\AutoJs6"
```

### 1. 现有脚本工作流（核心参考）
**路径：** `$YXS_DAY_TASK_ROOT`

必须分析的脚本：
- `capture-current.cmd`：截图拉取逻辑
- `capture-loop.cmd`：循环截图逻辑
- `generate-region-ref`：区域生成算法、regionRef 计算规则
- `matchReferenceTemplate`：OpenCV 匹配参数、阈值、orientation 处理

### 2. 受益项目文档（业务约束来源）
**路径：** `$YXS_DAY_TASK_ROOT\AGENTS.md`

该文档定义了：
- AutoJS6 API 使用规则
- 模板裁剪规则（禁止红点、动态元素）
- 横竖屏处理（landscape/portrait）
- regionRef 生成规则（必须用 generate-region-ref 工具）
- 图像识别 OOM 预防规则
- 坐标系对齐策略

### 3. AutoJS6 官方文档（API 发现与引用）
**路径：** `$AUTOJS6_DOCS_ROOT`

- `json/`：用于 API 发现（快速定位模块、方法名、参数结构）
- `api/`：用于引用（提取用法、参数、返回值、版本标签、示例）
- `docs/`：用于辅助阅读

### 4. AutoJS6 源码（最终定论层）
**路径：** `$AUTOJS6_SOURCE_ROOT`

关键源码根：
- `app\src\main\java\org\autojs\autojs\runtime\api\augment\`：API 实现
- `app\src\main\java\org\autojs\autojs\core\accessibility\`：无障碍桥接层
- `app\src\main\java\org\autojs\autojs\core\activity\`：前台应用检测

**强制规则：文档与源码冲突时，以源码为准。**

---

## AutoJS6 代码生成约束（最高优先级）

生成的 AutoJS6 代码必须严格遵循以下约束：

### 1. Rhino 引擎 const/let 必坑规则
AutoJS6 基于 Mozilla Rhino 引擎，**对 `const`/`let` 块级作用域支持不完整**。

强制规则：
- **循环体内禁止 `const`/`let`** — 必须用 `var`
- **函数顶层 `const`/`let` 可以用**（不在循环体内即可）
- **模块顶层 `const`/`let` 可以用**（如 `require` 声明）

错误示例：
```javascript
// ❌ 致命：Rhino 下 result 在第二次迭代仍保持第一次的值
while (true) {
    const result = computeSomething();
    process(result);
}
```

正确示例：
```javascript
// ✅ var 在循环体内每次迭代正确重新赋值
while (true) {
    var result = computeSomething();
    process(result);
}
```

### 2. 图像识别 OOM 预防规则
生成的代码必须默认把 **OOM 预防** 当成顶层约束。

强制要求：
- **单轮只允许一张截图对象**，禁止同一轮重复 `captureScreen()`
- **场景识别必须按目标最小化**，禁止"每一轮扫描所有 scene 模板"
- **场景识别必须分阶段短路**：先弹窗、再首页、再目标相关页面、最后才是兜底页面
- **模板匹配优先 region，小区域优先于全屏**
- **凡是临时 ImageWrapper / resize 结果 / clip 结果，必须及时回收**

禁止事项：
- 禁止在一轮场景识别里把所有 assets 全扫一遍
- 禁止默认多尺度
- 禁止在循环里无节制 `images.read + images.resize + images.matchTemplate`

### 3. 模板裁剪规则
生成代码时，必须提示用户遵循模板裁剪规则：

优先保留：
- 文字主体
- 图标主体
- 固定边框主体

默认排除：
- 红点
- 数字
- 倒计时
- 滚动公告
- 金币/元宝等动态数值
- 会因状态变化而改变的局部画面

### 4. 横竖屏处理规则
生成代码时，必须支持并显式约定 `orientation`：

- **默认方向是横屏（landscape）**
- **游戏内页面默认横屏**
- **QQ 登录拉起的外部页面默认竖屏**

### 5. regionRef 生成规则
生成代码时，必须提示用户：

- **新增或调整 `regionRef` 时，必须优先使用 `generate-region-ref` 工具**
- 禁止凭感觉手填或拍脑袋猜测
- 推荐入口：`.\generate-region-ref.cmd -Template assets\\xxx.png -Serial <serial>`

---

## 性能与工程要求（最高优先级）

### 1. 异步架构死规则
**所有 I/O 操作必须异步，严禁阻塞 UI 线程。**

强制要求：
- ADB 拉取、OpenCV 计算、Dump 解析、纹理上传全部 async/await
- ADB 截图拉取耗时 200-500ms，必须异步避免 UI 冻结
- OpenCV 模板匹配耗时 50-200ms，必须后台线程计算

### 2. 内存优化规则
- Win2D 使用 CanvasBitmap 缓存池，避免重复创建纹理
- 阈值滑动时仅重算匹配层，不重建图像纹理
- 控件树支持 5000+ 节点扁平化渲染，TreeView 启用 UI 虚拟化

### 3. 渲染性能规则
- 确保 60FPS 流畅渲染
- 分层渲染仅重绘变化图层
- 启用 GPU 加速

### 4. 模块规模规则
- 运行时、feature、action 模块的目标上限是 **255 行**
- 运行时、feature、action 模块的硬上限是 **512 行**
- 超过 512 行时，视为结构违规，必须拆分

---

## 坐标系对齐策略（最高优先级）

**图像坐标系与 Dump 坐标系均采用左上角原点。**

强制规则：
- Android bounds 格式固定为左上角+右下角坐标
- bounds="[x1,y1][x2,y2]" 直接映射为 Rect(x1, y1, x2-x1, y2-y1)
- Win2D 画布默认左上角原点，无需坐标系转换
- 像素坐标与控件坐标直接对应，简化双向联动逻辑

禁止事项：
- 禁止中心点坐标系
- 禁止坐标系转换
- 禁止坐标系不一致导致的对齐问题

---

## 控件树冗余布局容器过滤规则（最高优先级）

**完全忽略布局容器，仅提取最底层具备特征的控件节点。**

强制规则：
- 忽略：LinearLayout、ConstraintLayout、FrameLayout、RelativeLayout
- 提取：TextView、ImageView、Button、Switch、RecyclerView Item
- 过滤规则：class 包含 Layout 且无 clickable/text/content-desc 属性 → 跳过
- 过滤后可减少 70%+ 冗余节点

理由：
- AutoJS6 UiSelector 不依赖布局容器，仅需可交互/可识别控件
- 布局容器无 text/content-desc/resource-id，无业务价值

---

## 开发流程真实资源规则（最高优先级）

### 1. 禁止 mock / 禁止私造结果
强制要求：
- 所有业务结论都必须来自当前真实运行时、真实模板、真实截图、真实 AutoJs6 能力
- 如果检测不到，就应该老实表现为"未接管"或"等待"，而不是伪造一个"像是检测到了"的结果

禁止事项：
- 禁止 mock
- 禁止 fake detection
- 禁止为了让流程继续跑而手工拼一个假的 `found=true`
- 禁止为了点击成功而伪造 `clickX / clickY`

### 2. 资源文件同步规则
- 资源文件未同步到设备时，必须先修复**开发流程 / 同步流程 / 复制流程**
- 禁止为了赶结论，把真实 PNG 模板临时改成 base64 内联、mock 数据、伪资源替身

---

## 输出规则（强制）

### 1. 代码生成输出规则
生成的 AutoJS6 代码必须：
- 符合 Rhino 引擎约束（循环体内用 var）
- 符合 OOM 预防规则（单轮单截图、region 优先）
- 包含必要注释（说明关键步骤）
- 格式化（2 空格缩进、换行清晰）

### 2. 坐标输出规则
- 坐标系原点严格左上角
- 像素坐标格式：(x, y, w, h)
- 控件坐标格式：bounds="[x1,y1][x2,y2]"

### 3. 路径输出规则
- 路径分隔符统一为正斜杠 /
- 支持 assets/相对/绝对路径自动切换

---

## 一句话工作准则

**双核独立、异步非阻塞、60FPS 流畅、严格遵循 AutoJS6 约束、复用现有脚本逻辑。**
