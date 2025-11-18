# Release Process Documentation

This document describes the release process for the Convex .NET SDK using automated release management with conventional commits.

## Important: NuGet.org Only Publishing

**All releases are published exclusively to NuGet.org.** Private package repositories (such as GitHub Packages) are not used for official releases. The release workflow includes validation to ensure packages are only published to NuGet.org.

## Quick Start: Releasing with Release-Please

The Convex .NET SDK uses [release-please](https://github.com/googleapis/release-please) for automated release management. Releases are triggered automatically based on your commit messages.

### The Simple Workflow

1. **Write code with conventional commits** (see format below)
2. **Merge to main** - release-please opens a "Release PR" automatically
3. **Review the Release PR** - check version bump and generated CHANGELOG
4. **Merge the Release PR** - packages are automatically published to NuGet.org
5. **Done!** ✅

That's it! No manual version bumping, no manual CHANGELOG updates, no manual tagging.

## Conventional Commits

Release-please determines version bumps and generates changelogs from your commit messages. Follow the [Conventional Commits](https://www.conventionalcommits.org/) format:

### Commit Message Format

```
<type>: <description>

[optional body]

[optional footer(s)]
```

### Commit Types

| Type | Description | Version Bump | Example |
|------|-------------|--------------|---------|
| `feat` | New feature | Minor (0.x.0) | `feat: add query batching support` |
| `fix` | Bug fix | Patch (0.0.x) | `fix: handle null values in serialization` |
| `perf` | Performance improvement | Patch (0.0.x) | `perf: optimize connection pooling` |
| `docs` | Documentation only | None | `docs: update getting started guide` |
| `refactor` | Code refactoring | None | `refactor: simplify client initialization` |
| `test` | Test updates | None | `test: add integration tests for mutations` |
| `chore` | Maintenance tasks | None | `chore: update dependencies` |
| `ci` | CI/CD changes | None | `ci: add caching to workflows` |
| `build` | Build system changes | None | `build: update to .NET 9.0` |

### Breaking Changes

To trigger a **major version bump** (x.0.0), include `BREAKING CHANGE:` in the commit body or footer:

```
feat: redesign client API

BREAKING CHANGE: The client initialization API has changed.
Old: new ConvexClient(url)
New: ConvexClient.Create(url)
```

Or use the `!` shorthand:

```
feat!: redesign client API

The client initialization API has changed.
```

### Commit Message Examples

**Good commits:**

```bash
feat: add support for server-side streaming
fix: prevent memory leak in subscription cleanup
perf: reduce allocation in query serialization
docs: add examples for authentication flow
refactor: extract connection management to separate class
test: add E2E tests for reconnection scenarios
```

**Bad commits:**

```bash
updated code           # ❌ No type, unclear
fix bug                # ❌ Too vague
WIP                    # ❌ Not descriptive
misc changes           # ❌ Not specific
```

### Commit Message Validation

The repository includes automatic validation:

- **PR Validation**: Commit messages are checked when you open a PR
- **Local Hook (Optional)**: You can install commitlint locally for instant feedback

**Install local validation (optional):**

```bash
npm install --save-dev @commitlint/{cli,config-conventional}
npm install --save-dev husky
npx husky install
npx husky add .husky/commit-msg 'npx --no -- commitlint --edit "$1"'
```

## How Release-Please Works

### Automatic Release PR

When you merge commits to `main`, release-please:

1. **Analyzes commits** since the last release
2. **Determines version bump** based on commit types:
   - `feat` → minor version bump
   - `fix` / `perf` → patch version bump
   - `BREAKING CHANGE` → major version bump
3. **Generates CHANGELOG** from commit messages
4. **Opens/updates Release PR** with version bump and CHANGELOG

### Release PR Contents

The Release PR includes:

- **Version bump** in `msbuild/Directory.Build.props`
- **CHANGELOG.md update** with categorized changes
- **Commit history** since last release

**Example Release PR:**

```markdown
## 🚀 Release v0.3.0

### ✨ Features
- feat: add query batching support (#42)
- feat: implement connection retry logic (#45)

### 🐛 Bug Fixes
- fix: handle null values in serialization (#43)
- fix: prevent memory leak in subscription cleanup (#47)

### 📚 Documentation
- docs: add examples for authentication flow (#44)
```

### Publishing

When you merge the Release PR:

1. **Tag is created** (e.g., `v0.3.0`)
2. **Packages are built** for all platforms
3. **Tests run** to validate release
4. **Packages publish** to NuGet.org
5. **GitHub Release** is created with release notes

## Version Management

### Version Source of Truth

- **Git Tags**: Created automatically by release-please
- **Source Files**: `msbuild/Directory.Build.props` is updated automatically by release-please
- **Manual Override**: Not recommended (breaks automation)

### Version Format

We use [Semantic Versioning](https://semver.org/) (SemVer):

- Format: `MAJOR.MINOR.PATCH` (e.g., `1.2.3`)
- Git tags: Prefixed with `v` (e.g., `v1.2.3`)

**Version bumps:**
- **Major (x.0.0)**: Breaking changes (BREAKING CHANGE in commit)
- **Minor (0.x.0)**: New features (feat commits)
- **Patch (0.0.x)**: Bug fixes (fix, perf commits)

## Release Workflows

### Primary: Release-Please Workflow

**File:** `.github/workflows/release-please.yml`

**Trigger:** Push to `main` branch

**Jobs:**

1. **Release-Please**
   - Analyzes commits since last release
   - Creates/updates Release PR with version bump and CHANGELOG
   - When Release PR is merged:
     - Creates git tag
     - Triggers build and publish

2. **Build**
   - Builds all packages with the new version
   - Runs tests
   - Creates NuGet packages (.nupkg and .snupkg)
   - Uploads artifacts

3. **Publish**
   - Downloads package artifacts
   - Publishes to NuGet.org only
   - Updates GitHub Release with package info


## Configuration Files

### Release-Please Config

**File:** `.github/release-please-config.json`

Configures release-please behavior:

- Release type: `simple`
- Changelog sections: Features, fixes, performance, docs, etc.
- Extra files to update: `msbuild/Directory.Build.props`
- PR title pattern: `chore: release v${version}`

### Release-Please Manifest

**File:** `.github/.release-please-manifest.json`

Tracks the current released version. Updated automatically by release-please.

### Commitlint Config

**File:** `.commitlintrc.json`

Validates commit message format against conventional commits standard.

## CI/CD Configuration

### Required Secrets

**GitHub Secrets:**

- `NUGET_API_KEY`: NuGet.org API key for publishing packages
  - Get from: https://www.nuget.org/account/apikeys
  - Required scope: Push new packages and package versions

**GitHub Token:**

- Automatically provided by GitHub Actions
- Used for creating releases and tags

### Workflow Permissions

Release workflows require:

- `contents: write` - Create releases and tags
- `pull-requests: write` - Create and update Release PRs
- `id-token: write` - Publish to NuGet.org

## Troubleshooting

### Release PR Not Created

**Problem:** No Release PR appears after merging to main

**Solutions:**

1. **Check commits use conventional format**
   - Must have `feat:`, `fix:`, etc. prefix
   - Check commit messages: `git log --oneline`

2. **Verify no releasable commits**
   - Only `feat`, `fix`, `perf`, and `BREAKING CHANGE` trigger releases
   - `docs`, `test`, `chore` commits don't trigger releases

3. **Check workflow logs**
   - Go to Actions → "Release Please" → Check latest run
   - Look for errors or warnings

4. **Release PR might already exist**
   - Check open PRs for one titled "chore: release v..."
   - release-please updates existing PR if found

### Wrong Version Bump

**Problem:** Release PR shows wrong version number

**Solutions:**

1. **Review commit types**
   - `feat` → minor bump (0.x.0)
   - `fix`/`perf` → patch bump (0.0.x)
   - `BREAKING CHANGE` → major bump (x.0.0)

2. **Check commit history**
   - Review commits included in the release
   - Ensure commits are properly formatted

3. **Manual override (not recommended)**
   - Edit the Release PR to change version
   - Update both `msbuild/Directory.Build.props` and `CHANGELOG.md`
   - **Note:** This breaks automatic versioning

### CHANGELOG Missing Commits

**Problem:** CHANGELOG.md doesn't include all changes

**Solutions:**

1. **Check commit format**
   - Only commits with proper conventional format are included
   - `docs`, `test`, `chore`, etc. are hidden by default

2. **Review changelog sections**
   - Configured in `.github/release-please-config.json`
   - Some types are hidden: style, test, build, ci, chore

3. **Manually edit if needed**
   - You can edit the Release PR to add missing items
   - Follow the existing CHANGELOG format

### Packages Not Publishing

**Problem:** Release created but packages not on NuGet.org

**Solutions:**

1. **Check workflow logs**
   - Go to Actions → "Release Please" → Find your release run
   - Check the "nuget-publish" job logs

2. **Verify NUGET_API_KEY secret**
   - Settings → Secrets → Actions
   - Ensure `NUGET_API_KEY` is set and valid

3. **Check API key permissions**
   - NuGet.org → Account → API Keys
   - Key must have "Push" permission

4. **Package might already exist**
   - Workflow uses `--skip-duplicate`
   - Check NuGet.org to confirm package exists

### Build or Test Failures

**Problem:** Release process fails during build/test

**Solutions:**

1. **Fix issues before merging**
   - CI should pass before merging to main
   - Release-please creates PR, but publish happens on merge

2. **Review test failures**
   - Check workflow logs for specific failures
   - Fix issues and push to Release PR branch

3. **Emergency fix**
   - Close the Release PR
   - Fix issues in a separate PR
   - Release PR will be recreated/updated automatically

### Commitlint Failures

**Problem:** PR checks fail with commit message validation errors

**Solutions:**

1. **Fix commit messages**
   - Use interactive rebase: `git rebase -i HEAD~3`
   - Update commit messages to conventional format
   - Force push: `git push --force-with-lease`

2. **Squash and fix**
   - Squash commits with bad messages
   - Write a single good commit message

3. **Bypass validation (not recommended)**
   - You can merge despite validation failure
   - But Release PR might not be created

## Best Practices

### Writing Commits

1. **Be descriptive in commit messages**
   - ✅ `feat: add support for file upload in mutations`
   - ❌ `feat: add feature`

2. **One logical change per commit**
   - Makes CHANGELOG more readable
   - Easier to revert if needed

3. **Use imperative mood**
   - ✅ `fix: prevent memory leak`
   - ❌ `fix: prevented memory leak`

4. **Reference issues when relevant**
   - `feat: add query batching support (#42)`
   - `fix: handle null values (#43)`

### Release Frequency

- **Major releases**: When breaking changes are necessary (rare)
- **Minor releases**: Every few weeks or when features are ready
- **Patch releases**: As needed for bug fixes

### Pre-Merge Checklist (for Release PR)

- [ ] Review version bump is correct
- [ ] Review CHANGELOG.md entries
- [ ] All CI checks pass
- [ ] No pending critical fixes needed
- [ ] Documentation is up to date

### Post-Release Checklist

- [ ] Verify packages published to NuGet.org only
- [ ] Check GitHub release created with notes
- [ ] Test package installation: `dotnet add package Convex.Client --version <version>`
- [ ] Verify packages are accessible on NuGet.org
- [ ] Announce release (Discord, Twitter, etc.)

## Migration from Legacy Process

If you were using the old manual release process:

### What Changed

1. **No manual version bumping** - release-please handles it
2. **No manual CHANGELOG updates** - generated from commits
3. **No manual tagging** - tags created automatically
4. **Conventional commits required** - for automatic versioning

### Migration Steps

1. **Start using conventional commits**
   - Install commitlint (optional but recommended)
   - Follow commit message format

2. **Let release-please create first Release PR**
   - Merge your changes to main
   - Wait for Release PR to appear
   - Review and merge

3. **Done!**
   - You're now fully on the automated release system

## Advanced: Manual Version Override

**⚠️ Not recommended - breaks automation**

If you absolutely must override the version:

1. Edit the Release PR
2. Update version in `msbuild/Directory.Build.props`
3. Update version in `CHANGELOG.md`
4. Update version in `.github/.release-please-manifest.json`
5. Commit and merge

**Note:** This can confuse release-please for future releases.

## Related Documentation

- [Conventional Commits Specification](https://www.conventionalcommits.org/)
- [release-please Documentation](https://github.com/googleapis/release-please)
- [Getting Started Guide](getting-started.md)
- [API Reference](api-reference.md)

## Support

If you encounter issues with the release process:

1. Check this documentation
2. Review workflow logs in GitHub Actions
3. Check [GitHub Issues](https://github.com/zakstam/convex-dotnet/issues)
4. Ask in [Discord Community](https://convex.dev/community)
