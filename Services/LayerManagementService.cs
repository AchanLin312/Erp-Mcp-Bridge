using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ErpMcpBridge.Models;

namespace ErpMcpBridge.Services;

/// <summary>
/// Kinetic Layer 管理引擎
/// 基于 Ice.Lib.MetaFXSvc（Kinetic App Studio Layer 设计器后端）实现层的增删改查与发布
/// 关键约束（反编译 + REST 实测确认）：
/// - 复杂参数必须以 {"request":{...}} 包装
/// - EpMetaFxLayerRequest 必须显式传 Company:""，否则 GetLayer 返回空 {}
/// - SaveApp 的 applicationType 必须为 "view" 或 "component"（服务端 ToLowerInvariant 比较）
/// - ICESDK 许可证仅拦截"基线 App 保存"（layerInfo==null）和"带 CGCCode 的层保存"，普通层操作不受限
/// - SaveApp 服务端自动计算传入布局与基线的 PropDiff 差异（merged 模式），AI 无需自己算 diff
/// </summary>
public class LayerManagementService
{
    private readonly EpicorClientWrapper _epicor;
    private readonly EpicorConfig _config;
    private readonly MetaFxSchemaService _schema;

    private const string MetaFxSvc = "Ice.Lib.MetaFXSvc";
    public const string CustomerLayerType = "KNTCCustLayer";
    public const string PersLayerType = "KNTCPersLayer";

    public LayerManagementService(EpicorClientWrapper epicor, EpicorConfig config, MetaFxSchemaService schema)
    {
        _epicor = epicor;
        _config = config;
        _schema = schema;
    }

    #region 查询

    /// <summary>
    /// 列出 Kinetic 视图（App）。GetViews 无参数，本地按 searchText 过滤。
    /// 无过滤条件且结果超过 MaxViewCount 时截断并提示使用 searchText。
    /// </summary>
    public async Task<string> ListViewsAsync(string company, string searchText)
    {
        var response = await _epicor.BoPostAsync(company, MetaFxSvc, "GetViews", new { });
        var body = response.ResponseBody;

        if (string.IsNullOrWhiteSpace(searchText))
        {
            // 无过滤时若为超大 JSON 数组则截断防刷屏
            try
            {
                var arr = JArray.Parse(body);
                if (arr.Count <= MaxViewCount) return body;
                var capped = new JArray(arr.Take(MaxViewCount));
                return capped.ToString(Formatting.Indented)
                    + $"\n\n注意：共 {arr.Count} 个视图，仅显示前 {MaxViewCount} 个。请用 searchText 过滤（如 ListKineticViews(searchText=\"Erp.UI.Part\")）。";
            }
            catch (JsonReaderException)
            {
                return body;
            }
        }

        try
        {
            var views = JArray.Parse(body);
            var filtered = new JArray(views.Where(v => ViewMatches(v, searchText)));
            return filtered.ToString(Formatting.Indented);
        }
        catch (JsonReaderException)
        {
            return body; // 非预期格式则原样返回
        }
    }

    /// <summary>
    /// 列出指定视图上的层。GetLayers(EpMetaFxGetLayersRequest)。
    /// typeCode 为空时分别查询 KNTCCustLayer 与 KNTCPersLayer 并合并返回。
    /// </summary>
    public async Task<string> ListLayersAsync(string company, string viewId, string typeCode, bool includeUnpublishedLayers)
    {
        var typeError = ValidateTypeCode(typeCode, allowEmpty: true);
        if (typeError != null) return typeError;

        var types = string.IsNullOrWhiteSpace(typeCode)
            ? new[] { CustomerLayerType, PersLayerType }
            : new[] { typeCode };

        var result = new JObject();
        foreach (var t in types)
        {
            var payload = new JObject
            {
                ["request"] = new JObject
                {
                    ["ViewId"] = viewId,
                    ["TypeCode"] = t,
                    ["DeviceType"] = "Desktop",
                    ["IncludeUnpublishedLayers"] = includeUnpublishedLayers
                }
            };
            var response = await _epicor.BoPostAsync(company, MetaFxSvc, "GetLayers", payload);
            try { result[t] = JToken.Parse(response.ResponseBody); }
            catch (JsonReaderException) { result[t] = response.ResponseBody; }
        }

        return result.ToString(Formatting.Indented);
    }

