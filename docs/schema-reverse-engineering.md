# Epicor Kinetic App Studio UI Schema 逆向工程报告

> 版本：v2.0（2026-09-21）
> 环境基准：Epicor Kinetic（Server.Latest），`D:\Epicor\Server.Latest\Assemblies\Epicor.MetaFX.Core.dll`
> 反编译产物：`.decomp/MetaFXCore/`（ilspycmd 11.0.0.9375，475 个 .cs 文件全量落盘）
> v2.0 新增：三源对齐矩阵（DLL 模型 ∪ Designer 属性面板 ∩ 生产语料）+ 全量 presence 验证，见 §7；机器可读产物 `docs/three-source-alignment.json`

---

## 0. 结论（TL;DR）

**可行性：已被证实。** "App Studio schema 未公开" 的前提是真的，但 schema 的权威定义（ground truth）可以从三条独立证据链完整重建：

| 证据链 | 方法 | 结果 |
|---|---|---|
| A：正样本语料归纳 | SQL 扫描 `KNTCMetaUI` 表 25,194 个真实 UI 文件 | 拿到全部生产在用的 sourceTypeId、字段命名、结构形态 |
| B：服务端 oracle 自验证 | 通过 Create/Update/DeleteKineticLayer 提交假设 → 读回 diff → 修正 | 证实存储层**零校验**、运行时**宽容降级**，试错闭环安全且收敛 |
| C：服务端反编译 | 反编译 `Epicor.MetaFX.Core.dll`（ModelConverter 所在 DLL） | **直接拿到 schema 真身**：31 个注册组件类型 + 基类 19 属性 + 拍平算法 + 文件信封逻辑 |
| D：三源对齐（v2.0 新增） | DLL 模型 ∪ Designer 属性面板（main.js 65 组件）∩ 生产语料 presence | 交叉印证矩阵 + 全量 presence 计数，见 §7 |

关于用户提到的 AI "自验证"（Self-Verification）概念：本方案正是 generate → execute(oracle) → feedback → refine 循环（对应 Self-Refine / Self-Verification 类文献，以及 property-based testing 的思想）。链 B 的实验证明该闭环在本场景**可安全执行**（不会破坏系统），链 C 则把"试"的成本降到接近零——因为答案可以直接读出来。

**最重要的发现：所谓"未公开的 schema"其实一直藏在服务端 DLL 里。** `Epicor.MetaFX.Core.Models.Layout` 命名空间就是 schema 的 C# 表达形式，`ModelConverter` 按名字注册每一个组件类型，比手写 JSON Schema 更权威。

**v2.0 三源对齐结论（详见 §7）**：
1. **DLL ∩ Designer 的 19 个组件类型 100% 有生产使用**——两条独立证据链完全互证，这部分属性集可作为 schema 的"黄金核心"。
2. **Designer 超前于 DLL**：46 个 designerOnly 组件中 29 个有生产使用（如 `metafx-tabstrip` 415 文件、`metafx-col` 635 文件），服务端按 ModelConverter 退化机制落入基类存储、字段由前端消费。
3. **DLL 中唯一真孤儿是 `ep-embedded-app`**（注册但生产 0 使用）；11 个 `cfg-*` 属 Configurator 命名空间，不在 App Studio 语料中属预期。
4. **Designer 侧 `ep-*` 基础控件（text-box/button/combo-box 等 12 个）生产 0 使用**——生产文件一律用其 `erp-*` 增强版（`erp-text-box` 1,365 vs `ep-text-box` 0）。
5. 属性级：DLL∪Designer 合计 624 个属性，shared 147 / dllOnly 311 / designerOnly 166；基类经频次法交叉验证（designer 公共属性 18 ≈ DLL ComponentModel 19）。

---

## 1. 服务端强类型模型（Ground Truth）

### 1.1 Component 容器（树节点）

来源：`Epicor.MetaFX.Core.Models.Layout.Component`

| 字段 | 类型 | JSON 行为 | 说明 |
|---|---|---|---|
| `parentId` | string | Include（可为 null） | 拍平后指向父节点；根节点为 null |
| `id` | string | 默认 | 组件唯一标识，拍平命名空间内唯一 |
| `targetTypeId` | string | 默认 | 目标类型（sourceTypeId 缺失时的兜底） |
| `sourceTypeId` | string | 默认 | **组件类型注册键**（如 `erp-text-box`） |
| `model` | ComponentModel | 默认 | 强类型属性容器（嵌套式） |
| `components` | List&lt;Component&gt; | Ignore(null) | 子节点（仅树形形态使用） |
| `boxData` | ComponentBoxData | 默认 | 网格定位（仅网格布局） |
| `isSelectedTab` | bool? | 默认 | tab 选中态 |
| `tabIndex` | int? | 默认 | tab 序号 |

