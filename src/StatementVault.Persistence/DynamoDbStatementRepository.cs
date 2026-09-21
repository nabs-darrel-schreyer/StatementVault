using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Options;
using StatementVault.Domain;

namespace StatementVault.Persistence;

public sealed class DynamoDbStatementRepository : IStatementRepository
{
    private const string Pk = "PK";
    private const string Sk = "SK";

    private readonly IAmazonDynamoDB _dynamoDb;
    private readonly AwsOptions _options;

    public DynamoDbStatementRepository(IAmazonDynamoDB dynamoDb, IOptions<AwsOptions> options)
    {
        _dynamoDb = dynamoDb;
        _options = options.Value;
    }

    public async Task PutAsync(Statement statement, CancellationToken cancellationToken)
    {
        var request = new PutItemRequest
        {
            TableName = _options.TableName,
            ConditionExpression = "attribute_not_exists(PK) AND attribute_not_exists(SK)",
            Item = new Dictionary<string, AttributeValue>
            {
                [Pk] = new(StatementKeys.PartitionKey(statement.AccountId)),
                [Sk] = new(StatementKeys.SortKey(statement.StatementId)),
                ["PeriodStart"] = new(statement.PeriodStart.ToString("yyyy-MM-dd")),
                ["PeriodEnd"] = new(statement.PeriodEnd.ToString("yyyy-MM-dd")),
                ["ContentType"] = new(statement.ContentType),
                ["SizeBytes"] = new() { N = statement.SizeBytes.ToString() },
                ["S3Key"] = new(statement.S3Key),
                ["CreatedAtUtc"] = new(statement.CreatedAtUtc.UtcDateTime.ToString("O")),
                ["CorrelationId"] = new(statement.CorrelationId)
            }
        };

        await _dynamoDb.PutItemAsync(request, cancellationToken);
    }

    public async Task<Statement?> GetAsync(string accountId, string statementId, CancellationToken cancellationToken)
    {
        var response = await _dynamoDb.GetItemAsync(new GetItemRequest
        {
            TableName = _options.TableName,
            Key = new Dictionary<string, AttributeValue>
            {
                [Pk] = new(StatementKeys.PartitionKey(accountId)),
                [Sk] = new(StatementKeys.SortKey(statementId))
            },
            ConsistentRead = true
        }, cancellationToken);

        return response.IsItemSet ? Map(response.Item) : null;
    }

    public async Task<StatementPage> QueryByAccountAsync(
        string accountId,
        int limit,
        string? paginationToken,
        CancellationToken cancellationToken)
    {
        var request = new QueryRequest
        {
            TableName = _options.TableName,
            KeyConditionExpression = "PK = :pk AND begins_with(SK, :sk)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                [":pk"] = new(StatementKeys.PartitionKey(accountId)),
                [":sk"] = new(StatementKeys.StatementPrefix)
            },
            Limit = limit,
            ScanIndexForward = false
        };

        var startKey = DecodeToken(paginationToken);
        if (startKey is not null)
        {
            request.ExclusiveStartKey = startKey;
        }

        var response = await _dynamoDb.QueryAsync(request, cancellationToken);
        var items = response.Items.Select(Map).ToArray();
        var nextToken = response.LastEvaluatedKey is { Count: > 0 }
            ? EncodeToken(response.LastEvaluatedKey)
            : null;

        return new StatementPage(items, nextToken);
    }

    private static Statement Map(Dictionary<string, AttributeValue> item)
    {
        var accountId = StatementKeys.AccountIdFromPartitionKey(item[Pk].S);
        var statementId = StatementKeys.StatementIdFromSortKey(item[Sk].S);

        return new Statement
        {
            AccountId = accountId,
            StatementId = statementId,
            PeriodStart = DateOnly.Parse(item["PeriodStart"].S),
            PeriodEnd = DateOnly.Parse(item["PeriodEnd"].S),
            ContentType = item["ContentType"].S,
            SizeBytes = long.Parse(item["SizeBytes"].N),
            S3Key = item["S3Key"].S,
            CreatedAtUtc = DateTimeOffset.Parse(item["CreatedAtUtc"].S),
            CorrelationId = item["CorrelationId"].S,
            FileName = Path.GetFileName(item["S3Key"].S)
        };
    }

    private static string EncodeToken(Dictionary<string, AttributeValue> key)
    {
        var dto = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["pk"] = key[Pk].S,
            ["sk"] = key[Sk].S
        };

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(dto)));
    }

    private static Dictionary<string, AttributeValue>? DecodeToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var json = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var dto = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (dto is null || !dto.TryGetValue("pk", out var pk) || !dto.TryGetValue("sk", out var sk))
            {
                throw new FormatException("Pagination token is missing required keys.");
            }

            return new Dictionary<string, AttributeValue>
            {
                [Pk] = new(pk),
                [Sk] = new(sk)
            };
        }
        catch (Exception ex) when (ex is FormatException or JsonException or ArgumentException)
        {
            throw new FormatException("Pagination token is invalid.", ex);
        }
    }
}
