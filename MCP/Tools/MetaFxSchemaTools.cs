using System.ComponentModel;
using System.Text;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ErpMcpBridge.MCP.Tools;

/// <summary>
/// MetaFX 组件 Schema 查询工具（知识层）
/// 与 LayerManagementTools 配套：构建层 layout 前先查这里拿合法 sourceTypeId 白名单与属性明细。
/// 数据源为三源逆向产物 metafx-component-schema.json（随程序分发）。
/// </summary>
[McpServerToolType]
public class MetaFxSchemaTools
{
    private readonly MetaFxSchemaService _schema;

    public MetaFxSchemaTools(MetaFxSchemaService schema)
    {
        _schema = schema;
    }

    [McpServerTool(Name = "ListMetaFxComponents")]
    [Description("列出 Kinetic（App Studio）layout 可用的全部组件类型（即 sourceTypeId 白名单，含生产使用频次）。构建或修改层 layout 前必须先调用本工具；presence 越大 = 生产环境使用越广，recommended=true 的高频组件优先使用。cfg-*（namespace=configurator）属于配置器设计器，禁止用于 App Studio layout。选中组件后用 GetMetaFxComponentSchema 查看属性明细。")]
    public string ListMetaFxComponents(
        [Description("可选，按 typeId 关键字过滤（不区分大小写），如 \"text-box\"、\"panel-card\"")] string? searchText = null,
        [Description("可选，按命名空间过滤：appStudio（App Studio 层）或 configurator（配置器）")] string? ns = null,
        [Description("可选，仅返回 recommended=true 的高频组件")] bool recommendedOnly = false)
    {
        if (!_schema.IsLoaded)
            return $"Error: 组件 schema 知识库未加载：{_schema.LoadError}";

        IEnumerable<KeyValuePair<string, JObject>> entries = _schema.Components;
        if (!string.IsNullOrWhiteSpace(searchText))
            entries = entries.Where(kv => kv.Key.Contains(searchText, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(ns))
            entries = entries.Where(kv =>
                string.Equals((string?)kv.Value["namespace"], ns, StringComparison.OrdinalIgnoreCase));
        if (recommendedOnly)
            entries = entries.Where(kv => (bool?)kv.Value["recommended"] == true);

        var ordered = entries
            .OrderByDescending(kv => (int?)kv.Value["presence"] ?? 0)
            .ThenBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        var arr = new JArray();
        foreach (var (typeId, entry) in ordered)
        {
            var item = new JObject
            {
                ["typeId"] = typeId,
                ["source"] = (string?)entry["source"],
                ["namespace"] = (string?)entry["namespace"],
                ["presence"] = (int?)entry["presence"] ?? 0,
                ["recommended"] = (bool?)entry["recommended"] == true
            };
            if (entry["childCollections"] is JArray cc && cc.Count > 0)
                item["childCollections"] = cc;
            arr.Add(item);
        }

        return new JObject
        {
            ["componentCount"] = _schema.ComponentCount,
            ["shown"] = arr.Count,
            ["hint"] = "typeId 即 layout 节点的 sourceTypeId。确定选型后用 GetMetaFxComponentSchema(typeId) 查看属性明细与 layout 结构规则。recommended=true 与 presence 降序可作为选型优先级。",
            ["components"] = arr
        }.ToString(Formatting.Indented);
    }

    [McpServerTool(Name = "GetMetaFxComponentSchema")]
    [Description("查询单个 MetaFX 组件的完整 schema：全部属性（origin=base 为服务端基类属性 / dll 为服务端强类型属性 / designer 为 App Studio 前端消费属性，服务端原样保留不校验）、.NET 类型、属性面板 editorType、分类、枚举值、合法子集合，以及 layout 结构规则（节点形状 / boxData 网格约束 / panel-card 子集合与 parentId 约定 / tabstrip 约定 / contentJson 禁止文件级外壳）。")]
    public string GetMetaFxComponentSchema(
        [Description("组件类型 ID，必须来自 ListMetaFxComponents 返回的 typeId，如 erp-text-box")] string typeId)
    {
        if (!_schema.IsLoaded)
            return $"Error: 组件 schema 知识库未加载：{_schema.LoadError}";

        if (_schema.TryGetCanonical(typeId, out var canonical) && _schema.TryGet(canonical, out var entry))
        {
            return new JObject
            {
                ["typeId"] = canonical,
                ["schema"] = entry,
                ["layoutRules"] = _schema.LayoutRules
            }.ToString(Formatting.Indented);
        }

        var sb = new StringBuilder();
        sb.Append("Error: 未知组件类型 '").Append(typeId).Append("'。");
        var suggestions = _schema.SuggestClosest(typeId);
        sb.Append(suggestions.Count > 0
            ? "是否想用：" + string.Join(" / ", suggestions) + "。"
            : "无近似匹配。");
        sb.Append("合法 typeId 请通过 ListMetaFxComponents 查询。");
        return sb.ToString();
    }
}
