using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using StatementVault.Domain;

namespace StatementVault.Persistence;

public sealed class S3StatementObjectStore : IStatementObjectStore
{
    private readonly IAmazonS3 _s3;
    private readonly AwsOptions _options;

    public S3StatementObjectStore(IAmazonS3 s3, IOptions<AwsOptions> options)
    {
        _s3 = s3;
        _options = options.Value;
    }

    public async Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken)
    {
        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = content,
            ContentType = contentType,
            ServerSideEncryptionMethod = ServerSideEncryptionMethod.AES256,
            AutoCloseStream = false
        };

        await _s3.PutObjectAsync(request, cancellationToken);
    }

    public async Task<StatementObject?> GetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _s3.GetObjectAsync(_options.BucketName, key, cancellationToken);
            return new StatementObject(
                response.ResponseStream,
                response.Headers.ContentType ?? "application/octet-stream",
                response.ContentLength,
                new S3ResponseOwner(response));
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public Task<Uri> CreatePresignedGetUrlAsync(string key, TimeSpan lifetime, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var request = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = DateTime.UtcNow.Add(lifetime)
        };

        var url = _s3.GetPreSignedURL(request);
        return Task.FromResult(new Uri(url));
    }

    private sealed class S3ResponseOwner(GetObjectResponse response) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            response.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
