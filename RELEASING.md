# Releasing

Maintainer notes for publishing `Praxicraft.Assess` to NuGet.

## How publish works

[`.github/workflows/publish.yml`](.github/workflows/publish.yml) runs on pushes to **`main`** when:

- `src/**`
- `tests/**`
- `**/*.csproj`
- `CHANGELOG.md`
- `.github/workflows/publish.yml`

Flow:

1. Run `dotnet test` on .NET 8.
2. Require csproj `<Version>` to match `Version.String`.
3. If git tag `v{version}` already exists → skip.
4. Otherwise create + push `v{version}`, `dotnet pack`, `dotnet nuget push`.

## Cut a release

1. Bump `<Version>` in `src/Praxicraft.Assess/Praxicraft.Assess.csproj` and `Version.String` in `Version.cs` (keep them equal).
2. Update `CHANGELOG.md`.
3. Merge to `main`.

## One-time NuGet setup

1. Create the package id [`Praxicraft.Assess`](https://www.nuget.org) (or ensure you can push it).
2. Create GitHub Environment **`nuget`** on `praxicraft-platform/praxicraft-dotnet`.
3. Set repository secret `NUGET_API_KEY` (NuGet.org API key with push access).
4. Merge a version bump to `main` for the first release.

## GitHub Release

The Publish workflow also creates a **GitHub Release** for tag `v{version}` (with generated notes and package assets where applicable).

You can run **Actions → Publish → Run workflow** manually (`workflow_dispatch`) after bumping the version on `main`.

## Auto-bump

Pushes to `main` that change package source auto-bump the patch version, update `CHANGELOG.md`, commit `chore(release): vX.Y.Z`, tag, create a **GitHub Release**, and publish to the language registry when credentials are configured.

Skip with `[skip release]` in the commit message.