### 1.2 ComponentModel 基类（所有组件的公共属性，19 个）

来源：`Epicor.MetaFX.Core.Models.Layout.ComponentModel`。全部属性标注 `[JsonProperty(Order = int.MinValue)]`——这解释了真实 JSON 中基类属性总是排在最前面的现象。

| # | 属性 | .NET 类型 | 默认 JSON 行为 |
|---|---|---|---|
| 1 | `id` | string | 默认 |
| 2 | `class` | string | 默认 |
| 3 | `flexBasis` | string | 默认 |
| 4 | `guid` | string | 默认 |
| 5 | `labelText` | string | 默认 |
| 6 | `hidden` | bool | 默认（始终输出） |
| 7 | `isKeyField` | bool? | Ignore(null) |
| 8 | `epBinding` | string | 默认 |
| 9 | `tabIndex` | int? | Ignore(null) |
| 10 | `disabled` | bool? | Ignore(null) |
| 11 | `readonly` | bool? | Ignore(null) |
| 12 | `alignment` | string | 默认 |
| 13 | `minHeight` | int? | Ignore(null) |
| 14 | `disabledToolBar` | bool? | Ignore(null) |
| 15 | `epiLabel` | string | 默认 |
| 16 | `height` | long? | Ignore(null) |
| 17 | `width` | long? | Ignore(null) |
| 18 | `top` | long? | Ignore(null) |
| 19 | `left` | long? | Ignore(null) |

### 1.3 ComponentBoxData（网格定位）

来源：`ComponentBoxData.cs`

- 字段：`x` / `y` / `h` / `w`（int），默认构造 `x=1, y=1, h=3, w=6`
- **硬约束常量**：`MINIMUM_ROW_SIZE = 3`（h 最小 3）、`MINIMUM_COLUMN_SIZE = 6`（w 最小 6）

### 1.4 ModelConverter 反序列化机制（schema 的"注册表"）

来源：`Epicor.MetaFX.Core.Models.Layout.ModelConverter.cs`

- 静态构造器：反射扫描 `ComponentModel` 的所有子类，按 `[ComponentModel("sourceTypeId")]` 特性建立 `sourceTypeId → Type` 字典
- `ReadJson`：优先读 `sourceTypeId`，为空则回退 `targetTypeId`；查字典命中 → 构造对应强类型后 `serializer.Populate`；**未命中 → 退化为基类 `ComponentModel`（不报错）**
- `WriteJson`：`throw NotImplementedException`——只读方向的转换器
- **推论**：任何未注册的 sourceTypeId 都能通过反序列化（字段进基类），这是 oracle 实验中"未知组件宽容降级"的源码级解释

### 1.5 两种序列化形态

1. **树形（嵌套）**：`Component.components` / `model` 递归嵌套——数据库 `KNTCMetaUI` 中抽取的真实文件（草稿/工作态）呈此形态
2. **扁平（拍平）**：发布时经 `FlattenLayout.ConvertToFlattenLayout` 转换，`parentId` 引用替代嵌套——published 的 `layout.jsonc` 呈此形态

两种形态由 `ConvertToFlattenLayout` / `ConvertToHierarchicalStructure` 互逆转换。

---

## 2. 组件注册表（31 个 `[ComponentModel]` 注册类型）

### 2.1 Layout.ErpControls（16 个注册）

