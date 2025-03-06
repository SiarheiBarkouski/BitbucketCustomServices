using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class RepositoryNotificationSettingsConfiguration : IEntityTypeConfiguration<RepositoryNotificationSettings>
{
    public void Configure(EntityTypeBuilder<RepositoryNotificationSettings> builder)
    {
        builder
            .HasKey(x => x.RepositoryId);

        builder
            .Property(x => x.EventType)
            .IsRequired();

        builder
            .HasOne(x => x.Repository)
            .WithOne(x => x.RepositoryNotificationSettings)
            .OnDelete(DeleteBehavior.Cascade);
    }
}