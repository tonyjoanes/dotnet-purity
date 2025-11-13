# GitHub OAuth Setup Guide

## Quick Summary

**What it's for**: Allows users to sign in with GitHub instead of separate credentials. The frontend gets a token from GitHub and sends it to the API to authorize requests.

**Current Status**: ⚠️ **There's a compatibility issue** - GitHub OAuth Apps return access tokens, but the API expects JWT tokens. See section 4 for solutions.

## What is GitHub OAuth for?

GitHub OAuth allows users to sign in to Purity using their GitHub account. This provides:
- **User Authentication**: Users authenticate with GitHub instead of managing separate credentials
- **API Authorization**: The frontend receives a token from GitHub that it sends to the API
- **Secure API Access**: The API validates the GitHub-issued token before allowing scan requests

**Note**: The current implementation has a token format mismatch that needs to be addressed (see section 4).

## Step-by-Step Setup

### 1. Register a GitHub OAuth App

1. Go to GitHub → Settings → Developer settings → OAuth Apps
   - Direct link: https://github.com/settings/developers
2. Click **"New OAuth App"**
3. Fill in the form:
   - **Application name**: `Purity` (or your preferred name)
   - **Homepage URL**: 
     - Development: `https://localhost:5000` (or your frontend URL)
     - Production: Your production frontend URL
   - **Authorization callback URL**: 
     - Development: `https://localhost:5000/authentication/login-callback`
     - Production: `https://your-domain.com/authentication/login-callback`
4. Click **"Register application"**
5. **Important**: Copy the **Client ID** (you'll need this)
6. Click **"Generate a new client secret"** and copy the **Client Secret** (you'll need this too)

### 2. Configure Frontend (Blazor WASM)

Edit `src/Purity.Frontend/wwwroot/appsettings.json`:

```json
{
  "Api": {
    "BaseAddress": "https://localhost:5001"
  },
  "Authentication": {
    "GitHub": {
      "Authority": "https://github.com/login/oauth/authorize",
      "ClientId": "YOUR_CLIENT_ID_HERE",
      "ResponseType": "code",
      "Scopes": [
        "read:user"
      ]
    }
  }
}
```

**Steps**:
1. Replace `YOUR_CLIENT_ID_HERE` with the Client ID from step 1
2. For production, update `Api:BaseAddress` to your production API URL
3. The `Authority` should point to GitHub's OAuth authorization endpoint

**⚠️ Important Note**: The current `Program.cs` uses `AddOidcAuthentication`, which expects OIDC endpoints. GitHub OAuth Apps don't provide OIDC discovery endpoints, so this configuration may not work directly. You may need to:

1. **Option 1**: Implement custom OAuth flow in the frontend (more work, but proper)
2. **Option 2**: Use a different authentication library that supports OAuth 2.0 directly
3. **Option 3**: For development, skip authentication in the frontend and test the API directly

The frontend authentication setup needs to be adjusted to work with GitHub OAuth Apps properly.

### 3. Configure API Backend

Edit `src/Purity.Api/appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Cors": {
    "AllowedOrigins": [
      "https://localhost:5000",
      "http://localhost:5000"
    ]
  },
  "Authentication": {
    "GitHub": {
      "Authority": "",
      "Audience": ""
    }
  },
  "AllowedHosts": "*"
}
```

**Important Notes**:
- `Cors:AllowedOrigins` should include your frontend URL(s) (both HTTP and HTTPS for development)
- **Token Validation Issue**: The API is currently configured for JWT Bearer tokens, but GitHub OAuth Apps return access tokens (not JWTs). For development, you can:
  - Temporarily remove `.RequireAuthorization()` from endpoints to test without auth
  - Or implement a custom authentication handler that validates GitHub access tokens
- For production, you'll need to implement proper GitHub token validation

### 4. Important: GitHub OAuth Token Validation

**The Challenge**: 
- GitHub OAuth Apps return **access tokens** (not JWT tokens)
- The API is configured to validate **JWT Bearer tokens**
- These are incompatible without additional work

**Solutions**:

**Option A: Development - Temporarily Disable Auth** (Quickest for testing)
- Remove `.RequireAuthorization()` from endpoints in `src/Purity.Api/Endpoints/ScanEndpoints.cs`
- This allows testing the analyzer functionality without authentication

**Option B: Custom Token Validator** (Recommended for production)
- Create a custom authentication handler that:
  1. Extracts the GitHub access token from the Authorization header
  2. Validates it by calling `https://api.github.com/user` with the token
  3. If successful, creates a ClaimsPrincipal for the user
- This requires implementing `AuthenticationHandler<AuthenticationSchemeOptions>`

**Option C: Use GitHub Apps with OIDC** (More complex but proper)
- Register a GitHub App (not OAuth App)
- GitHub Apps support OIDC and issue proper JWT tokens
- This requires different setup but works with the current JWT configuration

**For Now**: Start with Option A to get the system working, then implement Option B for production.

### 5. Development vs Production

**Development**:
- Frontend: `https://localhost:5000` (or configured port)
- API: `https://localhost:5001` (or configured port)
- CORS: Allow both localhost URLs

**Production**:
- Update all URLs to your production domains
- Ensure HTTPS is enabled
- Update GitHub OAuth app callback URLs to production

## Testing the Setup

1. Start the API: `dotnet run --project src/Purity.Api`
2. Start the Frontend: `dotnet run --project src/Purity.Frontend`
3. Navigate to the frontend URL
4. Click "Sign in" - you should be redirected to GitHub
5. Authorize the app
6. You should be redirected back and authenticated

## Troubleshooting

- **CORS errors**: Ensure `AllowedOrigins` in API config includes your frontend URL
- **Authentication fails**: Verify Client ID is correct in frontend config
- **Token validation fails**: This is expected - see section 4 for the token format mismatch issue
- **Callback URL mismatch**: Ensure the callback URL in GitHub OAuth app matches exactly (including protocol and port)
- **OIDC errors in frontend**: GitHub OAuth Apps don't support OIDC - the frontend auth needs to be refactored

## Quick Checklist

- [ ] Register GitHub OAuth App at https://github.com/settings/developers
- [ ] Copy Client ID and Client Secret
- [ ] Update `src/Purity.Frontend/wwwroot/appsettings.json` with Client ID
- [ ] Update `src/Purity.Api/appsettings.json` CORS origins (already done)
- [ ] **Decide on authentication approach** (see section 4):
  - [ ] Option A: Disable auth for development testing
  - [ ] Option B: Implement custom GitHub token validator
  - [ ] Option C: Switch to GitHub Apps with OIDC
- [ ] Test the flow end-to-end

