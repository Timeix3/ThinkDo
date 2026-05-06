using AppApi.Repositories;
using AppApi.Repositories.Interfaces;
using AppApi.Services;
using AppApi.Services.Interfaces;
using Common.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi.Models;
using System.Data.Common;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddScoped<ITaskRepository, TaskRepository>();
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddScoped<IInboxRepository, InboxRepository>();
builder.Services.AddScoped<IInboxService, InboxService>();
builder.Services.AddScoped<IRoutineRepository, RoutineRepository>();
builder.Services.AddScoped<IRoutineService, RoutineService>();
builder.Services.AddScoped<IProjectRepository, ProjectRepository>();
builder.Services.AddScoped<IFlowPhaseRepository, FlowPhaseRepository>();
builder.Services.AddScoped<IProjectService, ProjectService>();
builder.Services.AddScoped<IInboxClassificationService, InboxClassificationService>();
builder.Services.AddScoped<IFlowPhaseService, FlowPhaseService>();
builder.Services.AddHostedService<RoutineSchedulerService>();
builder.Services.AddScoped<ISprintRepository, SprintRepository>();
builder.Services.AddScoped<ISprintService, SprintService>();

// Configure HttpClient for GitHub API calls
builder.Services.AddHttpClient("GitHub", client =>
{
    client.BaseAddress = new Uri("https://api.github.com/");
    client.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");
    client.DefaultRequestHeaders.Add("User-Agent", "AppApi-Resource-Server");
});

// Configure GitHub OAuth token validation for Resource Server
// GitHub tokens are opaque tokens, not JWTs, so we validate them via GitHub API
builder.Services.AddAuthentication("GitHub")
    .AddScheme<AuthenticationSchemeOptions, GitHubAuthenticationHandler>("GitHub", null);

builder.Services.AddAuthorization();

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "App API",
        Version = "v1",
        Description = "API приложения"
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // Configure Bearer token authentication for Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "OAuth2 Access Token",
        In = ParameterLocation.Header,
        Description = "Введите GitHub OAuth access token.\n\nПример: gho_xxxxxxxxxxxxxxxxxxxx"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:8080"
            )
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var db = services.GetRequiredService<AppDbContext>();
        logger.LogInformation("Applying database migrations...");
        EnsureMigrationsApplied(db, logger);
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database");
        throw;
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "App API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy",
    service = "app-api",
    timestamp = DateTime.UtcNow
}));

app.Run();

