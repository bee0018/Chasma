<#
.SYNOPSIS
Deploys a self-contained Chasma Git Manager Core Web API as a Windows Service.

.DESCRIPTION
This script:
1. Publishes project as a self-contained single executable.
2. Creates or updates a Windows Service.
3. Starts the service.

#>

param (
    [Parameter(Mandatory)]
    [string]$projectPath,

    [Parameter(Mandatory)]
    [ValidateScript({ Test-Path $_ })]
    [string]$publishDir
)

$runtime = "win-x64"
$serviceName = "ChasmaWebApiService"
$serviceDisplayName = "Chasma Web API Service"
$serviceDescription = "Provides management of multiple Git repositories by using the self-contained Chasma Git Manager Web API."
$startupType = "auto"

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping existing service..."
    Stop-Service $serviceName -Force
    $service.WaitForStatus('Stopped', '00:00:20')
    sc.exe config $serviceName `
        binPath= "`"$($exePath.FullName)`"" `
        start= $scStartType | Out-Null
}

Write-Host "Publishing project..."
dotnet publish $projectPath -c Release -r $runtime --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true -o $publishDir
if (-Not (Test-Path "$publishDir")) {
    Write-Error "Publish failed. Directory not found: $publishDir"
    exit 1
}

$exePath = Get-ChildItem $publishDir -Filter *.exe | Select-Object -First 1
if (-Not $exePath) {
    Write-Error "Executable not found in publish folder."
    exit 1
}

Write-Host "Processing developer HTTPS certificates..."
dotnet dev-certs https --clean
dotnet dev-certs https --trust

if ($service) {
    Write-Host "Starting existing service..."
}
else {
    Write-Host "Creating service '$serviceName'..."
    New-Service -Name $serviceName `
        -BinaryPathName "`"$($exePath.FullName)`"" `
        -DisplayName $serviceDisplayName `
        -Description $serviceDescription `
        -StartupType $startupType
}

Start-Service $serviceName
Write-Host "Service '$serviceName' started successfully!"

$choices = @(
    New-Object System.Management.Automation.Host.ChoiceDescription "&Yes", "Open the API Swagger Page"
    New-Object System.Management.Automation.Host.ChoiceDescription "&No", "Do not open"
)

$result = $Host.UI.PromptForChoice(
    "Open Website",
    "Do you want to open the API Swagger Page?",
    $choices,
    1
)

if ($result -eq 0) {
    Write-Host "Opening page..."
    Start-Process "http://localhost:5000/swagger/index.html"
}
