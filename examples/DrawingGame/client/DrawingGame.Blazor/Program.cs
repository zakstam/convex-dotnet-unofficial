using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Convex.Client.Extensions.DependencyInjection;
using DrawingGame.Blazor;
using DrawingGame.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Convex client
builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));

// Add game service
builder.Services.AddScoped<DrawingGameService>();

await builder.Build().RunAsync();
