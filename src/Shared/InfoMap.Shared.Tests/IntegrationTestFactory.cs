using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;

namespace InfoMap.Shared.Tests;

public abstract class IntegrationTestFactory<TProgram> : WebApplicationFactory<TProgram> where TProgram : class
{
    protected readonly SqliteConnection Connection = new("DataSource=:memory:");

    protected IntegrationTestFactory() => Connection.Open();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
        => builder.UseEnvironment("Testing");

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) Connection.Dispose();
    }
}
