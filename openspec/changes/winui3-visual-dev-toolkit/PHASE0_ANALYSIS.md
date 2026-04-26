# 前置准备阶段分析报告

**生成时间：** 2026-04-19  
**任务范围：** 0.1-0.10 前置准备阶段  
**目标：** 深入理解 AutoJS6 自动化项目 项目和 AutoJS6 生态，为 WinUI 3 工具开发奠定基础

---

## 1. 核心业务逻辑提取

### 1.1 截图拉取逻辑（截图拉取能力 / 截图拉取实现）

**核心流程：**
```powershell
1. 解析 ADB 设备序列号（支持多设备场景）
2. 执行 adb exec-out screencap -p 流式读取 PNG
3. 同步获取设备元数据：
   - mCurrentFocus（当前焦点窗口）
   - wm size（分辨率）
   - wm density（密度）
   - dumpsys activity top（顶层 Activity）
4. 保存截图 + 元数据文本
```

**关键技术点：**
- 使用 `exec-out` 而非 `shell screencap` 避免中间文件
- 流式读取避免大图内存溢出
- 元数据与截图配对保存，便于后续分析

### 1.2 区域生成算法（内置 regionRef 计算实现）

**核心算法：**
```javascript
1. 加载模板图和截图为 RGBA 原始数据
2. 构建锚点（Anchors）：
   - 过滤透明像素（alpha < threshold）
   - 计算局部对比度（local contrast）
   - 按对比度 + 亮度 + alpha 排序
   - 选取前 16 个分散锚点
3. 多容差搜索候选位置：
   - 先用严格容差（24）
   - 逐步放宽（40, 64）
   - 找到候选后停止
4. 逐像素评分：
   - 计算匹配像素比例（ratio）
   - 计算平均差异（avgDiff）
   - 要求 ratio >= 0.95
5. 应用 padding 并转换为参考坐标系
```

**参考坐标系约定：**
- 横屏（landscape）：1280x720
- 竖屏（portrait）：720x1280

**关键设计：**
- 基于 ImageMagick 的 RGBA 原始数据处理
- 锚点策略避免全图扫描
- 多容差策略平衡精度和召回率
- 强制 0.95 相似度阈值确保模板来自真实截图

### 1.3 模板匹配逻辑（reference-match-features.js）

**核心流程：**
```javascript
1. 检测 images.matchFeatures API 可用性
2. 提取模板特征（detectAndComputeFeatures）
3. 遍历候选区域（regions）：
   - 提取区域特征
   - 执行特征匹配（matchFeatures）
   - 返回 objectFrame（四角点 + 中心点）
4. 构建 detection 结果：
   - 计算边界框（从四角点）
   - 计算点击坐标（中心点）
   - 标记 regionMode 为 "feature"
```

**关键约束：**
- 仅在 `enableMatchFeatures=true` 时启用
- 作为模板匹配的 fallback 机制
- 自动回收 ImageFeatures 避免内存泄漏

---

## 2. AutoJS6 API 约束与限制

### 2.1 Rhino 引擎限制（最高优先级）

**致命问题：** 循环体内 `const`/`let` 不会重新绑定

```javascript
// ❌ 错误：第二次迭代 result 仍是第一次的值
while (true) {
    const result = computeSomething();
    process(result);
}

// ✅ 正确：使用 var
while (true) {
    var result = computeSomething();
    process(result);
}
```

**强制规则：**
- 循环体内禁止 `const`/`let`，必须用 `var`
- 函数顶层和模块顶层可以用 `const`/`let`

### 2.2 图像识别 OOM 预防规则

**核心约束：**
1. 单轮只允许一张截图对象
2. 场景识别必须按目标最小化
3. 场景识别必须分阶段短路（弹窗 → 首页 → 目标页 → 兜底）
4. 同分辨率优先 1:1 匹配，禁止无脑多尺度
5. 模板匹配优先 region，小区域优先于全屏
6. 临时 ImageWrapper / resize / clip 结果必须及时回收

**禁止事项：**
- 禁止一轮场景识别扫描所有 assets
- 禁止默认多尺度缩放
- 禁止循环内无节制 `images.read + images.resize + images.matchTemplate`

### 2.3 图像对象生命周期

**关键规则：**
```javascript
// 需要手动回收
var img = images.read("./1.png");
// ... 使用图片
img.recycle();

// 例外：captureScreen() 返回的图片不需要回收
var screen = captureScreen();
// 直接使用，无需 recycle
```

