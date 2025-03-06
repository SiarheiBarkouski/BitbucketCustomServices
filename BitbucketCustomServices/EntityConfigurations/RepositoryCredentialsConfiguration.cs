using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class RepositoryCredentialsConfiguration : IEntityTypeConfiguration<RepositoryCredentials>
{
    public void Configure(EntityTypeBuilder<RepositoryCredentials> builder)
    {
        builder
            .HasKey(x => x.RepositoryId);
        
        builder
            .Property(x => x.AuthType)
            .IsRequired();

        builder
            .HasOne(x => x.Repository)
            .WithOne(x => x.RepositoryCredentials)
            .OnDelete(DeleteBehavior.Cascade);
    }
}