using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Logging;
using RealtimeChat.Frontend;
using RealtimeChat.Frontend.Services;
using RealtimeChatClerk.Shared.Services;
using Convex.Client;
using Convex.Client.Extensions.Clerk.Blazor;
using Convex.Generated;
using Serilog;
using Serilog.Extensions.Logging;

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

builder.Logging.ClearProviders();
builder.Logging.AddSerilog();

// Configure logging to show Debug level logs in browser console
// Set default to Information to reduce noise, but allow Debug for Convex
builder.Logging.SetMinimumLevel(LogLevel.Information);
builder.Logging.AddFilter("Convex", LogLevel.Debug);
// Filter out Blazor rendering logs
builder.Logging.AddFilter("Microsoft.AspNetCore.Components", LogLevel.Warning);
builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Note: Blazor WebAssembly loads configuration from wwwroot/appsettings.json at runtime
// This file should match examples/RealTimeChat/appsettings.json
// Update both files to keep them in sync, or use environment variables

// Add Convex client with Clerk authentication for Blazor WebAssembly
// This automatically registers BlazorClerkTokenService and injects required JavaScript
builder.Services.AddConvexWithClerkForBlazor(
    builder.Configuration.GetSection("Clerk"),
    builder.Configuration.GetSection("Convex"));

// Register chat services - using shared services directly
builder.Services.AddScoped<ChatState>();
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.ChatService>(sp =>
{
    var convexClient = sp.GetRequiredService<IConvexClient>();
    var config = builder.Configuration;
    var initialMessageLimit = config.GetValue<int>("Convex:InitialMessageLimit", 10);
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
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.PresenceService>(sp =>
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
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.ReactionService>(sp =>
{
    var convexClient = sp.GetRequiredService<IConvexClient>();
    return new RealtimeChatClerk.Shared.Services.ReactionService(
        convexClient,
        toggleReactionFunctionName: ConvexFunctions.Mutations.ToggleReaction,
        getReactionsFunctionName: ConvexFunctions.Queries.GetReactions);
});
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.FileService>(sp =>
{
    var convexClient = sp.GetRequiredService<IConvexClient>();
    return new RealtimeChatClerk.Shared.Services.FileService(convexClient);
});
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.ReplyService>(sp =>
{
    var convexClient = sp.GetRequiredService<IConvexClient>();
    return new RealtimeChatClerk.Shared.Services.ReplyService(
        convexClient,
        getRepliesFunctionName: ConvexFunctions.Queries.GetReplies,
        sendReplyFunctionName: ConvexFunctions.Mutations.SendReply
    );
});
builder.Services.AddScoped<RealtimeChatClerk.Shared.Services.ReadReceiptService>(sp =>
{
    var convexClient = sp.GetRequiredService<IConvexClient>();
    return new RealtimeChatClerk.Shared.Services.ReadReceiptService(
        convexClient,
        markMessageReadFunctionName: "functions/markMessageRead",
        getMessageReadsFunctionName: "functions/getMessageReads");
});

await builder.Build().RunAsync();