    /// <summary>
    /// 读取层完整内容。GetLayer(EpMetaFxLayerRequest)。
    /// 返回 EpMetaFxLayer 结构（Content 为 Dictionary&lt;string,JToken&gt;，即层内容键值对）。
    /// includeDraftContent=true 时返回草稿内容（WIP）。
    /// </summary>
    public async Task<string> GetLayerDetailAsync(string company, string viewId, string layerName, string typeCode, string deviceType, bool includeDraftContent)
    {
        var error = ValidateLayerArgs(typeCode, layerName);
        if (error != null) return error;

        var response = await _epicor.BoPostAsync(company, MetaFxSvc, "GetLayer",
            BuildLayerRequest(viewId, layerName, typeCode, deviceType, includeDraftContent: includeDraftContent));
        return response.ResponseBody;
    }

    #endregion

    #region 写入

    /// <summary>
    /// 新建层。先 LayerExists 防重，再 SaveApp(layerInfo.IsNew=true, wip=true 默认草稿)。
    /// contentJson 可选键：layout(object)、classicLayout(object)、pages/events/rules/tools/dataviews/orphans/properties(array)、
    /// serializedEvents(object)、subApplicationType、viewType(string)、uxAppVersion(number)、configuratorInfo。未知键忽略并提示。
    /// </summary>
    public async Task<string> CreateLayerAsync(string company, string viewId, string layerName, string typeCode,
        string description, string? parentLayersJson, string? contentJson, bool wip, bool allowUnknownComponents = false)
    {
        var error = ValidateLayerArgs(typeCode, layerName, description);
        if (error != null) return error;

        // 防重检查
        var existsResponse = await _epicor.BoPostAsync(company, MetaFxSvc, "LayerExists",
            BuildLayerRequest(viewId, layerName, typeCode, "Desktop"));
        if (ParseBoolResponse(existsResponse.ResponseBody))
            return $"Error: 层 '{layerName}'（{typeCode}）在视图 '{viewId}' 上已存在。如需修改请用 UpdateKineticLayer。";

        var parentLayers = ParseStringArray(parentLayersJson);
        if (parentLayers == null)
            return "Error: parentLayersJson 必须是字符串数组 JSON，如 [\"SomeLayer\"] 或 []";

        var (content, ignoredKeys) = ParseContent(contentJson);
        var componentError = ValidateComponentTypes(content, allowUnknownComponents);
        if (componentError != null) return componentError;

        var (payload, _) = BuildSaveRequest(viewId, layerName, typeCode, "Desktop",
            description, parentLayers, wip, isNew: true, content, comment: "Created via Erp-Mcp-Bridge");

        var response = await _epicor.BoPostAsync(company, MetaFxSvc, "SaveApp", payload);
        return FormatSaveResult(response, ignoredKeys, "Created", viewId, layerName, typeCode, wip);
    }