| sourceTypeId | C# 类型 | 自有属性（类型 · JSON 行为） |
|---|---|---|
| `erp-text-box` | ErpTextBox | `multiline`(bool) · `enableSearch`(bool) |
| `erp-button` | ErpButton | `style`(int?) · `size`(string，默认 `"ep-btn-size-medium"`) |
| `erp-check-box` | ErpCheckBox | （无自有属性，仅基类 19 项） |
| `erp-combo-box` | ErpComboBox | `appDesignerComboType` · `appDesignerComboOverride`(bool?) · `epBindingTextColumn` · `$ref`(string，JsonProperty 特名) · `dropDownStyle` · `isDelimited`(bool?) · `textField` · `valueField` · `list`(SortFields[]) · `svc` · `svcPath` · `tableName` · `fieldName` · `dbColumn` · `sort` · `parameterName` · `dataSource` · `viewName` · `restArguments`(List&lt;string&gt;) · `queryId`(Ignore null) |
| `erp-date-picker` | ErpDatePicker | `min`(string) · `max`(string) |
| `erp-numeric-box` | ErpNumericBox | `nullable`(bool) · `mask`(string) · `min`(double?) · `max`(double?) · `step`(long?) |
| `erp-picture-box` | ErpPictureBox | `imageType`(int) · `imageFillType`(int)；枚举 `PictureBoxImageType`: ImageBlob=0, Url=1, IconClass=2, FileStore=3 |
| `erp-print-output-format` | ErpPrint | `epBindingEnableRouting` · `epBindingReportId` |
| `erp-radio-button` | ErpRadioButton | `data`(DataFields[]) · `svc` · `svcPath` · `tableName` · `fieldName` · `dbColumn` · `sort` · `parameterName` · `dataSource` · `viewName` · `verticalAlign`(bool) |
| `erp-text-area` | ErpTextArea | `cols`(int) · `rows`(int) |
| `erp-token-date` | ErpTokenDate | `epBindingToken` · `svc` |
| `erp-website` | ErpWebsite | `url` · `widgetType` · `widgetHeight`(long，ShouldSerialize 仅非 0) · `widgetWidth`(long，同上) · `iconClass` · `title` · `userContext`(bool) · `token`(bool) · `capabilities`(List&lt;Capability&gt;) |
| `metafx-div` | MetaFxDiv | `orientation`(string) · `expandPanelBar`(bool?) |
| `metafx-box` | MetaFxBox | `flexDirection`(string) · `columnsWide`(string) · `components`(List&lt;Component&gt;，**小写开头**，model 内子集合) |
| `metafx-panel-card` | MetaFxPanelCard | `expanded`(bool) · `highLightPanelBar`(bool) · `title` · `expandMore`(bool) · `enableActionButton`(bool) · `headerComponents`(List&lt;Component&gt;) · `expandedHeaderComponents`(List&lt;Component&gt;) · `extensionComponents`(List&lt;Component&gt;) · `footer`(Footer) · `actionData`(List&lt;ActionData&gt;) · `bodyComponents`(List&lt;Component&gt;) · `stacked`(bool)；构造器全部初始化为空集合 |
| `metafx-panel-card-grid` | MetaFxPanelCardGrid（继承 PanelCard） | `autoFillContainer`(bool) · `gridModel`(GridModel) · `viewOptions`(List&lt;ProviderModel&gt;) · `fullScreenButtonVisible`(bool) |

### 2.2 Layout.EpControls（4 个注册）

| sourceTypeId | C# 类型 | 自有属性 |
|---|---|---|
| `ep-label` | EpLabel | `textHAlign` · `textVAlign` |
| `ep-row-navigator` | EpRowNavigator | （无自有属性） |
| `ep-embedded-app` | EpEmbeddedApp | `widgetType` · `embeddedAppWidget`(EpAppWidget) · `iconClass` · `widgetHeight`(int) · `capabilities`(object[]) |
| `ep-search-chip-selection` | SearchChipSelection | `emptyLabelText` · `singularLabelText` · `pluralLabelText` |

### 2.3 Configurator 命名空间（11 个注册，顺带产出）

`cfg-erp-button`、`cfg-erp-text-box`、`cfg-text-area`、`cfg-check-box`、`cfg-radio-button`、`cfg-picture-box`、`cfg-numeric-box`、`cfg-date-picker`、`cfg-erp-combo-box`、`cfg-erp-website`、`cfg-slider-button`（均在 `Epicor.MetaFX.Core.Models.Configurator` 下，属于 Configurator 设计器而非 App Studio layout）

### 2.4 辅助模型（非 sourceTypeId，作为属性值嵌套）

