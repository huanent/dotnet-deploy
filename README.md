# Dotnet deploy tool

[![Nuget](https://img.shields.io/nuget/v/dotnet-deploy?label=nuget&style=for-the-badge)](https://www.nuget.org/packages/dotnet-deploy/)

## Install
#### Install on global environment
```
dotnet tool install dotnet-deploy -g
```

#### Or install on local directory
```
cd ./you_solution_directory
dotnet new tool-manifest
dotnet tool install dotnet-deploy
```

## Quick start
#### Publish to remote host 
```
dotnet deploy publish --host 127.0.0.1 --password abc123 --username root
```
#### Install a systemd service
> Run ```dotnet deploy publish``` command will auto restart service when system service installed 
```
dotnet deploy systemd install --host 127.0.0.1 --password abc123 --username root
```

## Use appsettings.json and user-secrets simplify command options
Define a ```Deploy``` section on configuration file
```
{
  ...
  "Deploy": {
    "Host": "127.0.0.1",
    "Username": "root"
  }
}
```
Use ```user-secrets``` protect host password
```
dotnet user-secrets set Deploy:password abc123
```
Command will auto read project configuration value
```
dotnet deploy publish
```

## Commands
### publish
```
dotnet deploy publish
```

### info
```
dotnet deploy info
```

### systemd
install systemd service
```
dotnet deploy systemd install
```
uninstall systemd service
```
dotnet deploy systemd uninstall
```
restart systemd service
```
dotnet deploy systemd restart
```
get systemd service status
```
dotnet deploy systemd status
```

## FAQ
## Directory contains multi project
```
dotnet deploy publish --project ./src/my_project
```

## Project contains multi csproj files
```
dotnet deploy publish --project ./src/my_project/my_project.csproj
```

## Support OS
Current version test on ubuntu,there is no guarantee that all linux distributions will work well,more case will be test on future
## Default deploy resource path on remote host
Project publish file: ```/var/dotnet-apps/[project_name]```

Service file: ```/etc/systemd/system/[project_name].service```

## Command document
all command use ```-h``` can see command description
```
dotnet deploy publish -h
```
```
Description:
  Publish project to remote host

Usage:
  DotnetDeploy publish [options]

Options:
  --host          Target host name or domain
  --username      SSH username
  --password      SSH password
  --private-key   SSH private key
  --project       Project path
  --include-files  Copy the specified project file or directory to output directory
  --all-hosts      Publish all hosts
  -?, -h, --help  Show help and usage information
```