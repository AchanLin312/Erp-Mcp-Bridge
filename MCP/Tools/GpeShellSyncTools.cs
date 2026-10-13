using System.ComponentModel;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;

namespace ErpMcpBridge.MCP.Tools;

[McpServerToolType]
public class GpeShellSyncTools
{
    private readonly GpeShellSyncService _sync;

    public GpeShellSyncTools(GpeShellSyncService sync)
    {
        _sync = sync;
    }

    [McpServerTool(Name = "SyncEfxLibraryLocally")]
    [Description("调用 gpeshell(GPEpicorShell) 将 Epicor 服务器端 EFX Library 的最新代码同步到本地 gpeshell 项目（服务器 → 本地单向同步）。适用场景：①本 MCP 在服务器端新建/修改/删除了 Function 之后；②有人直接在 Epicor 代码编辑器里改了代码之后——把服务器最新状态拉回本地项目，使本地项目与服务器保持一致。注意：a) 本地与服务器内容有差异的自定义 .cs 文件会被服务器版本覆盖；b) 本地独有 .cs 文件（服务器不存在）会被删除——因此本地新建但尚未推送到服务器的 Function 会丢失，务必先推送到服务器再同步；c) 所有被覆盖/删除的文件均有 .gpe 时间戳备份目录可恢复；d) 项目目录定位：优先使用调用方显式传入的 localPath——若当前工作区/对话上下文中已知 efx.<libraryId> 项目根目录（含 .gpe 子目录的那层），务必直接把该路径作为 localPath 传入；未传 localPath 时才回退到服务端配置的 LocalProjectsRoot\\efx.<libraryId>（未配置则报错）；e) gpeshell 路径解析顺序：appsettings 的 GPEpicorShell:ExePath → 环境变量 GPEPICSHELL_EXE → PATH → 常见目录自动探测，任选其一配置即可。")]
    public async Task<string> SyncEfxLibraryLocally(
        [Description("Library ID，如 TestLibForAchen（可带 EFX. 前缀）")] string libraryId,
        [Description("本地 gpeshell 项目完整路径（含 .gpe 子目录的项目根目录）。推荐：当前工作区/上下文已知项目路径时务必显式传入；仅在服务端已配置 LocalProjectsRoot 且项目位于其下时才可省略")] string? localPath = null,
        [Description("是否强制以服务器版本替换本地有差异的文件（临时设置 ForcedReplace=true 以规避非交互终端崩溃），默认 true")] bool forceReplace = true)
    {
        return await _sync.SyncLibraryLocallyAsync(libraryId, localPath, forceReplace);
    }
}
