using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MarkFDev.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<IdentityUser>(options), IDataProtectionKeyContext
{
    public DbSet<SiteProfile> Profiles => Set<SiteProfile>();
    public DbSet<SkillGroup> SkillGroups => Set<SkillGroup>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
}
