# Project 02 — Team Task Manager Checklist

> This file tracks **what has been done so far** and what remains.
> Keep it updated after each lesson or milestone.

## Current Status

- **Project:** Team Task Manager / Mini Jira
- **Current Phase:** Phase 2 — Core Domain APIs & Resource-Level Authorization
- **Status:** In Progress
- **Last Completed Lesson:** Label CRUD + Attach/Remove Labels from Tasks (Owner-only taxonomy + Member tagging)
- **Next Lesson:** Task Querying (Filtering, Searching & Pagination) OR AI Task Assistant

---

## ✅ Completed

### Project Structure

- [x] Created root project structure
- [x] Created `backend/`
- [x] Created `frontend/`
- [x] Created `docs/`
- [x] Added project master-plan documentation
- [x] Created interactive visual architecture documentation (`docs/Engineering/index.html` & `docs/Engineering/README.md`)
- [x] Added complete folder architecture guide in root `README.md`
- [x] Created comprehensive Role & Resource Authorization Matrix (`docs/permission-matrix.md`)

### Backend Setup

- [x] Created ASP.NET Core Web API project
- [x] Backend solution/project builds successfully
- [x] Backend application runs successfully
- [x] Created backend folders:
  - [x] `Controllers/`
  - [x] `Data/`
  - [x] `Models/`
  - [x] `Contracts/`
    - [x] `Contracts/Auth/`
    - [x] `Contracts/Projects/`
    - [x] `Contracts/Tasks/`
    - [x] `Contracts/Comments/`
  - [x] `Services/`
  - [x] `Auth/`
  - [x] `Authorization/`
  - [x] `Common/`
- [x] Configured Swagger UI with JWT Bearer authentication scheme (`AddSecurityDefinition` & `AddSecurityRequirement`)

### Frontend Setup

- [x] Created Next.js frontend
- [x] Using JavaScript / JSX
- [x] Using Tailwind CSS
- [x] Initialized shadcn/ui
- [x] Created/available frontend folders:
  - [x] `app/`
  - [x] `components/`
  - [x] `features/`
  - [x] `hooks/`
  - [x] `lib/`
  - [x] `public/`
- [x] `components.json` exists
- [x] `jsconfig.json` exists
- [x] `.env.local.example` exists

### Database / EF Core

- [x] Installed/configured Entity Framework Core
- [x] Installed/configured PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- [x] Configured PostgreSQL connection string in `appsettings.json`
- [x] Created `AppDbContext` inheriting `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>`
- [x] Registered `AppDbContext` in `Program.cs`
- [x] Backend successfully connects to PostgreSQL
- [x] Database created successfully (`team_task_manager`)
- [x] Default Workspace record seeded into database
- [x] Initial EF Core migration created and applied
- [x] Schema relationships, cascade rules, and indexes configured:
  - [x] ProjectMember composite key & unique constraint `(ProjectId, UserId)`
  - [x] Restrict delete behavior on User foreign keys (Assignee, CreatedBy, Author)
  - [x] Cascade delete behavior on Project -> TaskItem -> Comment hierarchy

### Domain Models

- [x] Created `Workspace`
- [x] Created `Project`
- [x] Created `ApplicationUser` (inheriting `IdentityUser<Guid>`)
- [x] Created `ProjectMember` (junction entity)
- [x] Created `ProjectRole` (`Owner`, `Member`)
- [x] Created `TaskItem`
- [x] Created `TaskItemStatus` (`Todo`, `InProgress`, `Done`)
- [x] Created `Comment`
- [x] Created `Label`

### Authentication & Identity API

- [x] ASP.NET Core Identity integration configured
- [x] Register (`POST /api/auth/register`) with automatic DisplayName & Email handling
- [x] Login (`POST /api/auth/login`) with password verification
- [x] Password hashing flow via ASP.NET Core Identity `UserManager`
- [x] JWT access token generation with HMAC-SHA256 signing
- [x] Token claims configured (`ClaimTypes.NameIdentifier`, `ClaimTypes.Email`, `ClaimTypes.Name`)
- [x] Current-user profile endpoint (`GET /api/auth/me`) protected by `[Authorize]`

