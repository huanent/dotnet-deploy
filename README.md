# Dotnet Deploy Tool

[![NuGet](https://img.shields.io/nuget/v/dotnet-deploy?label=nuget\&style=for-the-badge)](https://www.nuget.org/packages/dotnet-deploy/)

A lightweight deployment tool that simplifies publishing .NET applications to remote Linux hosts over SSH and optionally managing their systemd services.

---

## Installation

### Global installation

```bash
dotnet tool install dotnet-deploy -g
```

### Local installation

```bash
cd ./your_solution_directory
dotnet new tool-manifest
dotnet tool install dotnet-deploy
```

---

## Quick Start

### Publish to a remote host

```bash
dotnet deploy publish --host 127.0.0.1 --username root --password abc123
```

### Install as a systemd service

Running `dotnet deploy publish` will automatically restart the service if the systemd service is already installed.

```bash
dotnet deploy systemd install --host 127.0.0.1 --username root --password abc123
```

---

## Using appsettings.json and User Secrets to simplify commands

You can avoid repeatedly typing host and authentication parameters by defining a `Deploy` section in your configuration.

### appsettings.json

```json
{
  "Deploy": {
    "Host": "127.0.0.1",
    "Username": "root"
  }
}
```

### Protect password using User Secrets

```bash
dotnet user-secrets set Deploy:Password abc123
```

### Publish using configuration

```bash
dotnet deploy publish
```

The tool will automatically read values from configuration and user-secrets.

---

## Commands

### publish

Publishes the project to a remote host.

```bash
dotnet deploy publish
```

### info

Displays current deployment configuration.

```bash
dotnet deploy info
```

### systemd

Manage systemd service on the remote host.

Install:

```bash
dotnet deploy systemd install
```

Uninstall:

```bash
dotnet deploy systemd uninstall
```

Restart:

```bash
dotnet deploy systemd restart
```

Status:

```bash
dotnet deploy systemd status
```

---

## FAQ

### Directory contains multiple projects

Specify the project directory:

```bash
dotnet deploy publish --project ./src/my_project
```

### Directory contains multiple csproj files

Specify the exact project file:

```bash
dotnet deploy publish --project ./src/my_project/my_project.csproj
```

### Supported Operating Systems

The current version has been tested on Ubuntu. Other Linux distributions may work but are not yet fully validated.

### Default resource paths on remote host

Published files:

```
/var/dotnet-apps/[project_name]
```

Systemd service file:

```
/etc/systemd/system/[project_name].service
```

---

## Command Reference

Use `-h` on any command to view its description.

Example:

```bash
dotnet deploy publish -h
```

```
Description:
  Publish project to remote host

Usage:
  DotnetDeploy publish [options]

Options:
  --host            Target host name or domain
  --username        SSH username
  --password        SSH password
  --private-key     SSH private key
  --project         Project path
  --include-files   Copy the specified file or directory into the output directory
  --all-hosts       Publish to all configured hosts
  --before-command  Command to run before dotnet publish
  --after-command   Command to run after dotnet publish
  -?, -h, --help    Show help and usage information
```