| 类型 | 字段 |
|---|---|
| `PanelCardStackItem` | `id` · `name` |
| `ActionData` | `id` · `description`(Include null) · `icon` · `showInToolBar` · `userData` |
| `GridModel` | `height` · `scrollable` · `pageSize` · `sortable` · `resizable` · `editable` · `epBinding` · `providerModel`(Include null) · `columns`(Columns[]) · `group`(Group[]) |
| `ProviderModel` | `description` · `isDefault` · `serverPaging` · `epBinding` · `baqId` · `baqOptions`(BaqOptions) · `columns` · `autoRefreshOnLoad` |
| `BaqOptions` | `where` · `whereList`(List&lt;BAQWhereExpression&gt;) |
| `BAQWhereExpression` | `column` · `condition` · `value` · `honorNull`(bool) · `emptyValue`(object) · `compareColumns`(bool) |
| `Columns` | `field` · `title` · `sortable` · `filterable` · `resizable` · `editable` · `groupable` · `hidden` · `imageName` · `visibleIndex`(int，**小写 v**) |
| `Group` | `field` |
| `GroupInfo` | `groups`(List&lt;Group&gt;) · `queryID` · `viewID` |
| `SortFields` | `display` · `value` |
| `DataFields` | `label` · `value` |
| `UserData` | `tag` · `epBinding` · `type` · `pageLevel`(bool) |
| `Footer` | `showMore`(bool?) · `navLinkModel`(NavLinkModel) |
| `NavLinkModel` | `id` · `text` · `href` · `target` · `name` · `rel` · `title` · `page` · `iconClass` |
| `Capability` | `id` · `description` |
| `EpAppWidget` / `EpAppWidgetModel` | 嵌入式 App 的 widget 描述模型 |
| `EpAppCapability` | `id` · `description` · `model`(EpChartPubSub) |
| `EpChartPubSub` | `id` · `subscribers`(List&lt;EpChartSubscribers&gt;) |
| `EpChartSubscribers` | `publisher` · `condition` · `subscriber` |
| `MetaFxPanelCardStack` | **无 [ComponentModel] 特性**：`title` · `panels`(List&lt;PanelCardStackItem&gt;) · `autoSize` · `currentIndex`(bool) · `expanded`(bool) · `disableExpandCollapse`(bool) |
| `MetaFxTwoColumn` | **继承 Component**（非 ComponentModel）：`rowComponent` · `column1` · `column2`(均 Component) |
| `MetaFxTwoColumnComponentModel` | **无特性**：`columns`(int?)，`[JsonProperty(Order = int.MinValue)]` |

### 2.5 前端专属类型（服务端 DLL 无强类型，经语料归纳）

以下 sourceTypeId 在 `Epicor.MetaFX.Core.dll` 中**没有**对应类，ModelConverter 会将其退化为基类 `ComponentModel`；字段实际由前端消费。属性清单来自 `Ice.XXXDef`（KNTCMetaUI）生产语料归纳：

| sourceTypeId | 语料规模（v2.0 精确口径*） | 归纳结果 |
|---|---|---|
| `metafx-tabstrip` | 415 文件（裸 LIKE 含引用为 1,121） | 见下表 |
| `metafx-panel-card-gantt` | 1 文件（裸 LIKE 含引用为 5） | 见下表 |
| `metafx-udc` | **0 文件**（本环境无实例；FlattenLayout 有专门分支 + `ResetReusableComponentRef` 证明机制存在，引用走 `$ref`） | — |
| `erp-shape` | 164 文件（裸 LIKE 含引用为 546，反编译补查时新发现） | 见下表 |

\* v2.0 口径：`Content LIKE '%"sourceTypeId":"<id>"%'`（带引号精确匹配，避免子串污染）；裸口径为 `LIKE '%<id>%'`，会把注释/引用/父记录等一起计入。全量 77 类型精确计数见 §7.3 与 `.tools/presence-counts.json`。

**metafx-tabstrip**（tab 容器，子组件拍平时 parentId = `<tabstrip-id>~<tab-id>`）：

| 层 | 字段 |
|---|---|
| model | `id` · `canCustomize`(bool) · `canPersonalize`(bool) · `hidden`(bool，可选) · `guid` · `data[]` |
| model.data[] 项 | `id` · `title` · `page` · `selected`(bool) · `isKeyField`(bool，可选) · `appStudioSelected`(bool，可选) |
| Component 级扩展 | `layoutData`: `{flexLayout: bool, hostId: string}` |

**metafx-panel-card-gantt**（调度板甘特图，panel-card 变体——IsPanelCard 判定包含它，拍平规则同 `#` 约定）：

