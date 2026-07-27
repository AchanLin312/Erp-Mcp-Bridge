using System.ComponentModel;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;

namespace ErpMcpBridge.MCP.Tools;

[McpServerToolType]
public class BoMetadataTools
{
    private readonly BoMetadataService _boMetadata;

    public BoMetadataTools(BoMetadataService boMetadata)
    {
        _boMetadata = boMetadata;
    }

    [McpServerTool(Name = "ListEpicorServices")]
    [Description("列出当前Epicor环境所有可用的BO Service。")]
    public async Task<string> ListEpicorServices()
    {
        var services = _boMetadata.ListServices();
        return System.Text.Json.JsonSerializer.Serialize(services);
    }

    [McpServerTool(Name = "ListServiceMethods")]
    [Description("获取指定Epicor的BO Service中的所有方法列表。")]
    public async Task<string> ListServiceMethods(
        [Description("Service 名称，如 Erp.BO.PartSvc")] string serviceName)
    {
        var methods = await _boMetadata.ListMethodsAsync(serviceName);
        return System.Text.Json.JsonSerializer.Serialize(methods);
    }

    [McpServerTool(Name = "GetMethodSchema")]
    [Description("获取指定Epicor的BO方法的输入/输出参数格式。OutputTables 中列出所有返回的 Table 及其字段，Table 越多表示数据量越大（如 JobEntrySvc 有几十个子表），调用时注意可能存在性能影响。")]
    public async Task<string> GetMethodSchema(
        [Description("Service 名称，如 Erp.BO.PartSvc")] string serviceName,
        [Description("方法名称，如 GetByID")] string methodName)
    {
        var schema = await _boMetadata.GetMethodSchemaAsync(serviceName, methodName);
        return System.Text.Json.JsonSerializer.Serialize(schema);
    }

    [McpServerTool(Name = "GetEpicorEnvironment")]
    [Description("获取 Epicor 环境信息，包括可用公司列表及每个公司的工厂列表。")]
    public async Task<string> GetEpicorEnvironment()
    {
        return await _boMetadata.GetEnvironmentAsync();
    }
}
