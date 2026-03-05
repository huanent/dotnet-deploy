import * as vscode from 'vscode';

const terminalName = 'dotnet-deploy';

export function registerPublishCommand(context: vscode.ExtensionContext) {
    const disposable = vscode.commands.registerCommand('dotnet-deploy.publish', async (uri: vscode.Uri) => {
        let terminal = vscode.window.terminals.find(f => f.name === terminalName);

        if (terminal) {
            terminal.dispose();
        }

        terminal = vscode.window.createTerminal(terminalName);
        terminal.show();
        const command = ["dotnet", "deploy", "publish", "--project", uri.path];
        terminal.sendText(command.join(' '));
    });

    context.subscriptions.push(disposable);
}