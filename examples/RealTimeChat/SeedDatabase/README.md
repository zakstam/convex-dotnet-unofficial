# Database Seeder

A simple console application to seed the Convex chat database with sample messages.

## Usage

### Prerequisites

1. Ensure your Convex deployment URL is configured in `examples/RealTimeChat/appsettings.json`:
   ```json
   {
     "Convex": {
       "DeploymentUrl": "https://your-deployment.convex.cloud"
     }
   }
   ```

   Or set the `CONVEX_DEPLOYMENT_URL` environment variable.

### Running the Seeder

From the solution root:

```bash
dotnet run --project examples/RealTimeChat/SeedDatabase/SeedDatabase.csproj
```

Or from the SeedDatabase directory:

```bash
cd examples/RealTimeChat/SeedDatabase
dotnet run
```

### What It Does

The seeder will:
- Connect to your Convex deployment
- Create 20 sample messages from different users (Alice, Bob, Charlie, Diana, Eve)
- Messages demonstrate various features like:
  - Emojis
  - Markdown formatting
  - User mentions
  - General chat conversation

### Customization

You can modify `Program.cs` to:
- Change the number of messages
- Add different usernames
- Include replies (using `parentMessageId`)
- Add attachments
- Adjust the delay between messages

## Example Output

```
=== Convex Chat Database Seeder ===

Connecting to: https://handsome-perch-90.convex.cloud

Seeding 20 messages...

✓ [Alice] Hey everyone! 👋 Welcome to the chat!...
✓ [Bob] Thanks Alice! Excited to be here....
✓ [Charlie] This is a great chat app! Really enjoying the real-time...
...

=== Seeding Complete ===
Success: 20
Errors: 0
Total: 20
```

