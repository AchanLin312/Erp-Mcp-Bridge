using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var pipe = @"D:\MCP\Erp-Mcp-Bridge\.tools\pipe";

// ---- 1. Build contentJson from TEST-a readback, fix layout identity ----
var raw = File.ReadAllText(Path.Combine(pipe, "test_a_v4_expanded.json"));
var root = JObject.Parse(raw);
var ro = (JObject)root["returnObj"]!;

var content = new JObject
{
    ["layout"] = ro["Layout"]!.DeepClone(),
    ["pages"] = ro["Pages"]!.DeepClone(),
    ["events"] = ro["Events"]!.DeepClone(),
    ["tools"] = ro["ToolBar"]!.DeepClone(),
    ["dataviews"] = ro["DataViews"]!.DeepClone(),
    ["rules"] = ro["Rules"]!.DeepClone(),
};
content["layout"]!["name"] = "TEST-a";
content["layout"]!["namespace"] = "Ice.UIDbd.TEST-a";
var cj = content.ToString(Formatting.None);
File.WriteAllText(Path.Combine(pipe, "transfer_content_v4_fixed.json"), cj, new UTF8Encoding(false));
Console.WriteLine($"contentJson chars: {cj.Length}");
Console.WriteLine($"layout.name={content["layout"]!["name"]} namespace={content["layout"]!["namespace"]}");

// ---- 2. MCP HTTP flow ----
using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(600) };
client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/json, text/event-stream");
const string baseUrl = "http://localhost:5000/";

async Task<(int status, string sid, string body)> PostMcpAsync(string body)
{
    using var c = new StringContent(body, Encoding.UTF8, "application/json");
    using var resp = await client.PostAsync(baseUrl, c);
    var txt = await resp.Content.ReadAsStringAsync();
    var sid = resp.Headers.Contains("mcp-session-id") ? resp.Headers.GetValues("mcp-session-id").First() : "";
    return ((int)resp.StatusCode, sid, txt);
}

var (s1, sid, b1) = await PostMcpAsync("{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"initialize\",\"params\":{\"protocolVersion\":\"2025-03-26\",\"capabilities\":{},\"clientInfo\":{\"name\":\"mjtool\",\"version\":\"1.1\"}}}");
if (s1 != 200 || sid.Length == 0) { Console.WriteLine($"initialize failed: {s1} {Trunc(b1, 500)}"); return 1; }
client.DefaultRequestHeaders.TryAddWithoutValidation("Mcp-Session-Id", sid);
Console.WriteLine($"MCP session: {sid}");
await PostMcpAsync("{\"jsonrpc\":\"2.0\",\"method\":\"notifications/initialized\"}");

var toolArgs = new JObject
{
    ["company"] = "GPEHZ",
    ["viewId"] = "Ice.UIDbd.TEST-a",
    ["layerName"] = "TransferPrice V4",
    ["typeCode"] = "KNTCCustLayer",
    ["contentJson"] = cj,
    ["comment"] = "Repoint layout name/namespace to TEST-a base",
    ["allowUnknownComponents"] = true,
};
var rpc = new JObject
{
    ["jsonrpc"] = "2.0",
    ["id"] = 2,
    ["method"] = "tools/call",
    ["params"] = new JObject { ["name"] = "UpdateKineticLayer", ["arguments"] = toolArgs },
};
var (s2, _, b2) = await PostMcpAsync(rpc.ToString(Formatting.None));
if (s2 != 200) { Console.WriteLine($"HTTP {s2}: {Trunc(b2, 1500)}"); return 1; }
var data = string.Join("", b2.Split('\n').Where(l => l.StartsWith("data:")).Select(l => l.Substring(5).Trim()));
var jr = JObject.Parse(data);
if (jr["error"] != null) { Console.WriteLine("RPC ERROR: " + jr["error"]!.ToString(Formatting.None)); return 1; }
var isError = jr["result"]?["isError"]?.Value<bool>() ?? false;
var resultText = jr["result"]?["content"]?[0]?["text"]?.ToString() ?? "(no text)";
Console.WriteLine($"isError: {isError}");
Console.WriteLine("TOOL RESULT: " + Trunc(resultText, 3000));
Console.WriteLine("MJTOOL_DONE");
return 0;

static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n);
