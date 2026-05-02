# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/),
and this project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]


- New provider: **OpenAI Codex** (CLI). Uses `codex exec`; auth is whatever the local `codex login` resolves to (ChatGPT Plus/Pro/Team/Business/Edu/Enterprise subscription, or OpenAI API key)
- New setting: **Base URL** for the OpenAI-compatible and Anthropic-compatible providers. Lets users target GitHub Models, Groq, OpenRouter, Gemini's OpenAI shim, Cerebras, Together, Fireworks, DeepSeek, Mistral, local Ollama / LM Studio / vLLM, LiteLLM proxies, etc.

### Changed

- Renamed providers: **"OpenAI API (ChatGPT)" → "OpenAI-compatible API"**, **"Anthropic API (Claude)" → "Anthropic-compatible API"**

### Breaking

- The renamed providers are not aliased from their old display names. Users with OpenAI or Anthropic previously configured will see "Unknown AI provider 'OpenAI API (ChatGPT)'. Open AI Tools settings and re-select a provider." in the settings status, and must re-pick from the dropdown once

## [6.0.2] 2026-04-22

### Fixed

- GitHub Copilot provider now detects `copilot.cmd` on Windows (npm-installed Copilot CLI) ([#3](https://github.com/jbtremblay/GitExtensions.AITools/issues/3))
- Updated Copilot CLI install link in the "not found" error message

## [6.0.1] 2026-03-20

### Fixed

- Fixed discovery by the GitExtensions Plugin Manager

## [6.0.0] 2026-03-20

### Added

- Initial release: AI commit message generation for Git Extensions 6.x
- Providers: Anthropic (Claude), OpenAI, GitHub Copilot, Claude Code, OpenCode
- Auto-fill mode (watches `.git/index` for stage/unstage)
- Manual mode using the commit template dropdown integration
- Custom instructions support
- French translation
