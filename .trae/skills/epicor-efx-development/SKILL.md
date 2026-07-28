---
name: "epicor-efx-development"
description: "Epicor Kinetic EFX Function 开发与测试全周期工作流。当用户需要创建、修改、调试、测试 EFX Function 时触发。强制执行 Demote → 修改 → Publish 的正确顺序，防止遗漏关键步骤导致操作失败。"
---

# Epicor EFX Function 开发与测试

调用 `mcp_mcp_epicor-mcp-bridge` 工具链。

## 必须遵守的顺序

### 开发修改

```
DemoteEfxLibrary（先降级已发布库）
    ↓
CreateEfxFunction / PushEfxFunction / UpdateEfxFunctionSignature / DeleteEfxFunction / AddEfxReference（修改操作）
    ↓
PublishEfxLibrary（发布使修改生效）
    ↓
CallEfxFunction（验证，只能调用已发布库中的 Function）
```

**任何跳过 Demote 或 Publish 的操作都会失败。**

### 测试他人已发布的 Function

```
GetEfxFunctionSignature（了解参数） → CallEfxFunction（调用测试）
```

无需 Demote/Publish，只需理解签名后直接调用。

---

## 工具分工

| 阶段 | 工具 | 用途 |
|------|------|------|
| 浏览 | `ListEfxLibraries` / `GetLibraryDetail` / `ListEfxFunctions` | 查看 Library 和 Function 列表 |
| 发现 | `ListEfxReferenceCandidates` | 列出可添加的引用候选（Assembly/Table/Service/Library） |
| 查看 | `GetEfxFunctionDetail` | 查看 Function 完整源码 |
| 查看 | `GetEfxFunctionSignature` | 查看输入/输出参数签名（调用前必查） |
| 降级 | `DemoteEfxLibrary` | 发布库 → 开发环境（修改前的必做步骤） |
| 创建 | `CreateEfxFunction` | 创建新 Function |
| 更新 | `PushEfxFunction` | 更新 Function 代码体（不影响签名） |
| 更新 | `UpdateEfxFunctionSignature` | 修改 Function 输入/输出参数签名 |
| 添加 | `AddEfxReference` | 添加 Assembly/Service/Table/Library 引用 |
| 删除 | `DeleteEfxFunction` | 删除 Function |
| 发布 | `PublishEfxLibrary` | 发布库，使所有修改在 Epicor 中生效 |
| 测试 | `CallEfxFunction` | 调用已发布 Function |

---

## 场景一：创建新 Function

```
1. GetEfxFunctionSignature(company, libraryId, "已有Function")   ← 参考已有 Function 的签名格式
2. DemoteEfxLibrary(company, libraryId)                           ← 降级
3. CreateEfxFunction(company, libraryId, "NewFunc", "描述",
     csharpCode,
     signaturesJson)
4. PublishEfxLibrary(company, libraryId)                          ← 发布
5. CallEfxFunction(company, libraryId, "NewFunc", payload)        ← 验证
```

### csharpCode 写法

- 无需 `using` 语句，系统自动分离
- 输入参数用 `this.参数名` 读取，输出用 `this.输出参数名 = 值` 赋值

单参数示例：
```csharp
var result = $"Hello: {this.inputName}";
this.outputResult = result;
```

多参数示例：
```csharp
int sum = int.Parse(this.inputA) + int.Parse(this.inputB);
this.outputSum = sum.ToString();
```

### signaturesJson 格式

**是 JSON 数组（不是对象！）**，参考 `GetEfxFunctionSignature` 返回结果：
- `Response: false` = 输入参数，`Response: true` = 输出参数
- `ParameterID` 自增，`Order` 从 1 开始

示例（两输入、一输出）：
```json
[{"Response":false,"ParameterID":1,"ArgumentName":"inputA","Order":1,"DataType":"System.String","DataTypeInfo":null},
 {"Response":false,"ParameterID":2,"ArgumentName":"inputB","Order":2,"DataType":"System.String","DataTypeInfo":null},
 {"Response":true,"ParameterID":1,"ArgumentName":"outputSum","Order":1,"DataType":"System.String","DataTypeInfo":null}]
```

---