// Handles the case where the DB already has tables but __EFMigrationsHistory is empty/incomplete.
// Checks the actual schema, backfills any missing columns/tables introduced in each migration,
// then registers the migration as applied so EF Core's Migrate() doesn't re-run it.
static void EnsureMigrationsApplied(AppDbContext db, ILogger logger)
{
    const string productVersion = "9.0.4";

    var conn = db.Database.GetDbConnection();
    conn.Open();
    try
    {
        // Ensure migration history table exists
        Exec(conn, """
            CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
                "MigrationId" character varying(150) NOT NULL,
                "ProductVersion" character varying(32) NOT NULL,
                CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
            )
            """);

        // ── Always repair: create projects table if missing ────────────────────
        // The original DB had only inbox_items/tasks; projects was added later.
        // InitialCreateFull is already registered, so Migrate() won't create it.
        if (!TableExists(conn, "projects"))
        {
            logger.LogInformation("Creating missing projects table");
            Exec(conn, """
                CREATE TABLE IF NOT EXISTS projects (
                    id integer GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
                    user_id character varying(450) NOT NULL,
                    name character varying(200) NOT NULL,
                    description character varying(2000),
                    is_default boolean NOT NULL DEFAULT false,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone,
                    deleted_at timestamp with time zone
                )
                """);
            Exec(conn, """CREATE INDEX IF NOT EXISTS "ix_projects_deleted_at" ON projects (deleted_at)""");
            Exec(conn, """CREATE INDEX IF NOT EXISTS "ix_projects_user_id" ON projects (user_id)""");
            Exec(conn, """CREATE UNIQUE INDEX IF NOT EXISTS "ix_projects_user_default_unique" ON projects (user_id, is_default) WHERE is_default = true""");
        }
        else if (!ColumnExists(conn, "projects", "is_default"))
        {
            logger.LogInformation("Adding missing column projects.is_default");
            Exec(conn, """ALTER TABLE projects ADD COLUMN IF NOT EXISTS "is_default" boolean NOT NULL DEFAULT false""");
            Exec(conn, """CREATE UNIQUE INDEX IF NOT EXISTS "ix_projects_user_default_unique" ON projects (user_id, is_default) WHERE is_default = true""");
        }

        // ── Always repair: add ProjectId to tasks if missing ───────────────────
        if (TableExists(conn, "tasks") && !ColumnExists(conn, "tasks", "ProjectId"))
        {
            logger.LogInformation("Adding missing column tasks.ProjectId");
            Exec(conn, """ALTER TABLE tasks ADD COLUMN IF NOT EXISTS "ProjectId" integer NULL""");
            Exec(conn, """CREATE INDEX IF NOT EXISTS "IX_tasks_ProjectId" ON tasks ("ProjectId")""");
        }

        // ── Always repair: add FK tasks→projects if missing ────────────────────
        if (TableExists(conn, "tasks") && TableExists(conn, "projects"))
        {
            Exec(conn, """
                DO $$ BEGIN
                    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'FK_tasks_projects_ProjectId') THEN
                        ALTER TABLE tasks ADD CONSTRAINT "FK_tasks_projects_ProjectId"
                            FOREIGN KEY ("ProjectId") REFERENCES projects(id) ON DELETE SET NULL;
                    END IF;
                END $$
                """);
        }

        // ── Migration 1: InitialCreateFull ──────────────────────────────────────
        // Creates inbox_items, projects, tasks (with ProjectId FK to projects).
        // If inbox_items already exists the tables were created outside migrations,
        // register the migration so EF won't re-run it.
        if (TableExists(conn, "inbox_items") && !MigrationApplied(conn, "20260421230904_InitialCreateFull"))
        {
            RegisterMigration(conn, logger, "20260421230904_InitialCreateFull", productVersion);
        }

        // ── Migrations 2 & 3: AddRoutines + AddRoutineLastTriggeredAt ──────────
        // If routines table missing: unregister both so Migrate() creates them fresh.
        // If routines table exists: register what's present, unregister what's not.
        if (!TableExists(conn, "routines"))
        {
            if (MigrationApplied(conn, "20260422115654_AddRoutines"))
            {
                logger.LogInformation("routines table missing — unregistering AddRoutines so Migrate() creates it");
                UnregisterMigration(conn, "20260422115654_AddRoutines");
            }
            if (MigrationApplied(conn, "20260423193840_AddRoutineLastTriggeredAt"))
            {
                logger.LogInformation("routines table missing — unregistering AddRoutineLastTriggeredAt");
                UnregisterMigration(conn, "20260423193840_AddRoutineLastTriggeredAt");
            }
        }
        else
        {
            if (!MigrationApplied(conn, "20260422115654_AddRoutines"))
                RegisterMigration(conn, logger, "20260422115654_AddRoutines", productVersion);

            if (!ColumnExists(conn, "routines", "last_triggered_at")
                && MigrationApplied(conn, "20260423193840_AddRoutineLastTriggeredAt"))
            {
                logger.LogInformation("last_triggered_at missing — unregistering AddRoutineLastTriggeredAt so Migrate() adds it");
                UnregisterMigration(conn, "20260423193840_AddRoutineLastTriggeredAt");
            }
            else if (ColumnExists(conn, "routines", "last_triggered_at")
                && !MigrationApplied(conn, "20260423193840_AddRoutineLastTriggeredAt"))
            {
                RegisterMigration(conn, logger, "20260423193840_AddRoutineLastTriggeredAt", productVersion);
            }
        }
    }
    finally
    {
        conn.Close();
    }

    // Apply any migrations not yet in history (e.g. routines table on old DB)
    db.Database.Migrate();
}

static bool TableExists(DbConnection conn, string tableName)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText =
        $"SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = 'public' AND table_name = '{tableName}'";
    return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
}

static bool ColumnExists(DbConnection conn, string tableName, string columnName)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText =
        $"SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = 'public' AND table_name = '{tableName}' AND column_name = '{columnName}'";
    return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
}

static bool MigrationApplied(DbConnection conn, string migrationId)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"SELECT COUNT(1) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migrationId}'";
    return (long)(cmd.ExecuteScalar() ?? 0L) > 0;
}

static void UnregisterMigration(DbConnection conn, string migrationId)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = $"DELETE FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migrationId}'";
    cmd.ExecuteNonQuery();
}

static void RegisterMigration(DbConnection conn, ILogger logger, string migrationId, string productVersion)
{
    logger.LogInformation("Registering already-applied migration: {MigrationId}", migrationId);
    using var cmd = conn.CreateCommand();
    cmd.CommandText =
        $"INSERT INTO \"__EFMigrationsHistory\" (\"MigrationId\", \"ProductVersion\") VALUES ('{migrationId}', '{productVersion}')";
    cmd.ExecuteNonQuery();
}

static void Exec(DbConnection conn, string sql)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = sql;
    cmd.ExecuteNonQuery();
}

public partial class Program { }
