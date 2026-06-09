using System.ComponentModel.DataAnnotations;

namespace Aristokeides.Api.Models;

public class SetupViewModel
{
    [Required]
    public string DatabaseProvider { get; set; } = "SQLite";

    // For SQLite
    public string? SqliteFilePath { get; set; } = "aristokeides.db";

    // For Postgres/MySQL
    public string? DbHost { get; set; } = "localhost";
    public int? DbPort { get; set; }
    public string? DbName { get; set; } = "aristokeides";
    public string? DbUsername { get; set; }
    public string? DbPassword { get; set; }

    [Required]
    [StringLength(256)]
    public string AdminUsername { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string AdminEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string AdminPassword { get; set; } = string.Empty;

    [Required]
    [Compare(nameof(AdminPassword), ErrorMessage = "Passwords do not match.")]
    public string AdminPasswordConfirm { get; set; } = string.Empty;
}
