#!/usr/bin/env python3
import json
import os
import sys
import time
import urllib.error
import urllib.request
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


ALLOWED_ORDERS = {
    "focus_fire",
    "protect_healer",
    "pull_to_choke_point",
    "break_pursuit",
    "recover",
}


SYSTEM_PROMPT = """You control a four-member NPC adventure party in a ServUO shard.
Return only compact JSON with these keys:
order: one of focus_fire, protect_healer, pull_to_choke_point, break_pursuit, recover
target: nearest, weakest, none, or an empty string
speech: a short in-character sentence, or an empty string
reason: a short operational reason

Never target players, player pets, or summoned creatures.
Choose only squad-level tactics. Do not name spells, skills, bandages, mastery abilities, movement tiles, or code.
ServUO local execution handles class rotations, healer bandage positioning, spell healing, barding, weapon abilities, and mastery legality.
Use recover when multiple party members are critical, dead members need recovery, or the party should pause with no enemies nearby.
Use protect_healer when healerUnderPressure is true in a small fight.
Use focus_fire to burn down one priority target.
Use break_pursuit when the fighter or backline needs to drop direct pursuit pressure.
Use pull_to_choke_point when a large crowded pack should be split by pulling into a held line.
Keep speech short and practical."""


def env_bool(name, default=False):
    value = os.environ.get(name)

    if value is None:
        return default

    return value.strip().lower() in {"1", "true", "yes", "on"}


def normalize_order(value):
    order = str(value or "").strip()
    lowered = order.lower().replace("-", "_").replace(" ", "_")

    aliases = {
        "focusfire": "focus_fire",
        "focus_fire": "focus_fire",
        "protecthealer": "protect_healer",
        "protect_healer": "protect_healer",
        "guard_healer": "protect_healer",
        "pulltochokepoint": "pull_to_choke_point",
        "pull_to_choke_point": "pull_to_choke_point",
        "pull_to_choke": "pull_to_choke_point",
        "pull_choke": "pull_to_choke_point",
        "pull": "pull_to_choke_point",
        "breakpursuit": "break_pursuit",
        "break_pursuit": "break_pursuit",
        "drop_pursuit": "break_pursuit",
        "kite": "break_pursuit",
        "avoid_aoe": "break_pursuit",
        "avoid": "break_pursuit",
        "retreat": "break_pursuit",
        "fallback": "break_pursuit",
        "fall_back": "break_pursuit",
        "recover": "recover",
        "recovery": "recover",
        "rest": "recover",
        "heal": "recover",
        "hold": "recover",
        "hold_position": "recover",
        "explore": "recover",
        "engage": "focus_fire",
        "attack": "focus_fire",
        "fight": "focus_fire",
        "power_up": "focus_fire",
        "powerup": "focus_fire",
        "burst": "focus_fire",
    }

    matched = aliases.get(lowered, lowered)

    if matched not in ALLOWED_ORDERS:
        matched = "recover"

    return matched


def normalize_decision(decision):
    if not isinstance(decision, dict):
        raise ValueError("decision is not a JSON object")

    order = normalize_order(decision.get("order", "") or decision.get("action", ""))

    speech = str(decision.get("speech", "") or "").replace("\r", " ").replace("\n", " ").strip()
    reason = str(decision.get("reason", "") or "").replace("\r", " ").replace("\n", " ").strip()
    target = str(decision.get("target", "") or decision.get("targetSerial", "") or "").replace("\r", " ").replace("\n", " ").strip()

    return {
        "order": order,
        "target": target[:32],
        "speech": speech[:90],
        "reason": reason[:160],
    }


