# Creating and deploying the user installer for Windows Services
1. `cd` into the `chasma-pkg` root.
2. Enter the following command:
```
dotnet publish -c Release -r linux-x64 --self-contained true -o "<your_path>\Chasma\Installer\publish"
```
3. TAR the `Installer` folder.
4. Extract all the published package contents into the `chasma-pkg/opt/chasma/`.
5. Run the following command from the `chasma-pkg` root:
```
dpkg-deb --build chasma-pkg
```
5. Go to the `Chasma` [releases page.](https://github.com/bee0018/Chasma/releases)
6. Either create your own new release or modify an existing release.
7. Attach the Installer binaries to the file drop section and publish release.