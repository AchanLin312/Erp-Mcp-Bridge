using EpicorRestAPI;
using ErpMcpBridge.MCP.Tools;
using ErpMcpBridge.Models;
using ErpMcpBridge.Services;
using ErpMcpBridge.Utils;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ErpMcpBridge;

class Program
{
    static async Task Main(string[] args)
    {
        var config = LoadConfiguration();
        var mcpConfig = LoadMcpConfig();

        // 命令行参数覆盖配置文件
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--transport" && i + 1 < args.Length)
                mcpConfig.Transport = args[++i];
            else if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[++i], out var port))
                mcpConfig.HttpPort = port;
        }

        if (mcpConfig.Transport == "http")
        {
            await RunHttpServer(config, mcpConfig);
        }
        else
        {
            await RunStdioServer(config);
        }
    }

    private static async Task RunHttpServer(EpicorConfig config, MCPServerConfig mcpConfig)
    {
        Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{mcpConfig.HttpPort}");
        var builder = WebApplication.CreateBuilder();
        ConfigureSharedServices(builder.Services, config);
        builder.Services.AddMcpServer()
            .WithHttpTransport(options =>
            {
                options.Stateless = false;
                options.EnableLegacySse = true;
            })
            .WithTools<BoMetadataTools>()
            .WithTools<ExecutionTools>()
            .WithTools<EfxManagementTools>()
            .WithTools<LayerManagementTools>()
            .WithTools<MetaFxSchemaTools>()
            .WithTools<GpeShellSyncTools>();

        var app = builder.Build();
        app.MapMcp();
        await app.RunAsync();
    }

    private static async Task RunStdioServer(EpicorConfig config)
    {
        var builder = Host.CreateApplicationBuilder();
        ConfigureSharedServices(builder.Services, config);
        builder.Services.AddMcpServer()
            .WithStdioServerTransport()
            .WithTools<BoMetadataTools>()
            .WithTools<ExecutionTools>()
            .WithTools<EfxManagementTools>()
            .WithTools<LayerManagementTools>()
            .WithTools<MetaFxSchemaTools>()
            .WithTools<GpeShellSyncTools>();

        var app = builder.Build();
        await app.RunAsync();
    }

    private static void ConfigureSharedServices(IServiceCollection services, EpicorConfig config)
    {
        services.AddSingleton(config);

        // EpicorRestAPI 实例（独家 HTTP 调用入口）
        // Auth HttpClient（EpicorRestAPI HTTP 回退用）
        services.AddHttpClient("Auth", client =>
        {
            var authToken = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{config.UserName}:{config.Password}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
            client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        });
        services.AddSingleton(sp =>
        {
            var cfg = sp.GetRequiredService<EpicorConfig>();
            var epicor = new EpicorRest
            {
                AppPoolHost = new Uri(cfg.ServerUrl).Host,
                AppPoolInstance = cfg.Environment,
                UserName = cfg.UserName,
                Password = cfg.Password,
                APIKey = cfg.ApiKey,
                APIVersion = EpicorRestVersion.V2,
                License = EpicorLicenseType.Default
            };
            var client = sp.GetRequiredService<IHttpClientFactory>().CreateClient("Auth");
            return new EpicorClientWrapper(epicor, client, $"{cfg.ServerUrl}/{cfg.Environment}", cfg);
        });

        // Swagger 解析器（metadata 类工具需要 HTTP GET swagger.json，需认证）
        services.AddSingleton<SwaggerParser>();
        services.AddHttpClient("Swagger", client =>
        {
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
            var authToken = Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes($"{config.UserName}:{config.Password}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authToken);
        });

        // 服务层
        services.AddSingleton<BoMetadataService>(sp =>
        {
            var cfg = sp.GetRequiredService<EpicorConfig>();
            var clientFactory = sp.GetRequiredService<IHttpClientFactory>();
            return new BoMetadataService(
                cfg,
                clientFactory.CreateClient("Swagger"),
                sp.GetRequiredService<SwaggerParser>(),
                sp.GetRequiredService<EpicorClientWrapper>());
        });
        services.AddSingleton<EpicorExecutionService>();
        services.AddSingleton<EfxManagementService>();
        services.AddSingleton<MetaFxSchemaService>();
        services.AddSingleton<LayerManagementService>();

        // gpeshell 本地同步（GPEpicorShell）
        services.AddSingleton(sp =>
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            return builder.Build().GetSection("GPEpicorShell").Get<GPEpicorShellConfig>() ?? new GPEpicorShellConfig();
        });
        services.AddSingleton<GpeShellSyncService>();

        // Tool 类
        services.AddSingleton<BoMetadataTools>();
        services.AddSingleton<ExecutionTools>();
        services.AddSingleton<EfxManagementTools>();
        services.AddSingleton<LayerManagementTools>();
        services.AddSingleton<MetaFxSchemaTools>();
        services.AddSingleton<GpeShellSyncTools>();
    }

    private static EpicorConfig LoadConfiguration()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        return builder.Build().GetSection("Epicor").Get<EpicorConfig>()!;
    }

    private static MCPServerConfig LoadMcpConfig()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        return builder.Build().GetSection("MCPServer").Get<MCPServerConfig>()!;
    }
}