### Project Management & Membership API

- [x] Create project (`POST /api/projects`) with automatic `ProjectRole.Owner` membership assignment
- [x] Get project details (`GET /api/projects/{projectId}`) with membership check
- [x] Delete project (`DELETE /api/projects/{projectId}`) restricted strictly to `ProjectRole.Owner`
- [x] Add project member (`POST /api/projects/{projectId}/members`) - Owner only
- [x] List project members (`GET /api/projects/{projectId}/members`) - any project member
- [x] Update project member role (`PUT /api/projects/{projectId}/members/{userId}/role`) - Owner only
- [x] Remove project member (`DELETE /api/projects/{projectId}/members/{userId}`) - Owner only

### Task Management API

- [x] Create task (`POST /api/projects/{projectId}/tasks`)
  - Enforced project membership for caller
  - Verified optional `AssigneeId` is also a member of the same project
  - Enforced `CreatedById = currentUserId` from JWT (mass assignment defense)
  - Default status set to `TaskItemStatus.Todo`
- [x] List project tasks (`GET /api/projects/{projectId}/tasks`)
  - Filtered by `ProjectId`
  - Optimized projection `.Select(...)` preventing circular serialization
- [x] Get single task (`GET /api/tasks/{taskId}`)
  - IDOR defense: verified caller is a member of `task.ProjectId`
- [x] Update task (`PUT /api/tasks/{taskId}`)
  - Enum range validation (`Enum.IsDefined`)
  - Optional assignee project membership validation
  - Audit timestamp update (`ModifiedAt = DateTimeOffset.UtcNow`)
- [x] Delete task (`DELETE /api/tasks/{taskId}`)
  - Owner vs Creator authorization: Owner can delete any task; Member can only delete tasks they personally created

### Comment API

- [x] Create comment (`POST /api/tasks/{taskId}/comments`)
  - Verified caller is member of task's project
  - `AuthorId` strictly extracted from JWT
- [x] List task comments (`GET /api/tasks/{taskId}/comments`)
  - Chronological sorting via `.OrderBy(x => x.CreatedAt)`
  - Safe projection of author profile (`Id`, `DisplayName`, `Email`)
- [x] Update comment (`PUT /api/comments/{commentId}`)
  - Eager loaded `.Include(x => x.TaskItem)` for project verification
  - Strict Author-only modification rule (`comment.AuthorId == currentUserId`)
  - Updated `ModifiedAt = DateTimeOffset.UtcNow`
- [x] Delete comment (`DELETE /api/comments/{commentId}`)
  - Dual authorization rule: Author can delete own comment; Project Owner can delete any comment for moderation

### Labels & Task Categorization API

- [x] Create label (`POST /api/projects/{projectId}/labels`)
  - Owner-only authorization check
  - Application-level duplicate validation (`ProjectId` + `Name`)
- [x] List project labels (`GET /api/projects/{projectId}/labels`)
  - Any project member can view labels
  - Alphabetical sorting via `.OrderBy(x => x.Name)`
- [x] Update label (`PUT /api/labels/{labelId}`)
  - Owner-only authorization check
  - Self-excluding duplicate validation (`x.Id != labelId`)
- [x] Delete label (`DELETE /api/labels/{labelId}`)
  - Owner-only authorization check
  - EF Core automatically cleans up many-to-many junction table rows
- [x] Attach label to task (`POST /api/tasks/{taskId}/labels/{labelId}`)
  - Any project member can tag tasks
  - Cross-project boundary check (`label.ProjectId == task.ProjectId`)
  - Duplicate attachment prevention check (`task.Labels.Any(x => x.Id == labelId)`)
