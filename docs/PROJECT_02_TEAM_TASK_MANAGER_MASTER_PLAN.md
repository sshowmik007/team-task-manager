# Project 02 — Team Task Manager (Mini Jira)

> **Purpose of this file:**  
> This is the living project memory for Project 02. Keep it inside the VS Code project/repository.  
> Whenever you come back later, you can give this file to ChatGPT and ask things like:
>
> - "Check what has been done so far."
> - "What should I implement next?"
> - "Review my Project 2 progress against this plan."
> - "Update the progress section based on these files."
> - "Which requirements are still missing?"
> - "Does my authorization implementation satisfy the Project 2 goal?"
>
> Update the checkboxes and the **Current Progress Snapshot** as the project evolves.

---

## 1. Project Identity

**Project:** Team Task Manager / Mini Jira  
**Project Number:** 02  
**Mode:** Full-stack  
**Difficulty:** Beginner → Intermediate  
**Expected learning duration:** roughly 3–4 weeks of focused learning  
**Primary goal:** Move from simple CRUD applications into a real multi-user application with authentication, authorization, relational data, frontend integration, and a small controlled AI feature.

---

# 2. Problem Statement

Build a collaborative task-management application where users can:

- create or join workspaces,
- create projects inside a workspace,
- collaborate with other project members,
- create and assign tasks,
- move tasks between workflow states,
- comment on tasks,
- attach labels,
- search/filter/sort tasks,
- and securely access only resources they are allowed to use.

This is intentionally similar to a simplified Jira/Trello/Linear-style product.

The project is **not primarily about building a beautiful Kanban board**.

The real learning goal is to understand:

1. who a user is,
2. how the backend authenticates that user,
3. which workspace/project that user belongs to,
4. which operations that user is allowed to perform,
5. how the frontend and backend exchange typed data,
6. how relational entities are modeled in EF Core,
7. and how application security must be enforced by the backend.

---

# 3. Source Blueprint Requirements

The original Project 02 blueprint defines the following core requirements.

## Required entities

- `User`
- `Workspace`
- `Project`
- `ProjectMember`
- `TaskItem`
- `Comment`
- `Label`

## Required build sequence

1. Authentication, access tokens, refresh-token lifecycle, password handling, and user profiles.
2. Projects, task CRUD, comments, assignment, and task-state transitions.
3. Workspace membership and **resource-level authorization**.
4. Next.js/React TypeScript board, task detail view, filters, loading/error states, and API client.
5. Sorting, paging, search, and appropriate optimistic UI updates.
6. AI-assisted task description, proposed subtasks, and checklist generated from a task title using structured JSON output.

## Required learning outcomes

- JWT/access-token concepts
- refresh-token security
- claims
- authentication vs authorization
- RBAC
- resource-level authorization
- React/Next.js integration
- CORS
- DTOs
- async forms
- frontend state
- API contracts

## Suggested stack

- ASP.NET Core
- ASP.NET Core Identity or equivalent established identity mechanism
- Entity Framework Core
- PostgreSQL
- Next.js
- TypeScript
- LLM API for the optional AI feature

## Definition of done from the blueprint

The project is considered successful when:

- two team members can work inside the same project;
- an unrelated user cannot read or update that project's tasks even if they guess identifiers;
- and task creation works end-to-end from browser → API → database.

---

# 4. Product Mental Model

A simple mental picture:

```text
User
 │
 ├── belongs to Workspace
 │
 ▼
Workspace
 │
 ├── Project A
 │    ├── Member 1
 │    ├── Member 2
 │    ├── Task 1
 │    ├── Task 2
 │    └── Task 3
 │
 └── Project B
      ├── Members
      └── Tasks
```

Example:

```text
Workspace: Acme Software

Project: Website Redesign
├── Task: Build login page
├── Task: Fix mobile navbar
└── Task: Create dashboard

Project: Mobile Application
├── Task: Add profile page
└── Task: Implement notifications
```

A task can move through a workflow such as:

```text
Todo
  ↓
In Progress
  ↓
Review
  ↓
Done
```

For the first implementation, keep the workflow deliberately simple.

---

# 5. Core Architectural Goal

Project 01 may have felt roughly like:

```text
HTTP Request
    ↓
Controller / Endpoint
    ↓
Service
    ↓
EF Core
    ↓
Database
```

Project 02 extends that into:

```text
Next.js / Browser
        ↓
      HTTP
        ↓
ASP.NET Core API
        ↓
Authentication
        ↓
Authorization
        ↓
Application / Service Logic
        ↓
EF Core
        ↓
PostgreSQL
```

Do **not** turn Project 02 into a microservice project.

Do **not** add RabbitMQ, MassTransit, sagas, distributed events, Kubernetes, or service-to-service messaging unless there is a very specific learning reason later.

Those concepts belong to later projects.

Project 02 should first make the fundamentals of a secured full-stack application solid.

---

# 6. Recommended Repository Shape

One reasonable structure:

```text
team-task-manager/
│
├── backend/
│   ├── TeamTaskManager.sln
│   └── src/
│       └── TeamTaskManager.Api/
│           ├── Features/
│           ├── Models/
│           ├── Contracts/
│           ├── Data/
│           ├── Auth/
│           ├── Authorization/
│           ├── Services/
│           ├── Common/
│           ├── Program.cs
│           └── appsettings.json
│
├── frontend/
│   ├── app/
│   ├── components/
│   ├── features/
│   ├── lib/
│   ├── types/
│   └── package.json
│
├── docs/
│   └── PROJECT_02_MASTER_PLAN.md
│
└── README.md
```

This exact folder structure is **not mandatory**.

The important thing is to maintain clear responsibilities.

---

# 7. Domain Model

## 7.1 User

Represents an authenticated application user.

Possible responsibilities:

