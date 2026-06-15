using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ChtotibDocsPrintNET.Services;

/// <summary>
/// Подключение к 1С через OData (HTTP). Точные имена сущностей зависят от конфигурации и публикации.
/// </summary>
public static class OneCImportService
{
    public static async Task<(bool Ok, string Message)> TestODataAsync(OneCSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.ODataBaseUrl))
            return (false, "Укажите URL OData (например http://server/base/odata/standard.odata/).");

        try
        {
            using var client = CreateClient(settings);
            var baseUrl = NormalizeBaseUrl(settings.ODataBaseUrl);
            var probe = baseUrl.TrimEnd('/') + "/$metadata";
            using var resp = await client.GetAsync(probe).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
                return (true, $"✓ OData доступен ({(int)resp.StatusCode}). Метаданные: $metadata");

            // Некоторые публикации отдают 401 без auth — считаем «сервер отвечает»
            if (resp.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                return (true, $"Сервер отвечает ({(int)resp.StatusCode}). Проверьте логин/пароль и права публикации.");

            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            return (false, $"Код {(int)resp.StatusCode}: {Truncate(body, 400)}");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<(bool Ok, string Message)> FetchEntitySampleAsync(OneCSettings settings, int top = 5)
    {
        if (string.IsNullOrWhiteSpace(settings.StudentsEntity))
            return (false, "Укажите имя сущности OData.");

        try
        {
            using var client = CreateClient(settings);
            var baseUrl = NormalizeBaseUrl(settings.ODataBaseUrl);
            var entity = settings.StudentsEntity.Trim().Trim('/');
            var url = $"{baseUrl.TrimEnd('/')}/{entity}?$format=json&$top={top}";
            using var resp = await client.GetAsync(url).ConfigureAwait(false);
            var body = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (!resp.IsSuccessStatusCode)
                return (false, $"Код {(int)resp.StatusCode}: {Truncate(body, 600)}");
            return (true, Truncate(body, 2000));
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static HttpClient CreateClient(OneCSettings settings)
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        if (!string.IsNullOrWhiteSpace(settings.ODataUsername))
        {
            var token = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{settings.ODataUsername}:{settings.ODataPassword ?? ""}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
        return client;
    }

    private static string NormalizeBaseUrl(string url)
    {
        url = url.Trim();
        if (!url.EndsWith('/')) url += "/";
        return url;
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s[..max] + "…";
}
