namespace Shared.WebhookModels;

/// <summary>Sección "Webhook" de appsettings. El secreto HMAC lo comparten emisor y receptor.</summary>
public class WebhookOptions
{
    public string Secret { get; set; } = string.Empty;
}
