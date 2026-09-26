echo "Publising Ghost.AssetForge.CLI"
dotnet publish src/Tools/Ghost.AssetForge.CLI/Ghost.AssetForge.CLI.csproj -c Release -o src/Tools/Ghost.AssetForge.CLI/bin/Release/Publish
echo "Cleaning old asset caches"
dotnet clean src/Test/TestGame.csproj $args
echo "Building TestGame"
dotnet build src/Test/TestGame.csproj $args