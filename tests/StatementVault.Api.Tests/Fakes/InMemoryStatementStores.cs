using System.Collections.Concurrent;
using StatementVault.Domain;

namespace StatementVault.Api.Tests.Fakes;

internal sealed class InMemoryStatementRepository : IStatementRepository
{
    private readonly ConcurrentDictionary<(string AccountId, string StatementId), Statement> _items = new();

    public Task PutAsync(Statement statement, CancellationToken cancellationToken)
    {
        _items[(statement.AccountId, statement.StatementId)] = statement;
        return Task.CompletedTask;
    }

    public Task<Statement?> GetAsync(string accountId, string statementId, CancellationToken cancellationToken)
    {
        _items.TryGetValue((accountId, statementId), out var statement);
        return Task.FromResult(statement);
    }

    public Task<StatementPage> QueryByAccountAsync(
        string accountId,
        int limit,
        string? paginationToken,
        CancellationToken cancellationToken)
    {
        var matches = _items.Values
            .Where(item => item.AccountId == accountId)
            .OrderByDescending(item => item.CreatedAtUtc)
            .ToArray();

        var skip = 0;
        if (!string.IsNullOrWhiteSpace(paginationToken) && int.TryParse(paginationToken, out var parsed))
        {
            skip = parsed;
        }

        var page = matches.Skip(skip).Take(limit).ToArray();
        var next = skip + page.Length < matches.Length ? (skip + page.Length).ToString() : null;
        return Task.FromResult(new StatementPage(page, next));
    }
}

internal sealed class InMemoryStatementObjectStore : IStatementObjectStore
{
    private readonly ConcurrentDictionary<string, (byte[] Bytes, string ContentType)> _objects = new();

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        _objects[key] = (buffer.ToArray(), contentType);
    }

    public Task<StatementObject?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (!_objects.TryGetValue(key, out var stored))
        {
            return Task.FromResult<StatementObject?>(null);
        }

        return Task.FromResult<StatementObject?>(
            new StatementObject(new MemoryStream(stored.Bytes), stored.ContentType, stored.Bytes.Length));
    }

    public Task<Uri> CreatePresignedGetUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        if (!_objects.ContainsKey(key))
        {
            throw new InvalidOperationException($"Object '{key}' does not exist.");
        }

        _ = lifetime;
        return Task.FromResult(new Uri($"https://statements.example.test/{key}?exp=fake"));
    }
}
