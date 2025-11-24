using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Convex.Client.Extensions.DependencyInjection;
using CursorPlayground.Blazor;
using CursorPlayground.Shared.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Add Convex client
builder.Services.AddConvex(builder.Configuration.GetSection("Convex"));

// Add cursor service
builder.Services.AddScoped<CursorService>();

await builder.Build().RunAsync();
