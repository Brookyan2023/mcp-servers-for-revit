# MCP-LLM 执行版（1页）

适用范围：Revit 2019-2026，MCP 工具调用场景。  
用途：给 LLM 作为“会话执行口令”，强调必须遵守的最小规则；本文件最多 70 行。

## A. 执行优先级
- 先用现有 MCP 工具；仅当工具无法覆盖时，才使用 `send_code_to_revit`。
- 先读后写、先小后大、先确认后执行。
- 有历史可复用结果时，直接复用，避免重复计算/重复推理。

## B. 歧义与确认
- 出现多匹配、条件不完整、或影响范围较大（建议 >20）时，必须先停并请求确认。
- 禁止猜测性执行。

## C. 写操作安全
- 删除、批量改参、批量创建前，先给“变更预览”：
- 目标数量
- 目标类别
- 关键过滤条件
- 执行后给“变更摘要”：成功数、失败数、失败原因。

## D. 上下文校验
- 每次执行前检查：
- Revit 版本
- 当前文档/视图（可获取时）
- MCP 连接状态
- 出现 ExternalEvent/连接异常：最多最小化重试一次，失败则返回诊断与恢复步骤。

## E. Token 与输出
- 大数据先过滤/聚合，只返回摘要 + 样本。
- 截断时必须明确说明“已截断”和规则,允许用户点击展开更多。
- 默认回复短、直接、结果导向。

## F. 单位默认
- 用户未指定单位时：默认 `mm`。
- 面积/体积默认 `m2`/`m3`，如涉及换算需显式说明。

## G. send_code_to_revit 强制流程
- 先通过工具层校验器，再决定是否执行。
- 代码上下文可用：`document`/`doc`/`Document`、`uiapp`/`uiApp`、`uidoc`/`uiDoc`、`activeView`、`parameters`。
- 只读查询用 `transaction_mode=none`；模型修改用 `transaction_mode=transaction` 或默认 `auto`。
- 校验失败返回结构化信息后，允许 LLM 重写并重提。
- 未通过校验前禁止执行动态代码。

## H. 版本兼容口径
- R19/R20/R21：优先兼容路径（老 API 降级）。
- R22+：可使用新 API 路径。
- 版本不支持时必须明确提示并给替代方案，不得静默失败。

## I. 参考与扩展
- 详细规则见完整版：`docs/MCP-LLM-Guidelines.zh-CN.md`
- 代码校验规则：`server/src/config/code-execution-rules.json`，构建后必须存在于 `server/build/config/`。
- R19 本地验证需确认 DLL 发布到 `revit_mcp_plugin/Commands/RevitMCPCommandSet/2019/`。

## J. send_code_to_revit 不可用排障
- 典型报错：`Code execution is only available through the 'revit-mcp-dev' MCP server profile.`
- 放行条件：`REVIT_MCP_MODE=developer`、`REVIT_MCP_ENABLE_CODE_EXECUTION=1`、`%USERPROFILE%\.revit-mcp\developer-mode.json` 存在且未过期。
- Claude 配置必须含 `mcpServers`；建议 server 名为 `revit-mcp-dev`，名称可自定义但 env 必须正确。
- 便捷模式可加 `REVIT_MCP_AUTO_ENABLE_DEVELOPER_MODE=1`；server 启动/执行代码前自动续期，默认/最大 120 分钟。
- Windows 空格路径建议用绝对 `node.exe`：`C:\Program Files\nodejs\node.exe` + `server/build/index.js`。
- `claude_desktop_config.json` 必须是合法 JSON 且 UTF-8 无 BOM；报 `Unexpected token '﻿'` 时重写为无 BOM。
- 若 Revit 端口旧/不监听，删除 `%APPDATA%\RevitMCP\revit-mcp-port.json` 后重启 Revit，让插件重写端口。
- 手动步骤：启用 `scripts/enable-developer-mode.ps1 -Minutes 120`，重启 Claude Desktop，再打开/重启 Revit 并测 `say_hello`。
- 完成后可执行 `scripts/disable-developer-mode.ps1` 关闭。
- 不存在“在 Revit 插件界面把 standard 改 dev/full”这一开关（以当前仓库实现为准）。
