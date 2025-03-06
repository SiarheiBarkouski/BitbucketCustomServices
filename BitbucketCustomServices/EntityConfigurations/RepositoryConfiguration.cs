using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class RepositoryConfiguration : IEntityTypeConfiguration<Repository>
{
    public void Configure(EntityTypeBuilder<Repository> builder)
    {
        builder
            .HasIndex(x=>x.Name)
            .IsUnique();
        
        builder
            .HasKey(x => x.Id);

        builder
            .Property(x => x.Name)
            .IsRequired();

        builder
            .Property(x => x.MergeStrategy)
            .IsRequired();

        builder
            .HasOne(x => x.RepositoryCredentials)
            .WithOne(x => x.Repository)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder
            .HasOne(x => x.RepositoryNotificationSettings)
            .WithOne(x => x.Repository)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.Project)
            .WithMany(x => x.Repositories)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.BranchMappings)
            .WithOne(x => x.Repository)
            .HasForeignKey(x => x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}