| 分组 | 字段 |
|---|---|
| 基本 | `title` · `guid` · `id` · `labelText` · `autoFillContainer`(bool，可选) |
| 调度绑定 | `schedulingBoardType`（JobSchedulingBoard / MultiResourceSchedulingBoard / ProjectSchedulingBoard / ResourceSchedulingBoard）· `epBindingSchedulingBoard` · `epBindingSelectedJob` · `epBindingSBMaterial`（可选）· `epBindingSBJobs`（可选）· `epBindingSchedulingEngine` · `epBindingStartEndDate`（可选）· `epBindingRevision`（可选）· `epBindingSaveProject`（可选） |
| `ganttModel` | `epBinding` · `epBindingLink`（可选）· `columns[]`{`name` · `label` · `width`(int) · `align`(Left/…) · `resize`(bool) · `tree`(bool) · `isContextMenu`(bool)} · `config`{`orderBranch` · `dragProgress` · `showLinks` · `dragResize` · `autoScheduling` · `autoSchedulingInitial` · `autoSchedulingMoveProjects` · `showMarkers`（均可选 bool）} · `numberOfTaskLayers`(int) · `timeline`("Days"，可选) · `readonly`(bool，可选) · `verticalAutosize`(bool，可选) · `id`（可选） |
| panel-card 遗传 | `headerComponents[]` · `expandedHeaderComponents[]` · `actionData[]`{`id` · `description` · `icon`(mdi 类) · `epBinding`（可选）· `showInToolBar`(bool，可选)} |

**erp-shape**（形状/状态指示器，多用于 heroBar；Component 级与 panel-card 平级）：

| 层 | 字段 |
|---|---|
| model | `guid` · `id` · `enabledCaption` · `disabledCaption` · `personalizable`(bool) · `customizable`(bool) · `hidden`(bool) · `alignment`("Center"…) · `status`(int，1=警告 2=正常 之类语义) · `epBinding` |

**前端扩展的基类字段**（出现在 1,201 个文件中，但不在服务端 ComponentModel 基类 19 属性内）：

- `personalizable`(bool) / `customizable`(bool) —— 个性化/定制开关，前端消费，服务端存储时原样保留（零校验）

归纳方法（可复用）：`SELECT ... FROM Ice.XXXDef WHERE TypeCode='KNTCMetaUI' AND Content LIKE '%<type>%'` 抽样 → 字段并集；与 FlattenLayout 分支交叉印证。

---

## 3. 拍平规则（FlattenLayout.cs）

来源：`Epicor.MetaFX.Core.Helper.FlattenLayout.cs`

### 3.1 子集合命名约定（`#` 分隔）

panel-card 系列的 5 个合法子集合（`allowedChildrens`）：

```
components, bodyComponents, expandedHeaderComponents, extensionComponents, data
```

拍平后子组件的 `parentId = "<panel-card-id>#<collection-name>"`，例如：

```
parentId: "panel1#bodyComponents"
```

### 3.2 tabstrip 约定（`~` 分隔）

```
parentId: "<tabstrip-id>~<tab-id>"
```

### 3.3 其他规则

- **重名处理**：`getUniqueId` 对重复 id 追加数字后缀 2..99
- **ignoreKeys**（拍平/还原时忽略的顶层键）：
  `PatchInfo, DraftEvents, DraftLayout, DraftRules, PatchTokens, AllowVersions, References, Properties, Wasm, IsLayerDisabled, ConfiguratorInfo, ApplicationStrings`
- **三种特殊分支**：`metafx-tabstrip`、IsPanelCard（panel-card/grid/gantt）、`metafx-udc`

---

## 4. 文件信封（LayoutMetadata / PageMetadata / BaseMetadataType）

来源：`Epicor.MetaFX.Core.AppStudio.Metadata.*`

### 4.1 存储路径

| 文件 | 路径 | 形态 |
|---|---|---|
| published | `{rootPath}/{viewId}/layout.jsonc` | 拍平式 |
| designer draft | `{rootPath}/{viewId}/layout.designer.draft.jsonc` | 树形 |
| draft | `{rootPath}/{viewId}/layout.draft.jsonc` | 树形 |
| 分页 | `pages/` 子目录（PageMetadata 管理） | — |

### 4.2 信封与保存流程

- 文件信封包含 `layout` / `pageType` / `patch` 等顶层字段
- 保存前调用 `FlattenLayout.ConvertToFlattenLayout`，并执行 `ResetReusableComponentRef` / `ResetReusableComboRef`（udc/combo 引用复位）
- `BaseMetadataType`：draft/publish 双态模型，A/U/D（Add/Update/Delete）变更标志，`patch` 数组承载增量
- 服务端保存时自动计算 layout diff（PropDiff → Add/Remove/Replace）

---

## 5. 官方 schema 家族（已获取的 7 个公开 schema）

以下 URL 2024 实测公开可访问，且被大量生产文件引用：

