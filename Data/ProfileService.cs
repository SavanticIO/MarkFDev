using Microsoft.EntityFrameworkCore;

namespace MarkFDev.Data;

public class ProfileService(IDbContextFactory<AppDbContext> factory)
{
    public async Task<SiteProfile> GetProfileAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Profiles.AsNoTracking().FirstOrDefaultAsync() ?? new SiteProfile { Name = "Your Name", Title = "Your Title" };
    }

    public async Task<List<SkillGroup>> GetSkillsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.SkillGroups.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task<List<Job>> GetJobsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Jobs.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
    }

    public async Task<List<Project>> GetProjectsAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        return await db.Projects.AsNoTracking().OrderBy(x => x.SortOrder).ThenBy(x => x.Id).ToListAsync();
    }

    // Inserts when Id == 0, otherwise updates.
    public async Task SaveAsync<T>(T entity) where T : Entity
    {
        await using var db = await factory.CreateDbContextAsync();
        if (entity.Id == 0) db.Add(entity); else db.Update(entity);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync<T>(int id) where T : Entity
    {
        await using var db = await factory.CreateDbContextAsync();
        await db.Set<T>().Where(x => x.Id == id).ExecuteDeleteAsync();
    }
}
