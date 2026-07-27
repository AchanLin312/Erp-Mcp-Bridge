namespace ErpMcpBridge.Models;

/// <summary>
/// Epicor 连接配置（从 appsettings.json 读取）
/// </summary>
public class EpicorConfig
{
    public string ServerUrl { get; set; } = string.Empty;
    public string Environment { get; set; } = string.Empty;
    // Company 不在此处配置，由调用方通过 Tool 参数传入
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string AssembliesPath { get; set; } = string.Empty;
    public CallSettingsConfig CallSettings { get; set; } = new(); //Epicor Session中的上下文的Company
}

public class CallSettingsConfig
{
    public string Company { get; set; } = string.Empty;
    public string? Plant { get; set; }
    public string? Language { get; set; }
    public string? FormatCulture { get; set; }
    public string? TimezoneOffset { get; set; }
    /// <summary>BO/EFX 调用超时（秒），默认 300</summary>
    public int TimeoutSeconds { get; set; } = 300;
}

public class MCPServerConfig
{
    public string Transport { get; set; } = "stdio";
    public int HttpPort { get; set; } = 5000;
}