    /// <summary>
    /// 更新层内容（merged 模式）。先 LayerExists 确认存在，再 SaveApp(layerInfo.IsNew=false, wip=true 固定草稿)。
    /// contentJson 为完整 merged 内容（典型流程：GetKineticLayer 取 Content → 修改 → 本工具提交）。
    /// description 语义：null → 保留现有描述（GetLayer 回填，避免 SaveApp 以空串覆盖）；非 null → 设置为新描述。
    /// 发布请用 PublishKineticLayer，本工具固定保存为草稿。
    /// </summary>
    public async Task<string> UpdateLayerAsync(string company, string viewId, string layerName, string typeCode,
        string contentJson, string comment, bool allowUnknownComponents = false, string? description = null)
    {
        var error = ValidateLayerArgs(typeCode, layerName);
        if (error != null) return error;

        var existsResponse = await _epicor.BoPostAsync(company, MetaFxSvc, "LayerExists",
            BuildLayerRequest(viewId, layerName, typeCode, "Desktop"));
        if (!ParseBoolResponse(existsResponse.ResponseBody))
            return $"Error: 层 '{layerName}'（{typeCode}）在视图 '{viewId}' 上不存在。如需新建请用 CreateKineticLayer。";

        var (content, ignoredKeys) = ParseContent(contentJson);
        if (content == null || !content.HasValues)
            return "Error: contentJson 不能为空，需提供要保存的层内容对象（如 {\"layout\":{...}}）。";

        var componentError = ValidateComponentTypes(content, allowUnknownComponents);
        if (componentError != null) return componentError;

        if (description == null)
        {
            var detailResponse = await _epicor.BoPostAsync(company, MetaFxSvc, "GetLayer",
                BuildLayerRequest(viewId, layerName, typeCode, "Desktop", includeDraftContent: true));
            try { description = (string?)JObject.Parse(detailResponse.ResponseBody)["LayerDescription"]; }
            catch (JsonReaderException) { /* 读取失败保持 null，与旧行为一致 */ }
        }

        var (payload, _) = BuildSaveRequest(viewId, layerName, typeCode, "Desktop",
            description, parentLayers: null, wip: true, isNew: false, content, comment);

        var response = await _epicor.BoPostAsync(company, MetaFxSvc, "SaveApp", payload);
        return FormatSaveResult(response, ignoredKeys, "Updated", viewId, layerName, typeCode, wip: true);
    }

    /// <summary>
    /// 删除层。DeleteLayer(EpMetaFxLayerRequest)。
    /// 注意：不要用 DeletePersonalizationLayers 删 Company='' 的层——其内部
    /// DeletePersonalizationLayersFromDb 按 Session.CompanyID 过滤，Company='' 的行会静默匹配失败
    /// （返回成功但 DB 行仍在）。DeleteLayer 无此限制。
    /// </summary>
    public async Task<string> DeleteLayerAsync(string company, string viewId, string layerName, string typeCode, string deviceType)
    {
        var error = ValidateLayerArgs(typeCode, layerName);
        if (error != null) return error;

        var response = await _epicor.BoPostAsync(company, MetaFxSvc, "DeleteLayer",
            BuildLayerRequest(viewId, layerName, typeCode, deviceType));

        var body = response.ResponseBody;
        return string.IsNullOrWhiteSpace(body) || body.Trim() == "{}" || body.Trim() == "null"
            ? $"Deleted layer '{layerName}'（{typeCode}）from view '{viewId}'."
            : $"Deleted layer '{layerName}'（{typeCode}）from view '{viewId}'.\nServer response: {body}";
    }

    /// <summary>
    /// 发布层。
    /// KNTCCustLayer → BulkPublishLayers（App Studio 原生发布路径；LayerService 会执行 ViewId=Id 赋值）。
    /// KNTCPersLayer → BulkPublishLayers 路由会忽略个性化层，改走 GetLayer(草稿) → SaveApp(wip=false) 发布链路。
    /// </summary>
    public async Task<string> PublishLayerAsync(string company, string viewId, string layerName, string typeCode, string deviceType)
    {
        var error = ValidateLayerArgs(typeCode, layerName);
        if (error != null) return error;

        deviceType ??= "Desktop";

        if (typeCode == CustomerLayerType)
        {
            var payload = new JObject
            {
                ["layersToPublish"] = new JArray
                {
                    new JObject
                    {
                        ["Id"] = viewId,              // LayerService.BulkPublishLayers 内部执行 l.ViewId = l.Id
                        ["ViewId"] = viewId,
                        ["LayerName"] = layerName,
                        ["TypeCode"] = typeCode,
                        ["DeviceType"] = deviceType,
                        ["Company"] = "",
                        ["CGCCode"] = ""                // 必须显式传空串，null 会导致服务端按 CGCCode=null 过滤查不到行报 "Customization layer not found."
                    }
                },
                ["validateBeforePublish"] = true
            };
            var response = await _epicor.BoPostAsync(company, MetaFxSvc, "BulkPublishLayers", payload);
            return $"Published layer '{layerName}'（{typeCode}）on view '{viewId}'.\nServer response: {response.ResponseBody}";
        }

        // KNTCPersLayer：取草稿内容后以 wip=false 保存触发发布（SaveApp 中 wip=false → UxAppVersionSvc.PublishVersion）
        var layerResponse = await _epicor.BoPostAsync(company, MetaFxSvc, "GetLayer",
            BuildLayerRequest(viewId, layerName, typeCode, deviceType, includeDraftContent: true));
        JObject layerJson;
        try { layerJson = JObject.Parse(layerResponse.ResponseBody); }
        catch (JsonReaderException)
        {
            return $"Error: 读取层草稿内容失败，无法发布。\nServer response: {layerResponse.ResponseBody}";
        }

        var content = layerJson["Content"] as JObject;
        var (mappedContent, ignoredKeys) = ParseContent(content?.ToString(Formatting.None));
        var (savePayload, _) = BuildSaveRequest(viewId, layerName, typeCode, deviceType,
            description: (string?)layerJson["LayerDescription"],
            parentLayers: layerJson["ParentLayers"]?.ToObject<string[]>(),
            wip: false, isNew: false, mappedContent,
            comment: "Published via Erp-Mcp-Bridge");

        var saveResponse = await _epicor.BoPostAsync(company, MetaFxSvc, "SaveApp", savePayload);
        return FormatSaveResult(saveResponse, ignoredKeys, "Published", viewId, layerName, typeCode, wip: false);
    }

