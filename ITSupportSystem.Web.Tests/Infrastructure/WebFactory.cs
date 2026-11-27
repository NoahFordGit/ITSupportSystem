using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace ITSupportSystem.Web.Tests.Infrastructure;

/// <summary>
/// Boots the real Web app in memory, but points its named "Api" HttpClient at a
/// FakeApi instead of the real API. Each factory gets its own FakeApi.
/// </summary>
public class WebFactory : WebApplicationFactory<Program>
{
    public FakeApi Api { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Program.cs reads this when it registers the client. The host name is
        // never contacted, because the handler below answers every request.
        builder.UseSetting("Api:BaseUrl", "http://fake-api/");

        builder.ConfigureServices(services =>
        {
            // Adds to the app's existing "Api" registration: same BaseAddress,
            // but the bottom of the pipeline is the fake instead of the network.
            services.AddHttpClient("Api")
                .ConfigurePrimaryHttpMessageHandler(() => new FakeApiHandler(Api));
        });
    }
}