```text
User
- Id
- Email
- DisplayName
- PasswordHash        // if identity system exposes/owns it
- CreatedAt
- ModifiedAt
```

Prefer ASP.NET Core Identity or another established identity system instead of designing password storage yourself.

### User is responsible for

- login identity,
- profile information,
- membership references,
- ownership/audit references.

### User is NOT responsible for

```text
IsAdminForEveryProject
```

A user's permissions should often depend on the resource being accessed.

---

# 8. Workspace

A workspace is the top-level collaboration boundary.

Example:

```text
Workspace
Id: 8...
Name: "Acme Software"
```

Potential fields:

```csharp
Workspace
{
    Id
    Name
    CreatedAt
    CreatedById
}
```

A workspace may contain many projects.

```text
Workspace 1 ──────── * Project
```

Possible future concept:

```text
WorkspaceMember
```

The original required entities do not explicitly require a separate `WorkspaceMember`, so do not add it automatically before deciding how workspace membership will be represented.

For the first version, project membership can be the strongest resource-level authorization boundary if that keeps the implementation simpler.

---

# 9. Project

A project belongs to a workspace.

Possible fields:

```csharp
Project
{
    Id
    WorkspaceId
    Name
    Description
    CreatedAt
    CreatedById
}
```

Relationship:

```text
Workspace
   │
   └──── Project
            │
            ├──── ProjectMember
            └──── TaskItem
```

The project is one of the most important authorization resources.

---

# 10. ProjectMember

This entity connects:

```text
User ↔ Project
```

It exists because a user can have different permissions in different projects.

Example:

```text
Sadman
├── Project A → Owner
├── Project B → Member
└── Project C → no membership
```

Possible fields:

```csharp
ProjectMember
{
    Id
    ProjectId
    UserId
    Role
    JoinedAt
}
```

Possible role enum:

```csharp
public enum ProjectRole
{
    Owner,
    Admin,
    Member,
    Viewer
}
```

For the first version, you may simplify this to:

```text
Owner
Member
```

and add more roles only when required.

## Important database rule

Usually this should be unique:

```text
(ProjectId, UserId)
```

A user should not accidentally have duplicate membership rows for the same project.

---

# 11. TaskItem

`TaskItem` is the central business entity.

The name `TaskItem` is intentionally safer than `Task`, because `Task` already has a major meaning in C# asynchronous programming.

Possible fields:

```csharp
TaskItem
{
    Id
    ProjectId

    Title
    Description

    Status
    Priority

    AssigneeId?
    CreatedById

    DueDate?

    CreatedAt
    ModifiedAt
}
```

Potential status enum:

```csharp
public enum TaskStatus
{
    Todo,
    InProgress,
    Review,
    Done
}
```

Potential priority enum:

```csharp
public enum TaskPriority
{
    Low,
    Medium,
    High,
    Critical
}
```

Do not over-model at the beginning.

Start with:

```text
Title
Description
Status
Assignee
```

and evolve later.

---

# 12. Comment

A comment belongs to a task and a user.

Possible fields:

```csharp
Comment
{
    Id
    TaskItemId
    AuthorId
    Body
    CreatedAt
    ModifiedAt
}
```

Relationship:

```text
TaskItem
   │
   └──── * Comment
              │
              └──── User
```

Authorization rule:

> A user should only be able to read/write comments for tasks they are authorized to access.

Checking only the `Comment.Id` is not enough.

You need to trace:

```text
Comment
  ↓
Task
  ↓
Project
  ↓
ProjectMember
  ↓
Current User
```

---

# 13. Label

Labels classify tasks.

Example:

```text
bug
frontend
backend
urgent
design
```

Possible design:

```csharp
Label
{
    Id
    ProjectId
    Name
}
```

A task can have many labels and a label can belong to many tasks.

Therefore, EF Core may require a many-to-many relationship.

Conceptually:

```text
TaskItem * ───── * Label
```

EF Core can either create an implicit join table or you can explicitly model one.

For learning, either is acceptable.

---

# 14. Relationship Overview

The rough ER model:

```text
User
 │
 │ 1
 │
 │ *
ProjectMember
 │ *
 │
 │ 1
Project
 │
 ├────────────── Workspace
 │
 ├── * TaskItem
 │        │
 │        ├── * Comment
 │        │
 │        └── * Label
 │
 └── * ProjectMember
```

Another way to think about it:

```text
Workspace
    │
    └── Projects
          │
          ├── Members ── Users
          │
          └── Tasks
               ├── Assignee ── User
               ├── Creator ─── User
               ├── Comments ── Users
               └── Labels
```

---

# 15. Authentication

Authentication answers:

> **Who are you?**

Typical login flow:

```text
Browser
   │
   │ POST /api/auth/login
   ▼
ASP.NET API
   │
   ├── verify email/password
   │
   ├── issue access token
   │
   └── issue/rotate refresh token
   ▼
Browser/session state
```

Then authenticated requests carry the access token:

```http
Authorization: Bearer <access-token>
```

The backend validates the token and constructs the current authenticated user principal.

---

# 16. Password Security

Do not manually store:

```text
Password = "hello123"
```

Do not invent a custom hash algorithm.

Use:

- ASP.NET Core Identity,
- or another established password-hashing/identity mechanism.

Understand conceptually:

```text
password
   ↓
password hashing algorithm
   ↓
salted password hash
   ↓
database
```

The application verifies passwords through the identity/password-hashing mechanism.

---

# 17. Access Token vs Refresh Token

## Access token

Usually:

- relatively short lived,
- sent with API requests,
- contains/represents authentication claims.

Example conceptual claims:

```text
sub = user ID
email = user's email
name = display name
```

Do not put every project permission inside the token.

Project membership can change while a token is still alive.

Resource-level authorization should check current application/database state where appropriate.

## Refresh token

