using System.Text.Json;

namespace Shared.Helpers;

/// <summary>Opciones JSON únicas (camelCase) para el webhook, en emisor y receptor.</summary>
public static class WebhookJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
