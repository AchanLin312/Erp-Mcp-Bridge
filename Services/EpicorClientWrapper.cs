using EpicorRestAPI;
using ErpMcpBridge.Models;

namespace ErpMcpBridge.Services;

/// <summary>
/// EpicorRestAPI 的线程安全封装
/// BoPost / EfxPost 使用 EpicorRestAPI 原生方法（同步），通过 Task.Run 异步化并支持超时取消
/// GetEnvironment 有 HTTP 回退
/// </summary>
public class EpicorClientWrapper
{
    private readonly EpicorRest _epicor;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly int _timeoutSeconds;

    public EpicorClientWrapper(EpicorRest epicor, HttpClient httpClient, string baseUrl, EpicorConfig config)
    {
        _epicor = epicor;
        _httpClient = httpClient;
        _baseUrl = baseUrl.TrimEnd('/');
        _timeoutSeconds = config.CallSettings.TimeoutSeconds;
    }

    public async Task<EpicorResponseObject> BoPostAsync(string company, string service, string method, object data)
    {
        await _lock.WaitAsync();
        try
        {
            _epicor.Company = company;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            return await Task.Run(() => _epicor.BoPost(service, method, data), cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"BO 方法 {service}.{method} 调用超时（{_timeoutSeconds}s），响应数据量可能过大。");
        }
        finally { _lock.Release(); }
    }

    public async Task<EpicorResponseObject> EfxPostAsync(string company, string library, string function, object data)
    {
        await _lock.WaitAsync();
        try
        {
            _epicor.Company = company;
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(_timeoutSeconds));
            return await Task.Run(() => _epicor.EfxPost(library, function, data), cts.Token);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"EFX Function {library}.{function} 调用超时（{_timeoutSeconds}s）。");
        }
        finally { _lock.Release(); }
    }

    public async Task<string> GetEnvironmentAsync()
    {
        await _lock.WaitAsync();
        try
        {
            try
            {
                var env = await Task.Run(() => _epicor.GetEnvironment());
                return Newtonsoft.Json.JsonConvert.SerializeObject(env);
            }
            catch (Exception ex)
            {
                return await GetEnvironmentViaHttpAsync(ex.Message);
            }
        }
        finally { _lock.Release(); }
    }

    private async Task<string> GetEnvironmentViaHttpAsync(string fallbackReason)
    {
        var url = $"{_baseUrl}/api/v2/environment";
        var response = await _httpClient.GetAsync(url);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return $"EpicorRestAPI 调用失败: {fallbackReason}\nHTTP 回退也失败: {(int)response.StatusCode} {response.ReasonPhrase}: {Truncate(body)}";
        }

        return body;
    }

    private static string Truncate(string text, int maxLen = 500)
        => text.Length <= maxLen ? text : text[..maxLen] + "...";
}
