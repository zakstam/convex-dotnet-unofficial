# Automation Scripts

Developer automation scripts for the Convex .NET Client project.

## Available Scripts

### Build scripts

#### `build.ps1` / `build.sh`

Build the full solution or a specific project.

**PowerShell:**

```powershell
# Build entire solution
.\scripts\build.ps1

# Build a specific project
.\scripts\build.ps1 -Target Convex.Client

# Release build with clean
.\scripts\build.ps1 -Configuration Release -Clean
```

**Bash:**

```bash
# Build entire solution
./scripts/build.sh

# Build a specific project
./scripts/build.sh Convex.Client Release

# Clean build
./scripts/build.sh all Debug clean
```

## Testing

There are no test wrapper scripts in this folder. Use `dotnet test` directly against the solution or the relevant test project.

```bash
# Run all tests in the solution
dotnet test convex-dotnet-client.sln

# Run all non-integration tests, matching CI
dotnet test convex-dotnet-client.sln --filter "Category!=Integration"

# Run integration tests only
dotnet test tests/Convex.Client.Tests.Integration/Convex.Client.Tests.Integration.csproj --filter "Category=Integration"

# Run architecture tests only
dotnet test tests/Convex.Client.ArchitectureTests/Convex.Client.ArchitectureTests.csproj --filter "TestCategory=Architecture"

# Run unit tests marked as edge cases
dotnet test tests/Convex.Client.Tests.Unit/Convex.Client.Tests.Unit.csproj --filter "Category=EdgeCase"
```

## Test category reference

| Category | Where it appears | Description |
| --- | --- | --- |
| `Integration` | xUnit integration tests | Tests that require configured Convex integration settings |
| `EdgeCase` | xUnit unit tests | Focused unit coverage for boundary and edge-case behavior |
| `Bug` | xUnit unit tests | Regression tests for known bugs |
| `Architecture` | MSTest architecture tests | Vertical-slice and dependency-boundary checks |
| `FeatureIsolation` | MSTest architecture tests | Feature-to-feature dependency checks |
| `InfrastructureIsolation` | MSTest architecture tests | Infrastructure dependency checks |
| `FeatureStructure` | MSTest architecture tests | Feature folder and structure checks |
| `NamingConventions` | MSTest architecture tests | Naming convention checks |
| `LegacyCode` | MSTest architecture tests | Legacy-code boundary checks |
| `Documentation` | MSTest architecture tests | Documentation coverage checks |

## Testing utilities

The `scripts/testing/` subdirectory contains standalone C# helpers for serialization and oracle debugging:

- `debug-oracle.cs`
- `test-serialization.cs`
- `test-serialization-compatibility.cs`

The Convex test backend used by integration tests lives under `tests/convex-test-backend/`.

## Tips

- **Quick local check:**

  ```bash
  dotnet build convex-dotnet-client.sln
  dotnet test convex-dotnet-client.sln --filter "Category!=Integration"
  ```

- **Integration test setup:**

  Keep local credentials in ignored development settings files. The tracked integration-test `appsettings.json` is only a placeholder/config baseline.
