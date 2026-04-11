# Run Chasma Application from Dev Environment

1. Open your `launchSettings.json` in Visual Studio.
2. Locate your HTTP run configuration and configure it to the port where you want to run it.
3. Go to `config.xml` and update the element `<bindingPort>` to be the same port.
4. `cd` into `chasma-thin-client` and run  `npm run build`.
5. Copy the build contents into `Chasma\ChasmaWebApi\wwwroot`
6. Run the application. The backend will start AND serve the frontend application as well.


# Production Deployment

1. Publish a self contained project from the `ChasmaWebApi` using the command:
```
dotnet publish -c Release -r win-x64 --self-contained true -o "path_of_your_choosing"
```
2. Run the executable: `ChasmaWebApi.exe`