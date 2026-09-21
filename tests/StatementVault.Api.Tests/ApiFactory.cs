using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StatementVault.Api.Tests.Fakes;
using StatementVault.Domain;

namespace StatementVault.Api.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IStatementRepository>();
            services.RemoveAll<IStatementObjectStore>();
            services.AddSingleton<IStatementRepository, InMemoryStatementRepository>();
            services.AddSingleton<IStatementObjectStore, InMemoryStatementObjectStore>();
        });
    }
}
