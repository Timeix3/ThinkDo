using Common.Data;
using Common.Models;
using Microsoft.EntityFrameworkCore;
using AppApi.Repositories.Interfaces;

namespace AppApi.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly AppDbContext _context;
    public ProjectRepository(AppDbContext context) => _context = context;

    public async Task<IEnumerable<ProjectItem>> GetAllAsync(string userId)
    {
        return await _context.Projects
            .Where(p => p.UserId == userId && p.DeletedAt == null)
            .OrderByDescending(p => p.IsDefault) // Сначала Текучка
            .ThenBy(p => p.Name)
            .ToListAsync();
    }

    public async Task<ProjectItem?> GetByIdAsync(int id, string userId, bool includeTasks = false)
    {
        var query = _context.Projects.AsQueryable();
        if (includeTasks)
            query = query.Include(p => p.Tasks.Where(t => t.DeletedAt == null));

        return await query.FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId && p.DeletedAt == null);
    }

    public async Task<ProjectItem> AddAsync(ProjectItem project)
    {
        project.CreatedAt = DateTime.UtcNow;
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();
        return project;
    }

    public async Task UpdateAsync(ProjectItem project)
    {
        project.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Update(project);
        await _context.SaveChangesAsync();
    }

    public async Task<ProjectItem?> GetDefaultProjectAsync(string userId, bool includeDeleted)
    {
        var query = _context.Projects.AsQueryable();

        if (includeDeleted)
        {
            // IgnoreQueryFilters отключает глобальный фильтр (DeletedAt == null)
            // Это позволяет нам найти "удаленную" Текучку, чтобы восстановить её
            query = query.IgnoreQueryFilters();
        }

        return await query.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.IsDefault == true);
    }
}