namespace UserService.Application.Common.Settings;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string SecretKey { get; set; } = default!;
    public string Issuer { get; set; } = "NotificationPlatform.UserService";
    public string Audience { get; set; } = "NotificationPlatform";
    public int AccessTokenExpiryMinutes { get; set; } = 60;
    public int RefreshTokenExpiryDays { get; set; } = 7;
}
