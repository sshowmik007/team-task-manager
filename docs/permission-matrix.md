# Team Task Manager (Mini Jira) — Permission Matrix

This document defines the Role-Based Access Control (RBAC) and Resource-Level Authorization rules implemented in the **Team Task Manager API**.

---

## 1. Permission Matrix Table

| Resource | Action | Endpoint | Anonymous | Authenticated (Non-Member) | Project Member | Task / Comment Author | Project Owner |
|---|---|---|:---:|:---:|:---:|:---:|:---:|
| **Auth** | Register Account | `POST /api/auth/register` | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Auth** | Login & Get JWT | `POST /api/auth/login` | ✅ | ✅ | ✅ | ✅ | ✅ |
| **Auth** | Get Current Profile | `GET /api/auth/me` | ❌ `401` | ✅ | ✅ | ✅ | ✅ |
| **Project** | Create Project | `POST /api/projects` | ❌ `401` | ✅ *(Becomes Owner)* | ✅ *(Becomes Owner)* | ✅ *(Becomes Owner)* | ✅ *(Becomes Owner)* |
| **Project** | View Project Details | `GET /api/projects/{id}` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Project** | Delete Project | `DELETE /api/projects/{id}` | ❌ `401` | ❌ `403` | ❌ `403` | ❌ `403` | ✅ |
| **Project** | Add Member | `POST /api/projects/{id}/members` | ❌ `401` | ❌ `403` | ❌ `403` | ❌ `403` | ✅ |
| **Project** | List Members | `GET /api/projects/{id}/members` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Project** | Update Member Role | `PUT /api/projects/{id}/members/{uid}/role` | ❌ `401` | ❌ `403` | ❌ `403` | ❌ `403` | ✅ |
| **Project** | Remove Member | `DELETE /api/projects/{id}/members/{uid}` | ❌ `401` | ❌ `403` | ❌ `403` | ❌ `403` | ✅ |
| **Task** | Create Task | `POST /api/projects/{id}/tasks` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Task** | List Project Tasks | `GET /api/projects/{id}/tasks` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Task** | View Single Task | `GET /api/tasks/{id}` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Task** | Update Task Details | `PUT /api/tasks/{id}` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Task** | Delete Task | `DELETE /api/tasks/{id}` | ❌ `401` | ❌ `403` | ❌ `403` *(if not creator)* | ✅ *(Own Task)* | ✅ *(Any Task)* |
| **Comment** | View Task Comments | `GET /api/tasks/{id}/comments` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Comment** | Add Comment | `POST /api/tasks/{id}/comments` | ❌ `401` | ❌ `403` | ✅ | ✅ | ✅ |
| **Comment** | Edit Comment | `PUT /api/comments/{id}` | ❌ `401` | ❌ `403` | ❌ `403` | ✅ *(Own Comment only)* | ❌ `403` *(Preserves thread integrity)* |
| **Comment** | Delete Comment | `DELETE /api/comments/{id}` | ❌ `401` | ❌ `403` | ❌ `403` *(if not author)* | ✅ *(Own Comment)* | ✅ *(Moderation power)* |

---

## 2. Authorization Hierarchy & Architecture

```text
HTTP Request
     │
     ▼
[Authorize] Attribute (Authentication Gate)
     │  Is JWT valid and unexpired?
     ├── NO  ──► 401 Unauthorized
     └── YES ──► Extract currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     │
                     ▼
             Resource Lookup (EF Core)
                     │  Does target entity exist?
                     ├── NO  ──► 404 Not Found
                     └── YES ──► Fetch Parent Project ID
                                     │
                                     ▼
                     Resource-Level Authorization (IDOR Defense)
                             │  Is currentUserId in ProjectMembers for this ProjectId?
                             ├── NO  ──► 403 Forbidden
                             └── YES ──► Check Role / Ownership Rules
                                             │
                                             ├── Project Owner Actions ──► Allowed
                                             ├── Resource Author Actions ─► Allowed
                                             └── Disallowed Actions ────► 403 Forbidden
```

---

## 3. Detailed Logic Per Entity

### A. Authentication (`AuthController`)
* **`register` & `login`**: Open to public/anonymous traffic.
* **`me`**: Requires a valid JWT token (`[Authorize]`). Used by frontends to rehydrate user sessions.

### B. Projects (`ProjectsController`)
* **Create Project**: Any authenticated user can create a workspace project. The system automatically inserts a `ProjectMember` record assigning the creator `ProjectRole.Owner`.
* **Read Project & List Members**: Requires caller to belong to the project (`ProjectRole.Owner` or `ProjectRole.Member`).
* **Manage Members & Delete Project**: Administrative operations restricted strictly to `ProjectRole.Owner`. Members receive `403 Forbidden`.

### C. Tasks (`TasksController`)
* **Create Task**: Caller must be a project member. `CreatedById` is enforced server-side from JWT. If an `AssigneeId` is specified, the backend validates that the assignee is also a member of the same project.
* **Read Tasks (List or Single)**: IDOR protected. Requires membership in the parent project.
* **Update Task**: Caller must be a project member. Validates `TaskItemStatus` enum bounds. Validates that any updated `AssigneeId` belongs to the project.
* **Delete Task**: 
  - **Owner**: Can delete **any** task in the project.
  - **Member**: Can **only** delete tasks they personally created (`task.CreatedById == currentUserId`).

### D. Comments (`CommentsController`)
* **Create Comment**: Caller must be a project member. `AuthorId` is enforced server-side from JWT.
* **Read Comments**: Caller must be a project member. Results are returned sorted chronologically (`.OrderBy(x => x.CreatedAt)`).
* **Update Comment**: **Author-Only Rule**. Even Project Owners cannot alter another user's comment text to ensure message authenticity.
* **Delete Comment**: **Author OR Owner Rule**. The comment's author can delete their own comment, and the Project Owner can delete any comment for moderation purposes.
