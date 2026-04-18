## ADDED Requirements

### Requirement: 系统能够从设备拉取全屏截图并解码为位图
系统 SHALL 通过 ADB 命令从已连接设备拉取全屏 PNG 截图，并解码为 Win2D CanvasBitmap 用于画布渲染。

#### Scenario: 成功拉取并显示截图
- **WHEN** 用户点击"截图"按钮
- **THEN** 系统执行 `adb shell screencap -p` 并流式读取 PNG 数据
- **THEN** 解码为 CanvasBitmap 并在画布中心显示
- **THEN** 状态栏显示原始尺寸（如 1080x2400）

#### Scenario: 截图拉取失败
- **WHEN** ADB 命令执行失败或设备无响应
- **THEN** 系统显示 Toast 提示"截图失败，请检查设备连接"
- **THEN** 日志面板输出详细错误信息

#### Scenario: 高分辨率设备降采样
- **WHEN** 设备分辨率超过 1920x1080
- **THEN** 系统自动降采样至最大 1920x1080 保持宽高比
- **THEN** 状态栏显示"已降采样"标识

### Requirement: 系统能够支持交互式矩形裁剪
系统 SHALL 允许用户在画布上拖拽创建矩形裁剪区域，并支持拖拽顶点/边调整尺寸。

#### Scenario: 创建裁剪区域
- **WHEN** 用户在画布上按住鼠标左键并拖拽
- **THEN** 系统绘制实时跟随的矩形框
- **THEN** 释放鼠标后固定矩形区域
- **THEN** 实时显示像素坐标 (x, y, w, h)

#### Scenario: 调整裁剪区域顶点
- **WHEN** 用户拖拽矩形四个顶点之一
- **THEN** 系统调整矩形尺寸，对角顶点保持固定
- **THEN** 实时更新坐标显示

#### Scenario: 调整裁剪区域边
- **WHEN** 用户拖拽矩形四条边之一
- **THEN** 系统沿该边方向调整尺寸
- **THEN** 实时更新坐标显示

#### Scenario: Shift 锁定宽高比
- **WHEN** 用户按住 Shift 键并拖拽顶点
- **THEN** 系统锁定矩形宽高比
- **THEN** 调整时保持初始比例

### Requirement: 系统能够提供像素标尺与坐标拾取
系统 SHALL 在鼠标悬停时实时显示精确像素坐标，并支持十字准线锁定。

#### Scenario: 鼠标悬停显示坐标
- **WHEN** 用户鼠标在画布上移动
- **THEN** 状态栏实时显示当前像素坐标 (x, y)
- **THEN** 坐标基于原始图像尺寸，不受缩放影响

#### Scenario: 十字准线锁定
- **WHEN** 用户按下 Ctrl 键
- **THEN** 系统在鼠标位置绘制十字准线
- **THEN** 准线延伸至画布边界，辅助对齐

#### Scenario: 辅助网格显示
- **WHEN** 用户启用"显示网格"选项
- **THEN** 系统绘制 10x10 像素网格线
- **THEN** 网格随缩放自动调整密度

### Requirement: 系统能够导出裁剪区域为独立 PNG 模板
系统 SHALL 将裁剪区域导出为独立 PNG 文件，并记录相对于原图的偏移量。

#### Scenario: 导出裁剪区域
- **WHEN** 用户点击"导出模板"按钮
- **THEN** 系统裁剪指定区域并保存为 PNG 文件
- **THEN** 文件名包含坐标信息（如 template_100_200_300_400.png）
- **THEN** 同时生成 JSON 元数据文件记录偏移量

#### Scenario: 未创建裁剪区域时导出
- **WHEN** 用户未创建裁剪区域直接点击"导出模板"
- **THEN** 系统显示 Toast 提示"请先创建裁剪区域"

### Requirement: 系统能够执行 OpenCV 模板匹配
系统 SHALL 使用 OpenCV TM_CCOEFF_NORMED 算法在原图中匹配导出的模板，并返回匹配位置与置信度。

#### Scenario: 成功匹配模板
- **WHEN** 用户加载模板图像并点击"匹配"按钮
- **THEN** 系统异步执行 OpenCV matchTemplate 计算
- **THEN** 画布绘制绿色矩形框标识匹配位置
- **THEN** 显示置信度数值（如 0.95）与耗时

#### Scenario: 匹配置信度低于阈值
- **WHEN** 匹配结果置信度低于用户设置的阈值（默认 0.80）
- **THEN** 系统显示"未找到匹配"提示
- **THEN** 日志输出最高置信度值

#### Scenario: 多模板匹配
- **WHEN** 用户加载多个模板图像
- **THEN** 系统依次匹配每个模板
- **THEN** 画布绘制所有匹配结果，按置信度着色

### Requirement: 系统能够异步处理图像操作避免 UI 阻塞
系统 SHALL 使用异步架构执行所有图像处理操作（截图拉取、解码、OpenCV 计算、纹理上传）。

#### Scenario: 截图拉取不阻塞 UI
- **WHEN** 系统执行截图拉取（耗时 200-500ms）
- **THEN** UI 保持响应，用户可继续操作
- **THEN** 拉取完成后自动更新画布

#### Scenario: OpenCV 计算不阻塞 UI
- **WHEN** 系统执行模板匹配（耗时 50-200ms）
- **THEN** UI 保持响应，显示加载指示器
- **THEN** 计算完成后自动绘制匹配结果

### Requirement: 系统能够使用 CanvasBitmap 缓存池优化内存
系统 SHALL 使用 Win2D CanvasBitmap 缓存池，避免重复创建纹理导致内存泄漏。

#### Scenario: 缓存原始截图纹理
- **WHEN** 用户多次缩放/平移画布
- **THEN** 系统复用已缓存的 CanvasBitmap
- **THEN** 不重新解码 PNG 数据

#### Scenario: 阈值滑动仅重算匹配层
- **WHEN** 用户调整匹配阈值滑块
- **THEN** 系统仅重新计算匹配结果
- **THEN** 不重建图像纹理，仅更新 Overlay 图层
