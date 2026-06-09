using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Aristokeides.Api.Data;

public class PostgresDbContextFactory : IDesignTimeDbContextFactory<PostgresAppDbContext>
{
    public PostgresAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PostgresAppDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=dummy", x => x.MigrationsAssembly("Aristokeides.Api"));
        return new PostgresAppDbContext(optionsBuilder.Options);
    }
}

public class SqliteDbContextFactory : IDesignTimeDbContextFactory<SqliteAppDbContext>
{
    public SqliteAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SqliteAppDbContext>();
        optionsBuilder.UseSqlite("Data Source=dummy.db", x => x.MigrationsAssembly("Aristokeides.Api"));
        return new SqliteAppDbContext(optionsBuilder.Options);
    }
}

public class MysqlDbContextFactory : IDesignTimeDbContextFactory<MysqlAppDbContext>
{
    public MysqlAppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MysqlAppDbContext>();
        optionsBuilder.UseMySQL("Server=localhost;Database=dummy", x => x.MigrationsAssembly("Aristokeides.Api"));
        return new MysqlAppDbContext(optionsBuilder.Options);
    }
}
