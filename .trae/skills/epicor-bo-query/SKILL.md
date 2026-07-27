---
name: "epicor-bo-query"
description: "Epicor Kinetic BO 数据查询工作流。当用户需要查询 Epicor 业务数据、调用 BO 方法时触发。强制先 GetMethodSchema 再 CallBoMethod，防止跳过 Schema 直接调用大表方法导致超时。"
---

# Epicor BO 数据查询

调用 `mcp_mcp_epicor-mcp-bridge` 工具链。

## 核心规则

1. **任何 BO 方法调用前必须先 `GetMethodSchema`**，禁止跳过。
2. **大表 Service（如 JobEntrySvc，OutputTables 有 20+ 个 Table）必须用 `GetList` 分页，禁止直接 `GetByID`。**
3. Company 从 `GetEpicorEnvironment` 返回的可用公司中选择。
4. Service 名称因环境而异，若 `NotFound` 用 `ListEpicorServices` 查找。

## 标准流程

### 1. 发现 Service

不知道 Service 名时用 `ListEpicorServices` 搜索。常见 Service 包括：

| Service | 用途 |
|---------|------|
| `Erp.BO.PartSvc` | 物料 |
| `Erp.BO.CustomerSvc` | 客户 |
| `Erp.BO.SalesOrderSvc` | 销售订单 |
| `Erp.BO.VendorSvc` | 供应商 |
| `Erp.BO.APInvoiceSvc` | 应付发票 |
| `Erp.BO.PartClassSvc` | 物料类别 |
| `Erp.BO.JobEntrySvc` | 工单（大表，仅用 GetList） |

### 2. 分析 Schema

```
GetMethodSchema(serviceName, methodName)
```

关注返回结果：
- `InputParameters` — 必传参数及其类型，`Required: true` 的不可省略
- `OutputTables` — 返回的数据表及字段。**Table 越多数据量越大**，是判断该用 GetByID 还是 GetList 的关键依据

### 3. 调用

**GetByID — 单条查询（适用于 OutputTables 少的 Service）**：

```
CallBoMethod(company, serviceName, "GetByID", {"keyField": "value"})
```

```json
// 查询物料
{"partNum": "1403-7390+2"}
// 查询客户（CustNum 来自 GetList 返回）
{"custNum": 7}
// 查询订单
{"orderNum": 100001}
```

**GetList — 列表查询（适用于大表或需要过滤/分页的场景）**：

```
CallBoMethod(company, serviceName, "GetList", {
  "whereClause": "OData 过滤条件",
  "pageSize": 10,
  "absolutePage": 0
})
```

三个参数**缺一不可**：
- `whereClause` — 过滤条件，格式如 `"Field like '%value%'"`、`"Field = value"`
- `pageSize` — 每页记录数（建议 3-10）
- `absolutePage` — 页码从 0 开始

不同 Service 的 GetList 示例：

```json
// 物料（PartSvc）
{"whereClause": "PartNum like '1403%'", "pageSize": 10, "absolutePage": 0}

// 工单（JobEntrySvc — 大表，必须分页）
{"whereClause": "JobClosed = false", "pageSize": 3, "absolutePage": 0}

// 销售订单（SalesOrderSvc）
{"whereClause": "OpenOrder = true", "pageSize": 5, "absolutePage": 0}

// 客户（CustomerSvc）
{"whereClause": "Name like 'G%'", "pageSize": 5, "absolutePage": 0}

// 供应商（VendorSvc）
{"whereClause": "Name like '%TECH%'", "pageSize": 5, "absolutePage": 0}

// 应付发票（APInvoiceSvc）
{"whereClause": "OpenPayable = true", "pageSize": 5, "absolutePage": 0}

// 物料类别（PartClassSvc）
{"whereClause": "ClassID like 'M%'", "pageSize": 5, "absolutePage": 0}
```

返回中 `parameters.morePages: true` 表示还有更多数据，递增 `absolutePage` 翻页。

## 常见错误

| 错误信息 | 原因 | 处理 |
|---------|------|------|
| `NotFound` | Service 名不存在 | 用 `ListEpicorServices` 确认正确名称 |
| `Record not found` | 无匹配数据 | 放宽 whereClause 条件 |
| 参数缺失报错 | 缺少必填参数 | 对照 `GetMethodSchema` 的 InputParameters |
| `Company is not configured` | 公司代码无效 | 用 `GetEpicorEnvironment` 确认有效公司 |
