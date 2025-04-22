# GymKeepWebApi - Workout Tracking Application API

[![License: GPL-3.0](https://img.shields.io/badge/License-GPL-yellow.svg)](https://opensource.org/licenses/GPL-3.0)
![.NET Core](https://img.shields.io/badge/.NET-8.0-blueviolet)
![Entity Framework Core](https://img.shields.io/badge/EF%20Core-8.0-blue)

## Warning

**This project was developed for the Advanced Programming course midterm exam within the Department of Information Systems and Technologies at Atatürk University. Please evaluate the project within this context.**

## Description

This repository contains the ASP.NET Core Web API project providing backend services for the GymKeep mobile or web application. It offers RESTful endpoints for user management, exercise catalog, workout plans, session tracking, calorie calculation, and more.

## Table of Contents

- [GymKeepWebApi - Workout Tracking Application API](#gymkeepwebapi---workout-tracking-application-api)
  - [Warning](#warning)
  - [Description](#description)
  - [Table of Contents](#table-of-contents)
  - [Features](#features)
  - [Technologies Used](#technologies-used)
  - [Project Structure](#project-structure)
  - [Setup and Initialization](#setup-and-initialization)
    - [Prerequisites](#prerequisites)
    - [Setup Steps](#setup-steps)
    - [Database Setup](#database-setup)
  - [Running the Application](#running-the-application)
    - [Development Environment](#development-environment)
    - [Production Environment (Overview)](#production-environment-overview)
  - [API Endpoints](#api-endpoints)
    - [Swagger / OpenAPI](#swagger--openapi)
    - [Core Endpoint Groups](#core-endpoint-groups)
    - [Example Requests](#example-requests)
  - [Database](#database)
    - [Entity Framework Core Migrations](#entity-framework-core-migrations)
    - [Database Schema](#database-schema)
  - [Configuration](#configuration)
  - [Contributing](#contributing)
  - [License](#license)

## Features

*   **User Management:** Registration, login, fetching/updating/deleting user information.
*   **Exercise Catalog:** Listing exercises, difficulty levels, and target regions, viewing details.
*   **Workout Plans:** Creating, listing, updating, deleting user-specific workout plans.
*   **Plan Exercises:** Adding/removing exercises to/from plans, managing set/rep information.
*   **Workout Sessions:** Recording, listing, and detailing actual workout sessions.
*   **Session Exercises:** Recording exercises performed during a session.
*   **Set Logs:** Recording details of each set within a session (weight, reps, completion status).
*   **Calorie Calculation:** Saving/displaying TDEE and goal-oriented calorie calculations based on user data.
*   **Achievements:** Managing user achievements.
*   **Regional Workouts:** Managing predefined regional workout templates.
*   **User Settings:** Managing user-specific application settings (theme, notifications, etc.).

## Technologies Used

*   **.NET 8** (or the version you are using): Backend platform.
*   **ASP.NET Core Web API:** Framework for building RESTful APIs.
*   **Entity Framework Core 8** (or the version you are using): ORM (Object-Relational Mapper) for database interaction.
    *   **Code-First Approach:** Database schema is generated from C# model classes.
    *   **Migrations:** Used to manage database schema changes.
*   **C#:** Primary programming language.
*   **RESTful Principles:** For API design.
*   **JSON:** Data exchange format.
*   **Swagger (OpenAPI):** Used for API documentation and testing (integrated with ASP.NET Core).
*   **Database:** (Specify the database used in your project, e.g., PostgreSQL, SQL Server, SQLite).
*   **(Optional) JWT (JSON Web Token):** Recommended for secure authentication and authorization (Current password management is NOT SECURE and requires improvement).
*   **(Optional) AutoMapper or Mapster:** Can be used for DTO and Entity transformations.

## Project Structure

The project follows a standard ASP.NET Core Web API structure to separate concerns:

GymKeepWebApi/
├── Controllers/        # Controller classes containing API endpoints (UserController, WorkoutPlanController, etc.)
├── Data/               # Data access layer
│   ├── MyDbContext.cs  # Entity Framework Core database context
│   └── Migrations/     # EF Core database migration files
├── Dtos/               # Data Transfer Object classes (for API requests/responses)
├── Models/             # C# classes representing database entities (User, Exercise, etc.)
├── Properties/
│   └── launchSettings.json # Development environment launch settings
├── appsettings.json    # Main configuration file (connection strings, etc.)
├── Program.cs          # Application entry point, configuration of services and middleware
└── GymKeepWebApi.csproj # Project file

## Setup and Initialization

### Prerequisites

*   **.NET SDK 8.0** (or the version targeted by your project) - [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download)
*   **A Database Server:** (Specify the database you are using, e.g., PostgreSQL, SQL Server, SQL Server Express)
*   **IDE or Text Editor:** Visual Studio 2022, Visual Studio Code, or JetBrains Rider.
*   **(Optional) Git:** For version control.

### Setup Steps

1.  **Clone the Repository:**
    ```bash
    git clone https://github.com/your_username/GymKeepWebApi.git
    cd GymKeepWebApi
    ```
2.  **Restore NuGet Packages:**
    ```bash
    dotnet restore
    ```

### Database Setup

1.  **Create the Database:** Create an empty database named `GymKeepDb` (or your preferred name) on your chosen database server (e.g., PostgreSQL, SQL Server).
2.  **Set the Connection String:**
    *   Open the `appsettings.Development.json` file (if it doesn't exist, create one by copying `appsettings.json`).
    *   In the `ConnectionStrings` section, update the `DefaultConnection` value according to your database server and the database you created.
        *   **Example (SQL Server):** `"DefaultConnection": "Server=YOUR_SERVER_NAME;Database=GymKeepDb;User ID=YOUR_USER_ID;Password=YOUR_PASSWORD;TrustServerCertificate=True;"`
        *   **Example (PostgreSQL):** `"DefaultConnection": "Host=localhost;Database=GymKeepDb;Username=postgres;Password=YOUR_PASSWORD;"`
3.  **Apply the Database Schema (EF Core Migrations):**
    *   Open a terminal or command prompt in the project's root directory.
    *   Run the following command to create/update the database schema using EF Core Migrations:
        ```bash
        dotnet ef database update
        ```
    *   This command will apply the migration files from the `Data/Migrations` folder to your database.

## Running the Application

### Development Environment

1.  **Visual Studio:** Open the project in Visual Studio and start it by selecting the `https` profile (Usually F5).
2.  **VS Code or Terminal:**
    ```bash
    dotnet run --launch-profile https
    ```
    (or `--launch-profile http` for the http profile)

The application will start listening on the URLs specified in the `launchSettings.json` file by default (e.g., `https://localhost:7091` and `http://localhost:5091`).

### Production Environment (Overview)

To run in a production environment:

1.  Publish the application: `dotnet publish -c Release -o ./publish`
2.  Deploy the published files to a server (Azure App Service, IIS, Linux server, etc.).
3.  Configure the database connection string and other sensitive settings using `appsettings.Production.json` or environment variables for the production environment.
4.  Run the application behind a web server (Kestrel, IIS, Nginx, Apache) and **enforce HTTPS**.

## API Endpoints

### Swagger / OpenAPI

You can explore and test the API endpoints using the Swagger UI. While the application is running, navigate to the following address in your browser:

`/swagger` (e.g., `https://localhost:7091/swagger`)

### Core Endpoint Groups

*   `/api/User`: User operations (register, login, profile).
*   `/api/Exercise`: List exercises, view details.
*   `/api/ExerciseRegion`: List exercise regions.
*   `/api/DifficultyLevel`: List difficulty levels.
*   `/api/users/{userId}/WorkoutPlan`: User's workout plan operations (CRUD).
*   `/api/users/{userId}/WorkoutPlan/{planId}/Exercises`: Add/remove/update exercises in a plan.
*   `/api/users/{userId}/WorkoutSession`: User's workout session operations (CRUD).
*   `/api/users/{userId}/WorkoutSession/{sessionId}/exercises`: Add/remove/update exercises in a session.
*   `/api/users/{userId}/WorkoutSession/{sessionId}/exercises/{sessionExerciseId}/setlogs`: Add/remove/update/list set logs for a session exercise.
*   `/api/CalorieCalculation`: Calorie calculation operations.
*   `/api/UserSetting`: User settings operations.
*   *(Similar endpoints for other controllers)*

### Example Requests

**1. User Registration:**

*   **Method:** `POST`
*   **URL:** `/api/User/register`
*   **Body (application/json):**
    ```json
    {
      "username": "testuser",
      "email": "test@example.com",
      "password": "SecurePassword123"
    }
    ```
*   **Success Response:** `201 Created` (Returns `UserResponseDto` in the body)

**2. User Login:**

*   **Method:** `POST`
*   **URL:** `/api/User/login`
*   **Body (application/json):**
    ```json
    {
      "username": "testuser",
      "password": "SecurePassword123"
    }
    ```
*   **Success Response:** `200 OK` (Returns `UserResponseDto` in the body)
    **WARNING:** Password verification in the current code is not secure!

**3. List User's Plans:**

*   **Method:** `GET`
*   **URL:** `/api/users/1/WorkoutPlan` (Example: userId=1)
*   **Success Response:** `200 OK` (Returns a list of `WorkoutPlan` in the body)

**4. Add Exercise to Plan:**

*   **Method:** `POST`
*   **URL:** `/api/users/1/WorkoutPlan/5/Exercises` (Example: userId=1, planId=5)
*   **Body (application/json):**
    ```json
    {
      "exerciseId": 10,
      "sets": 3,
      "reps": 12,
      "restDurationSeconds": 60,
      "orderInPlan": 1
    }
    ```
*   **Success Response:** `201 Created` (Returns the created `PlanExercise` in the body)

## Database

### Entity Framework Core Migrations

This project uses EF Core Migrations to manage the database schema.

*   **Adding a New Migration:** After making changes to the model classes, create a new migration:
    ```bash
    dotnet ef migrations add MigrationName -p GymKeepWebApi.csproj -s GymKeepWebApi.csproj
    ```
*   **Updating the Database:** Apply the pending migrations to the database:
    ```bash
    dotnet ef database update
    ```
*   **Reverting to a Specific Migration:**
    ```bash
    dotnet ef database update PreviousMigrationName
    ```
*   **Removing a Migration:** Revert the last applied migration:
    ```bash
    dotnet ef migrations remove
    ```

All migration files are located in the `Data/Migrations` folder.

### Database Schema

The database schema is defined by the entity classes in the `Models` folder and the configurations in `MyDbContext`'s `OnModelCreating` method. Relationships, constraints, and indexes are specified there. The `database_schema.sql` file (if present) might contain an SQL representation of the schema.

## Configuration

The application's main configuration is in the `appsettings.json` file. Specific files like `appsettings.Development.json`, `appsettings.Staging.json`, and `appsettings.Production.json` can be used for different environments. Environment variables can also be used to override configuration settings.

Key configurations:

*   **`ConnectionStrings:DefaultConnection`**: Database connection string.
*   **`Logging`**: Logging levels and providers for the application.
*   **(Addable)** `JwtSettings`: JWT (JSON Web Token) settings (Secret Key, Issuer, Audience, etc.).

## Contributing

If you wish to contribute, please follow these steps:

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix (`git checkout -b feature/new-feature` or `git checkout -b bugfix/fix-bug`).
3.  Make your changes and commit them (`git commit -m 'Add new feature'`).
4.  Push your branch to the origin (`git push origin feature/new-feature`).
5.  Create a Pull Request (PR).

Please adhere to coding standards and document your changes clearly.

## License

This project is licensed under the [GPL-3.0 License](LICENSE). See the `LICENSE` file for more details.