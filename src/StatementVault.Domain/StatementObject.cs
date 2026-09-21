namespace StatementVault.Domain;

public sealed class StatementObject : IAsyncDisposable
{
    private readonly IAsyncDisposable? _owner;

    public StatementObject(Stream content, string contentType, long contentLength, IAsyncDisposable? owner = null)
    {
        Content = content;
        ContentType = contentType;
        ContentLength = contentLength;
        _owner = owner;
    }

    public Stream Content { get; }

    public string ContentType { get; }

    public long ContentLength { get; }

    public async ValueTask DisposeAsync()
    {
        if (_owner is not null)
        {
            await _owner.DisposeAsync();
        }
        else
        {
            await Content.DisposeAsync();
        }
    }
}
