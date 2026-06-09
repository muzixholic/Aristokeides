using System.ComponentModel.DataAnnotations;

namespace Aristokeides.Api.Models;

public class AdminSettingsViewModel
{
    [Required]
    public string DatabaseProvider { get; set; } = "SQLite";

    [Required]
    public string ConnectionString { get; set; } = string.Empty;

    public int SshPort { get; set; } = 2222;

    public string SshDomain { get; set; } = "localhost";
}
