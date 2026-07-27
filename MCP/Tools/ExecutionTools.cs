using System.ComponentModel;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;

namespace ErpMcpBridge.MCP.Tools;

[McpServerToolType]
public class ExecutionTools
{
    private readonly EpicorExecutionService _execution;

    public ExecutionTools(EpicorExecutionService execution)
    {
        _execution = execution;
    }

    [McpServerTool(Name = @"CallBoMethod")]
    [Description("调用指定 Epicor BO 方法。注意：某些 BO 方法（如 JobEntrySvc.GetByID）包含大量子表数据，响应可能较慢甚至超时，建议先用 GetMethodSchema 了解输出结构。GetList类的方法需传 whereClause、pageSize、absolutePage 三个参数。")]
    public async Task<string> CallBoMethod(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Service 名称，如 Erp.BO.PartSvc")] string serviceName,
        [Description("方法名称，如 GetByID、GetList")] string methodName,
        [Description("JSON 格式的请求参数。GetByID 示例：{\"partNum\":\"1422-8140+0\"}；GetList 示例：{\"whereClause\":\"PartNum like '1403%'\",\"pageSize\":10,\"absolutePage\":0}")] string jsonPayload)
    {
        return await _execution.CallBoMethodAsync(company, serviceName, methodName, jsonPayload);
    }

    [McpServerTool(Name = @"CallEfxFunction")]
    [Description("调用指定 Epicor EFX Function。注意：Function 所属的 Library 必须先 Publish 才能被调用。建议先使用 GetEfxFunctionSignature 了解输入参数格式。")]
    public async Task<string> CallEfxFunction(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Function Library ID，如 TestLibForAchen")] string libraryId,
        [Description("Function ID，如 Test1")] string functionId,
        [Description("JSON 格式的输入参数，参数名需与 GetEfxFunctionSignature 返回的 ArgumentName 一致。示例：{\"inputPartNum\":\"1403-7390+2\"}")] string jsonPayload)
    {
        return await _execution.CallEfxFunctionAsync(company, libraryId, functionId, jsonPayload);
    }
}
