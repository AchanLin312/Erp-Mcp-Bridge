using Newtonsoft.Json.Linq;

namespace ErpMcpBridge.Services;

/// <summary>
/// MetaFX 组件 Schema 知识库
/// 数据源：随程序分发的 MCP/Resources/metafx-component-schema.json
/// （DLL 反编译 + App Studio Designer 属性面板 + 生产语料 presence，三源合并的逆向产物，
///   详见 docs/schema-reverse-engineering.md）
/// 双重职责：
/// - 知识层：供 MetaFxSchemaTools（ListMetaFxComponents / GetMetaFxComponentSchema）按需查询
/// - 强制层：供 LayerManagementService 在 Create/Update 提交前做 sourceTypeId 白名单校验 + typo 建议
/// 文件缺失或损坏时 IsLoaded=false：查询工具返回错误、提交校验自动跳过（降级不阻断服务启动）。
/// </summary>
public class MetaFxSchemaService
{
    private const string ResourceRelPath = "MCP/Resources/metafx-component-schema.json";

    private readonly JObject? _root;
    private readonly Dictionary<string, JObject> _components = new(); // typeId -> 条目（精确大小写）

    /// <summary>知识库是否成功加载（false 时查询不可用、提交校验跳过）</summary>
    public bool IsLoaded { get; }

    /// <summary>加载失败原因（IsLoaded=true 时为 null）</summary>
    public string? LoadError { get; }

    /// <summary>组件总数（应为 77）</summary>
    public int ComponentCount => _components.Count;

    /// <summary>typeId -> 组件条目（精确大小写键，与前端渲染匹配行为一致）</summary>
    public IReadOnlyDictionary<string, JObject> Components => _components;

    /// <summary>layout 结构规则段（nodeShape / boxData 约束 / panel-card 子集合 / tabstrip 约定等）</summary>
    public JObject? LayoutRules => _root?["layoutRules"] as JObject;

    public MetaFxSchemaService()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory,
                ResourceRelPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
            {
                LoadError = $"schema 数据文件不存在: {path}";
                return;
            }
            _root = JObject.Parse(File.ReadAllText(path));
            if (_root["components"] is JObject comps)
            {
                foreach (var prop in comps.Properties())
                {
                    if (prop.Value is JObject entry)
                        _components[prop.Name] = entry;
                }
            }
            IsLoaded = _components.Count > 0;
            if (!IsLoaded) LoadError = "schema 数据文件中 components 为空";
        }
        catch (Exception ex)
        {
            LoadError = ex.Message;
            _root = null;
            _components.Clear();
        }
    }

    /// <summary>精确查找组件条目（大小写敏感，与前端渲染匹配行为一致）。</summary>
    public bool TryGet(string typeId, out JObject entry)
    {
        if (IsLoaded && !string.IsNullOrEmpty(typeId) && _components.TryGetValue(typeId, out var e))
        {
            entry = e;
            return true;
        }
        entry = null!;
        return false;
    }

    /// <summary>忽略大小写解析为规范 typeId（查询工具用；白名单校验不走此处，见 TryGet）。</summary>
    public bool TryGetCanonical(string typeId, out string canonical)
    {
        canonical = typeId ?? "";
        if (!IsLoaded || string.IsNullOrWhiteSpace(typeId)) return false;
        if (_components.ContainsKey(typeId)) return true;
        var hit = _components.Keys.FirstOrDefault(k =>
            string.Equals(k, typeId, StringComparison.OrdinalIgnoreCase));
        if (hit == null) return false;
        canonical = hit;
        return true;
    }

    /// <summary>
    /// 给出未知 typeId 的最近似合法组件名（Levenshtein 距离，忽略大小写）。
    /// 距离阈值随长度放宽（≤4 字符 1 / ≤8 字符 2 / 其余 3），无达标项返回空列表。
    /// </summary>
    public List<string> SuggestClosest(string typeId, int maxSuggestions = 3)
    {
        var result = new List<string>();
        if (!IsLoaded || string.IsNullOrWhiteSpace(typeId)) return result;

        var threshold = typeId.Length <= 4 ? 1 : typeId.Length <= 8 ? 2 : 3;
        var scored = new List<(string id, int dist)>();
        foreach (var key in _components.Keys)
        {
            scored.Add((key, Levenshtein(typeId, key)));
        }
        var best = scored.Min(s => s.dist);
        if (best > threshold) return result;

        result.AddRange(scored
            .Where(s => s.dist <= best + 1)
            .OrderBy(s => s.dist).ThenBy(s => s.id, StringComparer.Ordinal)
            .Take(maxSuggestions)
            .Select(s => s.id));
        return result;
    }

    private static int Levenshtein(string a, string b)
    {
        a = a.ToLowerInvariant();
        b = b.ToLowerInvariant();
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;
        var prev = new int[b.Length + 1];
        var cur = new int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++) prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
            }
            (prev, cur) = (cur, prev);
        }
        return prev[b.Length];
    }
}
