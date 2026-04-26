# 开发说明

[English](DEVELOPMENT.md) | [简体中文](DEVELOPMENT_zh_CN.md)

这个项目的发布维护可以按三条路径来理解：

- **日常开发**：正常开发、正常提交，不自动打整包
- **测试打包 / 补包**：需要确认包是否可用，或者给已有 Release 补文件时，使用 `manual-release-test`
- **正式发布**：确认包没问题后，再交给 `release-please` 去完成正式发布

这样做的目的很简单：

- 日常开发不浪费构建资源
- 验包时可以单独进行，不污染正式下载页
- 正式发布只处理已经确认过的版本

---

## 日常开发

日常开发阶段，不需要每次推送都自动打 ZIP、EXE、MSIX。

原因很直接：

- 整包构建成本高
- 频繁开发时会产生大量无意义构建
- 大部分提交并不值得立即产出完整安装包

所以日常开发只做正常提交，不自动跑整包发布链路。

如果某次改动后你确实需要确认“现在还能不能正常出包”，再主动运行测试打包流程即可。

---

## 跟随真实可运行参考的规则（重要）

当你手上已经有一个**真实跑通过**的参考仓库、参考 workflow、参考脚本时，默认策略不是“先顺手优化”，而是：

> **先跟随，再理解；先尊重接口，再决定是否调整。**

这条规则在 GitHub Actions、打包脚本、CLI 调用、manifest 配置这类问题上尤其重要。

### 为什么要这样做

因为这类参考往往不是“看起来差不多”的示例，而是：

- 已经被真实平台行为验证过
- 已经踩过 GitHub / `gh` CLI / Action 输入 / 环境变量映射的坑
- 每一个看似“不统一”的写法，背后都可能是接口约束

如果没先搞清楚这些约束，就直接做“命名统一”“风格整理”“看起来更顺”的改动，很容易把原本已跑通的链路改坏。

### 必须先区分的三层名字

在 workflow 里，至少要先分清以下三层：

1. **secret 存储名**
   - 例如：`secrets.GH_TOKEN`

2. **Action 输入名**
   - 例如：`with: token: ...`

3. **下游程序读取的环境变量接口名**
   - 例如：`env: GITHUB_TOKEN: ...`

这三层名字不是同一个概念，不能因为右边 secret 改名了，就把左边接口名也顺手一起改掉。

### 典型例子：为什么这里只改右边，不改左边

如果参考仓库里写的是：

```yaml
with:
  token: ${{ secrets.GH_TOKEN }}

env:
  GITHUB_TOKEN: ${{ secrets.GH_TOKEN }}
```

那它表达的是：

- `secrets.GH_TOKEN`：这是**值从哪里来**
- `with.token`：这是 **Action 需要的输入接口**
- `env.GITHUB_TOKEN`：这是 **下游 CLI / 工具读取的接口名**

所以这种情况下，正确思路是：

- 可以改右边的 secret 来源
- 不要为了表面统一，随手改左边的接口名

补充佐证：

- GitHub CLI 官方环境变量文档明确说明，`gh` 会读取 `GH_TOKEN` 与 `GITHUB_TOKEN` 这类约定名称，而不是读取你仓库 secret 的名字本身  
  文档：<https://cli.github.com/manual/gh_help_environment>

这也再次说明：

- 右边 `${{ secrets.GH_TOKEN }}` 是**值来源**
- 左边 `GITHUB_TOKEN` / `GH_TOKEN` 是**下游程序识别的接口名**
- 处理这类映射时，必须先看消费者接口，再决定左边能不能动

### 默认工作方法

以后遇到“参考仓库已经真实跑通”的情况，按这个顺序处理：

1. **先假设参考写法有原因**
2. **先判断左边是接口，还是你自己定义的变量**
3. **能不改接口名就不改**
4. **如果要改，先找到官方文档或真实运行依据**
5. **没有充分依据时，优先保持参考原样**

### 一句话原则

> **真实可运行的参考，不是拿来顺手规范化的，而是拿来先还原其约束的。**

---

## 本地 release 验包前置依赖

如果你要在本机先把 ZIP / EXE 验通，再交给 CI，至少要有：

- .NET 8 SDK
- Visual Studio 2022/2026 或 Build Tools（包含 **MSBuild** 和 Windows 10/11 SDK，也就是 **SignTool**）
- Inno Setup 6（提供 `ISCC.exe`）

现在的 release 脚本会自动探测这些工具；如果缺失，会尽量在一开始就报清楚，而不是拖到后面的打包阶段再模糊失败。

### GitHub 提交 / 代理前置说明

如果你当前网络环境无法直接访问 GitHub，请先完成代理设置，再继续：

- 推代码到 GitHub
- 推送 `.github/workflows/*`
- 验证 GitHub Actions

详见：

- [`PROXY_zh_CN.md`](PROXY_zh_CN.md)

特别注意：

