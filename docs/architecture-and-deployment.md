# Architecture and Deployment Guide

## How It Currently Works (Local Development)

### Current Flow

```
┌─────────────┐         ┌─────────────┐         ┌─────────────┐
│  Frontend   │         │     API     │         │   Engine    │
│ (Blazor     │────────▶│  (Minimal   │────────▶│  (Roslyn    │
│  WASM)      │  HTTP   │   API)      │         │  Analyzer)  │
└─────────────┘         └─────────────┘         └─────────────┘
     │                        │                        │
     │                        │                        │
     │  User enters:          │                        │
     │  "C:\dev\repo"         │                        │
     │                        │                        │
     │                        │  Reads from:           │
     │                        │  C:\dev\repo\*.cs      │
     │                        │  (Server's file       │
     │                        │   system)              │
     │                        │                        │
     │                        │  Returns:              │
     │                        │  Diagnostics[]         │
     │◀───────────────────────│                        │
     │                        │                        │
```

### Step-by-Step Process

1. **Frontend** (`Home.razor`):
   - User enters a repository path (e.g., `C:\dev\dotnet-purity`)
   - Sends POST to `/scan` with `{ repositoryPath: "C:\dev\dotnet-purity" }`

2. **API** (`ScanEndpoints.cs`):
   - Receives the request
   - Converts DTO to domain model (`ScanRequest`)
   - Calls `AnalyzerRunner.RunAsync()`

3. **Engine** (`AnalyzerRunner.cs`):
   - Checks if `Directory.Exists(request.RepositoryPath)` ✅ **This is the problem!**
   - Uses `Directory.EnumerateFiles()` to find all `*.cs` files
   - Reads files from the **server's local file system**
   - Parses them into syntax trees
   - Runs Roslyn analyzers
   - Returns diagnostics

### The Problem

**Current approach assumes:**
- The API server has direct file system access to the repository
- The path exists on the server's machine
- The server can read files from that location

**This breaks when deployed because:**
- ❌ The API server won't have access to the user's local `C:\dev\repo` path
- ❌ Different servers = different file systems
- ❌ Security: Servers shouldn't access arbitrary file paths
- ❌ Cloud deployments don't have persistent local file systems

## Solutions for Deployment

### Option 1: GitHub Integration (Recommended)

**How it works:**
1. User authenticates with GitHub OAuth
2. User selects a repository from their GitHub account
3. API clones the repository to a temporary directory
4. Analyzers run on the cloned code
5. Clean up temporary files

**Implementation needed:**
```csharp
// New service interface
public interface IRepositoryProvider
{
    Task<Either<RepositoryError, RepositoryContent>> GetRepositoryAsync(
        string owner, 
        string repo, 
        string? branch = null);
}

// Implementation
public class GitHubRepositoryProvider : IRepositoryProvider
{
    public async Task<Either<RepositoryError, RepositoryContent>> GetRepositoryAsync(...)
    {
        // 1. Clone repo to temp directory using LibGit2Sharp or Octokit
        // 2. Return file contents
        // 3. Clean up on dispose
    }
}
```

**Pros:**
- ✅ Works in cloud deployments
- ✅ Secure (uses GitHub API)
- ✅ No file system dependencies
- ✅ Supports private repos (with proper auth)

**Cons:**
- ⚠️ Requires GitHub OAuth setup
- ⚠️ Need to handle rate limits
- ⚠️ Temporary storage needed

### Option 2: File Upload

**How it works:**
1. User uploads a ZIP file of their repository
2. API extracts ZIP to temporary directory
3. Analyzers run on extracted files
4. Clean up temporary files

**Implementation needed:**
```csharp
// New endpoint
app.MapPost("/scan/upload", async (IFormFile file) =>
{
    var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    // Extract ZIP
    // Run analyzers
    // Clean up
});
```

**Pros:**
- ✅ Works with any repository (not just GitHub)
- ✅ No external dependencies
- ✅ Simple to implement

**Cons:**
- ⚠️ File size limits
- ⚠️ User must manually create ZIP
- ⚠️ Temporary storage needed

### Option 3: Git Clone via URL

**How it works:**
1. User provides a Git repository URL (GitHub, GitLab, etc.)
2. API clones the repository using `LibGit2Sharp` or `git` command
3. Analyzers run on cloned code
4. Clean up temporary directory

**Implementation needed:**
```csharp
public class GitRepositoryProvider : IRepositoryProvider
{
    public async Task<Either<RepositoryError, RepositoryContent>> GetRepositoryAsync(
        string gitUrl)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // Clone repo
        // Return file contents
    }
}
```

