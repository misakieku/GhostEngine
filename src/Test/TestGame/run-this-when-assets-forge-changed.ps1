echo "Publising Ghost.AssetForge.CLI"
dotnet publish ../../Tools/Ghost.AssetForge.CLI/Ghost.AssetForge.CLI.csproj -c Release -o ../../Tools/Ghost.AssetForge.CLI/bin/Release/Publish
echo "Cleaning old asset caches"
dotnet clean ./TestGame.csproj $args
echo "Building TestGame"
dotnet build ./TestGame.csproj $args