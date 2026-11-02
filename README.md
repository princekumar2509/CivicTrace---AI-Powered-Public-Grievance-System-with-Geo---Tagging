# CivicTrace – Real-Time Public Grievance and Issue Redressal System

CivicTrace is a web-based public grievance management system designed to help citizens report local issues, provide location and image evidence, track complaint progress, and give feedback on resolutions. It also provides an administrative portal for authorities to review, assign, update, resolve, and monitor complaints.

The project is built with **ASP.NET Core MVC**, **C#**, **Entity Framework Core**, and **SQL Server**, with Razor views for the user interface.

---

## Table of Contents

- [Project Objectives](#project-objectives)
- [Main Features](#main-features)
- [Technology Stack](#technology-stack)
- [Application Roles](#application-roles)
- [Complaint Workflow](#complaint-workflow)
- [Resolution Deadlines and Escalation](#resolution-deadlines-and-escalation)
- [Project Structure](#project-structure)
- [Prerequisites](#prerequisites)
- [Installation and Setup](#installation-and-setup)
- [Configuration](#configuration)
- [Database Migrations](#database-migrations)
- [Running the Application](#running-the-application)
- [Testing](#testing)
- [Important Notes and Current Limitations](#important-notes-and-current-limitations)
- [Future Enhancements](#future-enhancements)

---

## Project Objectives

CivicTrace aims to make public issue reporting and follow-up more organized and transparent. Its objectives are to:

1. Provide citizens with a convenient way to submit public grievances.
2. Capture complaint locations and supporting photographs.
3. Keep citizens informed about complaint progress and administrative decisions.
4. Help authorities categorize, prioritize, assign, and resolve complaints.
5. Maintain a history of complaint status changes and resolution responses.
6. Identify delayed complaints using configurable resolution targets.

## Main Features

### Citizen Portal

- Citizen registration and login using ASP.NET Core Identity.
- Google sign-in support, when OAuth credentials are configured.
- Citizen dashboard with complaint statistics and recent updates.
- Submit complaints with a title, description, category classification, and location details.
- Upload one or multiple evidence images, subject to administrator-configured upload rules.
- Select complaint location using an interactive map.
- View complaint details and status history.
- Track complaints belonging to the signed-in citizen.
- View before-and-after resolution photographs when resolution evidence is available.
- Confirm that a complaint has been resolved or report that it is not resolved.
- View the administrator's decision and remarks after a resolution objection is reviewed.
- Submit feedback on resolved complaints, where enabled.
- Reopen-related workflows and complaint status updates, subject to the project's configured rules.
- Community complaint support through upvotes, including restrictions against supporting one's own complaint.

### Administrative Portal

- Admin-only dashboard with complaint statistics and analytics.
- View and manage submitted complaints.
- Update complaint status, priority, and assigned department.
- View complaint location and supporting evidence.
- Upload resolution proof photographs, including a camera-capture option on supported mobile devices.
- Review citizen objections to a claimed resolution and approve reopening or reject the objection.
- View the most-supported community complaints.
- Use an Admin Quick Menu for:
  - Pending Resolution Reviews
  - Overdue Complaints
  - Escalated Complaints
- View dedicated pages for pending resolution reviews, overdue complaints, and escalated complaints.
- Configure upload, complaint workflow, resolution target, and escalation settings through System Settings.
- Access reports and analytics and citizen management pages, where configured in the application.

### AI-Assisted Features

The project includes service components for:

- **AIComplaintClassifier** – classifies complaint text and helps determine category, department, and priority.
- **DuplicateComplaintDetector** – checks for similar existing complaints and can warn the citizen before submission.
- **AISeverityPredictor** – predicts a severity level from complaint information.
- **AIImageAnalyzer** – analyses submitted images through the configured image-analysis integration.

AI-generated results are intended to assist the workflow. Administrative review remains important, especially for priority, severity, and resolution decisions.

### Location and Evidence

- Complaint records include latitude, longitude, address, and PIN code fields.
- The dashboard includes a complaint hotspot map.
- Citizen-submitted evidence and administrator-uploaded resolution evidence are stored as files, with their paths recorded in the database.

### Community Support

- Citizens can support complaints submitted by other citizens.
- Support counts are displayed in the community area and used to identify the most-supported complaints.
- A citizen cannot upvote their own complaint.
- A unique database index helps prevent duplicate upvotes by the same user for the same complaint.

---

## Technology Stack

| Area | Technology |
|---|---|
| Backend | C#, ASP.NET Core MVC |
| User interface | Razor Views, HTML, CSS, JavaScript |
| UI framework | Bootstrap |
| Data access | Entity Framework Core |
| Database | Microsoft SQL Server |
| Authentication and roles | ASP.NET Core Identity |
| External sign-in | Google OAuth (optional; requires credentials) |
| Mapping | Leaflet with OpenStreetMap tiles, as configured in the project |
| Charts | Chart.js |
| AI services | Project-specific classifier, duplicate detector, severity predictor, and image analyzer services |
| Image storage | Local application storage under `wwwroot/uploads` |

> Exact package versions and external service requirements are determined by the project's `.csproj`, configuration files, and service implementations.

## Application Roles

### Citizen

A citizen can register, sign in, submit and track their own complaints, provide supporting evidence, respond to resolution claims, and participate in community support features.

### Admin

An administrator can access the authority dashboard, manage complaints, assign departments, update statuses, review citizen objections, manage system settings, and monitor overdue or escalated complaints.

Administrative actions are protected with role-based authorization in the relevant controllers.

## Complaint Workflow

1. A citizen signs in and submits a complaint.
2. The application validates the submitted information and upload rules.
3. AI-assisted text classification determines the complaint category, department, and priority.
4. The severity predictor produces an AI severity result.
5. The duplicate detector checks for similar complaints when that setting is enabled.
6. The complaint is saved with the citizen's ownership information and an initial status-history entry.
7. Evidence images are saved and associated with the complaint.
8. An administrator reviews the complaint, assigns or confirms its department, and updates its status.
9. When the complaint is marked resolved, an administrator can upload resolution proof.
10. The citizen can inspect the resolution evidence and confirm the resolution or report that the issue remains unresolved.
11. If the citizen objects, the administrator can approve reopening or reject the objection. The decision and remarks are recorded in the complaint history and review record.

## Resolution Deadlines and Escalation

Administrators can configure resolution targets in **System Settings**. The initial example defaults are:

| Priority | Default target |
|---|---:|
| High | 24 hours |
| Medium | 72 hours |
| Low | 168 hours (7 days) |

Additional settings include:

- Automatic overdue detection: enabled by default.
- Escalation notifications: enabled by default.
- Additional escalation period: 24 hours by default.
- Resolution proof: enabled by default in the settings model.

When a complaint is submitted, its resolution deadline is calculated using the priority assigned after AI classification and the configured target. The deadline is stored on the complaint record; changing the priority later does not automatically recalculate the stored deadline.

A hosted background service checks deadlines every five minutes while the application is running. It records the first overdue event and, after the configured additional period, records an escalation event. These events are added to complaint status history and are reflected in the administrative quick-menu counts and dedicated lists.

**Important:** These values are configurable example targets for this project, not official government service-level agreements.

## Project Structure

The main folders and files include:

```text
PublicGrievanceSystem/
├── Areas/                 # ASP.NET Core Identity pages
├── Controllers/           # MVC controllers
├── Data/                  # ApplicationDbContext and database configuration
├── Migrations/            # Entity Framework Core migrations
├── Models/                # Complaint, settings, evidence, feedback, and other models
├── Services/              # AI services and background services
├── ViewComponents/        # Reusable view components, including admin quick-menu counts
├── ViewModels/            # View-specific data models
├── Views/                 # Razor views for Admin, Citizen, and other pages
├── wwwroot/               # CSS, JavaScript, images, and uploaded files
├── appsettings.json       # Application configuration (keep secrets out of source control)
├── Program.cs             # Application service registration and middleware pipeline
└── PublicGrievanceSystem.csproj
```

Some folders may contain additional files depending on the current project version.

## Prerequisites

Install the following before running the project:

- Windows, macOS, or Linux development environment.
- .NET SDK compatible with the target framework in `PublicGrievanceSystem.csproj`.
- Microsoft SQL Server or SQL Server Express/LocalDB.
- Visual Studio or Visual Studio Code.
- Entity Framework Core command-line tools for running migrations.
- Internet access for any configured external integrations, such as Google OAuth, map tiles, or AI image analysis.

Check the target framework in `PublicGrievanceSystem.csproj` before installing the .NET SDK.

## Installation and Setup

### 1. Get the project source

Clone the repository or open the project folder supplied to you:

```bash
git clone <your-repository-url>
cd PublicGrievanceSystem
```

Replace `<your-repository-url>` with the actual repository URL. If the source code is already on your computer, open the folder containing `PublicGrievanceSystem.csproj` instead.

### 2. Restore NuGet packages

```bash
dotnet restore
```

### 3. Configure the database connection

Open `appsettings.json` or `appsettings.Development.json` and configure the `DefaultConnection` connection string for your SQL Server instance.

Example format (replace the server and database values with your own):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<YOUR_SQL_SERVER>;Database=PublicGrievanceSystemDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  }
}
```

For SQL Server authentication, use the appropriate SQL Server username and password in a secure local configuration. Do not commit passwords or production connection strings to Git.

### 4. Configure external credentials

Google sign-in is configured in `Program.cs` using these configuration keys:

```text
Authentication:Google:ClientId
Authentication:Google:ClientSecret
```

Set them through .NET User Secrets or environment variables. For example, using User Secrets from the project directory:

```bash
dotnet user-secrets init
dotnet user-secrets set "Authentication:Google:ClientId" "<YOUR_GOOGLE_CLIENT_ID>"
dotnet user-secrets set "Authentication:Google:ClientSecret" "<YOUR_GOOGLE_CLIENT_SECRET>"
```

Configure any AI/image-analysis credentials and email settings according to the actual implementation in `Services/AIImageAnalyzer.cs` and the email sender service. Do not place API keys, SMTP passwords, or other secrets in a public repository.

### 5. Install or update EF Core tools

If the EF Core CLI tool is not installed:

```bash
dotnet tool install --global dotnet-ef
```

If it is already installed and needs updating:

```bash
dotnet tool update --global dotnet-ef
```

### 6. Apply database migrations

From the directory containing `PublicGrievanceSystem.csproj`, run:

```bash
dotnet ef database update
```

This applies the migrations included in the project to the configured database.

## Configuration

The administrator can configure several application rules from **Admin Dashboard → System Settings**, including:

- Maximum image size and allowed image formats.
- Whether an image is required.
- Whether multiple images are allowed and the maximum number per complaint.
- Whether new complaints are accepted.
- Whether citizens can request reopening.
- Whether a complaint description and location are required.
- Duplicate detection and citizen feedback settings.
- High-, medium-, and low-priority resolution targets.
- Automatic overdue detection and escalation notifications.
- The additional escalation period.
- Resolution proof setting.

The settings page stores configuration values. A setting only changes runtime behaviour where the corresponding controller or service enforces it. Check the current implementation before assuming every switch is fully enforced.

## Database Migrations

When model properties are changed, create a migration and apply it:

```bash
dotnet ef migrations add <MigrationName>
dotnet ef database update
```

Use a descriptive migration name, and review the generated migration before applying it to an important database. Back up any database containing data you need to preserve.

## Running the Application

Start the application from the project directory:

```bash
dotnet run
```

Use the local HTTP or HTTPS address printed in the terminal to open CivicTrace in a browser.

To build without running:

```bash
dotnet build
```

## Testing

Suggested manual test checklist:

### Citizen

- Register a citizen account and sign in.
- Submit a complaint with valid details and location.
- Test image upload and configured image limits.
- Confirm that classification, priority, department, and severity information are populated as expected.
- Test duplicate-complaint warnings.
- Verify that a citizen can view only their own complaint details and tracking history.
- Test community upvotes and confirm that a citizen cannot upvote their own complaint.
- Test resolution proof viewing and the confirm/report-not-resolved response workflow.
- Verify that a citizen can see the administrator's review outcome and remarks.

### Admin

- Sign in with an account assigned the Admin role.
- Open the Authority Dashboard and inspect the statistics and analytics.
- Update a complaint's status, department, and priority.
- Upload resolution proof for a resolved complaint.
- Review a citizen's resolution objection and test both reopening approval and rejection.
- Save System Settings and confirm that values persist after refreshing.
- Inspect the three Admin Quick Menu counts and their dedicated pages.
- Verify that overdue and escalation events are recorded after the configured deadlines.

### Deadline and escalation testing

For testing, use short resolution targets in a development database and keep the application running so the hosted background service can execute. The service checks at five-minute intervals, so a newly passed threshold may not appear immediately. Restore the intended settings after testing.

## Important Notes and Current Limitations

- The background deadline checker runs inside the web application process. It requires the application to be running; it is not a separate durable job scheduler.
- The current escalation workflow records events in complaint status history and surfaces counts/lists in the admin interface. It does not, by itself, provide email, SMS, browser push, or mobile push delivery.
- Resolution targets are project configuration values and should not be presented as official service commitments without approval from the relevant authority.
- Local uploads under `wwwroot/uploads` are suitable for development. A production deployment should evaluate durable object storage, backup, access control, file-content validation, and retention policies.
- Validate uploaded files by content and size, not only by filename extension. Review the upload endpoints before production use.
- Protect secrets with User Secrets or a secure deployment secret store. Never publish API keys, OAuth secrets, database passwords, or SMTP credentials.
- Use HTTPS in deployed environments and review Identity, role assignment, authorization, logging, and database backup configuration before production release.
- The application uses local server time (`DateTime.Now`) in the current deadline workflow. For deployments across time zones or multiple servers, consider standardizing persisted timestamps on UTC and converting for display.
- The `EnableResolutionProof` setting is represented in the settings model and UI. Confirm that the upload action enforces this switch before relying on it as an operational control.
- Anonymous complaint handling should remain disabled unless the complete anonymous-submission, privacy, ownership, and abuse-prevention workflow is implemented and tested.

## Future Enhancements

Possible next improvements include:

- Durable scheduled processing using a job scheduler or hosted worker backed by persistent state.
- Email, SMS, and browser/mobile push notifications.
- Read/unread notification tracking for citizens and administrators.
- Secure cloud storage for complaint and resolution photographs.
- File signature/content validation and malware scanning for uploads.
- Audit logs for sensitive administrator actions.
- More detailed reports, filters, and export options.
- Configurable escalation levels and escalation recipients by department.
- Automated tests for controllers, services, ownership checks, and deadline rules.
- Accessibility and usability improvements across desktop and mobile layouts.

---

## Project Disclaimer

CivicTrace is an academic/software project demonstrating a public grievance reporting and management workflow. It is not an official government service unless formally adopted and operated by the relevant authority. Complaint deadlines and escalation targets are configurable project examples and are not official service-level commitments.
