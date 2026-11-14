using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using System;
using System.IO.Pipelines;
using TeamTaskManager.Api.Data;
using TeamTaskManager.Api.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace TeamTaskManager.Api.Data;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();

    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<ProjectMember>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();

        builder.Entity<ProjectMember>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId);

        builder.Entity<ProjectMember>()
            .HasOne(x => x.User)
            .WithMany(x => x.ProjectMemberships)
            .HasForeignKey(x => x.UserId);
    }

}




//This constructor allows ASP.NET Core's Dependency Injection (DI) system to pass database configuration settings (like your connection string and database provider) into your `AppDbContext`, which then forwards them directly to Entity Framework Core's base `DbContext` class.

//Here is a breakdown of each piece of that syntax:

//***`public AppDbContext(...)`**: This is the constructor for your `AppDbContext` class. It runs automatically whenever an instance of `AppDbContext` is created.
//***`DbContextOptions<AppDbContext> options`**: This is the parameter the constructor expects.It holds all the configuration details for your database—such as whether you are using SQL Server, PostgreSQL, or SQLite, along with the connection string and logging rules.The `<AppDbContext>` generic type ensures these specific options belong only to this context (useful if your app connects to multiple databases).
//***`: base(options)`**: Because your `AppDbContext` inherits from Entity Framework's built-in `DbContext` (`: DbContext`), it needs the parent class to do the heavy lifting of managing connections and tracking changes. `: base(options)` takes the `options` parameter you received and passes it straight up to the parent `DbContext` constructor.
//***`{ }`**: The constructor body is empty because `AppDbContext` doesn't need to run any extra setup code of its own—passing `options` to `base` handles everything.

//### How it connects to the rest of your app

//In your `Program.cs` file, you likely have a line similar to this:

//```csharp
//builder.Services.AddDbContext<AppDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

//```

//When you configure `AddDbContext` in `Program.cs`, ASP.NET Core packages up that `UseSqlServer(...)` configuration into a `DbContextOptions<AppDbContext>` object and automatically injects it into the constructor you asked about whenever a controller or service requests `AppDbContext`.