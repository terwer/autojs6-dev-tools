# GitHub / Proxy Setup

## 1. When this document is needed

If your network cannot reach GitHub directly, the following tasks in this project may fail:

- `git clone`
- `git fetch / pull / push`
- opening GitHub in the browser
- pushing `.github/workflows/*` to GitHub before validating Actions

> Important:  
> If the workflow files have not actually been pushed to the default branch on GitHub, the Actions page may show no usable workflows at all.

---

## 2. Check whether your remote uses HTTPS or SSH

Run:

```powershell
git remote -v
```

Common forms:

### HTTPS remote

```text
origin  https://github.com/terwer/autojs6-dev-tools.git (fetch)
origin  https://github.com/terwer/autojs6-dev-tools.git (push)
```

### SSH remote

```text
origin  git@github.com:terwer/autojs6-dev-tools.git (fetch)
origin  git@github.com:terwer/autojs6-dev-tools.git (push)
```

---

## 3. Recommended default approach

For most developers, the simplest approach is:

1. switch the GitHub remote to **HTTPS**
2. configure a Git proxy

This is usually easier to debug than keeping an SSH remote behind a proxy.

---

## 4. Option A: use HTTPS remote (recommended)

### Temporary proxy in the current PowerShell session

For a SOCKS5 proxy:

```powershell
$env:HTTP_PROXY="socks5h://127.0.0.1:12334"
$env:HTTPS_PROXY="socks5h://127.0.0.1:12334"
$env:ALL_PROXY="socks5h://127.0.0.1:12334"
```

For an HTTP proxy:

```powershell
$env:HTTP_PROXY="http://127.0.0.1:7890"
$env:HTTPS_PROXY="http://127.0.0.1:7890"
```

### Test basic connectivity

```powershell
curl.exe --proxy socks5h://127.0.0.1:12334 https://github.com
```

### Switch the GitHub remote to HTTPS

```powershell
git remote set-url origin https://github.com/terwer/autojs6-dev-tools.git
```

### Configure Git proxy

```powershell
git config --global http.proxy socks5h://127.0.0.1:12334
git config --global https.proxy socks5h://127.0.0.1:12334
```

### Verify Git access

```powershell
git ls-remote origin
git fetch origin
```

---

## 5. Option B: keep the SSH remote

If you keep:

```text
git@github.com:terwer/autojs6-dev-tools.git
```

then plain `HTTP_PROXY` / `HTTPS_PROXY` may not be enough.  
You usually need SSH proxy support as well.

Example `~/.ssh/config` using `ncat` with SOCKS5:

```sshconfig
Host github.com
  HostName github.com
  User git
  ProxyCommand ncat --proxy 127.0.0.1:12334 --proxy-type socks5 %h %p
```

Then test:

```powershell
ssh -T git@github.com
git ls-remote origin
```

If your machine does not have `ncat`, the faster fix is usually to switch the remote to HTTPS instead.

---

## 6. Common mistake

If `origin` still points to `git@github.com:...`, do not assume that setting only `HTTP_PROXY` and `HTTPS_PROXY` will make `git push` work.

That setup often fixes browser and curl traffic, but not SSH-based Git pushes.

---

## 7. Recommended verification order

1. `curl.exe --proxy ... https://github.com`
2. `git remote -v`
3. `git ls-remote origin`
4. `git fetch origin`
5. `git push origin <branch>`
6. confirm the branch update on GitHub
7. only then check whether the Actions workflows are visible

