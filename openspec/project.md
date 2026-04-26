# WinUI 3 可视化开发工具包 - 开发清单

适用范围：
- 新增核心功能模块（ADB 设备管理、图像处理引擎、UI 图层分析引擎、画布交互、代码生成器、实时匹配测试）
- 修改现有功能逻辑
- 性能优化与架构调整
- 代码生成逻辑调整

这份清单是 **AGENTS.md 的执行展开版**。默认假设：
- 双核独立架构不可打破
- 项目层依赖关系强制单向
- 异步非阻塞架构
- 60FPS 流畅渲染

---

## 0. 开发前先理解当前项目与 AutoJS6 生态

先完成这 10 项准备工作：

1. **阅读当前项目文档**
   - `AGENTS.md`
   - `README.md` / `README_zh_CN.md`
   - `openspec/project.md`

2. **查阅当前仓库实现**
   - `App/`：WinUI 交互、窗口、视图与 MVVM
   - `Core/`：纯业务逻辑、模型与接口
   - `Infrastructure/`：外部依赖封装
   - `App/CodeTemplates/`：AutoJS6 代码模板

3. **查阅 AutoJS6 文档**
   - `$AUTOJS6_DOCS_ROOT\json\`：API 发现
   - `$AUTOJS6_DOCS_ROOT\api\`：API 引用
   - `$AUTOJS6_DOCS_ROOT\docs\`：辅助阅读

4. **查阅 AutoJS6 源码**
   - `$AUTOJS6_SOURCE_ROOT\app\src\main\java\org\autojs\autojs\runtime\api\augment\`
   - `$AUTOJS6_SOURCE_ROOT\app\src\main\java\org\autojs\autojs\core\`

5. **提取核心业务逻辑**
   - 坐标计算公式（左上角原点、bounds 映射规则）
   - 路径处理规则（正斜杠、assets/相对/绝对路径）
   - 匹配算法参数（TM_CCOEFF_NORMED、阈值范围 0.50~0.95）
   - 横竖屏处理（landscape/portrait、参考分辨率）
   - regionRef 生成规则（基于当前工具真实截图、模板尺寸与导出结果）

6. **理解 AutoJS6 API 约束**
   - `images.findImage()`、`images.matchTemplate()`
   - `UiSelector`：id()、text()、desc()、boundsInside()
   - `requestScreenCapture()`
   - 线程限制（UI / Non-UI）
   - 权限要求

7. **理解 Rhino 引擎约束**
   - 循环体内禁止 const/let，必须用 var
   - 函数顶层和模块顶层可以用 const/let

8. **理解图像识别 OOM 预防规则**
   - 单轮单截图
   - 场景识别按目标最小化
   - 分阶段短路（先弹窗、再首页、再目标相关页面）
   - region 优先于全屏

9. **理解模板裁剪规则**
   - 优先保留：文字主体、图标主体、固定边框
   - 默认排除：红点、数字、倒计时、动态数值

10. **理解双核独立架构**
    - 图像引擎：像素/位图 → 绝对像素坐标
    - UI 引擎：控件树 → UiSelector 选择器链
    - 严禁耦合

---

## 1. 新增功能模块前必须先判断归属

先回答三个问题：

1. 当前功能属于哪个引擎？
   - 图像处理引擎（截图、裁剪、OpenCV 匹配、像素坐标）
   - UI 图层分析引擎（UI 树解析、控件边界框、UiSelector 生成）
   - 共享基础设施（ADB 通信、设备管理、日志输出）

2. 当前功能属于哪个项目层？
   - Core（纯业务逻辑，无 UI 依赖）
   - Infrastructure（外部依赖封装）
   - App（UI 与 MVVM）

3. 当前功能是否会打破双核独立架构？
   - 如果会，必须重新设计
   - 如果不会，继续下一步

默认策略：
- **图像处理功能**：Core/Services/OpenCVMatchService.cs + Infrastructure/Imaging/
- **UI 图层分析功能**：Core/Services/UiDumpParser.cs
- **ADB 通信功能**：Infrastructure/Adb/AdbServiceImpl.cs
- **画布渲染功能**：App/Views/CanvasView.xaml.cs
- **代码生成功能**：Core/Services/AutoJS6CodeGenerator.cs

---

## 2. 新增功能必须先做架构验证

新增功能前，至少验证这 5 项：

1. **项目层依赖关系是否正确**
   - Core 是否无项目内部依赖？
   - Infrastructure 是否只依赖 Core？
   - App 是否只依赖 Core 和 Infrastructure？

2. **双核独立架构是否保持**
   - 图像引擎与 UI 引擎是否完全解耦？
   - 数据源、处理管线、渲染逻辑是否独立？

3. **异步架构是否正确**
   - 所有 I/O 操作是否使用 async/await？
   - 是否避免阻塞 UI 线程？

4. **内存优化是否到位**
   - 是否使用 CanvasBitmap 缓存池？
   - 是否及时回收临时对象？

5. **模块规模是否合理**
   - 单个模块是否超过 512 行？
   - 是否需要拆分？

---

## 3. 代码生成逻辑调整规则

### 图像模式代码生成
必须生成以下结构：
```javascript
// 申请截图权限
requestScreenCapture();

