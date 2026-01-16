# Developer Deployment Instructions

1. Open an elevated (Administrator) Powershell
2. 'cd' into the root of the `Chasma` repository
3. 'cd' into `Deployment'
4. Run the `'installWebApiWindowsService.ps1` script:
```
.\installWebApiWindowsService.ps1 -projectPath "<your computer path>\ChasmaWebApi.csproj" -publishDir "C:\Services\Chasma"
```
5. Click `Yes` to clean the developer certificate.
6. Click `Yes` to install new developer certificate.
7. Input `Y` and then you will brought to the Swagger web page.