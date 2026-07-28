using System.ComponentModel;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;

namespace ErpMcpBridge.MCP.Tools;

[McpServerToolType]
public class EfxManagementTools
{
    private readonly EfxManagementService _efxMgr;

    public EfxManagementTools(EfxManagementService efxMgr)
    {
        _efxMgr = efxMgr;
    }

    [McpServerTool(Name = "ListEfxLibraries")]
    [Description("获取当前公司中有哪些 Library。")]
    public async Task<string> ListEfxLibraries(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company)
    {
        return await _efxMgr.ListLibrariesAsync(company);
    }

    [McpServerTool(Name = "ListEfxReferenceCandidates")]
    [Description("列出可用于 AddEfxReference 的候选引用。type 取值为 Assembly（列出可用 DLL）、Table（列出可用数据库表）、Service（列出可用 BO Service）、Library（列出可用 EFX 库）。")]
    public async Task<string> ListEfxReferenceCandidates(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("引用类型：Assembly、Table、Service、Library。Assembly 返回 DLL 文件名，Table 返回 Erp.XXX 格式表名，Service 返回 Erp.BO.XXXSvc 格式，Library 返回库 ID 列表")] string type)
    {
        return await _efxMgr.ListReferenceCandidatesAsync(company, type);
    }

    [McpServerTool(Name = "GetLibraryDetail")]
    [Description("获取指定Epicor的EFX库(Library)中的除Function源码以外的详细信息，要获取Function源码请使用 GetEfxFunctionDetail。")]
    public async Task<string> GetLibraryDetail(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId)
    {
        return await _efxMgr.GetLibraryDetailAsync(company, libraryId);
    }

    [McpServerTool(Name = "ListEfxFunctions")]
    [Description("获取指定Epicor的EFX库(Library)中的所有Function。")]
    public async Task<string> ListEfxFunctions(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId)
    {
        return await _efxMgr.ListFunctionsAsync(company, libraryId);
    }

    [McpServerTool(Name = "GetEfxFunctionDetail")]
    [Description("获取指定Epicor的EFX库(Library)中的Function 详细信息(Function源码，Using语句等)。")]
    public async Task<string> GetEfxFunctionDetail(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId)
    {
        return await _efxMgr.GetFunctionDetailAsync(company, libraryId, functionId);
    }

    [McpServerTool(Name = "GetEfxFunctionSignature")]
    [Description("获取指定 Epicor EFX 库(Library)中的 Function 参数签名。返回的 SignatureData 中 Response=false 为输入参数，Response=true 为输出参数。此结果可用作 CreateEfxFunction 中 signaturesJson 的格式参考。")]
    public async Task<string> GetEfxFunctionSignature(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId)
    {
        return await _efxMgr.GetFunctionSignatureAsync(company, libraryId, functionId);
    }

    [McpServerTool(Name = "PushEfxFunction")]
    [Description("将指定 Epicor EFX 库(Library)中的 Function 源代码推送到 Epicor。注意：Library 必须先 Demote。此操作只更新已存在 Function 的代码体，不影响参数签名。")]
    public async Task<string> PushEfxFunction(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId,
        [Description("C# 源代码，无需写 using 语句，会自动分离")] string csharpCode)
    {
        return await _efxMgr.PushFunctionAsync(company, libraryId, functionId, csharpCode);
    }

    [McpServerTool(Name = "UpdateEfxFunctionSignature")]
    [Description("修改指定 Epicor EFX 库(Library)中已存在 Function 的参数签名（输入/输出参数）。注意：Library 必须先 Demote。signaturesJson 格式与 CreateEfxFunction 相同，会完全替换旧签名。")]
    public async Task<string> UpdateEfxFunctionSignature(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId,
        [Description("参数签名 JSON 数组，格式如：[{\"Response\":false,\"ParameterID\":1,\"ArgumentName\":\"inputName\",\"Order\":1,\"DataType\":\"System.String\",\"DataTypeInfo\":null},{\"Response\":true,\"ParameterID\":2,\"ArgumentName\":\"outputResult\",\"Order\":2,\"DataType\":\"System.String\",\"DataTypeInfo\":null}]。Response=false 为输入参数，Response=true 为输出参数")] string signaturesJson)
    {
        return await _efxMgr.UpdateFunctionSignatureAsync(company, libraryId, functionId, signaturesJson);
    }

    [McpServerTool(Name = "CreateEfxFunction")]
    [Description("在指定的 Epicor EFX 库(Library)中创建 Function。注意：Library 必须先 Demote。signaturesJson 为 JSON 数组，格式参考 GetEfxFunctionSignature 的返回结果。csharpCode 无需包含 using 语句，会自动分离。")]
    public async Task<string> CreateEfxFunction(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId,
        [Description("Function 描述")] string description,
        [Description("C# 源代码，无需写 using 语句，会自动分离。示例：\nvar result = $\"Hello: {this.inputName}\";\nthis.outputResult = result;")] string csharpCode,
        [Description("参数签名 JSON 数组，格式如：[{\"Response\":false,\"ParameterID\":1,\"ArgumentName\":\"inputName\",\"Order\":1,\"DataType\":\"System.String\",\"DataTypeInfo\":null}]。注意是数组不是对象，Response=false 为输入参数，Response=true 为输出参数")] string signaturesJson)
    {
        return await _efxMgr.CreateFunctionAsync(company, libraryId, functionId, description, csharpCode, signaturesJson);
    }

    [McpServerTool(Name = "DeleteEfxFunction")]
    [Description("删除指定 Epicor EFX 库(Library)中的 Function。注意：Library 必须先 Demote。")]
    public async Task<string> DeleteEfxFunction(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("Function ID")] string functionId)
    {
        return await _efxMgr.DeleteFunctionAsync(company, libraryId, functionId);
    }

    [McpServerTool(Name = "AddEfxReference")]
    [Description("向指定的 Epicor EFX Library 添加引用。引用类型：Assembly(程序集DLL)、Service(BO Service)、Table(数据库表)、Library(其他EFX库)。注意：Library 必须先 Demote 才能添加引用。")]
    public async Task<string> AddEfxReference(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId,
        [Description("引用类型：Assembly、Service、Table、Library")] string type,
        [Description("引用标识：Assembly为DLL文件名(如 Erp.Contracts.BO.Part.dll)，Service为(如 ERP:BO:Part)，Table为(如 ERP.Part)，Library为(如 WorkflowGP)")] string referenceId)
    {
        return await _efxMgr.AddEfxReferenceAsync(company, libraryId, type, referenceId);
    }

    [McpServerTool(Name = "DemoteEfxLibrary")]
    [Description("将指定的EFX库从生产环境降级(Demote)到开发环境。注意：修改已发布的 Library (Published=true) 前必须先 Demote，否则 ApplyChanges 会失败。")]
    public async Task<string> DemoteEfxLibrary(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId)
    {
        return await _efxMgr.DemoteLibraryAsync(company, libraryId);
    }

    [McpServerTool(Name = "PublishEfxLibrary")]
    [Description("发布指定 Epicor EFX 库(Library)。发布后所有修改（新增/修改/删除的 Function）才会在 Epicor 中生效，CallEfxFunction 也只能调用已发布的 Function。")]
    public async Task<string> PublishEfxLibrary(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("Library ID")] string libraryId)
    {
        return await _efxMgr.PublishLibraryAsync(company, libraryId);
    }
}