| Schema | URL | JSON Schema Draft | 生产引用数 |
|---|---|---|---|
| layout | `https://schemas.developer.epicor.com/schemas/v3.0/layout.json` | draft-07 | 4,659 |
| events | `https://schemas.developer.epicor.com/schemas/v5.0/events.json` | draft-04 | 4,538 |
| rules | `https://schemas.developer.epicor.com/schemas/v3.0/rules.json` | draft-07 | 3,269 |
| dataviews | `https://schemas.developer.epicor.com/schemas/v4.0/dataviews.json` | draft-07 | 2,862 |
| patch | `https://schemas.developer.epicor.com/schemas/v2.0/patch.json` | draft-07 | 1,693 |
| tools | `https://schemas.developer.epicor.com/schemas/v2.0/tools.json` | draft-07 | 1,680 |
| search | `https://schemas.developer.epicor.com/schemas/v4.0/search.json` | draft-07 | 1,206 |

要点：
- `search.json` 中的 component 是 `{sourceTypeId, model}` 嵌套式，与 layout 实际文件形态不同——注意区分
- **events v5.0 完整 schema 同时内嵌在 DLL 里**：`Epicor.MetaFX.Core.Events.EventFiles.events.v5.0.json`（反编译产物之一）
- 历史版本代差（v2.1/v3.2/v1.2）在少量文件中散落存在

---

## 6. Oracle 自验证实测结论（链 B）

实验载体：`MCPMinimalLayer`（已删除），组件树 `panel-card → div → text-box`。

| 实验 | 结果 | 结论 |
|---|---|---|
| 提交含未知字段（`metafx-div` 带 `columns:1`，C# 类无此字段） | 存储往返接受 | 存储层对 layout JSON **零 schema 校验**，未知字段静默保留 |
| 提交自定义 sourceTypeId | 不报错 | 运行时**宽容降级**（ModelConverter 退化机制证实） |
| SaveAsDraft / Publish 往返 diff | 可读回 | 自验证闭环的 feedback 通道成立 |

**自验证闭环安全性证明**：试错不会破坏系统（宽容降级 + 零校验），且每次往返都能拿到权威反馈（diff / 渲染结果 / 运行时行为），闭环收敛条件为"往返 diff = 0"。

**方法论对照（回应"自验证"原始问题）**：
1. **假设生成**：从语料（链 A）与反编译（链 C）提出 schema 假设
2. **Oracle 执行**：UpdateKineticLayer 提交假设性 layout
3. **反馈**：读回 diff + App Studio 加载 + 运行时渲染
4. **修正/收敛**：diff = 0 即假设被证实
该流程即 LLM Self-Refine / Self-Verification 文献中的 generate-feedback-refine 循环；反编译（链 C）的存在把大部分"试"变成"读"，自验证只用于验证反编译覆盖不到的前端行为。

---

## 7. 三源对齐矩阵与 presence 验证（v2.0 新增）

### 7.1 方法与产物

三个独立来源交叉对齐，脚本化产出机器可读矩阵：

| 源 | 内容 | 提取方式 |
|---|---|---|
| S1：DLL 模型 | 31 个 `[ComponentModel]` 注册类型 + 属性（§2） | ilspycmd 反编译 → 逐类解析（`align-three-sources.ps1` Step 1-2） |
| S2：Designer 属性面板 | main.be1d1e62c94b7406.js 中 65 个组件的 propertyChain groups | `extract-designer-schema.ps1` → `docs/designer-property-schema.json` |
| S3：生产语料 | `Ice.XXXDef`（KNTCMetaUI）25,194 文件 | SQL presence 计数（`"sourceTypeId":"<id>"` 精确匹配，10 批 × 8 LIKE） |

S2 两个关键修正：
- **override 继承**：`erp-*` 组件通过 `"override": "ep-*"` 继承属性面板，需递归合并（own wins），否则 erp 系列误判为"无面板属性"
- **混淆基类**：main.js 尾部基类（g5）无 groups 元数据，无法用链尾比较 → 改用**属性频次法**：在 ≥10/65 组件中出现的属性视为公共属性

产物：`.tools/align-three-sources.ps1` → `docs/three-source-alignment.json`（类型级 + 属性级 + presence 三段）。

### 7.2 类型级对齐矩阵

| 交集 | 数量 | 明细 |
|---|---|---|
| DLL ∩ Designer | **19** | 12 erp-*（button/check-box/combo-box/date-picker/numeric-box/picture-box/print-output-format/radio-button/text-area/text-box/token-date/website）+ 4 metafx-*（box/div/panel-card/panel-card-grid）+ 3 ep-*（label/row-navigator/search-chip-selection） |
| 仅 DLL | 1 | `ep-embedded-app` |
| 仅 Designer | 46 | 16 ep-* + 22 erp-* + 6 metafx-*（col/flex-box/matrix/panel-card-gantt/relationship-map/tabstrip）+ 2 page-*（details/diagram） |
| 仅 DLL（Configurator） | 11 | `cfg-*`（另一设计器命名空间，与 App Studio 语料天然隔离） |

