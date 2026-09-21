using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using StatementVault.Api.Features.Statements;

namespace StatementVault.Api.Tests;

public sealed class StatementsApiTests : IClassFixture<ApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public StatementsApiTests(ApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Upload_ThenGet_List_AndDownload()
    {
        using var content = CreateMultipart("acc-1001", "2026-01-01", "2026-01-31", "jan.pdf", "application/pdf", "pdf-bytes");
        using var uploadRequest = new HttpRequestMessage(HttpMethod.Post, "/api/statements")
        {
            Content = content
        };
        uploadRequest.Headers.TryAddWithoutValidation("X-Correlation-ID", "test-correlation");

        var upload = await _client.SendAsync(uploadRequest, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, upload.StatusCode);

        var created = await upload.Content.ReadFromJsonAsync<StatementResponse>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(created);
        Assert.Equal("acc-1001", created.AccountId);
        Assert.Equal("test-correlation", created.CorrelationId);
        Assert.Equal("statements/acc-1001/" + created.StatementId + ".pdf", created.S3Key);

        var metadata = await _client.GetAsync(
            $"/api/statements/{created.AccountId}/{created.StatementId}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, metadata.StatusCode);

        var list = await _client.GetFromJsonAsync<StatementListResponse>(
            $"/api/statements/{created.AccountId}",
            JsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(list);
        Assert.Contains(list.Items, item => item.StatementId == created.StatementId);

        var download = await _client.GetAsync(
            $"/api/statements/{created.AccountId}/{created.StatementId}/content",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal("pdf-bytes", await download.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

        var presign = await _client.GetFromJsonAsync<PresignedContentResponse>(
            $"/api/statements/{created.AccountId}/{created.StatementId}/content?mode=url",
            JsonOptions,
            TestContext.Current.CancellationToken);
        Assert.NotNull(presign);
        Assert.Contains(created.S3Key, presign.Url.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Upload_MissingFile_ReturnsValidationProblem()
    {
        using var form = new MultipartFormDataContent
        {
            { new StringContent("acc-1001"), "accountId" },
            { new StringContent("2026-01-01"), "periodStart" },
            { new StringContent("2026-01-31"), "periodEnd" }
        };

        var response = await _client.PostAsync("/api/statements", form, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Get_UnknownStatement_ReturnsNotFound()
    {
        var response = await _client.GetAsync(
            "/api/statements/acc-1001/does-not-exist",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static MultipartFormDataContent CreateMultipart(
        string accountId,
        string periodStart,
        string periodEnd,
        string fileName,
        string contentType,
        string body)
    {
        var file = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(body));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);

        return new MultipartFormDataContent
        {
            { new StringContent(accountId), "accountId" },
            { new StringContent(periodStart), "periodStart" },
            { new StringContent(periodEnd), "periodEnd" },
            { file, "file", fileName }
        };
    }
}
