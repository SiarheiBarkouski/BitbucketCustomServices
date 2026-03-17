#nullable enable
namespace BitbucketCustomServices.Models;

public class SeedConfiguration
{
    public List<SeedUserWithRoleConfiguration>? Users { get; set; }
    public List<SeedProjectConfiguration>? Projects { get; set; }
}

public class SeedUserWithRoleConfiguration
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // Role name: Admin, Moderator, or User
}

public class SeedProjectConfiguration
{
    public string Name { get; set; } = string.Empty;
    public List<SeedRepositoryConfiguration> Repositories { get; set; } = new();
}

public class SeedRepositoryConfiguration
{
    public string Name { get; set; } = string.Empty;
    public string MergeStrategy { get; set; } = "merge_commit";
    /// <summary>When true, cascade merge is enabled. If omitted, defaults to false.</summary>
    public bool? CascadeMergeEnabled { get; set; }
    /// <summary>When true, Telegram notifications are enabled. If omitted, defaults to false.</summary>
    public bool? TelegramNotificationsEnabled { get; set; }
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    /// <summary>Bitbucket API token (AuthToken auth).</summary>
    public string? BitbucketToken { get; set; }
    /// <summary>Username for BasicPasswordAuth. Use with Password.</summary>
    public string? UserName { get; set; }
    /// <summary>Password for BasicPasswordAuth. Use with UserName.</summary>
    public string? Password { get; set; }
    /// <summary>Email for BasicTokenAuth. Use with UserToken.</summary>
    public string? UserEmail { get; set; }
    /// <summary>API token for BasicTokenAuth. Use with UserEmail.</summary>
    public string? UserToken { get; set; }
    /// <summary>Auth type: "BasicPasswordAuth", "BasicTokenAuth", or "AuthToken". If omitted, inferred from credentials.</summary>
    public string? AuthType { get; set; }
    /// <summary>Webhook secret for X-Hub-Signature verification. Leave empty to skip verification.</summary>
    public string? WebhookSecret { get; set; }
    public List<SeedBranchMappingConfiguration> BranchMappings { get; set; } = new();
    public SeedRepositoryNotificationSettings? NotificationSettings { get; set; }
    public List<string>? UserNames { get; set; } // Usernames of users who should have access
}

public class SeedBranchMappingConfiguration
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
}

public class SeedRepositoryNotificationSettings
{
    public bool IgnoreAutoMergeNotifications { get; set; } = false;
}
