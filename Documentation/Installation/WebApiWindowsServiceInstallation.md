# Creating and deploying the user installer for Windows Services
1. `cd` into the Chasma root.
2. Enter the following command:
```
dotnet publish "<your_path>\ChasmaWebApi.csproj" -c Release -r "win-x64" --self-contained true /p:PublishSingleFile=true /p:IncludeAllContentForSelfExtract=true -o "<your_path>\Chasma\Installer\Artifacts"
```
3. Zip the `Installer` folder.
4. Go to the `Chasma` [releases page.](https://github.com/bee0018/Chasma/releases)
5. Either create your own new release or modify an existing release.
6. Attach the Installer binaries to the file drop section and publish release.