**回收检查：**
```javascript
if (img && typeof img.recycle === "function") {
    if (typeof img.isRecycled === "function" && !img.isRecycled()) {
        img.recycle();
    }
}
```

### 2.4 横竖屏处理（orientation）

**默认约定：**
- 游戏内页面默认横屏（landscape）
- QQ 外部授权页默认竖屏（portrait）

**强制要求：**
- 新增模板/scene 必须明确声明 `orientation`
- `regionRef` 必须基于参考分辨率定义
- 禁止把横竖屏问题下沉为业务层兜底补丁

---

## 3. 坐标系统与区域映射

### 3.1 坐标系对齐策略

**统一约定：**
- 图像坐标系与 Dump 坐标系均采用左上角原点
- Android bounds 格式：`[x1,y1][x2,y2]`
- 映射为 Rect：`Rect(x1, y1, x2-x1, y2-y1)`

**参考分辨率：**
- 横屏：1280x720
- 竖屏：720x1280

### 3.2 regionRef 生成规则

**强制流程：**
1. 必须使用 `内置 regionRef 计算` 工具
2. 基于真实模板图 + 当前 ADB 截图生成
3. 禁止凭感觉手填或拍脑袋猜测
4. 工具输出后仍需结合页面方向和业务语义核对

**命令示例：**
```cmd
.\内置 regionRef 计算.cmd -Template assets\xxx.png -Serial emulator-5554
.\内置 regionRef 计算.cmd -Template assets\xxx.png -Screenshot captures\xxx.png
```

### 3.3 模板裁剪规则

**优先保留：**
- 文字主体
- 图标主体
- 固定边框
- 稳定装饰元素

**默认排除：**
- 红点
- 数字
- 倒计时
- 动态数值（元宝、金币等）
- 滚动公告
- 状态变化很大的局部

---

## 4. 核心技术栈映射

### 4.1 现有技术栈（AutoJS6 自动化项目）

| 功能 | 现有实现 | 技术 |
|------|---------|------|
| 截图拉取 | 截图拉取实现 | PowerShell + ADB |
| 图像处理 | 内置 regionRef 计算实现 | Node.js + ImageMagick |
| 模板匹配 | images.matchTemplate | AutoJS6 + OpenCV |
| 特征匹配 | images.matchFeatures | AutoJS6 + OpenCV |
| UI 树解析 | uiautomator dump | ADB + XML |
| 坐标计算 | 手动计算 + regionRef | JavaScript |

### 4.2 目标技术栈（WinUI 3 工具）

| 功能 | 目标实现 | 技术 |
|------|---------|------|
| 截图拉取 | AdbServiceImpl | C# + SharpAdbClient |
| 图像处理 | ImageProcessor | C# + SixLabors.ImageSharp |
| 模板匹配 | OpenCVMatchService | C# + OpenCvSharp4 |
| 特征匹配 | （可选）OpenCVMatchService | C# + OpenCvSharp4 |
| UI 树解析 | UiDumpParser | C# + System.Xml.Linq |
| 坐标计算 | 自动计算 + regionRef | C# |
| 画布渲染 | CanvasView | C# + Win2D |
| 代码生成 | AutoJS6CodeGenerator | C# |

---

## 5. 关键业务规则

### 5.1 双引擎独立架构

**强制解耦：**
- 图像处理引擎：基于像素/位图（PNG 截图 + OpenCV）
- UI 图层分析引擎：基于控件树（uiautomator dump + XML 解析）
- 两者数据源、坐标系、匹配算法完全不同

**禁止耦合：**
- 图像引擎输出绝对像素坐标 (x, y, w, h)
- UI 引擎输出 UiSelector 选择器链
- 禁止混合使用或相互依赖

### 5.2 控件树冗余布局容器过滤规则

**过滤规则：**
- 完全忽略布局容器（LinearLayout/ConstraintLayout/FrameLayout/RelativeLayout）
- 仅提取最底层具备特征的控件节点（TextView/ImageView/Button/Switch/RecyclerView Item）
- 过滤条件：class 包含 Layout 且无 clickable/text/content-desc 属性

**收益：**
- 减少 70%+ 冗余节点
- 提升 TreeView 渲染性能
- 符合 AutoJS6 UiSelector 实际需求

### 5.3 双路径代码生成参数映射

**图像模式：**
```javascript
requestScreenCapture();
var img = images.read("./assets/template.png");
var screen = captureScreen();
var result = images.findImage(screen, img, {
    threshold: 0.80,
    region: [x, y, w, h]
});
if (result) {
    click(result.x + img.width / 2, result.y + img.height / 2);
}
```

