using Common.Models;
using Microsoft.EntityFrameworkCore;
using Common.Enums;

namespace Common.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<InboxItem> InboxItems => Set<InboxItem>();
    public DbSet<ProjectItem> Projects => Set<ProjectItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.ToTable("tasks");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .HasMaxLength(2000);

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasDefaultValue(TasksStatus.Available);

            entity.Property(e => e.BlockedByTaskId)
                .HasColumnName("blocked_by_task_id");

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_tasks_user_id");

            entity.HasIndex(e => new { e.UserId, e.Id })
                .HasDatabaseName("ix_tasks_user_id_id");

            entity.HasIndex(e => e.DeletedAt)
                .HasDatabaseName("ix_tasks_deleted_at");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("ix_tasks_status");

            entity.HasIndex(e => e.BlockedByTaskId)
                .HasDatabaseName("ix_tasks_blocked_by_task_id");
        });

        modelBuilder.Entity<InboxItem>(entity =>
        {
            entity.ToTable("inbox_items");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .HasColumnName("user_id")
                .HasMaxLength(450)
                .IsRequired();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_inbox_items_user_id");

            entity.HasIndex(e => new { e.UserId, e.Id })
                .HasDatabaseName("ix_inbox_items_user_id_id");

            entity.HasIndex(e => e.DeletedAt)
                .HasDatabaseName("ix_inbox_items_deleted_at");
        });

        modelBuilder.Entity<ProjectItem>(entity =>
        {
            entity.ToTable("projects");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Name)
                .HasMaxLength(200).IsRequired();

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.UserId)
                .HasMaxLength(450).IsRequired();

            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false);

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_projects_user_id");
                
            entity.HasIndex(e => e.DeletedAt)
                .HasDatabaseName("ix_projects_deleted_at");
        });

        modelBuilder.Entity<TaskItem>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}