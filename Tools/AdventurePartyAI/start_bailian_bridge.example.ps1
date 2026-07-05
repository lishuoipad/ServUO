$env:ADVENTURE_AI_DRY_RUN = "0"
$env:ADVENTURE_AI_BASE_URL = "https://dashscope.aliyuncs.com/compatible-mode/v1"
$env:ADVENTURE_AI_MODEL = "qwen3.6-flash"
$env:ADVENTURE_AI_API_KEY = "put-your-bailian-api-key-here"

# Enable this only after your selected model/provider confirms tool calling works well.
$env:ADVENTURE_AI_USE_TOOLS = "0"

$env:ADVENTURE_AI_TEMPERATURE = "0.3"
$env:ADVENTURE_AI_MAX_TOKENS = "240"
$env:ADVENTURE_AI_UPSTREAM_TIMEOUT = "8"

& "C:\Users\Shuo\.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe" "C:\Users\Shuo\Desktop\testServUO-57.3\ServUO-57.3\Tools\AdventurePartyAI\adventure_party_ai_bridge.py"
