if ((Test-Path -Path "showsensors" -PathType Container) `
        -and (Test-Path -Path "service" -PathType Container) `
        -and (Test-Path -Path "installer" -PathType Container)) {
    dotnet clean installer
    dotnet clean
    dotnet build -c Release
    dotnet publish -c Release
    dotnet build installer -c Release
}
