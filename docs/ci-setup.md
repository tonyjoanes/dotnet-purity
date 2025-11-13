# CI/CD Setup Guide

This document describes the CI/CD setup for the Purity project, including GitHub Actions, Snyk security scanning, and Dependabot.

## GitHub Actions CI

The CI pipeline is defined in `.github/workflows/ci.yml` and includes:

### Build Job
- Restores NuGet dependencies
- Builds the entire solution in Release configuration
- Runs tests (if any test projects exist)

### Snyk Security Scan Job
- Scans all .NET dependencies for known vulnerabilities
- Runs on pull requests and pushes to main/develop branches
- Fails the build if high-severity vulnerabilities are found

## Setting Up Snyk

To enable Snyk security scanning in CI:

1. **Get your Snyk API token:**
   - Log in to your Snyk account at https://app.snyk.io
   - Go to Settings → General → Account → API Token
   - Copy your API token

2. **Add the token to GitHub Secrets:**
   - Go to your GitHub repository
   - Navigate to Settings → Secrets and variables → Actions
   - Click "New repository secret"
   - Name: `SNYK_TOKEN`
   - Value: Paste your Snyk API token
   - Click "Add secret"

3. **Verify the integration:**
   - Create a pull request or push to main/develop branch
   - Check the "Snyk Security Scan" job in the Actions tab
   - The scan will run automatically and report any vulnerabilities

## Dependabot

Dependabot is configured in `.github/dependabot.yml` to automatically:

- Check for dependency updates weekly (Mondays at 9:00 AM)
- Create pull requests for available updates
- Group related dependencies (Microsoft.AspNetCore.*, Microsoft.CodeAnalysis.*, etc.)
- Limit to 10 open pull requests at a time

### Dependabot Groups

Dependencies are automatically grouped into:
- **microsoft-aspnetcore**: All Microsoft.AspNetCore.* packages
- **microsoft-codeanalysis**: All Microsoft.CodeAnalysis.* packages
- **sentry**: All Sentry.* packages
- **language-ext**: All LanguageExt.* packages
- **other**: Minor and patch updates for other packages

This reduces PR noise by grouping related updates together.

## Workflow Triggers

The CI pipeline runs on:
- Push to `main` or `develop` branches
- Pull requests targeting `main` or `develop` branches

## Manual Workflow Execution

You can manually trigger workflows from the Actions tab in GitHub:
1. Go to the Actions tab
2. Select the "CI" workflow
3. Click "Run workflow"
4. Choose the branch and click "Run workflow"

