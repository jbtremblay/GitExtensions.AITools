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
  - GitHub Copilot CLI
  - Claude Code
  - OpenAI Codex
  - OpenCode
  - Anthropic-compatible API (Claude default; etc.)
  - OpenAI-compatible API (OpenAI default; GitHub Models, Groq, Gemini, OpenRouter, Ollama, LM Studio, etc.)

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
| API Key | API key (required for OpenAI-compatible / Anthropic-compatible) | — |
| Base URL | Endpoint override for the OpenAI-compatible and Anthropic-compatible providers (blank = official API) | — |
| Model override | Use a specific model instead of the provider default | — |
| Commit types | Comma-separated list of allowed conventional commit types | `feat, fix, refactor, ...` |
| Custom instructions | Appended to the built-in prompt | — |

### Connecting to OpenAI-compatible services

The **OpenAI-compatible API** provider can target any service that speaks the OpenAI Chat Completions wire format. Leave **Base URL** blank to hit `api.openai.com`, or fill it in to use an alternative. Click the **Model override** value to see the full catalog of model names the service accepts:

| Service | Base URL | Model override | Sign-up |
|---|---|---|---|
| OpenAI | `https://api.openai.com/v1` | [`gpt-4o-mini`](https://platform.openai.com/docs/models) | [platform.openai.com](https://platform.openai.com) |
| GitHub Models | `https://models.github.ai/inference` | [`openai/gpt-4o-mini`](https://github.com/marketplace?type=models) | [GitHub PAT with `models:read`](https://github.com/settings/tokens?type=beta) |
| Groq | `https://api.groq.com/openai/v1` | [`llama-3.3-70b-versatile`](https://console.groq.com/docs/models) | [console.groq.com](https://console.groq.com) |
| Google Gemini | `https://generativelanguage.googleapis.com/v1beta/openai` | [`gemini-2.0-flash`](https://ai.google.dev/gemini-api/docs/models) | [aistudio.google.com](https://aistudio.google.com) |
| OpenRouter | `https://openrouter.ai/api/v1` | [`meta-llama/llama-3.3-70b-instruct:free`](https://openrouter.ai/models) | [openrouter.ai](https://openrouter.ai) |
| Cerebras | `https://api.cerebras.ai/v1` | [`llama-3.3-70b`](https://inference-docs.cerebras.ai/introduction) | [cloud.cerebras.ai](https://cloud.cerebras.ai) |
| Mistral | `https://api.mistral.ai/v1` | [`mistral-small-latest`](https://docs.mistral.ai/getting-started/models/models_overview/) | [console.mistral.ai](https://console.mistral.ai) |
| Ollama (local) | `http://localhost:11434/v1` | [`llama3.3`](https://ollama.com/library) | [ollama.com](https://ollama.com) |
| LM Studio (local) | `http://localhost:1234/v1` | [`llama-3.3-70b-instruct`](https://lmstudio.ai/models) | [lmstudio.ai](https://lmstudio.ai) |

### Connecting to Anthropic-compatible services

The **Anthropic-compatible API** provider speaks Anthropic's `/v1/messages` wire format. Leave **Base URL** blank for the official Anthropic API with your `sk-ant-…` api key, or set it to any proxy that accepts the api format.

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
