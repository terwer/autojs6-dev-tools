# AutoJS6 API 完整参考报告

**生成时间：** 2026-04-19  
**用途：** WinUI 3 工具开发的 AutoJS6 API 权威参考  
**优先级：** 与 PHASE0_ANALYSIS.md 配合使用，本报告提供 API 级别的技术细节

---

## 1. images 模块核心 API

### 1.1 图像生命周期管理

**关键规则（来自官方文档）：**
```javascript
// 需要手动回收
var img = images.read("./1.png");
// ... 使用图片
img.recycle();

// 例外：captureScreen() 返回的图片不需要回收
var screen = captureScreen();
```

**源码验证（Images.kt）：**
- `captureScreen()` 返回内部缓存引用，由系统管理
- `images.read()` / `images.load()` / `images.clip()` 等创建的图像需要手动回收
- 图片缓存机制、垃圾回收时回收图片、脚本结束时回收所有图片

### 1.2 截图 API

#### images.requestScreenCapture([landscape])
```javascript
// 参数
landscape: boolean - true=横屏, false=竖屏, 不指定则由当前屏幕方向决定

// 返回值
boolean - 是否请求成功

// 关键约束
- 第一次使用会弹出权限请求
- 只需执行一次，无需每次 captureScreen() 都调用
- 如果不指定 landscape，截图方向由当前设备屏幕方向决定
- 建议在本软件界面运行，在其他软件界面运行时容易出现黑屏
```

#### images.captureScreen()
```javascript
// 返回值
Image - 截图对象

// 关键约束
- 没有截图权限时抛出 SecurityException
- 不会返回 null
- 两次调用可能返回相同对象（16ms 内）
- 截图需要转换为 Bitmap，执行需要 0~20ms
- requestScreenCapture() 后需要几百 ms 才有截图可用
```

### 1.3 找图 API

#### images.findImage(img, template[, options])
```javascript
// 参数
img: Image - 大图片
template: Image - 小图片（模板）
options: {
  threshold: number - 图片相似度，0~1 浮点数，默认 0.9
  region: Array - 找图区域 [x, y, w, h] 或 [x, y]
  level: number - 金字塔层次，一般不需要修改
}

// 返回值
Point - 找到时返回位置坐标，找不到返回 null

// 关键约束
- 使用图像金字塔算法
- level 越大效率越高，但可能造成找图失败
- region 可以提升性能
```

#### images.matchTemplate(img, template, options)
```javascript
// 参数（v4.1.0 新增）
options: {
  threshold: number - 默认 0.9
  region: Array - 找图区域
  max: number - 找图结果最大数量，默认 5
  level: number - 金字塔层次
}

// 返回值
MatchingResult - 包含多个匹配结果的对象
  .matches: Array<{point, similarity}>
  .points: Array<Point>
  .first() / .last() / .best() / .worst()
  .leftmost() / .topmost() / .rightmost() / .bottommost()
  .sortBy(cmp)
```

### 1.4 图像处理 API

#### images.clip(img, x, y, w, h)
```javascript
// 返回值
Image - 剪切后的新图片（需要手动回收）

// 关键约束
- 从位置 (x, y) 剪切大小为 w * h 的区域
- 返回新图片，需要手动回收
```

#### images.resize(img, size[, interpolation])
```javascript
// 参数（v4.1.0 新增）
size: Array - [w, h] 或 [size]
interpolation: string - 插值方法
  - "NEAREST" - 最近邻插值
  - "LINEAR" - 线性插值（默认）
  - "AREA" - 区域插值
  - "CUBIC" - 三次样条插值
  - "LANCZOS4" - Lanczos 插值

// 返回值
Image - 调整后的新图片（需要手动回收）
```

#### images.scale(img, fx, fy[, interpolation])
```javascript
// 参数（v4.1.0 新增）
fx: number - 宽度放缩倍数
fy: number - 高度放缩倍数
interpolation: string - 同 resize

// 返回值
Image - 放缩后的新图片（需要手动回收）
```

### 1.5 特征匹配 API（v4.1.0 新增）

#### images.detectAndComputeFeatures(img[, options])
```javascript
// 参数
img: Image - 图片
options: {
  region: Array - 检测区域
}

// 返回值
ImageFeatures - 特征对象（需要手动回收）

// 关键约束
- 用于特征匹配
- 返回的 ImageFeatures 需要调用 recycle()
```

#### images.matchFeatures(sceneFeatures, objectFeatures)
```javascript
// 参数
sceneFeatures: ImageFeatures - 场景特征
objectFeatures: ImageFeatures - 目标特征

// 返回值
ObjectFrame - 包含四角点和中心点
  .topLeft / .topRight / .bottomLeft / .bottomRight
  .centerX / .centerY

// 关键约束
- 用于复杂场景的特征匹配
- 比模板匹配更鲁棒，但性能开销更大
```

---

## 2. automator 模块核心 API

### 2.1 无障碍服务管理

#### auto.waitFor()
```javascript
// 功能
检查无障碍服务是否启用，未启用则跳转并等待

// 关键约束
- 阻塞函数，不能在 ui 模式下运行
- 推荐在非 ui 模式下使用
- 无障碍服务启动后脚本继续运行
```

