using Newtonsoft.Json.Linq;
using ErpMcpBridge.Models;
using ErpMcpBridge.Utils;

namespace ErpMcpBridge.Services;

/// <summary>
/// BO Service 元数据引擎
/// 负责：DLL 文件名扫描 → Swagger 获取 → $ref 递归解析 → 环境查询
/// 除了 GetEnvironmentAsync（使用 EpicorRestAPI），其余为纯元数据查询，不涉及 EpicorRestAPI
/// </summary>
public class BoMetadataService
{
    private readonly EpicorConfig _config;
    private readonly HttpClient _httpClient;
    private readonly SwaggerParser _swaggerParser;
    private readonly EpicorClientWrapper _epicor;

    public BoMetadataService(EpicorConfig config, HttpClient httpClient, SwaggerParser swaggerParser, EpicorClientWrapper epicor)
    {
        _config = config;
        _httpClient = httpClient;
        _swaggerParser = swaggerParser;
        _epicor = epicor;
    }

    /// <summary>
    /// 扫描 Assemblies 目录，从 DLL 文件名提取所有可用的 BO Service 名称
    /// </summary>
    public List<string> ListServices()
    {
        var dllFiles = Directory.GetFiles(_config.AssembliesPath, "*Contracts.BO.*.dll");
        var services = dllFiles.Select(file =>
            Path.GetFileNameWithoutExtension(file).Replace("Contracts.BO.", "BO.")
        ).ToList();

        return services;
    }

    /// <summary>
    /// 获取指定 Service 的 Swagger JSON，解析出所有 BO 方法名和描述
    /// </summary>
    public async Task<List<MethodInfo>> ListMethodsAsync(string serviceName)
    {
        var swaggerJson = await FetchSwaggerAsync(serviceName);
        var doc = JObject.Parse(swaggerJson);

        var methods = new List<MethodInfo>();
        var paths = (JObject)doc["paths"]!;

        foreach (var path in paths.Properties())
        {
            var pathObj = (JObject)path.Value;
            foreach (var httpMethod in pathObj.Properties())
            {
                var methodName = path.Name.Split('/').Last();
                var summary = (string?)httpMethod.Value["summary"] ?? string.Empty;

                if (!methods.Any(m => m.Name == methodName))
                {
                    methods.Add(new MethodInfo
                    {
                        Name = methodName,
                        Summary = summary,
                        HttpMethod = httpMethod.Name.ToUpper()
                    });
                }
            }
        }

        return methods;
    }

    /// <summary>
    /// 获取指定方法的输入/输出参数 Schema（精简版，已递归解析 $ref）
    /// 匹配策略：精确路径匹配 → operationId 模糊匹配 → 语义匹配（HTTP方法+实体名）
    /// </summary>
    public async Task<MethodSchema> GetMethodSchemaAsync(string serviceName, string methodName)
    {
        var swaggerJson = await FetchSwaggerAsync(serviceName);
        var doc = JObject.Parse(swaggerJson);

        var paths = (JObject)doc["paths"]!;
        JToken? targetMethod = null;
        string? httpMethod = null;

        // 1. 精确匹配：路径以 methodName 结尾 (如 /List)
        foreach (var path in paths.Properties())
        {
            var pathName = path.Name.Contains('(') ? path.Name[..path.Name.IndexOf('(')] : path.Name;
            if (pathName.EndsWith($"/{methodName}") || pathName == $"/{{currentCompany}}/{serviceName}/{methodName}")
            {
                if (TryGetHttpMethod(path, out targetMethod, out httpMethod))
                    break;
            }
        }

        // 2. 模糊匹配：通过 operationId 名称匹配
        if (targetMethod == null)
        {
            var normalizedSearch = methodName.Replace("_", "").ToLowerInvariant();
            foreach (var path in paths.Properties())
            {
                if (FindByOperationId(path, normalizedSearch, out targetMethod, out httpMethod))
                    break;
            }
        }

        // 3. 语义匹配：根据 BO 方法名推断 OData 操作
        if (targetMethod == null)
        {
            targetMethod = FindBySemanticMatch(paths, methodName, out httpMethod);
        }

        if (targetMethod == null)
            throw new InvalidOperationException($"Method '{methodName}' not found in Swagger for service '{serviceName}'.");

        return _swaggerParser.ExtractSchema(doc, targetMethod, httpMethod ?? "get");
    }

