using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace MarkFDev.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services, IConfiguration config, ILogger logger)
    {
        var db = services.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();

        if (!await db.Profiles.AnyAsync())
            await SeedContentAsync(db);

        var email = config["Admin:Email"];
        var password = config["Admin:Password"];
        var users = services.GetRequiredService<UserManager<IdentityUser>>();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            if (!await users.Users.AnyAsync())
                logger.LogWarning("No admin user exists and Admin:Email / Admin:Password are not configured.");
            return;
        }

        var admin = await users.FindByEmailAsync(email);
        if (admin is null)
        {
            var result = await users.CreateAsync(new IdentityUser { UserName = email, Email = email, EmailConfirmed = true }, password);
            if (!result.Succeeded)
                logger.LogError("Could not create admin user: {Errors}", string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    // Placeholder content: edit it from the admin page.
    private static async Task SeedContentAsync(AppDbContext db)
    {
        db.Profiles.Add(new SiteProfile
        {
            Name = "Mark F",
            Title = "Senior Software Engineer",
            Tagline = "9 years building and shipping reliable .NET software, from API design to cloud deployment.",
            Location = "United Kingdom",
            GitHubUrl = "https://github.com/your-username",
            LinkedInUrl = "https://www.linkedin.com/in/your-username",
            Email = "you@example.com",
            About = "I'm a software engineer with nine years of professional experience delivering production systems in C# and .NET.\n\n" +
                    "This site is itself a project: a Blazor app using MudBlazor, containerised with Docker and deployed to the live internet through a CI/CD pipeline."
        });

        db.SkillGroups.AddRange(
            new SkillGroup { Name = "Languages", Items = ["C#", "SQL", "TypeScript", "JavaScript"], SortOrder = 1 },
            new SkillGroup { Name = "Frameworks", Items = ["ASP.NET Core", "Blazor", "Entity Framework Core", "MudBlazor"], SortOrder = 2 },
            new SkillGroup { Name = "Cloud & DevOps", Items = ["Docker", "GitHub Actions", "NGINX", "PostgreSQL", "CI/CD"], SortOrder = 3 },
            new SkillGroup { Name = "Practices", Items = ["Clean architecture", "Unit & integration testing", "Code review", "Agile"], SortOrder = 4 });

        db.Jobs.AddRange(
            new Job
            {
                Role = "Senior Software Engineer", Company = "Company A", Period = "2022 - Present", SortOrder = 1,
                Summary = "Lead developer on a customer-facing platform.",
                Highlights = ["Designed and delivered REST APIs serving thousands of daily users", "Cut deployment time by introducing automated pipelines", "Mentored junior engineers"],
                Tech = ["C#", ".NET", "Azure", "SQL Server"]
            },
            new Job
            {
                Role = "Software Engineer", Company = "Company B", Period = "2019 - 2022", SortOrder = 2,
                Summary = "Full-stack developer in a product team.",
                Highlights = ["Built features end-to-end across web UI and backend services", "Improved test coverage and reliability"],
                Tech = ["C#", "ASP.NET Core", "Docker"]
            },
            new Job
            {
                Role = "Junior Developer", Company = "Company C", Period = "2017 - 2019", SortOrder = 3,
                Summary = "Started my career working on internal line-of-business applications.",
                Highlights = ["Maintained and extended legacy systems", "Learned engineering fundamentals in a supportive team"],
                Tech = ["C#", "SQL", "JavaScript"]
            });

        db.Projects.AddRange(
            new Project
            {
                Name = "MarkFDev (this site)", SortOrder = 1,
                Description = "Personal portfolio built with Blazor and MudBlazor, backed by PostgreSQL, containerised and deployed with CI/CD.",
                Tech = ["Blazor", "MudBlazor", ".NET 10", "PostgreSQL", "Docker", "GitHub Actions"],
                RepoUrl = "https://github.com/your-username/MarkFDev"
            },
            new Project
            {
                Name = "Project Two", SortOrder = 2,
                Description = "Describe what it does, the problem it solves and what you're proud of.",
                Tech = ["C#", "ASP.NET Core", "PostgreSQL"],
                RepoUrl = "https://github.com/your-username/project-two"
            });

        await db.SaveChangesAsync();
    }
}
