using Newtonsoft.Json.Linq;

namespace ErpMcpBridge.Utils;

/// <summary>
/// Swagger $ref 递归解析器
/// 从 Epicor 的 OpenAPI 3.0.1 OData swagger.json 中提取方法的参数 Schema，
/// 递归展开 $ref 引用并只保留核心业务字段。
/// 
/// Epicor OData Swagger 结构：
///   - paths: /{currentCompany}/Erp.BO.PartSvc/Parts  (OData CRUD)
///   - components/schemas: Erp.Part, Erp.PartSetResponse, ...
///   - components/parameters: currentCompany, X-API-Key, etc.
///   - $ref 路径: #/components/schemas/Erp.Part 或 #/components/parameters/currentCompany
/// </summary>
public class SwaggerParser
{
    /// <summary>
    /// 从 OpenAPI 3.0.1 Swagger JSON 中提取指定方法的 Schema
    /// </summary>
    public MethodSchema ExtractSchema(JObject swaggerDoc, JToken methodToken, string httpMethod)
    {
        var methodObj = (JObject)methodToken;
        var schema = new MethodSchema();

        // 1. 提取输入参数
        ExtractInputParameters(swaggerDoc, methodObj, schema);

        // 2. 提取输出 Schema
        ExtractOutputSchema(swaggerDoc, methodObj, schema);

        return schema;
    }