### 7.3 presence 全量验证（77 类型 × 25,194 文件）

Top 10 生产使用（含 DLL 与 Designer 双重来源标注）：

| # | sourceTypeId | 文件数 | 来源 |
|---|---|---|---|
| 1 | `erp-text-box` | 1,365 | both |
| 2 | `erp-combo-box` | 688 | both |
| 3 | `metafx-col` | 635 | designerOnly |
| 4 | `metafx-div` | 635 | both |
| 5 | `metafx-panel-card` | 626 | both |
| 6 | `erp-check-box` | 512 | both |
| 7 | `metafx-panel-card-grid` | 489 | both |
| 8 | `metafx-tabstrip` | 415 | designerOnly |
| 9 | `erp-numeric-box` | 390 | both |
| 10 | `erp-date-picker` | 333 | both |

关键结果：

| 集合 | 非零 | 零 | 结论 |
|---|---|---|---|
| DLL∩Designer（both 19） | **19** | **0** | 三源交集 100% 被语料证实——"黄金核心" |
| 仅 DLL（20） | 19 | 1（`ep-embedded-app`） | 唯一真孤儿 |
| 仅 Designer（46） | 29 | 17 | Designer 超前于 DLL：29 个由 ModelConverter 退化机制承接、前端消费 |
| cfg-*（11） | 0 | 11 | 属预期：Configurator 语料在另一 TypeCode 空间 |

designerOnly 中 17 个零使用清单：`ep-button`、`ep-check-box`、`ep-combo-box`、`ep-date-picker`、`ep-multi-level-menu`、`ep-multi-view-calendar`、`ep-numeric-box`、`ep-radio-button`、`ep-selection-list`、`ep-text-area`、`ep-text-box`、`ep-time-picker`（12 个 ep-* 基础控件，生产一律用 `erp-*` 增强版）、`erp-grow-widget`、`erp-list-box`、`metafx-matrix`、`page-details`、`page-diagram`。

观察：
- `metafx-col` 与 `metafx-div` 计数恒等（635=635）——col 是 div 的行/列子容器约定，同现出现
- `ep-*` 基础控件面板仍保留在 Designer 中，属**面向未来的面板定义**，当前生产未见使用
- 语料较 v1.0 增长 24,960 → 25,194（生产持续变更），presence 数字应视为时点快照

### 7.4 属性级对齐与基类频次校验

对 both 交集的 19 个组件做属性级三方比对（Designer 侧合并 override 继承后）：

| 属性集合 | 数量 | 说明 |
|---|---|---|
| shared（DLL ∩ Designer） | 147 | 服务端强类型且 Designer 可编辑——schema 校验的第一优先级 |
| dllOnly | 311 | 含基类布局属性（class/flexBasis/guid/height/top/left…）与嵌套模型（columns/providerModel/gridModel…）；Designer 不直接暴露 → 服务端权威 |
| designerOnly | 166 | 前端消费属性，服务端零校验原样保留（与 §6 oracle 实测一致） |

**基类频次法交叉验证**（阈值 ≥10/65 组件）：Designer 公共属性 18 个，与 DLL `ComponentModel` 基类 19 个对照——

| 类别 | 属性（出现次数） |
|---|---|
| 双源 shared（9） | `epBinding`(38) `readonly`(37) `hidden`(37) `disabled`(37) `id`(34) `labelText`(33) `isKeyField`(32) `alignment`(29) `width`(28) |
| 仅 DLL（10） | `class` `flexBasis` `guid` `tabIndex` `minHeight` `disabledToolBar` `epiLabel` `height` `top` `left` —— 布局/技术属性，Designer 面板不直接编辑 |
| 仅 Designer（9） | `comments`(33) `customizable`(32) `personalizable`(32) `autoFocus`(27) `prismEnabled/ContextLabel/Capable/AllowIfDisabled`(各19) `isShortControl`(12) —— 与 §2.5 语料归纳的 `personalizable`/`customizable` **吻合**（三源第二次互证） |

### 7.5 对 schema 重建的推论

