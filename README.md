# Task4UserManager

ASP.NET Core MVC user management app for Itransition Task 4.

## Features

- Registration and login
- E-mail confirmation
- Bulk block, unblock, delete, and delete-unverified actions
- Automatic logout/redirection for blocked or deleted users
- Unique database index for normalized e-mail
- Bootstrap-based admin table UI

## Local Run

1. Update `appsettings.json` with a working PostgreSQL connection string and SMTP settings.
2. Run:

```bash
dotnet build
dotnet run
```

## Render Deployment

This repo includes:

- `Dockerfile`
- `render.yaml`

Set these Render environment variables:

- `ConnectionStrings__DefaultConnection`
- `Smtp__User`
- `Smtp__Password`
- `Smtp__FromEmail`

Optional/defaulted in `render.yaml`:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Smtp__Host=smtp.gmail.com`
- `Smtp__Port=587`
- `Smtp__FromName=Task4 App`

## Submission Notes

Demo video should show:

- registration
- e-mail confirmation
- login
- selecting another user
- blocking and unblocking users with visible status change
- selecting all users including current user
- self-block leading to login redirection
- proof of the unique DB index
