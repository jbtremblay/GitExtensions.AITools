<p align="center">
  <img src="assets/ai-tools-logo.svg" alt="AI Tools Logo" width="128" height="128" />
</p>

<h1 align="center">Git Extensions AI Tools</h1>

<p align="center">
  AI-powered commit message generation for <a href="https://github.com/gitextensions/gitextensions">Git Extensions</a>
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/GitExtensions.AITools"><img src="https://img.shields.io/nuget/v/GitExtensions.AITools" alt="NuGet Version" /></a>
  <a href="https://www.nuget.org/packages/GitExtensions.AITools"><img src="https://img.shields.io/nuget/dt/GitExtensions.AITools" alt="NuGet Downloads" /></a>
  <a href="LICENSE"><img src="https://img.shields.io/github/license/JBTremblay/gitextensions.aitools" alt="License" /></a>
</p>

## Features

- **AI-generated commit messages** — generates conventional commit messages from staged diffs
- **Auto-fill mode** — automatically writes the commit message as you stage/unstage files
- **Commit template** — also available as a selectable template in the commit dialog dropdown
- **Multiple LLM providers:**
  - Anthropic (Claude)
  - OpenAI
  - GitHub Copilot (uses `gh auth token`, no API key needed)
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
| Provider | LLM provider to use | Anthropic (Claude) |
| API Key | API key (optional for GitHub Copilot / Claude Code / OpenCode) | — |
| Model override | Use a specific model instead of the provider default | — |
| Commit types | Comma-separated list of allowed conventional commit types | `feat, fix, refactor, ...` |
| Custom instructions | Appended to the built-in prompt | — |

## How It Works

- **With auto-fill enabled (default):** The commit message is generated automatically when you stage or unstage files and updates as you go.
- **With auto-fill disabled:** Select the **"AI: Generate commit message"** template from the commit message dropdown to trigger generation.

## Building from Source

Requires .NET 9 SDK.

```
dotnet build -c Debug
```

The build automatically downloads Git Extensions binaries for development. After building, the plugin DLL is copied to the Git Extensions `UserPlugins` directory for testing. Press F5 to launch Git Extensions with the plugin loaded.

To pack as a NuGet package:

```
dotnet pack -c Release
```

## Contributing

Contributions are welcome! To get started:

1. Fork the repository
2. Create a branch from `develop`
3. Make your changes and push to your fork
4. Open a pull request targeting `develop`

Please make sure the CI build passes before requesting a review.

## License

[MIT](LICENSE)

---

<p align="center">
  Made by <a href="https://github.com/JBTremblay">JBTremblay</a> · <a href="https://github.com/sponsors/JBTremblay">Sponsor</a>
</p>
