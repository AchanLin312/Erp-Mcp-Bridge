using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using ErpMcpBridge.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ErpMcpBridge.Services;

/// <summary>
/// gpeshell（GPEpicorShell）本地同步引擎
/// 调用 GPEpicorShell.exe download --path 将服务器端 EFX Library 最新代码同步到本地 gpeshell 项目。
/// 通过临时将 .gpproj.conf 的 ForcedReplace 置为 true，规避 Spectre.Console 在非交互终端上
/// UserDecision 选择弹窗导致的崩溃（NotSupportedException: Cannot show selection prompt），
/// 运行结束后恢复原值，不影响用户手动右键同步时的交互体验。
/// </summary>
public class GpeShellSyncService
{
    private readonly GPEpicorShellConfig _cfg;

    /// <summary>log.txt 行格式： "Add: Test1.cs" / "Replace: library.impl.cs" / "Delete: xxx.cs"</summary>
    private static readonly Regex LogLineRx =
        new(@"^(Add|Replace|Delete|User\s+Replace|Ignore)\s*:\s*(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>剥离 Spectre.Console 输出中的 ANSI 转义序列</summary>
    private static readonly Regex AnsiRx = new(@"\x1B\[[0-9;]*[A-Za-z]", RegexOptions.Compiled);

    /// <summary>gpeshell 任务历史目录名（17 位时间戳 yyyyMMddHHmmssfff）</summary>
    private static readonly Regex TaskFolderRx = new(@"^\d{17}$", RegexOptions.Compiled);

    private const int MaxTextLength = 6000;

    private const string ExeName = "GPEpicorShell.exe";

    /// <summary>自动发现探测的根目录（在其中搜索 GPEpicorShell* 目录）</summary>
    private static readonly string[] ProbeRoots =
    {
        @"C:\", @"D:\", @"E:\",
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    };

    /// <summary>已解析成功的 gpeshell 路径缓存（仅缓存成功结果，失败时下次重试）</summary>
    private string? _resolvedExePath;

    public GpeShellSyncService(GPEpicorShellConfig cfg)
    {
        _cfg = cfg;
    }

    public async Task<string> SyncLibraryLocallyAsync(string libraryId, string? localPath, bool forceReplace)
    {
        if (string.IsNullOrWhiteSpace(libraryId))
            return Error("libraryId 不能为空");

        // 1. 解析 gpeshell 本体路径（显式配置 → 环境变量 → PATH → 常见目录探测）
        var exePath = ResolveExePath();
        if (exePath == null)
            return Error("未找到 GPEpicorShell.exe。已依次尝试: ① appsettings.json 的 GPEpicorShell:ExePath ② 环境变量 GPEPICSHELL_EXE ③ PATH 查找 ④ 常见目录探测（C:\\、D:\\、E:\\ 及 Program Files、%LOCALAPPDATA% 下的 GPEpicorShell* 目录）。请先安装 gpeshell 或完成以上任一配置。");

        // 2. 解析本地项目目录（libraryId 可带 EFX. 前缀，目录约定为 efx.<id>）
        var bareId = libraryId.Trim();
        if (bareId.StartsWith("EFX.", StringComparison.OrdinalIgnoreCase))
            bareId = bareId.Substring(4).Trim();

        string projectDir;
        if (!string.IsNullOrWhiteSpace(localPath))
        {
            projectDir = Path.GetFullPath(localPath);
        }
        else
        {
            var root = _cfg.LocalProjectsRoot?.Trim();
            if (string.IsNullOrEmpty(root))
                return Error("无法定位本地项目目录：未提供 localPath 参数，且未配置 LocalProjectsRoot。请通过 localPath 直接传入 gpeshell 项目完整路径（若当前工作区就是 efx.<libraryId> 项目根目录，直接传工作区路径即可；项目根目录指含 .gpe 子目录的那层）。");
            projectDir = Path.Combine(root, $"efx.{bareId}");
            if (!Directory.Exists(projectDir))
            {
                var alt = Path.Combine(root, $"EFX.{bareId}");
                if (Directory.Exists(alt))
                    projectDir = alt;
            }
        }

        if (!Directory.Exists(projectDir))
            return Error($"本地项目目录不存在: {projectDir}。若已知项目实际路径（如当前工作区就是项目目录），请通过 localPath 参数直接指定；否则请先用 gpeshell 初始化该项目（在目标目录右键 → synchronize the project locally）。");

        // 3. 读取 .gpproj.conf 并校验 FullProjectID
        var confPath = Path.Combine(projectDir, ".gpe", ".gpproj.conf");
        if (!File.Exists(confPath))
            return Error($"未找到 gpeshell 项目配置: {confPath}，该目录可能不是 gpeshell 项目。");

        string confOriginal;
        JObject conf;
        try
        {
            confOriginal = await File.ReadAllTextAsync(confPath);
            conf = JObject.Parse(confOriginal);
        }
        catch (Exception ex)
        {
            return Error($"读取或解析 .gpproj.conf 失败: {ex.Message}");
        }

        var fullId = conf["FullProjectID"]?.ToString();
        var expected = $"EFX.{bareId}";
        if (string.IsNullOrEmpty(fullId) || !string.Equals(fullId, expected, StringComparison.OrdinalIgnoreCase))
            return Error($"FullProjectID 不匹配: .gpproj.conf 中为 '{fullId}'，与 libraryId '{expected}' 不一致。请确认 libraryId 或使用 localPath 指定正确项目。");

        // 4. 临时 patch ForcedReplace（finally 恢复原文）
        var patched = false;
        var originalForcedReplace = conf["ForcedReplace"]?.Value<bool>() ?? false;
        if (forceReplace && !originalForcedReplace)
        {
            conf["ForcedReplace"] = true;
            await File.WriteAllTextAsync(confPath, conf.ToString(Formatting.Indented), new UTF8Encoding(false));
            patched = true;
        }

        try
        {
            // 5. 快照 .gpe 现有任务目录，用于识别本次新产生的任务文件夹
            var gpeDir = Path.Combine(projectDir, ".gpe");
            var before = Directory.Exists(gpeDir)
                ? new HashSet<string>(Directory.GetDirectories(gpeDir), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 6. 执行 download
            var sw = Stopwatch.StartNew();
            var (exitCode, stdout, stderr, timedOut) = await RunDownloadAsync(exePath, projectDir);
            sw.Stop();

            if (timedOut)
                return Error($"gpeshell 执行超时（>{_cfg.TimeoutSeconds}s），已强制终止。OutputTail:\n{Tail(stdout + "\n" + stderr)}");

            // 7. 定位本次新任务文件夹及其 log.txt
            string? newTaskFolder = null;
            string? logPath = null;
            string? logText = null;
            if (Directory.Exists(gpeDir))
            {
                newTaskFolder = Directory.GetDirectories(gpeDir)
                    .Except(before, StringComparer.OrdinalIgnoreCase)
                    .Select(Path.GetFileName)
                    .Where(n => n != null && TaskFolderRx.IsMatch(n))
                    .OrderByDescending(n => n)
                    .FirstOrDefault();
                if (newTaskFolder != null)
                {
                    logPath = Path.Combine(gpeDir, newTaskFolder, "log.txt");
                    if (File.Exists(logPath))
                        logText = await File.ReadAllTextAsync(logPath);
                }
            }

            // 8. 解析 log.txt 分类统计
            var added = new List<string>();
            var replaced = new List<string>();
            var deleted = new List<string>();
            var ignored = new List<string>();
            if (!string.IsNullOrEmpty(logText))
            {
                foreach (var line in logText.Split('\n'))
                {
                    var m = LogLineRx.Match(line.Trim());
                    if (!m.Success) continue;
                    var op = Regex.Replace(m.Groups[1].Value, @"\s+", " ").ToLowerInvariant();
                    var file = m.Groups[2].Value.Trim();
                    switch (op)
                    {
                        case "add": added.Add(file); break;
                        case "replace":
                        case "user replace": replaced.Add(file); break;
                        case "delete": deleted.Add(file); break;
                        case "ignore": ignored.Add(file); break;
                    }
                }
            }

            // 9. 组装结果
            var leftoverTemp = new List<string>();
            if (Directory.Exists(gpeDir))
            {
                leftoverTemp = Directory.GetDirectories(gpeDir)
                    .Except(before, StringComparer.OrdinalIgnoreCase)
                    .Select(Path.GetFileName)
                    .Where(n => n != null && !TaskFolderRx.IsMatch(n!))
                    .Select(n => n!)
                    .ToList();
            }

            var result = new
            {
                success = exitCode == 0,
                exitCode,
                durationSeconds = Math.Round(sw.Elapsed.TotalSeconds, 1),
                projectDir,
                fullProjectId = fullId,
                gpeshellExe = exePath,
                added,
                replaced,
                deleted,
                ignoredCount = ignored.Count,
                logPath,
                log = Tail(logText),
                outputTail = Tail(stdout),
                stderrTail = string.IsNullOrWhiteSpace(stderr) ? null : Tail(stderr),
                warning = BuildWarning(exitCode, added, replaced, deleted, leftoverTemp)
            };
            return JsonConvert.SerializeObject(result, Formatting.Indented);
        }
        finally
        {
            if (patched)
            {
                try
                {
                    await File.WriteAllTextAsync(confPath, confOriginal, new UTF8Encoding(false));
                }
                catch
                {
                    // 恢复失败不影响主结果，用户可手动检查 .gpproj.conf
                }
            }
        }
    }

    private async Task<(int ExitCode, string Stdout, string Stderr, bool TimedOut)> RunDownloadAsync(string exePath, string projectDir)
    {
        var psi = new ProcessStartInfo
        {
            FileName = exePath,
            Arguments = $"download --path \"{projectDir}\"",
            WorkingDirectory = projectDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true, // gpeshell download 末尾会 Console.ReadLine() 等回车；关闭 stdin 使其立即读到 EOF 返回
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var proc = new Process { StartInfo = psi };
        if (!proc.Start())
            throw new InvalidOperationException("无法启动 GPEpicorShell.exe 进程");

        proc.StandardInput.Close();

        var stdoutTask = proc.StandardOutput.ReadToEndAsync();
        var stderrTask = proc.StandardError.ReadToEndAsync();

        var timedOut = false;
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Math.Max(30, _cfg.TimeoutSeconds)));
        try
        {
            await proc.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            timedOut = true;
            try { proc.Kill(entireProcessTree: true); } catch { /* 进程可能已自行退出 */ }
            try { await proc.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(5)); } catch { }
        }

        var stdout = AnsiRx.Replace(await stdoutTask, string.Empty);
        var stderr = AnsiRx.Replace(await stderrTask, string.Empty);
        var exitCode = proc.HasExited ? proc.ExitCode : -1;
        return (exitCode, stdout, stderr, timedOut);
    }

