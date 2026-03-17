using BitbucketCustomServices.Enums;

namespace BitbucketCustomServices.Entities;

public class RepositoryCredentials
{
    public Guid RepositoryId { get; set; }
    
    public string Username { get; set; }

    public string Password { get; set; }

    public string Email { get; set; }

    public string Token { get; set; }

    public AuthType AuthType { get; set; }
    
    public virtual Repository Repository { get; set; }

    public (bool, string) Validate()
    {
        if (AuthType == AuthType.BasicPasswordAuth)
        {
            if (string.IsNullOrEmpty(Username))
                return (false, "Username is required for Basic Password auth");
            if (string.IsNullOrEmpty(Password))
                return (false, "Password is required for Basic Password auth");
        }
        else if (AuthType == AuthType.BasicTokenAuth)
        {
            if (string.IsNullOrEmpty(Email))
                return (false, "Email is required for Basic Token auth");
            if (string.IsNullOrEmpty(Token))
                return (false, "Token is required for Basic Token auth");
        }
        else if (AuthType == AuthType.AuthToken)
        {
            if (string.IsNullOrEmpty(Token))
                return (false, "Token is required for Auth Token auth");
        }

        return (true, null);
    }
}