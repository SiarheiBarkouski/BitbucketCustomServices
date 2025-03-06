namespace BitbucketCustomServices.Entities;

public abstract class EntityAutoIdentifier
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
}