    /// <summary>
    /// 解析 GPEpicorShell.exe 路径（成功结果缓存）。
    /// 优先级: ① appsettings 的 ExePath ② 环境变量 GPEPICSHELL_EXE ③ PATH 查找 ④ 常见目录探测（根目录下的 GPEpicorShell* 文件夹）。
    /// </summary>
    private string? ResolveExePath()
    {
        if (_resolvedExePath != null)
            return _resolvedExePath;

        var configured = _cfg.ExePath?.Trim();
        if (!string.IsNullOrEmpty(configured) && File.Exists(configured))
            return _resolvedExePath = configured;

        var env = Environment.GetEnvironmentVariable("GPEPICSHELL_EXE");
        if (!string.IsNullOrWhiteSpace(env) && File.Exists(env.Trim()))
            return _resolvedExePath = env.Trim();

        var pathDirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var dir in pathDirs)
        {
            try
            {
                var candidate = Path.Combine(dir, ExeName);
                if (File.Exists(candidate))
                    return _resolvedExePath = candidate;
            }
            catch { /* 非法 PATH 项，跳过 */ }
        }

        foreach (var root in ProbeRoots)
        {
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) continue;
            try
            {
                foreach (var dir in Directory.EnumerateDirectories(root, "GPEpicorShell*", SearchOption.TopDirectoryOnly))
                {
                    var candidate = Path.Combine(dir, ExeName);
                    if (File.Exists(candidate))
                        return _resolvedExePath = candidate;
                }
            }
            catch { /* 无权限或目录异常，跳过 */ }
        }

        return null;
    }

    private static string? BuildWarning(int exitCode, List<string> added, List<string> replaced,
        List<string> deleted, List<string> leftoverTemp)
    {
        var tips = new List<string>();
        if (exitCode != 0)
        {
            tips.Add("gpeshell 非零退出，本次同步可能未完成。若输出含 'Cannot show selection prompt'，说明仍有交互决策未被 ForcedReplace 消除，请检查 .gpproj.conf。");
        }
        if (deleted.Count > 0)
            tips.Add($"本地独有文件被删除（服务器不存在）: {string.Join(", ", deleted)}。若是尚未推送到服务器的本地新建 Function，请立即重新推送。");
        if (replaced.Count > 0)
            tips.Add("本地与服务器有差异的文件已被服务器版本覆盖，如需找回本地改动请从 .gpe 时间戳备份目录恢复。");
        if (leftoverTemp.Count > 0)
            tips.Add($"检测到 gpeshell 遗留的临时目录（可安全手动删除）: {string.Join(", ", leftoverTemp)}。");
        return tips.Count > 0 ? string.Join(" ", tips) : null;
    }

    private static string? Tail(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        return text.Length <= MaxTextLength ? text : "..." + text[^MaxTextLength..];
    }

    private static string Error(string message) =>
        JsonConvert.SerializeObject(new { success = false, error = message }, Formatting.Indented);
}
