using Microsoft.Extensions.Configuration;

namespace Aristokeides.Api.Services.Ssh;

public class SshUrlHelper
{
    private readonly IConfiguration _configuration;

    public SshUrlHelper(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GetSshCloneUrl(string username, string repoName)
    {
        var port = _configuration.GetValue<int>("Ssh:Port", 2222);
        var domain = _configuration.GetValue<string>("Ssh:Domain", "localhost");
        return $"ssh://git@{domain}:{port}/{username}/{repoName}.git";
    }

    public string GetHttpCloneUrl(string username, string repoName)
    {
        return $"http://localhost:5000/{username}/{repoName}.git";
    }
}
