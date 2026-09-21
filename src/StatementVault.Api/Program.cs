using FluentValidation;
using Scalar.AspNetCore;
using StatementVault.Api.Features.Statements;
using StatementVault.Api.Features.Statements.UploadStatement;
using StatementVault.Api.Hosting;
using StatementVault.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["correlationId"] = CorrelationIdMiddleware.Get(context.HttpContext);
    };
});
builder.Services.AddExceptionHandler<UnhandledExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddValidatorsFromAssemblyContaining<UploadStatementValidator>();
builder.Services.AddStatementVaultPersistence(builder.Configuration, builder.Environment);
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = UploadStatementValidator.MaxFileBytes;
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseMiddleware<CorrelationIdMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapDefaultEndpoints();
app.MapStatementEndpoints();

app.Run();

public partial class Program;
