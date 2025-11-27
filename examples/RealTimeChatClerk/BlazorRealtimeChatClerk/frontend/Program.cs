using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using RealtimeChat.Frontend;
using RealtimeChat.Frontend.Services;
using Convex.Client;
using Convex.Client.Extensions.Clerk.Blazor;
using Convex.Generated;
using Serilog;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.RootComponents.Add<App>("#app");
        builder.RootComponents.Add<HeadOutlet>("head::after");

        // Configure Serilog for browser console logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft.AspNetCore.Components", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Information)
            .WriteTo.BrowserConsole()
            .CreateLogger();

        _ = builder.Logging.ClearProviders();
        _ = builder.Logging.AddSerilog();

        // Configure logging to show Debug level logs in browser console
        // Set default to Information to reduce noise, but allow Debug for Convex
        _ = builder.Logging.SetMinimumLevel(LogLevel.Information);
        _ = builder.Logging.AddFilter("Convex", LogLevel.Debug);
        // Filter out Blazor rendering logs
        _ = builder.Logging.AddFilter("Microsoft.AspNetCore.Components", LogLevel.Warning);
        _ = builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

        _ = builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

        // Note: Blazor WebAssembly loads configuration from wwwroot/appsettings.json at runtime
        // This file should match examples/RealTimeChat/appsettings.json
        // Update both files to keep them in sync, or use environment variables

        // Add Convex client with Clerk authentication for Blazor WebAssembly
        // This automatically registers BlazorClerkTokenService and injects required JavaScript
        _ = builder.Services.AddConvexWithClerkForBlazor(
            builder.Configuration.GetSection("Clerk"),
            builder.Configuration.GetSection("Convex"));

        // Register chat services - using shared services directly
        _ = builder.Services.AddScoped<ChatState>();
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            var config = builder.Configuration;
            var initialMessageLimit = config.GetValue("Convex:InitialMessageLimit", 10);
            return new RealtimeChatClerk.Shared.Services.ChatService(
                convexClient,
                getMessagesFunctionName: ConvexFunctions.Queries.GetMessages,
                sendMessageFunctionName: ConvexFunctions.Mutations.SendMessage,
                initialMessageLimit: initialMessageLimit,
                editMessageFunctionName: ConvexFunctions.Mutations.EditMessage,
                deleteMessageFunctionName: ConvexFunctions.Mutations.DeleteMessage,
                searchMessagesFunctionName: ConvexFunctions.Queries.SearchMessages
            );
        });
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new RealtimeChatClerk.Shared.Services.PresenceService(
                convexClient,
                updatePresenceFunctionName: ConvexFunctions.Mutations.UpdatePresence,
                setTypingFunctionName: ConvexFunctions.Mutations.SetTyping,
                getOnlineUsersFunctionName: ConvexFunctions.Queries.GetOnlineUsers,
                getTypingUsersFunctionName: ConvexFunctions.Queries.GetTypingUsers
            );
        });
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new RealtimeChatClerk.Shared.Services.ReactionService(
                convexClient,
                toggleReactionFunctionName: ConvexFunctions.Mutations.ToggleReaction,
                getReactionsFunctionName: ConvexFunctions.Queries.GetReactions);
        });
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new RealtimeChatClerk.Shared.Services.FileService(convexClient);
        });
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new RealtimeChatClerk.Shared.Services.ReplyService(
                convexClient,
                getRepliesFunctionName: ConvexFunctions.Queries.GetReplies,
                sendReplyFunctionName: ConvexFunctions.Mutations.SendReply
            );
        });
        _ = builder.Services.AddScoped(sp =>
        {
            var convexClient = sp.GetRequiredService<IConvexClient>();
            return new RealtimeChatClerk.Shared.Services.ReadReceiptService(
                convexClient,
                markMessageReadFunctionName: ConvexFunctions.Mutations.MarkMessageRead,
                getMessageReadsFunctionName: ConvexFunctions.Queries.GetMessageReads);
        });

        await builder.Build().RunAsync();
    }
}
