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
        var hasBasicPasswordCreds = !string.IsNullOrWhiteSpace(repoConfig.UserName) && !string.IsNullOrWhiteSpace(repoConfig.Password);
        var hasBasicTokenCreds = !string.IsNullOrWhiteSpace(repoConfig.UserEmail) && !string.IsNullOrWhiteSpace(repoConfig.UserToken);

        if (!string.IsNullOrWhiteSpace(repoConfig.AuthType))
        {
            if (repoConfig.AuthType.Equals("BasicPasswordAuth", StringComparison.OrdinalIgnoreCase) && hasBasicPasswordCreds)
                return AuthType.BasicPasswordAuth;
            if (repoConfig.AuthType.Equals("BasicTokenAuth", StringComparison.OrdinalIgnoreCase) && hasBasicTokenCreds)
                return AuthType.BasicTokenAuth;
            if (repoConfig.AuthType.Equals("AuthToken", StringComparison.OrdinalIgnoreCase))
                return AuthType.AuthToken;
        }

        if (hasBasicPasswordCreds) return AuthType.BasicPasswordAuth;
        if (hasBasicTokenCreds) return AuthType.BasicTokenAuth;
        return AuthType.AuthToken;
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

        RepositoryCredentials credentials = authType switch
        {
            AuthType.BasicPasswordAuth => new RepositoryCredentials
            {
                AuthType = AuthType.BasicPasswordAuth,
                Username = repoConfig.UserName!.Trim(),
                Password = repoConfig.Password!.Trim()
            },
            AuthType.BasicTokenAuth => new RepositoryCredentials
            {
                AuthType = AuthType.BasicTokenAuth,
                Email = repoConfig.UserEmail!.Trim(),
                Token = repoConfig.UserToken!.Trim()
            },
            _ => new RepositoryCredentials
            {
                AuthType = AuthType.AuthToken,
                Token = repoConfig.BitbucketToken ?? string.Empty
            }
        };

        return new Entities.Repository
        {
            Name = repoConfig.Name,
            MergeStrategy = repoConfig.MergeStrategy,
            CascadeMergeEnabled = repoConfig.CascadeMergeEnabled ?? false,
            TelegramNotificationsEnabled = repoConfig.TelegramNotificationsEnabled ?? false,
            TelegramBotToken = repoConfig.TelegramBotToken ?? string.Empty,
            TelegramChatId = repoConfig.TelegramChatId ?? string.Empty,
            WebhookSecret = string.IsNullOrWhiteSpace(repoConfig.WebhookSecret) ? null : repoConfig.WebhookSecret.Trim(),
            Users = [],
            RepositoryNotificationSettings = new RepositoryNotificationSettings
            {
                EventType = DefaultEventTypes,
                IgnoreAutoMergeNotifications = repoConfig.NotificationSettings?.IgnoreAutoMergeNotifications ?? false
            },
            RepositoryCredentials = credentials,
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
