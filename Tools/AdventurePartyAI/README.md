# Adventure Party AI Bridge

This local bridge lets ServUO call a network AI without storing the network API key in ServUO saves.

ServUO sends party snapshots to:

```text
http://127.0.0.1:8787/decide
```

The bridge then calls an OpenAI-compatible Chat Completions endpoint and returns a constrained JSON decision:

```json
{"order":"recover","target":"none","speech":"","reason":"No immediate threat."}
```

Allowed network orders:

```text
focus_fire
protect_healer
pull_to_choke_point
break_pursuit
recover
```

These orders are squad-level only. ServUO still owns pathing, distance checks, cooldowns, skill legality, target validation, and concrete execution.

## Dry Run

Use dry run first. It does not call any network AI.

PowerShell:

```powershell
$env:ADVENTURE_AI_DRY_RUN = "1"
python .\Tools\AdventurePartyAI\adventure_party_ai_bridge.py
```

In game:

```text
[AdventureParty
[AdventurePartyAI endpoint http://127.0.0.1:8787/decide
[AdventurePartyAI http
[AdventurePartyAI on
[AdventurePartyAI status
```

## Network AI

### Aliyun Bailian / DashScope compatible mode

Recommended first test:

```powershell
$env:ADVENTURE_AI_DRY_RUN = "0"
$env:ADVENTURE_AI_BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
$env:ADVENTURE_AI_MODEL = "qwen3.6-flash"
$env:ADVENTURE_AI_API_KEY = "your-bailian-api-key"
$env:ADVENTURE_AI_TEMPERATURE = "0.3"
$env:ADVENTURE_AI_MAX_TOKENS = "240"
$env:ADVENTURE_AI_UPSTREAM_TIMEOUT = "8"
python .\Tools\AdventurePartyAI\adventure_party_ai_bridge.py
```

For harder encounters, stop the bridge, change the model, and restart it:

```powershell
$env:ADVENTURE_AI_MODEL = "qwen3.7-plus"
```

Optional tool/function calling mode:

```powershell
$env:ADVENTURE_AI_USE_TOOLS = "1"
```

When tool calling is enabled, the bridge asks the model to call:

```text
issue_squad_order
```

The bridge still normalizes and validates the returned order before ServUO sees it.

If the upstream model times out, returns invalid JSON, is rate limited, or returns an empty response, the bridge returns a conservative local fallback order instead of blocking the party.

You can also copy and edit:

```text
Tools/AdventurePartyAI/start_bailian_bridge.example.ps1
```

### Generic OpenAI-compatible endpoint

Set these environment variables before starting the bridge:

```powershell
$env:ADVENTURE_AI_DRY_RUN = "0"
$env:ADVENTURE_AI_UPSTREAM_URL = "https://api.openai.com/v1/chat/completions"
$env:ADVENTURE_AI_MODEL = "your-model-name"
$env:ADVENTURE_AI_API_KEY = "your-api-key"
python .\Tools\AdventurePartyAI\adventure_party_ai_bridge.py
```

For other OpenAI-compatible providers, change `ADVENTURE_AI_UPSTREAM_URL` and `ADVENTURE_AI_MODEL`.

You may also set only a base URL:

```powershell
$env:ADVENTURE_AI_BASE_URL = "https://example.com/compatible-mode/v1"
```

The bridge will call:

```text
<base-url>/chat/completions
```

Optional:

```powershell
$env:ADVENTURE_AI_RESPONSE_FORMAT = "1"
$env:ADVENTURE_AI_UPSTREAM_TIMEOUT = "8"
$env:ADVENTURE_AI_MAX_TOKENS = "160"
$env:ADVENTURE_AI_TEMPERATURE = "0.2"
```

## Failure Tests

After dry run works, test HTTP mode with:

```text
empty response
invalid JSON
HTTP 429 / rate limit
slow response longer than RequestTimeout
```

ServUO should continue using local AI behavior. Any accepted network decision should contain only one of the allowed network orders above.

## ServUO Config

The ServUO endpoint is configured in:

```text
Config/AdventurePartyAI.cfg
```

Default:

```text
HttpEndpointUrl=http://127.0.0.1:8787/decide
```

Do not put the network AI API key in ServUO config. Keep the key in the bridge environment.