Refresh tokens allow a user to obtain a new access token without typing their password again.

Conceptual flow:

```text
Access token expires
      ↓
Frontend calls refresh endpoint
      ↓
Backend validates refresh token
      ↓
Old token may be rotated/revoked
      ↓
New access token issued
```

Important security concepts to eventually learn:

- refresh-token expiration,
- rotation,
- revocation,
- reuse detection,
- secure storage,
- logout invalidation.

Do not build an unnecessarily sophisticated token platform before basic auth works.

---

# 18. Authentication vs Authorization

This distinction must be understood clearly.

## Authentication

```text
"Who are you?"
```

Example:

```text
Current user = Alice
```

## Authorization

```text
"What is Alice allowed to do?"
```

Example:

```text
Alice is a member of Project A
→ may view Project A tasks

Alice is not a member of Project B
→ may NOT view Project B tasks
```

Authentication alone does not secure project data.

---

# 19. The Main Security Requirement

This is the central Project 02 security requirement.

Suppose:

```text
Project X

Members:
- Alice
- Bob

Unrelated user:
- Charlie
```

Alice may call:

```http
GET /api/projects/{projectXId}/tasks
```

Bob may call it too.

Charlie must NOT obtain Project X's tasks.

Even if Charlie discovers a real task identifier:

```http
GET /api/tasks/4cdd...
```

the backend must still reject unauthorized access.

Possible response:

```http
403 Forbidden
```

or, depending on your security design, `404 Not Found` may intentionally hide resource existence.

The exact choice can be decided later.

The important requirement is:

> **Changing an identifier manually must not bypass authorization.**

---

# 20. Resource-Level Authorization

Avoid thinking only in terms of:

```csharp
[Authorize(Roles = "Admin")]
```

That may be useful for global administrative actions, but it is not enough for project collaboration.

Instead, think:

```text
Current user
      ↓
Requested task
      ↓
Task.ProjectId
      ↓
ProjectMember exists?
      ↓
Role permits requested operation?
```

Example authorization logic:

```text
CanReadTask(userId, taskId)
    ↓
load task/project relationship
    ↓
check project membership
    ↓
allow / deny
```

For updates:

```text
CanUpdateTask
```

may require:

```text
Owner/Admin/Member
```

while deleting an entire project may require:

```text
Owner
```

---

# 21. Authorization Matrix

Start with a simple permission matrix.

| Action | Owner | Member | Unrelated User |
|---|---:|---:|---:|
| View project | ✅ | ✅ | ❌ |
| View tasks | ✅ | ✅ | ❌ |
| Create task | ✅ | ✅ | ❌ |
| Update normal task | ✅ | ✅ | ❌ |
| Comment | ✅ | ✅ | ❌ |
| Assign task | ✅ | ✅ | ❌ |
| Add member | ✅ | ❌ | ❌ |
| Remove member | ✅ | ❌ | ❌ |
| Delete project | ✅ | ❌ | ❌ |

Do not blindly implement this table if product requirements later change.

It is the initial learning model.

---

# 22. API Contracts and DTOs

Do not return EF Core entities directly.

Use request and response DTOs.

Example:

```csharp
public sealed class CreateTaskRequest
{
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Guid? AssigneeId { get; init; }
}
```

Possible response:

```csharp
public sealed class TaskResponse
{
    public required Guid Id { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public required string Status { get; init; }
    public UserSummaryResponse? Assignee { get; init; }
}
```

Reasons for DTOs:

- API contracts remain explicit,
- database internals remain hidden,
- you control what the frontend sees,
- navigation cycles are avoided,
- versioning/refactoring becomes safer,
- validation boundaries are clearer.

---

# 23. Initial API Sketch

These routes are a **working design**, not an immutable specification.

## Authentication

```http
POST /api/auth/register
POST /api/auth/login
POST /api/auth/refresh
POST /api/auth/logout
GET  /api/users/me
```

## Workspaces

```http
POST /api/workspaces
GET  /api/workspaces
GET  /api/workspaces/{workspaceId}
```

## Projects

```http
POST /api/workspaces/{workspaceId}/projects
GET  /api/workspaces/{workspaceId}/projects
GET  /api/projects/{projectId}
PUT  /api/projects/{projectId}
DELETE /api/projects/{projectId}
```

## Project members

```http
GET    /api/projects/{projectId}/members
POST   /api/projects/{projectId}/members
DELETE /api/projects/{projectId}/members/{userId}
```

## Tasks

```http
POST   /api/projects/{projectId}/tasks
GET    /api/projects/{projectId}/tasks
GET    /api/tasks/{taskId}
PUT    /api/tasks/{taskId}
DELETE /api/tasks/{taskId}
```

Possible workflow-specific endpoint:

```http
PATCH /api/tasks/{taskId}/status
```

Possible assignment endpoint:

```http
PATCH /api/tasks/{taskId}/assignee
```

Whether these are separate or part of `PUT/PATCH` can be decided during implementation.

## Comments

```http
POST   /api/tasks/{taskId}/comments
GET    /api/tasks/{taskId}/comments
PUT    /api/comments/{commentId}
DELETE /api/comments/{commentId}
```

## Labels

```http
POST   /api/projects/{projectId}/labels
GET    /api/projects/{projectId}/labels
POST   /api/tasks/{taskId}/labels/{labelId}
DELETE /api/tasks/{taskId}/labels/{labelId}
```

---

# 24. HTTP Status Codes to Practice

Typical cases:

```text
200 OK
201 Created
204 No Content
400 Bad Request
401 Unauthorized
403 Forbidden
404 Not Found
409 Conflict
```

Remember:

## 401 Unauthorized

Usually means:

```text
"You are not authenticated correctly."
```

## 403 Forbidden

Usually means:

```text
"You are authenticated, but not allowed to do this."
```

These two are commonly confused.

