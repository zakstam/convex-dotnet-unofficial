# Releasing Convex .NET SDK

This project uses [release-please](https://github.com/googleapis/release-please) for fully automated releases.

## Quick Start

### 1. Write Code with Conventional Commits

```bash
# New feature (minor version bump)
git commit -m "feat: add query batching support"

# Bug fix (patch version bump)
git commit -m "fix: handle null values in serialization"

# Breaking change (major version bump)
git commit -m "feat!: redesign client API

BREAKING CHANGE: Client initialization has changed"
```

### 2. Merge to Main

When you merge to `main`, GitHub Actions automatically:
- Runs release-please to analyze commits
- Determines the version bump
- Creates or updates a **Release PR** with:
  - Auto-generated CHANGELOG
  - Version bump ready to apply
  - List of changes by category

**Note:** Code is validated during PR review. Workflows run on self-hosted runners to avoid GitHub Actions billing costs.

### 3. Merge the Release PR

Go to [Pull Requests](../../pulls) and find the release-please PR.

Review:
- ✅ Version number is correct
- ✅ CHANGELOG entries look good
- ✅ Release notes are accurate

Then **merge the PR**. This will:
- ✅ Create a GitHub release automatically
- ✅ Tag the release with the version number

### 4. Automatic Publishing (GitHub Actions)

When the release is created, GitHub Actions automatically:
- ✅ Triggers the CI/CD workflow on self-hosted runner
- ✅ Builds and tests the release
- ✅ Packages are created with correct version
- ✅ Published to NuGet.org automatically
- ✅ GitHub release notes updated with NuGet badge

**That's it!** No manual version bumping, no manual CHANGELOG updates, no manual tagging required.

**Build Status:** Check [Actions](../../actions) for build progress and logs.

## Benefits of Release-Please Workflow

✅ **Review before publish** - Catch issues before NuGet deployment via Release PR
✅ **Clean git history** - Single merge commit per release
✅ **Contributor-proof** - External PRs can't interfere with releases
✅ **Easy recovery** - Close PR and re-run if needed
✅ **Automatic changelog** - Generated from conventional commits
✅ **Cost-effective** - Runs on self-hosted runners (no billing)

## Conventional Commit Types

| Type | Version Bump | Description |
|------|--------------|-------------|
| `feat:` | Minor (0.x.0) | New feature |
| `fix:` | Patch (0.0.x) | Bug fix |
| `perf:` | Patch (0.0.x) | Performance improvement |
| `docs:` | None | Documentation only |
| `refactor:` | None | Code refactoring |
| `test:` | None | Test updates |
| `chore:` | None | Maintenance |
| `BREAKING CHANGE:` | Major (x.0.0) | Breaking change |

## Example Commit Messages

✅ **Good:**
```
feat: add support for server-sent events
fix: prevent memory leak in subscription cleanup
perf: optimize serialization allocations
docs: update authentication guide
```

❌ **Bad:**
```
updated code
fix bug
WIP
misc changes
```

## Setting Up Self-Hosted Runner

To avoid GitHub Actions billing, this project uses self-hosted runners. Here's how to set one up:

### Prerequisites
- A machine with .NET 9.0 SDK installed
- Windows, Linux, or macOS
- Internet connection
- GitHub repository admin access

### Quick Setup

1. **Go to Repository Settings**
   - Navigate to Settings → Actions → Runners
   - Click "New self-hosted runner"

2. **Download and Configure**
   - Follow GitHub's instructions for your OS
   - Use default settings when prompted
   - Runner will automatically register

3. **Install .NET 9.0 SDK**
   ```bash
   # Download from: https://dotnet.microsoft.com/download/dotnet/9.0
   dotnet --version  # Verify installation
   ```

4. **Start the Runner**
   ```bash
   # Linux/macOS
   ./run.sh

   # Windows
   .\run.cmd

   # Or install as a service (recommended)
   # See GitHub's documentation for service installation
   ```

5. **Verify Setup**
   - Check that runner appears as "Idle" in Settings → Actions → Runners
   - Trigger a workflow to test

### Running as a Service (Recommended)

**Linux (systemd):**
```bash
sudo ./svc.sh install
sudo ./svc.sh start
sudo ./svc.sh status
```

**Windows (PowerShell as Admin):**
```powershell
.\svc.cmd install
.\svc.cmd start
.\svc.cmd status
```

**macOS:**
```bash
./svc.sh install
./svc.sh start
./svc.sh status
```

## Full Documentation

For complete details, troubleshooting, and advanced scenarios:
- **[GitHub Actions Documentation](https://docs.github.com/en/actions/hosting-your-own-runners)**
- **[Self-Hosted Runner Security](https://docs.github.com/en/actions/hosting-your-own-runners/managing-self-hosted-runners/about-self-hosted-runners#self-hosted-runner-security)**

## Learn More

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [release-please Documentation](https://github.com/googleapis/release-please)
- [GitHub Actions Workflows](https://docs.github.com/en/actions/using-workflows)
