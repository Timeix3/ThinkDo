using Common.Data;
using Microsoft.EntityFrameworkCore;

namespace AppApi.Tests.Helpers;

public static class DbContextHelper
{
    public static AppDbContext CreateInMemoryContext(string? dbName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