    #endregion

    #region 私有 helper

    private const int MaxViewCount = 200;

    /// <summary>构造 {"request":{...}} 包装的 EpMetaFxLayerRequest。Company 必须显式传 ""（否则 GetLayer 返回空 {}）。</summary>
    private JObject BuildLayerRequest(string viewId, string layerName, string typeCode, string deviceType,
        string[]? parentLayers = null, bool includeDraftContent = false)
    {
        var req = new JObject
        {
            ["ViewId"] = viewId,
            ["Company"] = "",
            ["TypeCode"] = typeCode,
            ["DeviceType"] = deviceType ?? "Desktop",
            ["UserName"] = _config.UserName,
            ["CGCCode"] = ""
        };
        if (!string.IsNullOrEmpty(layerName)) req["LayerName"] = layerName;
        if (parentLayers != null) req["ParentLayers"] = JArray.FromObject(parentLayers);
        if (includeDraftContent) req["IncludeDraftContent"] = true;
        return new JObject { ["request"] = req };
    }

    /// <summary>构造 {"request":{...}} 包装的 EpMetaFxSaveRequestEx（字段名为反编译确认的 camelCase 属性名）。</summary>
    private (JObject payload, List<string> ignoredKeys) BuildSaveRequest(string viewId, string layerName, string typeCode,
        string deviceType, string? description, string[]? parentLayers, bool wip, bool isNew,
        JObject? content, string? comment)
    {
        var save = new JObject
        {
            ["id"] = viewId,
            ["commentText"] = comment ?? "",
            ["deviceType"] = deviceType,
            ["userName"] = _config.UserName,          // 服务端会用 Session.UserID 覆写
            ["applicationType"] = "view",              // 服务端强制校验：仅接受 view/component
            ["countryGroupCode"] = "",
            ["countryCode"] = "",
            ["layerInfo"] = new JObject
            {
                ["company"] = "",
                ["typeCode"] = typeCode,
                ["layerName"] = layerName,
                ["layerDescription"] = description ?? "",
                ["parentLayers"] = JArray.FromObject(parentLayers ?? Array.Empty<string>()),
                ["wip"] = wip,
                ["IsNew"] = isNew,
                ["CGCCode"] = "",                      // 非空会触发 ICESDK 许可证校验
                ["PublishParentLayers"] = false
            }
        };

        var ignored = new List<string>();
        if (content != null)
        {
            foreach (var prop in content.Properties())
            {
                switch (prop.Name)
                {
                    case "layout":
                    case "classicLayout":
                        if (prop.Value is JObject obj) save[prop.Name] = obj; else ignored.Add(prop.Name);
                        break;
                    case "pages":
                    case "events":
                    case "rules":
                    case "tools":
                    case "dataviews":
                    case "orphans":
                    case "properties":
                        if (prop.Value is JArray arr) save[prop.Name] = arr; else ignored.Add(prop.Name);
                        break;
                    case "serializedEvents":
                        if (prop.Value is JObject so) save["serializedEvents"] = so; else ignored.Add(prop.Name);
                        break;
                    case "viewType":
                    case "subApplicationType":
                        save[prop.Name] = prop.Value?.ToString() ?? "";
                        break;
                    case "uxAppVersion":
                        if (int.TryParse(prop.Value?.ToString(), out var ver)) save["uxAppVersion"] = ver;
                        break;
                    case "configuratorInfo":
                        save["configuratorInfo"] = prop.Value;
                        break;
                    default:
                        ignored.Add(prop.Name);
                        break;
                }
            }
        }

        return (new JObject { ["request"] = save }, ignored);
    }

