using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Server.Engines.AdventureParty
{
    public enum AdventurePartyAIAction
    {
        None,
        Explore,
        Engage,
        FocusFire,
        ProtectHealer,
        Kite,
        AvoidAOE,
        HoldChokePoint,
        PullToChokePoint,
        PowerUp,
        Rest,
        Retreat,
        Avoid,
        HoldPosition,
        BreakPursuit,
        Recover,
        Say
    }

    public enum AdventurePartyAIProviderMode
    {
        Mock,
        HttpEndpoint
    }

    public static class AdventurePartyAIConfig
    {
        public static bool EnabledByDefault = Server.Config.Get("AdventurePartyAI.EnabledByDefault", false);
        public static AdventurePartyAIProviderMode DefaultProvider = Server.Config.GetEnum("AdventurePartyAI.DefaultProvider", AdventurePartyAIProviderMode.Mock);

        // Leave these blank in source control. Point this at a local bridge service when you are ready.
        public static string HttpEndpointUrl = Server.Config.Get("AdventurePartyAI.HttpEndpointUrl", "http://127.0.0.1:8787/decide");
        public static string HttpAuthorizationHeader = Server.Config.Get("AdventurePartyAI.HttpAuthorizationHeader", "");

        public static readonly TimeSpan RequestCooldown = Server.Config.Get("AdventurePartyAI.RequestCooldown", TimeSpan.FromSeconds(20.0));
        public static readonly TimeSpan FailureCooldown = Server.Config.Get("AdventurePartyAI.FailureCooldown", TimeSpan.FromSeconds(60.0));
        public static readonly TimeSpan RequestTimeout = Server.Config.Get("AdventurePartyAI.RequestTimeout", TimeSpan.FromSeconds(3.0));
        public static readonly TimeSpan DecisionMaxAge = Server.Config.Get("AdventurePartyAI.DecisionMaxAge", TimeSpan.FromSeconds(45.0));

        public const int MaxEnemiesInSnapshot = 6;
        public const int MaxSpeechLength = 90;
    }

    public sealed class AdventurePartyAIMemberInfo
    {
        public string Role;
        public int Hits;
        public int HitsMax;
        public int Mana;
        public int ManaMax;
        public int DistanceToLeader;
        public bool Critical;
        public bool UnderPressure;
        public bool DirectTarget;
        public int NearbyEnemies;
        public string Build;

        public string ToJson()
        {
            return String.Format(
                "{{\"role\":\"{0}\",\"build\":\"{1}\",\"hits\":{2},\"hitsMax\":{3},\"mana\":{4},\"manaMax\":{5},\"distanceToLeader\":{6},\"critical\":{7},\"underPressure\":{8},\"directTarget\":{9},\"nearbyEnemies\":{10}}}",
                AdventurePartyAI.EscapeJson(Role),
                AdventurePartyAI.EscapeJson(Build),
                Hits,
                HitsMax,
                Mana,
                ManaMax,
                DistanceToLeader,
                AdventurePartyAI.BoolJson(Critical),
                AdventurePartyAI.BoolJson(UnderPressure),
                AdventurePartyAI.BoolJson(DirectTarget),
                NearbyEnemies);
        }
    }

    public sealed class AdventurePartyAIEnemyInfo
    {
        public string TypeName;
        public string Name;
        public string Serial;
        public int Hits;
        public int HitsMax;
        public int Distance;
        public bool HardTarget;
        public bool CasterLike;
        public bool TargetingParty;
        public string TargetRole;

        public string ToJson()
        {
            return String.Format(
                "{{\"type\":\"{0}\",\"name\":\"{1}\",\"serial\":\"{2}\",\"hits\":{3},\"hitsMax\":{4},\"distance\":{5},\"hardTarget\":{6},\"casterLike\":{7},\"targetingParty\":{8},\"targetRole\":\"{9}\"}}",
                AdventurePartyAI.EscapeJson(TypeName),
                AdventurePartyAI.EscapeJson(Name),
                AdventurePartyAI.EscapeJson(Serial),
                Hits,
                HitsMax,
                Distance,
                AdventurePartyAI.BoolJson(HardTarget),
                AdventurePartyAI.BoolJson(CasterLike),
                AdventurePartyAI.BoolJson(TargetingParty),
                AdventurePartyAI.EscapeJson(TargetRole));
        }
    }

    public sealed class AdventurePartyAISnapshot
    {
        public string State;
        public string Tactic;
        public string MapName;
        public string RegionName;
        public string Location;
        public int LivingMembers;
        public int InjuredMembers;
        public int CriticalMembers;
        public int EnemyCount;
        public int HardTargets;
        public int DistinctEnemyTypes;
        public int HealerThreats;
        public int FighterThreats;
        public int BacklineThreats;
        public bool HealerUnderPressure;
        public bool FighterUnderPressure;
        public bool BacklineUnderPressure;
        public bool MixedEnemyTypes;
        public bool PartyHealthy;
        public bool BurstRecommended;
        public readonly List<AdventurePartyAIMemberInfo> Members = new List<AdventurePartyAIMemberInfo>();
        public readonly List<AdventurePartyAIEnemyInfo> Enemies = new List<AdventurePartyAIEnemyInfo>();

        public string ToJson()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("{");
            builder.AppendFormat("\"state\":\"{0}\",", AdventurePartyAI.EscapeJson(State));
            builder.AppendFormat("\"tactic\":\"{0}\",", AdventurePartyAI.EscapeJson(Tactic));
            builder.AppendFormat("\"map\":\"{0}\",", AdventurePartyAI.EscapeJson(MapName));
            builder.AppendFormat("\"region\":\"{0}\",", AdventurePartyAI.EscapeJson(RegionName));
            builder.AppendFormat("\"location\":\"{0}\",", AdventurePartyAI.EscapeJson(Location));
            builder.AppendFormat("\"livingMembers\":{0},", LivingMembers);
            builder.AppendFormat("\"injuredMembers\":{0},", InjuredMembers);
            builder.AppendFormat("\"criticalMembers\":{0},", CriticalMembers);
            builder.AppendFormat("\"enemyCount\":{0},", EnemyCount);
            builder.AppendFormat("\"hardTargets\":{0},", HardTargets);
            builder.AppendFormat("\"distinctEnemyTypes\":{0},", DistinctEnemyTypes);
            builder.AppendFormat("\"healerThreats\":{0},", HealerThreats);
            builder.AppendFormat("\"fighterThreats\":{0},", FighterThreats);
            builder.AppendFormat("\"backlineThreats\":{0},", BacklineThreats);
            builder.AppendFormat("\"healerUnderPressure\":{0},", AdventurePartyAI.BoolJson(HealerUnderPressure));
            builder.AppendFormat("\"fighterUnderPressure\":{0},", AdventurePartyAI.BoolJson(FighterUnderPressure));
            builder.AppendFormat("\"backlineUnderPressure\":{0},", AdventurePartyAI.BoolJson(BacklineUnderPressure));
            builder.AppendFormat("\"mixedEnemyTypes\":{0},", AdventurePartyAI.BoolJson(MixedEnemyTypes));
            builder.AppendFormat("\"partyHealthy\":{0},", AdventurePartyAI.BoolJson(PartyHealthy));
            builder.AppendFormat("\"burstRecommended\":{0},", AdventurePartyAI.BoolJson(BurstRecommended));

            builder.Append("\"members\":[");

            for (int i = 0; i < Members.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.Append(Members[i].ToJson());
            }

            builder.Append("],\"nearbyEnemies\":[");

            for (int i = 0; i < Enemies.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(",");
                }

                builder.Append(Enemies[i].ToJson());
            }

            builder.Append("]}");

            return builder.ToString();
        }
    }

    public sealed class AdventurePartyAIDecision
    {
        public AdventurePartyAIAction Action;
        public string Target;
        public string Speech;
        public string Reason;
        public DateTime Created;

        public bool IsExpired
        {
            get { return Created != DateTime.MinValue && DateTime.UtcNow - Created > AdventurePartyAIConfig.DecisionMaxAge; }
        }

        public override string ToString()
        {
            if (String.IsNullOrEmpty(Speech))
            {
                return Action.ToString();
            }

            return String.Format("{0}: {1}", Action, Speech);
        }
    }

    public delegate void AdventurePartyAICallback(AdventurePartyAIDecision decision, string error);

    public static class AdventurePartyAI
    {
        private const string AllowedOrders = "focus_fire, protect_healer, pull_to_choke_point, break_pursuit, recover";

        public static void BeginRequest(
            AdventurePartyAISnapshot snapshot,
            AdventurePartyAIProviderMode provider,
            AdventurePartyAICallback callback)
        {
            ThreadPool.QueueUserWorkItem(
                delegate
                {
                    AdventurePartyAIDecision decision = null;
                    string error = null;

                    try
                    {
                        if (provider == AdventurePartyAIProviderMode.HttpEndpoint)
                        {
                            decision = RequestHttpDecision(snapshot);
                        }
                        else
                        {
                            decision = CreateMockDecision(snapshot);
                        }
                    }
                    catch (Exception ex)
                    {
                        error = ex.GetType().Name + ": " + ex.Message;
                    }

                    if (callback != null)
                    {
                        callback(decision, error);
                    }
                });
        }

        public static AdventurePartyAIDecision CreateMockDecision(AdventurePartyAISnapshot snapshot)
        {
            AdventurePartyAIDecision decision = new AdventurePartyAIDecision();

            decision.Created = DateTime.UtcNow;

            if (snapshot == null)
            {
                decision.Action = AdventurePartyAIAction.None;
                decision.Reason = "No snapshot.";
                return decision;
            }

            if (snapshot.CriticalMembers >= 2)
            {
                decision.Action = AdventurePartyAIAction.Retreat;
                decision.Speech = "Back to the rally point. Keep moving.";
                decision.Reason = "Multiple members are critical.";
            }
            else if (snapshot.Enemies.Count > 0)
            {
                bool largePack = snapshot.EnemyCount >= 4 || snapshot.FighterThreats >= 3 || (snapshot.MixedEnemyTypes && snapshot.EnemyCount >= 3);

                if (largePack)
                {
                    decision.Action = AdventurePartyAIAction.PullToChokePoint;
                    decision.Target = "nearest";
                    decision.Speech = "Fall back to the line. Pull one through.";
                    decision.Reason = "The visible pack is large enough to favor pulling into a held line.";
                }
                else if (snapshot.HealerUnderPressure)
                {
                    decision.Action = AdventurePartyAIAction.ProtectHealer;
                    decision.Target = "nearest";
                    decision.Speech = "Clear the healer.";
                    decision.Reason = "Healer is under direct pressure.";
                }
                else if (snapshot.CriticalMembers > 0)
                {
                    decision.Action = AdventurePartyAIAction.BreakPursuit;
                    decision.Target = "nearest";
                    decision.Speech = "Make room for healing.";
                    decision.Reason = "A party member is critical while enemies are nearby.";
                }
                else if (snapshot.BacklineUnderPressure)
                {
                    decision.Action = AdventurePartyAIAction.Kite;
                    decision.Target = "nearest";
                    decision.Speech = "Back line, keep moving.";
                    decision.Reason = "A ranged or caster member is under pressure.";
                }
                else if (snapshot.FighterThreats >= 2 || (snapshot.MixedEnemyTypes && snapshot.EnemyCount >= 2))
                {
                    decision.Action = AdventurePartyAIAction.HoldChokePoint;
                    decision.Target = "nearest";
                    decision.Speech = "Hold the front. Keep them stacked.";
                    decision.Reason = "The fighter is handling a crowded or mixed pack.";
                }
                else if (snapshot.BurstRecommended)
                {
                    decision.Action = AdventurePartyAIAction.PowerUp;
                    decision.Target = "nearest";
                    decision.Speech = "Push hard while we have control.";
                    decision.Reason = "Party is healthy and a burst window is available.";
                }
                else if (snapshot.HardTargets > 0)
                {
                    decision.Action = AdventurePartyAIAction.FocusFire;
                    decision.Target = "nearest";
                    decision.Speech = "Focus the dangerous one.";
                    decision.Reason = "A hard target is present.";
                }
                else
                {
                    decision.Action = AdventurePartyAIAction.Engage;
                    decision.Target = "nearest";
                    decision.Speech = "Take the closest threat. Guard the healer.";
                    decision.Reason = "Hostile creature nearby.";
                }
            }
            else if (snapshot.InjuredMembers > 0)
            {
                decision.Action = AdventurePartyAIAction.Recover;
                decision.Speech = "Hold here a moment.";
                decision.Reason = "One or more members are injured.";
            }
            else if ((DateTime.UtcNow.Ticks % 5L) == 0L)
            {
                decision.Action = AdventurePartyAIAction.Say;
                decision.Speech = "Quiet steps. Eyes open.";
                decision.Reason = "Ambient party chatter.";
            }
            else
            {
                decision.Action = AdventurePartyAIAction.Explore;
                decision.Reason = "No immediate threat.";
            }

            NormalizeDecision(decision);

            return decision;
        }

        public static AdventurePartyAIDecision RequestHttpDecision(AdventurePartyAISnapshot snapshot)
        {
            if (String.IsNullOrEmpty(AdventurePartyAIConfig.HttpEndpointUrl))
            {
                throw new InvalidOperationException("AdventurePartyAIConfig.HttpEndpointUrl is empty.");
            }

            string body = BuildHttpRequestBody(snapshot);
            byte[] bytes = Encoding.UTF8.GetBytes(body);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(AdventurePartyAIConfig.HttpEndpointUrl);

            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8";
            request.Accept = "application/json";
            request.Timeout = (int)AdventurePartyAIConfig.RequestTimeout.TotalMilliseconds;
            request.ReadWriteTimeout = (int)AdventurePartyAIConfig.RequestTimeout.TotalMilliseconds;
            request.ContentLength = bytes.Length;

            if (!String.IsNullOrEmpty(AdventurePartyAIConfig.HttpAuthorizationHeader))
            {
                request.Headers["Authorization"] = AdventurePartyAIConfig.HttpAuthorizationHeader;
            }

            using (Stream stream = request.GetRequestStream())
            {
                stream.Write(bytes, 0, bytes.Length);
            }

            using (WebResponse response = request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                string text = reader.ReadToEnd();
                AdventurePartyAIDecision decision = ParseDecision(text);

                if (decision == null)
                {
                    throw new InvalidOperationException("AI endpoint returned no usable decision.");
                }

                return decision;
            }
        }

        public static string BuildHttpRequestBody(AdventurePartyAISnapshot snapshot)
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("{");
            builder.Append("\"kind\":\"servuo_adventure_party_decision\",");
            builder.AppendFormat("\"allowedOrders\":\"{0}\",", EscapeJson(AllowedOrders));
            builder.Append("\"rules\":\"Return compact JSON with order, target, speech, and reason. The order must be exactly one of: focus_fire, protect_healer, pull_to_choke_point, break_pursuit, recover. Choose only squad-level tactics and short party dialogue. Do not name spells, skills, bandages, masteries, movement tiles, code, players, pets, or summoned creatures. ServUO local execution handles movement, pathing, distance checks, class rotations, healing, bandage positioning, barding, weapon abilities, cooldowns, target legality, and mastery legality. Prefer recover when party members are dead, critically injured, or no enemies are nearby and members need recovery; pull_to_choke_point for large crowded packs that should be split, even when backline members are pressured; protect_healer when healerUnderPressure in a small fight; break_pursuit when the fighter or backline needs to drop direct pursuit pressure; focus_fire when a hard or priority target should be burned down.\",");
            builder.Append("\"snapshot\":");
            builder.Append(snapshot == null ? "{}" : snapshot.ToJson());
            builder.Append("}");

            return builder.ToString();
        }

        public static AdventurePartyAIDecision ParseDecision(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return null;
            }

            AdventurePartyAIDecision decision = new AdventurePartyAIDecision();

            decision.Action = ParseOrder(ReadJsonString(text, "order"));
            decision.Target = ReadJsonString(text, "target");
            decision.Speech = ReadJsonString(text, "speech");
            decision.Reason = ReadJsonString(text, "reason");
            decision.Created = DateTime.UtcNow;

            NormalizeDecision(decision);

            if (decision.Action == AdventurePartyAIAction.None)
            {
                return null;
            }

            return decision;
        }

        private static AdventurePartyAIAction ParseOrder(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return AdventurePartyAIAction.None;
            }

            string normalized = value.Trim();

            if (String.Equals(normalized, "focus_fire", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "FocusFire", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.FocusFire;
            }

            if (String.Equals(normalized, "protect_healer", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "ProtectHealer", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.ProtectHealer;
            }

            if (String.Equals(normalized, "pull_to_choke_point", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "PullToChokePoint", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.PullToChokePoint;
            }

            if (String.Equals(normalized, "break_pursuit", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "BreakPursuit", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.BreakPursuit;
            }

            if (String.Equals(normalized, "recover", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Recover", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Recover;
            }

            return AdventurePartyAIAction.None;
        }

        public static AdventurePartyAIAction ParseAction(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return AdventurePartyAIAction.None;
            }

            string normalized = value.Trim();

            if (String.Equals(normalized, "Explore", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Explore;
            }

            if (String.Equals(normalized, "Engage", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Engage;
            }

            if (String.Equals(normalized, "FocusFire", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Focus_Fire", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "focus_fire", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.FocusFire;
            }

            if (String.Equals(normalized, "ProtectHealer", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Protect_Healer", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "protect_healer", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.ProtectHealer;
            }

            if (String.Equals(normalized, "Kite", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Kite;
            }

            if (String.Equals(normalized, "AvoidAOE", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Avoid_AOE", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "avoid_aoe", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.AvoidAOE;
            }

            if (String.Equals(normalized, "HoldChokePoint", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Hold_Choke_Point", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "hold_choke_point", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.HoldChokePoint;
            }

            if (String.Equals(normalized, "PullToChokePoint", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Pull_To_Choke_Point", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "pull_to_choke_point", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "PullToChoke", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "pull_to_choke", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Pull", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.PullToChokePoint;
            }

            if (String.Equals(normalized, "BreakPursuit", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Break_Pursuit", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "break_pursuit", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "DropPursuit", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "drop_pursuit", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.BreakPursuit;
            }

            if (String.Equals(normalized, "Recover", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Recovery", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "recover", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Recover;
            }

            if (String.Equals(normalized, "PowerUp", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Power_Up", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "power_up", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Burst", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "burst", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "PrepareBurst", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "prepare_burst", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "UseMastery", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "use_mastery", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.PowerUp;
            }

            if (String.Equals(normalized, "Rest", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Rest;
            }

            if (String.Equals(normalized, "Retreat", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Retreat;
            }

            if (String.Equals(normalized, "Avoid", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Avoid;
            }

            if (String.Equals(normalized, "HoldPosition", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Hold_Position", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "hold_position", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(normalized, "Hold", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.HoldPosition;
            }

            if (String.Equals(normalized, "Say", StringComparison.OrdinalIgnoreCase))
            {
                return AdventurePartyAIAction.Say;
            }

            return AdventurePartyAIAction.None;
        }

        public static string EscapeJson(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }

            StringBuilder builder = new StringBuilder(value.Length + 8);

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];

                switch (c)
                {
                    case '\\':
                        builder.Append("\\\\");
                        break;
                    case '"':
                        builder.Append("\\\"");
                        break;
                    case '\r':
                        builder.Append("\\r");
                        break;
                    case '\n':
                        builder.Append("\\n");
                        break;
                    case '\t':
                        builder.Append("\\t");
                        break;
                    default:
                        builder.Append(c);
                        break;
                }
            }

            return builder.ToString();
        }

        public static string BoolJson(bool value)
        {
            return value ? "true" : "false";
        }

        private static string ReadJsonString(string json, string name)
        {
            Match match = Regex.Match(
                json,
                "\"" + Regex.Escape(name) + "\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"",
                RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return "";
            }

            return UnescapeJson(match.Groups[1].Value);
        }

        private static string UnescapeJson(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return "";
            }

            return value
                .Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");
        }

        private static void NormalizeDecision(AdventurePartyAIDecision decision)
        {
            if (decision == null)
            {
                return;
            }

            if (decision.Created == DateTime.MinValue)
            {
                decision.Created = DateTime.UtcNow;
            }

            decision.Target = NormalizeShortText(decision.Target, 32);
            decision.Reason = NormalizeShortText(decision.Reason, 160);
            decision.Speech = NormalizeShortText(decision.Speech, AdventurePartyAIConfig.MaxSpeechLength);
        }

        private static string NormalizeShortText(string text, int maxLength)
        {
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            text = text.Replace('\r', ' ').Replace('\n', ' ').Trim();

            if (text.Length > maxLength)
            {
                text = text.Substring(0, maxLength);
            }

            return text;
        }
    }
}
