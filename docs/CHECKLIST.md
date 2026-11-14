# Project 02 — Team Task Manager Checklist

> This file tracks **what has been done so far** and what remains.
> Keep it updated after each lesson or milestone.

## Current Status

- **Project:** Team Task Manager / Mini Jira
- **Current Phase:** Phase 1 — Domain + Database Foundation
- **Status:** In Progress
- **Next Lesson:** User + ProjectMember + ProjectRole

---

## ✅ Completed

### Project Structure

- [x] Created root project structure
- [x] Created `backend/`
- [x] Created `frontend/`
- [x] Created `docs/`
- [x] Added project master-plan documentation

### Backend Setup

- [x] Created ASP.NET Core Web API project
- [x] Backend solution/project builds successfully
- [x] Backend application runs successfully
- [x] Created backend folders:
  - [x] `Controllers/`
  - [x] `Data/`
  - [x] `Models/`
  - [x] `Contracts/`
  - [x] `Services/`
  - [x] `Auth/`
  - [x] `Authorization/`
  - [x] `Common/`

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
- [x] Installed/configured PostgreSQL provider
- [x] Configured PostgreSQL connection string
- [x] Created `AppDbContext`
- [x] Registered `AppDbContext` in `Program.cs`
- [x] Backend successfully connects to PostgreSQL
- [x] Database created successfully
- [x] Initial EF Core migration created/applied

### Domain Models

- [x] Created `Workspace`
- [x] Created `Project`
- [x] Added `Workspace.Id`
- [x] Added `Project.WorkspaceId`
- [x] Added `Project.Workspace` navigation property
- [x] Added `Workspace.Projects` collection navigation
- [x] Established one-to-many relationship:

```text
Workspace
   1
   │
   └──── *
       Project
```

### Problems Solved

- [x] Fixed EF migration command/project-path issue
- [x] Fixed `Workspace` namespace/type resolution error
- [x] Fixed `IdentityDbContext<,,>` namespace resolution in `AppDbContext.cs`
- [x] Aligned EF Core package versions (`8.0.11`) to eliminate `MSB3277` warning
- [x] Rebuilt successfully after fixes
- [x] Application ran successfully after fixes

---

## 🟡 In Progress

### Phase 1 — Domain + Database Foundation

Completed inside Phase 1:

- [x] Workspace entity
- [x] Project entity
- [x] Workspace → Project relationship
- [x] AppDbContext
- [x] PostgreSQL connection
- [x] Initial migration
- [x] Database creation

Still required for Phase 1:

- [x] User / Identity model decision
- [x] ProjectMember
- [x] ProjectRole
- [x] TaskItem
- [x] Comment
- [x] Label
- [x] Remaining entity relationships
- [x] Unique constraints
- [x] Useful indexes
- [x] Delete behaviors
- [x] String length configuration
- [x] Review generated migrations
- [ ] Inspect database tables manually

---

## ⏭️ Next Immediate Lesson

### User + ProjectMember + ProjectRole

Next tasks:

- [x] Decide how `User` will integrate with ASP.NET Core Identity
- [x] Create `ProjectMember`
- [x] Create `ProjectRole`
- [x] Connect `User` to `ProjectMember`
- [x] Connect `Project` to `ProjectMember`
- [x] Configure many-to-many relationship through `ProjectMember`
- [x] Add unique constraint:

```text
(ProjectId, UserId)
```

- [x] Create next migration
- [x] Apply migration
- [x] Inspect resulting database schema

Target mental model:

```text
User
  │
  ▼
ProjectMember
  │
  ▼
Project
```

---

## ❌ Not Started Yet

### Authentication

- [x] ASP.NET Core Identity integration
- [ ] Register
- [ ] Login
- [ ] Password hashing flow
- [ ] JWT access token
- [ ] Refresh token
- [ ] Logout
- [ ] Current-user endpoint

### Authorization

- [ ] Project membership checks
- [ ] Project roles
- [ ] Resource-level authorization
- [ ] Owner-only actions
- [ ] Unauthorized identifier-guessing protection

### Tasks

- [x] TaskItem model
- [ ] Create task
- [ ] Get task
- [ ] List tasks
- [ ] Update task
- [ ] Delete task
- [x] Task status
- [x] Task assignee

### Comments

- [x] Comment model
- [ ] Add comment
- [ ] List comments
- [ ] Update comment
- [ ] Delete comment

### Labels

- [x] Label model
- [ ] Create label
- [ ] List labels
- [x] Attach label to task
- [ ] Remove label from task

### Query Features

- [ ] Search
- [ ] Filtering
- [ ] Sorting
- [ ] Pagination

### Frontend Features

- [ ] Register page
- [ ] Login page
- [ ] Dashboard
- [ ] Workspace UI
- [ ] Project UI
- [ ] Project board
- [ ] Task detail
- [ ] Comments UI
- [ ] Labels UI
- [ ] Search/filter UI
- [ ] Loading states
- [ ] Error states

### AI Feature

- [ ] AI task-description endpoint
- [ ] Structured JSON output
- [ ] Subtask suggestions
- [ ] Checklist suggestions
- [ ] User review/edit/accept flow

### Testing

- [ ] Unit tests
- [ ] Integration tests
- [ ] Authentication tests
- [ ] Authorization tests
- [ ] Validation tests
- [ ] Identifier-guessing security tests

---

## Progress Summary

```text
Project setup                  ✅ Done
Backend Web API                ✅ Done
Frontend Next.js setup         ✅ Done
Tailwind + shadcn/ui           ✅ Done
EF Core + PostgreSQL           ✅ Done
AppDbContext                   ✅ Done
Workspace                      ✅ Done
Project                        ✅ Done
Workspace → Project relation   ✅ Done
Initial migration              ✅ Done
Database created               ✅ Done

User / Identity                ✅ Done
ProjectMember                  ✅ Done
ProjectRole                    ✅ Done
TaskItem, Comment, Label       ✅ Done

Authentication                 ❌ Not started
Authorization                  ❌ Not started
Task CRUD                      ❌ Not started
Comments                       ❌ Not started
Labels                         ❌ Not started
Frontend features              ❌ Not started
AI feature                     ❌ Not started
Testing                        ❌ Not started
```

## Last Known Working State

```text
Backend builds successfully.
Backend runs successfully.
Database has been created.
Workspace and Project are persisted through EF Core.
Frontend project structure exists.
No blocking issue is currently recorded.
```
