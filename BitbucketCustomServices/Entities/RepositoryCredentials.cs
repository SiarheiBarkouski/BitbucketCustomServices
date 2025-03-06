using BitbucketCustomServices.Enums;

namespace BitbucketCustomServices.Entities;

public class RepositoryCredentials
{
    public Guid RepositoryId { get; set; }
    
    public string Username { get; set; }

    public string Password { get; set; }

    public string Token { get; set; }

    public AuthType AuthType { get; set; }
    
    public virtual Repository Repository { get; set; }

    public (bool, string) Validate()
    {
        if (AuthType == AuthType.Basic)
        {
            if (string.IsNullOrEmpty(Username))
                return (false, "Username is required for Basic auth");
            
            if (string.IsNullOrEmpty(Password))
                return (false,"Password is required for Basic auth");
        }
        else if (AuthType == AuthType.AuthToken)
        {
            if (string.IsNullOrEmpty(Token))
                return (false, "Token is required for AuthToken auth");
        }
        
        return (true, null);
    }
}