# dotnet-deploy

[![NuGet](https://img.shields.io/nuget/v/dotnet-deploy?label=nuget&style=for-the-badge)](https://www.nuget.org/packages/dotnet-deploy/)

`dotnet-deploy` is a .NET tool for publishing a project to a remote Linux server over SSH and optionally managing the matching `systemd` service.

It is built for the common case of deploying a single ASP.NET Core or console application to a host you control:

- discover the target `.csproj`
- build a self-contained Linux publish output for the remote CPU architecture
- upload and extract the package on the server
- restart the matching `systemd` service when it already exists
- generate and install a `systemd` unit file when needed

## Features

- SSH deployment to remote Linux hosts
- Password or private-key authentication
- Automatic runtime identifier selection from the remote machine architecture
- Optional extra file inclusion into the publish output
- Deployment settings from `appsettings.json`, `appsettings.deploy.json`, and User Secrets
- Multi-host deployment through `Deploy:Hosts`
- Built-in `systemd` install, uninstall, restart, and status commands

## Requirements

Local machine:

- .NET SDK 8 or later
- A deployable .NET project

Remote machine:

- A Linux host reachable over SSH
- A user that can run `sudo`
- `systemd` if you want service management
- A supported CPU architecture:
  - `x86_64` -> `linux-x64`
  - `aarch64` -> `linux-arm64`

## Installation

### Global installation

```bash
dotnet tool install -g dotnet-deploy
```

Run the global tool with:

```bash
dotnet-deploy -h
```

### Local installation

```bash
dotnet new tool-manifest
dotnet tool install dotnet-deploy
```

Run the local tool with:

```bash
dotnet tool run dotnet-deploy -h
```

## Quick Start

### Publish to a remote host

```bash
dotnet-deploy publish --host 192.168.1.10 --username root --password abc123
```

During `publish`, the tool:

1. discovers the project file
2. loads deployment settings
3. connects to the remote server over SSH
4. runs a self-contained local `dotnet publish`
5. compresses the publish output
6. uploads the archive to the remote host
7. extracts files into the remote application directory
8. attempts to restart the matching `systemd` service if it already exists

### Install as a systemd service

```bash
dotnet-deploy systemd install --host 192.168.1.10 --username root --password abc123
```

### Check service status

```bash
dotnet-deploy systemd status --host 192.168.1.10 --username root --password abc123
```

## Project Discovery

By default, the tool starts from the current working directory.

Accepted `--project` values:

- a `.csproj` file path
- a directory that contains exactly one `.csproj`
- a relative or absolute path

If the directory contains multiple project files, pass the exact `.csproj` path.

Examples:

```bash
dotnet-deploy publish --project ./src/MyApp/MyApp.csproj --host 192.168.1.10
dotnet-deploy publish --project ./src/MyApp --host 192.168.1.10
```

## Configuration

The tool loads deployment configuration from the project directory in this order:

1. `appsettings.json`
2. `appsettings.deploy.json`
3. User Secrets for the target project, when `UserSecretsId` is defined

All deployment settings are read from the `Deploy` section.

### Minimal configuration

```json
{
  "Deploy": {
    "Host": "192.168.1.10",
    "Username": "root"
  }
}
```

Store the password in User Secrets:

```bash
dotnet user-secrets set Deploy:Password abc123
```

Then publish with:

```bash
dotnet-deploy publish
```

### Example appsettings.deploy.json

```json
{
  "Deploy": {
    "Host": "192.168.1.10",
    "Username": "root",
    "BeforeCommand": "dotnet test",
    "AfterCommand": "echo publish completed",
    "IncludeFiles": [
      "appsettings.Production.json",
      "Assets/*"
    ],
    "Systemd": {
      "Unit": {
        "Description": "MyApp service"
      },
      "Service": {
        "RestartSec": 3,
        "Environment": {
          "ASPNETCORE_URLS": "http://0.0.0.0:8080",
          "ConnectionStrings__Default": "..."
        }
      }
    },
    "Hosts": {
      "staging": {
        "Host": "192.168.1.20",
        "Username": "deploy",
        "Systemd": {
          "Service": {
            "Environment": {
              "ASPNETCORE_ENVIRONMENT": "Staging"
            }
          }
        }
      },
      "production": {
        "Host": "192.168.1.30",
        "Username": "deploy"
      }
    }
  }
}
```

### Supported Deploy properties

Top-level settings:

- `Host`
- `Username`
- `Password`
- `PrivateKey`
- `IncludeFiles`
- `BeforeCommand`
- `AfterCommand`
- `Systemd`
- `Hosts`

Entries under `Deploy:Hosts:<name>` override the shared top-level settings for that host.

## Authentication

The tool supports:

- password authentication via `Password`
- private key authentication via `PrivateKey`

When a private key is used, `Password` can also act as the private key passphrase.

Examples:

```bash
dotnet-deploy publish --host 192.168.1.10 --username root --password abc123
dotnet-deploy publish --host 192.168.1.10 --username root --private-key ~/.ssh/id_rsa
```

## Multi-Host Deployment

If `Deploy:Hosts` is configured, you can select a named host directly:

```bash
dotnet-deploy publish --host production
```

Or deploy to all configured hosts in sequence:

```bash
dotnet-deploy publish --all-hosts --host staging
```

Behavior notes:

- the host selected by `--host` or `Deploy:Host` runs first
- `--all-hosts` then runs the same command for every configured host in `Deploy:Hosts`
- the initial host is skipped if it appears again in `Deploy:Hosts`

## Publish Behavior

### Local build output

`publish` creates temporary output under:

```text
<project-directory>/bin/__dotnet_deploy_temp__/publish
```

It also creates a temporary archive at:

```text
<project-directory>/bin/__dotnet_deploy_temp__/publish.tar.gz
```

### Remote deployment paths

The remote root directory is:

```text
/opt/dotnet
```

For a project whose assembly name is `MyApp`, deployment uses:

- application directory: `/opt/dotnet/MyApp`
- uploaded archive: `/opt/dotnet/MyApp.tar.gz`

### IncludeFiles

`IncludeFiles` entries must be relative paths.

The tool will:

- copy an explicitly named file if it exists
- search the project directory recursively for matching patterns and copy matches into the publish output

Absolute paths are rejected.

### BeforeCommand and AfterCommand

If `BeforeCommand` or `AfterCommand` is configured, that command runs locally in the project root directory.

Typical uses:

- run tests before publishing
- build additional assets
- run a post-publish script

## systemd Integration

The tool can generate a `systemd` service based on the project assembly name.

Default generated values include:

- `WorkingDirectory=/opt/dotnet/<AssemblyName>`
- `ExecStart=/opt/dotnet/<AssemblyName>/<AssemblyName>`
- `Restart=always`
- `RestartSec=10`
- `KillSignal=SIGINT`
- `SyslogIdentifier=<AssemblyName>`
- `Environment=ASPNETCORE_ENVIRONMENT=Production`
- `WantedBy=multi-user.target`

During `systemd install`, the tool also adds:

- `User=<ssh username>`

The generated service file is uploaded to:

```text
/opt/dotnet/<AssemblyName>.service
```

Then it is linked into:

```text
/etc/systemd/system/<AssemblyName>.service
```

### Override generated systemd content

Use `Deploy:Systemd` to override or extend the generated unit.

Example:

```json
{
  "Deploy": {
    "Systemd": {
      "Unit": {
        "Description": "My custom service"
      },
      "Service": {
        "RestartSec": 5,
        "Environment": {
          "ASPNETCORE_ENVIRONMENT": "Production",
          "MyOptions__Enabled": "true"
        }
      },
      "Install": {
        "WantedBy": "multi-user.target"
      }
    }
  }
}
```

## Commands

### publish

Publish the project to a remote host.

```bash
dotnet-deploy publish [options]
```

Common options:

- `--host`
- `--username`
- `--password`
- `--private-key`
- `--project`
- `--all-hosts`
- `--include-files`

### info

Display resolved deployment information for the target project and host.

```bash
dotnet-deploy info [options]
```

This command prints:

- assembly name
- project root directory
- temporary working directory
- resolved `.csproj` path
- selected host
- resolved deployment options as JSON
- generated `systemd` service content

### systemd install

Generate, upload, enable, and start a `systemd` service.

```bash
dotnet-deploy systemd install [options]
```

### systemd uninstall

Stop and disable the service, then remove the uploaded service file.

```bash
dotnet-deploy systemd uninstall [options]
```

### systemd restart

Restart the service.

```bash
dotnet-deploy systemd restart [options]
```

### systemd status

Show service status using `systemctl status --no-pager -l`.

```bash
dotnet-deploy systemd status [options]
```

## Examples

### Deploy using only configuration

```bash
dotnet-deploy publish
```

### Deploy a specific project file

```bash
dotnet-deploy publish --project ./src/MyApp/MyApp.csproj --host production
```

### Deploy with additional files

```bash
dotnet-deploy publish --host production --include-files appsettings.Production.json --include-files wwwroot/*
```

### Install and start the service

```bash
dotnet-deploy systemd install --host production
```

### Check resolved settings before deploying

```bash
dotnet-deploy info --host production
```

## Troubleshooting

### The project directory contains multiple `.csproj` files

Pass the exact project file:

```bash
dotnet-deploy publish --project ./src/MyApp/MyApp.csproj
```

### The tool says the host is empty

Provide `--host` explicitly or set `Deploy:Host` in configuration.

### The service is not restarted after publish

`publish` attempts to stop and restart `<AssemblyName>` automatically, but only if the service already exists on the remote machine. Install it first:

```bash
dotnet-deploy systemd install --host production
```

### The server architecture is not supported

At the moment, only these remote architectures are mapped for publish:

- `x86_64`
- `aarch64`

## Help

Use `-h` or `--help` on any command.

Examples:

```bash
dotnet-deploy -h
dotnet-deploy publish -h
dotnet-deploy systemd -h
dotnet-deploy systemd install -h
```