using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class ProjectConfiguration: IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder
            .HasIndex(x=>x.Name)
            .IsUnique();
        
        builder
            .HasKey(p => p.Id);
        
        builder
            .Property(p => p.Name)
            .IsRequired();

        builder
            .HasMany(x => x.Repositories)
            .WithOne(x => x.Project)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}