# GitHub Actions 使用手册

## A. `manual-release-test`

## 用途

这条工作流只做一件事：

- **按你指定的代码来源打包并产出文件**

可选动作：

- 上传到 GitHub Release

---

## 什么时候用

用在下面两种情况：

1. **只想测试打包**
2. **想测试上传到 GitHub Release**

---

## 在哪里打开

1. 打开 GitHub 仓库
2. 打开 **Actions**
3. 左侧点击 **manual-release-test**
4. 右上点击 **Run workflow**

---

## `Use workflow from` 怎么选

- 如果你要测试 `dev` 上的代码，就选：`dev`
- 如果你要测试 `main` 上的代码，就选：`main`
- 如果你要测试某个 tag，就选包含该 workflow 的分支，再在下面 `source_ref` 里填 tag

---

## 字段怎么填

| 字段 | 填写规则 |
|---|---|
| `source_ref` | 填这次要打包的**真实代码来源**。可以是分支名，也可以是 tag。 |
| `version` | 填这次要验证的**真实版本号**。格式必须是 `x.y.z`。不要填 `-test`、`-rc`、`v` 前缀。 |
| `publish_to_release` | 只测试打包填 `false`；要上传到 GitHub Release 才填 `true`。 |
| `release_tag` | 只有上传到 GitHub Release 时才填写。填这次上传要使用的真实 tag 名。普通打包测试留空。 |
| `release_name` | 只有上传到 GitHub Release 时才填写。填这次上传要显示的真实标题。普通打包测试留空。 |
| `prerelease` | 只在创建 GitHub Release 时生效。预演发布通常填 `true`。 |

---

## 场景 1：只测试打包

### 操作步骤

1. 打开 **manual-release-test**
2. 点击 **Run workflow**
3. `Use workflow from` 选择你当前要测试的分支
4. 填写：

| 字段 | 怎么填 |
|---|---|
| `source_ref` | 当前要打包的真实分支或真实 tag |
| `version` | 当前要验证的真实版本号 |
| `publish_to_release` | `false` |
| `release_tag` | 留空 |
| `release_name` | 留空 |
| `prerelease` | 保持默认即可 |

5. 点击 **Run workflow**

### 成功后检查

1. 运行结果为绿色成功
2. 下载 artifact
3. 检查是否包含：
   - ZIP
   - setup.exe
   - `SHA256SUMS.txt`
   - `BUILD-INFO.txt`

> 当前版本已临时关闭 MSIX 发包链路；artifact 不再包含 MSIX、证书文件或 MSIX 安装说明。

---

## 场景 2：测试上传到 GitHub Release

### 操作步骤

1. 打开 **manual-release-test**
2. 点击 **Run workflow**
3. `Use workflow from` 选择你当前要测试的分支
4. 填写：

| 字段 | 怎么填 |
|---|---|
| `source_ref` | 当前要打包的真实分支或真实 tag |
| `version` | 当前要验证的真实版本号 |
| `publish_to_release` | `true` |
| `release_tag` | 这次上传目标的真实 tag 名 |
| `release_name` | 这次上传显示的真实标题 |
| `prerelease` | 预演发布通常填 `true` |

5. 点击 **Run workflow**

### 成功后检查

1. 打开 GitHub 仓库 **Releases**
2. 找到你刚才填写的：
   - `release_tag`
3. 检查资产是否上传完整

---

## `manual-release-test` 只看这几个结果

### 成功

- Actions 绿色成功
- artifact 完整
- 如果开了上传，Release 页面资产完整

### 失败

先看失败步骤对应日志。

---

## B. `release-please`

## 用途

这条工作流只做一件事：

- **正式发版**

---

## 什么时候用

只有在下面这个场景使用：

- **要把版本正式发布到 `main`**

---

## 它监听哪个分支

- `main`

如果代码不在 `main`，这条工作流不负责处理。

---

## 开始前确认

开始前只确认这几件事：

1. 工作流已经在 GitHub 上可见
2. 仓库 Secret 已配置：
   - `GH_TOKEN`
3. GitHub 仓库权限已允许：
   - GitHub Actions 创建 PR
   - GitHub Actions 写入内容

---

## 操作步骤

1. 把要发布的代码合到 `main`
2. push 到 GitHub
3. 打开 **Actions**
4. 左侧点击 **release-please**
5. 等待运行结果

---

## 运行后怎么看

### 情况 1：它创建或更新了 release PR

你只做这三步：

1. 打开这个 PR
2. 检查内容
3. 合并 PR

### 情况 2：它已经开始正式产物上传

你只做这两步：

1. 打开 GitHub 仓库 **Releases**
2. 检查正式版本资产是否完整

---

## `release-please` 只看这几个结果

### 成功

- release PR 正常创建 / 更新
- 正式 Release 正常生成
- 正式资产上传完整

### 失败

先看失败步骤对应日志。

---

## 常见问题

| 问题 | 先看哪里 |
|---|---|
| 看不到 workflow | 先确认 workflow 是否已经 push 到 GitHub |
| push 不上 GitHub | `PROXY_zh_CN.md` |
| `release-please` 权限错误 | GitHub 仓库 `Settings → Actions → Workflow permissions` |
| token 错误 | 仓库 Secret `GH_TOKEN` |
| 上传 Release 失败 | `publish_to_release`、`release_tag`、`release_name` 是否填写正确 |
| 日志停在证书导入 / 信任步骤 | 先看 `DEVELOPMENT_zh_CN.md` 的“CI 无交互安全说明与卡住点排查” |

