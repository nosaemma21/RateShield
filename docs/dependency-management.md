# Dependency Management

RateShield targets .NET 8 LTS.

## .NET SDK

The repository pins the .NET SDK with `global.json`:

```json
{
  "sdk": {
    "version": "8.0.416",
    "rollForward": "latestPatch",
    "allowPrerelease": false
  }
}
```

This keeps local development and CI on the same .NET SDK feature band. The `rollForward` setting allows newer .NET 8 patch SDKs, but prevents the project from silently moving to .NET 9 or .NET 10.

## Package Versions

NuGet package versions are managed centrally in `Directory.Packages.props`.

Project files should reference packages without hardcoded versions:

```xml
<PackageReference Include="Some.Package" />
```

The version belongs in `Directory.Packages.props`:

```xml
<PackageVersion Include="Some.Package" Version="1.2.3" />
```

## Dependency Updates

Dependabot checks NuGet dependencies weekly.

Minor and patch updates can be grouped into pull requests. Major version upgrades are ignored by default and should be handled manually because they may change framework compatibility, runtime behavior, or public APIs.

## Upgrade Rule

Before accepting dependency updates:

1. Restore packages.
2. Build the full solution.
3. Run all tests.
4. Check release notes for behavior changes.
5. Verify Docker and CI still pass.
