@echo off
powershell -NoProfile -ExecutionPolicy Bypass -Command ^
  "Start-Process powershell -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File ""%~dp0ChasmaInstaller.ps1""' -Verb RunAs"
