# Team Task Manager (Mini Jira)

Project 02 — Team Task Manager (Mini Jira) full-stack application built with **ASP.NET Core Web API** and **Next.js (App Router)**.

## Project Structure

```text
team-task-manager/
│
├── backend/
│   ├── TeamTaskManager.sln
│   │
│   ├── src/
│   │   └── TeamTaskManager.Api/
│   │       ├── Controllers/
│   │       ├── Data/
│   │       ├── Models/
│   │       ├── Contracts/
│   │       ├── Services/
│   │       ├── Auth/
│   │       ├── Authorization/
│   │       ├── Common/
│   │       ├── Properties/
│   │       ├── Program.cs
│   │       ├── appsettings.json
│   │       ├── appsettings.Development.json
│   │       └── TeamTaskManager.Api.csproj
│   │
│   └── tests/
│       └── TeamTaskManager.Api.Tests/
│
├── frontend/
│   ├── app/
│   │   ├── layout.js
│   │   ├── page.js
│   │   └── globals.css
│   ├── components/
│   │   ├── ui/
│   │   └── shared/
│   ├── features/
│   │   ├── auth/
│   │   ├── workspaces/
│   │   ├── projects/
│   │   ├── tasks/
│   │   └── comments/
│   ├── lib/
│   │   ├── api/
│   │   └── utils.js
│   ├── hooks/
│   ├── public/
│   ├── .env.local.example
│   ├── components.json
│   ├── jsconfig.json
│   ├── next.config.mjs
│   ├── package.json
│   └── eslint.config.mjs
│
├── docs/
│   └── PROJECT_02_TEAM_TASK_MANAGER_MASTER_PLAN.md
│
├── .gitignore
└── README.md
```

## Running the Backend

From the repository root:

```bash
cd backend/src/TeamTaskManager.Api
dotnet run
```

Or from the `backend/` folder:

```bash
cd backend
dotnet run --project src/TeamTaskManager.Api
```

Swagger UI is available at `https://localhost:7196/swagger` (or configured port in `Properties/launchSettings.json`).

To run backend tests:

```bash
cd backend
dotnet test
```

## Running the Frontend

From the repository root:

```bash
cd frontend
npm run dev
```

The application will start on [http://localhost:3000](http://localhost:3000).
