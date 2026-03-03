import * as vscode from 'vscode';
import { registerPublishCommand } from './commands/publish';

export function activate(context: vscode.ExtensionContext) {
	registerPublishCommand(context);
}

export function deactivate() { }
