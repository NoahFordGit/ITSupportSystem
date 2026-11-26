# IT Support System

An MVC-based IT support ticketing system featuring user authentication, role-based access, and ticket management.

## Tech Stack

- **Frontend:** HTML, CSS, JavaScript
- **Backend:** C# / ASP.NET Core MVC
- **ORM:** Entity Framework Core
- **Database:** SQLite
- **Authentication:** ASP.NET Core Identity
- **Version Control:** Git / GitHub

## Features

- User authentication and accounts
- Role-based access
- IT support ticket creation and tracking
- Ticket priority and status management
- Ticket comments
- Administrator user and role management

## Project Structure

```text
Controllers/    MVC controllers
Data/           Database context and configuration
Models/         Application data models
Views/          Razor views
wwwroot/        CSS, JavaScript, and static files
Migrations/     Entity Framework Core migrations
```

## Getting Started

### 1. Set the admin password (one time per machine)

On startup the API creates an **Admin** account so you can log in and manage users.
The username and email are in `ITSupportSystem.API/appsettings.json`. The password is
**not** in the repo. Each person keeps their own copy in .NET
[user secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets), which are
stored outside the project folder and never committed.

From the repository root, run:

```bash
dotnet user-secrets set "SeedAdmin:Password" "YourPasswordHere1!" --project ITSupportSystem.API
```

The password must meet ASP.NET Core Identity's default rules: at least 6 characters,
with an uppercase letter, a lowercase letter, a digit, and a symbol.

To check what you set:

```bash
dotnet user-secrets list --project ITSupportSystem.API
```

If you skip this step, the API stops at startup with the error
`Missing configuration value 'SeedAdmin:Password'`.

> **Already have a database from before this change?** The seeder only creates the admin
> if no user named `admin` exists, so an existing `ITSupportSystem.API/Data/AppDb.db`
> keeps its old admin password. Delete `AppDb.db` and run `dotnet ef database update --project ITSupportSystem.API`
> to start fresh with your new password.

### 2. Run the API

```bash
dotnet run --project ITSupportSystem.API
```

Log in with `POST /api/auth/login`, sending the admin username in the `email` field
(ASP.NET Identity's login endpoint matches that field against the username).

### 3. Run the tests

```bash
dotnet test
```

The tests don't need user secrets. They use their own in-memory database and provide
their own admin login (see `ITSupportSystem.API.Tests/Infrastructure/ApiFactory.cs`).

### Outside of development

User secrets only load in the `Development` environment. Anywhere else, set the password
as an environment variable. A double underscore stands in for the `:`.

```bash
SeedAdmin__Password=YourPasswordHere1!
```

<sub>Made for ETSU CSCI-3110 (Advanced Topics in Web Development)</sub>