**控件模式：**
```javascript
var widget = id("resource_id")
    .text("按钮文字")
    .boundsInside(x, y, x+w, y+h)
    .findOne();
if (widget) {
    widget.click();
}
```

**强制约束：**
- 生成代码必须严格遵循 AGENTS.md 中的 AutoJS6 API 约束
- Rhino 引擎循环体内禁止 const/let
- 图像识别必须遵循 OOM 预防规则

---

## 6. 开发工具链

### 6.1 ImageMagick 工具规则

**安装路径：** `C:\Program Files\ImageMagick-7.1.2-Q16-HDRI\magick.exe`

**常用命令：**
```bash
# 查看图片尺寸/通道/类型
magick identify image.png

# 从截图裁剪模板（x=500, y=300, w=300, h=50）
magick screenshot.png -crop 300x50+500+300 +repage template.png

# 两张图的归一化互相关（模拟 matchTemplate 相似度）
magick compare -metric NCC a.png b.png null: 2>&1

# 检查 Alpha 通道范围
magick image.png -channel a -separate -format "min=%[min] max=%[max]\n" info:
```

**强制要求：**
- 裁剪新模板必须用 ImageMagick 从 ADB 实机截图裁切
- 替换旧模板前必须用 `identify` 检查尺寸和通道信息
- 模板匹配异常时必须用 `compare -metric NCC` 做像素级对比排查

### 6.2 ADB 取证命令

**标准取证流程：**
```powershell
# 1. 看当前前台窗口
adb shell dumpsys window windows | findstr /i "mCurrentFocus"

# 2. 看当前设备分辨率与 density
adb shell wm size
adb shell wm density

# 3. 抓当前真机截图
adb exec-out screencap -p > captures/current-screen.png

# 4. 尝试抓 UI hierarchy
adb shell uiautomator dump /sdcard/window_dump.xml
adb pull /sdcard/window_dump.xml captures/
```

---

## 7. 核心算法复用清单

### 7.1 必须复用的算法

| 算法 | 现有实现 | 目标实现 | 优先级 |
|------|---------|---------|--------|
| 锚点构建 | 内置 regionRef 计算实现 | ImageProcessor.BuildAnchors() | P0 |
| 多容差搜索 | 内置 regionRef 计算实现 | OpenCVMatchService.MultiToleranceSearch() | P0 |
| 逐像素评分 | 内置 regionRef 计算实现 | OpenCVMatchService.ScoreCandidate() | P0 |
| regionRef 转换 | 内置 regionRef 计算实现 | CoordinateMapper.ToReferenceRect() | P0 |
| 模板匹配 | images.matchTemplate | OpenCVMatchService.MatchTemplate() | P0 |
| 特征匹配 | images.matchFeatures | OpenCVMatchService.MatchFeatures() | P1 |
| UI 树解析 | XML 解析 | UiDumpParser.Parse() | P0 |
| 布局容器过滤 | 业务逻辑 | UiDumpParser.FilterLayoutContainers() | P0 |

### 7.2 必须保持一致的参数

| 参数 | 现有值 | 说明 |
|------|--------|------|
| DEFAULT_PADDING | 20 | regionRef 外扩像素 |
| DEFAULT_ALPHA_THRESHOLD | 32 | 透明像素过滤阈值 |
| DEFAULT_PIXEL_TOLERANCE | 20 | 逐像素容差 |
| SEARCH_TOLERANCE_STEPS | [24, 40, 64] | 多容差搜索步长 |
| MIN_MATCH_RATIO | 0.95 | 最小匹配相似度 |
| ANCHOR_COUNT | 16 | 锚点数量 |
| ANCHOR_MIN_DISTANCE | 4 | 锚点最小间距 |

---

## 8. 关键风险与缓解措施

### 8.1 ADB 连接不稳定

**风险：** 截图/Dump 拉取失败

**缓解措施：**
- 实现重试机制（最多 3 次）
- 超时设置 5 秒
- 异常捕获后 Toast 提示用户检查设备连接

### 8.2 OpenCV 模板匹配误报/漏报

**风险：** 匹配结果不准确

**缓解措施：**
- 提供阈值滑块（0.50~0.95）实时调节
- 画布绘制置信度数值
- 支持多模板匹配降低误报

### 8.3 控件树解析失败

**风险：** XML 格式异常/节点缺失

