namespace StatementVault.Domain;

public interface IStatementObjectStore
{
    Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken);

    Task<StatementObject?> GetAsync(string key, CancellationToken cancellationToken);

    Task<Uri> CreatePresignedGetUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken);
}
