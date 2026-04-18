## ADDED Requirements

### Requirement: 系统能够生成图像模式 AutoJS6 代码
系统 SHALL 基于裁剪区域与模板图像，自动生成 AutoJS6 图像匹配代码，包含 requestScreenCapture()、images.read()、images.findImage()、点击逻辑、重试/超时机制。

#### Scenario: 生成基础图像匹配代码
- **WHEN** 用户在图像模式下点击"生成代码"按钮
- **THEN** 系统生成包含以下内容的 JS 代码：
  - requestScreenCapture() 申请截图权限
  - images.read() 读取模板图像
  - images.findImage() 执行模板匹配
  - click() 点击匹配位置
- **THEN** 代码预览面板显示生成的代码

#### Scenario: 生成带阈值参数的代码
- **WHEN** 用户设置匹配阈值为 0.85
- **THEN** 生成的代码包含 threshold: 0.85 参数
- **THEN** 代码格式：images.findImage(img, template, {threshold: 0.85})

#### Scenario: 生成带区域限制的代码
- **WHEN** 用户创建裁剪区域 (100, 200, 300, 400)
- **THEN** 生成的代码包含 region: [100, 200, 300, 400] 参数
- **THEN** 代码格式：images.findImage(img, template, {region: [100, 200, 300, 400]})

#### Scenario: 生成带重试机制的代码
- **WHEN** 用户启用"重试机制"选项（最多 3 次，间隔 1 秒）
- **THEN** 生成的代码包含 for 循环重试逻辑
- **THEN** 每次重试间隔 sleep(1000)

#### Scenario: 生成带超时机制的代码
- **WHEN** 用户设置超时时间为 10 秒
- **THEN** 生成的代码包含 setTimeout() 超时逻辑
- **THEN** 超时后抛出异常或返回 null

### Requirement: 系统能够生成控件模式 AutoJS6 代码
系统 SHALL 基于选中控件节点，自动生成最优 UiSelector 选择器链，优先 id()，降级 text()/desc()，补充 boundsInside()。

#### Scenario: 生成基于 resource-id 的选择器
- **WHEN** 选中控件包含 resource-id="com.example:id/login_button"
- **THEN** 生成的代码为：id("com.example:id/login_button").findOne()

#### Scenario: 生成基于 text 的选择器
- **WHEN** 选中控件无 resource-id 但包含 text="登录"
- **THEN** 生成的代码为：text("登录").findOne()

#### Scenario: 生成基于 content-desc 的选择器
- **WHEN** 选中控件无 resource-id 和 text 但包含 content-desc="登录按钮"
- **THEN** 生成的代码为：desc("登录按钮").findOne()

#### Scenario: 生成组合选择器
- **WHEN** 选中控件包含 resource-id 和 text
- **THEN** 生成的代码为：id("xxx").text("yyy").findOne()

#### Scenario: 生成带 boundsInside 的选择器
- **WHEN** 选中控件 bounds 为 (100, 200, 300, 400)
- **THEN** 生成的代码包含：boundsInside(100, 200, 300, 400)

#### Scenario: 生成带操作方法的代码
- **WHEN** 用户选择操作类型为"点击"
- **THEN** 生成的代码附加 .click() 方法
- **WHEN** 用户选择操作类型为"输入文本"
- **THEN** 生成的代码附加 .setText("文本内容") 方法

### Requirement: 系统能够处理路径兼容性
系统 SHALL 自动切换 assets/相对/绝对路径，支持变量名自定义。

#### Scenario: 生成 assets 路径
- **WHEN** 模板图像位于项目 assets 目录
- **THEN** 生成的代码使用相对路径：images.read("./assets/template.png")

#### Scenario: 生成绝对路径
- **WHEN** 模板图像位于任意目录
- **THEN** 生成的代码使用绝对路径：images.read("C:/Users/.../template.png")

#### Scenario: 自定义变量名
- **WHEN** 用户设置变量名为 "loginButton"
- **THEN** 生成的代码使用该变量名：var loginButton = id("xxx").findOne()

### Requirement: 系统能够格式化生成的 JS 代码
系统 SHALL 对生成的代码进行格式化，确保缩进、换行、注释符合 JS 规范。

#### Scenario: 代码自动缩进
- **WHEN** 生成包含嵌套结构的代码
- **THEN** 系统自动添加 2 空格缩进
- **THEN** 代码结构清晰易读

#### Scenario: 添加注释
- **WHEN** 生成代码包含关键步骤
- **THEN** 系统自动添加单行注释说明
- **THEN** 注释格式：// 申请截图权限

### Requirement: 系统能够支持代码预览与手动编辑
系统 SHALL 提供代码预览面板，支持语法高亮、手动编辑、一键复制。

#### Scenario: 代码预览面板显示
- **WHEN** 系统生成代码
- **THEN** 代码预览面板显示完整代码
- **THEN** 支持 JavaScript 语法高亮

#### Scenario: 手动编辑代码
- **WHEN** 用户在预览面板中修改代码
- **THEN** 系统保存修改后的代码
- **THEN** 不影响原始生成逻辑

#### Scenario: 一键复制代码
- **WHEN** 用户点击"复制代码"按钮或按下 Ctrl+S
- **THEN** 系统将完整代码复制到剪贴板
- **THEN** 显示 Toast 提示"代码已复制"

### Requirement: 系统能够导出代码为 JS 文件
系统 SHALL 支持将生成的代码导出为独立 .js 文件。

#### Scenario: 导出为 JS 文件
- **WHEN** 用户点击"导出文件"按钮
- **THEN** 系统打开文件保存对话框
- **THEN** 默认文件名为 "autojs6_script_<timestamp>.js"
- **THEN** 保存后显示 Toast 提示"文件已保存"

### Requirement: 系统能够确保生成代码与现有 cmd 脚本行为一致
系统 SHALL 严格复用现有 cmd 脚本的坐标计算、匹配算法、路径处理逻辑。

#### Scenario: 坐标计算一致性
- **WHEN** 生成图像匹配代码
- **THEN** 坐标系原点为左上角
- **THEN** 坐标计算公式与 cmd 脚本一致

#### Scenario: 匹配算法一致性
- **WHEN** 生成模板匹配代码
- **THEN** 使用 OpenCV TM_CCOEFF_NORMED 算法
- **THEN** 阈值默认值与 cmd 脚本一致（0.80）

#### Scenario: 路径处理一致性
- **WHEN** 生成文件路径
- **THEN** 路径分隔符统一为正斜杠 /
- **THEN** 路径处理逻辑与 cmd 脚本一致
