# Git Extensions AI Tools

AI-powered commit message generation for [Git Extensions](https://github.com/gitextensions/gitextensions).

## Features

- **AI-generated commit messages** — generates conventional commit messages from staged diffs
- **Auto-fill mode** — automatically writes the commit message as you stage/unstage files
- **Commit template** — also available as a selectable template in the commit dialog dropdown
- **Multiple LLM providers:**
  - Anthropic (Claude)
  - OpenAI
  - Github Copilot
  - Claude Code
  - OpenCode

## Installation

Install via [GitExtensions.PluginManager](https://github.com/gitextensions/gitextensions.pluginmanager):

1. Open Git Extensions → **Tools** → **Plugin Manager**
2. Search for **AI Tools**
3. Install and restart Git Extensions

## Configuration

Open **Plugins → AI Tools** in Git Extensions to configure:

| Setting | Description | Default |
|---------|-------------|---------|
| Enabled | Enable/disable the plugin | `true` |
| Auto-fill on stage/unstage | Automatically fill the commit message box | `true` |
| Provider | LLM provider to use | GitHub Copilot |
| API Key | API key (optional for GitHub Copilot / Claude Code / OpenCode) | — |
| Model override | Use a specific model instead of the provider default | — |
| Commit types | Comma-separated list of allowed conventional commit types | `feat, fix, refactor, ...` |
| Custom instructions | Appended to the built-in prompt | — |

## How It Works

- **With auto-fill enabled (default):** The commit message is generated automatically when you stage or unstage files and updates as you go.
- **With auto-fill disabled:** Select the **"AI: Generate commit message"** template from the commit message dropdown to trigger generation.

## Links

- [Source code](https://github.com/JBTremblay/gitextensions.aitools)
- [Report an issue](https://github.com/JBTremblay/gitextensions.aitools/issues)
- [Changelog](https://github.com/JBTremblay/gitextensions.aitools/blob/main/CHANGELOG.md)
