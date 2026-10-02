# Creating and deploying the user installer for Windows Services
1. `cd` into the `emryce-pkg` root.
2. Enter the following command:
```
dotnet publish -c Release -r linux-x64 --self-contained true -o "<your_path>\emryce\Installer\publish"
```
3. TAR the `Installer` folder.
4. Extract all the published package contents into the `emryce-pkg/opt/emryce/`.
5. Run the following command from the `Installer/Linux` root:
```
dpkg-deb --build emryce-<version>-<architecture>
```
5. Go to the `emryce` [releases page.](https://github.com/bee0018/emryce/releases)
6. Either create your own new release or modify an existing release.
7. Attach the Installer binaries to the file drop section and publish release.