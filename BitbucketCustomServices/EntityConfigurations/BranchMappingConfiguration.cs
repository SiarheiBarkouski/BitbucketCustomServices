using BitbucketCustomServices.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BitbucketCustomServices.EntityConfigurations;

public class BranchMappingConfiguration:IEntityTypeConfiguration<BranchMapping>
{
    public void Configure(EntityTypeBuilder<BranchMapping> builder)
    {
        builder
            .HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .ValueGeneratedNever();

        builder
            .Property(x => x.From)
            .IsRequired();
        
        builder
            .Property(x => x.To)
            .IsRequired();

        builder
            .HasOne(x => x.Repository)
            .WithMany(x=>x.BranchMappings)
            .HasForeignKey(x=>x.RepositoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}