- 如果 `origin` 是 `git@github.com:...`，单纯设置 `HTTP_PROXY` / `HTTPS_PROXY` 可能仍然无法 `git push`
- 如果只是为了尽快把本项目提交到 GitHub，推荐默认方案：**把 GitHub 远端改成 HTTPS，再给 Git 配代理**

---

## 推荐的本地验包顺序

本机验证 release 候选版本时，建议按这个顺序走：

1. `dotnet restore autojs6-dev-tools.slnx`
2. `dotnet build autojs6-dev-tools.slnx -c Release`
3. `dotnet test autojs6-dev-tools.slnx -c Release`
4. 构建 `win-x64` 和 `win-arm64` 便携 ZIP
5. 用 `scripts/release/Test-PortablePackageSmoke.ps1` 对 `win-x64` 便携版 EXE 做一次冒烟启动检查
6. 构建 `win-x64` 和 `win-arm64` EXE 安装器
7. 检查 `release-assets/` 里的文件名、版本号、发布者和 SHA256 清单是否一致

CI 应该放在本地验证之后做复验，而不是完全替代本地验包。

---

## CI 无交互安全说明与卡住点排查

这条发布链路现在已经专门按“**会不会在 CI 里等人点确认**”这个标准做过检查。

### 已确认的高风险交互点

- `scripts/release/New-CodeSigningCertificate.ps1`
  - 风险来源：把生成的证书导入 `TrustedPeople` / `Root`
  - 为什么会卡住：导入 `Root` 信任根时，Windows 可能弹出信任确认；在无桌面的 GitHub Actions 里就会表现成卡死
  - 现在的行为：
    - CI / 无交互环境会自动跳过证书信任导入
    - 本机导入改成**显式开启**
    - 只有手动传下面参数时才会尝试导入：
      - `-ImportToTrustedPeople`
      - `-ImportToRoot`

### 当前发版策略

- `manual-release-test` 和 `release-please` 已暂时关闭 MSIX 发包
- 当前受支持的发布产物只有：
  - ZIP
  - EXE 安装器
- MSIX 保留到后续版本再恢复，前提是证书信任、签名校验、安装流程和最终用户体验全部端到端验证通过

### 已检查的发布脚本：正常情况下不应等待人工确认

- `scripts/release/Build-PortablePackage.ps1`
  - 本质是 `dotnet publish`
  - 脚本本身没有交互确认路径

- `scripts/release/Build-InnoInstaller.ps1`
  - 调用 `ISCC.exe`
  - 没有脚本级确认逻辑
  - 如果 Inno Setup 缺失，应该直接失败，而不是等待输入

- `scripts/release/Build-MsixPackage.ps1`
  - 目前仅保留在仓库中，供后续版本继续实现
  - 当前不属于 CI 正式发版链路

- `scripts/release/Set-AppReleaseMetadata.ps1`
  - 只改 manifest 文件，无交互路径

- `scripts/release/Test-PortablePackageSmoke.ps1`
  - 启动程序后只等待有限秒数，再强制结束
  - 这是有上限的冒烟等待，不是无限期卡住

### 明确不应放进 CI 的本地安装脚本

- `App/AppPackages/**/Add-AppDevPackage.ps1`
- `App/AppPackages/**/Install.ps1`

这些是本地侧载 / 本地安装用脚本，天然更接近“装包”和“信任证书”场景，可能涉及交互，不属于 GitHub Actions 发版链路，不应该在 CI 里调用。

### 如果 workflow 看起来“卡住了”，最快排查方法

如果 Windows 发版任务看起来不动了，按这个顺序判断：

1. 看**最后一行日志**停在哪里
2. 先定位具体卡在**哪个脚本 / 哪个 step**
3. 默认把最后一个可见操作当成第一嫌疑点
4. 如果明显是信任、安装、UI、证书确认类动作，直接取消运行
5. 先把脚本改成 fail-fast 或 non-interactive，再重新跑

### 一句话规则

> 在 CI 里可以生成包，但不应该默认修改机器信任状态。

---

## 测试打包 / 补包

测试打包和补包统一使用：

- GitHub Actions 工作流：`manual-release-test`

这条流适合两种最常见的情况：

### 1. 想先确认包还能不能正常用

例如：

- 工作流刚改过
- 打包脚本刚改过
- 发布前想先确认 ZIP / EXE 都还能正常生成

这时候直接运行 `manual-release-test` 即可。

最常见的做法是：

- 从 `main` 打包
- 给一个正常的测试版本号
- 不上传到 GitHub Release

这样跑完以后，所有产物都会放在这次 Actions 运行结果里，适合下载下来自己验证，但不会影响正式 Release 页面。

### 2. 正式 Release 已存在，但文件不完整

例如：

- 正式 Release 页面已经创建了
- 但 ZIP / EXE 缺文件
- 或者上传过程中断了

这时候不需要立刻发一个新版本，应该先补包。

做法是：

