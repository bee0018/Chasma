$serviceName      = "ChasmaWebApiService"
$backendDir       = "C:\Services\Chasma"
$frontendDir      = "C:\Services\ChasmaFrontend"
$taskName         = "ChasmaFrontend"
$desktopShortcut  = Join-Path ([Environment]::GetFolderPath("Desktop")) "GitCtrl.url"
$frontendPort     = 3000

Write-Host "Starting Chasma uninstall..."
$service = Get-Service -Name $serviceName -ErrorAction SilentlyContinue
if ($service) {
    Write-Host "Stopping backend service..."
    Stop-Service -Name $serviceName -Force -ErrorAction SilentlyContinue
    $service.WaitForStatus('Stopped', '00:00:20')

    Write-Host "Deleting backend service..."
    sc.exe delete $serviceName | Out-Null

    Start-Sleep -Seconds 2
} else {
    Write-Host "Backend service not found."
}

Write-Host "Stopping frontend processes..."
Get-NetTCPConnection -LocalPort $frontendPort -ErrorAction SilentlyContinue |
    ForEach-Object {
        $proc = Get-Process -Id $_.OwningProcess -ErrorAction SilentlyContinue
        if ($proc) {
            Write-Host "Stopping process $($proc.ProcessName) (PID $($proc.Id))"
            Stop-Process -Id $proc.Id -Force
        }
    }

Get-Process node, npx -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
if (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue) {
    Write-Host "Removing scheduled task '$taskName'..."
    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
} else {
    Write-Host "Scheduled task not found."
}

if (Test-Path $desktopShortcut) {
    Write-Host "Removing desktop shortcut..."
    Remove-Item $desktopShortcut -Force
} else {
    Write-Host "Desktop shortcut not found."
}

foreach ($dir in @($backendDir, $frontendDir)) {
    if (Test-Path $dir) {
        Write-Host "Removing directory: $dir"
        Remove-Item -Path $dir -Recurse -Force
    } else {
        Write-Host "Directory not found: $dir"
    }
}

Write-Host "Chasma uninstall completed successfully."