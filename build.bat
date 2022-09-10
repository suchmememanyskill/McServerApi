rmdir /s /q Release
cd McServerApi
rmdir /s /q __mc_server
dotnet publish -o ../Release
cd ..
cd Release
rmdir /s /q __mc_maps
del ServerData.json