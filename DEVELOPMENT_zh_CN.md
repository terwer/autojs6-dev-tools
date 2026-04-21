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

## 测试打包 / 补包

测试打包和补包统一使用：

- GitHub Actions 工作流：`manual-release-test`

这条流适合两种最常见的情况：

### 1. 想先确认包还能不能正常用

例如：

- 工作流刚改过
- 打包脚本刚改过
- 发布前想先确认 ZIP / EXE / MSIX 都还能正常生成

这时候直接运行 `manual-release-test` 即可。

最常见的做法是：

- 从 `main` 打包
- 给一个正常的测试版本号
- 不上传到 GitHub Release

这样跑完以后，所有产物都会放在这次 Actions 运行结果里，适合下载下来自己验证，但不会影响正式 Release 页面。

### 2. 正式 Release 已存在，但文件不完整

例如：

- 正式 Release 页面已经创建了
- 但 ZIP / EXE / MSIX 缺文件
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

- `1.0.1`
- `1.2.3`

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

MSIX 也会生成，但当前仍需要先信任证书，所以它不是普通用户最顺手的第一选择。

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

- `1.0.1` 有问题
- 修复后发布 `1.0.2`

除非有非常明确的原因，否则不要优先去改写已经发布的正式 tag。

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
- `scripts/release/Build-MsixPackage.ps1`
- `packaging/windows/autojs6-dev-tools.iss`
- `packaging/windows/MSIX-INSTALL.md`
