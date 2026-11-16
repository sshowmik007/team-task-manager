
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TeamTaskManager.Api.Models;



namespace TeamTaskManager.Api.Data;

public class AppDbContext
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    // =========================================================================
    // DATABASE TABLES (DbSets)
    // Each DbSet represents a table in your PostgreSQL database.
    // =========================================================================
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<Label> Labels => Set<Label>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // 1. PROJECT MEMBERSHIP COMPOSITE UNIQUE INDEX:
        // Ensures a User can only join a specific Project ONCE (no duplicate memberships).
        builder.Entity<ProjectMember>()
            .HasIndex(x => new { x.ProjectId, x.UserId })
            .IsUnique();

        // 2. PROJECT MEMBER RELATIONSHIPS:
        builder.Entity<ProjectMember>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId);

        builder.Entity<ProjectMember>()
            .HasOne(x => x.User)
            .WithMany(x => x.ProjectMemberships)
            .HasForeignKey(x => x.UserId);

        // 3. TASK ITEM RELATIONSHIPS:
        builder.Entity<TaskItem>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId);

        // DeleteBehavior.Restrict: Prevents deleting a User if they still have tasks assigned
        builder.Entity<TaskItem>()
            .HasOne(x => x.Assignee)
            .WithMany(x => x.AssignedTasks)
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        // DeleteBehavior.Restrict: Prevents deleting a User if they are the creator of tasks
        builder.Entity<TaskItem>()
            .HasOne(x => x.CreatedBy)
            .WithMany(x => x.CreatedTasks)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        // 4. COMMENT RELATIONSHIPS:
        builder.Entity<Comment>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TaskItemId);

        // DeleteBehavior.Restrict: Prevents deleting a User if they authored comments
        builder.Entity<Comment>()
            .HasOne(x => x.Author)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        // 5. LABEL RELATIONSHIPS & CONSTRAINTS:
        builder.Entity<Label>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Labels)
            .HasForeignKey(x => x.ProjectId);

        // Unique label name within a project (e.g. only one 'bug' tag per project)
        builder.Entity<Label>()
            .HasIndex(x => new { x.ProjectId, x.Name })
            .IsUnique();

        // 6. MANY-TO-MANY (TaskItem <-> Label):
        // EF Core automatically configures an internal join table for tasks and labels
        builder.Entity<TaskItem>()
            .HasMany(x => x.Labels)
            .WithMany(x => x.Tasks);

        // 7. COLUMN CONSTRAINTS & SIZING:
        // Enforces string bounds and NOT NULL constraints in PostgreSQL
        builder.Entity<Workspace>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();
        });

        builder.Entity<Project>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(150)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(2000);
        });

        // 8. QUERY PERFORMANCE INDEXES:
        // Creating indexes on ProjectId, AssigneeId, and Status makes Kanban filtering fast
        builder.Entity<TaskItem>(entity =>
        {
            entity.Property(x => x.Title)
                .HasMaxLength(200)
                .IsRequired();

            entity.Property(x => x.Description)
                .HasMaxLength(4000);

            entity.HasIndex(x => x.ProjectId);  // Accelerates: WHERE ProjectId = @pId
            entity.HasIndex(x => x.AssigneeId); // Accelerates: WHERE AssigneeId = @userId
            entity.HasIndex(x => x.Status);     // Accelerates: WHERE Status = Todo / Done
        });

        builder.Entity<Comment>(entity =>
        {
            entity.Property(x => x.Body)
                .HasMaxLength(4000)
                .IsRequired();

            entity.HasIndex(x => x.TaskItemId);
        });

        builder.Entity<Label>(entity =>
        {
            entity.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.HasIndex(x => new
            {
                x.ProjectId,
                x.Name
            })
            .IsUnique();
        });

        builder.Entity<TaskItem>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Tasks)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Comment>()
            .HasOne(x => x.TaskItem)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskItem>()
            .HasOne(x => x.Assignee)
            .WithMany(x => x.AssignedTasks)
            .HasForeignKey(x => x.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<TaskItem>()
            .HasOne(x => x.CreatedBy)
            .WithMany(x => x.CreatedTasks)
            .HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Comment>()
            .HasOne(x => x.Author)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectMember>()
            .HasOne(x => x.Project)
            .WithMany(x => x.Members)
            .HasForeignKey(x => x.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.Entity<ProjectMember>()
            .HasOne(x => x.User)
            .WithMany(x => x.ProjectMemberships)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectMember>()
            .HasIndex(x => new
            {
                x.ProjectId,
                x.UserId
            })
            .IsUnique();


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


//This code is an** Entity Framework Core(EF Core) `DbContext**`, which acts as the bridge between your C# classes (models) and your database tables.

//Inside the `OnModelCreating` method, you are using the ** Fluent API** to define the exact rules, relationships, and constraints for how your database tables should be built.

//Here is a breakdown of what those terms and methods mean, from the ground up.

//---

//### 1. The Basics (`DbSet` and `OnModelCreating`)

//* **`public DbSet<Project> Projects => Set<Project>();`**: This tells EF Core, *"Create a database table named `Projects` based on my `Project` C# class."* Each `DbSet` represents a table.
//***`protected override void OnModelCreating(ModelBuilder builder)`**: This method runs when EF Core is figuring out the database structure.You use `builder.Entity<T>()` to configure specific rules for a table.

//---

//### 2. Relationships (`HasOne`, `WithMany`, `HasForeignKey`)

//Relational databases connect tables together.In EF Core, you always describe a relationship from **both sides**.

//Take this block from your code as an example:

//```csharp
//builder.Entity<TaskItem>()
//    .HasOne(x => x.Project)       // Side A: A TaskItem belongs to ONE Project
//    .WithMany(x => x.Tasks)       // Side B: A Project can have MANY TaskItems
//    .HasForeignKey(x => x.ProjectId); // The glue: The column in TaskItems that stores the Project's ID

//```

//***`HasOne(...)`**: Defines the "one" side of a relationship.A single `TaskItem` only lives inside one `Project`.
//* **`WithMany(...)`**: Defines the "many" side.A single `Project` contains a list/collection of many `TaskItem`s. * (Note: `HasMany` + `WithMany` is also used in your code for `TaskItem` and `Label`—meaning a task can have many labels, and a label can be attached to many tasks).*
//***`HasForeignKey(...)`**: A** Foreign Key** is a column in one table that points to the Primary Key(ID) of another table.Here, the `TaskItems` table has a `ProjectId` column to remember which project it belongs to.

//---

//### 3. Deletion Rules (`OnDelete`)

//When you delete a record that other records depend on, the database needs to know what to do with the "orphaned" data.

//***`DeleteBehavior.Cascade`**: *"If the parent is deleted, automatically delete the children too."*
//* **Example in your code:** If you delete a `Project`, EF Core will automatically delete all `TaskItem`s inside that project.


//* **`DeleteBehavior.Restrict`**: *"Block the deletion if children still exist."*
//* **Example in your code:** `TaskItem` has `OnDelete(DeleteBehavior.Restrict)` for `Assignee` (a User). If you try to delete a User from the database who still has tasks assigned to them, the database will throw an error and prevent you from deleting the user until you reassign or delete their tasks first.



//---

//### 4. Data Rules & Performance (`IsRequired`, `HasMaxLength`, `HasIndex`, `IsUnique`)

//```csharp
//builder.Entity<Label>(entity =>
//{
//    entity.Property(x => x.Name)
//        .HasMaxLength(50)
//        .IsRequired();

//    entity.HasIndex(x => new { x.ProjectId, x.Name })
//        .IsUnique();
//});

//```

//***`IsRequired()`**: Makes the database column `NOT NULL`. You cannot save a `Label` without giving it a `Name`.
//***`HasMaxLength(50)`**: Limits the text column(e.g., `VARCHAR(50)` or `NVARCHAR(50)`) to 50 characters to save space and enforce validation.
//***`HasIndex(...)`**: Creates a database index(like the index at the back of a textbook). It makes searching and filtering by that column(like `ProjectId` or `Status`) drastically faster.
//* **`IsUnique()`**: Prevents duplicate values.In your `ProjectMember` table:
//```csharp
//.HasIndex(x => new { x.ProjectId, x.UserId }).IsUnique();

//```


//This creates a **Composite Unique Index**. It ensures that the same `UserId` cannot be added to the same `ProjectId` twice (preventing duplicate memberships), while still allowing that user to join *other* projects.

//---

//### Quick Tip on Your Code

//Notice that in your `OnModelCreating` method, several configurations are written **twice** (for example, `TaskItem` -> `Project`, `TaskItem` -> `Assignee`, `Comment` -> `Author`, and `ProjectMember` are configured near the top, and then configured again at the bottom with `.OnDelete(...)` added).

//EF Core won't crash—the bottom configurations will just overwrite the top ones—but you can safely delete the duplicate blocks at the top to keep your file clean and easy to read.