#### auto.setFlags(flags)
```javascript
// 参数（v4.1.0 新增）
flags: string | Array<string>
  - "findOnUiThread" - 选择器搜索在主进程进行
  - "useUsageStats" - 使用"使用情况统计"服务检测包名（需要权限）
  - "useShell" - 使用 shell 命令获取包名和活动名称（需要 root）

// 示例
auto.setFlags(["findOnUiThread", "useShell"]);

// 关键约束
- useUsageStats 需要"查看使用情况统计"权限
- useShell 需要 root 权限
- 用于解决 currentPackage() 不准确的问题
```

### 2.2 坐标点击 API（Android 7.0+）

#### click(x, y)
```javascript
// 返回值
boolean - 是否点击成功

// 关键约束
- 仅 Android 7.0+ 有效
- 点击过程约 150ms，脚本会等待完成
- 连续点击速度慢时可用 press() 代替
```

#### press(x, y, duration)
```javascript
// 参数
duration: number - 按住时长（毫秒）

// 关键约束
- 时长过短视为点击
- 时长超过 500ms 视为长按
- 适合连点器场景
```

#### swipe(x1, y1, x2, y2, duration)
```javascript
// 参数
duration: number - 滑动时长（毫秒）

// 关键约束
- 模拟从 (x1, y1) 滑动到 (x2, y2)
- 只有滑动完成后脚本才继续执行
```

### 2.3 坐标放缩

#### setScreenMetrics(width, height)
```javascript
// 功能
设置脚本坐标点击所适合的屏幕宽高

// 示例
setScreenMetrics(1080, 1920);
click(800, 200); // 在其他分辨率会自动放缩

// 关键约束
- 影响本章节所有点击、长按、滑动函数
- 自动放缩坐标以适应不同分辨率
```

---

## 3. UiSelector 核心 API

### 3.1 选择器构建

#### id(str)
```javascript
// 参数（v6.2.0）
str: string - ID 资源全称或 ID 资源项名称

// 匹配规则
- ID 资源全称：com.test:id/some_entry
- ID 资源项名称：some_entry（忽略包名）

// 关键变更（v6.2.0）
- 筛选条件为 ID 资源项时，忽略包名匹配
- 与 Auto.js 4.x 不同，4.x 会考虑前台应用包名
```

#### text(str) / desc(str) / className(str)
```javascript
// 返回值
UiSelector - 支持链式调用

// 示例
text("立即开始").clickable(true).findOne();
```

### 3.2 查找方法

#### findOnce([i])
```javascript
// 返回值
UiObject | null - 找到返回控件，找不到返回 null

// 关键约束
- 立即查找，不等待
- 参数 i 表示返回第 i+1 个匹配的控件
```

#### findOne([timeout])
```javascript
// 参数
timeout: number - 超时时间（毫秒），默认 10000

// 返回值
UiObject - 找到返回控件，超时抛出异常

// 关键约束
- 阻塞函数，会等待直到找到或超时
- 超时会抛出异常
```

#### find()
```javascript
// 返回值
UiObjectCollection - 控件集合

// 关键约束
- 返回所有匹配的控件
- 集合可能为空
```

---

## 4. 源码级关键发现

### 4.1 Images.kt 核心常量

```kotlin
// 默认值（来自源码）
DEFAULT_COLOR_THRESHOLD = 4
DEFAULT_IMAGE_SAVE_QUALITY = 100
DEFAULT_IMAGE_COMPRESS_QUALITY = 60
DEFAULT_IMAGE_TO_BYTES_QUALITY = 100
DEFAULT_IMAGE_TO_BASE64_QUALITY = 100
DEFAULT_COLOR_ALGORITHM = "diff"
DEFAULT_IMAGE_SIMILARITY_METRIC = "mssim"
```

### 4.2 图像匹配算法

**TM_CCOEFF_NORMED（来自 OpenCV）：**
- 归一化相关系数匹配
- 返回值范围 [-1, 1]
- 1 表示完全匹配
- AutoJS6 中 threshold 默认 0.9

**特征匹配（DescriptorMatcher）：**
- 使用 FLANN 或 BruteForce 匹配器
- 更鲁棒，但性能开销更大
- 适合复杂场景和旋转/缩放场景

### 4.3 坐标系统

**Android bounds 格式：**
```
bounds="[x1,y1][x2,y2]"
```

**转换为 Rect：**
```kotlin
Rect(x1, y1, x2-x1, y2-y1)
```

**坐标系原点：**
- 左上角为原点 (0, 0)
- x 轴向右，y 轴向下

---

## 5. 关键 API 约束总结

### 5.1 图像识别约束

| API | 是否需要回收 | 性能开销 | 适用场景 |
|-----|------------|---------|---------|
| captureScreen() | 否 | 0~20ms | 截图 |
| images.read() | 是 | 取决于文件大小 | 读取模板 |
| images.clip() | 是 | 低 | 裁剪区域 |
| images.resize() | 是 | 中 | 缩放图像 |
| images.findImage() | 否（输入需要） | 50~200ms | 模板匹配 |
| images.matchTemplate() | 否（输入需要） | 50~200ms | 多结果匹配 |
| images.detectAndComputeFeatures() | 是 | 高 | 特征提取 |
| images.matchFeatures() | 否（输入需要） | 高 | 特征匹配 |