- [x] Remove label from task (`DELETE /api/tasks/{taskId}/labels/{labelId}`)
  - Any project member can detach labels from tasks

### Problems Solved

- [x] Fixed EF migration command/project-path issue
- [x] Fixed `Workspace` namespace/type resolution error
- [x] Fixed `IdentityDbContext<,,>` namespace resolution in `AppDbContext.cs`
- [x] Aligned EF Core package versions (`8.0.11`) to eliminate `MSB3277` warning
- [x] Fixed misplaced OpenApi `using` directive in `Program.cs`
- [x] Fixed DbContext naming reference to `AppDbContext` across all controllers
- [x] Seeded default `Workspace` record into PostgreSQL to resolve `404 Workspace not found` during project creation
- [x] Rebuilt and verified full solution compilation with 0 warnings and 0 errors

---

## 🟡 In Progress

### Phase 2 — Core Domain APIs & Resource-Level Authorization

Completed inside Phase 2:

- [x] Authentication API (`AuthController`)
- [x] Project Membership & Roles API (`ProjectsController`)
- [x] Task CRUD API (`TasksController`)
- [x] Comment CRUD API (`CommentsController`)
- [x] Labels & Task Tagging API (`LabelsController`)
- [x] Permission Matrix & Security Documentation (`docs/permission-matrix.md`)

Still required for Phase 2:

- [ ] Query Features (Search, Filtering by status/assignee, Pagination)

---

## ⏭️ Next Immediate Lessons

1. **Task Querying (Filter, Search & Pagination)**
   - Filter by `Status`, `AssigneeId`
   - Search by `Title` keyword
   - Pagination parameters (`page`, `pageSize`)
2. **AI Task Assistant**
   - Integrate Gemini API for task breakdowns & checklists

---

## ❌ Not Started Yet

### Query Features

- [ ] Search by keyword
- [ ] Filtering (by status, assignee, priority)
- [ ] Sorting
- [ ] Pagination (`page`, `pageSize`)

### Frontend Features

- [ ] Register page
- [ ] Login page
- [ ] Dashboard / Workspace overview
- [ ] Project board view (Kanban columns: Todo, In Progress, Done)
- [ ] Task detail modal/drawer
- [ ] Comments thread UI
- [ ] Labels selector & badges UI
- [ ] Search & filter bar
- [ ] Responsive loading & error states

### AI Feature

- [ ] AI task-description endpoint (Gemini API integration)
- [ ] Structured JSON output for subtasks & acceptance criteria
- [ ] Frontend user review/edit/accept flow

### Automated Testing

- [ ] Unit tests (DTO validation, business rules)
- [ ] Integration tests (WebApplicationFactory + PostgreSQL Testcontainers)
- [ ] Authorization / IDOR security test suite

---

## Progress Summary

```text
Project setup                  ✅ Done
Backend Web API                ✅ Done
Frontend Next.js setup         ✅ Done
Tailwind + shadcn/ui           ✅ Done
EF Core + PostgreSQL           ✅ Done
AppDbContext                   ✅ Done
Domain Entities (All)          ✅ Done
Authentication (JWT)           ✅ Done
Projects & Roles API           ✅ Done
Task CRUD API                  ✅ Done
Comment CRUD API               ✅ Done
Labels & Tagging API           ✅ Done
Permission Matrix Docs         ✅ Done

Query Features (Search/Filter) 🟡 Next up
Frontend UI Pages              ❌ Not started
AI Task Feature                ❌ Not started
Automated Test Suite           ❌ Not started
```

## Last Known Working State

```text
Backend builds cleanly with 0 errors and 0 warnings.
Swagger UI exposes Auth, Projects, Tasks, and Comments with JWT Bearer support.
PostgreSQL database 'team_task_manager' is fully migrated and seeded.
Resource-level authorization (IDOR defense) and Owner vs Member permissions are fully active.
Documentation includes comprehensive architecture diagrams, folder guides, and permission matrices.
```
