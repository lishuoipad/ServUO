using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Server.Commands;
using Server.Engines.AdvancedCombatAI;
using Server.Engines.AdventureParty;
using Server.Items;
using Server.Mobiles;
using Server.Spells.SkillMasteries;

namespace Server.Commands
{
    public static class AdventurePartyMonitorCommands
    {
        private static readonly object m_LogSyncRoot = new object();

        private const int ControllerSearchRange = 20;
        private const int EnemyScanRange = 18;
        private const int MemberThreatRange = 12;
        private const int EnemyLogLimit = 10;

        private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(1.0);
        private static readonly TimeSpan MaxInterval = TimeSpan.FromSeconds(30.0);

        private static Timer m_Timer;
        private static bool m_AllControllers;
        private static AdventurePartyController m_TargetController;
        private static TimeSpan m_Interval = TimeSpan.FromSeconds(2.0);

        public static void Initialize()
        {
            CommandSystem.Register("AdventurePartyMonitor", AccessLevel.GameMaster, new CommandEventHandler(AdventurePartyMonitor_OnCommand));
        }

        private static void AdventurePartyMonitor_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
            {
                return;
            }

            bool all = String.Equals(e.GetString(0), "all", StringComparison.OrdinalIgnoreCase);
            string action = all ? e.GetString(1) : e.GetString(0);
            int valueIndex = all ? 2 : 1;

            if (String.IsNullOrEmpty(action))
            {
                action = "status";
            }

            if (String.Equals(action, "on", StringComparison.OrdinalIgnoreCase))
            {
                List<AdventurePartyController> targets = FindControllers(from, all);

                if (targets.Count == 0)
                {
                    from.SendMessage(all ? "No adventure party controllers exist." : "No adventure party controller found nearby.");
                    SendUsage(from);
                    return;
                }

                int seconds = e.GetInt32(valueIndex);

                if (seconds > 0)
                {
                    SetInterval(TimeSpan.FromSeconds(seconds));
                }

                m_AllControllers = all;
                m_TargetController = all ? null : targets[0];

                StartTimer();
                WriteSnapshots(targets, "monitor-start");

                from.SendMessage(
                    "Adventure party monitor is now on for {0}. Interval={1:0.0}s. Log={2}",
                    all ? "all controllers" : "the nearest controller",
                    m_Interval.TotalSeconds,
                    GetLogPath());
                return;
            }

            if (String.Equals(action, "off", StringComparison.OrdinalIgnoreCase))
            {
                StopTimer();
                from.SendMessage("Adventure party monitor is now off.");
                return;
            }

            if (String.Equals(action, "once", StringComparison.OrdinalIgnoreCase) ||
                String.Equals(action, "check", StringComparison.OrdinalIgnoreCase))
            {
                List<AdventurePartyController> targets = FindControllers(from, all);

                if (targets.Count == 0)
                {
                    from.SendMessage(all ? "No adventure party controllers exist." : "No adventure party controller found nearby.");
                    return;
                }

                WriteSnapshots(targets, "manual-check");
                from.SendMessage("Adventure party monitor wrote {0} snapshot(s) to {1}.", targets.Count, GetLogPath());
                return;
            }

            if (String.Equals(action, "interval", StringComparison.OrdinalIgnoreCase))
            {
                int seconds = e.GetInt32(valueIndex);

                if (seconds <= 0)
                {
                    from.SendMessage("Current adventure party monitor interval is {0:0.0}s.", m_Interval.TotalSeconds);
                    return;
                }

                SetInterval(TimeSpan.FromSeconds(seconds));

                if (m_Timer != null)
                {
                    StartTimer();
                }

                from.SendMessage("Adventure party monitor interval set to {0:0.0}s.", m_Interval.TotalSeconds);
                return;
            }

            if (String.Equals(action, "status", StringComparison.OrdinalIgnoreCase))
            {
                from.SendMessage(
                    "Adventure party monitor: enabled={0}, scope={1}, interval={2:0.0}s, log={3}",
                    m_Timer != null,
                    m_Timer == null ? "none" : m_AllControllers ? "all" : FormatController(m_TargetController),
                    m_Interval.TotalSeconds,
                    GetLogPath());
                return;
            }