def fallback_decision(servuo_request):
    snapshot = servuo_request.get("snapshot") or {}
    critical = int(snapshot.get("criticalMembers") or 0)
    injured = int(snapshot.get("injuredMembers") or 0)
    enemy_count = int(snapshot.get("enemyCount") or 0)
    hard_targets = int(snapshot.get("hardTargets") or 0)
    fighter_threats = int(snapshot.get("fighterThreats") or 0)
    enemies = snapshot.get("nearbyEnemies") or []
    healer_under_pressure = bool(snapshot.get("healerUnderPressure"))
    backline_under_pressure = bool(snapshot.get("backlineUnderPressure"))
    mixed_enemy_types = bool(snapshot.get("mixedEnemyTypes"))
    burst_recommended = bool(snapshot.get("burstRecommended"))

    if critical >= 2:
        return normalize_decision(
            {
                "order": "recover",
                "target": "none",
                "speech": "Back to the rally point.",
                "reason": "Multiple members are critical.",
            }
        )

    if enemies:
        if enemy_count >= 4 or fighter_threats >= 3 or (mixed_enemy_types and enemy_count >= 3):
            return normalize_decision(
                {
                    "order": "pull_to_choke_point",
                    "target": "nearest",
                    "speech": "Fall back to the line. Pull one through.",
                    "reason": "Large pack should be split before full engagement.",
                }
            )

        if healer_under_pressure:
            return normalize_decision(
                {
                    "order": "protect_healer",
                    "target": "nearest",
                    "speech": "Clear the healer.",
                    "reason": "Healer is under direct pressure.",
                }
            )

        if critical > 0 or backline_under_pressure:
            return normalize_decision(
                {
                    "order": "break_pursuit",
                    "target": "nearest",
                    "speech": "Make room and drop pressure.",
                    "reason": "A member is critical or the back line is under pressure.",
                }
            )

        if fighter_threats >= 2 or (mixed_enemy_types and enemy_count >= 2):
            return normalize_decision(
                {
                    "order": "pull_to_choke_point",
                    "target": "nearest",
                    "speech": "Hold the front. Keep them stacked.",
                    "reason": "Crowded or mixed pack near the fighter.",
                }
            )

        if burst_recommended or hard_targets > 0:
            return normalize_decision(
                {
                    "order": "focus_fire",
                    "target": "nearest",
                    "speech": "Focus the dangerous one.",
                    "reason": "A priority target is present and the party is stable.",
                }
            )

        return normalize_decision(
            {
                "order": "focus_fire",
                "target": "nearest",
                "speech": "Take the nearest threat.",
                "reason": "Hostile creature nearby.",
            }
        )

    if injured > 0:
        return normalize_decision(
            {
                "order": "recover",
                "target": "none",
                "speech": "Hold here a moment.",
                "reason": "Party is injured and no enemy is nearby.",
            }
        )

    return normalize_decision(
        {
            "order": "recover",
            "target": "none",
            "speech": "",
            "reason": "No immediate threat.",
        }
    )


def extract_json_object(text):
    if not text:
        raise ValueError("empty AI response")

    text = text.strip()

    try:
        return json.loads(text)
    except json.JSONDecodeError:
        pass

    start = text.find("{")
    end = text.rfind("}")

    if start < 0 or end <= start:
        raise ValueError("AI response did not contain a JSON object")

    return json.loads(text[start : end + 1])


def build_upstream_payload(servuo_request):
    model = os.environ.get("ADVENTURE_AI_MODEL", "").strip()

    if not model:
        raise RuntimeError("ADVENTURE_AI_MODEL is not set")

    user_payload = {
        "allowedOrders": sorted(ALLOWED_ORDERS),
        "rules": servuo_request.get("rules", ""),
        "snapshot": servuo_request.get("snapshot") or {},
    }

    payload = {
        "model": model,
        "messages": [
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": json.dumps(user_payload, ensure_ascii=False, separators=(",", ":"))},
        ],
        "temperature": float(os.environ.get("ADVENTURE_AI_TEMPERATURE", "0.2")),
        "max_tokens": int(os.environ.get("ADVENTURE_AI_MAX_TOKENS", "160")),
    }

    if env_bool("ADVENTURE_AI_RESPONSE_FORMAT", False):
        payload["response_format"] = {"type": "json_object"}

    if env_bool("ADVENTURE_AI_USE_TOOLS", False):
        payload["tools"] = [
            {
                "type": "function",
                "function": {
                    "name": "issue_squad_order",
                    "description": "Issue one safe high-level order to the ServUO adventure party.",
                    "parameters": {
                        "type": "object",
                        "properties": {
                            "order": {
                                "type": "string",
                                "enum": [
                                    "focus_fire",
                                    "protect_healer",
                                    "pull_to_choke_point",
                                    "break_pursuit",
                                    "recover",
                                ],
                            },
                            "targetSerial": {"type": ["string", "number"]},
                            "formation": {"type": "string"},
                            "retreatHpPercent": {"type": "number"},
                            "memberOrders": {"type": "array", "items": {"type": "object"}},
                            "speech": {"type": "string"},
                            "validForMs": {"type": "number"},
                            "reason": {"type": "string"},
                        },
                        "required": ["order"],
                    },
                },
            }
        ]
        payload["tool_choice"] = {
            "type": "function",
            "function": {"name": "issue_squad_order"},
        }

    return payload


