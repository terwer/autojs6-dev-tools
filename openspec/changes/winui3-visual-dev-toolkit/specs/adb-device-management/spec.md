## ADDED Requirements

### Requirement: 系统能够扫描并列出所有已连接的 ADB 设备
系统 SHALL 通过 ADB 命令扫描所有已连接的 Android 设备，并在 UI 中显示设备列表，包含设备序列号、型号、状态信息。

#### Scenario: 成功扫描到多个设备
- **WHEN** 用户点击"刷新设备"按钮
- **THEN** 系统执行 `adb devices -l` 命令并解析输出
- **THEN** UI 显示所有设备的序列号、型号、连接状态（device/offline/unauthorized）

#### Scenario: 未检测到任何设备
- **WHEN** 用户点击"刷新设备"按钮且无设备连接
- **THEN** 系统显示"未检测到设备"提示
- **THEN** UI 提供 ADB 环境配置指引链接

#### Scenario: ADB 命令执行失败
- **WHEN** ADB 未安装或环境变量未配置
- **THEN** 系统捕获异常并显示 Toast 提示"ADB 不可用，请检查环境配置"

### Requirement: 系统能够选择并连接指定设备
系统 SHALL 允许用户从设备列表中选择目标设备，并将其设置为后续所有 ADB 命令的默认设备。

#### Scenario: 选择单个设备
- **WHEN** 用户点击设备列表中的某个设备
- **THEN** 系统高亮显示该设备
- **THEN** 后续所有 ADB 命令自动附加 `-s <serial>` 参数

#### Scenario: 多设备环境下未选择设备
- **WHEN** 存在多个设备且用户未选择任何设备
- **THEN** 系统禁用所有需要设备连接的功能按钮
- **THEN** 显示提示"请先选择目标设备"

### Requirement: 系统能够执行 ADB 命令并实时输出日志
系统 SHALL 提供日志面板，实时流式输出所有 ADB 命令的执行结果，包括 stdout 和 stderr。

#### Scenario: 成功执行命令并输出日志
- **WHEN** 系统执行 `adb shell screencap -p` 命令
- **THEN** 日志面板实时显示命令执行过程
- **THEN** 命令完成后显示执行耗时与状态（成功/失败）

#### Scenario: 命令执行超时
- **WHEN** ADB 命令执行超过 5 秒未响应
- **THEN** 系统终止命令进程
- **THEN** 日志面板显示"命令执行超时"错误信息

#### Scenario: 设备连接中断
- **WHEN** 命令执行过程中设备断开连接
- **THEN** 系统捕获异常并显示 Toast 提示"设备连接中断"
- **THEN** 自动刷新设备列表

### Requirement: 系统能够支持 USB 和 TCP/IP 连接模式
系统 SHALL 支持通过 USB 和 TCP/IP（无线调试）两种方式连接 Android 设备。

#### Scenario: USB 连接模式
- **WHEN** 设备通过 USB 线连接到计算机
- **THEN** 系统通过 `adb devices` 自动检测到设备
- **THEN** 设备列表显示连接类型为"USB"

#### Scenario: TCP/IP 连接模式
- **WHEN** 用户输入设备 IP 地址和端口（如 192.168.1.100:5555）并点击"连接"
- **THEN** 系统执行 `adb connect <ip>:<port>` 命令
- **THEN** 连接成功后设备列表显示连接类型为"TCP/IP"

#### Scenario: TCP/IP 连接失败
- **WHEN** 用户输入的 IP 地址无法连接
- **THEN** 系统显示 Toast 提示"连接失败，请检查设备 IP 和端口"
- **THEN** 日志面板输出详细错误信息

### Requirement: 系统能够异步执行 ADB 命令避免 UI 阻塞
系统 SHALL 使用异步架构执行所有 ADB 命令，确保 UI 线程不被阻塞。

#### Scenario: 长时间命令执行不阻塞 UI
- **WHEN** 系统执行耗时 ADB 命令（如截图拉取）
- **THEN** UI 保持响应，用户可继续操作其他功能
- **THEN** 命令执行期间显示加载指示器

#### Scenario: 并发执行多个命令
- **WHEN** 用户同时触发截图拉取和 UI Dump 拉取
- **THEN** 系统并发执行两个命令
- **THEN** 每个命令独立完成并更新对应 UI 区域
