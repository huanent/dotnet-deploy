import * as vscode from 'vscode';
import fs from "fs";
import path from 'path';

const terminalName = 'dotnet-deploy';

export function registerPublishCommand(context: vscode.ExtensionContext) {
    const disposable = vscode.commands.registerCommand('dotnet-deploy.publish', async (uri: vscode.Uri) => {
        const files = fs.readdirSync(uri.path, {
            recursive: true,
            withFileTypes: true
        });

        const csprojFiles = files
            .filter(f => f.isFile() && f.name.endsWith('.csproj'))
            .map(f => path.join(f.parentPath, f.name));

        if (!csprojFiles.length) return;

        let terminal = vscode.window.terminals.find(f => f.name === terminalName);

        if (terminal) {
            terminal.dispose();
        }

        terminal = vscode.window.createTerminal(terminalName);
        terminal.show();
        const command = ["dotnet", "deploy", "publish", "--project", csprojFiles[0]];
        terminal.sendText(command.join(' '));
    });

    context.subscriptions.push(disposable);
}