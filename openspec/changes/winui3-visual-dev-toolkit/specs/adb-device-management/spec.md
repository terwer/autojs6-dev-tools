## ADDED Requirements

### Requirement: 系统能够扫描并列出所有已连接的 ADB 设备
系统 SHALL 通过 AdvancedSharpAdbClient 底层 API 扫描所有已连接的 Android 设备，并在 UI 中显示设备列表，包含设备序列号、型号、状态信息。

**实现约束**：必须使用 `AdbClient.GetDevices()` 底层 API，禁止使用 `adb devices` 命令。

#### Scenario: 成功扫描到多个设备
- **WHEN** 用户点击"刷新设备"按钮
- **THEN** 系统调用 `AdbClient.GetDevices()` 获取设备列表
- **THEN** UI 显示所有设备的序列号、型号、连接状态（Online/Offline/Unauthorized）

#### Scenario: 未检测到任何设备
- **WHEN** 用户点击"刷新设备"按钮且无设备连接
- **THEN** 系统显示"未检测到设备"提示
- **THEN** UI 提供 ADB 环境配置指引链接

#### Scenario: ADB 服务未启动
- **WHEN** ADB Server 未运行
- **THEN** 系统调用 `AdbServer.StartServer()` 自动启动
- **THEN** 启动失败时显示 Toast 提示"ADB 不可用，请检查环境配置"

### Requirement: 系统能够选择并连接指定设备
系统 SHALL 允许用户从设备列表中选择目标设备，并将其设置为后续所有 ADB 操作的默认设备。

#### Scenario: 选择单个设备
- **WHEN** 用户点击设备列表中的某个设备
- **THEN** 系统高亮显示该设备
- **THEN** 后续所有 ADB API 调用自动使用该 DeviceData 对象

#### Scenario: 多设备环境下未选择设备
- **WHEN** 存在多个设备且用户未选择任何设备
- **THEN** 系统禁用所有需要设备连接的功能按钮
- **THEN** 显示提示"请先选择目标设备"

### Requirement: 系统能够实时输出操作日志
系统 SHALL 提供日志面板，实时流式输出所有 ADB 操作的执行结果。

**实现约束**：日志记录 API 调用和结果，不记录命令文本。

#### Scenario: 成功执行操作并输出日志
- **WHEN** 系统调用 `GetFrameBufferAsync()` 拉取截图
- **THEN** 日志面板实时显示操作进度
- **THEN** 操作完成后显示执行耗时与状态（成功/失败）

#### Scenario: 操作超时
- **WHEN** ADB 操作执行超过 10 秒未响应
- **THEN** 系统通过 CancellationToken 取消操作
- **THEN** 日志面板显示"操作超时"错误信息

#### Scenario: 设备连接中断
- **WHEN** 操作执行过程中设备断开连接
- **THEN** 系统捕获异常并显示 Toast 提示"设备连接中断"
- **THEN** 自动刷新设备列表

### Requirement: 系统能够支持 USB 和 TCP/IP 连接模式
系统 SHALL 支持通过 USB 和 TCP/IP（无线调试）两种方式连接 Android 设备。

**实现约束**：必须使用 `AdbClient.Connect()` 底层 API，禁止使用 `adb connect` 命令。

#### Scenario: USB 连接模式
- **WHEN** 设备通过 USB 线连接到计算机
- **THEN** 系统通过 `AdbClient.GetDevices()` 自动检测到设备
- **THEN** 设备列表显示连接类型为"USB"（Serial 不包含冒号）

#### Scenario: TCP/IP 连接模式
- **WHEN** 用户输入设备 IP 地址和端口（如 192.168.1.100:5555）并点击"连接"
- **THEN** 系统调用 `AdbClient.Connect(address)` 建立连接
- **THEN** 连接成功后设备列表显示连接类型为"TCP/IP"（Serial 包含冒号）

#### Scenario: TCP/IP 连接失败
- **WHEN** 用户输入的 IP 地址无法连接
- **THEN** 系统显示 Toast 提示"连接失败，请检查设备 IP 和端口"
- **THEN** 日志面板输出详细错误信息

### Requirement: 系统能够异步执行 ADB 操作避免 UI 阻塞
系统 SHALL 使用异步架构执行所有 ADB 操作，确保 UI 线程不被阻塞。

**实现约束**：所有 ADB API 调用必须使用 async/await，支持 CancellationToken。

#### Scenario: 长时间操作不阻塞 UI
- **WHEN** 系统执行耗时 ADB 操作（如截图拉取）
- **THEN** UI 保持响应，用户可继续操作其他功能
- **THEN** 操作执行期间显示加载指示器

#### Scenario: 并发执行多个操作
- **WHEN** 用户同时触发截图拉取和 UI Dump 拉取
- **THEN** 系统并发执行两个异步操作
- **THEN** 每个操作独立完成并更新对应 UI 区域
