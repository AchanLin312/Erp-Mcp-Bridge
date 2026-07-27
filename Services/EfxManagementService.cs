using Newtonsoft.Json.Linq;
using ErpMcpBridge.Models;

namespace ErpMcpBridge.Services;

/// <summary>
/// EFX Function 管理引擎
/// 基于 EpicorRestAPI 的 BoPost 调用 EfxLibraryDesignerSvc 的 BO 方法
/// TrackChanges 模式：GetLibrary → 修改 Dataset → ApplyChanges → PromoteToProduction
/// </summary>
public class EfxManagementService
{
    private readonly EpicorClientWrapper _epicor;

    private const string EfxDesignerSvc = "Ice.Lib.EfxLibraryDesignerSvc";

    public EfxManagementService(EpicorClientWrapper epicor)
    {
        _epicor = epicor;
    }

    public async Task<string> ListLibrariesAsync(string company)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetLibraryList", new { options = new { } });
        return response.ResponseBody;
    }

    public async Task<string> GetLibraryDetailAsync(string company, string libraryId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetLibrary", new { libraryId = libraryId });
        var json = JObject.Parse(response.ResponseBody);

        var efxFunction = json["returnObj"]?["EfxFunction"] as JArray;
        if (efxFunction != null)
        {
            foreach (var func in efxFunction.OfType<JObject>())
            {
                func.Remove("Body");
            }
        }

        return json.ToString(Newtonsoft.Json.Formatting.None);
    }

    public async Task<string> ListFunctionsAsync(string company, string libraryId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetFunctionList",
            new { options = new { libraryID = libraryId, kind = 0, functionIDStartsWith = "" } });
        return response.ResponseBody;
    }

    public async Task<string> GetFunctionDetailAsync(string company, string libraryId, string functionId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetKineticFunction",
            new { libraryId = libraryId, functionId = functionId });
        return response.ResponseBody;
    }

    public async Task<string> GetFunctionSignatureAsync(string company, string libraryId, string functionId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetFunctionInfo",
            new { options = new { libraryId = libraryId, functionId = functionId } });
        return response.ResponseBody;
    }

    public async Task<string> PushFunctionAsync(string company, string libraryId, string functionId, string csharpCode)
    {
        var (returnObj, getErr) = await GetLibraryDatasetAsync(company, libraryId);
        if (getErr != null) return getErr;

        var efxFunction = (JArray)returnObj["EfxFunction"]!;
        var bodyJson = BuildBodyJson(csharpCode);
        bool found = false;

        foreach (var func in efxFunction.OfType<JObject>())
        {
            if ((string)func["FunctionID"]! == functionId)
            {
                func["Body"] = bodyJson;
                func["RowMod"] = "U";
                found = true;
                break;
            }
        }

        if (!found)
            return $"Error: Function '{functionId}' not found in Library '{libraryId}'";

        return await ApplyAndSaveAsync(company, $"Pushed function '{functionId}'", returnObj);
    }

    public async Task<string> CreateFunctionAsync(string company, string libraryId, string functionId,
        string description, string csharpCode, string signaturesJson)
    {
        var (returnObj, getErr) = await GetLibraryDatasetAsync(company, libraryId);
        if (getErr != null) return getErr;

        // 检查是否已存在
        var efxFunction = (JArray)returnObj["EfxFunction"]!;
        foreach (var func in efxFunction.OfType<JObject>())
        {
            if ((string)func["FunctionID"]! == functionId)
                return $"Error: Function '{functionId}' already exists in Library '{libraryId}'";
        }

        // 添加新 Function 行
        efxFunction.Add(new JObject
        {
            ["LibraryID"] = libraryId,
            ["FunctionID"] = functionId,
            ["Description"] = description,
            ["Kind"] = 2,
            ["Body"] = BuildBodyJson(csharpCode),
            ["Disabled"] = false,
            ["Invalid"] = false,
            ["Private"] = false,
            ["RowMod"] = "A"
        });

        // 添加签名
        if (!string.IsNullOrWhiteSpace(signaturesJson))
        {
            var signatures = JArray.Parse(signaturesJson);
            var efxSignatures = (JArray)returnObj["EfxFunctionSignature"]!;
            foreach (var sig in signatures.OfType<JObject>())
            {
                sig["LibraryID"] = libraryId;
                sig["FunctionID"] = functionId;
                sig["RowMod"] = "A";
                efxSignatures.Add(sig);
            }
        }

        return await ApplyAndSaveAsync(company, $"Created function '{functionId}'", returnObj);
    }

    public async Task<string> DeleteFunctionAsync(string company, string libraryId, string functionId)
    {
        var (returnObj, getErr) = await GetLibraryDatasetAsync(company, libraryId);
        if (getErr != null) return getErr;

        // 标记 Function 为删除
        var efxFunction = (JArray)returnObj["EfxFunction"]!;
        bool found = false;
        foreach (var func in efxFunction.OfType<JObject>())
        {
            if ((string)func["FunctionID"]! == functionId)
            {
                func["RowMod"] = "D";
                found = true;
                break;
            }
        }

        if (!found)
            return $"Error: Function '{functionId}' not found in Library '{libraryId}'";

        // 标记关联签名为删除
        var efxSignatures = (JArray)returnObj["EfxFunctionSignature"]!;
        foreach (var sig in efxSignatures.OfType<JObject>())
        {
            if ((string)sig["FunctionID"]! == functionId)
                sig["RowMod"] = "D";
        }

        return await ApplyAndSaveAsync(company, $"Deleted function '{functionId}'", returnObj);
    }

    public async Task<string> PublishLibraryAsync(string company, string libraryId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "PromoteToProduction", new { libraryID = libraryId });

        if (response.IsErrorResponse)
            return $"Publish failed [{response.ResponseStatus}]: {response.ResponseError}\n{response.ResponseBody}";

        return $"Published library '{libraryId}' to production successfully.";
    }

    public async Task<string> AddEfxReferenceAsync(string company, string libraryId, string type, string referenceId)
    {
        var (returnObj, getErr) = await GetLibraryDatasetAsync(company, libraryId);
        if (getErr != null) return getErr;

        var tableName = GetReferenceTableName(type);
        if (tableName == null)
            return $"Error: Invalid reference type '{type}'. Must be one of: Assembly, Service, Table, Library";

        var idField = GetReferenceIdField(type);
        var table = (JArray)returnObj[tableName]!;

        // 检查是否已存在
        foreach (var row in table.OfType<JObject>())
        {
            if ((string)row[idField]! == referenceId)
                return $"Error: {type} reference '{referenceId}' already exists in Library '{libraryId}'";
        }

        var newRow = new JObject
        {
            ["LibraryID"] = libraryId,
            [idField] = referenceId,
            ["RowMod"] = "A"
        };

        if (type == "Table")
            newRow["Updatable"] = false;
        if (type == "Library")
            newRow["Mode"] = 0;

        table.Add(newRow);

        return await ApplyAndSaveAsync(company, $"Added {type} reference '{referenceId}'", returnObj);
    }

    public async Task<string> DemoteLibraryAsync(string company, string libraryId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "DemoteFromProduction", new { libraryID = libraryId });

        if (response.IsErrorResponse)
            return $"Demote failed [{response.ResponseStatus}]: {response.ResponseError}\n{response.ResponseBody}";

        return $"Demoted library '{libraryId}' from production successfully.";
    }

    // ─── Private Helpers ───

    private async Task<(JObject returnObj, string? error)> GetLibraryDatasetAsync(string company, string libraryId)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "GetLibrary", new { libraryId = libraryId });

        if (response.IsErrorResponse)
            return (null!, $"GetLibrary failed [{response.ResponseStatus}]: {response.ResponseError}");

        var json = JObject.Parse(response.ResponseBody);
        return ((JObject)json["returnObj"]!, null);
    }

    private async Task<string> ApplyAndSaveAsync(string company, string successPrefix, JObject returnObj)
    {
        var response = await _epicor.BoPostAsync(company, EfxDesignerSvc, "ApplyChanges", new { input = returnObj });

        if (response.IsErrorResponse)
            return $"Save failed [{response.ResponseStatus}]: {response.ResponseError}\n{response.ResponseBody}";

        return $"{successPrefix} in library '{returnObj["EfxLibrary"]?[0]?["LibraryID"]}' successfully.";
    }

    /// <summary>
    /// 从 C# 源码中分离 using 语句和代码体，构造成 Epicor Function 的 Body JSON 格式
    /// Body 格式: {"Code": "...", "Usings": "using ...;\r\n..."}
    /// </summary>
    private static string BuildBodyJson(string csharpCode)
    {
        var lines = csharpCode.Replace("\r\n", "\n").Split('\n');
        var usingLines = new List<string>();
        var codeLines = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("using ") && trimmed.EndsWith(";"))
                usingLines.Add(trimmed);
            else
                codeLines.Add(line);
        }

        // 去除代码体首尾空行
        while (codeLines.Count > 0 && string.IsNullOrWhiteSpace(codeLines[0]))
            codeLines.RemoveAt(0);
        while (codeLines.Count > 0 && string.IsNullOrWhiteSpace(codeLines[codeLines.Count - 1]))
            codeLines.RemoveAt(codeLines.Count - 1);

        return Newtonsoft.Json.JsonConvert.SerializeObject(new
        {
            Code = string.Join("\r\n", codeLines),
            Usings = string.Join("\r\n", usingLines)
        });
    }

    private static string? GetReferenceTableName(string type) => type switch
    {
        "Assembly" => "EfxRefAssembly",
        "Service" => "EfxRefService",
        "Table" => "EfxRefTable",
        "Library" => "EfxRefLibrary",
        _ => null
    };

    private static string GetReferenceIdField(string type) => type switch
    {
        "Assembly" => "Assembly",
        "Service" => "ServiceID",
        "Table" => "TableID",
        "Library" => "LibraryRef",
        _ => throw new ArgumentException($"Invalid reference type: {type}")
    };
}
