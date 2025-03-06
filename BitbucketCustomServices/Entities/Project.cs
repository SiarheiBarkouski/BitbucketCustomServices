namespace BitbucketCustomServices.Entities;

public class Project : EntityAutoIdentifier
{
    public string Name { get; set; }
    
    public virtual List<Repository> Repositories { get; set; }
}