using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Data;

public class SqliteAppDbContext : AppDbContext
{
    public SqliteAppDbContext(DbContextOptions<SqliteAppDbContext> options) : base(options)
    {
    }
}
