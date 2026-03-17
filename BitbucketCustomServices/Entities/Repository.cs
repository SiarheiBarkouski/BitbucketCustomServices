namespace BitbucketCustomServices.Entities;

public class Repository : EntityAutoIdentifier
{
    public string Name { get; set; }
    
    public string MergeStrategy { get; set; }
    
    public bool CascadeMergeEnabled { get; set; } = false;
    
    public bool TelegramNotificationsEnabled { get; set; } = false;
    
    public string TelegramBotToken { get; set; }
    
    public string TelegramChatId { get; set; }
    
    public string? WebhookSecret { get; set; }
    
    public virtual RepositoryCredentials RepositoryCredentials { get; set; }
    
    public virtual RepositoryNotificationSettings RepositoryNotificationSettings { get; set; }
    
    public Guid ProjectId { get; set; }
    
    public virtual Project Project { get; set; }
    
    public virtual List<BranchMapping> BranchMappings { get; set; }
    
    public virtual List<User> Users { get; set; }
    
    public virtual List<UserToRepositoryAccess> UserToRepositoryAccesses { get; set; }
}