- 对着那个已经存在的正式 tag 重新打包
- 保持同一个版本号
- 允许上传到 GitHub Release
- 把产物补回同一个 Release

这样版本历史不会乱，处理方式也最干净。

### 运行测试打包时，真正需要关心的事

你不用把所有输入项都当成重点，只需要先看这三件事：

#### 代码从哪里来

如果只是日常验包，通常直接用 `main`。

如果是补包，就用已经发布出去的那个正式 tag。

#### 这次包显示成什么版本

写正常版本号即可，例如：

- `0.0.1`
- `0.1.0`

#### 这次只是自己验包，还是要上传到 Release

- 如果只是测试，保持不上传
- 如果是在补一个已经存在的 Release，才打开上传

这就是测试打包时最需要判断的部分。

---

## 正式发布

正式发布统一由：

- GitHub Actions 工作流：`release-please`

来完成。

推荐顺序如下：

1. 日常功能和修复先正常合入 `main`
2. 等待 `release-please` 创建或更新 release PR
3. 不要急着立刻合并
4. 先运行一次 `manual-release-test`
5. 确认主要产物已经可用
6. 再合并 release PR
7. 让 `release-please` 自动完成正式发布

这样做的好处是：

- 正式版本号更稳定
- 正式 tag 不会被测试动作污染
- 出问题时更容易追踪是哪一版出了问题

可以把这两条流理解成：

- `manual-release-test` 负责确认“这包现在能不能交给用户”
- `release-please` 负责确认“这版现在正式发出去”

---

## 从最终用户角度，最重要的产物是什么

每次完整发包后，都会有一整套文件，但真正决定普通用户体验的，主要还是这两个：

- **EXE**：最符合普通 Windows 用户习惯，下载安装即可使用
- **ZIP**：适合不想安装、只想解压直接运行的用户

如果要从“用户下载后会不会立刻放弃”来判断优先级，应该优先保证：

- EXE 安装流程顺畅
- ZIP 解压后可直接运行

这两个做好了，发布体验才真正站得住。

---

## 出问题时怎么处理

### 测试打包都跑不过

这通常说明问题在当前代码或打包配置本身，而不是正式发布动作。

这时候应该先修：

- 代码问题
- 打包脚本问题
- 工作流配置问题

在测试打包通过之前，不要继续正式发布。

### 正式 Release 已创建，但文件没传全

这种情况说明版本已经存在，只是下载页不完整。

优先做法是补包，而不是立刻发一个新版本。

也就是重新运行 `manual-release-test`，对着同一个正式 tag 重打，并把产物补传回同一个 Release。

### 正式包已经发出，但用户用不了

如果问题已经影响最终用户，例如：

- 安装失败
- 打不开
- 一启动就崩

更稳妥的处理方式是：

1. 在 `main` 上修复问题
2. 发布下一个补丁版本

例如：

- `0.0.1` 有问题
- 修复后发布 `0.0.2`

除非有非常明确的原因，否则不要优先去改写已经发布的正式 tag。

### 本地 `dotnet build -c Release` 在打包前就失败

优先检查这几件事：

- App 项目是否还在普通 Release 构建里默认开启 trim / ReadyToRun
- MSBuild 是否落到了 AnyCPU，而不是明确的平台
- 如果当前还没走到 MSIX 这一步，先把基础 Release 构建修通，再看发布脚本

### 本地 MSIX 构建报证书或签名错误

按顺序检查：

- 证书 `Subject` 是否与 `App/Package.appxmanifest` 里的 `Publisher` 完全一致
- `signtool.exe` 是否已安装并可被脚本发现
- 本地验证前，生成出来的 `.cer` 是否已同时导入当前用户的 **Trusted People** 和 **Trusted Root Certification Authorities**

这几项不满足时，优先按环境 / 签名配置问题处理，不要直接怀疑业务代码。

### 本地 EXE 安装器一开始就构建失败

优先检查：

- `ISCC.exe` 是否存在（例如 Inno Setup 6 是否已安装）
- 发布源目录里是否真的有构建好的应用文件
- 安装器输出目录是否可写

---

## 当前发布标识

当前这套发布配置使用的是：

- 产品名：`AutoJS6 Visual Development Toolkit`
- 包标识：`space.terwer.autojs6devtools`
- 发布者：`CN=terwer`

如果后面发现安装器名称、包名、发布者信息异常，先检查这里，再继续往下查。

---

## 需要深入排查时再看的文件

- `.github/workflows/release-please.yml`
- `.github/workflows/manual-release-test.yml`
- `scripts/release/Set-AppReleaseMetadata.ps1`
- `scripts/release/New-CodeSigningCertificate.ps1`
- `scripts/release/Build-PortablePackage.ps1`
- `scripts/release/Build-InnoInstaller.ps1`
- `packaging/windows/autojs6-dev-tools.iss`
- `packaging/windows/ChineseSimplified.isl`