---

# 25. Backend Validation

Validate requests at API boundaries.

Examples:

```text
Task title:
- required
- sensible maximum length

Description:
- optional
- sensible maximum length

Project name:
- required

Comment body:
- required
- must not be whitespace

Assignee:
- must refer to an allowed project member
```

Validation is not only syntax.

For example:

```text
AssigneeId is a valid GUID
```

is structural validation.

But:

```text
AssigneeId belongs to a member of this project
```

is a business/domain validation rule.

Learn to distinguish the two.

---

# 26. EF Core Configuration

Things to practice explicitly:

- primary keys,
- required/optional fields,
- string lengths,
- foreign keys,
- cascade behavior,
- indexes,
- unique indexes,
- many-to-many relationships,
- migrations.

Potential constraints:

```text
ProjectMember:
UNIQUE(ProjectId, UserId)

Label:
possibly UNIQUE(ProjectId, Name)
```

Indexes worth considering later:

```text
TaskItem(ProjectId)
TaskItem(ProjectId, Status)
TaskItem(AssigneeId)
Comment(TaskItemId)
```

Do not add indexes randomly.

Add them because a query/access pattern needs them.

---

# 27. Migrations

The learning cycle should be:

```text
Change model/configuration
      ↓
Create migration
      ↓
Read migration
      ↓
Apply migration
      ↓
Inspect database
```

Useful commands will depend on the final solution structure, but conceptually:

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

Do not treat a generated migration as magic.

Read what it changes.

---

# 28. Backend Query Patterns

You should practice:

```text
Where
Select
OrderBy
OrderByDescending
Skip
Take
AnyAsync
SingleOrDefaultAsync
CountAsync
AsNoTracking
```

A typical task list flow may become:

```text
authorized project
      ↓
base tasks query
      ↓
optional search
      ↓
optional status filter
      ↓
optional assignee filter
      ↓
count
      ↓
sort
      ↓
Skip / Take
      ↓
Select response DTO
```

Example mental pattern:

```csharp
var query = dbContext.TaskItems
    .AsNoTracking()
    .Where(x => x.ProjectId == projectId);

if (!string.IsNullOrWhiteSpace(search))
{
    query = query.Where(x => x.Title.Contains(search));
}

var totalItems = await query.CountAsync();

var items = await query
    .OrderByDescending(x => x.CreatedAt)
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(x => new TaskResponse
    {
        // projection
    })
    .ToListAsync();
```

The final code may differ.

The important idea is understanding the stages.

---

# 29. Search, Filtering, Sorting, Paging

The blueprint specifically asks for these.

Possible query:

```http
GET /api/projects/{projectId}/tasks
    ?search=login
    &status=InProgress
    &assigneeId=...
    &sort=createdAt
    &direction=desc
    &page=1
    &pageSize=20
```

Possible response:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalItems": 53,
  "totalPages": 3
}
```

Do not implement every filter on day one.

Suggested order:

```text
1. Basic list
2. Paging
3. Search
4. Status filter
5. Assignee filter
6. Sorting
```

---

# 30. Frontend Goal

Frontend technology:

```text
Next.js + React + TypeScript
```

Main screens:

```text
Login
Register
Dashboard
Workspace page
Project board
Task detail
Project member management
```

Possible navigation:

```text
Login
  ↓
Dashboard
  ↓
Workspace
  ↓
Project
  ↓
Board
  ↓
Task Detail
```

---

# 31. Project Board UI

Initial board:

```text
┌──────────────┬──────────────┬──────────────┬──────────────┐
│ TODO         │ IN PROGRESS  │ REVIEW       │ DONE         │
├──────────────┼──────────────┼──────────────┼──────────────┤
│ Build login  │ Dashboard    │ API docs     │ DB setup     │
│ Navbar       │ Search       │              │              │
│ Profile      │              │              │              │
└──────────────┴──────────────┴──────────────┴──────────────┘
```

Do not start by implementing drag-and-drop.

First make status changes work through a normal control/button.

Example:

```text
Task Details
Status: [In Progress ▼]

Save
```

Once the backend state transition works reliably, drag-and-drop can become an enhancement.

---

# 32. Task Detail UI

Task detail should eventually display:

```text
Title
Description
Status
Priority
Assignee
Labels
Created by
Created date
Comments
```

Possible actions:

```text
Edit task
Change status
Assign user
Add/remove label
Add comment
Delete task (if permitted)
```

---

# 33. Frontend API Client

Avoid scattering raw `fetch()` calls everywhere.

Create an API layer.

Example concept:

```text
lib/api/
├── auth.ts
├── workspaces.ts
├── projects.ts
├── tasks.ts
└── comments.ts
```

or:

```text
features/tasks/api.ts
```

A consistent API client should eventually handle:

- base API URL,
- JSON headers,
- auth credentials/token,
- error parsing,
- refresh logic if applicable,
- typed request/response models.

---

# 34. Frontend State

Keep state simple at first.

Use local/component state where appropriate.

Examples:

```text
form state
modal open/closed
loading
error
selected task
filters
```

Do not add Redux automatically.

If server-state complexity grows, a tool such as TanStack Query may become useful, but first understand what problem it solves.

---

# 35. Loading and Error States

Every async UI needs deliberate states.

Example:

```text
Loading project...
```

Then:

```text
Could not load project.
[Try again]
```

Do not assume every network request succeeds.

Practice handling:

```text
400
401
403
404
500
network failure
timeout
```

---

# 36. CORS

Because frontend and backend may run on different origins during development:

```text
Frontend:
http://localhost:3000

