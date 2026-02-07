<#
.SYNOPSIS
Deploys a self-contained Chasma Git Manager Web API as a Windows Service.
This script also deploys the frontend and starts the website in the background.
When the user logs in, the website will be begin running so the user can use
the site immediately. A desktop shortcut will also be created for ease of use.

.DESCRIPTION
This script:
1. Installs the self-contained solution at the published directory.
2. Creates or updates a Windows Service.
3. Starts the service.
4. Deploys the frontend.
5. Starts the website.
6. Starts the logger for the frontend.
#>

# BACKEND DEPLOYMENT #
$serviceName = "ChasmaWebApiService"
$serviceDisplayName = "Chasma Web API Service"
$serviceDescription = "Provides management of multiple Git repositories by using the self-contained Chasma Git Manager Web API."
$startupType = "auto"
$publishDir = "C:\Services\Chasma"
if (-Not (Test-Path "$publishDir")) {
    mkdir $publishDir
}

$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping existing service..."
    Stop-Service $serviceName -Force
    $service.WaitForStatus('Stopped', '00:00:20')
    sc.exe config $serviceName `
        binPath= "`"$($exePath.FullName)`"" `
        start= $scStartType | Out-Null
}

$artifactDirectory = Join-Path $PSScriptRoot "Artifacts\WebApi"
$exePath = Get-ChildItem $artifactDirectory -Filter *.exe | Select-Object -First 1
if (-not $exePath) {
    Write-Error "Executable not found in '$artifactDirectory'."
    Read-Host "Press ENTER to exit"
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

# --- FRONTEND DEPLOYMENT SETTINGS ---
$frontendPort = 3000
$frontendUrl = "http://localhost:$frontendPort"
$artifactFrontendDir = Join-Path $PSScriptRoot "Artifacts\Thin-Client"
$serveDir = "C:\Services\ChasmaFrontend"
$logFile = Join-Path $serveDir "frontend.log"

if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Error "Node.js is not installed. Please install Node.js before running this installer."
    exit 1
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    Write-Error "npm is not installed. Please install Node.js/npm before running this installer."
    exit 1
}

if (-not (Get-Command serve -ErrorAction SilentlyContinue)) {
    Write-Host "Installing serve globally..."
    npm install -g serve
}

if (-not (Test-Path $serveDir)) {
    New-Item -ItemType Directory -Path $serveDir | Out-Null
}

Write-Host "Deploying frontend artifacts..."
Copy-Item "$artifactFrontendDir\*" $serveDir -Recurse -Force

Write-Host "Starting frontend in background..."
$cmdExe = "$env:SystemRoot\System32\cmd.exe"
$serveCommand = "/c cd /d `"$serveDir`" && npx serve -s build -l $frontendPort >> `"$logFile`" 2>&1"
Start-Process -FilePath $cmdExe -ArgumentList $serveCommand -WindowStyle Hidden

Write-Host "Creating desktop shortcut..."
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "GitCtrl.url"
if (Test-Path $shortcutPath) { Remove-Item $shortcutPath -Force }

@"
[InternetShortcut]
URL=$frontendUrl
"@ | Set-Content $shortcutPath -Encoding ASCII

Start-Process $frontendUrl
Write-Host "Installation complete. Frontend running in background at $frontendUrl."
Write-Host "Check $logFile for logs."

# --- CREATE SCHEDULED TASK TO RUN FRONTEND AT LOGIN ---
$taskName = "ChasmaFrontend"
$taskDescription = "Starts Chasma frontend at user login"
$taskAction = New-ScheduledTaskAction -Execute "cmd.exe" -Argument "/c cd /d `"$serveDir`" && npx serve -s build -l $frontendPort >> `"$logFile`" 2>&1"
$taskTrigger = New-ScheduledTaskTrigger -AtLogOn
$taskPrincipal = New-ScheduledTaskPrincipal -UserId $env:USERNAME -LogonType Interactive -RunLevel Limited
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Write-Host "Scheduled task '$taskName' already exists. Updating..."
    Set-ScheduledTask -TaskName $taskName -Action $taskAction -Trigger $taskTrigger
} else {
    Write-Host "Creating scheduled task '$taskName'..."
    Register-ScheduledTask -TaskName $taskName -Description $taskDescription -Action $taskAction -Trigger $taskTrigger -Principal $taskPrincipal
}
