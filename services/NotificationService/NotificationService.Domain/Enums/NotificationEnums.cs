namespace NotificationService.Domain.Enums;

public enum NotificationType
{
    Welcome,
    PasswordReset,
    ProfileUpdate,
    OrderConfirmation
}

public enum NotificationChannel
{
    Email,
    InApp,
    SMS
}

public enum NotificationStatus
{
    Pending,
    Sent,
    Failed
}
