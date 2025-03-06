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
    public string? TelegramBotToken { get; set; }
    public string? TelegramChatId { get; set; }
    public string? BitbucketToken { get; set; }
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
