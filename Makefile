all:
	dotnet publish "Windows Installer.sln" --nologo --self-contained --runtime win-x64 --configuration Release
