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

1. Put your local secrets in `appsettings.Development.json` or environment variables.
2. Example `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5435;Database=task4db;Username=postgres;Password=YOUR_DB_PASSWORD"
  },
  "Resend": {
    "ApiKey": "YOUR_RESEND_API_KEY",
    "FromName": "Task4 App",
    "FromEmail": "YOUR_VERIFIED_SENDER_EMAIL"
  }
}
```

3. `appsettings.Development.json` is already gitignored, so your secrets stay local.
4. Resend requires a sender address your account can use. For broader delivery, verify your own domain in Resend and use that address as `FromEmail`.
5. Run:

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
- `Resend__ApiKey`
- `Resend__FromEmail`

Optional/defaulted in `render.yaml`:

- `ASPNETCORE_ENVIRONMENT=Production`
- `Resend__FromName=Task4 App`

Notes:

- Render free services block outbound SMTP ports, so production e-mail uses the Resend HTTPS API instead of SMTP.
- `Resend__FromEmail` must be a sender address Resend accepts for your account. For non-test sending, verify your domain in Resend first.

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