Backend:
https://localhost:7xxx
```

the backend may require CORS configuration.

Understand:

> CORS is a browser security policy/configuration issue.  
> CORS is **not authentication** and is **not authorization**.

A permissive CORS policy does not mean an endpoint is authorized.

---

# 37. AI Feature — Controlled Task Breakdown

The blueprint asks for a small AI enhancement.

Input:

```text
Task title:
"Implement password reset"
```

Possible AI output:

```json
{
  "description": "Implement a secure password reset flow...",
  "subtasks": [
    "Create forgot-password endpoint",
    "Generate reset token",
    "Create reset-password endpoint",
    "Add frontend reset form"
  ],
  "checklist": [
    "Validate token expiration",
    "Handle invalid tokens",
    "Do not reveal whether an email exists",
    "Add tests"
  ]
}
```

The AI output should be structured JSON.

Do not simply accept arbitrary free-form text and blindly write it to the database.

Flow:

```text
User enters task title
      ↓
Frontend requests AI suggestion
      ↓
Backend sends controlled prompt
      ↓
LLM returns structured output
      ↓
Backend validates output
      ↓
Frontend displays suggestion
      ↓
User reviews/edits
      ↓
User accepts
      ↓
Task fields updated
```

Key principle:

> **AI proposes. User decides.**

The AI feature should remain optional and reversible.

---

# 38. AI Safety / Engineering Boundaries

For Project 02:

Do:

- validate structured model output,
- use a clear schema,
- handle invalid AI output,
- allow editing,
- allow rejecting suggestions,
- set request timeouts,
- log errors without exposing secrets.

Do not:

- allow AI-generated SQL,
- give the model direct unrestricted database access,
- silently mutate project data,
- trust AI output without validation,
- place API keys in frontend JavaScript.

LLM API keys must stay server-side.

---

# 39. Suggested Implementation Phases

---

## Phase 0 — Project Setup

### Goal

Create the backend/frontend skeletons and establish local development.

### Checklist

- [ ] Create Git repository
- [ ] Create backend solution/project
- [ ] Create Next.js TypeScript frontend
- [ ] Add `.gitignore`
- [ ] Configure environment variables
- [ ] Configure PostgreSQL
- [ ] Add EF Core packages
- [ ] Add initial health/test endpoint
- [ ] Confirm frontend runs
- [ ] Confirm backend runs
- [ ] Confirm database connection

### Done when

```text
Frontend starts
Backend starts
Backend can reach PostgreSQL
Repository is clean and understandable
```

---

# 40. Phase 1 — Domain + Database Foundation

### Goal

Model the collaboration domain before introducing complex authentication behavior.

### Minimum entities

- [ ] User/Identity integration decided
- [ ] Workspace
- [ ] Project
- [ ] ProjectMember
- [ ] TaskItem
- [ ] Comment
- [ ] Label

### EF configuration

- [ ] Primary keys
- [ ] Foreign keys
- [ ] Required/optional properties
- [ ] String lengths
- [ ] Delete behavior
- [ ] Unique ProjectMember index
- [ ] Useful initial indexes

### Database

- [ ] Initial migration
- [ ] Migration reviewed
- [ ] Database created
- [ ] Tables inspected

### Done when

You can explain every entity and relationship without looking at code.

---

# 41. Phase 2 — Authentication

### Goal

Users can register/login and the backend knows the authenticated user's identity.

### Checklist

- [ ] Identity approach selected
- [ ] Register endpoint
- [ ] Login endpoint
- [ ] Password hashing handled by established mechanism
- [ ] Access token generation
- [ ] Authentication middleware
- [ ] Current user endpoint
- [ ] Protected test endpoint
- [ ] Expired token behavior understood
- [ ] Invalid token behavior tested

### Refresh-token lifecycle

- [ ] Refresh-token model/storage decision
- [ ] Refresh endpoint
- [ ] Expiration
- [ ] Rotation or replacement strategy
- [ ] Logout/revocation behavior
- [ ] Refresh failure behavior

### Done when

```text
Anonymous request → rejected
Valid login → token issued
Valid token → current user identified
Expired/invalid token → rejected correctly
Refresh path works
```

---

# 42. Phase 3 — Workspace and Project Authorization

### Goal

Introduce resource-level security.

### Checklist

- [ ] Create workspace
- [ ] Create project
- [ ] Add project member
- [ ] List project members
- [ ] Remove member
- [ ] Project role represented
- [ ] Authorization service/helper/policy exists
- [ ] Member can access project
- [ ] Unrelated user cannot access project
- [ ] Owner-only action implemented

### Critical security tests

- [ ] User A creates Project X
- [ ] User B joins Project X
- [ ] User B can read Project X
- [ ] User C cannot read Project X
- [ ] User C guesses Project X ID → still denied
- [ ] User C guesses Task ID belonging to Project X → still denied

### Done when

Identifier guessing does not bypass authorization.

---

# 43. Phase 4 — Task CRUD

### Goal

Create the core task workflow.

### Checklist

- [ ] Create task
- [ ] Get task by ID
- [ ] List project tasks
- [ ] Update task
- [ ] Delete task
- [ ] Status enum
- [ ] Status transition
- [ ] Assignee
- [ ] Assignee must be valid project member
- [ ] DTO validation
- [ ] Resource authorization on every task operation

### Done when

Two project members can collaboratively manage tasks while unrelated users cannot access them.

---

# 44. Phase 5 — Comments and Labels

### Comments

- [ ] Add comment
- [ ] List task comments
- [ ] Edit own/allowed comment
- [ ] Delete own/allowed comment
- [ ] Project membership enforced

### Labels

- [ ] Create project label
- [ ] List labels
- [ ] Attach label to task
- [ ] Remove label
- [ ] Duplicate-name behavior decided

### Done when

Task details contain useful collaboration data without authorization gaps.

---

# 45. Phase 6 — Search, Filtering, Sorting, Paging

### Checklist

- [ ] Page/pageSize DTO
- [ ] Maximum page size
- [ ] Total item count
- [ ] Search by title
- [ ] Filter by status
- [ ] Filter by assignee
- [ ] Optional label filter
- [ ] Sorting
- [ ] `AsNoTracking()` on appropriate read queries
- [ ] Projection to response DTO

### Done when

The backend can support a realistic task board/list without returning every row.

---

# 46. Phase 7 — Next.js Authentication UI

### Checklist

- [ ] Register page
- [ ] Login page
- [ ] Logout
- [ ] Current-user state
- [ ] Protected route/page behavior
- [ ] API auth handling
- [ ] 401 handling
- [ ] Token refresh behavior
- [ ] Loading state
- [ ] Error state

### Done when

A user can open the app, log in, refresh the page, and continue using authenticated features appropriately.

---

# 47. Phase 8 — Workspace / Project UI

### Checklist

- [ ] Dashboard
- [ ] Workspace list
- [ ] Create workspace
- [ ] Project list
- [ ] Create project
- [ ] Project detail page
- [ ] Members view
- [ ] Add/remove member UI
- [ ] Authorization-aware actions

Important:

Hiding an unauthorized button is good UX.

It is **not security**.

Backend authorization must still exist.

---

# 48. Phase 9 — Board + Task UI

### Checklist

- [ ] Board columns
- [ ] Task cards
- [ ] Create task form
- [ ] Task detail modal/page
- [ ] Edit task
- [ ] Change status
- [ ] Assign user
- [ ] Comments
- [ ] Labels
- [ ] Search/filter UI
- [ ] Paging/load-more strategy if needed
- [ ] Loading/error/empty states

### Enhancement later

- [ ] Drag and drop
- [ ] Optimistic update
- [ ] Revert optimistic update on failure

Do not make drag-and-drop block the core project.

---

# 49. Phase 10 — AI Task Assistant

### Checklist

- [ ] Backend AI endpoint
- [ ] Server-side API key
- [ ] Prompt designed
- [ ] Structured response schema
- [ ] Output validation
- [ ] Error handling
- [ ] User review UI
- [ ] Accept suggestion
- [ ] Edit suggestion
- [ ] Reject suggestion
- [ ] No automatic destructive writes

### Done when

AI helps the user write a better task without becoming a trusted authority or direct database agent.

---

# 50. Phase 11 — Testing and Hardening

## Unit tests

Possible areas:

- [ ] validation
- [ ] permission decisions
- [ ] workflow/state rules

## Integration tests

Highest value:

- [ ] register/login
- [ ] create project
- [ ] authorized member gets task
- [ ] unauthorized user cannot get task
- [ ] unauthorized user cannot update task
- [ ] create task persists correctly
- [ ] paging works
- [ ] invalid assignee rejected
- [ ] comment authorization works

## Security checks

- [ ] identifier guessing
- [ ] missing token
- [ ] expired token
- [ ] invalid token
- [ ] removed project member loses access
- [ ] user cannot assign unrelated user
- [ ] user cannot manipulate foreign project IDs

---

# 51. Phase 12 — Final Polish

Possible finishing work:

- [ ] consistent API errors
- [ ] logging
- [ ] clean validation messages
- [ ] README
- [ ] seed/demo data
- [ ] screenshots
- [ ] architecture diagram
- [ ] deployment
- [ ] environment documentation
- [ ] API collection / OpenAPI cleanup

---

# 52. Suggested Development Order

Recommended concrete order:

```text
1. Backend skeleton
2. PostgreSQL connection
3. Identity/User
4. Workspace
5. Project
6. ProjectMember
7. Authentication
8. Project authorization
9. Task CRUD
10. Task authorization tests
11. Comments
12. Labels
13. Search/paging/filter
14. Frontend login
15. Frontend dashboard
16. Project board
17. Task detail
18. AI helper
19. Integration/security tests
20. Final cleanup
```

One valid alternative is implementing authentication before all domain entities.

The exact order is less important than maintaining small verified steps.

---

# 53. What NOT to Add Yet

Avoid premature complexity.

Do not add these simply because they sound professional:

```text
RabbitMQ
MassTransit
Kafka
Microservices
Kubernetes
Event sourcing
CQRS framework
Redis
Elasticsearch
GraphQL
WebSockets
SignalR
Distributed cache
Service mesh
```

They can be studied later.

Project 02 already has enough complexity through:

```text
Auth
Refresh tokens
Resource authorization
Relational data
Next.js
Search/paging
AI integration
Testing
```

---

# 54. Important Design Questions to Answer During Development

Keep these questions in mind:

### Domain

- Why does `ProjectMember` exist?
- Is membership workspace-level, project-level, or both?
- Can a task have no assignee?
- Can a task be assigned to someone outside the project?
- Can labels be reused across projects?

### Security

- Who can create a project?
- Who can add/remove members?
- Who can delete tasks?
- What happens after a member is removed?
- Should unauthorized resource access return `403` or `404`?

### Data

- Which foreign keys are required?
- Which delete behaviors should cascade?
- Which entities need unique indexes?
- Which list queries need indexes?

### API

- PUT vs PATCH?
- Are task state transitions generic updates or a dedicated action?
- What data should list responses include?
- What data belongs only in detail responses?

### Frontend

- Where should authentication state live?
- How are API errors represented?
- How is refresh handled?
- When should optimistic UI be used?

---

# 55. Example Security Scenario

This scenario should eventually pass.

## Users

```text
Alice
Bob
Charlie
```

## Project

```text
Project: Phoenix
```

## Membership

```text
Alice → Owner
Bob   → Member
Charlie → no membership
```

## Expected behavior

```text
Alice:
GET task        ✅
CREATE task     ✅
UPDATE task     ✅
ADD member      ✅

