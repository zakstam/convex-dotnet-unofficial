using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Convex.Client.Extensions.DependencyInjection;
using TicTacToe.Shared.Services;
using Convex.Client;
using TicTacToe.Blazor;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        _ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // Add Convex client - bind to the "Convex" configuration section
        _ = builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));

        // Register TicTacToe service
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new TicTacToeService(convexClient);
        });

        await builder.Build().RunAsync();
    }
}