## 场景二：修改已有 Function 代码

```
1. DemoteEfxLibrary(company, libraryId)
2. GetEfxFunctionDetail(company, libraryId, functionId)           ← 查看当前源码
3. PushEfxFunction(company, libraryId, functionId, newCode)       ← 推送新代码
4. PublishEfxLibrary(company, libraryId)
5. CallEfxFunction(company, libraryId, functionId, payload)       ← 验证
```

---

## 场景三：测试他人开发的已发布 Function

当同事或项目中已有 Function，想了解其用法并测试时：

```
1. ListEfxLibraries(company)                                      ← 查找目标 Library
2. GetLibraryDetail(company, libraryId)                           ← 查看库中有哪些 Function
3. GetEfxFunctionSignature(company, libraryId, functionId)        ← 了解输入输出参数
4. CallEfxFunction(company, libraryId, functionId, payload)       ← 调用测试
```

关键点：
- 参数名必须与 Signature 返回的 `ArgumentName` **完全一致**
- `Response: false` 是输入参数，传入 jsonPayload；`Response: true` 是输出参数
- 参数名不一致会导致函数内参数为 null，抛出 `NullReferenceException`

示例：
```
GetEfxFunctionSignature("GPEHZ", "TestLibForAchen", "SpotBuyVQNote")
→ 输入: VQID (string), 输出: InfoFromSpotBuyVQNote (string)

CallEfxFunction("GPEHZ", "TestLibForAchen", "SpotBuyVQNote", {"VQID": "TEST-VQ-001"})
→ {"InfoFromSpotBuyVQNote": "..."}
```

---

## 场景四：删除 Function

```
1. DemoteEfxLibrary(company, libraryId)
2. DeleteEfxFunction(company, libraryId, functionId)
3. PublishEfxLibrary(company, libraryId)
```

---

## 场景五：添加引用

```
1. ListEfxReferenceCandidates(company, type)                       ← 先查看有哪些可用引用
   type="Assembly" → DLL 文件名, "Table" → Erp.XXX 表名, "Service" → BO Service, "Library" → EFX 库
2. DemoteEfxLibrary(company, libraryId)
3. AddEfxReference(company, libraryId, type, referenceId)
   type 仅可取: Assembly / Service / Table / Library
   referenceId 示例:
     Assembly → "Erp.Contracts.BO.Part.dll"
     Service  → "Erp.BO.PartSvc"
     Table    → "Erp.Part"
     Library  → "WorkflowGP"
4. PublishEfxLibrary(company, libraryId)
```

---

## 场景六：修改已有 Function 签名

当需要调整 Function 的输入参数或输出参数时（如新增参数、删除参数、改名、改类型）：

```
1. DemoteEfxLibrary(company, libraryId)
2. GetEfxFunctionSignature(company, libraryId, functionId)          ← 查看当前签名作为基线
3. UpdateEfxFunctionSignature(company, libraryId, functionId,
     newSignaturesJson)                                             ← 完全替换旧签名
4. PublishEfxLibrary(company, libraryId)
5. CallEfxFunction(company, libraryId, functionId, payload)         ← 验证新签名生效
```

**注意：**
- `signaturesJson` 会**完全替换**旧签名，不是追加合并
- 签名修改后必须同步修改 `PushEfxFunction` 的代码体，否则编译报错
- 如果只为已有参数改名，代码体中 `this.旧参数名` 引用会自动失效

---

## 常见错误

| 错误信息 | 原因 | 处理 |
|---------|------|------|
| 修改 Published 库失败 | 库处于发布状态 | 先 `DemoteEfxLibrary` |
| `CallEfxFunction` NotFound | Function 未发布 | 先 `PublishEfxLibrary` |
| `Function 'xxx' already exists` | 重复创建同名 Function | 换 functionId 或先删除旧的 |
| 函数内 `NullReferenceException` | 参数名与 Signature 不一致 | 检查 jsonPayload 参数名是否与 ArgumentName 完全匹配 |
| `Value cannot be null` | 代码中 int.Parse(null) | 同上，参数名错误导致未传入 |
| `Invalid reference type` | type 参数值不合法 | 只能取 Assembly/Service/Table/Library |
