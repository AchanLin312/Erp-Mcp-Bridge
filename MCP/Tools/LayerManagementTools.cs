using System.ComponentModel;
using ErpMcpBridge.Services;
using ModelContextProtocol.Server;

namespace ErpMcpBridge.MCP.Tools;

/// <summary>
/// Kinetic Layer（App Studio 层）管理工具
/// 基于 Ice.Lib.MetaFXSvc 实现层的增删改查与发布
/// </summary>
[McpServerToolType]
public class LayerManagementTools
{
    private readonly LayerManagementService _layerMgr;

    public LayerManagementTools(LayerManagementService layerMgr)
    {
        _layerMgr = layerMgr;
    }

    [McpServerTool(Name = "ListKineticViews")]
    [Description("列出 Kinetic（App Studio）视图（App）。结果较大时建议用 searchText 过滤，如 \"Erp.UI.Part\"、\"MCPTestApp\"。")]
    public async Task<string> ListKineticViews(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("可选，按视图名模糊过滤（不区分大小写）")] string? searchText = null)
    {
        return await _layerMgr.ListViewsAsync(company, searchText ?? "");
    }

    [McpServerTool(Name = "ListKineticLayers")]
    [Description("列出指定 Kinetic 视图（App）上的层。typeCode 不填时同时查询客户自定义层（KNTCCustLayer）与个性化层（KNTCPersLayer）。")]
    public async Task<string> ListKineticLayers(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID，如 Erp.UI.AbcCodeEntry")] string viewId,
        [Description("可选，层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层），不填查两者")] string? typeCode = null,
        [Description("是否包含未发布（草稿/WIP）层，默认 true")] bool includeUnpublishedLayers = true)
    {
        return await _layerMgr.ListLayersAsync(company, viewId, typeCode ?? "", includeUnpublishedLayers);
    }

    [McpServerTool(Name = "GetKineticLayer")]
    [Description("读取指定 Kinetic 层的完整内容。返回的 Content 即层内容键值对（layout/pages 等）。典型工作流：GetKineticLayer 取内容 → 修改 → UpdateKineticLayer 提交。")]
    public async Task<string> GetKineticLayer(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID，如 Erp.UI.AbcCodeEntry")] string viewId,
        [Description("层名称")] string layerName,
        [Description("层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）")] string typeCode,
        [Description("设备类型，默认 Desktop")] string? deviceType = null,
        [Description("是否返回草稿（WIP）内容，默认 true")] bool includeDraftContent = true)
    {
        return await _layerMgr.GetLayerDetailAsync(company, viewId, layerName, typeCode, deviceType ?? "Desktop", includeDraftContent);
    }

    [McpServerTool(Name = "CreateKineticLayer")]
    [Description("在指定 Kinetic 视图（App）上新建层。默认保存为草稿（wip=true），发布请用 PublishKineticLayer。contentJson 可选，为层内容 JSON 对象，可用键：layout(object)、classicLayout(object)、pages/events/rules/tools/dataviews/orphans/properties(array)、serializedEvents(object)。最小示例：{\"layout\":{}}。首次构建 layout 前必须先调用 ListMetaFxComponents 获取合法 sourceTypeId 白名单（优先 presence 高、recommended=true 的组件），并用 GetMetaFxComponentSchema 查看组件属性与 layout 结构规则；提交时会校验 layout 内全部 sourceTypeId，未知组件将被拦截。注意：layout 键的值必须是内层 layout 节点（如 {\"name\":\"App\",\"guid\":\"...\",...}），不要传带 $schema/contentVersion/imports 的文件级外壳，否则会产生整节点 Replace 的大 diff。层名禁止逗号、最长 50 字符。若目标 App 是自建的（经 SaveApp 创建），ViewId 必须带 ApplicationType.json 定义的前缀（Ice.UIDbd.=仪表盘、Ice.UI./Erp.UI.=应用、Ice.UIRpt./Erp.UIRpt./Erp.UIDynRpt.=报表、Ice.UIProc./Erp.UIProc.=流程、Erp.UIShared.=共享、Erp.UICfg.=配置器），否则 App Studio 列表按前缀解析子类型失败会把整个 App（含其上的层）过滤为不可见，尽管数据库行完整存在。")]
    public async Task<string> CreateKineticLayer(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID，如 Erp.UI.AbcCodeEntry 或自定义 App ID（自建 App 必须带 Ice.UIDbd. 等前缀，否则 App Studio 不可见）")] string viewId,
        [Description("层名称（同视图内唯一，禁止逗号，最长 50 字符）")] string layerName,
        [Description("层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）")] string typeCode,
        [Description("可选，层描述（最长 60 字符）")] string? description = null,
        [Description("可选，父层名称数组 JSON，如 [\"BaseLayer\"]，默认空数组（直接基于基线）")] string? parentLayersJson = null,
        [Description("可选，层内容 JSON 对象，如 {\"layout\":{...}} 或 {\"layout\":{},\"pages\":[...]}")] string? contentJson = null,
        [Description("是否保存为草稿（WIP），默认 true。false 时保存并直接发布")] bool wip = true,
        [Description("跳过 sourceTypeId 白名单校验（不推荐）。默认 false：未知组件会被拦截并给出最近似合法名建议")] bool allowUnknownComponents = false)
    {
        return await _layerMgr.CreateLayerAsync(company, viewId, layerName, typeCode, description ?? "",
            parentLayersJson, contentJson, wip, allowUnknownComponents);
    }

    [McpServerTool(Name = "UpdateKineticLayer")]
    [Description("更新指定 Kinetic 层的内容（merged 模式：服务端自动计算与基线的差异，无需自己算 diff）。contentJson 为完整 merged 内容 JSON 对象，可用键：layout(object)、classicLayout(object)、pages/events/rules/tools/dataviews/orphans/properties(array)、serializedEvents(object)、viewType、subApplicationType、uxAppVersion。典型流程：GetKineticLayer 取 Content → 修改 → 本工具提交。layout 内新增或修改组件前，先调用 ListMetaFxComponents 获取合法 sourceTypeId 白名单（属性明细用 GetMetaFxComponentSchema 查看）；提交时会校验 layout 内全部 sourceTypeId，未知组件将被拦截。注意：layout 键的值必须是内层 layout 节点（GetKineticLayer 返回的 Content.layout 即是），不要传带 $schema/contentVersion/imports 的文件级外壳，否则会产生整节点 Replace 的大 diff。固定保存为草稿，发布请用 PublishKineticLayer。")]
    public async Task<string> UpdateKineticLayer(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID")] string viewId,
        [Description("层名称")] string layerName,
        [Description("层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）")] string typeCode,
        [Description("完整 merged 层内容 JSON 对象，如 {\"layout\":{...},\"pages\":[...]}")] string contentJson,
        [Description("可选，变更备注")] string? comment = null,
        [Description("跳过 sourceTypeId 白名单校验（不推荐）。默认 false：未知组件会被拦截并给出最近似合法名建议")] bool allowUnknownComponents = false,
        [Description("可选，层描述（最长 60 字符）。不传则保留现有描述不变")] string? description = null)
    {
        return await _layerMgr.UpdateLayerAsync(company, viewId, layerName, typeCode, contentJson, comment ?? "",
            allowUnknownComponents, description);
    }

    [McpServerTool(Name = "DeleteKineticLayer")]
    [Description("删除指定 Kinetic 层。危险操作：必须显式传 confirm=true 才会执行。删除前建议先 GetKineticLayer 确认目标。")]
    public async Task<string> DeleteKineticLayer(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID")] string viewId,
        [Description("层名称")] string layerName,
        [Description("层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）")] string typeCode,
        [Description("设备类型，默认 Desktop")] string? deviceType = null,
        [Description("删除确认，必须为 true 才执行")] bool confirm = false)
    {
        if (!confirm)
        {
            return "未执行：删除层为危险操作。确认目标无误后，以 confirm=true 重新调用本工具。"
                 + $"当前目标：view '{viewId}'，layer '{layerName}'（{typeCode}）。";
        }
        return await _layerMgr.DeleteLayerAsync(company, viewId, layerName, typeCode, deviceType ?? "Desktop");
    }

    [McpServerTool(Name = "PublishKineticLayer")]
    [Description("发布指定 Kinetic 层（将草稿内容发布为生效版本）。危险操作：必须显式传 confirm=true 才会执行。注意：发布层不会自动发布其父层。")]
    public async Task<string> PublishKineticLayer(
        [Description("公司代码，如 GPEHZ、GPEHK")] string company,
        [Description("视图（App）ID")] string viewId,
        [Description("层名称")] string layerName,
        [Description("层类型：KNTCCustLayer（客户自定义层）或 KNTCPersLayer（个性化层）")] string typeCode,
        [Description("设备类型，默认 Desktop")] string? deviceType = null,
        [Description("发布确认，必须为 true 才执行")] bool confirm = false)
    {
        if (!confirm)
        {
            return "未执行：发布层为危险操作。确认目标无误后，以 confirm=true 重新调用本工具。"
                 + $"当前目标：view '{viewId}'，layer '{layerName}'（{typeCode}）。";
        }
        return await _layerMgr.PublishLayerAsync(company, viewId, layerName, typeCode, deviceType ?? "Desktop");
    }
}
