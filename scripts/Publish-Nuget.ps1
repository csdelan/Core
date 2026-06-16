[CmdletBinding()]
param(
    [Parameter()]
    [string[]]$ProjectPaths = @(
        (Join-Path $PSScriptRoot '..\Core\Core.csproj'),
        (Join-Path $PSScriptRoot '..\Core.Persistence\Core.Persistence.csproj')
    ),

    # Back-compat: a single -ProjectPath overrides -ProjectPaths when supplied.
    [Parameter()]
    [string]$ProjectPath,

    [Parameter()]
    [string]$LocalFeedPath = '\\bart\MyNuget',

    [Parameter()]
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Debug'
)

$ErrorActionPreference = 'Stop'

if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPaths = @($ProjectPath)
}

function Write-Step {
    param([string]$Message)

    Write-Host "==> $Message" -ForegroundColor Cyan
}

function Ensure-GitVersionTool {
    $toolList = dotnet tool list --global | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw 'Failed to query globally installed dotnet tools.'
    }

    if ($toolList -notmatch 'GitVersion\.Tool') {
        Write-Step 'Installing GitVersion.Tool'
        dotnet tool install --global GitVersion.Tool
        if ($LASTEXITCODE -ne 0) {
            throw 'Failed to install GitVersion.Tool.'
        }
    }
}

function Resolve-LocalFeedPath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return [System.IO.Path]::GetFullPath($Path)
    }

    return [System.IO.Path]::GetFullPath((Join-Path $PWD.Path $Path))
}

function Ensure-DirectoryExists {
    param([string]$Path)

    try {
        [System.IO.Directory]::CreateDirectory($Path) | Out-Null
    }
    catch {
        throw "Unable to access or create local feed path '$Path'. For UNC paths, ensure the share already exists and is reachable. $($_.Exception.Message)"
    }
}

function Get-ProjectPackageVersion {
    param([string]$ProjectFilePath)

    [xml]$projectXml = Get-Content -LiteralPath $ProjectFilePath -Raw

    $propertyGroups = @($projectXml.Project.PropertyGroup)
    $version = $propertyGroups | ForEach-Object { $_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if (-not [string]::IsNullOrWhiteSpace($version)) {
        return $version.Trim()
    }

    $versionPrefix = $propertyGroups | ForEach-Object { $_.VersionPrefix } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionPrefix)) {
        throw "Unable to determine package version from project file '$ProjectFilePath'."
    }

    $versionSuffix = $propertyGroups | ForEach-Object { $_.VersionSuffix } | Where-Object { $_ -ne $null } | Select-Object -First 1
    if ([string]::IsNullOrWhiteSpace($versionSuffix)) {
        return $versionPrefix.Trim()
    }

    return "$($versionPrefix.Trim())-$($versionSuffix.Trim())"
}

function Publish-Project {
    param(
        [string]$ProjectPath,
        [string]$LocalFeedFullPath,
        [string]$Configuration
    )

    $resolvedProjectPath = [System.IO.Path]::GetFullPath($ProjectPath)
    if (-not (Test-Path -LiteralPath $resolvedProjectPath)) {
        throw "Project file not found: $resolvedProjectPath"
    }

    $projectDirectory = Split-Path -Parent $resolvedProjectPath
    $artifactsPath = Join-Path $projectDirectory 'artifacts'
    $packageVersion = Get-ProjectPackageVersion -ProjectFilePath $resolvedProjectPath

    Write-Step 'Cleaning previous package artifacts'
    if (Test-Path -LiteralPath $artifactsPath) {
        Remove-Item -LiteralPath $artifactsPath -Recurse -Force
    }
    Ensure-DirectoryExists -Path $artifactsPath

    Write-Step "Building $resolvedProjectPath with package version $packageVersion"
    dotnet build $resolvedProjectPath --configuration $Configuration --nologo /p:UpdateVersionProperties=false /p:Version=$packageVersion /p:PackageVersion=$packageVersion | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet build failed.'
    }

    Write-Step "Packing $resolvedProjectPath with package version $packageVersion"
    dotnet pack $resolvedProjectPath --configuration $Configuration --no-build --output $artifactsPath --nologo /p:UpdateVersionProperties=false /p:Version=$packageVersion /p:PackageVersion=$packageVersion | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw 'dotnet pack failed.'
    }

    $packages = Get-ChildItem -Path $artifactsPath -Filter '*.nupkg' | Where-Object { $_.Name -notlike '*.symbols.nupkg' }
    if (-not $packages) {
        throw "No NuGet packages were produced in $artifactsPath"
    }

    $published = @()
    foreach ($package in $packages) {
        Write-Step "Publishing $($package.Name) to $LocalFeedFullPath"
        dotnet nuget push $package.FullName --source $LocalFeedFullPath --skip-duplicate | Out-Host
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to publish package $($package.FullName)"
        }
        $published += $package.Name
    }

    return $published
}

$localFeedFullPath = Resolve-LocalFeedPath -Path $LocalFeedPath

Write-Step 'Ensuring prerequisites'
Ensure-GitVersionTool

Write-Step "Creating local feed folder at $localFeedFullPath"
Ensure-DirectoryExists -Path $localFeedFullPath

$allPublished = @()
foreach ($project in $ProjectPaths) {
    $allPublished += Publish-Project -ProjectPath $project -LocalFeedFullPath $localFeedFullPath -Configuration $Configuration
}

Write-Step 'Completed successfully'
Write-Host "Published the following package(s) to: $localFeedFullPath" -ForegroundColor Green
foreach ($name in $allPublished) {
    Write-Host "  - $name" -ForegroundColor Green
}
