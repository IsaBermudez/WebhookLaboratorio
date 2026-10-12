using System.Text.Json;

namespace Shared.Helpers;

public static class WebhookJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
