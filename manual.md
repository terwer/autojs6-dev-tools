# GitHub Actions 发版前验证手册

## 1. 目的

这份手册用于指导 `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools` 在**正式发版前最后一轮**如何验证 GitHub Actions，确保：

- GitHub 托管 Windows Runner 能成功打包
- ZIP / 安装包 / MSIX / 校验文件能完整产出
- GitHub Release 上传链路可用
- 正式发版时 `release-please` 不会因为工作流问题临门翻车

> 这一步是**发版链路验证**，不是代码功能验证。  
> 代码功能是否可发，先以 `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/checklist.md` 为准。

---

## 2. 本仓库当前涉及的工作流

### 2.1 发版演练工作流

- 文件：`D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/.github/workflows/manual-release-test.yml`
- 触发方式：手动执行 `workflow_dispatch`
- 作用：
  - 校验 release metadata
  - 生成 x64 / ARM64 便携包
  - 生成 x64 / ARM64 安装包
  - 生成 x64 / ARM64 MSIX
  - 生成证书、说明文档、SHA256 校验文件
  - 可选上传到 GitHub Release

### 2.2 正式发版工作流

- 文件：`D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/.github/workflows/release-please.yml`
- 触发方式：`push` 到 `main`
- 作用：
  - 调用 `release-please-action`
  - 在满足条件时创建 tag / release
  - 基于 tag 构建并上传正式发版资产

---

## 3. 发版前的基本原则

### 3.1 先过功能验证，再验 Actions

GitHub Actions 不是第一道门，而是最后一道门。  
推荐顺序：

1. 先完成人工功能验证（按 `checklist.md`）
2. 再验证 GitHub Actions 打包与上传链路
3. 最后才推进正式发版

### 3.2 不要直接拿 `main` 做预演

发版前演练时，`manual-release-test` 的 `source_ref` **不要优先用 `main`**，建议用：

- 候选提交的完整 commit SHA

这样能保证：

- 你知道验证的是哪一个提交
- 演练版本与最终发布版本一致
- 避免 `main` 变化造成结果失真

### 3.3 先验证“能打包”，再验证“能上传”

发版前至少跑两次 `manual-release-test`：

1. **不上传 Release**：验证打包链
2. **上传到临时 prerelease**：验证上传链

---

## 4. 发版前准备

在开始 GitHub Actions 验证之前，先准备以下信息。

### 4.1 候选提交

- 记录本次候选版本对应的 commit SHA

### 4.2 候选版本号

- 必须使用语义化版本号：`x.y.z`
- 例如：`0.0.1`

### 4.3 确认版本配置文件有效

检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/.release-please-manifest.json`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/release-please-config.json`

要求：

- manifest 中根版本存在
- 版本格式正确
- 与你准备发的版本不冲突

### 4.4 确认当前仓库可正常访问 Actions / Releases

至少需要：

- 可以进入 GitHub Actions 页面
- 可以手动执行 workflow
- 有权限查看和下载 artifacts
- 如需上传 Release，需有仓库内容写权限

---

## 5. 第一次验证：只验证打包链，不上传 Release

这是**必须执行**的一轮。

### 5.1 进入工作流

GitHub 仓库页面：

1. 打开 **Actions**
2. 选择工作流 **manual-release-test**
3. 点击 **Run workflow**

### 5.2 推荐输入参数

- `source_ref`：填写候选提交的 **commit SHA**
- `version`：填写候选版本，例如 `0.0.1`
- `publish_to_release`：`false`
- `release_tag`：留空
- `release_name`：留空
- `prerelease`：`true`

### 5.3 这一步的目标

确认 GitHub Windows Runner 上以下步骤全部成功：

- Resolve build metadata
- Check out selected source
- Validate release metadata files
- Set up .NET 8 SDK
- Set up MSBuild
- Restore app project
- Install Inno Setup
- Apply release metadata
- Create signing certificate
- Build x64 portable ZIP
- Build ARM64 portable ZIP
- Build x64 installer
- Build ARM64 installer
- Build x64 MSIX
- Build ARM64 MSIX
- Prepare output files
- Upload build results to Actions artifacts

### 5.4 通过标准

整个 job 必须是绿色成功状态，并且 artifact 中应至少包含以下文件：

- `autojs6-dev-tools-win-x64-portable.zip`
- `autojs6-dev-tools-win-arm64-portable.zip`
- `autojs6-dev-tools-win-x64-setup.exe`
- `autojs6-dev-tools-win-arm64-setup.exe`
- `autojs6-dev-tools-win-x64.msix`
- `autojs6-dev-tools-win-arm64.msix`
- `autojs6-dev-tools-signing.cer`
- `MSIX-INSTALL.md`
- `SHA256SUMS.txt`
- `BUILD-INFO.txt`

