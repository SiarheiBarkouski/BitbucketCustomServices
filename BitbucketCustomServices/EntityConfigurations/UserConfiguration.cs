using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasMany(x => x.UserRepositories)
            .WithMany(x => x.Users)
            .UsingEntity<UserToRepositoryAccess>(
                r => r.HasOne(e => e.Repository).WithMany(e => e.UserToRepositoryAccesses).HasForeignKey(e => e.RepositoryId),
                l => l.HasOne(e => e.User).WithMany(e => e.UserToRepositoryAccesses).HasForeignKey(e => e.UserId),
                x =>
                {
                    x.HasKey(t => new { t.UserId, t.RepositoryId });
                    x.ToTable("UserToRepositoryAccess");
                });
    }
}