    /// <summary>
    /// 提取输入参数
    /// </summary>
    private void ExtractInputParameters(JObject doc, JObject methodObj, MethodSchema schema)
    {
        // 提取 OData URL 路径参数（如 Company, PartNum）
        var parameters = methodObj["parameters"] as JArray;
        if (parameters != null)
        {
            foreach (var param in parameters)
            {
                var resolved = ResolveRef(doc, param);
                if (resolved is JObject paramObj)
                {
                    var name = (string?)paramObj["name"];
                    var description = (string?)paramObj["description"] ?? "";
                    var required = (bool?)paramObj["required"] ?? false;
                    var schema2 = paramObj["schema"] as JObject;
                    var type = schema2 != null
                        ? ((string?)schema2["type"] ?? (string?)schema2["format"] ?? "string")
                        : "string";
                    var paramIn = (string?)paramObj["in"];

                    // 跳过 API key header 和 OData 查询参数（$select, $filter 等）
                    if (name != null && paramIn != "header" && !name.StartsWith("$") && !name.StartsWith("op_"))
                    {
                        if (!schema.InputParameters.Any(p => p.Name == name))
                        {
                            schema.InputParameters.Add(new SchemaField
                            {
                                Name = name,
                                Type = type,
                                Description = description,
                                Required = required
                            });
                        }
                    }
                }
            }
        }

        // 提取 requestBody (POST/PATCH)
        var requestBody = methodObj["requestBody"] as JObject;
        if (requestBody != null)
        {
            var content = requestBody["content"] as JObject;
            var jsonContent = content?["application/json"] as JObject;
            var bodySchema = jsonContent?["schema"];
            if (bodySchema != null)
            {
                var resolved = ResolveRef(doc, bodySchema);
                if (resolved is JObject bodyObj)
                {
                    var props = bodyObj["properties"] as JObject;
                    if (props != null)
                    {
                        foreach (var prop in props.Properties())
                        {
                            var resolvedProp = ResolveRef(doc, prop.Value);
                            if (resolvedProp is JObject propObj)
                            {
                                var fieldType = (string?)propObj["type"] ?? "object";
                                var fieldDesc = (string?)propObj["description"] ?? "";

                                // 跳过元数据字段和非核心字段
                                if (IsCoreBusinessField(prop.Name))
                                {
                                    schema.InputParameters.Add(new SchemaField
                                    {
                                        Name = prop.Name,
                                        Type = fieldType,
                                        Description = fieldDesc,
                                        Required = IsRequiredField(bodyObj, prop.Name)
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// 提取输出 Schema（响应中的实体/集合结构）
    /// </summary>
    private void ExtractOutputSchema(JObject doc, JObject methodObj, MethodSchema schema)
    {
        var responses = methodObj["responses"] as JObject;
        if (responses == null) return;

        // 优先取 200，回退取 201
        var successResponse = responses["200"] as JObject ?? responses["201"] as JObject;
        if (successResponse == null) return;

        var content = successResponse["content"] as JObject;
        var jsonContent = content?["application/json"] as JObject;
        var responseSchema = jsonContent?["schema"];
        if (responseSchema == null) return;

        var resolved = ResolveRef(doc, responseSchema);
        if (!(resolved is JObject schemaObj)) return;

        // 检查是否为 SetResponse（OData 集合包装）
        var props = schemaObj["properties"] as JObject;
        if (props == null) return;

        // 检测 SetResponse 包装：有 "value" 属性且是数组 → 实体集合
        var valueProp = props["value"];
        if (valueProp != null)
        {
            var valueItems = ResolveRef(doc, valueProp);
            if (valueItems is JObject valueObj)
            {
                var items = valueObj["items"];
                if (items != null)
                {
                    var entitySchema = ResolveRef(doc, items);
                    if (entitySchema is JObject entityObj)
                    {
                        // 从实体 schema 中提取字段
                        var entityProps = entityObj["properties"] as JObject;
                        var tableSchema = new SchemaTable
                        {
                            TableName = GetEntityName(entityObj, doc),
                            Fields = entityProps != null
                                ? ExtractFieldsFromProperties(doc, entityProps)
                                : new List<SchemaField>()
                        };

                        // 从 SetResponse 内提取 @odata.context 推测实体名
                        if (string.IsNullOrEmpty(tableSchema.TableName) || tableSchema.TableName == "Entity")
                        {
                            var contextField = props["@odata.context"] as JObject;
                            if (contextField != null)
                                tableSchema.TableName = "ListItems";
                        }

                        schema.OutputTables.Add(tableSchema);
                        return;
                    }
                }
            }
        }

        // 非 SetResponse：直接实体或多表结构
        // 对于 OData swagger，通常是单个实体或嵌套实体
        foreach (var prop in props.Properties())
        {
            if (prop.Name.StartsWith("@odata")) continue; // 跳过元数据

            var resolvedProp = ResolveRef(doc, prop.Value);
            if (resolvedProp is JObject propObj)
            {
                var propType = (string?)propObj["type"];

                // 数组类型 → 子表
                if (propType == "array")
                {
                    var items = propObj["items"];
                    if (items != null)
                    {
                        var itemSchema = ResolveRef(doc, items);
                        if (itemSchema is JObject itemObj)
                        {
                            var itemProps = itemObj["properties"] as JObject;
                            schema.OutputTables.Add(new SchemaTable
                            {
                                TableName = prop.Name,
                                Fields = itemProps != null
                                    ? ExtractFieldsFromProperties(doc, itemProps)
                                    : new List<SchemaField>()
                            });
                        }
                    }
                }
                // 对象类型 → 单表或嵌套实体
                else if (propType == "object")
                {
                    var objProps = propObj["properties"] as JObject;
                    if (objProps != null)
                    {
                        schema.OutputTables.Add(new SchemaTable
                        {
                            TableName = prop.Name,
                            Fields = ExtractFieldsFromProperties(doc, objProps)
                        });
                    }
                }
                // 简单类型 → 输出字段
                else if (propType != null)
                {
                    if (IsCoreBusinessField(prop.Name))
                    {
                        schema.OutputFields.Add(new SchemaField
                        {
                            Name = prop.Name,
                            Type = propType,
                            Description = (string?)propObj["description"] ?? ""
                        });
                    }
                }
            }
        }

        // 如果 OutputTables 为空，将整个实体作为一张表
        if (schema.OutputTables.Count == 0 && schema.OutputFields.Count > 0)
        {
            schema.OutputTables.Add(new SchemaTable
            {
                TableName = "Result",
                Fields = schema.OutputFields
            });
            schema.OutputFields = new List<SchemaField>();
        }
    }

    /// <summary>
    /// 递归展开 $ref 引用
    /// Epicor OData 格式: "#/components/schemas/Erp.Part" 或 "#/components/parameters/currentCompany"
    /// </summary>
    private JToken ResolveRef(JObject doc, JToken token)
    {
        // 如果不是 $ref，直接返回
        if (token is JObject obj)
        {
            var refPath = (string?)obj["$ref"];
            if (refPath == null) return token;

            // 解析路径: #/components/schemas/Erp.Part → ["components","schemas","Erp.Part"]
            var parts = refPath.TrimStart('#').Split('/', StringSplitOptions.RemoveEmptyEntries);
            var current = (JToken)doc;
            foreach (var part in parts)
            {
                if (current is JObject curObj)
                    current = curObj[part] ?? new JObject();
                else
                    return token; // 无法解析，返回原始 token
            }

            // 解析到的 token 可能还包含 $ref（嵌套引用），递归解决
            return ResolveRef(doc, current);
        }

        // 如果是数组（如 items）
        if (token is JObject jObj2 && jObj2["items"] != null)
        {
            var items = jObj2["items"];
            if (items is JObject itemsObj && itemsObj["$ref"] != null)
            {
                var resolved = ResolveRef(doc, items);
                jObj2["items"] = resolved;
            }
        }

        return token;
    }

    /// <summary>
    /// 从属性集合中提取字段列表，递归展开 $ref
    /// </summary>
    private List<SchemaField> ExtractFieldsFromProperties(JObject doc, JObject properties)
    {
        var fields = new List<SchemaField>();
        foreach (var prop in properties.Properties())
        {
            if (!IsCoreBusinessField(prop.Name)) continue;

            var resolved = ResolveRef(doc, prop.Value);
            if (resolved is JObject propObj)
            {
                var fieldType = (string?)propObj["type"] ?? "string";
                var fieldFormat = (string?)propObj["format"];
                if (fieldFormat != null && fieldFormat != "uuid")
                    fieldType = $"{fieldType}({fieldFormat})";

                fields.Add(new SchemaField
                {
                    Name = prop.Name,
                    Type = fieldType,
                    Description = (string?)propObj["description"] ?? "",
                    Required = false
                });
            }
        }
        return fields;
    }

    /// <summary>
    /// 获取实体 Schema 的名称（从 ref 路径中推断）
    /// </summary>
    private string GetEntityName(JObject entityObj, JObject doc)
    {
        // 尝试从 properties 中找到标识性字段推断类型
        var props = entityObj["properties"] as JObject;
        if (props == null) return "Entity";

        // 某些 Epicor 实体有描述性字段组合
        if (props["PartNum"] != null) return "Part";
        if (props["OrderNum"] != null) return "OrderHed";
        if (props["InvoiceNum"] != null) return "Invoice";
        if (props["CustID"] != null) return "Customer";

        return "Entity";
    }

    /// <summary>
    /// 判断字段是否在 required 列表中
    /// </summary>
    private bool IsRequiredField(JObject schemaObj, string fieldName)
    {
        var required = schemaObj["required"] as JArray;
        if (required == null) return false;
        return required.Any(r => (string?)r == fieldName);
    }

    /// <summary>
    /// 判断字段是否为核心业务字段（非元数据字段）
    /// </summary>
    private bool IsCoreBusinessField(string fieldName)
    {
        var metaFields = new HashSet<string>
        {
            "SysRowID", "SysRevID", "RowMod", "BitFlag",
            "DBRowMod", "Company", "ForeignSysRowID",
            "RelatedToFile", "EnableGetByID", "CheckedBy",
            "CheckDate", "CheckTime"
        };
        return !metaFields.Contains(fieldName);
    }
}

/// <summary>
/// 方法参数 Schema（输入/输出）
/// </summary>
public class MethodSchema
{
    public List<SchemaField> InputParameters { get; set; } = new();
    public List<SchemaField> OutputFields { get; set; } = new();
    public List<SchemaTable> OutputTables { get; set; } = new();
}

public class SchemaField
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool Required { get; set; }
}

public class SchemaTable
{
    public string TableName { get; set; } = string.Empty;
    public List<SchemaField> Fields { get; set; } = new();
}