**缓解措施：**
- 容错解析器（跳过无效节点）
- 日志记录解析错误
- 提供原始 Dump 文本查看面板

### 8.4 生成代码与当前工具实现行为不一致

**风险：** 生成代码不符合 AutoJS6 约束

**缓解措施：**
- 严格复用当前工具实现的坐标计算/匹配算法/路径处理逻辑
- 提供代码预览与手动编辑功能
- 实施前完整理解 AutoJS6 自动化项目 业务逻辑（AGENTS.md、README.md、openspec/project.md）
- 确保生成代码符合 AutoJS6 API 约束（Rhino 引擎限制、图像识别 OOM 预防、模板裁剪规则、横竖屏处理、regionRef 生成规则）

---

## 9. 下一步行动建议

### 9.1 立即可开始的任务

1. **配置项目依赖（任务 1.1-1.7）**
   - 添加 NuGet 包：OpenCvSharp4.Windows、SixLabors.ImageSharp、SharpAdbClient、Microsoft.Graphics.Win2D、CommunityToolkit.Mvvm
   - 创建目录结构：Core/{Abstractions,Models,Services,Helpers}、Infrastructure/{Adb,Imaging}、App/{Views,ViewModels,Resources}

2. **定义核心数据模型（任务 2.1-2.5）**
   - AdbDevice、WidgetNode、CropRegion、MatchResult、AutoJS6CodeOptions

3. **实现 ADB 通信层（任务 4.1-4.6）**
   - 设备扫描、命令执行、截图拉取、UI Dump 拉取

### 9.2 需要进一步调研的内容

1. **Win2D 性能优化**
   - CanvasBitmap 缓存池设计
   - 分层渲染管线实现
   - 60FPS 渲染优化策略

2. **OpenCvSharp4 API 映射**
   - TM_CCOEFF_NORMED 算法封装
   - 多尺度匹配实现
   - 特征匹配 API 调用

3. **WinUI 3 交互设计**
   - 滚轮缩放实现
   - 拖拽平移实现
   - 交互式矩形裁剪实现

---

## 10. 总结

### 10.1 核心发现

1. **AutoJS6 自动化项目 项目已经形成了完整的 AutoJS6 开发工作流**
   - 截图取证 → 模板裁剪 → regionRef 生成 → 代码生成 → 真机验证
   - 所有核心算法和业务规则都已经过真机验证

2. **AutoJS6 API 约束非常严格**
   - Rhino 引擎限制必须在代码生成时强制遵守
   - 图像识别 OOM 预防规则必须在工具设计时考虑
   - 横竖屏处理必须在模板管理时明确声明

3. **双引擎独立架构是核心设计原则**
   - 图像处理引擎与 UI 图层分析引擎完全解耦
   - 两者数据源、坐标系、匹配算法完全不同
   - 禁止混合使用或相互依赖

### 10.2 关键成功因素

1. **严格复用现有算法**
   - 内置 regionRef 计算实现 的锚点构建、多容差搜索、逐像素评分算法必须完整移植
   - 参数值必须保持一致（padding=20, alphaThreshold=32, pixelTolerance=20 等）

2. **遵守 AutoJS6 API 约束**
   - 生成代码必须符合 Rhino 引擎限制
   - 图像识别必须遵循 OOM 预防规则
   - 横竖屏处理必须明确声明

3. **提供可视化验证能力**
   - 实时预览匹配结果
   - 可视化调整阈值和区域
   - 自动生成 AutoJS6 代码

### 10.3 下一阶段目标

**如果继续实施，建议按以下顺序推进：**

1. **阶段 1：Core 层 + Infrastructure 层（任务 1-8）**
   - 配置项目依赖
   - 定义数据模型和服务接口
   - 实现 ADB 通信、图像处理、UI 树解析、OpenCV 匹配、代码生成

2. **阶段 2：App 层基础（任务 9-15）**
   - 实现 Win2D 画布控件
   - 实现画布交互（缩放、平移、裁剪）
   - 实现设备管理 UI
   - 实现 MVVM 视图模型
   - 实现主窗口布局

3. **阶段 3：高级功能（任务 16-17）**
   - 实现实时匹配测试 UI
   - 实现全局快捷键与状态管理

4. **阶段 4：优化与测试（任务 18-21）**
   - 性能优化与异步架构
   - 错误处理与日志
   - 测试与验证
   - 文档与部署

---

**报告完成。前置准备阶段（任务 0.1-0.10）已完成。**
