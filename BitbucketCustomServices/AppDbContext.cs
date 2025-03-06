using BitbucketCustomServices.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BitbucketCustomServices;

public class AppDbContext : IdentityDbContext<User>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<Project> Projects { get; set; }

    public DbSet<Repository> Repositories { get; set; }
    
    public DbSet<BranchMapping> BranchesMappings { get; set; }
    
    public DbSet<RepositoryCredentials> RepositoryCredentials { get; set; }
    
    public DbSet<RepositoryNotificationSettings> RepositoryNotificationSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        
        base.OnModelCreating(modelBuilder);
    }
}