// 读取模板图像
var template = images.read("./assets/template.png");

// 执行模板匹配
var result = images.findImage(screen, template, {
    threshold: 0.85,
    region: [100, 200, 300, 400]
});

// 点击匹配位置
if (result) {
    click(result.x, result.y);
}

// 回收资源
template.recycle();
```

强制要求：
- 循环体内必须用 var，禁止 const/let
- 单轮只允许一张截图对象
- region 优先于全屏匹配
- 必须及时回收 ImageWrapper

### 控件模式代码生成
必须生成以下结构：
```javascript
// 优先 id()
var widget = id("com.example:id/login_button").findOne();

// 降级 text()
if (!widget) {
    widget = text("登录").findOne();
}

// 降级 desc()
if (!widget) {
    widget = desc("登录按钮").findOne();
}

// 补充 boundsInside()
if (!widget) {
    widget = id("xxx").boundsInside(100, 200, 300, 400).findOne();
}

// 执行操作
if (widget) {
    widget.click();
}
```

强制要求：
- 优先 id()，降级 text()/desc()，补充 boundsInside()
- 循环体内必须用 var，禁止 const/let

---

## 4. 坐标系对齐验证规则

新增坐标相关功能时，必须验证：

1. **图像坐标系是否左上角原点**
   - 像素坐标格式：(x, y, w, h)
   - 裁剪区域坐标：(x, y, width, height)

2. **Dump 坐标系是否左上角原点**
   - bounds="[x1,y1][x2,y2]" 映射为 Rect(x1, y1, x2-x1, y2-y1)

3. **画布坐标系是否左上角原点**
   - Win2D 画布默认左上角原点

4. **坐标转换是否正确**
   - 禁止中心点坐标系
   - 禁止坐标系转换
   - 像素坐标与控件坐标直接对应

---

## 5. 控件树解析验证规则

新增或修改控件树解析逻辑时，必须验证：

1. **布局容器是否正确过滤**
   - LinearLayout、ConstraintLayout、FrameLayout、RelativeLayout 是否跳过？
   - class 包含 Layout 且无 clickable/text/content-desc 属性是否跳过？

2. **有效控件是否正确提取**
   - TextView、ImageView、Button、Switch、RecyclerView Item 是否提取？

3. **属性解析是否正确**
   - resource-id、text、content-desc、class、clickable、bounds、package 是否正确解析？

4. **坐标映射是否正确**
   - bounds="[x1,y1][x2,y2]" 是否映射为 Rect(x1, y1, x2-x1, y2-y1)？

5. **容错解析是否到位**
   - 无效节点是否跳过？
   - 缺失属性是否设为空字符串？
   - 是否记录警告日志？

---

## 6. 画布渲染验证规则

新增或修改画布渲染逻辑时，必须验证：

1. **分层渲染是否正确**
   - CanvasImageLayer（底层位图）是否独立？
   - CanvasOverlayLayer（上层控件边界框）是否独立？

2. **渲染性能是否达标**
   - 是否保持 60FPS？
   - 是否仅重绘变化图层？

3. **缓存池是否正确使用**
   - CanvasBitmap 是否使用缓存池？
   - 是否避免重复创建纹理？

4. **GPU 加速是否启用**
   - Win2D 是否启用 GPU 加速？

---

## 7. 异步架构验证规则

新增或修改异步操作时，必须验证：

1. **所有 I/O 操作是否异步**
   - ADB 拉取是否使用 async/await？
   - OpenCV 计算是否使用 Task.Run？
   - Dump 解析是否使用 async/await？
   - 纹理上传是否使用 async/await？

2. **是否避免阻塞 UI 线程**
   - 是否在后台线程执行耗时操作？
   - 是否在 UI 线程更新 UI？

3. **是否正确处理 CancellationToken**
   - 是否支持取消操作？
   - 是否正确传递 CancellationToken？

---

## 8. 性能优化验证规则

新增或修改性能相关逻辑时，必须验证：

1. **内存优化是否到位**
   - CanvasBitmap 缓存池是否正确使用？
   - 临时对象是否及时回收？
   - TreeView 是否启用 UI 虚拟化？
   - 属性面板是否懒加载？

2. **渲染优化是否到位**
   - 是否仅重绘变化图层？
   - 阈值滑动时是否仅重算匹配层？
   - 是否避免重建图像纹理？

3. **控件树性能是否达标**
   - 是否支持 5000+ 节点扁平化渲染？
   - TreeView 是否启用 UI 虚拟化？

---

## 9. 错误处理验证规则

新增或修改错误处理逻辑时，必须验证：

1. **ADB 连接异常是否正确处理**
   - 是否捕获异常？
   - 是否显示 Toast 提示？
   - 是否实现重试机制（最多 3 次）？
   - 是否设置超时（5 秒）？

2. **OpenCV 匹配异常是否正确处理**
   - 是否捕获异常？
   - 是否显示错误信息？

3. **UI 树解析异常是否正确处理**
   - 是否容错解析（跳过无效节点）？
   - 是否记录警告日志？

4. **日志输出是否完整**
   - 是否输出所有 ADB 命令结果？
   - 是否输出错误信息？

---

## 10. 每次提交前最少验证项

### 核心功能模块
- [ ] 项目层依赖关系是否正确（App → Infrastructure → Core）
- [ ] 双核独立架构是否保持（图像引擎与 UI 引擎完全解耦）
- [ ] 异步架构是否正确（所有 I/O 操作使用 async/await）
- [ ] 内存优化是否到位（CanvasBitmap 缓存池、及时回收）
- [ ] 渲染性能是否达标（60FPS、仅重绘变化图层）
- [ ] 模块规模是否合理（单个模块不超过 512 行）

### 代码生成逻辑
- [ ] 是否符合 Rhino 引擎约束（循环体内用 var）
- [ ] 是否符合 OOM 预防规则（单轮单截图、region 优先）
- [ ] 是否包含必要注释（说明关键步骤）
- [ ] 是否格式化（2 空格缩进、换行清晰）

### 坐标系对齐
- [ ] 图像坐标系是否左上角原点
- [ ] Dump 坐标系是否左上角原点
- [ ] 画布坐标系是否左上角原点
- [ ] 坐标转换是否正确

### 控件树解析
- [ ] 布局容器是否正确过滤
- [ ] 有效控件是否正确提取
- [ ] 属性解析是否正确
- [ ] 坐标映射是否正确
- [ ] 容错解析是否到位

### 画布渲染
- [ ] 分层渲染是否正确
- [ ] 渲染性能是否达标（60FPS）
- [ ] 缓存池是否正确使用
- [ ] GPU 加速是否启用

### 错误处理
- [ ] ADB 连接异常是否正确处理
- [ ] OpenCV 匹配异常是否正确处理
- [ ] UI 树解析异常是否正确处理
- [ ] 日志输出是否完整

---

## 11. 对未来开发最重要的一句话

**不要先问"怎么实现"，先问"这个功能属于哪个引擎、哪个项目层、是否会打破双核独立架构"。**

只有先把这三个问题回答清楚，后面的实现才会稳。
