# GitHub / 代理设置说明

## 1. 适用场景

如果你所在网络环境无法直接访问 GitHub，本项目在以下环节都可能失败：

- `git clone`
- `git fetch / pull / push`
- 访问 GitHub 网页
- 把 `.github/workflows/*` 推到 GitHub 之后在 Actions 页面验证工作流

> 重要：  
> **如果 workflow 文件还没真正 push 到 GitHub 默认分支，GitHub Actions 页面就可能完全看不到对应工作流。**  
> 所以“先解决代理，确保能 push 到 GitHub”，是本项目使用 GitHub Actions 的前置条件。

---

## 2. 先确认你当前用的是 HTTPS 远端还是 SSH 远端

执行：

```powershell
git remote -v
```

常见结果有两种：

### 2.1 HTTPS 远端

```text
origin  https://github.com/terwer/autojs6-dev-tools.git (fetch)
origin  https://github.com/terwer/autojs6-dev-tools.git (push)
```

### 2.2 SSH 远端

```text
origin  git@github.com:terwer/autojs6-dev-tools.git (fetch)
origin  git@github.com:terwer/autojs6-dev-tools.git (push)
```

---

## 3. 先说结论：最省事的方案

### 推荐默认方案：改成 HTTPS 远端，再配 Git 代理

原因：

- 配置最简单
- 最容易排查
- 对大多数本地代理软件最友好
- 不依赖额外 SSH 代理工具

如果你只是想尽快把代码 push 到 GitHub，**优先推荐这一条**。

---

## 4. 方案 A：使用 HTTPS 远端（推荐）

## 4.1 PowerShell 当前窗口临时代理

如果你的代理提供的是 SOCKS5 端口，例如 `127.0.0.1:12334`，先在当前 PowerShell 会话里设置：

```powershell
$env:HTTP_PROXY="socks5h://127.0.0.1:12334"
$env:HTTPS_PROXY="socks5h://127.0.0.1:12334"
$env:ALL_PROXY="socks5h://127.0.0.1:12334"
```

如果你的代理提供 HTTP 端口，优先可以写成：

```powershell
$env:HTTP_PROXY="http://127.0.0.1:7890"
$env:HTTPS_PROXY="http://127.0.0.1:7890"
```

> 经验建议：  
> 如果本机代理同时提供 HTTP 和 SOCKS5 端口，**Git / curl 在 Windows 上优先试 HTTP 端口**，通常更稳。

---

## 4.2 直接测试网页连通性

建议用 `curl.exe`，避免 PowerShell 别名混淆：

```powershell
curl.exe --proxy socks5h://127.0.0.1:12334 https://github.com
```

如果你用 HTTP 代理：

```powershell
curl.exe --proxy http://127.0.0.1:7890 https://github.com
```

如果这一步都不通，先不要继续折腾 Git。

---

## 4.3 把 GitHub 远端改成 HTTPS

本仓库当前如果还是 SSH 远端，可以改成：

```powershell
git remote set-url origin https://github.com/terwer/autojs6-dev-tools.git
```

然后检查：

```powershell
git remote -v
```

确保 `origin` 已经变成 `https://github.com/...`

---

## 4.4 给 Git 配置代理

### 全局配置

```powershell
git config --global http.proxy socks5h://127.0.0.1:12334
git config --global https.proxy socks5h://127.0.0.1:12334
```

如果你使用 HTTP 代理端口：

```powershell
git config --global http.proxy http://127.0.0.1:7890
git config --global https.proxy http://127.0.0.1:7890
```

### 仅对当前仓库配置

```powershell
git config --local http.proxy socks5h://127.0.0.1:12334
git config --local https.proxy socks5h://127.0.0.1:12334
```

---

## 4.5 验证 GitHub 连通性

先测试：

```powershell
git ls-remote origin
```

再测试推送前基本链路：

```powershell
git fetch origin
```

如果这两步都通了，再继续正常 `push`。

---

## 4.6 推送完成后要做什么

如果你这次推的是：

- `.github/workflows/manual-release-test.yml`
- `.github/workflows/release-please.yml`

那么 push 完成后再去看 GitHub Actions 页面。

否则你本地虽然有 workflow 文件，GitHub 页面仍然可能看不到。

---

## 5. 方案 B：保留 SSH 远端

如果你坚持继续使用：

```text
git@github.com:terwer/autojs6-dev-tools.git
```

那就要注意：

> **仅设置 `HTTP_PROXY` / `HTTPS_PROXY` 不一定能让 SSH Push 生效。**

因为 SSH 走的是另一套链路。

---

## 5.1 需要额外配置 SSH 代理

编辑：

```text
C:\Users\<你的用户名>\.ssh\config
```

示例（使用 `ncat` 走 SOCKS5）：

```sshconfig
Host github.com
  HostName github.com
  User git
  ProxyCommand ncat --proxy 127.0.0.1:12334 --proxy-type socks5 %h %p
```

如果你还有别的 GitHub 仓库，也可以继续复用这个配置。

---

## 5.2 验证 SSH 是否可用

```powershell
ssh -T git@github.com
```

如果能握手成功，再继续：

```powershell
git ls-remote origin
git fetch origin
git push origin <branch>
```

---

## 5.3 如果本机没有 `ncat`

最简单的替代方案不是继续折腾 SSH，而是：

> **直接切回 HTTPS 远端。**

对这个项目来说，这是更省时间的方案。

---

## 6. 常见误区

### 6.1 已经设置了 PowerShell 代理，为什么 `git push` 还是不通？

因为你的 `origin` 很可能还是：

```text
git@github.com:...
```

也就是 SSH 远端。  
这时 HTTP 代理变量未必能自动接管 SSH。

### 6.2 `curl https://x.com` 不通，说明 GitHub 一定也不通？

不一定，但通常说明当前代理链本身就没打通。  
建议先把 `curl.exe --proxy ... https://github.com` 跑通，再测 Git。

### 6.3 GitHub Actions 页面没显示 workflow，是不是 GitHub 缓存？

很多时候不是缓存，而是：

- workflow 文件还没 push 到 GitHub
- workflow 文件不在默认分支
- push 根本没成功

所以第一步不是刷新页面，而是先验证：

```powershell
git push origin <branch>
```

真的成功了没有。

---

## 7. 代理配置完成后的推荐验证顺序

1. `curl.exe --proxy ... https://github.com`
2. `git remote -v`
3. `git ls-remote origin`
4. `git fetch origin`
5. `git push origin <branch>`
6. 到 GitHub 网页确认分支更新
7. 再去看 Actions 页面是否出现 workflow

---

## 8. 如何取消代理

### 8.1 清空当前 PowerShell 会话环境变量

```powershell
Remove-Item Env:HTTP_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:HTTPS_PROXY -ErrorAction SilentlyContinue
Remove-Item Env:ALL_PROXY -ErrorAction SilentlyContinue
```

### 8.2 删除 Git 全局代理

```powershell
git config --global --unset http.proxy
git config --global --unset https.proxy
```

### 8.3 删除 Git 当前仓库代理

```powershell
git config --local --unset http.proxy
git config --local --unset https.proxy
```

---

## 9. 对本项目的建议

如果你当前只是为了尽快把本项目推到 GitHub，并让 Actions 可见，推荐优先采用：

1. **把 `origin` 改成 HTTPS**
2. **给 Git 配置代理**
3. **先确认 `git push origin <branch>` 成功**
4. **再回头验证 GitHub Actions**

这样通常比继续折腾 SSH 代理快得多。

