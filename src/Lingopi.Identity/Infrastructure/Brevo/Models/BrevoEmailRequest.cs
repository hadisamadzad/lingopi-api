namespace Lingopi.Identity.Infrastructure.Brevo.Models;

public class BrevoEmailRequest
{
    public RecipientModel? Sender { get; set; }
    public required List<RecipientModel> To { get; set; }
    public string? HtmlContent { get; set; }
    public string? TextContent { get; set; }
    public string? Subject { get; set; }
    public long? TemplateId { get; set; }
    public Dictionary<string, string>? Params { get; set; }
}