1. **权威属性集** = DLL 类型骨架 ∪（override 合并后的）Designer 属性组；`shared` 147 个是校验收益最高区间。
2. **MVP 排序依据** = presence 计数：both 19 全部非零、designerOnly 29 非零优先覆盖（合计覆盖生产文件的绝大多数组件实例）。
3. **零使用 ≠ 无效**：17 个 designerOnly 零使用与 `ep-embedded-app` 仍保留在矩阵中，标注 `presence=0`，为升级/迁移场景兜底。
4. 机器可读矩阵（`docs/three-source-alignment.json`）可直接作为 JSON Schema 生成器的输入（对应 §8 遗留问题 4）。

---

## 8. 遗留问题

1. ~~**前端专属 3 类型**（tabstrip / gantt / udc）的完整属性需语料归纳~~ → **已完成**（见 §2.5：tabstrip 1,121 文件、gantt 5 文件归纳完毕；udc 本环境 0 实例，机制已由 FlattenLayout 证实；并新发现 `erp-shape` 546 文件）
2. **`metafx-div` 的 `columns:1`**：被存储接受但 C# 类无此字段——推论为被 ModelConverter.Populate 静默忽略，待运行时验证其是否有渲染效果
3. **少量次要公开 schema 未拉取**：`v3.0/strings.json`、`v1/Strings.json`、`v1/combo.json`、`v2.0/properties.json`
4. ~~**可选工具化**：将本报告组件表转为 JSON Schema，沉淀为 `ValidateKineticContent` MCP 离线校验工具~~ → **前置已完成**（§7 三源对齐矩阵即生成器输入），下一步为生成器实现
5. **designerOnly 29 个非零组件的属性全集**：Designer 面板已给出可编辑属性，但个别组件（如 `metafx-tabstrip` 的 `data[]`、`erp-qty-uom` 的绑定面）仍需按 §2.5 方法做语料字段并集补充
6. **cfg-* 11 个类型**需在 Configurator 自身语料空间（非 KNTCMetaUI）另行 presence 验证

---

## 附录 A：反编译产物索引

- 全量：`.decomp/MetaFXCore/`（475 个 .cs，ilspycmd 11.0.0.9375）
- 核心模型：`.decomp/MetaFXCore/Epicor.MetaFX.Core.Models.Layout/`（Component/ComponentModel/ComponentBoxData/ModelConverter/ComponentModelAttribute）
- 控件：`.../Epicor.MetaFX.Core.Models.Layout.ErpControls/`（33 个文件）、`.../EpControls/`（9 个文件）、`.../Configurator/`
- 拍平算法：`.decomp/MetaFXCore/Epicor.MetaFX.Core.Helper/FlattenLayout.cs`
- 文件信封：`.decomp/MetaFXCore/Epicor.MetaFX.Core.AppStudio.Metadata/`（LayoutMetadata/PageMetadata/BaseMetadataType）
- 内嵌 schema：`Epicor.MetaFX.Core.Events.EventFiles.events.v5.0.json`
- 交叉验证样本 1：SQL `KNTCMetaUI` 中抽取的真实 `metafx-panel-card-grid` 文件（字段 100% 吻合：`parentId:null` 根节点、`model` 嵌套、`gridModel.providerModel.baqOptions`、小写 `visibleIndex`）
- 交叉验证样本 2：SQL 抽取的 `metafx-tabstrip`（Erp.UI.AbcCodeEntry / Csfcn.UI.DigitalEInvoiceExport 等）与 `metafx-panel-card-gantt`（Erp.UI.JobSchedulingBoardEntry 等 4 个调度板 App），归纳结果见 §2.5

## 附录 B：v2.0 三源对齐产物索引

| 产物 | 说明 |
|---|---|
| `.tools/extract-designer-schema.ps1` | main.js 属性面板提取脚本 → 65 组件 |
| `docs/designer-property-schema.json` | Designer 属性面板机器可读 schema（含 override 继承关系） |
| `.tools/align-three-sources.ps1` | 三源对齐脚本（7 步：解析 → 扁平化 → override 合并 → id 对齐 → 属性级比对 + 频次法基类校验 → 输出） |
| `docs/three-source-alignment.json` | 对齐矩阵主产物：summary / baseClassCheck / propertyLevel（both 19 逐组件）/ designerOnlyInventory / dllTypes / presence |
| `.tools/presence-counts.json` | 77 类型 × 25,194 文件 presence 计数明细（10 批 SQL，精确口径） |
| 本报告 §7 | 矩阵的结论层：黄金核心 19、Designer 超前集 29、孤儿 1、cfg 隔离 11 |
