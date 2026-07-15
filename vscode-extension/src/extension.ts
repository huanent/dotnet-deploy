import * as vscode from 'vscode';
import { registerPublishCommand } from './commands/publish';

async function updateProjectFolders() {
	const projectFiles = await vscode.workspace.findFiles('**/*.csproj');
	const projectFolders = [...new Set(projectFiles.map(uri => vscode.Uri.joinPath(uri, '..').path))];

	await vscode.commands.executeCommand('setContext', 'dotnet-deploy.projectFolders', projectFolders);
}

export async function activate(context: vscode.ExtensionContext) {
	registerPublishCommand(context);

	const projectWatcher = vscode.workspace.createFileSystemWatcher('**/*.csproj');
	projectWatcher.onDidCreate(updateProjectFolders, undefined, context.subscriptions);
	projectWatcher.onDidDelete(updateProjectFolders, undefined, context.subscriptions);
	context.subscriptions.push(projectWatcher);

	await updateProjectFolders();
}

export function deactivate() { }