            SendUsage(from);
        }

        private static void SendUsage(Mobile from)
        {
            if (from == null)
            {
                return;
            }

            from.SendMessage("Usage: [AdventurePartyMonitor on [seconds]");
            from.SendMessage("Usage: [AdventurePartyMonitor all on [seconds]");
            from.SendMessage("Usage: [AdventurePartyMonitor off|status|once|interval [seconds]");
            from.SendMessage("Usage: [AdventurePartyMonitor all once");
        }

        private static void SetInterval(TimeSpan value)
        {
            if (value < MinInterval)
            {
                value = MinInterval;
            }
            else if (value > MaxInterval)
            {
                value = MaxInterval;
            }

            m_Interval = value;
        }

        private static void StartTimer()
        {
            StopTimer();

            m_Timer = new AdventurePartyMonitorTimer();
            m_Timer.Start();
        }

        private static void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private static List<AdventurePartyController> FindControllers(Mobile from, bool all)
        {
            List<AdventurePartyController> list = new List<AdventurePartyController>();

            if (all)
            {
                foreach (Item item in World.Items.Values)
                {
                    AdventurePartyController controller = item as AdventurePartyController;

                    if (controller != null && !controller.Deleted)
                    {
                        list.Add(controller);
                    }
                }

                SortControllersBySerial(list);
                return list;
            }

            if (from == null || from.Map == null || from.Map == Map.Internal)
            {
                return list;
            }

            AdventurePartyController best = null;
            double bestDistance = ControllerSearchRange;

            foreach (Item item in World.Items.Values)
            {
                AdventurePartyController controller = item as AdventurePartyController;

                if (controller == null || controller.Deleted || controller.Map != from.Map)
                {
                    continue;
                }

                double distance = from.GetDistanceToSqrt(controller.Location);

                if (distance <= bestDistance)
                {
                    best = controller;
                    bestDistance = distance;
                }
            }

            if (best != null)
            {
                list.Add(best);
            }

            return list;
        }

        private static List<AdventurePartyController> FindMonitoredControllers()
        {
            List<AdventurePartyController> list = new List<AdventurePartyController>();

            if (m_AllControllers)
            {
                foreach (Item item in World.Items.Values)
                {
                    AdventurePartyController controller = item as AdventurePartyController;

                    if (controller != null && !controller.Deleted)
                    {
                        list.Add(controller);
                    }
                }

                SortControllersBySerial(list);
                return list;
            }

            if (m_TargetController != null && !m_TargetController.Deleted)
            {
                list.Add(m_TargetController);
            }

            return list;
        }

        private static void SortControllersBySerial(List<AdventurePartyController> list)
        {
            list.Sort(
                delegate(AdventurePartyController left, AdventurePartyController right)
                {
                    return left.Serial.Value.CompareTo(right.Serial.Value);
                });
        }

        private static void WriteTimerSnapshot()
        {
            List<AdventurePartyController> targets = FindMonitoredControllers();

            if (targets.Count == 0)
            {
                if (!m_AllControllers)
                {
                    WriteRawLine("monitor-stop: target controller no longer exists.");
                    StopTimer();
                }

                return;
            }

            WriteSnapshots(targets, "timer");
        }

