# Development Container

This directory contains configuration for GitHub Codespaces and VS Code Dev Containers.

## What's Included

- **.NET 8.0 SDK** - Latest .NET development tools
- **Node.js 18+** - For TypeScript Test Oracle
- **Git** - Version control
- **VS Code Extensions**:
  - C# Dev Kit
  - EditorConfig support
  - GitHub Copilot
  - Pull Request integration

## Usage

### GitHub Codespaces

1. Click "Code" → "Create codespace on main"
2. Wait for container to build
3. Start developing!

### VS Code Dev Containers

1. Install "Dev Containers" extension in VS Code
2. Open project folder
3. Click "Reopen in Container" when prompted
4. Wait for container setup

## Post-Creation Setup

The container automatically:
- Restores .NET packages (`dotnet restore`)
- Installs npm dependencies for test oracle
- Forwards port 3000 for oracle service

## Running Tests

```bash
# Build project
./scripts/build.sh

# Run unit tests
./scripts/test.sh Unit

# Start oracle and run compatibility tests
./scripts/oracle-start.sh &
./scripts/test.sh Compatibility
```

## Customization

Edit `devcontainer.json` to:
- Add more VS Code extensions
- Change Node.js version
- Add additional tools
- Modify post-creation commands

## Learn More

- [VS Code Dev Containers](https://code.visualstudio.com/docs/devcontainers/containers)
- [GitHub Codespaces](https://github.com/features/codespaces)
- [Dev Container Specification](https://containers.dev/)
