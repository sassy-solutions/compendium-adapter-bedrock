# Changelog

All notable changes to `Compendium.Adapters.Bedrock` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html). Versions are driven by git tags via [MinVer](https://github.com/adamralph/minver).

## [Unreleased]

### Added
- First-cut implementation of `IAIProvider` against the AWS Bedrock runtime plane, backed by `AWSSDK.BedrockRuntime` 4.0.17.9.
- Synchronous `CompleteAsync` via Bedrock's unified `Converse` API — works across every hosted model family (Anthropic Claude, Meta Llama, Mistral, Amazon Nova, Cohere) without per-model JSON marshaling.
- Streaming `StreamCompleteAsync` via `ConverseStream` — translates the event-stream union (`ContentBlockDeltaEvent`, `MessageStopEvent`, `ConverseStreamMetadataEvent`, …) into Compendium's `Result<CompletionChunk>` flow.
- Embeddings via `InvokeModel` with auto-detected family routing :
  - `amazon.titan-embed-*` → Titan single-text per call with the `dimensions` hint.
  - `cohere.embed-*` → Cohere batch (one HTTP call per request), defaults to `input_type = "search_document"`.
  - Unknown ids fail fast with `AI.InvalidRequest`.
- Typed `BedrockOptions` — `Region` (required), `DefaultModelId`, `EmbeddingModelId`, `AccessKey` / `SecretKey` / `SessionToken`, `DefaultMaxTokens`, `Timeout`, `MaxRetries`.
- AWS credential resolution defaults to the SDK's standard chain (env vars, shared profile, EC2 instance profile, EKS pod identity, IMDSv2); explicit access keys + session token supported for dev / CI.
- Typed error mapping for `401 / 403 / 402 / 404 / 413 / 429 / 503` plus a `ProviderError` fallback, surfaced as `Result.Failure` with `AIErrors.*` codes.
- Message-shape adapter — IAIProvider's text-only `Message` is coalesced into Bedrock's role-alternating `Message`/`SystemContentBlock` model, with system-message promotion and a synthetic user prompt when the request carries only system content.
- Stop-reason mapping for the full `StopReason` constant set (`End_turn`, `Stop_sequence`, `Max_tokens`, `Tool_use`, `Content_filtered`, `Guardrail_intervened`, `Model_context_window_exceeded`, `Malformed_*`).
- Static curated model catalog returned from `ListModelsAsync` (Claude 3.5 Sonnet v2, Claude 3 Haiku, Llama 3.1 70B, Mistral Large 2, Nova Pro, Titan Embed v2, Cohere Embed English v3). No control-plane permission required.
- `HealthCheckAsync` — 1-token probe against the default model id.
- `services.AddCompendiumBedrock(IConfiguration)` and `services.AddCompendiumBedrock(Action<BedrockOptions>)` DI extensions, wiring `IAmazonBedrockRuntime` with the configured credentials, region, and retry policy.
- Sample app `samples/01-chat-with-claude-via-bedrock` demonstrating sync + streaming completions against Claude 3 Haiku.
- 95 unit tests covering provider mapping, streaming translation, embedding family routing, error mapping, options binding, and DI registration — 99.17 % line / 90.74 % branch coverage. All Bedrock SDK calls mocked through `NSubstitute` against `IAmazonBedrockRuntime`.

### Out of scope (deferred to follow-ups)

- Vision / image content blocks (Bedrock's `ImageBlock` is Converse-native but doesn't fit the text-only `IAIProvider.Message` shape; will land on a typed Bedrock-native client).
- Tool / function calling (`ToolConfiguration` + `ToolUseBlock`) — handled by the agent loop in `Compendium.Application`.
- Prompt caching (Anthropic-only on Bedrock; will be opt-in via `BedrockOptions.EnablePromptCaching`).
- Direct `InvokeModel` for chat (per-model JSON bodies). Converse covers 95 % of needs.
- Bedrock Agents (Knowledge Bases, Action Groups). Separate adapter when a consumer needs them.
- Bidirectional streaming (`InvokeModelWithBidirectionalStream`). Reserved for the realtime audio surface.
