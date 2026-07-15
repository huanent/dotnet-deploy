import * as vscode from 'vscode';
import { execFile } from 'child_process';

const terminalName = 'dotnet-deploy';
const installCommand = 'dotnet tool install -g dotnet-deploy';

function isDotnetDeployAvailable() {
    return new Promise<boolean>(resolve => {
        execFile('dotnet', ['deploy', '--help'], { timeout: 10000 }, error => resolve(!error));
    });
}

async function ensureDotnetDeployAvailable() {
    if (await isDotnetDeployAvailable()) {
        return true;
    }

    const install = await vscode.window.showWarningMessage(
        'dotnet deploy is not installed.',
        'Install'
    );

    if (install === 'Install') {
        const terminal = vscode.window.createTerminal(terminalName);
        terminal.show();
        terminal.sendText(installCommand);
    }

    return false;
}

async function selectProject(folderUri: vscode.Uri) {
    const entries = await vscode.workspace.fs.readDirectory(folderUri);
    const projectUris = entries
        .filter(([name, type]) => type === vscode.FileType.File && name.toLowerCase().endsWith('.csproj'))
        .map(([name]) => vscode.Uri.joinPath(folderUri, name));

    if (projectUris.length === 1) {
        return projectUris[0];
    }

    if (projectUris.length > 1) {
        const selected = await vscode.window.showQuickPick(
            projectUris.map(uri => ({ label: uri.path.split('/').pop()!, uri })),
            { placeHolder: 'Select a project to deploy' }
        );

        return selected?.uri;
    }
}

export function registerPublishCommand(context: vscode.ExtensionContext) {
    const disposable = vscode.commands.registerCommand('dotnet-deploy.publish', async (uri: vscode.Uri) => {
        if (!await ensureDotnetDeployAvailable()) {
            return;
        }

        const projectUri = await selectProject(uri);
        if (!projectUri) {
            return;
        }

        let terminal = vscode.window.terminals.find(f => f.name === terminalName);

        if (terminal) {
            terminal.dispose();
        }

        terminal = vscode.window.createTerminal(terminalName);
        terminal.show();
        const projectPath = `"${projectUri.fsPath.replaceAll('"', '\\"')}"`;
        const command = ["dotnet", "deploy", "publish", "--project", projectPath];
        terminal.sendText(command.join(' '));
    });

    context.subscriptions.push(disposable);
}