**Pros:**
- ✅ Works with any Git repository
- ✅ Supports public repos easily
- ✅ No authentication needed for public repos

**Cons:**
- ⚠️ Private repos need credentials
- ⚠️ Temporary storage needed
- ⚠️ Network dependency

### Option 4: Source Code as Payload

**How it works:**
1. Frontend reads files from user's local file system (using File API)
2. Sends file contents in the request payload
3. API creates temporary files or works in-memory
4. Analyzers run on provided code

**Implementation needed:**
```csharp
public record ScanRequestDto
{
    public Dictionary<string, string> Files { get; init; } = new();
    // Key = file path, Value = file content
}
```

**Pros:**
- ✅ No server-side file system access needed
- ✅ Works entirely in-memory
- ✅ Simple for small repositories

**Cons:**
- ⚠️ Request size limits (can't send huge repos)
- ⚠️ Browser memory limits
- ⚠️ User must select files manually

## Recommended Architecture Changes

### 1. Abstract Repository Access

```csharp
// Domain/IRepositorySource.cs
public interface IRepositorySource
{
    Task<Either<SourceError, SourceContent>> GetSourceAsync(
        SourceRequest request, 
        CancellationToken cancellationToken);
}

public record SourceRequest
{
    public SourceType Type { get; init; }
    public string? LocalPath { get; init; }  // For development
    public string? GitUrl { get; init; }
    public string? GitHubOwner { get; init; }
    public string? GitHubRepo { get; init; }
    public Dictionary<string, string>? Files { get; init; }  // For upload
}

public enum SourceType
{
    LocalPath,      // Development only
    GitHub,
    GitUrl,
    FileUpload,
    InMemory
}
```

### 2. Update AnalyzerRunner

```csharp
public class AnalyzerRunner : IAnalyzerRunner
{
    private readonly IAnalyzerCatalog _catalog;
    private readonly IRepositorySource _repositorySource;

    public AnalyzerRunner(
        IAnalyzerCatalog catalog, 
        IRepositorySource repositorySource)
    {
        _catalog = catalog;
        _repositorySource = repositorySource;
    }

    public async Task<Either<AnalyzerFailure, AnalyzerReport>> RunAsync(
        ScanRequest request, 
        CancellationToken cancellationToken)
    {
        // Get source from repository provider (not file system)
        var sourceResult = await _repositorySource.GetSourceAsync(
            request.ToSourceRequest(), 
            cancellationToken);

        return await sourceResult.MatchAsync(
            Right: async source => await AnalyzeSourceAsync(source, cancellationToken),
            Left: error => Task.FromResult<Either<AnalyzerFailure, AnalyzerReport>>(
                Left<AnalyzerFailure, AnalyzerReport>(
                    new AnalyzerFailure("SOURCE-ERROR", error.Message))));
    }

    private async Task<Either<AnalyzerFailure, AnalyzerReport>> AnalyzeSourceAsync(
        SourceContent source, 
        CancellationToken cancellationToken)
    {
        // Parse source files (from memory or temp directory)
        // Run analyzers
        // Return results
    }
}
```

### 3. Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "RepositorySource": {
    "Type": "LocalPath",
    "AllowedPaths": ["C:\\dev"]
  }
}

// appsettings.Production.json
{
  "RepositorySource": {
    "Type": "GitHub",
    "TempDirectory": "/tmp/purity-scans",
    "CleanupAfterMinutes": 30
  }
}
```

## Migration Path

1. **Phase 1**: Keep current local path approach for development
2. **Phase 2**: Add `IRepositorySource` abstraction
3. **Phase 3**: Implement GitHub provider
4. **Phase 4**: Add file upload as alternative
5. **Phase 5**: Remove local path support in production

## Security Considerations

- ✅ Never accept arbitrary file paths in production
- ✅ Validate and sanitize all inputs
- ✅ Use temporary directories with automatic cleanup
- ✅ Set file size limits for uploads
- ✅ Rate limit repository access
- ✅ Authenticate GitHub requests properly
- ✅ Clean up temporary files after analysis

## Summary

**Current State:**
- ✅ Works great for local development
- ❌ Won't work when deployed to cloud

**Needed Changes:**
1. Abstract repository access behind an interface
2. Implement GitHub integration (recommended)
3. Add file upload as fallback
4. Remove direct file system access in production
5. Add proper authentication and authorization

The architecture is already modular enough to support these changes without major refactoring!

