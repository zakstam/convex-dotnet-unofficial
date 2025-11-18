using System.Reflection;
using Convex.Client.Shared.ErrorHandling;
using RealtimeChat.Shared.Configuration;
using RealtimeChat.Shared.Models;

namespace SeedDatabase;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Convex Chat Database Seeder ===\n");

        // Load configuration from the shared appsettings.json
        // The shared file is at examples/RealTimeChat/appsettings.json
        var executableDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? AppDomain.CurrentDomain.BaseDirectory;
        var sharedAppSettingsPath = Path.GetFullPath(
            Path.Combine(executableDir, "..", "..", "..", "..", "appsettings.json"));

        var config = ChatConfiguration.Load(
            appsettingsPath: sharedAppSettingsPath,
            defaultDeploymentUrl: "https://your-deployment.convex.cloud");

        if (string.IsNullOrWhiteSpace(config.DeploymentUrl) || config.DeploymentUrl == "https://your-deployment.convex.cloud")
        {
            Console.Error.WriteLine("ERROR: Deployment URL not configured!");
            Console.Error.WriteLine($"Tried to load from: {sharedAppSettingsPath}");
            Console.Error.WriteLine($"File exists: {File.Exists(sharedAppSettingsPath)}");
            Console.Error.WriteLine("Please set CONVEX_DEPLOYMENT_URL environment variable or update appsettings.json");
            Console.Error.WriteLine($"Expected location: examples/RealTimeChat/appsettings.json");
            Environment.Exit(1);
        }

        Console.WriteLine($"Connecting to: {config.DeploymentUrl}\n");

        // Create client
        var client = config.CreateClient();

        // Sample messages to seed (60 messages)
        var sampleMessages = new[]
        {
            new { Username = "Alice", Text = "Hey everyone! 👋 Welcome to the chat!" },
            new { Username = "Bob", Text = "Thanks Alice! Excited to be here." },
            new { Username = "Charlie", Text = "This is a great chat app! Really enjoying the real-time features." },
            new { Username = "Alice", Text = "Glad you like it! The auto-scroll feature is pretty neat too." },
            new { Username = "Diana", Text = "Has anyone tried the file upload feature yet?" },
            new { Username = "Bob", Text = "Not yet, but I'm planning to test it soon." },
            new { Username = "Eve", Text = "The markdown support is awesome! You can use **bold** and *italic* text." },
            new { Username = "Charlie", Text = "Really? Let me try: `code blocks` work too!" },
            new { Username = "Alice", Text = "Yes! And you can mention users with @username" },
            new { Username = "Bob", Text = "@Alice Thanks for the tip!" },
            new { Username = "Diana", Text = "The reactions feature is fun too! 😄" },
            new { Username = "Eve", Text = "I love how smooth the UI is. Great work on the design!" },
            new { Username = "Charlie", Text = "Has anyone noticed the typing indicators? They're really helpful." },
            new { Username = "Alice", Text = "Yes! It's great to see when someone is typing." },
            new { Username = "Bob", Text = "The search functionality is also really useful for finding old messages." },
            new { Username = "Diana", Text = "I've been using it to find links I shared earlier. Very handy!" },
            new { Username = "Eve", Text = "The dark mode toggle is a nice touch too 🌙" },
            new { Username = "Charlie", Text = "Agreed! I prefer dark mode for late-night chats." },
            new { Username = "Alice", Text = "Feel free to test out all the features! Let us know if you find any issues." },
            new { Username = "Bob", Text = "Will do! Thanks for building such a great chat app! 🎉" },
            new { Username = "Frank", Text = "Just joined! This looks amazing!" },
            new { Username = "Grace", Text = "Welcome @Frank! You're going to love it here." },
            new { Username = "Henry", Text = "The real-time updates are so smooth. No lag at all!" },
            new { Username = "Iris", Text = "I've been testing the message editing feature. Works perfectly!" },
            new { Username = "Alice", Text = "Yes, you can edit your messages after sending them." },
            new { Username = "Jack", Text = "What about deleting messages? Can we do that too?" },
            new { Username = "Bob", Text = "Yes! You can delete your own messages. Try it out!" },
            new { Username = "Kate", Text = "The emoji picker is really convenient! 😊😎🎉" },
            new { Username = "Liam", Text = "I love how you can react to messages with emojis!" },
            new { Username = "Mia", Text = "The user colors make it easy to distinguish who's who." },
            new { Username = "Noah", Text = "Has anyone tried the reply feature? It's great for threading!" },
            new { Username = "Olivia", Text = "Yes! I've been using it to keep conversations organized." },
            new { Username = "Paul", Text = "The message grouping by user is a nice touch." },
            new { Username = "Quinn", Text = "I appreciate that messages from the same user are grouped together." },
            new { Username = "Rachel", Text = "The timestamp formatting is really user-friendly." },
            new { Username = "Sam", Text = "I like how it shows 'Just now' for recent messages." },
            new { Username = "Tina", Text = "The 'Load older messages' button works great for scrolling back." },
            new { Username = "Alice", Text = "Yes, pagination makes it easy to browse through history." },
            new { Username = "Victor", Text = "I've been sharing code snippets. The markdown rendering is perfect!" },
            new { Username = "Wendy", Text = "The code blocks preserve formatting really well." },
            new { Username = "Xavier", Text = "Has anyone tested with very long messages?" },
            new { Username = "Yara", Text = "I tried a long message and it wrapped nicely in the UI." },
            new { Username = "Zoe", Text = "The message input area is really responsive." },
            new { Username = "Alice", Text = "You can also use keyboard shortcuts for sending messages!" },
            new { Username = "Bob", Text = "Enter to send, Shift+Enter for new line. Very intuitive!" },
            new { Username = "Charlie", Text = "The mention autocomplete is super helpful when tagging users." },
            new { Username = "Diana", Text = "I love how it filters users as you type @username" },
            new { Username = "Eve", Text = "The online users list is always up to date." },
            new { Username = "Frank", Text = "It's nice to see who's currently active in the chat." },
            new { Username = "Grace", Text = "The presence indicators work really well." },
            new { Username = "Henry", Text = "I've been testing the file uploads. Very smooth process!" },
            new { Username = "Iris", Text = "What file types are supported?" },
            new { Username = "Alice", Text = "You can upload images, documents, and other files. Try it!" },
            new { Username = "Jack", Text = "The file previews look great in the chat." },
            new { Username = "Kate", Text = "I uploaded a screenshot and it displayed perfectly!" },
            new { Username = "Liam", Text = "The attachment handling is really well done." },
            new { Username = "Mia", Text = "Has anyone tried the search feature with special characters?" },
            new { Username = "Noah", Text = "I searched for messages with emojis and it worked great!" },
            new { Username = "Olivia", Text = "The search highlights matching text which is helpful." },
            new { Username = "Paul", Text = "I love how you can click on search results to jump to the message." },
            new { Username = "Quinn", Text = "The UI animations are smooth and not distracting." },
            new { Username = "Rachel", Text = "Everything feels very polished and professional." },
            new { Username = "Sam", Text = "Great job to the developers! This is an excellent chat app! 🚀" },
        };

        Console.WriteLine($"Seeding {sampleMessages.Length} messages in batches...\n");

        var successCount = 0;
        var errorCount = 0;
        const int batchSize = 5; // Send 5 messages in parallel per batch

        // Process messages in batches
        for (int batchStart = 0; batchStart < sampleMessages.Length; batchStart += batchSize)
        {
            var batchEnd = Math.Min(batchStart + batchSize, sampleMessages.Length);
            var batch = sampleMessages[batchStart..batchEnd];

            // Create tasks for all messages in this batch
            var tasks = batch.Select(async (message, index) =>
            {
                try
                {
                    var messageArgs = new SendMessageArgs
                    {
                        Username = message.Username,
                        Text = message.Text
                    };

                    var result = await client.Mutate<object>("functions/sendMessage")
                        .WithArgs(messageArgs)
                        .ExecuteAsync();

                    Interlocked.Increment(ref successCount);
                    Console.WriteLine($"✓ [{message.Username}] {message.Text.Substring(0, Math.Min(50, message.Text.Length))}...");
                    return (Success: true, Message: message, Error: (string?)null);
                }
                catch (ConvexException ex)
                {
                    Interlocked.Increment(ref errorCount);
                    Console.Error.WriteLine($"✗ Failed to send message from {message.Username}: {ex.Message}");
                    return (Success: false, Message: message, Error: ex.Message);
                }
            });

            // Wait for all messages in this batch to complete
            await Task.WhenAll(tasks);

            // Small delay between batches to avoid overwhelming the server
            if (batchEnd < sampleMessages.Length)
            {
                await Task.Delay(100); // 100ms between batches
            }
        }

        Console.WriteLine($"\n=== Seeding Complete ===");
        Console.WriteLine($"Success: {successCount}");
        Console.WriteLine($"Errors: {errorCount}");
        Console.WriteLine($"Total: {sampleMessages.Length}");

        if (errorCount > 0)
        {
            Environment.Exit(1);
        }
    }
}

