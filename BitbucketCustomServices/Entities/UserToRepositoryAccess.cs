namespace BitbucketCustomServices.Entities;

public class UserToRepositoryAccess
{
    public string UserId { get; set; }
   
    public User User { get; set; }
    
    public Guid RepositoryId { get; set; }
    
    public Repository Repository { get; set; }
}