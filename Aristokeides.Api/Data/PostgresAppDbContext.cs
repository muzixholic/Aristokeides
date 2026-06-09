using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Data;

public class PostgresAppDbContext : AppDbContext
{
    public PostgresAppDbContext(DbContextOptions<PostgresAppDbContext> options) : base(options)
    {
    }
}
