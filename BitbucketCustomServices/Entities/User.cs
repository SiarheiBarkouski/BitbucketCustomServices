using Microsoft.AspNetCore.Identity;

namespace BitbucketCustomServices.Entities;

public class User : IdentityUser
{
    public virtual List<Repository> UserRepositories { get; set; }
    
    public virtual List<UserToRepositoryAccess> UserToRepositoryAccesses { get; set; }
}