### 5.5 本轮检查重点

下载 artifact 后，重点检查：

1. 文件数量是否完整
2. `BUILD-INFO.txt` 中的 `source_ref` 和 `version` 是否正确
3. `SHA256SUMS.txt` 是否存在且非空
4. ARM64 / MSIX 步骤是否真实产物落地，而不是“步骤成功但无文件”

---

## 6. 第二次验证：验证 GitHub Release 上传链

这是**强烈建议执行**的一轮。  
如果不做这一步，你只是验证了“能打包”，没有验证“能上传到 Release”。

### 6.1 再次运行 `manual-release-test`

推荐输入：

- `source_ref`：仍使用同一个候选 commit SHA
- `version`：仍使用同一个版本，例如 `0.0.1`
- `publish_to_release`：`true`
- `release_tag`：显式填写，例如 `preflight-2026-04-24-v0.0.1`
- `release_name`：例如 `Preflight Package v0.0.1`
- `prerelease`：`true`

### 6.2 这一步的目标

在通过所有打包步骤之后，继续验证：

- `gh release view`
- `gh release create`
- `gh release edit`
- `gh release upload`

即：

- Release 能创建
- 已存在 Release 时能编辑
- 资产能上传

### 6.3 通过标准

GitHub **Releases** 页面里应出现一个**临时 prerelease**，并附带完整资产。

### 6.4 本轮必须检查的内容

1. Release 是否成功创建
2. 标题是否正确
3. 资产是否完整
4. 下载是否正常
5. 下载后的文件大小是否合理
6. `SHA256SUMS.txt` 是否可以用于校验下载文件

### 6.5 建议最少抽样下载

至少下载并检查：

- `autojs6-dev-tools-win-x64-portable.zip`
- `autojs6-dev-tools-win-x64-setup.exe`
- `autojs6-dev-tools-win-x64.msix`
- `autojs6-dev-tools-signing.cer`
- `MSIX-INSTALL.md`

### 6.6 这一步结束后的处理建议

临时 prerelease 验证完成后：

- 可保留，作为预演记录
- 或在最终正式发版前手动删除，避免对外展示混乱

---

## 7. 正式发版前的最终判断

满足以下条件后，才建议推进正式发版：

- `checklist.md` 中 P0 项已通过
- 第一次 `manual-release-test`（不上传）通过
- 第二次 `manual-release-test`（上传 prerelease）通过
- prerelease 资产下载抽检通过
- 当前候选提交就是你准备推入 `main` 的正式版本提交

如果以上任一项不满足，不建议直接进入正式发版。

---

## 8. 正式发版时如何观察 `release-please`

正式发版 workflow 文件：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/.github/workflows/release-please.yml`

### 8.1 触发方式

向 `main` 推送正式候选提交。

### 8.2 需要观察的两种情况

#### 情况 A：只生成或更新 release PR

这说明还没有真正开始正式打包发版。

#### 情况 B：`release_created == true`

这说明真正进入正式发版链。

只有出现这种情况，后续打包上传步骤才会执行。

### 8.3 正式发版时重点盯的步骤

- Check out repository
- Validate release metadata files
- `googleapis/release-please-action`
- Check out tagged source
- Resolve release version
- Restore app project
- Install Inno Setup
- Apply release metadata
- Create signing certificate
- Build x64 / ARM64 portable ZIP
- Build x64 / ARM64 installer
- Build x64 / ARM64 MSIX
- Prepare release support files
- Upload release assets

### 8.4 正式通过标准

GitHub 正式 Release 页面中：

- 有正确的 tag
- 有正确的版本号
- 有完整的发布资产
- 文件可下载
- 文件大小与预演时基本一致

---

## 9. 推荐的实际执行顺序

这是本项目最稳妥的发版前验证顺序。

### 9.1 发版前

1. 完成 `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/checklist.md` 的 P0 验证
2. 选定候选提交 SHA
3. 运行一次 `manual-release-test`，`publish_to_release=false`
4. 检查 artifact 完整性
5. 再运行一次 `manual-release-test`，`publish_to_release=true`
6. 检查临时 prerelease 页面与下载结果

### 9.2 正式发版

7. 将候选提交推进到 `main`
8. 观察 `release-please`
9. 检查正式 Release 页面
10. 抽样下载正式资产进行最终确认

---

## 10. 常见失败点与排查顺序

### 10.1 `Validate release metadata files` 失败

优先检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/.release-please-manifest.json`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/release-please-config.json`

重点看：

- JSON 是否合法
- 版本号是否为 `x.y.z`
- 根版本是否为空

### 10.2 `Apply release metadata` 失败

优先检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/scripts/release/Set-AppReleaseMetadata.ps1`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/App/Package.appxmanifest`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/App/app.manifest`

重点看：

