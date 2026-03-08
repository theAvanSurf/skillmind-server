namespace SkillMind.Core.Domain.Settings;

/// <summary>
/// SMTP configuration bound from the "Email" section in appsettings.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string From { get; set; } = string.Empty;
    public string FromDisplayName { get; set; } = "SkillMind";
    public bool EnableSsl { get; set; } = true;
}
