using Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Common.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<TaskItem> Tasks => Set<TaskItem>();

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

            entity.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired()
                .HasDefaultValueSql("NOW()");

            entity.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at");

            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_tasks_user_id");

            entity.HasIndex(e => new { e.UserId, e.Id })
                .HasDatabaseName("ix_tasks_user_id_id");
        });
    }
}