Bob:
GET task        ✅
CREATE task     ✅
UPDATE task     ✅
ADD member      ❌

Charlie:
GET task        ❌
CREATE task     ❌
UPDATE task     ❌
COMMENT         ❌
```

Now Charlie manually copies a valid task ID:

```http
GET /api/tasks/real-task-guid
```

Expected:

```text
DENIED
```

That test is more important than whether a button is hidden in the frontend.

---

# 56. End-to-End Success Scenario

The final project should support this story.

```text
Alice registers
    ↓
Alice logs in
    ↓
Alice creates Workspace "Acme"
    ↓
Alice creates Project "Website Redesign"
    ↓
Alice adds Bob as a project member
    ↓
Alice creates Task "Build login page"
    ↓
Alice assigns Bob
    ↓
Bob logs in
    ↓
Bob sees the project
    ↓
Bob opens the task
    ↓
Bob changes status to In Progress
    ↓
Bob adds a comment
    ↓
Alice sees the update
```

Meanwhile:

```text
Charlie logs in
    ↓
Charlie guesses task ID
    ↓
Backend refuses access
```

That is the core Project 02 demonstration.

---

# 57. Definition of Done

Project 02 is functionally complete when all critical points below are true.

## Authentication

- [ ] Register works
- [ ] Login works
- [ ] Access token works
- [ ] Refresh flow works
- [ ] Logout/revocation behavior exists
- [ ] Protected API requires authentication

## Collaboration

- [ ] Workspace exists
- [ ] Projects exist
- [ ] Project membership works
- [ ] Two users can collaborate

## Tasks

- [ ] Create
- [ ] Read
- [ ] Update
- [ ] Delete
- [ ] Assign
- [ ] Change status
- [ ] Comment
- [ ] Labels

## Authorization

- [ ] Non-member cannot view project
- [ ] Non-member cannot view task
- [ ] Non-member cannot update task
- [ ] Identifier guessing does not bypass security
- [ ] Role-restricted project actions work

## Querying

- [ ] Search
- [ ] Filter
- [ ] Sort
- [ ] Page

## Frontend

- [ ] Login UI
- [ ] Dashboard
- [ ] Project UI
- [ ] Board
- [ ] Task detail
- [ ] Loading states
- [ ] Error states

## AI

- [ ] Task title can request suggestion
- [ ] Structured response
- [ ] Response validated
- [ ] User can edit
- [ ] User can accept/reject

## Testing

- [ ] Core integration tests
- [ ] Authorization tests
- [ ] Validation tests

---

# 58. Interview / Explain-Back Questions

By the end, you should be able to answer these without memorized definitions.

## Authentication

1. What is authentication?
2. What is authorization?
3. What is an access token?
4. What happens when an access token expires?
5. Why do refresh tokens exist?
6. Why should password hashing use an established mechanism?
7. What are claims?

## Authorization

8. Why is `[Authorize]` alone not enough?
9. What is resource-level authorization?
10. Why does hiding a button in React not secure an API?
11. Why do we need `ProjectMember`?
12. Why should a task access check follow task → project → membership?
13. What happens when a user's membership is removed?

## EF Core

14. What is a foreign key?
15. What is a navigation property?
16. What is a unique index?
17. Why could `(ProjectId, UserId)` be unique?
18. What is a migration?
19. What does `AsNoTracking()` do?
20. Why use projection instead of returning entities?

## API

21. DTO vs entity?
22. 401 vs 403?
23. Why paginate?
24. Why count before `Skip/Take` when total count is required?
25. Why validate `AssigneeId` against project membership?

## Frontend

26. Why have an API client layer?
27. What is server state?
28. What is optimistic UI?
29. When can optimistic UI be risky?
30. What should happen when an API returns 403?

## AI

31. Why use structured JSON output?
32. Why validate model output?
33. Why should the user approve generated suggestions?
34. Why should the LLM key stay on the backend?
35. Why shouldn't the LLM write SQL directly?

---

# 59. Mistakes to Avoid

## Backend mistakes

- returning EF entities directly,
- trusting `ProjectId` supplied by client without authorization checks,
- checking authentication but not membership,
- putting every permission into a global role,
- storing plaintext passwords,
- manually inventing crypto,
- swallowing exceptions,
- returning `200` for every outcome,
- skipping validation,
- forgetting cancellation tokens where appropriate,
- loading huge tables into memory before filtering,
- allowing arbitrary page sizes.

## Frontend mistakes

- assuming hidden buttons equal security,
- placing secret keys in frontend `.env` exposed to browser code,
- duplicating fetch/error logic everywhere,
- ignoring loading/error states,
- trusting browser state as authorization truth.

## Database mistakes

- missing foreign keys,
- duplicate project memberships,
- careless cascade deletes,
- no migrations,
- never reviewing generated migrations,
- indexing every column without reason.

## AI mistakes

- trusting model output,
- automatic destructive actions,
- sending secrets in prompts,
- allowing free-form output when the app expects structured fields.

---

# 60. Project Learning Journal

Use this section while working.

## Session Template

Copy this block for every meaningful coding session:

```md
### YYYY-MM-DD — Session Title

