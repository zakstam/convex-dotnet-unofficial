# Automation Scripts

Developer automation scripts for the Convex .NET Client project.

## Available Scripts

### Build Scripts

#### `build.ps1` / `build.sh`
Build the project or specific components.

**PowerShell:**
```powershell
# Build entire solution
.\scripts\build.ps1

# Build specific project
.\scripts\build.ps1 -Target Convex.Client

# Release build with clean
.\scripts\build.ps1 -Configuration Release -Clean
```

**Bash:**
```bash
# Build entire solution
./scripts/build.sh

# Build specific project
./scripts/build.sh Convex.Client Release

# Clean build
./scripts/build.sh all Debug clean
```

### Test Scripts

#### `test.ps1` / `test.sh`
Run tests with filtering and coverage options.

**PowerShell:**
```powershell
# Run all tests
.\scripts\test.ps1

# Run specific category
.\scripts\test.ps1 -Category Unit
.\scripts\test.ps1 -Category Integration
.\scripts\test.ps1 -Category Compatibility

# Run with coverage
.\scripts\test.ps1 -Category Unit -Coverage

# Custom filter
.\scripts\test.ps1 -Filter "FullyQualifiedName~Authentication"
```

**Bash:**
```bash
# Run all tests
./scripts/test.sh

# Run specific category
./scripts/test.sh Unit
./scripts/test.sh Integration

# Run with coverage
./scripts/test.sh Unit false true
```

### Benchmark Scripts

#### `benchmark.ps1` / `benchmark.sh`
Run performance benchmarks.

**PowerShell:**
```powershell
# Run all benchmarks
.\scripts\benchmark.ps1

# Filter benchmarks
.\scripts\benchmark.ps1 -Filter "*Query*"

# Export results
.\scripts\benchmark.ps1 -Export html,markdown
```

**Bash:**
```bash
# Run all benchmarks
./scripts/benchmark.sh

# Filter benchmarks
./scripts/benchmark.sh "*Query*"
```

### Oracle Scripts

#### `oracle-start.ps1` / `oracle-start.sh`
Start the TypeScript Test Oracle for compatibility testing.

**PowerShell:**
```powershell
# Start oracle (default port 3000)
.\scripts\oracle-start.ps1

# Start on custom port
.\scripts\oracle-start.ps1 -Port 3001

# Install dependencies and start
.\scripts\oracle-start.ps1 -Install
```

**Bash:**
```bash
# Start oracle (default port 3000)
./scripts/oracle-start.sh

# Start on custom port
./scripts/oracle-start.sh 3001

# Install dependencies first
./scripts/oracle-start.sh 3000 true
```

## Test Category Reference

| Category | Description |
|----------|-------------|
| `Unit` | Fast, isolated unit tests |
| `Integration` | Integration tests with dependencies |
| `Acceptance` | End-to-end acceptance tests |
| `Performance` | Performance and load tests |
| `Compatibility` | TypeScript protocol compatibility tests |
| `LiveIntegration` | Tests against live Convex backend |

## Testing Utilities

The `testing/` subdirectory contains additional test utilities:
- `debug-oracle.cs` - Oracle debugging helpers
- `test-serialization.cs` - Serialization testing utilities
- `test-serialization-compatibility.cs` - Compatibility test helpers

## Tips

- **Quick testing workflow:**
  ```powershell
  .\scripts\build.ps1
  .\scripts\test.ps1 -Category Unit
  ```

- **Pre-commit checks:**
  ```powershell
  .\scripts\build.ps1 -Configuration Release
  .\scripts\test.ps1 -Category Unit
  .\scripts\test.ps1 -Category Integration
  ```

- **Compatibility testing:**
  ```powershell
  # Terminal 1: Start oracle
  .\scripts\oracle-start.ps1

  # Terminal 2: Run compatibility tests
  .\scripts\test.ps1 -Category Compatibility
  ```

- **Performance testing:**
  ```powershell
  .\scripts\benchmark.ps1 -Export html
  # Open benchmarks/Convex.Client.Benchmarks/BenchmarkDotNet.Artifacts/results/index.html
  ```
