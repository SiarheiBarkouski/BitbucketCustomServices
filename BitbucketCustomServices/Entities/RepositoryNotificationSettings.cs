using BitbucketCustomServices.Enums;

namespace BitbucketCustomServices.Entities;

public class RepositoryNotificationSettings
{
    public Guid RepositoryId { get; set; }
    
    public EventType EventType { get; set; }
    
    public bool IgnoreAutoMergeNotifications { get; set; }
    
    public virtual Repository Repository { get; set; }
}