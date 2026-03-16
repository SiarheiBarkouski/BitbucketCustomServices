#nullable enable
using BitbucketCustomServices.Entities;
using Entities = BitbucketCustomServices.Entities;
using BitbucketCustomServices.Enums;
using BitbucketCustomServices.Models;

namespace BitbucketCustomServices;

/// <summary>
/// Helper for seed configuration validation and resolution. Exposed for unit testing.
/// </summary>
public static class SeedConfigHelper
{
    public static readonly EventType DefaultEventTypes = EventType.PullRequestApproved |
        EventType.PullRequestCreated | EventType.PullRequestDeclined |
        EventType.PullRequestMerged | EventType.PullRequestChangesRequested;

    public static readonly string[] StandardRoles = ["Admin", "Moderator", "User"];

    public static AuthType ResolveAuthType(SeedRepositoryConfiguration repoConfig)
    {
        var hasBasicCreds = !string.IsNullOrWhiteSpace(repoConfig.UserEmail) && !string.IsNullOrWhiteSpace(repoConfig.UserToken);

        if (!string.IsNullOrWhiteSpace(repoConfig.AuthType))
        {
            if (repoConfig.AuthType.Equals("Basic", StringComparison.OrdinalIgnoreCase) && hasBasicCreds)
                return AuthType.Basic;
            if (repoConfig.AuthType.Equals("AuthToken", StringComparison.OrdinalIgnoreCase))
                return AuthType.AuthToken;
        }

        return hasBasicCreds ? AuthType.Basic : AuthType.AuthToken;
    }

    public static bool IsValidRole(string? role) =>
        !string.IsNullOrEmpty(role) && StandardRoles.Contains(role);

    public static bool IsValidUserConfig(SeedUserWithRoleConfiguration userConfig) =>
        !string.IsNullOrWhiteSpace(userConfig.UserName) &&
        !string.IsNullOrWhiteSpace(userConfig.Email) &&
        !string.IsNullOrWhiteSpace(userConfig.Password) &&
        IsValidRole(userConfig.Role);

    public static Entities.Repository CreateRepositoryFromConfig(SeedRepositoryConfiguration repoConfig)
    {
        var authType = ResolveAuthType(repoConfig);
        var useBasicAuth = authType == AuthType.Basic;

        return new Entities.Repository
        {
            Name = repoConfig.Name,
            MergeStrategy = repoConfig.MergeStrategy,
            TelegramBotToken = repoConfig.TelegramBotToken ?? string.Empty,
            TelegramChatId = repoConfig.TelegramChatId ?? string.Empty,
            WebhookSecret = string.IsNullOrWhiteSpace(repoConfig.WebhookSecret) ? null : repoConfig.WebhookSecret.Trim(),
            Users = [],
            RepositoryNotificationSettings = new RepositoryNotificationSettings
            {
                EventType = DefaultEventTypes,
                IgnoreAutoMergeNotifications = repoConfig.NotificationSettings?.IgnoreAutoMergeNotifications ?? false
            },
            RepositoryCredentials = useBasicAuth
                ? new RepositoryCredentials
                {
                    AuthType = AuthType.Basic,
                    Username = repoConfig.UserEmail!.Trim(),
                    Password = repoConfig.UserToken!.Trim()
                }
                : new RepositoryCredentials
                {
                    AuthType = AuthType.AuthToken,
                    Token = repoConfig.BitbucketToken ?? string.Empty
                },
            BranchMappings = repoConfig.BranchMappings
                .Select(bm => new BranchMapping
                {
                    From = bm.From,
                    To = bm.To
                })
                .ToList()
        };
    }
}
