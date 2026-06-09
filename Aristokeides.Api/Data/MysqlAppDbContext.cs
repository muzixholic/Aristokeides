using Microsoft.EntityFrameworkCore;

namespace Aristokeides.Api.Data;

public class MysqlAppDbContext : AppDbContext
{
    public MysqlAppDbContext(DbContextOptions<MysqlAppDbContext> options) : base(options)
    {
    }
}