- manifest 节点是否还存在
- Publisher / Name / Version 是否可被正确写入

### 10.3 `Build x64/ARM64 portable ZIP` 失败

优先检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/scripts/release/Build-PortablePackage.ps1`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/App/App.csproj`

重点看：

- `dotnet publish` 是否成功
- 指定 RID 是否能产出 EXE
- 输出目录中是否存在 `autojs6-dev-tools.exe`

### 10.4 `Build x64/ARM64 installer` 失败

优先检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/scripts/release/Build-InnoInstaller.ps1`
- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/packaging/windows/autojs6-dev-tools.iss`

重点看：

- Inno Setup 是否正确安装
- `SourceDirectory` 下文件是否完整
- 输出路径是否正确

### 10.5 `Build x64/ARM64 MSIX` 失败

优先检查：

- `D:/Users/Administrator/Documents/myproject/autojs6-dev-tools/scripts/release/Build-MsixPackage.ps1`

重点看：

- MSBuild 是否可用
- SignTool 是否可用
- 自签证书是否生成成功
- `.msix` 是否真实生成
- 签名与校验是否成功

### 10.6 `Upload files to GitHub Release` 失败

优先检查：

- GitHub token 是否可用
- workflow 是否有 `contents: write`
- release tag 是否冲突
- 是否存在资产重名但上传失败的情况

---

## 11. 这个仓库当前要特别注意的事项

### 11.1 `manual-release-test` 不等于完整 CI

它当前不会自动跑测试，所以你不能只看 Actions 绿了就认定可发。

必须配合：

- `checklist.md`
- 本地/人工抽检

### 11.2 建议预演用 commit SHA，不要直接用 `main`

这是避免“验证的是 A，最后发的是 B”的最有效办法。

### 11.3 如果你想验证“上传链”就必须开 `publish_to_release=true`

只跑 dry-run，不足以证明 Release 上传过程可用。

---

## 12. 最终发布判断标准

满足以下全部条件时，可以认为 GitHub Actions 层面的发版前保险已到位：

- `manual-release-test` dry-run 成功
- `manual-release-test` prerelease 上传成功
- prerelease 资产完整且可下载
- 正式发版前候选提交未变化
- `release-please` 正式执行成功
- 正式 Release 页面资产完整

如果你只能做一件事来降低临门翻车风险，那就做：

> **在正式发版前，跑一次带 `publish_to_release=true` 的 `manual-release-test` 预演。**

---

## 13. 一页版执行清单（发版当天照着做）

### A. 发版前 5 分钟确认

- [ ] `checklist.md` 的 P0 已通过
- [ ] 已确定候选 commit SHA
- [ ] 已确定发版版本号（如 `0.0.1`）
- [ ] 本次是否真的要发 ARM64 / MSIX 已确认
- [ ] `.release-please-manifest.json` 中版本格式正确

### B. 先跑 dry-run

工作流：`manual-release-test`

输入：

- `source_ref` = 候选 commit SHA
- `version` = 本次版本号
- `publish_to_release` = `false`
- `release_tag` = 留空
- `release_name` = 留空
- `prerelease` = `true`

检查：

- [ ] job 全绿
- [ ] artifact 上传成功
- [ ] 产物齐全（ZIP / setup / MSIX / cer / SHA256SUMS / BUILD-INFO）
- [ ] `BUILD-INFO.txt` 中版本与提交正确

### C. 再跑一次 prerelease 预演

工作流：`manual-release-test`

输入：

- `source_ref` = 同一个候选 commit SHA
- `version` = 同一个版本号
- `publish_to_release` = `true`
- `release_tag` = 例如 `preflight-2026-04-24-v0.0.1`
- `release_name` = 例如 `Preflight Package v0.0.1`
- `prerelease` = `true`

检查：

- [ ] job 全绿
- [ ] GitHub Releases 页面出现临时 prerelease
- [ ] 资产齐全
- [ ] 至少抽样下载 x64 ZIP / x64 setup / x64 MSIX
- [ ] `SHA256SUMS.txt` 可用

### D. 满足下面条件再推 `main`

- [ ] dry-run 成功
- [ ] prerelease 上传成功
- [ ] 下载抽检通过
- [ ] 候选提交未变化

### E. 正式发版时

- [ ] 将候选提交推进到 `main`
- [ ] 打开 `release-please` workflow 观察执行
- [ ] 确认出现正式 tag / release
- [ ] 确认正式 Release 资产完整
- [ ] 最少再抽样下载一个正式包确认

### F. 一票否决项

出现以下任一情况，先停发：

- [ ] `manual-release-test` 任一步失败
- [ ] prerelease 无法上传
- [ ] Release 资产不完整
- [ ] 版本号 / 提交号对不上
- [ ] 正式 Release 页面资产异常或下载损坏