    private bool TryGetHttpMethod(JProperty path, out JToken? method, out string? httpMethod)
    {
        var pathObj = (JObject)path.Value;
        foreach (var http in pathObj.Properties())
        {
            if (http.Name is "get" or "post" or "patch" or "put" or "delete")
            {
                method = http.Value;
                httpMethod = http.Name;
                return true;
            }
        }
        method = null;
        httpMethod = null;
        return false;
    }

    private bool FindByOperationId(JProperty path, string normalizedSearch, out JToken? method, out string? httpMethod)
    {
        var pathObj = (JObject)path.Value;
        foreach (var http in pathObj.Properties())
        {
            if (http.Name is "get" or "post" or "patch" or "put" or "delete")
            {
                var operationId = (string?)((JObject)http.Value)["operationId"];
                if (operationId != null)
                {
                    var normalizedOpId = operationId.Replace("_", "").ToLowerInvariant();
                    if (normalizedOpId.Contains(normalizedSearch) || normalizedSearch.Contains(normalizedOpId))
                    {
                        method = http.Value;
                        httpMethod = http.Name;
                        return true;
                    }
                }
            }
        }
        method = null;
        httpMethod = null;
        return false;
    }

    /// <summary>
    /// 语义匹配：根据 BO 方法名推断对应的 OData 操作
    /// GetByID → GET on keyed path, GetList → GET on collection,
    /// GetNew* → POST, Update* → PATCH, Delete* → DELETE
    /// </summary>
    private JToken? FindBySemanticMatch(JObject paths, string methodName, out string? httpMethod)
    {
        var methodLower = methodName.ToLowerInvariant();

        // 确定目标 HTTP 方法
        string targetHttp;
        if (methodLower.StartsWith("getbyid") || methodLower.StartsWith("getbysysrow"))
            targetHttp = "get";       // GetByID → GET on single entity
        else if (methodLower.StartsWith("getlist") || methodLower == "list")
            targetHttp = "get";       // GetList → GET on collection
        else if (methodLower.StartsWith("getnew") || methodLower.StartsWith("getdefault"))
            targetHttp = "post";      // GetNew* → POST
        else if (methodLower.StartsWith("update") || methodLower.StartsWith("change"))
            targetHttp = "patch";     // Update* → PATCH
        else if (methodLower.StartsWith("delete") || methodLower.StartsWith("remove"))
            targetHttp = "delete";    // Delete* → DELETE
        else
        {
            httpMethod = null;
            return null;
        }

        // 在 paths 中找匹配的 HTTP 方法
        foreach (var path in paths.Properties())
        {
            var pathObj = (JObject)path.Value;
            foreach (var http in pathObj.Properties())
            {
                if (http.Name == targetHttp)
                {
                    // GetByID → 优先匹配有键参数的单实体路径 (路径包含 '(')
                    if (targetHttp == "get" && methodLower.StartsWith("getbyid"))
                    {
                        if (path.Name.Contains('(') && path.Name.Contains(')'))
                        {
                            httpMethod = targetHttp;
                            return http.Value;
                        }
                    }
                    // GetList → 优先匹配无键参数的集合路径或 List 端点
                    else if (targetHttp == "get" && (methodLower.StartsWith("getlist") || methodLower == "list"))
                    {
                        if (!path.Name.Contains('('))
                        {
                            httpMethod = targetHttp;
                            return http.Value;
                        }
                    }
                    // POST/PATCH/DELETE → 匹配第一个匹配操作
                    else
                    {
                        httpMethod = targetHttp;
                        return http.Value;
                    }
                }
            }
        }

        httpMethod = null;
        return null;
    }

    /// <summary>
    /// 获取 Epicor 环境信息（公司列表、每个公司下的工厂列表）
    /// 直接使用 EpicorRestAPI 的 GetEnvironmentAsync
    /// </summary>
    public async Task<string> GetEnvironmentAsync()
    {
        return await _epicor.GetEnvironmentAsync();
    }

    /// <summary>
    /// 从 Epicor Swagger 端点获取指定 Service 的 JSON
    /// </summary>
    private async Task<string> FetchSwaggerAsync(string serviceName)
    {
        var url = $"{_config.ServerUrl}/{_config.Environment}/api/swagger/v2/odata/{serviceName}.json";
        return await _httpClient.GetStringAsync(url);
    }
}

public class MethodInfo
{
    public string Name { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string HttpMethod { get; set; } = string.Empty;
}
