## ADDED Requirements

### Requirement: 系统能够拉取并解析 Android UI 树
系统 SHALL 通过 ADB 命令拉取当前界面的 UI 树数据（uiautomator dump），并解析为结构化控件节点树。

#### Scenario: 成功拉取并解析 UI 树
- **WHEN** 用户点击"拉取 UI 树"按钮
- **THEN** 系统执行 `adb shell uiautomator dump /dev/tty` 并流式读取 XML 数据
- **THEN** 解析 XML 并构建控件节点树
- **THEN** TreeView 显示控件层级结构

#### Scenario: UI 树拉取失败
- **WHEN** ADB 命令执行失败或设备无响应
- **THEN** 系统显示 Toast 提示"UI 树拉取失败，请检查设备连接"
- **THEN** 日志面板输出详细错误信息

#### Scenario: XML 格式异常容错解析
- **WHEN** UI 树 XML 包含无效节点或缺失属性
- **THEN** 系统跳过无效节点并记录警告日志
- **THEN** 继续解析有效节点并显示在 TreeView

### Requirement: 系统能够过滤冗余布局容器节点
系统 SHALL 完全忽略布局容器（LinearLayout/ConstraintLayout/FrameLayout/RelativeLayout），仅提取最底层具备特征的控件节点。

#### Scenario: 过滤无特征布局容器
- **WHEN** UI 树包含 LinearLayout 且无 clickable/text/content-desc 属性
- **THEN** 系统跳过该节点，不显示在 TreeView
- **THEN** 直接提取其子节点

#### Scenario: 保留可交互布局容器
- **WHEN** UI 树包含 FrameLayout 且 clickable="true"
- **THEN** 系统保留该节点并显示在 TreeView
- **THEN** 标记为可交互控件

#### Scenario: 提取有效控件节点
- **WHEN** UI 树包含 TextView/ImageView/Button/Switch 节点
- **THEN** 系统提取所有这些节点
- **THEN** TreeView 显示节点类型、text、resource-id 属性

### Requirement: 系统能够精准解析控件属性
系统 SHALL 精准解析每个控件节点的 resource-id、text、content-desc、class、clickable、bounds、package 属性。

#### Scenario: 解析完整属性
- **WHEN** 控件节点包含所有标准属性
- **THEN** 系统解析并存储所有属性值
- **THEN** 属性面板显示完整详情

#### Scenario: 解析 bounds 坐标
- **WHEN** 控件节点 bounds="[100,200][400,600]"
- **THEN** 系统解析为 Rect(100, 200, 300, 400)
- **THEN** 坐标基于左上角原点，宽度=400-100，高度=600-200

#### Scenario: 处理缺失属性
- **WHEN** 控件节点缺少 text 或 content-desc 属性
- **THEN** 系统将该属性值设为空字符串
- **THEN** 不影响其他属性解析

### Requirement: 系统能够渲染控件边界框并按类型着色
系统 SHALL 在画布 Overlay 图层绘制控件边界框，按控件类型着色（蓝色=Text，绿色=Button，橙色=Image，灰色=其他）。

#### Scenario: 绘制所有控件边界框
- **WHEN** UI 树解析完成
- **THEN** 系统在画布上绘制所有控件的边界框
- **THEN** 边界框坐标与截图坐标系对齐

#### Scenario: 按类型着色
- **WHEN** 控件类型为 TextView
- **THEN** 边界框颜色为蓝色（RGB: 0, 120, 215）
- **WHEN** 控件类型为 Button
- **THEN** 边界框颜色为绿色（RGB: 16, 124, 16）
- **WHEN** 控件类型为 ImageView
- **THEN** 边界框颜色为橙色（RGB: 247, 99, 12）

#### Scenario: 透明度与显示过滤
- **WHEN** 用户调整透明度滑块至 50%
- **THEN** 所有边界框透明度设为 50%
- **WHEN** 用户取消勾选"显示 TextView"
- **THEN** 系统隐藏所有 TextView 边界框

### Requirement: 系统能够实现 TreeView 与画布的双向联动
系统 SHALL 支持 TreeView 点击节点高亮画布控件框，以及画布点击控件框自动展开并定位 TreeView 节点。

#### Scenario: TreeView 点击联动画布
- **WHEN** 用户在 TreeView 中点击某个控件节点
- **THEN** 画布高亮显示对应控件边界框（加粗红色）
- **THEN** 画布自动滚动至该控件位置

#### Scenario: 画布点击联动 TreeView
- **WHEN** 用户在画布上点击某个控件边界框
- **THEN** TreeView 自动展开并定位到对应节点
- **THEN** 节点高亮显示

#### Scenario: 多层嵌套节点展开
- **WHEN** 用户点击深层嵌套的控件边界框
- **THEN** TreeView 自动展开所有父节点
- **THEN** 滚动至目标节点并高亮

### Requirement: 系统能够显示控件属性面板
系统 SHALL 在选中控件节点时，显示属性面板展示完整详情，并支持一键复制坐标、文本或 XPath 表达式。

#### Scenario: 显示完整属性
- **WHEN** 用户选中某个控件节点
- **THEN** 属性面板显示 resource-id、text、content-desc、class、clickable、bounds、package
- **THEN** 属性值支持文本选择与复制

#### Scenario: 一键复制坐标
- **WHEN** 用户点击"复制坐标"按钮
- **THEN** 系统将 bounds 坐标复制到剪贴板（格式：100,200,300,400）

#### Scenario: 一键复制 XPath
- **WHEN** 用户点击"复制 XPath"按钮
- **THEN** 系统生成并复制完整 XPath 表达式（如 //android.widget.TextView[@text='登录']）

### Requirement: 系统能够支持 5000+ 节点的高性能渲染
系统 SHALL 支持解析和渲染包含 5000+ 控件节点的 UI 树，TreeView 启用 UI 虚拟化，属性面板懒加载。

#### Scenario: 大规模节点树渲染
- **WHEN** UI 树包含 5000+ 控件节点
- **THEN** TreeView 启用虚拟化，仅渲染可见节点
- **THEN** 滚动流畅无卡顿

#### Scenario: 属性面板懒加载
- **WHEN** 用户选中节点
- **THEN** 属性面板仅加载当前节点属性
- **THEN** 不预加载其他节点数据

### Requirement: 系统能够异步解析 UI 树避免 UI 阻塞
系统 SHALL 使用异步架构解析 UI 树 XML，确保 UI 线程不被阻塞。

#### Scenario: UI 树解析不阻塞 UI
- **WHEN** 系统解析包含 5000+ 节点的 UI 树
- **THEN** UI 保持响应，显示加载指示器
- **THEN** 解析完成后自动更新 TreeView 和画布