    /// <summary>解析 contentJson 为 JObject，解析失败返回 null。</summary>
    private static (JObject? content, List<string> ignoredKeys) ParseContent(string? contentJson)
    {
        if (string.IsNullOrWhiteSpace(contentJson)) return (null, new List<string>());
        try
        {
            var parsed = JToken.Parse(contentJson);
            return parsed is JObject obj ? (obj, new List<string>())
                 : (null, new List<string> { "(contentJson 不是 JSON 对象)" });
        }
        catch (JsonReaderException)
        {
            return (null, new List<string> { "(contentJson 不是合法 JSON)" });
        }
    }

    private static string? ValidateTypeCode(string? typeCode, bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(typeCode))
            return allowEmpty ? null : "Error: typeCode 必填，取值为 KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）。";
        return typeCode is CustomerLayerType or PersLayerType
            ? null
            : $"Error: 无效的 typeCode '{typeCode}'。仅允许 KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）。";
    }

    private static string? ValidateLayerArgs(string? typeCode, string? layerName, string? description = null)
    {
        var typeError = ValidateTypeCode(typeCode, allowEmpty: false);
        if (typeError != null) return typeError;

        if (string.IsNullOrWhiteSpace(layerName))
            return "Error: layerName 必填（同一视图内唯一，禁止包含逗号，长度不超过 50）。";
        if (layerName.Contains(','))
            return "Error: layerName 禁止包含逗号（服务端校验规则）。";
        if (layerName.Length > 50)
            return $"Error: layerName 长度 {layerName.Length} 超过服务端上限 50。";
        if (!string.IsNullOrEmpty(description) && description.Length > 60)
            return $"Error: description 长度 {description.Length} 超过服务端上限 60。";
        return null;
    }

    private static string FormatSaveResult(EpicorRestAPI.EpicorResponseObject response, List<string> ignoredKeys,
        string action, string viewId, string layerName, string typeCode, bool wip)
    {
        var sb = new StringBuilder();
        sb.Append(action).Append(" layer '").Append(layerName).Append("'（").Append(typeCode)
          .Append("）on view '").Append(viewId).Append("'");

        if (action == "Created" || action == "Updated")
            sb.Append(wip ? " as draft (WIP)" : " and published");
        sb.Append('.');

        if (ignoredKeys.Count > 0)
        {
            sb.Append("\n注意：contentJson 中以下键无法映射到 SaveApp 请求，已忽略：")
              .Append(string.Join(", ", ignoredKeys))
              .Append("。可用键：layout/classicLayout(object)、pages/events/rules/tools/dataviews/orphans/properties(array)、serializedEvents(object)、viewType、subApplicationType、uxAppVersion、configuratorInfo。");
        }

        var body = response.ResponseBody;
        if (!string.IsNullOrWhiteSpace(body) && body.Trim() != "{}" && body.Trim() != "null")
            sb.Append("\nServer response: ").Append(body);

        return sb.ToString();
    }

    /// <summary>解析 LayerExists 等布尔响应（兼容裸布尔与常见包装结构）。</summary>
    private static bool ParseBoolResponse(string body)
    {
        var trimmed = body?.Trim() ?? "";
        if (bool.TryParse(trimmed, out var direct)) return direct;
        try
        {
            var token = JToken.Parse(trimmed);
            if (token is JValue v && v.Value is bool b) return b;
            if (token is JObject obj)
            {
                foreach (var key in new[] { "returnObj", "value", "result", "Response" })
                    if (obj[key] is { } nestedToken && bool.TryParse(nestedToken.ToString(), out var nested)) return nested;
            }
        }
        catch (JsonReaderException) { }
        return false;
    }

    /// <summary>解析 JSON 字符串数组参数，失败返回 null。</summary>
    private static string[]? ParseStringArray(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            var token = JToken.Parse(json);
            return token.Type == JTokenType.Array ? token.ToObject<string[]>() : null;
        }
        catch (JsonReaderException)
        {
            return null;
        }
    }

    /// <summary>
    /// 提交前 sourceTypeId 白名单校验（强制层）。
    /// 递归扫描 contentJson 全树收集 sourceTypeId，未知组件硬报错并给出最近似合法名建议。
    /// 服务端对未知 sourceTypeId 静默容忍（按 base model 存储、前端忽略渲染，无效层），
    /// 必须在此提前拦截。schema 知识库未加载时自动跳过（降级不阻断）。
    /// </summary>
    private string? ValidateComponentTypes(JObject? content, bool allowUnknown)
    {
        if (allowUnknown) return null;
        if (content == null || !_schema.IsLoaded) return null;

        var found = new List<(string path, string typeId)>();
        CollectSourceTypes(content, "$", found);
        var unknown = found.Where(f => !_schema.TryGet(f.typeId, out _)).ToList();
        if (unknown.Count == 0) return null;

        var sb = new StringBuilder();
        sb.Append("Error: contentJson 中发现 ").Append(unknown.Count)
          .Append(" 个不在组件白名单内的 sourceTypeId。服务端会静默接受未知组件但前端不会渲染（无效层），已在此拦截。");
        foreach (var (path, typeId) in unknown.Take(20))
        {
            sb.Append("\n- '").Append(typeId).Append("'  @ ").Append(path);
            var suggestions = _schema.SuggestClosest(typeId);
            if (suggestions.Count > 0)
                sb.Append("  → 是否想用：").Append(string.Join(" / ", suggestions));
        }
        if (unknown.Count > 20)
            sb.Append("\n- ...（其余 ").Append(unknown.Count - 20).Append(" 个略）");
        sb.Append("\n合法 sourceTypeId 清单请调用 ListMetaFxComponents；组件属性明细用 GetMetaFxComponentSchema 查询。");
        sb.Append("\n若确需提交未知组件（不推荐），以 allowUnknownComponents=true 重新调用。");
        return sb.ToString();
    }

    /// <summary>递归收集 JSON 树中全部 sourceTypeId 字符串值（layout/pages 等任意嵌套层级）。</summary>
    private static void CollectSourceTypes(JToken token, string path, List<(string path, string typeId)> found)
    {
        if (token is JObject obj)
        {
            if (obj["sourceTypeId"] is JValue v && v.Type == JTokenType.String)
            {
                var typeId = v.Value<string>();
                if (!string.IsNullOrWhiteSpace(typeId)) found.Add((path, typeId));
            }
            foreach (var prop in obj.Properties())
                CollectSourceTypes(prop.Value, path + "." + prop.Name, found);
        }
        else if (token is JArray arr)
        {
            for (var i = 0; i < arr.Count; i++)
                CollectSourceTypes(arr[i], $"{path}[{i}]", found);
        }
    }

    private static bool ViewMatches(JToken view, string searchText)
    {
        if (view.Type == JTokenType.String)
            return ((string?)view)?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true;
        if (view is JObject obj)
        {
            foreach (var key in new[] { "Id", "ViewId", "viewId", "id", "Name", "name" })
                if (obj[key] != null && ((string?)obj[key])?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                    return true;
            return obj.ToString(Formatting.None).Contains(searchText, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    #endregion
}
