#r "nuget: Npgsql.EntityFrameworkCore.PostgreSQL, 10.0.2"
#r "nuget: Microsoft.EntityFrameworkCore, 10.0.8"
using System.Linq;

// Let's connect using Npgsql directly for a quick query
var connString = "Host=localhost;Port=5432;Database=aristokeides;Username=postgres;Password=postgres";
using var conn = new Npgsql.NpgsqlConnection(connString);
conn.Open();

using var cmd1 = new Npgsql.NpgsqlCommand("SELECT \"Id\", \"Username\", \"Email\" FROM \"Users\"", conn);
using var reader1 = cmd1.ExecuteReader();
System.Console.WriteLine("--- USERS ---");
while(reader1.Read()) {
    System.Console.WriteLine($"Id: {reader1[0]}, Username: {reader1[1]}, Email: {reader1[2]}");
}
reader1.Close();

using var cmd2 = new Npgsql.NpgsqlCommand("SELECT \"Id\", \"Name\", \"OwnerId\" FROM \"Repositories\"", conn);
using var reader2 = cmd2.ExecuteReader();
System.Console.WriteLine("--- REPOS ---");
while(reader2.Read()) {
    System.Console.WriteLine($"Id: {reader2[0]}, Name: {reader2[1]}, OwnerId: {reader2[2]}");
}
