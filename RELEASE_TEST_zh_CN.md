# Release 测试文档入口（先看这个）

如果你现在是在做 **GitHub Actions 发版前测试 / 验包 / 补包 / 正式发版前确认**，请按下面顺序看文档：

## 1. 先看操作手册

- 文件：`manual.md`
- 作用：告诉你 **manual-release-test** 和 **release-please** 应该怎么跑、每一步看什么、怎么判断通过

## 2. 再看验证清单

- 文件：`checklist.md`
- 作用：告诉你这版包从用户视角是否真的达到可发布标准

## 3. 如果 GitHub 推送 / 代理有问题

- 文件：`PROXY_zh_CN.md`
- 作用：告诉你为什么本地有 workflow 但 GitHub 上可能还看不到，以及 GitHub / 代理 / push 怎么配

## 4. 如果你在改 workflow / release 脚本

- 文件：`DEVELOPMENT_zh_CN.md`
- 作用：解释本项目的发布路径、工作流职责，以及“为什么真实跑通的参考只能先跟随、不能顺手乱改”

---

## 最短路径（只看结论）

### 我现在要跑 `manual-release-test`

先看：

1. `manual.md`
2. `checklist.md`

### 我现在要解决 GitHub Push / Actions 不显示

先看：

1. `PROXY_zh_CN.md`
2. `manual.md`

### 我现在要改 workflow 里的 token / secret / env

先看：

1. `DEVELOPMENT_zh_CN.md`

---

## 一句话记忆

### 发版前测试文档入口就是：

- `RELEASE_TEST_zh_CN.md` ← 先看这个
- `manual.md`
- `checklist.md`