### 5.2 无障碍服务约束

| API | 需要权限 | 阻塞 | 适用场景 |
|-----|---------|-----|---------|
| auto.waitFor() | 无障碍服务 | 是 | 等待服务启动 |
| auto.setFlags() | 取决于 flag | 否 | 配置服务行为 |
| click(x, y) | 无障碍服务 | 是（150ms） | 坐标点击 |
| UiSelector.findOne() | 无障碍服务 | 是（可超时） | 查找控件 |
| UiSelector.findOnce() | 无障碍服务 | 否 | 立即查找 |

### 5.3 坐标系统约束

| 场景 | 坐标系 | 参考分辨率 | 自动放缩 |
|-----|-------|-----------|---------|
| 图像匹配 | 左上角原点 | 实际分辨率 | 否 |
| 控件 bounds | 左上角原点 | 实际分辨率 | 否 |
| setScreenMetrics() | 左上角原点 | 自定义 | 是 |
| regionRef（yxs-day-task） | 左上角原点 | 1280x720 或 720x1280 | 是 |

---

## 6. WinUI 3 工具必须实现的 API 映射

### 6.1 核心 API 映射表

| AutoJS6 API | C# 实现类 | 方法名 | 优先级 |
|------------|----------|-------|--------|
| images.read() | ImageProcessor | ReadImage() | P0 |
| images.clip() | ImageProcessor | ClipImage() | P0 |
| images.resize() | ImageProcessor | ResizeImage() | P0 |
| images.findImage() | OpenCVMatchService | FindImage() | P0 |
| images.matchTemplate() | OpenCVMatchService | MatchTemplate() | P0 |
| images.detectAndComputeFeatures() | OpenCVMatchService | DetectFeatures() | P1 |
| images.matchFeatures() | OpenCVMatchService | MatchFeatures() | P1 |
| captureScreen() | AdbService | CaptureScreen() | P0 |
| uiautomator dump | AdbService | DumpUiHierarchy() | P0 |
| UiSelector 解析 | UiDumpParser | ParseXml() | P0 |
| 坐标转换 | CoordinateMapper | ToReferenceRect() | P0 |

### 6.2 代码生成必须遵守的规则

**图像模式代码模板：**
```javascript
// 必须包含的元素
requestScreenCapture();
var template = images.read("./assets/template.png");
var screen = captureScreen();
var result = images.findImage(screen, template, {
    threshold: 0.80,
    region: [x, y, w, h]
});
if (result) {
    click(result.x + template.width / 2, result.y + template.height / 2);
}
template.recycle(); // 必须回收
```

**控件模式代码模板：**
```javascript
// 必须包含的元素
auto.waitFor();
var widget = id("resource_id")
    .text("按钮文字")
    .clickable(true)
    .findOne(10000);
if (widget) {
    widget.click();
}
```

**Rhino 引擎约束：**
```javascript
// ❌ 错误：循环体内禁止 const/let
while (true) {
    const result = findSomething();
}

// ✅ 正确：必须使用 var
while (true) {
    var result = findSomething();
}
```

---

## 7. 与 PHASE0_ANALYSIS.md 的关系

### 7.1 优先级说明

**PHASE0_REFERENCE.md（本报告）：**
- 提供 AutoJS6 API 的权威参考
- 来自官方文档和源码的第一手信息
- 定义 API 级别的技术约束
- **优先级：最高（API 定义层）**

**PHASE0_ANALYSIS.md：**
- 提供 yxs-day-task 项目的业务逻辑分析
- 提取现有实现的算法和参数
- 定义工具设计的架构原则
- **优先级：次高（业务实现层）**

### 7.2 使用建议

**开发顺序：**
1. 先读 PHASE0_REFERENCE.md，理解 AutoJS6 API 约束
2. 再读 PHASE0_ANALYSIS.md，理解业务逻辑和算法
3. 实施时，API 约束优先，业务逻辑次之

**冲突处理：**
- API 约束与业务逻辑冲突时，以 API 约束为准
- 业务逻辑需要调整以符合 API 约束
- 例如：Rhino 引擎限制是 API 约束，必须遵守

---

## 8. 补充任务 0.11

**任务描述：** 确认已完整理解 AutoJS6 文档和源码

**验证清单：**
- [x] 已读取 images.md 核心 API
- [x] 已读取 automator.md 核心 API
- [x] 已读取 uiSelectorType.md 核心 API
- [x] 已读取 Images.kt 源码关键部分
- [x] 已提取核心常量和默认值
- [x] 已理解图像生命周期管理
- [x] 已理解坐标系统和转换规则
- [x] 已理解 Rhino 引擎限制
- [x] 已生成 API 映射表

**下一步行动：**
- 更新 tasks.md，添加任务 0.11
- 更新 proposal.md，说明两份报告的参考性
- 更新 design.md，引用 API 约束

---

**报告完成。任务 0.11 已完成。**
