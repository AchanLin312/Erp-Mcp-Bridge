# Erp-Mcp-Bridge

基于 MCP 协议的 Epicor Kinetic ERP 桥接工具，提供 **17 个工具**，让 AI 助手直接查询和操作 Epicor 系统。

## 工具列表

### 元数据查询
| 工具 | 说明 |
|------|------|
| `ListEpicorServices` | 列出所有可用的 BO Service |
| `GetEpicorEnvironment` | 获取环境信息（公司列表及工厂） |
| `ListServiceMethods` | 获取指定 Service 的所有方法 |
| `GetMethodSchema` | 获取 BO 方法的输入/输出参数格式 |

### 业务执行
| 工具 | 说明 |
|------|------|
| `CallBoMethod` | 调用 BO 方法（GetByID、GetList、Update 等） |
| `CallEfxFunction` | 调用已发布的 EFX Function |

### EFX 查询
| 工具 | 说明 |
|------|------|
| `ListEfxLibraries` | 列出公司下所有 EFX Library |
| `GetLibraryDetail` | Library 详情及 Function 列表 |
| `ListEfxFunctions` | Library 中所有 Function（简要） |
| `GetEfxFunctionDetail` | Function 完整源码 |
| `GetEfxFunctionSignature` | Function 参数签名（输入/输出） |

### EFX 管理
| 工具 | 说明 |
|------|------|
| `DemoteEfxLibrary` | 将库从发布环境降级到开发环境 |
| `CreateEfxFunction` | 在库中创建新 Function |
| `PushEfxFunction` | 更新已有 Function 的代码体 |
| `UpdateEfxFunctionSignature` | 修改已有 Function 的输入/输出参数签名 |
| `ListEfxReferenceCandidates` | 列出可添加的引用候选（Assembly/Table/Service/Library） |
| `AddEfxReference` | 添加引用（Assembly/Service/Table/Library） |
| `DeleteEfxFunction` | 删除 Function |
| `PublishEfxLibrary` | 发布库，使修改生效 |

## 快速开始

1. 安装 .NET 10.0 SDK
2. `cp appsettings.example.json appsettings.json` 并填入 Epicor 连接信息
3. 发布并启动：

```bash
dotnet publish -c Release -o ./publish
```

4. 选择传输模式：

**stdio 模式**（单机）：

```bash
./publish/ErpMcpBridge.exe --transport stdio
```

**HTTP/SSE 模式**（团队共享）：

```bash
./publish/ErpMcpBridge.exe --transport http --port 5000
```

服务启动后监听 `http://localhost:5000`，Trae 中 URL 填 `http://localhost:5000/sse`。

> `appsettings.json` 中的 `MCPServer.Transport` 会被 `--transport` 参数覆盖，`HttpPort` 会被 `--port` 覆盖。不传参数时使用配置文件的值。

## MCP 配置

在 Trae 或其他 MCP 客户端中添加以下配置：

### stdio 模式

```json
{
  "mcpServers": {
    "mcp_epicor-mcp-bridge": {
      "command": "D:\\MCP\\Erp-Mcp-Bridge\\publish\\ErpMcpBridge.exe",
      "args": ["--transport", "stdio"],
      "env": {}
    }
  }
}
```

### HTTP/SSE 模式

先启动服务：`./publish/ErpMcpBridge.exe --transport http`，然后：

```json
{
  "mcpServers": {
    "mcp_mcp_epicor-mcp-bridge": {
      "url": "http://localhost:5000/sse"
    }
  }
}
```

## 典型工作流

### 查询 BO 数据

```
ListEpicorServices → ListServiceMethods → GetMethodSchema → CallBoMethod
```

大表 Service（如 JobEntrySvc）必须用 `GetList` 分页，禁止直接 `GetByID`。详见 Skill `epicor-bo-query`。

### 开发 EFX Function

```
DemoteEfxLibrary → 修改操作 → PublishEfxLibrary → CallEfxFunction
```

任何修改前必须先 Demote，任何修改后必须 Publish 才能生效。详见 Skill `epicor-efx-development`。

### 测试他人已发布的 Function

```
GetEfxFunctionSignature → CallEfxFunction
```

无需 Demote/Publish，签名了解后直接调用即可。

## 项目结构

```
Erp-Mcp-Bridge/
├── Program.cs                     # 启动入口
├── MCP/Tools/                     # MCP 工具定义
│   ├── BoMetadataTools.cs         # 元数据查询
│   ├── ExecutionTools.cs          # 业务执行
│   └── EfxManagementTools.cs      # EFX 管理
├── Services/                      # 业务逻辑层
│   ├── BoMetadataService.cs       # Swagger 解析
│   ├── EpicorExecutionService.cs  # BO/EFX 调用
│   └── EfxManagementService.cs    # EFX CRUD
├── Models/EpicorConfig.cs
├── Utils/SwaggerParser.cs         # Swagger JSON 解析器
└── .trae/skills/                  # 工作流 Skill
    ├── epicor-bo-query/           # BO 查询工作流
    └── epicor-efx-development/    # EFX 开发工作流
```

## 架构

`Trae AI → MCP Protocol (stdio / HTTP-SSE) → Tools → Services → EpicorRestAPI → Epicor REST API`

- **BoMetadataTools** — Swagger 解析 BO Service 元数据
- **ExecutionTools** — 调用 BO 方法和 EFX Function
- **EfxManagementTools** — EFX Function 完整 CRUD（TrackChanges 模式）

## 技术栈

- [ModelContextProtocol](https://www.nuget.org/packages/ModelContextProtocol) 2.0.0-preview.3
- [EpicorRestAPI](https://www.nuget.org/packages/EpicorRestAPI) 3.0.2
- .NET 10.0 · Newtonsoft.Json