        private static void WriteSnapshots(List<AdventurePartyController> controllers, string reason)
        {
            if (controllers == null || controllers.Count == 0)
            {
                return;
            }

            try
            {
                lock (m_LogSyncRoot)
                {
                    string path = GetLogPath();
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (StreamWriter writer = new StreamWriter(path, true))
                    {
                        for (int i = 0; i < controllers.Count; i++)
                        {
                            AdventurePartyController controller = controllers[i];

                            if (controller != null && !controller.Deleted)
                            {
                                writer.Write(BuildSnapshot(controller, reason));
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[AdventurePartyMonitor] Failed to write monitor log: {0}", ex.Message);
            }
        }

        private static void WriteRawLine(string text)
        {
            try
            {
                lock (m_LogSyncRoot)
                {
                    string path = GetLogPath();
                    Directory.CreateDirectory(Path.GetDirectoryName(path));

                    using (StreamWriter writer = new StreamWriter(path, true))
                    {
                        writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss.fff} {1}", DateTime.Now, text);
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetLogPath()
        {
            string path = Path.Combine(
                "Logs",
                "AdventurePartyMonitor",
                String.Format("state-{0:yyyy-MM-dd}.log", DateTime.Now));

            return path;
        }

        private static string BuildSnapshot(AdventurePartyController controller, string reason)
        {
            DateTime now = DateTime.UtcNow;
            List<BaseAdventurer> members = GetMembers(controller);
            BaseAdventurer leader = FindLeader(members);
            List<Mobile> enemies = GetNearbyEnemies(controller, leader, members);

            int living = 0;
            int ghosts = 0;
            int injured = 0;
            int critical = 0;

            for (int i = 0; i < members.Count; i++)
            {
                BaseAdventurer member = members[i];

                if (IsLivingMember(member))
                {
                    living++;

                    if (member.HitsMax > 0 && member.Hits < member.HitsMax)
                    {
                        injured++;
                    }

                    if (member.HitsMax > 0 && member.Hits <= member.HitsMax * 35 / 100)
                    {
                        critical++;
                    }
                }
                else if (member != null && !member.Deleted && member.IsDeadPet)
                {
                    ghosts++;
                }
            }

            int enemiesTargetingParty = CountEnemiesTargetingParty(enemies, members);
            StringBuilder builder = new StringBuilder(2048);

            builder.AppendLine();
            builder.AppendFormat(
                "=== {0:yyyy-MM-dd HH:mm:ss.fff} reason={1} controller={2} ===",
                DateTime.Now,
                reason,
                FormatController(controller));
            builder.AppendLine();

            builder.AppendFormat(
                "controller map={0} loc={1} state={2} tactic={3} tacticTarget=\"{4}\" tacticExpiresIn={5} aiEnabled={6} provider={7} pending={8} requests={9} decisions={10} failures={11}",
                FormatMap(controller.Map),
                FormatPoint(controller.Location),
                controller.State,
                controller.CurrentTactic,
                Clean(controller.TacticTarget),
                FormatSeconds(controller.TacticExpires, now),
                controller.AIEnabled,
                controller.AIProvider,
                controller.AIRequestPending,
                controller.AIRequests,
                controller.AIDecisions,
                controller.AIFailures);
            builder.AppendLine();

            builder.AppendFormat(
                "aiStatus=\"{0}\" members={1} living={2} ghosts={3} injured={4} critical={5} enemiesIn{6}={7} enemiesTargetingParty={8}",
                Clean(controller.LastAIStatus),
                members.Count,
                living,
                ghosts,
                injured,
                critical,
                EnemyScanRange,
                enemies.Count,
                enemiesTargetingParty);
            builder.AppendLine();

            for (int i = 0; i < members.Count; i++)
            {
                AppendMember(builder, members[i], leader, enemies, now);
            }

            AppendHealerDecision(builder, controller);

            if (enemies.Count == 0)
            {
                builder.AppendLine("enemies: none");
            }
            else
            {
                SortEnemiesByDistance(enemies, leader == null ? controller.Location : leader.Location);

                int count = Math.Min(enemies.Count, EnemyLogLimit);

                for (int i = 0; i < count; i++)
                {
                    AppendEnemy(builder, enemies[i], controller, leader, i + 1);
                }

                if (enemies.Count > count)
                {
                    builder.AppendFormat("enemies: {0} more not listed", enemies.Count - count);
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static void AppendHealerDecision(StringBuilder builder, AdventurePartyController controller)
        {
            if (builder == null || controller == null || controller.Deleted)
            {
                return;
            }

            List<string> lines = controller.GetHealerMonitorLines();

            if (lines == null || lines.Count == 0)
            {
                return;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                builder.AppendFormat("healerDecision#{0} \"{1}\"", i + 1, Clean(lines[i]));
                builder.AppendLine();
            }
        }

        private static void AppendMember(
            StringBuilder builder,
            BaseAdventurer member,
            BaseAdventurer leader,
            List<Mobile> enemies,
            DateTime now)
        {
            if (member == null)
            {
                return;
            }

            Mobile combatant = member.Combatant as Mobile;
            int nearbyEnemies = CountEnemiesNear(member, enemies, MemberThreatRange);
            int directThreats = CountEnemiesTargetingMember(enemies, member);
            string brain = AdvancedCombatBrain.GetCombatDebugState(member);

            builder.AppendFormat(
                "member role={0} name=\"{1}\" serial={2} status={3} loc={4} hp={5}/{6}({7}%) mana={8}/{9} stam={10}/{11} distLeader={12} nextAction={13} flags={14} combatant={15} combatantDist={16} nearbyEnemies={17} directThreats={18} action=\"{19}\" spell=\"{20}\" mastery=\"{21}\" brain=\"{22}\"",
                member.Role,
                Clean(member.Name),
                member.Serial,
                GetMemberStatus(member),
                FormatPoint(member.Location),
                member.Hits,
                member.HitsMax,
                Percent(member.Hits, member.HitsMax),
                member.Mana,
                member.ManaMax,
                member.Stam,
                member.StamMax,
                leader == null || leader == member ? "0.0" : FormatDistance(member.Location, leader.Location),
                FormatNextAction(member.NextPartyAction, now),
                GetFlags(member),
                FormatMobile(combatant),
                combatant == null || combatant.Map != member.Map ? "n/a" : FormatDistance(member.Location, combatant.Location),
                nearbyEnemies,
                directThreats,
                GetBrainField(brain, "lastAction"),
                GetSpellState(member, brain),
                GetMasteryState(member, brain),
                Clean(brain));
            builder.AppendLine();
        }

        private static void AppendEnemy(
            StringBuilder builder,
            Mobile enemy,
            AdventurePartyController controller,
            BaseAdventurer leader,
            int index)
        {
            Mobile target = enemy == null ? null : enemy.Combatant as Mobile;
            BaseAdventurer targetMember = target as BaseAdventurer;
            bool targetingParty = targetMember != null && targetMember.Controller == controller;
            string targetRole = targetingParty ? targetMember.Role.ToString() : "none";
            string lineOfSight = leader != null && enemy != null && leader.Map == enemy.Map ? leader.InLOS(enemy).ToString() : "n/a";

            builder.AppendFormat(
                "enemy#{0} type={1} name=\"{2}\" serial={3} loc={4} hp={5}/{6}({7}%) distLeader={8} target={9} targetRole={10} targetingParty={11} losLeader={12}",
                index,
                enemy == null ? "null" : enemy.GetType().Name,
                enemy == null ? "" : Clean(enemy.Name),
                enemy == null ? Serial.Zero : enemy.Serial,
                enemy == null ? "n/a" : FormatPoint(enemy.Location),
                enemy == null ? 0 : enemy.Hits,
                enemy == null ? 0 : enemy.HitsMax,
                enemy == null ? 0 : Percent(enemy.Hits, enemy.HitsMax),
                leader == null || enemy == null || enemy.Map != leader.Map ? "n/a" : FormatDistance(leader.Location, enemy.Location),
                FormatMobile(target),
                targetRole,
                targetingParty,
                lineOfSight);
            builder.AppendLine();
        }

        private static List<BaseAdventurer> GetMembers(AdventurePartyController controller)
        {
            List<BaseAdventurer> members = new List<BaseAdventurer>();

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseAdventurer member = mobile as BaseAdventurer;

                if (member != null && !member.Deleted && member.Controller == controller)
                {
                    members.Add(member);
                }
            }

            members.Sort(
                delegate(BaseAdventurer left, BaseAdventurer right)
                {
                    int role = left.Role.CompareTo(right.Role);

                    if (role != 0)
                    {
                        return role;
                    }

                    return left.Serial.Value.CompareTo(right.Serial.Value);
                });

            return members;
        }

        private static BaseAdventurer FindLeader(List<BaseAdventurer> members)
        {
            BaseAdventurer firstLiving = null;

            for (int i = 0; i < members.Count; i++)
            {
                BaseAdventurer member = members[i];

                if (!IsLivingMember(member))
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Fighter)
                {
                    return member;
                }

                if (firstLiving == null)
                {
                    firstLiving = member;
                }
            }

            return firstLiving;
        }

        private static List<Mobile> GetNearbyEnemies(AdventurePartyController controller, BaseAdventurer leader, List<BaseAdventurer> members)
        {
            List<Mobile> enemies = new List<Mobile>();

            if (controller == null || controller.Deleted)
            {
                return enemies;
            }

            Map map = leader != null && leader.Map != null && leader.Map != Map.Internal ? leader.Map : controller.Map;
            Point3D center = leader != null && leader.Map == map ? leader.Location : controller.Location;

            if (map == null || map == Map.Internal)
            {
                return enemies;
            }

            IPooledEnumerable eable = map.GetMobilesInRange(center, EnemyScanRange);

            foreach (Mobile mobile in eable)
            {
                if (IsMonitorEnemy(mobile))
                {
                    enemies.Add(mobile);
                }
            }

            eable.Free();
            return enemies;
        }

        private static bool IsMonitorEnemy(Mobile mobile)
        {
            if (mobile == null || mobile.Deleted || !mobile.Alive || mobile.Map == null || mobile.Map == Map.Internal)
            {
                return false;
            }

            if (mobile.Player || mobile is PlayerMobile)
            {
                return false;
            }

            BaseAdventurer adventurer = mobile as BaseAdventurer;

            if (adventurer != null)
            {
                return false;
            }

            BaseCreature creature = mobile as BaseCreature;

            if (creature != null && (creature.Controlled || creature.Summoned || creature.IsDeadPet))
            {
                return false;
            }

            return true;
        }

        private static bool IsLivingMember(BaseAdventurer member)
        {
            return member != null && !member.Deleted && member.Alive && !member.IsDeadPet && member.Map != null && member.Map != Map.Internal;
        }

        private static int CountEnemiesTargetingParty(List<Mobile> enemies, List<BaseAdventurer> members)
        {
            int count = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile combatant = enemies[i].Combatant as Mobile;

                for (int j = 0; j < members.Count; j++)
                {
                    if (combatant == members[j])
                    {
                        count++;
                        break;
                    }
                }
            }

            return count;
        }

        private static int CountEnemiesTargetingMember(List<Mobile> enemies, BaseAdventurer member)
        {
            int count = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Combatant == member)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnemiesNear(BaseAdventurer member, List<Mobile> enemies, int range)
        {
            int count = 0;

            if (member == null || member.Map == null)
            {
                return 0;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy != null && enemy.Map == member.Map && GetDistance(member.Location, enemy.Location) <= range)
                {
                    count++;
                }
            }

            return count;
        }

        private static void SortEnemiesByDistance(List<Mobile> enemies, Point3D center)
        {
            enemies.Sort(
                delegate(Mobile left, Mobile right)
                {
                    return GetDistance(left.Location, center).CompareTo(GetDistance(right.Location, center));
                });
        }

        private static string GetMemberStatus(BaseAdventurer member)
        {
            if (member == null || member.Deleted)
            {
                return "deleted";
            }

            if (member.IsDeadPet)
            {
                return "ghost";
            }

            return member.Alive ? "alive" : "dead";
        }

        private static string GetFlags(BaseAdventurer member)
        {
            List<string> flags = new List<string>();

            if (member.Hidden)
            {
                flags.Add("hidden");
            }

            if (member.Poisoned)
            {
                flags.Add("poisoned");
            }

            if (member.Paralyzed)
            {
                flags.Add("paralyzed");
            }

            if (member.Frozen)
            {
                flags.Add("frozen");
            }

            if (member.Warmode)
            {
                flags.Add("warmode");
            }

            return flags.Count == 0 ? "none" : String.Join(",", flags.ToArray());
        }

        private static string GetMasteryState(BaseAdventurer member, string brain)
        {
            List<string> flags = new List<string>();

            if (AdvancedCombatBrain.IsChannelingDeathRay(member))
            {
                flags.Add("DeathRay");
            }

            if (AdvancedCombatBrain.IsPlayingTheOddsActive(member))
            {
                flags.Add("PlayingTheOdds");
            }

            if (SkillMasterySpell.GetSpell(member, typeof(ManaShieldSpell)) != null)
            {
                flags.Add("ManaShield");
            }

            string active = flags.Count == 0 ? "none" : String.Join(",", flags.ToArray());
            string last = GetBrainField(brain, "lastMastery");

            return String.Format("active={0};last={1}", active, last);
        }

        private static string GetSpellState(BaseAdventurer member, string brain)
        {
            if (member != null && member.Spell != null)
            {
                return "casting:" + member.Spell.GetType().Name;
            }

            return GetBrainField(brain, "lastSpell");
        }

        private static string GetBrainField(string brain, string key)
        {
            if (String.IsNullOrEmpty(brain) || String.IsNullOrEmpty(key))
            {
                return "none";
            }

            string prefix = key + "=";
            int start = brain.IndexOf(prefix, StringComparison.Ordinal);

            if (start < 0)
            {
                return "none";
            }

            start += prefix.Length;
            int end = brain.IndexOf(';', start);
            string value = end < 0 ? brain.Substring(start) : brain.Substring(start, end - start);

            return String.IsNullOrEmpty(value) ? "none" : Clean(value);
        }

        private static string FormatController(AdventurePartyController controller)
        {
            if (controller == null || controller.Deleted)
            {
                return "none";
            }

            return String.Format("{0}@{1}", controller.Serial, FormatPoint(controller.Location));
        }

        private static string FormatMobile(Mobile mobile)
        {
            if (mobile == null || mobile.Deleted)
            {
                return "none";
            }

            return String.Format("{0}:{1}[{2}]", mobile.GetType().Name, Clean(mobile.Name), mobile.Serial);
        }

        private static string FormatMap(Map map)
        {
            return map == null ? "null" : map.Name;
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("({0},{1},{2})", point.X, point.Y, point.Z);
        }

        private static string FormatDistance(Point3D left, Point3D right)
        {
            return GetDistance(left, right).ToString("0.0");
        }

        private static double GetDistance(Point3D left, Point3D right)
        {
            int x = left.X - right.X;
            int y = left.Y - right.Y;

            return Math.Sqrt((x * x) + (y * y));
        }

        private static int Percent(int value, int max)
        {
            if (max <= 0)
            {
                return 0;
            }

            return Math.Max(0, Math.Min(100, (value * 100) / max));
        }

        private static string FormatSeconds(DateTime time, DateTime now)
        {
            if (time == DateTime.MinValue)
            {
                return "none";
            }

            double seconds = (time - now).TotalSeconds;

            if (seconds <= 0.0)
            {
                return "expired";
            }

            return seconds.ToString("0.0") + "s";
        }

        private static string FormatNextAction(DateTime time, DateTime now)
        {
            if (time == DateTime.MinValue || time <= now)
            {
                return "ready";
            }

            return (time - now).TotalSeconds.ToString("0.0") + "s";
        }

        private static string Clean(string text)
        {
            if (String.IsNullOrEmpty(text))
            {
                return "";
            }

            return text.Replace("\r", " ").Replace("\n", " ").Replace("\"", "'");
        }

        private class AdventurePartyMonitorTimer : Timer
        {
            public AdventurePartyMonitorTimer()
                : base(TimeSpan.Zero, m_Interval)
            {
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                AdventurePartyMonitorCommands.WriteTimerSnapshot();
            }
        }
    }
}
