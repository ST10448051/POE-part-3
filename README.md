# RaceDay

RaceDay is an event-management application built with **ASP.NET Core Web API**, **ASP.NET Core MVC**, **Entity Framework Core**, and **SQL Server**. It supports separate **Organiser** and **Participant** experiences for managing sporting events, enrolments, and results. Event image storage is integrated with **Azure Blob Storage**.

## Solution structure

```text
RaceDay/
├── RaceDay.API/          # REST API, EF Core data access, authentication, Azure storage
├── RaceDay.API.Tests/    # API automated tests
└── RaceDay.MVC/          # MVC website consuming the REST API
```

## Technologies

- C# and ASP.NET Core Web API
- ASP.NET Core MVC and Razor views
- Entity Framework Core and SQL Server
- Cookie-based authentication and role-based authorisation
- Azure Blob Storage (`Azure.Storage.Blobs`)
- Swagger / OpenAPI for API exploration
- Visual Studio and NuGet

## Features

### Organisers

- Sign in to an Organiser dashboard.
- Create, view, edit, and delete events.
- Manage categories and assign categories to events.
- View event enrolments and participant information.
- Record, edit, and delete event results.
- Upload, view, and delete event images stored in Azure Blob Storage.

### Participants and visitors

- Register and sign in as a Participant.
- Browse events and view event details.
- Enrol in events and view personal enrolments.
- View personal and event results.
- Browse event image galleries.

### Results

Result records support `Finished`, `DidNotFinish`, and `Disqualified` statuses. Finished results include a finish time and position; other statuses do not.

## Prerequisites

- Visual Studio with ASP.NET and web development tooling, or a compatible .NET SDK and editor
- .NET SDK matching the target framework specified in the project `.csproj` files
- SQL Server or SQL Server Express/LocalDB, as configured for the API
- An Azure subscription and an Azure Storage Account with a **private** blob container named `event-media`
- Access to NuGet packages and the Azure Storage service

## Local configuration

### 1. Clone the repository

```bash
git clone https://github.com/ST10448051/POE-part-3
cd POE-part-3
```

Open the solution in Visual Studio and restore NuGet packages.

### 2. Configure the SQL Server connection

The API uses an EF Core database context named `RaceDayDbContext`. Configure its connection string using the **exact configuration key referenced in `RaceDay.API/Program.cs`** (for example, `ConnectionStrings:RaceDayConnection` if that is the key in your project).

For local development, store credentials outside committed configuration files, such as with .NET user secrets or environment variables.

### 3. Configure Azure Blob Storage

In `RaceDay.API/appsettings.json`, include the non-secret container setting:

```json
{
  "AzureBlobStorage": {
    "ContainerName": "event-media"
  }
}
```

Merge this section into the existing JSON; do **not** replace other application settings.

For local development, use **Manage User Secrets** on `RaceDay.API`, or run:

```bash
dotnet user-secrets init --project RaceDay.API
dotnet user-secrets set "AzureBlobStorage:ConnectionString" "<YOUR_PRIVATE_CONNECTION_STRING>" --project RaceDay.API
```

The storage account must permit the authentication method used by `BlobStorageService`. If shared-key access is disabled, the service must be adapted to use Microsoft Entra ID / managed identity rather than enabling shared-key access unnecessarily.


### 4. Apply database migrations

From the Visual Studio Package Manager Console, with `RaceDay.API` as the default project:

```powershell
Update-Database -Project RaceDay.API -StartupProject RaceDay.API
```

Alternatively, if EF Core CLI tools are configured:

```bash
dotnet ef database update --project RaceDay.API --startup-project RaceDay.API
```

Review migrations before applying them to a shared or production database. The event-media feature uses an `EventMedia` table to associate uploaded files with events.

### 5. Start the API and MVC projects

Configure **multiple startup projects** in Visual Studio and start both `RaceDay.API` and `RaceDay.MVC`.

The development URLs used during the walkthrough were:

| Component | Local URL |
| --- | --- |
| API | `https://localhost:7078` |
| API Swagger UI | `https://localhost:7078/swagger` |
| MVC website | `https://localhost:7089` |

These ports are development examples. Check each project's `Properties/launchSettings.json` and the MVC named `HttpClient` configuration if your ports differ.

## Application architecture

```text
Browser
   |
   v
RaceDay.MVC (Razor pages, controllers, MVC session)
   |
   | Named HttpClient: RaceDayAPI
   | AuthenticatedApiService forwards the API session cookie
   v
RaceDay.API (controllers, validation, authorisation)
   |                          |
   v                          v
SQL Server / EF Core      Azure Blob Storage
(events, users,            (event images)
 enrolments, results,
 media metadata)
```

The MVC application uses `RaceDayApiService` for public API calls and `AuthenticatedApiService` for authenticated operations. The API must independently enforce roles and ownership restrictions, even when MVC hides controls from unauthorised users.

## Selected API routes

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/events/{eventId}/media` | List an event's images |
| GET | `/api/events/{eventId}/media/{mediaId}` | Retrieve an image through the API |
| POST | `/api/events/{eventId}/media` | Upload an image (event Organiser) |
| DELETE | `/api/events/{eventId}/media/{mediaId}` | Delete an image (event Organiser) |
| GET | `/api/results/event/{eventId}` | View event results |
| GET | `/api/results/my` | View the signed-in Participant's results |
| POST | `/api/results` | Record a result (Organiser) |
| PUT | `/api/results/{id}` | Update a result (Organiser) |
| DELETE | `/api/results/{id}` | Delete a result (Organiser) |

Other API controllers cover authentication, events, categories, event-category associations, enrolments, participants, routes, weather, and profiles. Consult Swagger for the exact current route contracts and request bodies.

### Image upload rules

The event-media API currently accepts JPEG, PNG, and WebP uploads up to **5 MB**. Uploaded files receive unique blob names, while SQL Server stores metadata such as the original filename, MIME type, size, and upload date. The container is private; images are served through the API.

## Running tests

Use **Test > Test Explorer** in Visual Studio to run tests in `RaceDay.API.Tests`, or run:

```bash
dotnet test
```

Suggested manual test scenarios:

1. Register, log in, and log out as a Participant.
2. Log in as an Organiser and create/edit an event.
3. Add categories and enrol a Participant.
4. Record a result and confirm it appears on the event and Participant pages.
5. Upload an event image, view it in the MVC gallery, and delete it.
6. Verify that a Participant cannot call Organiser-only write endpoints.
7. Verify that an Organiser cannot modify another Organiser's event or images.
8. Verify that invalid image types and files over 5 MB are rejected.

Do not claim tests pass until you have run them in your environment.


## Licence

No licence has been specified. Add a licence only if the project owner chooses one.
