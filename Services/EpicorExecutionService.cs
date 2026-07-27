using Newtonsoft.Json.Linq;

namespace ErpMcpBridge.Services;

/// <summary>
/// Epicor API 执行引擎，内部使用 EpicorRestAPI 的 BoPost / EfxPost
/// 注意：EpicorRestAPI 内部使用 Newtonsoft.Json，所以传参必须用 JObject
/// </summary>
public class EpicorExecutionService
{
    private readonly EpicorClientWrapper _epicor;

    public EpicorExecutionService(EpicorClientWrapper epicor)
    {
        _epicor = epicor;
    }

    public async Task<string> CallBoMethodAsync(string company, string serviceName, string methodName, string jsonPayload)
    {
        var data = string.IsNullOrWhiteSpace(jsonPayload)
            ? new { }
            : Newtonsoft.Json.JsonConvert.DeserializeObject(jsonPayload);
        var response = await _epicor.BoPostAsync(company, serviceName, methodName, data!);

        if (response.IsErrorResponse)
            return $"Epicor Error [{response.ResponseStatus}]: {response.ResponseError}\nBody: {response.ResponseBody}";

        return response.ResponseBody;
    }

    public async Task<string> CallEfxFunctionAsync(string company, string libraryId, string functionId, string jsonPayload)
    {
        var data = string.IsNullOrWhiteSpace(jsonPayload)
            ? new { }
            : Newtonsoft.Json.JsonConvert.DeserializeObject(jsonPayload);
        var response = await _epicor.EfxPostAsync(company, libraryId, functionId, data!);

        if (response.IsErrorResponse)
            return $"Epicor Error [{response.ResponseStatus}]: {response.ResponseError}\nBody: {response.ResponseBody}";

        return response.ResponseBody;
    }
}
