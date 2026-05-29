using System.Threading.Channels;

namespace Aristokeides.Api.Services;

public class RepositoryCreationChannel
{
    private readonly Channel<Guid> _channel;

    public RepositoryCreationChannel()
    {
        _channel = Channel.CreateUnbounded<Guid>();
    }

    public async ValueTask EnqueueAsync(Guid repositoryId, CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(repositoryId, cancellationToken);
    }

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