**Goal:**
-

**Implemented:**
-

**Files changed:**
-

**What I learned:**
-

**Problems / bugs:**
-

**Decisions made:**
-

**Still unclear:**
-

**Next step:**
-
```

---

# 61. Architecture Decision Log

Use this when you make decisions we should remember later.

Example:

```md
### ADR-001 — Use ASP.NET Core Identity

**Date:** YYYY-MM-DD

**Decision:**
Use ASP.NET Core Identity for user/password management.

**Reason:**
Avoid designing custom password hashing and identity storage.

**Consequences:**
Application User integrates with IdentityUser.
JWT/access-token issuance remains application/API responsibility.
```

Suggested decisions to record:

- authentication mechanism,
- refresh-token storage,
- role model,
- workspace membership model,
- authorization approach,
- task status model,
- soft delete or hard delete,
- frontend auth storage,
- API error format.

---

# 62. Current Progress Snapshot

> **IMPORTANT:** Keep this section current.  
> This is the first place ChatGPT should read when asked:  
> **"What have we done so far?"**

## Overall status

```text
Status: NOT STARTED / IN PROGRESS / PAUSED / COMPLETE
Current phase:
Current branch:
Last updated:
```

## Completed

- Nothing recorded yet.

## In progress

- Project planning / architecture discussion.

## Next immediate task

- Decide the initial solution/repository structure.
- Decide the authentication approach.
- Create the first backend/frontend skeleton only when ready to begin implementation.

## Known issues

- None recorded yet.

## Open questions

- Will workspace membership be modeled separately or inferred through project membership?
- Which exact authentication/token strategy will be used?
- Will task deletion be hard-delete or soft-delete for this learning project?
- Which authorization style will be used: policies, authorization service, endpoint helpers, or a combination?

---

# 63. Progress Checklist — Master

## Setup

- [ ] Repository initialized
- [ ] Backend created
- [ ] Frontend created
- [ ] PostgreSQL configured
- [ ] Git ignore/configuration finished

## Data model

- [ ] User
- [ ] Workspace
- [ ] Project
- [ ] ProjectMember
- [ ] TaskItem
- [ ] Comment
- [ ] Label
- [ ] EF configurations
- [ ] Initial migration

## Auth

- [ ] Registration
- [ ] Login
- [ ] Access token
- [ ] Refresh token
- [ ] Logout
- [ ] Current user
- [ ] Protected endpoint

## Authorization

- [ ] Project membership check
- [ ] Project role check
- [ ] Task read authorization
- [ ] Task update authorization
- [ ] Task delete authorization
- [ ] Comment authorization
- [ ] Identifier-guessing tests

## Projects

- [ ] Create project
- [ ] Get project
- [ ] List projects
- [ ] Update project
- [ ] Delete project
- [ ] Add member
- [ ] Remove member

## Tasks

- [ ] Create
- [ ] Get
- [ ] List
- [ ] Update
- [ ] Delete
- [ ] Status
- [ ] Assignee
- [ ] Validation

## Comments

- [ ] Create
- [ ] List
- [ ] Update
- [ ] Delete

## Labels

- [ ] Create
- [ ] List
- [ ] Attach
- [ ] Remove

## Query features

- [ ] Search
- [ ] Filter
- [ ] Sort
- [ ] Paging

## Frontend

- [ ] Register
- [ ] Login
- [ ] Dashboard
- [ ] Workspace UI
- [ ] Project UI
- [ ] Board
- [ ] Task detail
- [ ] Comments
- [ ] Labels
- [ ] Filters
- [ ] Loading/error states

## AI

- [ ] AI endpoint
- [ ] Structured schema
- [ ] Validation
- [ ] Task description suggestion
- [ ] Subtask suggestion
- [ ] Checklist suggestion
- [ ] User review
- [ ] Accept/edit/reject

## Tests

- [ ] Backend unit tests
- [ ] Integration tests
- [ ] Auth tests
- [ ] Authorization tests
- [ ] Paging tests
- [ ] Validation tests

## Documentation

- [ ] README
- [ ] Environment setup
- [ ] API notes
- [ ] Architecture explanation
- [ ] Demo instructions

---

# 64. How to Use This File With ChatGPT

When returning to the project, attach or provide this file along with the relevant source files.

Useful prompts:

```text
Read PROJECT_02_MASTER_PLAN.md and inspect my current backend.
Tell me exactly what has been completed and update the progress checklist.
```

```text
Compare my current code against Project 02's Phase 3 authorization requirements.
Do not add new architecture unless necessary.
```

```text
Based on PROJECT_02_MASTER_PLAN.md, what is the smallest next task I should implement?
```

```text
Check whether ProjectMember authorization is implemented securely.
Test mentally for identifier guessing.
```

```text
Update only the Current Progress Snapshot and Master Checklist based on these files.
```

```text
I finished task CRUD. Review it against the Project 02 definition of done.
```

```text
Explain the code I have written so far in beginner-friendly Bangla + English.
```

---

# 65. Rules for Future ChatGPT Reviews

When reviewing Project 02 later:

1. Read **Current Progress Snapshot** first.
2. Inspect actual code before marking something complete.
3. Treat checkboxes as notes, not proof.
4. Verify authorization from backend code, not frontend visibility.
5. Do not introduce later-project architecture prematurely.
6. Prefer small next steps.
7. Distinguish:
   - implemented,
   - partially implemented,
   - planned,
   - tested,
   - verified.
8. When code and this document disagree, actual code is the implementation truth.
9. Update this document after important milestones.
10. Preserve the primary Project 02 learning goal: **secure multi-user full-stack development**.

---

# 66. Final Mental Model

By the end of this project, this flow should feel natural:

```text
User logs in
    ↓
Backend authenticates identity
    ↓
Frontend calls secured API
    ↓
Backend identifies current user
    ↓
Backend loads requested resource
    ↓
Backend checks project membership / role
    ↓
Backend validates requested operation
    ↓
EF Core reads or writes PostgreSQL
    ↓
API returns DTO
    ↓
Next.js updates UI
```

And the security rule should be automatic in your thinking:

```text
Knowing the ID of a resource
        ≠
Having permission to access it
```

That is the core lesson of Project 02.

---

# 67. Initial State

As of this document's creation:

```text
Planning: started
Implementation: not yet recorded
Authentication: not yet recorded
Database: not yet recorded
Backend task features: not yet recorded
Frontend: not yet recorded
AI feature: not yet recorded
Testing: not yet recorded
```

The next stage is to begin implementation in small, verifiable slices and continuously update the **Current Progress Snapshot**.

