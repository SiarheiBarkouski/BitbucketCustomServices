namespace BitbucketCustomServices.Entities;

public class BranchMapping : EntityAutoIdentifier
{
    public string From { get; set; }
    
    public string To { get; set; }
    
    public Guid RepositoryId { get; set; }
    
    public virtual Repository Repository { get; set; }
}