def call_upstream(servuo_request):
    url = os.environ.get("ADVENTURE_AI_UPSTREAM_URL", "").strip()
    base_url = os.environ.get("ADVENTURE_AI_BASE_URL", "").strip()
    api_key = os.environ.get("ADVENTURE_AI_API_KEY", "").strip()

    if not api_key:
        raise RuntimeError("ADVENTURE_AI_API_KEY is not set")

    if not url:
        if base_url:
            url = base_url.rstrip("/") + "/chat/completions"
        else:
            url = "https://api.openai.com/v1/chat/completions"

    payload = build_upstream_payload(servuo_request)
    body = json.dumps(payload, ensure_ascii=False).encode("utf-8")

    request = urllib.request.Request(
        url,
        data=body,
        method="POST",
        headers={
            "Content-Type": "application/json",
            "Accept": "application/json",
            "Authorization": "Bearer " + api_key,
        },
    )

    timeout = float(os.environ.get("ADVENTURE_AI_UPSTREAM_TIMEOUT", "8"))

    with urllib.request.urlopen(request, timeout=timeout) as response:
        response_body = response.read().decode("utf-8")

    data = json.loads(response_body)

    if isinstance(data, dict) and ("order" in data or "action" in data):
        return normalize_decision(data)

    choices = data.get("choices") if isinstance(data, dict) else None

    if not choices:
        raise ValueError("upstream response has no choices")

    message = choices[0].get("message") or {}
    tool_calls = message.get("tool_calls") or []

    if tool_calls:
        function = (tool_calls[0] or {}).get("function") or {}
        arguments = function.get("arguments") or ""
        decision = extract_json_object(arguments)
        return normalize_decision(decision)

    function_call = message.get("function_call") or {}

    if function_call:
        decision = extract_json_object(function_call.get("arguments") or "")
        return normalize_decision(decision)

    content = message.get("content") or ""
    decision = extract_json_object(content)

    return normalize_decision(decision)


class Handler(BaseHTTPRequestHandler):
    server_version = "AdventurePartyAI/1.0"

    def log_message(self, fmt, *args):
        sys.stdout.write("%s - %s\n" % (self.log_date_time_string(), fmt % args))
        sys.stdout.flush()

    def send_json(self, status, obj):
        data = json.dumps(obj, ensure_ascii=False, separators=(",", ":")).encode("utf-8")

        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(data)))
        self.end_headers()
        self.wfile.write(data)

    def do_GET(self):
        if self.path == "/health":
            self.send_json(
                200,
                {
                    "ok": True,
                    "service": "AdventurePartyAI",
                    "dryRun": env_bool("ADVENTURE_AI_DRY_RUN", False),
                    "time": int(time.time()),
                },
            )
            return

        self.send_json(404, {"error": "not found"})

    def do_POST(self):
        if self.path != "/decide":
            self.send_json(404, {"error": "not found"})
            return

        length = int(self.headers.get("Content-Length") or "0")

        if length <= 0 or length > 131072:
            self.send_json(400, {"error": "invalid request size"})
            return

        try:
            body = self.rfile.read(length).decode("utf-8")
            servuo_request = json.loads(body)

            if env_bool("ADVENTURE_AI_DRY_RUN", False):
                decision = fallback_decision(servuo_request)
            else:
                try:
                    decision = call_upstream(servuo_request)
                except Exception as exc:
                    decision = fallback_decision(servuo_request)
                    decision["reason"] = ("Bridge fallback after %s: %s" % (exc.__class__.__name__, exc))[:160]

            self.send_json(200, decision)
        except urllib.error.HTTPError as exc:
            detail = exc.read().decode("utf-8", errors="replace")
            self.send_json(502, {"error": "upstream http error", "status": exc.code, "detail": detail[:500]})
        except Exception as exc:
            self.send_json(500, {"error": exc.__class__.__name__, "detail": str(exc)[:500]})


def main():
    host = os.environ.get("ADVENTURE_AI_HOST", "127.0.0.1")
    port = int(os.environ.get("ADVENTURE_AI_PORT", "8787"))
    dry_run = env_bool("ADVENTURE_AI_DRY_RUN", False)
    upstream = os.environ.get("ADVENTURE_AI_UPSTREAM_URL", "")
    base_url = os.environ.get("ADVENTURE_AI_BASE_URL", "")
    model = os.environ.get("ADVENTURE_AI_MODEL", "")

    server = ThreadingHTTPServer((host, port), Handler)

    print("AdventurePartyAI bridge listening on http://%s:%s/decide" % (host, port))
    print("Dry run: %s" % dry_run)
    if not upstream and base_url:
        upstream = base_url.rstrip("/") + "/chat/completions"

    print("Upstream: %s" % (upstream or "https://api.openai.com/v1/chat/completions"))
    print("Model: %s" % (model or "(not set)"))
    print("Use tools: %s" % env_bool("ADVENTURE_AI_USE_TOOLS", False))
    print("Press Ctrl+C to stop.")

    try:
        server.serve_forever()
    except KeyboardInterrupt:
        pass
    finally:
        server.server_close()


if __name__ == "__main__":
    main()
