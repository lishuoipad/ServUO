using System;
using System.Collections.Generic;
using System.IO;
using Server.Commands;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Spells.Sixth;

namespace Server.Engines.AdventureParty
{
    public enum AdventurePartyRole
    {
        Fighter,
        Archer,
        Mage,
        Healer
    }

    public enum AdventurePartyState
    {
        Exploring,
        Engaging,
        Resting,
        Retreating,
        Wiped
    }

    public enum AdventurePartyTactic
    {
        Standard,
        FocusFire,
        ProtectHealer,
        Kite,
        AvoidAOE,
        HoldChokePoint,
        PowerUp,
        PullToChokePoint,
        Recovering
    }

    public static class AdventurePartySettings
    {
        public const int Team = 9401;
        public const int PerceptionRange = 12;
        public const int ExplorationFormationMaxDistance = 6;
        public const int ExplorationFormationRegroupDistance = 8;
        public const int ExplorationRegroupRunSteps = 2;
        public const int BacklineThreatRange = 4;
        public const int BacklineMeleeThreatRange = 2;
        public const int BacklineThreatSafeRange = 8;
        public const int BacklineOrbitFighterRange = 7;
        public const int BacklineOrbitFighterMaxRange = 11;
        public const int BacklineEvasiveMoveSteps = 3;
        public const int FighterFastEngageDistance = 4;
        public const int FighterFastEngageSteps = 2;
        public const int FleeingTargetFinishHitPercent = 25;
        public const int FleeingTargetFinishSafeRange = 18;
        public const int FleeingTargetFinishMaxDistance = 36;
        public const int FleeingTargetFinishMoveSteps = 4;
        public const int IsolatedBacklineInterceptSafeRange = 18;
        public const int IsolatedBacklineInterceptPackScanRange = 24;
        public const int IsolatedBacklineInterceptMaxDistance = 40;
        public const int IsolatedBacklineInterceptMoveSteps = 4;
        public const int FighterPressureBreakPursuitRunSteps = 3;
        public const int PullHardResetRunSteps = 4;
        public const int MovePulseDelayMilliseconds = 200;
        public const int FighterPressureBreakPursuitDirectThreats = 2;
        public const int FighterPressureBreakPursuitHitPercent = 60;
        public const int HealerBandageApproachRange = 12;
        public const int HealerLowManaBandageMana = 25;
        public const int HealerCriticalBandageHitPercent = 50;
        public const int PullChokeKillZoneRange = 8;
        public const int PullerMaxDistanceFromAnchor = 12;
        public const int PullerDangerRange = 5;
        public const int PullAnchorSearchRadius = 18;
        public const int PullAnchorMaxLeaderDistance = 22;
        public const int PullAnchorEnemyScanRange = 24;
        public const int PullAnchorLocalEnemyRange = 8;
        public const int PullAnchorPreferredEnemyDistance = 14;
        public const int PullAnchorMinimumEnemyDistance = 9;
        public const int PullAnchorWaypointMaxDistance = 26;
        public const int PullBreakPursuitEnemyCount = 3;
        public const int PullBreakPursuitScanRange = 10;
        public const int PullBreakPursuitBackDistance = 6;
        public const int PullBreakPursuitHidingMinimum = 80;
        public const int PullBreakPursuitMageryMinimum = 75;
        public const int PullHardResetEnemyCount = 4;
        public const int PullHardResetDirectPartyTargets = 3;
        public const int PullHardResetBacklineTargets = 1;
        public const int PullHardResetRangedThreats = 2;
        public const int PullRangedThreatScanRange = 16;
        public const int RangedDisengageThreats = 2;
        public const int RangedDisengageScanRange = 32;
        public const int RangedDisengageBreakDistance = 42;
        public const int RangedDisengageCornerMinDistance = 8;
        public const int RangedDisengageCornerMaxDistance = 24;
        public const int RangedDisengageRunSteps = 7;
        public const int RangedDisengageClearRange = 28;
        public const int RangedDisengageCrowdingRange = 12;
        public const int RangedDisengageCounterAttackRange = 32;
        public const int RangedDisengageRoomCounterAttackRange = 24;
        public const int RangedDisengageWeakPackCounterAttackMaxEnemies = 4;
        public const int RangedDisengageWeakPackCounterAttackHitPercent = 70;
        public const int LocalWeakPackCounterAttackMaxEnemies = 4;
        public const int LocalWeakPackCounterAttackHitPercent = 70;
        public const int RangedDisengageRouteThreatRange = 5;
        public const int RangedDisengageFormationMaxDistance = 8;
        public const int RangedDisengageFormationRegroupDistance = 12;
        public const int NoResurrectorDisengageScanRange = 36;
        public const int RangedDisengageStuckMoveDistance = 3;
        public const int RangedDisengageStuckThreatRange = 6;
        public const int RangedDisengageBadPointRange = 14;
        public const int DragonBreathObserveRange = 18;
        public const int DragonBreathBurstWindowRange = 32;
        public const int DragonBreathBurstMinimumDamage = 35;
        public const int DragonBreathBurstMinimumHitPercent = 20;
        public const int PullResetLocalEnemyRange = 5;
        public const int PullResetTargetNeighborRange = 6;
        public const int PullResetPreferredMaxNeighbors = 1;
        public const int PullResetFallbackMaxNeighbors = 2;
        public const int ResurrectionClearRange = 6;
        public const int ResurrectionDangerRange = 4;
        public const int ResurrectionPullDistance = 10;
        public const int ResurrectionAnchorSearchRadius = 16;
        public const int ResurrectionGhostFollowRange = 1;
        public const int ResurrectionGhostDangerRange = 8;
        public const int ResurrectionGhostRunSteps = 3;
        public const int ResurrectionRangedThreatScanRange = 16;
        public const int ResurrectionRangedThreatBreakDistance = 14;
        public const int ResurrectionRallyUnsafeEnemyLimit = 2;
        public const int ResurrectionRallyCriticalEnemyLimit = 4;
        public const int PostResurrectionRecoveryHitPercent = 70;
        public const int RecoveryHardRegroupDistance = 12;
        public const int RecoveryRegroupRunSteps = 3;
        public static readonly TimeSpan WipeObservationTime = TimeSpan.FromMinutes(5.0);
        public static readonly TimeSpan TacticDuration = TimeSpan.FromSeconds(24.0);
        public static readonly TimeSpan ControllerInitialDelay = TimeSpan.FromSeconds(0.25);
        public static readonly TimeSpan ControllerThinkInterval = TimeSpan.FromSeconds(0.5);
        public static readonly TimeSpan PullBreakPursuitAttemptDelay = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan PullBreakPursuitPendingTargetTime = TimeSpan.FromSeconds(5.0);
        public static readonly TimeSpan PullResetSettleTime = TimeSpan.FromSeconds(4.0);
        public static readonly TimeSpan PullResetMaxWaitTime = TimeSpan.FromSeconds(12.0);
        public static readonly TimeSpan RangedDisengageMinimumTime = TimeSpan.FromSeconds(8.0);
        public static readonly TimeSpan RangedDisengageMaximumTime = TimeSpan.FromSeconds(24.0);
        public static readonly TimeSpan RangedDisengageSettleTime = TimeSpan.FromSeconds(5.0);
        public static readonly TimeSpan RangedDisengageStuckCheckTime = TimeSpan.FromSeconds(2.0);
        public static readonly TimeSpan RangedDisengageBadPointTime = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan DragonBreathBurstWindow = TimeSpan.FromSeconds(9.0);
        public static readonly TimeSpan ResurrectionRallyHoldTime = TimeSpan.FromSeconds(20.0);
        public static readonly TimeSpan ResurrectionRallyMinimumHoldTime = TimeSpan.FromSeconds(10.0);
        public static readonly TimeSpan PostResurrectionRecoveryTime = TimeSpan.FromSeconds(10.0);
    }

    public static class AdventurePartyDebug
    {
        private static readonly object m_LogSyncRoot = new object();

        public static bool MasteryDebugEnabled;
        public static bool HealerDebugEnabled;

        public static void Mastery(BaseCreature actor, string message)
        {
            if (!MasteryDebugEnabled || String.IsNullOrEmpty(message))
            {
                return;
            }

            Write(actor, message, true, false);
        }

        public static void Healer(BaseCreature actor, string message)
        {
            if (!HealerDebugEnabled || String.IsNullOrEmpty(message))
            {
                return;
            }

            Write(actor, message, true, true);
        }

        public static void HealerCheck(BaseCreature actor, string message)
        {
            if (String.IsNullOrEmpty(message))
            {
                return;
            }

            Write(actor, message, false, true);
        }

        private static void Write(BaseCreature actor, string message, bool sendToGameMasters, bool writeHealerLog)
        {
            string name = actor == null || String.IsNullOrEmpty(actor.Name) ? "AdventureParty" : actor.Name;
            string text = String.Format("{0}: {1}", name, message);

            Console.WriteLine("[AdventurePartyDebug] " + text);

            if (sendToGameMasters)
            {
                foreach (Mobile mobile in World.Mobiles.Values)
                {
                    if (mobile != null && !mobile.Deleted && mobile.NetState != null && mobile.AccessLevel >= AccessLevel.GameMaster)
                    {
                        mobile.SendMessage("[AdventurePartyDebug] " + text);
                    }
                }
            }

            if (writeHealerLog)
            {
                WriteHealerLog(text);
            }
        }

        private static void WriteHealerLog(string text)
        {
            try
            {
                lock (m_LogSyncRoot)
                {
                    string directory = Path.Combine("Logs", "AdventurePartyDebug");
                    Directory.CreateDirectory(directory);

                    using (StreamWriter writer = new StreamWriter(Path.Combine(directory, "healer.log"), true))
                    {
                        writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} {1}", DateTime.Now, text);
                    }
                }
            }
            catch
            {
            }
        }
    }
}

namespace Server.Items
{
    using Server.Engines.AdvancedCombatAI;
    using Server.Engines.AdventureParty;

    public class AdventurePartyController : Item
    {
        private readonly object m_AISyncRoot = new object();
        private readonly List<BaseAdventurer> m_Members;
        private readonly Dictionary<Serial, DateTime> m_MovePulseUntil = new Dictionary<Serial, DateTime>();
        private readonly Dictionary<Serial, int> m_LastObservedHits = new Dictionary<Serial, int>();
        private Point3D[] m_Waypoints;
        private int m_WaypointIndex;
        private AdventurePartyState m_State;
        private Timer m_Timer;
        private int m_StateTicks;
        private int m_StuckTicks;
        private Point3D m_LastLeaderLocation;
        private DateTime m_DeleteAfterWipe;
        private bool m_Deleting;
        private bool m_AIEnabled;
        private AdventurePartyAIProviderMode m_AIProviderMode;
        private bool m_AIRequestPending;
        private DateTime m_NextAIRequest;
        private DateTime m_AIFailureCooldownUntil;
        private AdventurePartyAIDecision m_PendingAIDecision;
        private string m_LastAIStatus;
        private int m_AIRequests;
        private int m_AIDecisions;
        private int m_AIFailures;
        private AdventurePartyTactic m_CurrentTactic;
        private DateTime m_TacticExpires;
        private string m_TacticTarget;
        private Point3D m_TacticPoint;
        private string m_LastPullAnchorStatus;
        private Serial m_BreakPursuitPendingCaster;
        private Serial m_BreakPursuitPendingTarget;
        private DateTime m_BreakPursuitPendingUntil;
        private Serial m_BreakPursuitSupportCaster;
        private DateTime m_BreakPursuitSupportUntil;
        private string m_LastBreakPursuitStatus;
        private bool m_ResetPullActive;
        private Serial m_ResetPullOriginalTarget;
        private DateTime m_ResetPullStarted;
        private DateTime m_ResetPullSettleUntil;
        private bool m_RangedDisengageActive;
        private Point3D m_RangedDisengagePoint;
        private DateTime m_RangedDisengageStarted;
        private DateTime m_RangedDisengageUntil;
        private DateTime m_RangedDisengageSettleUntil;
        private Point3D m_RangedDisengageProgressAnchor;
        private DateTime m_RangedDisengageProgressChecked;
        private int m_RangedDisengageStuckTicks;
        private Point3D m_RangedDisengageBadPoint;
        private DateTime m_RangedDisengageBadPointUntil;
        private int m_RangedDisengageTurnBias;
        private Serial m_LastDragonBreathThreat;
        private DateTime m_LastDragonBreathObserved;
        private Serial m_LastDragonBreathCounterThreat;
        private DateTime m_LastDragonBreathCounterObserved;
        private Point3D m_ResurrectionRallyPoint;
        private DateTime m_ResurrectionRallyCreated;
        private DateTime m_ResurrectionRallyExpires;
        private Serial m_ResurrectionTarget;
        private Serial m_PostResurrectionMember;
        private DateTime m_PostResurrectionRecoveryUntil;
        private DateTime m_NextHealerDebug;
        private DateTime m_LastHealerSupportActionTime;
        private string m_LastHealerSupportAction;

        [CommandProperty(AccessLevel.GameMaster)]
        public AdventurePartyState State
        {
            get { return m_State; }
            set
            {
                if (m_State != value)
                {
                    m_State = value;
                    m_StateTicks = 0;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int MemberCount
        {
            get { return m_Members.Count; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int WaypointIndex
        {
            get { return m_WaypointIndex; }
            set
            {
                if (m_Waypoints == null || m_Waypoints.Length == 0)
                {
                    m_WaypointIndex = 0;
                }
                else
                {
                    m_WaypointIndex = Math.Max(0, Math.Min(value, m_Waypoints.Length - 1));
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime DeleteAfterWipe
        {
            get { return m_DeleteAfterWipe; }
            set { m_DeleteAfterWipe = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AIEnabled
        {
            get { return m_AIEnabled; }
            set
            {
                m_AIEnabled = value;
                m_LastAIStatus = value ? "AI enabled." : "AI disabled.";
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AdventurePartyAIProviderMode AIProvider
        {
            get { return m_AIProviderMode; }
            set { m_AIProviderMode = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public bool AIRequestPending
        {
            get
            {
                lock (m_AISyncRoot)
                {
                    return m_AIRequestPending;
                }
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string LastAIStatus
        {
            get { return m_LastAIStatus; }
            set { m_LastAIStatus = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AIRequests
        {
            get { return m_AIRequests; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AIDecisions
        {
            get { return m_AIDecisions; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public int AIFailures
        {
            get { return m_AIFailures; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AdventurePartyTactic CurrentTactic
        {
            get { return m_CurrentTactic; }
            set { m_CurrentTactic = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public DateTime TacticExpires
        {
            get { return m_TacticExpires; }
            set { m_TacticExpires = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string TacticTarget
        {
            get { return m_TacticTarget; }
            set { m_TacticTarget = value; }
        }

        [Constructable]
        public AdventurePartyController()
            : base(0x1F14)
        {
            Name = "adventure party controller";
            Movable = false;
            Visible = false;

            m_Members = new List<BaseAdventurer>();
            m_Waypoints = new Point3D[0];
            m_State = AdventurePartyState.Exploring;
            m_DeleteAfterWipe = DateTime.MinValue;
            m_AIEnabled = AdventurePartyAIConfig.EnabledByDefault;
            m_AIProviderMode = AdventurePartyAIConfig.DefaultProvider;
            m_LastAIStatus = m_AIEnabled ? "AI enabled." : "AI disabled.";
            ClearTactic();
        }

        public AdventurePartyController(Point3D start, Map map)
            : this()
        {
            MoveToWorld(start, map);
            BuildDefaultRoute(start);
        }

        public AdventurePartyController(Serial serial)
            : base(serial)
        {
            m_Members = new List<BaseAdventurer>();
        }

        public void BuildDefaultRoute(Point3D start)
        {
            m_Waypoints = new Point3D[]
            {
                start,
                new Point3D(start.X + 8, start.Y, start.Z),
                new Point3D(start.X + 8, start.Y + 8, start.Z),
                new Point3D(start.X, start.Y + 8, start.Z),
                new Point3D(start.X - 6, start.Y + 3, start.Z)
            };

            m_WaypointIndex = 0;
            m_LastLeaderLocation = start;
        }

        public void SpawnParty()
        {
            DeleteMembers();

            if (Map == null || Map == Map.Internal)
            {
                return;
            }

            m_DeleteAfterWipe = DateTime.MinValue;
            State = AdventurePartyState.Exploring;

            int primaryHue;
            int secondaryHue;
            int accentHue;

            BaseAdventurer.ChoosePartyAppearanceHues(out primaryHue, out secondaryHue, out accentHue);

            AddMember(new AdventurerFighter(), 0, 0, primaryHue, secondaryHue, accentHue);
            AddMember(new AdventurerArcher(), -1, 1, primaryHue, secondaryHue, accentHue);
            AddMember(new AdventurerMage(), 1, 1, primaryHue, secondaryHue, accentHue);
            AddMember(new AdventurerHealer(), 0, 2, primaryHue, secondaryHue, accentHue);

            PartyMessage("Stay sharp. We move together.");
            StartTimer();
        }

        public void AddExistingMember(BaseAdventurer member)
        {
            if (member == null || member.Deleted || m_Members.Contains(member))
            {
                return;
            }

            member.Controller = this;
            member.Team = AdventurePartySettings.Team;
            member.IsBonded = true;
            member.Home = Location;
            member.RangeHome = 30;
            member.SeeksHome = false;
            member.ValidateAdventureEquipment();
            m_Members.Add(member);
        }

        public void RemoveMember(BaseAdventurer member)
        {
            if (m_Deleting || member == null)
            {
                return;
            }

            m_Members.Remove(member);
        }

        public AdvancedCombatContext GetAdvancedCombatContext(BaseAdventurer member)
        {
            ClearExpiredTactic();

            Mobile fallback = member == null ? null : member.Combatant as Mobile;
            Mobile target = ResolveTacticTarget(fallback);

            return new AdvancedCombatContext(m_State, m_CurrentTactic, target, m_TacticPoint, m_TacticExpires);
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from == null || from.AccessLevel < AccessLevel.GameMaster)
            {
                return;
            }

            from.SendMessage(
                "Adventure party: {0} living member(s), {1} recoverable dead member(s), state {2}, waypoint {3}/{4}.",
                CountLivingMembers(),
                CountRecoverableDeadMembers(),
                m_State,
                m_WaypointIndex + 1,
                m_Waypoints == null ? 0 : m_Waypoints.Length);

            if (m_State == AdventurePartyState.Wiped && m_DeleteAfterWipe != DateTime.MinValue)
            {
                double seconds = Math.Max(0.0, (m_DeleteAfterWipe - DateTime.UtcNow).TotalSeconds);
                from.SendMessage("This wiped party controller will delete in about {0} second(s).", Math.Ceiling(seconds));
            }

            from.SendMessage("AI: enabled={0}, provider={1}, pending={2}, tactic={3}, status={4}", m_AIEnabled, m_AIProviderMode, AIRequestPending, m_CurrentTactic, m_LastAIStatus);
        }

        public override void OnAfterDelete()
        {
            m_Deleting = true;
            StopTimer();
            DeleteMembers();

            base.OnAfterDelete();
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)3); // version

            writer.Write((int)m_State);
            writer.Write((int)m_WaypointIndex);

            writer.Write(m_Waypoints == null ? 0 : m_Waypoints.Length);

            if (m_Waypoints != null)
            {
                for (int i = 0; i < m_Waypoints.Length; i++)
                {
                    writer.Write((Point3D)m_Waypoints[i]);
                }
            }

            writer.Write(m_Members.Count);

            for (int i = 0; i < m_Members.Count; i++)
            {
                writer.Write((Mobile)m_Members[i]);
            }

            writer.Write((DateTime)m_DeleteAfterWipe);

            writer.Write((bool)m_AIEnabled);
            writer.Write((int)m_AIProviderMode);
            writer.Write((string)m_LastAIStatus);
            writer.Write((int)m_CurrentTactic);
            writer.Write((DateTime)m_TacticExpires);
            writer.Write((string)m_TacticTarget);
            writer.Write((Point3D)m_TacticPoint);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_State = (AdventurePartyState)reader.ReadInt();
            m_WaypointIndex = reader.ReadInt();

            int waypointCount = reader.ReadInt();
            m_Waypoints = new Point3D[waypointCount];

            for (int i = 0; i < waypointCount; i++)
            {
                m_Waypoints[i] = reader.ReadPoint3D();
            }

            int memberCount = reader.ReadInt();

            for (int i = 0; i < memberCount; i++)
            {
                BaseAdventurer member = reader.ReadMobile() as BaseAdventurer;

                if (member != null && !member.Deleted)
                {
                    AddExistingMember(member);
                }
            }

            if (version >= 1)
            {
                m_DeleteAfterWipe = reader.ReadDateTime();
            }
            else
            {
                m_DeleteAfterWipe = DateTime.MinValue;
            }

            if (version >= 2)
            {
                m_AIEnabled = reader.ReadBool();
                m_AIProviderMode = (AdventurePartyAIProviderMode)reader.ReadInt();
                m_LastAIStatus = reader.ReadString();
            }
            else
            {
                m_AIEnabled = AdventurePartyAIConfig.EnabledByDefault;
                m_AIProviderMode = AdventurePartyAIConfig.DefaultProvider;
                m_LastAIStatus = m_AIEnabled ? "AI enabled." : "AI disabled.";
            }

            if (String.IsNullOrEmpty(m_LastAIStatus))
            {
                m_LastAIStatus = m_AIEnabled ? "AI enabled." : "AI disabled.";
            }

            if (version >= 3)
            {
                m_CurrentTactic = (AdventurePartyTactic)reader.ReadInt();
                m_TacticExpires = reader.ReadDateTime();
                m_TacticTarget = reader.ReadString();
                m_TacticPoint = reader.ReadPoint3D();
            }
            else
            {
                ClearTactic();
            }

            if (m_Waypoints.Length == 0)
            {
                BuildDefaultRoute(Location);
            }

            StartTimer();
        }

        private void AddMember(BaseAdventurer member, int xOffset, int yOffset)
        {
            AddMember(member, xOffset, yOffset, false, 0, 0, 0);
        }

        private void AddMember(BaseAdventurer member, int xOffset, int yOffset, int primaryHue, int secondaryHue, int accentHue)
        {
            AddMember(member, xOffset, yOffset, true, primaryHue, secondaryHue, accentHue);
        }

        private void AddMember(BaseAdventurer member, int xOffset, int yOffset, bool applyPartyAppearance, int primaryHue, int secondaryHue, int accentHue)
        {
            if (member == null)
            {
                return;
            }

            if (applyPartyAppearance)
            {
                member.ApplyPartyAppearance(primaryHue, secondaryHue, accentHue);
            }

            AddExistingMember(member);
            member.MoveToWorld(FindFitPoint(Location, xOffset, yOffset), Map);
        }

        private Point3D FindFitPoint(Point3D origin, int xOffset, int yOffset)
        {
            if (Map == null || Map == Map.Internal)
            {
                return origin;
            }

            Point3D point = new Point3D(origin.X + xOffset, origin.Y + yOffset, origin.Z);

            if (Map.CanFit(point, 16, false, false))
            {
                return point;
            }

            return origin;
        }

        private void StartTimer()
        {
            if (m_Timer != null)
            {
                return;
            }

            m_Timer = new AdventurePartyTimer(this);
            m_Timer.Start();
        }

        private void StopTimer()
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
                m_Timer = null;
            }
        }

        private void DeleteMembers()
        {
            for (int i = m_Members.Count - 1; i >= 0; i--)
            {
                BaseAdventurer member = m_Members[i];

                if (member != null && !member.Deleted)
                {
                    member.Controller = null;
                    member.Delete();
                }
            }

            m_Members.Clear();
        }

        private void OnTick()
        {
            if (Deleted)
            {
                StopTimer();
                return;
            }

            PruneMembers();

            int livingMembers = CountLivingMembers();

            if (m_Members.Count == 0 || livingMembers == 0)
            {
                HandleWipedParty();
                return;
            }

            m_DeleteAfterWipe = DateTime.MinValue;
            m_StateTicks++;
            ClearExpiredTactic();

            if (CountRecoverableDeadMembers() == 0 && !IsPostResurrectionRecoveryActive())
            {
                ClearResurrectionRally();
            }

            BaseAdventurer threatenedMember;
            Mobile directPartyThreat = FindDirectPartyThreat(out threatenedMember);
            Mobile enemy = FindEnemy();

            if (enemy == null && directPartyThreat != null)
            {
                enemy = directPartyThreat;
            }

            ObserveDragonBreathWindow(enemy, directPartyThreat);

            if (TryPrioritizeResurrectionRecovery(enemy))
            {
                ReportHealerDebug(enemy);
                return;
            }

            if (TryHandleRangedDisengage(enemy, directPartyThreat, threatenedMember))
            {
                ReportHealerDebug(enemy);
                return;
            }

            if (CountRecoverableDeadMembers() == 0 && TryHandlePostResurrectionRecovery(enemy))
            {
                ReportHealerDebug(enemy);
                return;
            }

            if (TryHandleDeadMemberRecovery(enemy))
            {
                ReportHealerDebug(enemy);
                return;
            }

            if (TryHandleRecoveryWithoutLivingResurrector(enemy))
            {
                ReportHealerDebug(enemy);
                return;
            }

            if (TryApplyDragonBreathWindowCounterAttack(enemy, directPartyThreat))
            {
                ReportHealerDebug(enemy);
                return;
            }

            RequestAIDecisionIfNeeded(enemy);
            AdventurePartyAIDecision aiDecision = ConsumeAIDecision();
            int criticalMembers = CountCriticalMembers();
            bool handledByAI = false;

            if (criticalMembers >= 2)
            {
                ClearTactic();
                ChangeState(AdventurePartyState.Retreating);
            }
            else if (TryApplyLocalFastEngage(directPartyThreat, threatenedMember))
            {
                handledByAI = true;
            }
            else
            {
                handledByAI = TryApplyAIDecision(aiDecision, enemy);
            }

            if (!handledByAI && enemy != null)
            {
                ChangeState(AdventurePartyState.Engaging);
            }
            else if (!handledByAI && ShouldRest())
            {
                ChangeState(AdventurePartyState.Resting);
            }
            else if (!handledByAI && IsChokeTactic(m_CurrentTactic))
            {
                ChangeState(AdventurePartyState.Resting);
            }
            else if (!handledByAI && m_State != AdventurePartyState.Exploring)
            {
                ChangeState(AdventurePartyState.Exploring);
            }

            switch (m_State)
            {
                case AdventurePartyState.Engaging:
                    Engage(enemy);
                    break;
                case AdventurePartyState.Resting:
                    Rest();
                    break;
                case AdventurePartyState.Retreating:
                    Retreat();
                    break;
                default:
                    Explore();
                    break;
            }

            ReportHealerDebug(enemy);
        }

        private void HandleWipedParty()
        {
            if (m_DeleteAfterWipe == DateTime.MinValue)
            {
                ClearTactic();
                ChangeState(AdventurePartyState.Wiped);
                m_DeleteAfterWipe = DateTime.UtcNow + AdventurePartySettings.WipeObservationTime;
            }

            if (DateTime.UtcNow >= m_DeleteAfterWipe)
            {
                Delete();
            }
        }

        private void RequestAIDecisionIfNeeded(Mobile enemy)
        {
            if (!m_AIEnabled || m_State == AdventurePartyState.Wiped)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;

            if (now < m_NextAIRequest || now < m_AIFailureCooldownUntil)
            {
                return;
            }

            lock (m_AISyncRoot)
            {
                if (m_AIRequestPending)
                {
                    return;
                }

                m_AIRequestPending = true;
                m_AIRequests++;
            }

            m_NextAIRequest = now + AdventurePartyAIConfig.RequestCooldown;
            m_LastAIStatus = "AI request queued.";

            AdventurePartyAISnapshot snapshot = BuildAISnapshot(enemy);
            AdventurePartyAIProviderMode provider = m_AIProviderMode;

            AdventurePartyAI.BeginRequest(
                snapshot,
                provider,
                delegate(AdventurePartyAIDecision decision, string error)
                {
                    ReceiveAIDecision(decision, error);
                });
        }

        private void ReceiveAIDecision(AdventurePartyAIDecision decision, string error)
        {
            lock (m_AISyncRoot)
            {
                m_AIRequestPending = false;

                if (!String.IsNullOrEmpty(error))
                {
                    m_AIFailures++;
                    m_AIFailureCooldownUntil = DateTime.UtcNow + AdventurePartyAIConfig.FailureCooldown;
                    m_LastAIStatus = "AI failed: " + error;
                    return;
                }

                if (decision == null)
                {
                    m_AIFailures++;
                    m_AIFailureCooldownUntil = DateTime.UtcNow + AdventurePartyAIConfig.FailureCooldown;
                    m_LastAIStatus = "AI returned no decision.";
                    return;
                }

                m_PendingAIDecision = decision;
                m_AIDecisions++;
                m_LastAIStatus = "AI decision ready: " + decision;
            }
        }

        private AdventurePartyAIDecision ConsumeAIDecision()
        {
            AdventurePartyAIDecision decision;

            lock (m_AISyncRoot)
            {
                decision = m_PendingAIDecision;
                m_PendingAIDecision = null;
            }

            if (decision == null)
            {
                return null;
            }

            if (decision.IsExpired)
            {
                m_LastAIStatus = "AI decision expired.";
                return null;
            }

            return decision;
        }

        private bool TryApplyAIDecision(AdventurePartyAIDecision decision, Mobile enemy)
        {
            if (decision == null)
            {
                return false;
            }

            if (!String.IsNullOrEmpty(decision.Speech))
            {
                PartyMessage(decision.Speech);
            }

            switch (decision.Action)
            {
                case AdventurePartyAIAction.FocusFire:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, true);

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.FocusFire, target);
                            ChangeState(AdventurePartyState.Engaging);
                            m_LastAIStatus = "AI applied FocusFire.";
                            return true;
                        }

                        break;
                    }
                case AdventurePartyAIAction.ProtectHealer:
                    {
                        Mobile target = FindThreatNearHealer();

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.ProtectHealer, target);
                            ChangeState(AdventurePartyState.Engaging);
                            m_LastAIStatus = "AI applied ProtectHealer.";
                            return true;
                        }

                        target = ResolveDecisionTarget(decision, enemy, false);

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.Standard, target);
                            ChangeState(AdventurePartyState.Engaging);
                            m_LastAIStatus = "AI downgraded ProtectHealer: no healer pressure.";
                            return true;
                        }

                        m_LastAIStatus = "AI ignored ProtectHealer: no healer pressure.";
                        return true;
                    }
                case AdventurePartyAIAction.Kite:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, false);

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.Kite, target);
                            ChangeState(AdventurePartyState.Engaging);
                            m_LastAIStatus = "AI applied Kite.";
                            return true;
                        }

                        break;
                    }
                case AdventurePartyAIAction.AvoidAOE:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, false);

                        SetTactic(AdventurePartyTactic.AvoidAOE, target);
                        ChangeState(target == null ? AdventurePartyState.Exploring : AdventurePartyState.Retreating);
                        m_LastAIStatus = "AI applied AvoidAOE.";
                        return true;
                    }
                case AdventurePartyAIAction.HoldChokePoint:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, false);

                        SetTactic(AdventurePartyTactic.HoldChokePoint, target);
                        ChangeState(target == null ? AdventurePartyState.Resting : AdventurePartyState.Engaging);
                        m_LastAIStatus = "AI applied HoldChokePoint.";
                        return true;
                    }
                case AdventurePartyAIAction.PullToChokePoint:
                case AdventurePartyAIAction.BreakPursuit:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, false);

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.PullToChokePoint, target);
                            ChangeState(AdventurePartyState.Engaging);
                            string name = decision.Action == AdventurePartyAIAction.BreakPursuit ? "BreakPursuit" : "PullToChokePoint";
                            m_LastAIStatus = String.IsNullOrEmpty(m_LastPullAnchorStatus) ? "AI applied " + name + "." : "AI applied " + name + ": " + m_LastPullAnchorStatus;
                            return true;
                        }

                        break;
                    }
                case AdventurePartyAIAction.PowerUp:
                    {
                        Mobile target = ResolveDecisionTarget(decision, enemy, false);

                        if (target != null)
                        {
                            SetTactic(AdventurePartyTactic.PowerUp, target);
                            ChangeState(AdventurePartyState.Engaging);
                            m_LastAIStatus = "AI applied PowerUp.";
                            return true;
                        }

                        break;
                    }
                case AdventurePartyAIAction.Engage:
                    if (enemy != null)
                    {
                        SetTactic(AdventurePartyTactic.Standard, enemy);
                        ChangeState(AdventurePartyState.Engaging);
                        m_LastAIStatus = "AI applied Engage.";
                        return true;
                    }
                    break;
                case AdventurePartyAIAction.Retreat:
                case AdventurePartyAIAction.Avoid:
                    SetTactic(decision.Action == AdventurePartyAIAction.Avoid ? AdventurePartyTactic.AvoidAOE : AdventurePartyTactic.Standard, enemy);
                    ChangeState(AdventurePartyState.Retreating);
                    m_LastAIStatus = "AI applied " + decision.Action + ".";
                    return true;
                case AdventurePartyAIAction.Rest:
                case AdventurePartyAIAction.Recover:
                case AdventurePartyAIAction.HoldPosition:
                    if (enemy == null)
                    {
                        SetTactic(decision.Action == AdventurePartyAIAction.HoldPosition ? AdventurePartyTactic.HoldChokePoint : AdventurePartyTactic.Standard, null);
                        ChangeState(AdventurePartyState.Resting);
                        m_LastAIStatus = "AI applied " + decision.Action + ".";
                        return true;
                    }
                    else if (decision.Action == AdventurePartyAIAction.Recover)
                    {
                        SetTactic(AdventurePartyTactic.Standard, enemy);
                        ChangeState(AdventurePartyState.Retreating);
                        m_LastAIStatus = "AI applied Recover as local retreat/recovery.";
                        return true;
                    }
                    break;
                case AdventurePartyAIAction.Explore:
                    if (enemy == null)
                    {
                        ClearTactic();
                        ChangeState(AdventurePartyState.Exploring);
                        m_LastAIStatus = "AI applied Explore.";
                        return true;
                    }
                    break;
                case AdventurePartyAIAction.Say:
                    m_LastAIStatus = "AI applied Say.";
                    break;
            }

            return false;
        }

        private bool TryApplyLocalFastEngage(Mobile directThreat, BaseAdventurer threatenedMember)
        {
            if (!IsActive(threatenedMember) || !IsValidEnemy(directThreat, threatenedMember))
            {
                return false;
            }

            if (m_State == AdventurePartyState.Retreating ||
                m_CurrentTactic == AdventurePartyTactic.AvoidAOE ||
                m_CurrentTactic == AdventurePartyTactic.Recovering ||
                CountRecoverableDeadMembers() > 0)
            {
                return false;
            }

            bool backlineThreat = threatenedMember.Role != AdventurePartyRole.Fighter;

            if (m_CurrentTactic == AdventurePartyTactic.PullToChokePoint)
            {
                if (ShouldFinishFleeingTarget(directThreat, threatenedMember))
                {
                    ClearResetPullState();
                    SetTactic(AdventurePartyTactic.PowerUp, directThreat);
                    ChangeState(AdventurePartyState.Engaging);
                    AssignFinishFleeingTarget(directThreat);
                    m_LastAIStatus = String.Format(
                        "Finish fleeing target: {0} is low and isolated; dropping pull anchor for a short chase.",
                        GetDebugName(directThreat));
                    return true;
                }

                if (ShouldInterceptIsolatedBacklineTarget(directThreat, threatenedMember))
                {
                    ClearResetPullState();
                    SetTactic(AdventurePartyTactic.PowerUp, directThreat);
                    ChangeState(AdventurePartyState.Engaging);
                    AssignInterceptionTarget(directThreat);
                    m_LastAIStatus = String.Format(
                        "Isolated backline intercept: {0} is alone and pressuring {1}; fighter is taking over.",
                        GetDebugName(directThreat),
                        threatenedMember.Role);
                    return true;
                }

                if (backlineThreat)
                {
                    ChangeState(AdventurePartyState.Engaging);
                    m_LastAIStatus = String.Format(
                        "Local pull response: {0} is pressuring {1}; preserving pull anchor while backline circles the fighter.",
                        GetDebugName(directThreat),
                        threatenedMember.Role);
                    return true;
                }

                if (!IsFighterPressureBreakNeeded(threatenedMember))
                {
                    return false;
                }
            }

            if (IsFighterPressureBreakNeeded(threatenedMember))
            {
                SetTactic(AdventurePartyTactic.PullToChokePoint, directThreat);
                ChangeState(AdventurePartyState.Engaging);
                m_LastAIStatus = String.Format(
                    "Local pressure relief: {0} is overloading the fighter; forcing early BreakPursuit setup.",
                    GetDebugName(directThreat));
                return true;
            }

            if (m_CurrentTactic == AdventurePartyTactic.PowerUp &&
                threatenedMember.Role == AdventurePartyRole.Fighter &&
                ShouldCommitToLocalWeakPackCounterAttack(threatenedMember, directThreat))
            {
                ChangeState(AdventurePartyState.Engaging);
                m_LastAIStatus = String.Format(
                    "Local weak pack counterattack: {0} is pressuring the fighter, but this room is cleanable; holding PowerUp.",
                    GetDebugName(directThreat));
                return true;
            }

            if (ShouldPreferPackPull(threatenedMember, directThreat))
            {
                SetTactic(AdventurePartyTactic.PullToChokePoint, directThreat);
                ChangeState(AdventurePartyState.Engaging);
                m_LastAIStatus = String.Format(
                    "Local pack response: {0} is pressuring {1}; pulling the pack instead of dueling in place.",
                    GetDebugName(directThreat),
                    threatenedMember.Role);
                return true;
            }

            if (!backlineThreat && m_State == AdventurePartyState.Engaging)
            {
                return false;
            }

            AdventurePartyTactic tactic = AdventurePartyTactic.Standard;

            if (threatenedMember.Role == AdventurePartyRole.Healer)
            {
                tactic = AdventurePartyTactic.ProtectHealer;
            }
            else if (backlineThreat)
            {
                tactic = AdventurePartyTactic.Kite;
            }

            SetTactic(tactic, directThreat);
            ChangeState(AdventurePartyState.Engaging);
            m_LastAIStatus = String.Format("Local fast engage: {0} is targeting {1}.", GetDebugName(directThreat), threatenedMember.Role);
            return true;
        }

        private bool ShouldCommitToLocalWeakPackCounterAttack(BaseAdventurer fighter, Mobile directThreat)
        {
            if (!IsActive(fighter) || !IsValidEnemy(directThreat, fighter) ||
                CountRecoverableDeadMembers() > 0 || IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0 || GetHitPercent(fighter) < 85)
            {
                return false;
            }

            List<Mobile> enemies = FindRoomRangedDisengageCounterAttackPack(fighter, directThreat, directThreat);

            if (enemies.Count == 0 ||
                enemies.Count > AdventurePartySettings.LocalWeakPackCounterAttackMaxEnemies)
            {
                return false;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (IsDragonBreathThreat(enemy))
                {
                    return false;
                }

                if (IsRangedPressureEnemy(enemy) && !IsEnemyPressuringParty(enemy))
                {
                    return false;
                }

                if (IsAIHardTarget(enemy) &&
                    GetHitPercent(enemy) > AdventurePartySettings.LocalWeakPackCounterAttackHitPercent)
                {
                    return false;
                }
            }

            return true;
        }

        private bool ShouldPreferPackPull(BaseAdventurer threatenedMember, Mobile directThreat)
        {
            if (!IsActive(threatenedMember) || !IsValidEnemy(directThreat, threatenedMember))
            {
                return false;
            }

            int nearby = CountNearbyEnemies(threatenedMember, AdventurePartySettings.PerceptionRange);
            int directPursuers = CountDirectPartyPursuers(threatenedMember, AdventurePartySettings.PerceptionRange);
            int rangedPressure = CountRangedPressureThreats(
                threatenedMember,
                Math.Max(AdventurePartySettings.PerceptionRange, AdventurePartySettings.PullRangedThreatScanRange));

            if (nearby >= 4 || directPursuers >= 3 ||
                rangedPressure >= AdventurePartySettings.PullHardResetRangedThreats)
            {
                return true;
            }

            if (threatenedMember.Role != AdventurePartyRole.Fighter && IsRangedPressureEnemy(directThreat))
            {
                return true;
            }

            return threatenedMember.Role == AdventurePartyRole.Fighter &&
                CountNearbyEnemies(threatenedMember, AdventurePartySettings.BacklineThreatRange) >= 3;
        }

        private void ObserveDragonBreathWindow(Mobile enemy, Mobile directPartyThreat)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                int previousHits;

                if (!m_LastObservedHits.TryGetValue(member.Serial, out previousHits))
                {
                    m_LastObservedHits[member.Serial] = member.Hits;
                    continue;
                }

                int damage = previousHits - member.Hits;
                m_LastObservedHits[member.Serial] = member.Hits;

                if (damage <= 0 || member.HitsMax <= 0)
                {
                    continue;
                }

                int hitPercent = (damage * 100) / member.HitsMax;

                if (damage < AdventurePartySettings.DragonBreathBurstMinimumDamage &&
                    hitPercent < AdventurePartySettings.DragonBreathBurstMinimumHitPercent)
                {
                    continue;
                }

                Mobile source = FindLikelyDragonBreathSource(member, enemy, directPartyThreat);

                if (source == null)
                {
                    continue;
                }

                m_LastDragonBreathThreat = source.Serial;
                m_LastDragonBreathObserved = DateTime.UtcNow;
                m_LastAIStatus = String.Format(
                    "DragonBreathWindow observed: {0} hit {1} for {2}; watching cooldown burst window.",
                    GetDebugName(source),
                    member.Role,
                    damage);
            }
        }

        private Mobile FindLikelyDragonBreathSource(BaseAdventurer victim, Mobile enemy, Mobile directPartyThreat)
        {
            Mobile best = null;
            double bestDistance = Double.MaxValue;

            ConsiderDragonBreathSource(victim, enemy, ref best, ref bestDistance);
            ConsiderDragonBreathSource(victim, directPartyThreat, ref best, ref bestDistance);
            ConsiderDragonBreathSource(victim, victim.Combatant as Mobile, ref best, ref bestDistance);

            IPooledEnumerable eable = victim.GetMobilesInRange(AdventurePartySettings.DragonBreathObserveRange);

            foreach (Mobile mobile in eable)
            {
                ConsiderDragonBreathSource(victim, mobile, ref best, ref bestDistance);
            }

            eable.Free();
            return best;
        }

        private void ConsiderDragonBreathSource(BaseAdventurer victim, Mobile candidate, ref Mobile best, ref double bestDistance)
        {
            if (!IsActive(victim) || !IsValidEnemy(candidate, victim) || !IsDragonBreathThreat(candidate))
            {
                return;
            }

            double distance = victim.GetDistanceToSqrt(candidate);

            if (distance > AdventurePartySettings.DragonBreathObserveRange &&
                candidate.Combatant != victim && !CanEnemyNoticePoint(candidate, victim.Location))
            {
                return;
            }

            if (candidate.Combatant == victim)
            {
                distance -= 6.0;
            }

            if (best == null || distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        private bool TryApplyDragonBreathWindowCounterAttack(Mobile enemy, Mobile directPartyThreat)
        {
            BaseAdventurer observer = GetLeader();

            if (!IsActive(observer) || CountRecoverableDeadMembers() > 0 || IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0)
            {
                return false;
            }

            Mobile target = FindDragonBreathWindowCounterAttackTarget(observer, enemy, directPartyThreat);

            if (target == null)
            {
                return false;
            }

            if (m_LastDragonBreathCounterThreat == target.Serial &&
                m_LastDragonBreathCounterObserved == m_LastDragonBreathObserved)
            {
                return false;
            }

            ClearRangedDisengageState();
            ClearBreakPursuitPending();
            ClearResetPullState();
            SetTactic(AdventurePartyTactic.PowerUp, target);
            ChangeState(AdventurePartyState.Engaging);
            Engage(target);
            m_LastDragonBreathCounterThreat = target.Serial;
            m_LastDragonBreathCounterObserved = m_LastDragonBreathObserved;

            double seconds = (DateTime.UtcNow - m_LastDragonBreathObserved).TotalSeconds;
            m_LastAIStatus = String.Format(
                "DragonBreathWindow counterattack: {0} breathed {1:0.0}s ago; bursting during cooldown.",
                GetDebugName(target),
                seconds);
            return true;
        }

        private Mobile FindDragonBreathWindowCounterAttackTarget(BaseAdventurer observer, Mobile enemy, Mobile directPartyThreat)
        {
            Mobile remembered = World.FindMobile(m_LastDragonBreathThreat);

            if (!IsRecentDragonBreathWindow(remembered) || !observer.InRange(remembered, AdventurePartySettings.DragonBreathBurstWindowRange))
            {
                return null;
            }

            List<Mobile> enemies = new List<Mobile>();

            AddCounterAttackCandidate(enemies, observer, remembered);
            AddCounterAttackCandidate(enemies, observer, enemy);
            AddCounterAttackCandidate(enemies, observer, directPartyThreat);

            IPooledEnumerable eable = observer.GetMobilesInRange(AdventurePartySettings.DragonBreathBurstWindowRange);

            foreach (Mobile mobile in eable)
            {
                AddCounterAttackCandidate(enemies, observer, mobile);
            }

            eable.Free();
            return enemies.Count == 1 && enemies[0] == remembered ? remembered : null;
        }

        private bool IsRecentDragonBreathWindow(Mobile mobile)
        {
            return IsDragonBreathThreat(mobile) &&
                m_LastDragonBreathThreat == mobile.Serial &&
                DateTime.UtcNow <= m_LastDragonBreathObserved + AdventurePartySettings.DragonBreathBurstWindow;
        }

        private bool IsDragonBreathThreat(Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;

            if (creature == null)
            {
                return false;
            }

            return creature.HasAbility(SpecialAbility.DragonBreath);
        }

        private bool TryHandleRangedDisengage(Mobile enemy, Mobile directPartyThreat, BaseAdventurer threatenedMember)
        {
            BaseAdventurer observer = GetRangedDisengageObserver();

            if (!IsActive(observer))
            {
                ClearRangedDisengageState();
                return false;
            }

            List<Mobile> threats = FindRangedDisengageThreats(observer, enemy, AdventurePartySettings.RangedDisengageScanRange);
            int directTargets = CountPullPartyTargets(observer, AdventurePartySettings.RangedDisengageScanRange, false);
            bool recoveryPressure = CountRecoverableDeadMembers() > 0 || IsPostResurrectionRecoveryActive();
            bool shouldStart = threats.Count >= AdventurePartySettings.RangedDisengageThreats &&
                (enemy != null || directTargets > 0 || CountCriticalMembers() > 0 || recoveryPressure ||
                (directPartyThreat != null && IsRangedPressureEnemy(directPartyThreat)));

            if (!m_RangedDisengageActive && !shouldStart)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            if (!m_RangedDisengageActive)
            {
                BeginRangedDisengage(observer, threats);
            }
            else if (threats.Count > 0)
            {
                m_RangedDisengagePoint = GetRangedDisengagePoint(observer, threats);
            }

            bool minimumElapsed = now >= m_RangedDisengageStarted + AdventurePartySettings.RangedDisengageMinimumTime;
            bool maximumElapsed = now >= m_RangedDisengageUntil;
            int visibleThreats = CountRangedDisengageThreatsSeeingParty(threats);
            int localThreats = CountRangedPressureThreatsNearPoint(observer, m_RangedDisengagePoint, AdventurePartySettings.RangedDisengageClearRange);
            string stuckStatus = CheckRangedDisengageProgress(observer, threats, visibleThreats, localThreats, directTargets);

            if (TryApplyRangedDisengageRoomCounterAttack(observer, enemy, directPartyThreat))
            {
                return true;
            }

            if (visibleThreats > 0 || localThreats > 0)
            {
                m_RangedDisengageSettleUntil = now + AdventurePartySettings.RangedDisengageSettleTime;
            }

            if (minimumElapsed && visibleThreats == 0 && localThreats == 0 &&
                now >= m_RangedDisengageSettleUntil)
            {
                if (TryApplyRangedDisengageCounterAttack(observer, enemy))
                {
                    return true;
                }

                ClearRangedDisengageState();
                return false;
            }

            if (maximumElapsed && visibleThreats == 0 && localThreats == 0 && directTargets == 0)
            {
                if (TryApplyRangedDisengageCounterAttack(observer, enemy))
                {
                    return true;
                }

                ClearRangedDisengageState();
                return false;
            }

            ClearBreakPursuitPending();
            ClearResetPullState();
            ChangeState(AdventurePartyState.Retreating);
            m_CurrentTactic = AdventurePartyTactic.Recovering;
            m_TacticPoint = m_RangedDisengagePoint;
            m_TacticTarget = "";
            m_TacticExpires = now + AdventurePartySettings.RangedDisengageMaximumTime;

            ExecuteRangedDisengage(m_RangedDisengagePoint);

            m_LastAIStatus = String.Format(
                String.IsNullOrEmpty(stuckStatus) ?
                "RangedDisengage: {0} ranged threat(s), {1} still seeing party, {2} near escape point; breaking line of sight toward ({3},{4},{5})." :
                "RangedDisengage: {0} ranged threat(s), {1} still seeing party, {2} near escape point; {6}; breaking line of sight toward ({3},{4},{5}).",
                threats.Count,
                visibleThreats,
                localThreats,
                m_RangedDisengagePoint.X,
                m_RangedDisengagePoint.Y,
                m_RangedDisengagePoint.Z,
                stuckStatus);
            return true;
        }

        private bool TryApplyRangedDisengageRoomCounterAttack(BaseAdventurer observer, Mobile fallback, Mobile directPartyThreat)
        {
            if (!IsActive(observer) || CountRecoverableDeadMembers() > 0 || IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0)
            {
                return false;
            }

            List<Mobile> enemies = FindRoomRangedDisengageCounterAttackPack(observer, fallback, directPartyThreat);

            if (!IsWeakRangedDisengageCounterAttackPack(enemies))
            {
                return false;
            }

            Mobile target = SelectRangedDisengageCounterAttackTarget(enemies);

            if (target == null)
            {
                return false;
            }

            ClearRangedDisengageState();
            ClearBreakPursuitPending();
            ClearResetPullState();
            SetTactic(AdventurePartyTactic.PowerUp, target);
            ChangeState(AdventurePartyState.Engaging);
            Engage(target);
            m_LastAIStatus = String.Format(
                "RangedDisengage counterattack: room weak pack ({0} enemies); turning back on {1}.",
                enemies.Count,
                GetDebugName(target));
            return true;
        }

        private List<Mobile> FindRoomRangedDisengageCounterAttackPack(BaseAdventurer observer, Mobile fallback, Mobile directPartyThreat)
        {
            List<Mobile> enemies = new List<Mobile>();

            AddRoomCounterAttackCandidate(enemies, observer, fallback);
            AddRoomCounterAttackCandidate(enemies, observer, directPartyThreat);

            IPooledEnumerable eable = observer.GetMobilesInRange(AdventurePartySettings.RangedDisengageRoomCounterAttackRange);

            foreach (Mobile mobile in eable)
            {
                AddRoomCounterAttackCandidate(enemies, observer, mobile);
            }

            eable.Free();
            return enemies;
        }

        private void AddRoomCounterAttackCandidate(List<Mobile> enemies, BaseAdventurer observer, Mobile candidate)
        {
            if (!IsValidEnemy(candidate, observer) || enemies.Contains(candidate))
            {
                return;
            }

            if (IsEnemyPressuringParty(candidate))
            {
                enemies.Add(candidate);
                return;
            }

            if (!candidate.InRange(observer, AdventurePartySettings.RangedDisengageRoomCounterAttackRange) ||
                !IsLikelySameRoomElevation(candidate) ||
                !HasPartyRoomLineOfSight(candidate))
            {
                return;
            }

            enemies.Add(candidate);
        }

        private bool IsLikelySameRoomElevation(Mobile enemy)
        {
            if (enemy == null || enemy.Deleted || enemy.Map != Map)
            {
                return false;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Map == Map &&
                    member.InRange(enemy, AdventurePartySettings.RangedDisengageRoomCounterAttackRange) &&
                    Math.Abs(member.Z - enemy.Z) <= 12)
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasPartyRoomLineOfSight(Mobile enemy)
        {
            if (enemy == null || enemy.Deleted || !enemy.Alive || enemy.Map != Map)
            {
                return false;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Map == Map &&
                    member.InRange(enemy, AdventurePartySettings.RangedDisengageRoomCounterAttackRange) &&
                    Map.LineOfSight(member.Location, enemy.Location))
                {
                    return true;
                }
            }

            return false;
        }

        private BaseAdventurer GetRangedDisengageObserver()
        {
            BaseAdventurer leader = GetLeader();

            if (IsActive(leader))
            {
                return leader;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member))
                {
                    return member;
                }
            }

            return null;
        }

        private void BeginRangedDisengage(BaseAdventurer observer, List<Mobile> threats)
        {
            DateTime now = DateTime.UtcNow;

            m_RangedDisengageActive = true;
            m_RangedDisengageStarted = now;
            m_RangedDisengageUntil = now + AdventurePartySettings.RangedDisengageMaximumTime;
            m_RangedDisengageSettleUntil = now + AdventurePartySettings.RangedDisengageSettleTime;
            m_RangedDisengagePoint = GetRangedDisengagePoint(observer, threats);
            m_RangedDisengageProgressAnchor = GetPartyDisengageAnchor(observer);
            m_RangedDisengageProgressChecked = now;
            m_RangedDisengageStuckTicks = 0;
        }

        private void ClearRangedDisengageState()
        {
            m_RangedDisengageActive = false;
            m_RangedDisengagePoint = Point3D.Zero;
            m_RangedDisengageStarted = DateTime.MinValue;
            m_RangedDisengageUntil = DateTime.MinValue;
            m_RangedDisengageSettleUntil = DateTime.MinValue;
            m_RangedDisengageProgressAnchor = Point3D.Zero;
            m_RangedDisengageProgressChecked = DateTime.MinValue;
            m_RangedDisengageStuckTicks = 0;
            m_RangedDisengageBadPoint = Point3D.Zero;
            m_RangedDisengageBadPointUntil = DateTime.MinValue;
            m_RangedDisengageTurnBias = 0;
        }

        private string CheckRangedDisengageProgress(
            BaseAdventurer observer,
            List<Mobile> threats,
            int visibleThreats,
            int localThreats,
            int directTargets)
        {
            if (!m_RangedDisengageActive || !IsActive(observer))
            {
                return "";
            }

            DateTime now = DateTime.UtcNow;
            Point3D anchor = GetPartyDisengageAnchor(observer);

            if (m_RangedDisengageProgressChecked == DateTime.MinValue ||
                now < m_RangedDisengageProgressChecked + AdventurePartySettings.RangedDisengageStuckCheckTime)
            {
                if (m_RangedDisengageProgressChecked == DateTime.MinValue)
                {
                    m_RangedDisengageProgressAnchor = anchor;
                    m_RangedDisengageProgressChecked = now;
                }

                return "";
            }

            double moved = GetPointDistance(anchor, m_RangedDisengageProgressAnchor);
            int closeThreats = CountEnemiesNearPoint(observer, anchor, AdventurePartySettings.RangedDisengageStuckThreatRange);
            bool pressureStillHigh = visibleThreats > 0 || directTargets > 0 || closeThreats >= 2;

            if (moved < AdventurePartySettings.RangedDisengageStuckMoveDistance && pressureStillHigh)
            {
                m_RangedDisengageStuckTicks++;
            }
            else
            {
                m_RangedDisengageStuckTicks = 0;
            }

            m_RangedDisengageProgressAnchor = anchor;
            m_RangedDisengageProgressChecked = now;

            if (m_RangedDisengageStuckTicks < 1)
            {
                return "";
            }

            m_RangedDisengageBadPoint = m_RangedDisengagePoint;
            m_RangedDisengageBadPointUntil = now + AdventurePartySettings.RangedDisengageBadPointTime;
            m_RangedDisengageTurnBias++;
            m_RangedDisengagePoint = GetRangedDisengagePoint(observer, threats);
            m_RangedDisengageSettleUntil = now + AdventurePartySettings.RangedDisengageSettleTime;
            m_RangedDisengageStuckTicks = 0;

            return String.Format(
                "escape route stalled after moving {0:0.0} tiles with {1} close threat(s), blacklisting ({2},{3},{4})",
                moved,
                closeThreats,
                m_RangedDisengageBadPoint.X,
                m_RangedDisengageBadPoint.Y,
                m_RangedDisengageBadPoint.Z);
        }

        private bool TryApplyRangedDisengageCounterAttack(BaseAdventurer observer, Mobile fallback)
        {
            if (!IsActive(observer) || CountRecoverableDeadMembers() > 0 || IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0)
            {
                return false;
            }

            Mobile target = FindRangedDisengageCounterAttackTarget(observer, fallback);

            if (target == null)
            {
                return false;
            }

            ClearRangedDisengageState();
            ClearBreakPursuitPending();
            ClearResetPullState();
            SetTactic(AdventurePartyTactic.PowerUp, target);
            ChangeState(AdventurePartyState.Engaging);
            Engage(target);

            if (IsRecentDragonBreathWindow(target))
            {
                double seconds = (DateTime.UtcNow - m_LastDragonBreathObserved).TotalSeconds;
                m_LastAIStatus = String.Format(
                    "RangedDisengage counterattack: only {0} remains after breath {1:0.0}s ago; turning back with PowerUp.",
                    GetDebugName(target),
                    seconds);
            }
            else
            {
                m_LastAIStatus = String.Format(
                    "RangedDisengage counterattack: only {0} remains; turning back with PowerUp.",
                    GetDebugName(target));
            }
            return true;
        }

        private Mobile FindRangedDisengageCounterAttackTarget(BaseAdventurer observer, Mobile fallback)
        {
            List<Mobile> enemies = new List<Mobile>();

            AddCounterAttackCandidate(enemies, observer, fallback);

            IPooledEnumerable eable = observer.GetMobilesInRange(AdventurePartySettings.RangedDisengageCounterAttackRange);

            foreach (Mobile mobile in eable)
            {
                AddCounterAttackCandidate(enemies, observer, mobile);
            }

            eable.Free();

            if (enemies.Count == 1)
            {
                return enemies[0];
            }

            if (IsWeakRangedDisengageCounterAttackPack(enemies))
            {
                return SelectRangedDisengageCounterAttackTarget(enemies);
            }

            return null;
        }

        private void AddCounterAttackCandidate(List<Mobile> enemies, BaseAdventurer observer, Mobile candidate)
        {
            if (!IsValidEnemy(candidate, observer) || enemies.Contains(candidate))
            {
                return;
            }

            enemies.Add(candidate);
        }

        private bool IsWeakRangedDisengageCounterAttackPack(List<Mobile> enemies)
        {
            if (enemies == null || enemies.Count == 0 ||
                enemies.Count > AdventurePartySettings.RangedDisengageWeakPackCounterAttackMaxEnemies)
            {
                return false;
            }

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy == null || enemy.Deleted || !enemy.Alive || enemy.Map != Map)
                {
                    return false;
                }

                if (IsDragonBreathThreat(enemy))
                {
                    return false;
                }

                if (IsAIHardTarget(enemy) &&
                    GetHitPercent(enemy) > AdventurePartySettings.RangedDisengageWeakPackCounterAttackHitPercent)
                {
                    return false;
                }
            }

            return true;
        }

        private Mobile SelectRangedDisengageCounterAttackTarget(List<Mobile> enemies)
        {
            Mobile best = null;
            double bestScore = Double.MinValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy == null || enemy.Deleted || !enemy.Alive)
                {
                    continue;
                }

                double score = 100.0 - GetHitPercent(enemy);

                if (IsRangedPressureEnemy(enemy))
                {
                    score += 25.0;
                }

                if (IsAIHardTarget(enemy))
                {
                    score += 10.0;
                }

                if (best == null || score > bestScore)
                {
                    best = enemy;
                    bestScore = score;
                }
            }

            return best;
        }

        private List<Mobile> FindRangedDisengageThreats(BaseAdventurer observer, Mobile focus, int range)
        {
            List<Mobile> threats = new List<Mobile>();

            if (!IsActive(observer))
            {
                return threats;
            }

            if (IsValidEnemy(focus, observer) && IsRangedPressureEnemy(focus) &&
                (IsEnemyPressuringParty(focus) || CanEnemyNoticeParty(focus)))
            {
                threats.Add(focus);
            }

            IPooledEnumerable eable = observer.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer) || !IsRangedPressureEnemy(mobile))
                {
                    continue;
                }

                if (mobile == focus || IsEnemyPressuringParty(mobile) || CanEnemyNoticeParty(mobile))
                {
                    if (!threats.Contains(mobile))
                    {
                        threats.Add(mobile);
                    }
                }
            }

            eable.Free();
            return threats;
        }

        private bool IsEnemyPressuringParty(Mobile enemy)
        {
            BaseAdventurer target = enemy == null ? null : enemy.Combatant as BaseAdventurer;
            return IsActive(target) && IsPartyMember(target);
        }

        private bool CanEnemyNoticeParty(Mobile enemy)
        {
            if (enemy == null || enemy.Deleted || enemy.Map != Map)
            {
                return false;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if ((IsActive(member) || IsRecoverableDeadMember(member)) && CanEnemyNoticePoint(enemy, member.Location))
                {
                    return true;
                }
            }

            return false;
        }

        private int CountRangedDisengageThreatsSeeingParty(List<Mobile> threats)
        {
            int count = 0;

            for (int i = 0; threats != null && i < threats.Count; i++)
            {
                if (CanEnemyNoticeParty(threats[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountRangedPressureThreatsNearPoint(BaseAdventurer observer, Point3D point, int range)
        {
            if (!IsActive(observer) || point == Point3D.Zero || Map == null || Map == Map.Internal)
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, observer) && IsRangedPressureEnemy(mobile) &&
                    GetPointDistance(point, mobile.Location) <= range)
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountEnemiesNearPoint(BaseAdventurer observer, Point3D point, int range)
        {
            if (!IsActive(observer) || Map == null || Map == Map.Internal)
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, observer) && GetPointDistance(point, mobile.Location) <= range)
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private Point3D GetRangedDisengagePoint(BaseAdventurer observer, List<Mobile> threats)
        {
            Point3D anchor = GetPartyDisengageAnchor(observer);
            Point3D danger = GetThreatCenter(threats, anchor);
            Point3D best = anchor;
            double bestScore = Double.MinValue;

            for (int distance = AdventurePartySettings.RangedDisengageCornerMinDistance;
                distance <= AdventurePartySettings.RangedDisengageBreakDistance + 12;
                distance += 4)
            {
                for (int direction = 0; direction < 8; direction++)
                {
                    Point3D raw = GetRangedDisengageDirectionPoint(anchor, danger, distance, direction);

                    for (int x = -6; x <= 6; x += 3)
                    {
                        for (int y = -6; y <= 6; y += 3)
                        {
                            Point3D candidate = new Point3D(raw.X + x, raw.Y + y, raw.Z);

                            if (Map == null || Map == Map.Internal ||
                                !Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16, false, true, true, observer))
                            {
                                continue;
                            }

                            double score = ScoreRangedDisengagePoint(threats, candidate, anchor);

                            if (score > bestScore)
                            {
                                best = candidate;
                                bestScore = score;
                            }
                        }
                    }
                }
            }

            return best == anchor ? GetPointAwayFrom(anchor, danger, AdventurePartySettings.RangedDisengageBreakDistance) : best;
        }

        private Point3D GetRangedDisengageDirectionPoint(Point3D anchor, Point3D danger, int distance, int directionIndex)
        {
            int awayX = anchor.X - danger.X;
            int awayY = anchor.Y - danger.Y;

            if (awayX == 0 && awayY == 0)
            {
                awayX = 1;
            }

            int sideX = -awayY;
            int sideY = awayX;

            int variant = (directionIndex + m_RangedDisengageTurnBias) & 0x7;
            int x;
            int y;

            switch (variant)
            {
                case 1:
                    x = awayX + sideX;
                    y = awayY + sideY;
                    break;
                case 2:
                    x = sideX;
                    y = sideY;
                    break;
                case 3:
                    x = -awayX + sideX;
                    y = -awayY + sideY;
                    break;
                case 4:
                    x = -awayX;
                    y = -awayY;
                    break;
                case 5:
                    x = -awayX - sideX;
                    y = -awayY - sideY;
                    break;
                case 6:
                    x = -sideX;
                    y = -sideY;
                    break;
                case 7:
                    x = awayX - sideX;
                    y = awayY - sideY;
                    break;
                default:
                    x = awayX;
                    y = awayY;
                    break;
            }

            double length = Math.Sqrt((x * x) + (y * y));

            if (length <= 0.0)
            {
                return anchor;
            }

            return new Point3D(
                anchor.X + (int)Math.Round((x / length) * distance),
                anchor.Y + (int)Math.Round((y / length) * distance),
                anchor.Z);
        }

        private Point3D GetPartyDisengageAnchor(BaseAdventurer fallback)
        {
            int x = 0;
            int y = 0;
            int z = fallback == null ? Location.Z : fallback.Z;
            int count = 0;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                x += member.X;
                y += member.Y;
                z = member.Z;
                count++;
            }

            if (count == 0)
            {
                return fallback == null ? Location : fallback.Location;
            }

            return new Point3D(x / count, y / count, z);
        }

        private Point3D GetThreatCenter(List<Mobile> threats, Point3D fallback)
        {
            int x = 0;
            int y = 0;
            int z = fallback.Z;
            int count = 0;

            for (int i = 0; threats != null && i < threats.Count; i++)
            {
                Mobile threat = threats[i];

                if (threat == null || threat.Deleted || threat.Map != Map)
                {
                    continue;
                }

                x += threat.X;
                y += threat.Y;
                z = threat.Z;
                count++;
            }

            if (count == 0)
            {
                return fallback;
            }

            return new Point3D(x / count, y / count, z);
        }

        private double ScoreRangedDisengagePoint(List<Mobile> threats, Point3D point, Point3D anchor)
        {
            double score = 0.0;
            double anchorDistance = GetPointDistance(anchor, point);

            score -= anchorDistance * 1.2;
            score -= ScoreRangedDisengageRouteDanger(threats, anchor, point);
            score -= ScoreRangedDisengagePointCrowding(point);
            score += ScoreRangedDisengagePointOpenness(point);

            if (anchorDistance <= AdventurePartySettings.RangedDisengageCornerMaxDistance)
            {
                score += 60.0;
            }

            if (m_RangedDisengageBadPoint != Point3D.Zero &&
                DateTime.UtcNow <= m_RangedDisengageBadPointUntil &&
                GetPointDistance(point, m_RangedDisengageBadPoint) <= AdventurePartySettings.RangedDisengageBadPointRange)
            {
                score -= 900.0;
            }

            for (int i = 0; threats != null && i < threats.Count; i++)
            {
                Mobile threat = threats[i];

                if (threat == null || threat.Deleted || threat.Map != Map)
                {
                    continue;
                }

                double distance = GetPointDistance(point, threat.Location);
                score += Math.Min(240.0, distance * 12.0);

                if (!CanEnemyNoticePoint(threat, point))
                {
                    score += 180.0;
                }
                else
                {
                    score -= 240.0;
                }
            }

            return score;
        }

        private double ScoreRangedDisengagePointCrowding(Point3D point)
        {
            BaseAdventurer observer = GetRangedDisengageObserver();

            if (!IsActive(observer) || Map == null || Map == Map.Internal)
            {
                return 0.0;
            }

            double penalty = 0.0;
            IPooledEnumerable eable = Map.GetMobilesInRange(point, AdventurePartySettings.RangedDisengageCrowdingRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer))
                {
                    continue;
                }

                double distance = Math.Max(1.0, GetPointDistance(point, mobile.Location));
                double localPenalty = (AdventurePartySettings.RangedDisengageCrowdingRange - distance + 1.0) * 35.0;

                if (IsRangedPressureEnemy(mobile))
                {
                    localPenalty *= 2.2;
                }
                else if (IsAIHardTarget(mobile))
                {
                    localPenalty *= 1.6;
                }

                if (CanEnemyNoticePoint(mobile, point))
                {
                    localPenalty += 160.0;
                }

                penalty += localPenalty;
            }

            eable.Free();
            return penalty;
        }

        private double ScoreRangedDisengagePointOpenness(Point3D point)
        {
            if (Map == null || Map == Map.Internal)
            {
                return 0.0;
            }

            double score = 0.0;

            for (int x = -2; x <= 2; x += 2)
            {
                for (int y = -2; y <= 2; y += 2)
                {
                    if (x == 0 && y == 0)
                    {
                        continue;
                    }

                    if (Map.CanFit(point.X + x, point.Y + y, point.Z, 16, false, true, true))
                    {
                        score += 18.0;
                    }
                    else
                    {
                        score -= 24.0;
                    }
                }
            }

            return score;
        }

        private double ScoreRangedDisengageRouteDanger(List<Mobile> threats, Point3D anchor, Point3D point)
        {
            double penalty = 0.0;
            BaseAdventurer observer = GetRangedDisengageObserver();

            for (int i = 0; threats != null && i < threats.Count; i++)
            {
                Mobile threat = threats[i];

                if (threat == null || threat.Deleted || threat.Map != Map)
                {
                    continue;
                }

                penalty += GetRouteThreatPenalty(threat, anchor, point);
            }

            if (observer == null || observer.Map != Map)
            {
                return penalty;
            }

            IPooledEnumerable eable = Map.GetMobilesInRange(
                anchor,
                AdventurePartySettings.RangedDisengageBreakDistance + 16);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer) || !IsRangedPressureEnemy(mobile))
                {
                    continue;
                }

                penalty += GetRouteThreatPenalty(mobile, anchor, point);
            }

            eable.Free();
            return penalty;
        }

        private double GetRouteThreatPenalty(Mobile threat, Point3D start, Point3D end)
        {
            double routeDistance = GetPointToSegmentDistance(threat.Location, start, end);

            if (routeDistance > AdventurePartySettings.RangedDisengageRouteThreatRange)
            {
                return 0.0;
            }

            double penalty = (AdventurePartySettings.RangedDisengageRouteThreatRange - routeDistance + 1.0) * 90.0;

            if (IsDragonBreathThreat(threat))
            {
                penalty *= 1.8;
            }

            if (CanEnemyNoticePoint(threat, end))
            {
                penalty += 180.0;
            }

            return penalty;
        }

        private void ExecuteRangedDisengage(Point3D escapePoint)
        {
            BaseAdventurer leader = GetRangedDisengageObserver();

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) && !IsRecoverableDeadMember(member))
                {
                    continue;
                }

                if (IsActive(member))
                {
                    member.Combatant = null;
                    member.Warmode = false;
                }

                Point3D formationAnchor = GetRangedDisengageFormationAnchor(member, leader, escapePoint);
                Point3D formationPoint = GetRangedDisengageFormationPoint(formationAnchor, member.Role);
                int steps = AdventurePartySettings.RangedDisengageRunSteps;

                if (member != leader && IsActive(leader) &&
                    !member.InRange(leader, AdventurePartySettings.RangedDisengageFormationRegroupDistance))
                {
                    formationPoint = GetRangedDisengageFormationPoint(leader.Location, member.Role);
                    steps = AdventurePartySettings.RecoveryRegroupRunSteps;
                }

                MoveMemberToward(
                    member,
                    formationPoint,
                    steps,
                    true,
                    true);
            }
        }

        private Point3D GetRangedDisengageFormationAnchor(BaseAdventurer member, BaseAdventurer leader, Point3D escapePoint)
        {
            if (member == leader || !IsActive(leader))
            {
                return escapePoint;
            }

            if (!leader.InRange(escapePoint, AdventurePartySettings.RangedDisengageFormationMaxDistance))
            {
                return leader.Location;
            }

            return escapePoint;
        }

        private static Point3D GetRangedDisengageFormationPoint(Point3D escapePoint, AdventurePartyRole role)
        {
            switch (role)
            {
                case AdventurePartyRole.Archer:
                    return new Point3D(escapePoint.X - 1, escapePoint.Y + 1, escapePoint.Z);
                case AdventurePartyRole.Mage:
                    return new Point3D(escapePoint.X + 1, escapePoint.Y + 1, escapePoint.Z);
                case AdventurePartyRole.Healer:
                    return new Point3D(escapePoint.X, escapePoint.Y + 2, escapePoint.Z);
                default:
                    return escapePoint;
            }
        }

        private bool IsFighterPressureBreakNeeded(BaseAdventurer member)
        {
            return IsActive(member) &&
                member.Role == AdventurePartyRole.Fighter &&
                GetHitPercent(member) < AdventurePartySettings.FighterPressureBreakPursuitHitPercent &&
                CountDirectPursuers(member, AdventurePartySettings.PullBreakPursuitScanRange) >= AdventurePartySettings.FighterPressureBreakPursuitDirectThreats;
        }

        private void SetTactic(AdventurePartyTactic tactic, Mobile target)
        {
            m_CurrentTactic = tactic;
            m_TacticExpires = DateTime.UtcNow + AdventurePartySettings.TacticDuration;
            m_TacticTarget = target == null ? "" : target.Serial.ToString();

            BaseAdventurer leader = GetLeader();
            m_TacticPoint = tactic == AdventurePartyTactic.PullToChokePoint ? FindPullAnchor(target, leader) : leader == null ? Location : leader.Location;
        }

        private void SetRecoveryTactic(BaseAdventurer target, Point3D rally)
        {
            m_CurrentTactic = AdventurePartyTactic.Recovering;
            m_TacticExpires = DateTime.UtcNow + AdventurePartySettings.TacticDuration;
            m_TacticTarget = target == null ? "" : target.Serial.ToString();
            m_TacticPoint = rally;
            ClearBreakPursuitPending();
            ClearResetPullState();
        }

        private Point3D FindPullAnchor(Mobile target, BaseAdventurer leader)
        {
            Point3D fallback = leader == null ? Location : leader.Location;
            m_LastPullAnchorStatus = "using fallback anchor.";

            if (leader == null || Map == null || Map == Map.Internal)
            {
                return fallback;
            }

            List<Mobile> enemies = BuildPullEnemyList(leader, target);
            List<Point3D> candidates = BuildPullAnchorCandidates(leader, target, fallback);
            Point3D best = fallback;
            double bestScore = Double.MaxValue;
            string bestStatus = "";

            for (int i = 0; i < candidates.Count; i++)
            {
                Point3D candidate;

                if (!TryCreatePullAnchorPoint(leader, candidates[i], out candidate))
                {
                    continue;
                }

                double score = ScorePullAnchor(leader, target, enemies, candidate, out bestStatus);

                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                    m_LastPullAnchorStatus = bestStatus;
                }
            }

            return best;
        }

        private List<Mobile> BuildPullEnemyList(BaseAdventurer leader, Mobile target)
        {
            List<Mobile> enemies = new List<Mobile>();

            AddPullEnemy(enemies, leader, target);

            IPooledEnumerable eable = leader.GetMobilesInRange(AdventurePartySettings.PullAnchorEnemyScanRange);

            foreach (Mobile mobile in eable)
            {
                AddPullEnemy(enemies, leader, mobile);
            }

            eable.Free();

            if (target != null && !target.Deleted && target.Map == Map)
            {
                eable = target.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

                foreach (Mobile mobile in eable)
                {
                    AddPullEnemy(enemies, leader, mobile);
                }

                eable.Free();
            }

            return enemies;
        }

        private void AddPullEnemy(List<Mobile> enemies, BaseAdventurer observer, Mobile mobile)
        {
            if (!IsValidEnemy(mobile, observer) || enemies.Contains(mobile))
            {
                return;
            }

            enemies.Add(mobile);
        }

        private List<Point3D> BuildPullAnchorCandidates(BaseAdventurer leader, Mobile target, Point3D fallback)
        {
            List<Point3D> candidates = new List<Point3D>();

            AddPullAnchorCandidate(candidates, fallback);

            if (m_Waypoints != null)
            {
                for (int i = 0; i < m_Waypoints.Length; i++)
                {
                    if (GetPointDistance(leader.Location, m_Waypoints[i]) <= AdventurePartySettings.PullAnchorWaypointMaxDistance)
                    {
                        AddPullAnchorCandidate(candidates, m_Waypoints[i]);
                    }
                }
            }

            for (int distance = 4; distance <= AdventurePartySettings.PullAnchorSearchRadius; distance += 2)
            {
                for (int i = 0; i < 8; i++)
                {
                    int xOffset;
                    int yOffset;

                    GetDirectionOffset((Direction)i, out xOffset, out yOffset);
                    AddPullAnchorCandidate(candidates, new Point3D(leader.X + (xOffset * distance), leader.Y + (yOffset * distance), leader.Z));
                }
            }

            if (target != null && !target.Deleted && target.Map == Map)
            {
                int awayX = Math.Sign(leader.X - target.X);
                int awayY = Math.Sign(leader.Y - target.Y);

                if (awayX == 0 && awayY == 0)
                {
                    awayY = 1;
                }

                int sideX = -awayY;
                int sideY = awayX;

                for (int distance = 6; distance <= AdventurePartySettings.PullAnchorSearchRadius + 4; distance += 4)
                {
                    AddPullAnchorCandidate(candidates, new Point3D(leader.X + (awayX * distance), leader.Y + (awayY * distance), leader.Z));
                    AddPullAnchorCandidate(candidates, new Point3D(leader.X + (awayX * distance) + (sideX * 3), leader.Y + (awayY * distance) + (sideY * 3), leader.Z));
                    AddPullAnchorCandidate(candidates, new Point3D(leader.X + (awayX * distance) - (sideX * 3), leader.Y + (awayY * distance) - (sideY * 3), leader.Z));
                }
            }

            return candidates;
        }

        private static void AddPullAnchorCandidate(List<Point3D> candidates, Point3D point)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                if (GetPointDistance(candidates[i], point) <= 1.0)
                {
                    return;
                }
            }

            candidates.Add(point);
        }

        private bool TryCreatePullAnchorPoint(BaseAdventurer leader, Point3D raw, out Point3D point)
        {
            point = Point3D.Zero;

            if (leader == null || Map == null || Map == Map.Internal)
            {
                return false;
            }

            if (TryUsePullAnchorPoint(leader, raw, out point))
            {
                return true;
            }

            int averageZ = Map.GetAverageZ(raw.X, raw.Y);

            if (averageZ != raw.Z && TryUsePullAnchorPoint(leader, new Point3D(raw.X, raw.Y, averageZ), out point))
            {
                return true;
            }

            if (leader.Z != raw.Z && leader.Z != averageZ &&
                TryUsePullAnchorPoint(leader, new Point3D(raw.X, raw.Y, leader.Z), out point))
            {
                return true;
            }

            if (Z != raw.Z && Z != averageZ && Z != leader.Z &&
                TryUsePullAnchorPoint(leader, new Point3D(raw.X, raw.Y, Z), out point))
            {
                return true;
            }

            return false;
        }

        private bool TryUsePullAnchorPoint(BaseAdventurer leader, Point3D candidate, out Point3D point)
        {
            point = Point3D.Zero;

            if (!Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16, false, true, true, leader))
            {
                return false;
            }

            point = candidate;
            return true;
        }

        private double ScorePullAnchor(BaseAdventurer leader, Mobile target, List<Mobile> enemies, Point3D point, out string status)
        {
            double leaderDistance = GetPointDistance(leader.Location, point);
            double nearestEnemyDistance = GetNearestPullEnemyDistance(enemies, point);
            double targetDistance = target == null ? nearestEnemyDistance : GetPointDistance(target.Location, point);
            int localEnemies = CountPullEnemiesNearPoint(leader, point, AdventurePartySettings.PullAnchorLocalEnemyRange);
            int noticingEnemies = CountPullEnemiesThatCanNoticePoint(enemies, point);
            int formationSlots = CountPullFormationSlots(leader, target, point);
            int livingMembers = CountLivingMembers();
            int missingSlots = Math.Max(0, livingMembers - formationSlots);
            bool breaksLineOfSight = target != null && target.Map == Map && !Map.LineOfSight(point, target.Location);
            double score = 0.0;

            score += leaderDistance * 2.0;

            if (leaderDistance > AdventurePartySettings.PullAnchorMaxLeaderDistance)
            {
                score += (leaderDistance - AdventurePartySettings.PullAnchorMaxLeaderDistance) * 18.0;
            }

            if (nearestEnemyDistance < AdventurePartySettings.PullerDangerRange)
            {
                score += 240.0;
            }

            if (nearestEnemyDistance < AdventurePartySettings.PullAnchorMinimumEnemyDistance)
            {
                score += (AdventurePartySettings.PullAnchorMinimumEnemyDistance - nearestEnemyDistance) * 42.0;
            }
            else if (nearestEnemyDistance >= AdventurePartySettings.PullAnchorPreferredEnemyDistance)
            {
                score -= Math.Min(36.0, (nearestEnemyDistance - AdventurePartySettings.PullAnchorPreferredEnemyDistance + 1.0) * 4.0);
            }

            score += localEnemies * 85.0;
            score += noticingEnemies * 28.0;
            score += missingSlots * 55.0;
            score -= formationSlots * 7.0;

            if (localEnemies == 0)
            {
                score -= 22.0;
            }

            if (noticingEnemies == 0)
            {
                score -= 18.0;
            }

            if (breaksLineOfSight)
            {
                score -= 16.0;
            }

            if (target != null && target.Map == Map)
            {
                double currentTargetDistance = leader.GetDistanceToSqrt(target);

                if (targetDistance > currentTargetDistance + 2.0)
                {
                    score -= Math.Min(32.0, (targetDistance - currentTargetDistance) * 4.0);
                }
                else if (targetDistance + 1.0 < currentTargetDistance)
                {
                    score += (currentTargetDistance - targetDistance) * 12.0;
                }

                if (targetDistance < AdventurePartySettings.PullChokeKillZoneRange)
                {
                    score += (AdventurePartySettings.PullChokeKillZoneRange - targetDistance) * 35.0;
                }
            }

            status = String.Format(
                "safe anchor=({0},{1},{2}), leaderDist={3:0.0}, targetDist={4:0.0}, nearbyEnemies={5}, noticingEnemies={6}, formationSlots={7}, losBreak={8}, score={9:0.0}",
                point.X,
                point.Y,
                point.Z,
                leaderDistance,
                targetDistance >= Double.MaxValue / 2.0 ? 99.0 : targetDistance,
                localEnemies,
                noticingEnemies,
                formationSlots,
                breaksLineOfSight ? "yes" : "no",
                score);

            return score;
        }

        private double GetNearestPullEnemyDistance(List<Mobile> enemies, Point3D point)
        {
            double nearest = Double.MaxValue;

            for (int i = 0; enemies != null && i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy == null || enemy.Deleted || !enemy.Alive || enemy.Map != Map)
                {
                    continue;
                }

                nearest = Math.Min(nearest, GetPointDistance(point, enemy.Location));
            }

            return nearest;
        }

        private int CountPullEnemiesNearPoint(BaseAdventurer observer, Point3D point, int range)
        {
            int count = 0;
            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, observer))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountPullEnemiesThatCanNoticePoint(List<Mobile> enemies, Point3D point)
        {
            int count = 0;

            for (int i = 0; enemies != null && i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy == null || enemy.Deleted || !enemy.Alive || enemy.Map != Map)
                {
                    continue;
                }

                if (CanEnemyNoticePoint(enemy, point))
                {
                    count++;
                }
            }

            return count;
        }

        private bool CanEnemyNoticePoint(Mobile enemy, Point3D point)
        {
            if (enemy == null || enemy.Deleted || !enemy.Alive || enemy.Map != Map)
            {
                return false;
            }

            double distance = GetPointDistance(point, enemy.Location);

            if (distance <= AdventurePartySettings.BacklineThreatRange)
            {
                return true;
            }

            return distance <= GetEnemyPerceptionRange(enemy) && Map.LineOfSight(point, enemy.Location);
        }

        private static int GetEnemyPerceptionRange(Mobile enemy)
        {
            BaseCreature creature = enemy as BaseCreature;

            if (creature != null && creature.RangePerception > 0)
            {
                return creature.RangePerception;
            }

            return AdventurePartySettings.PerceptionRange;
        }

        private int CountPullFormationSlots(BaseAdventurer leader, Mobile target, Point3D anchor)
        {
            int slots = 0;
            Point3D threat = target == null ? Point3D.Zero : target.Location;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                Point3D raw = GetPullFormationPoint(anchor, member.Role, threat);
                Point3D point;

                if (TryCreatePullAnchorPoint(leader, raw, out point))
                {
                    slots++;
                }
            }

            return slots;
        }

        private void ClearTactic()
        {
            m_CurrentTactic = AdventurePartyTactic.Standard;
            m_TacticExpires = DateTime.MinValue;
            m_TacticTarget = "";
            m_TacticPoint = Point3D.Zero;
            ClearBreakPursuitPending();
            ClearResetPullState();
            ClearRangedDisengageState();
        }

        private void ClearExpiredTactic()
        {
            if (m_CurrentTactic != AdventurePartyTactic.Standard && m_TacticExpires != DateTime.MinValue &&
                DateTime.UtcNow >= m_TacticExpires)
            {
                ClearTactic();
            }
        }

        private Mobile ResolveDecisionTarget(AdventurePartyAIDecision decision, Mobile fallback, bool preferWeakest)
        {
            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return null;
            }

            string target = decision == null ? "" : decision.Target;

            if (!String.IsNullOrEmpty(target))
            {
                if (String.Equals(target, "weakest", StringComparison.OrdinalIgnoreCase))
                {
                    return FindBestEnemy(true);
                }

                if (!String.Equals(target, "nearest", StringComparison.OrdinalIgnoreCase) &&
                    !String.Equals(target, "none", StringComparison.OrdinalIgnoreCase))
                {
                    Mobile serialTarget = FindMobileBySerial(target);

                    if (IsValidEnemy(serialTarget, leader))
                    {
                        return serialTarget;
                    }
                }
            }

            if (preferWeakest)
            {
                Mobile weakest = FindBestEnemy(true);

                if (weakest != null)
                {
                    return weakest;
                }
            }

            if (IsValidEnemy(fallback, leader))
            {
                return fallback;
            }

            return FindBestEnemy(false);
        }

        private Mobile ResolveTacticTarget(Mobile fallback)
        {
            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return null;
            }

            Mobile target = FindMobileBySerial(m_TacticTarget);

            if (IsValidEnemy(target, leader))
            {
                return target;
            }

            if (m_CurrentTactic == AdventurePartyTactic.FocusFire)
            {
                return FindBestEnemy(true);
            }

            if (m_CurrentTactic == AdventurePartyTactic.PowerUp)
            {
                return FindBestEnemy(false);
            }

            if (m_CurrentTactic == AdventurePartyTactic.ProtectHealer)
            {
                Mobile threat = FindThreatNearHealer();

                if (threat != null)
                {
                    return threat;
                }
            }

            if (IsValidEnemy(fallback, leader))
            {
                return fallback;
            }

            return FindBestEnemy(false);
        }

        private Mobile FindMobileBySerial(string value)
        {
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            value = value.Trim();

            try
            {
                int serial;

                if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                {
                    serial = Convert.ToInt32(value.Substring(2), 16);
                }
                else if (!Int32.TryParse(value, out serial))
                {
                    return null;
                }

                return World.FindMobile((Serial)serial);
            }
            catch
            {
                return null;
            }
        }

        private Mobile FindBestEnemy(bool weakest)
        {
            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return null;
            }

            Mobile best = null;
            double bestScore = weakest ? Double.MaxValue : Double.MinValue;
            IPooledEnumerable eable = leader.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, leader))
                {
                    continue;
                }

                double distance = Math.Max(1.0, leader.GetDistanceToSqrt(mobile));
                double score;

                if (weakest)
                {
                    score = mobile.HitsMax > 0 ? (double)mobile.Hits / mobile.HitsMax : 1.0;
                    score += distance * 0.01;

                    if (best == null || score < bestScore)
                    {
                        best = mobile;
                        bestScore = score;
                    }
                }
                else
                {
                    score = 100.0 / distance;

                    BaseCreature creature = mobile as BaseCreature;

                    if (creature != null)
                    {
                        score += creature.Skills[SkillName.Magery].Value * 0.20;
                        score += creature.Skills[SkillName.EvalInt].Value * 0.10;

                        if (creature.HitsMax > 0 && creature.Hits < creature.HitsMax / 2)
                        {
                            score += 20.0;
                        }
                    }

                    if (best == null || score > bestScore)
                    {
                        best = mobile;
                        bestScore = score;
                    }
                }
            }

            eable.Free();

            return best;
        }

        private Mobile FindThreatNearHealer()
        {
            BaseAdventurer healer = GetHealer();

            if (healer == null)
            {
                return null;
            }

            Mobile best = null;
            double bestDistance = Double.MaxValue;
            IPooledEnumerable eable = healer.GetMobilesInRange(6);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, healer))
                {
                    continue;
                }

                double distance = healer.GetDistanceToSqrt(mobile);

                if (best == null || distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }

            eable.Free();

            return best;
        }

        private AdventurePartyAISnapshot BuildAISnapshot(Mobile currentEnemy)
        {
            AdventurePartyAISnapshot snapshot = new AdventurePartyAISnapshot();
            BaseAdventurer leader = GetLeader();
            Region region = leader != null ? leader.Region : Region.Find(Location, Map);

            snapshot.State = m_State.ToString();
            snapshot.Tactic = m_CurrentTactic.ToString();
            snapshot.MapName = Map == null ? "" : Map.ToString();
            snapshot.RegionName = region == null ? "" : region.Name;
            snapshot.Location = String.Format("{0},{1},{2}", X, Y, Z);
            snapshot.LivingMembers = CountLivingMembers();
            snapshot.CriticalMembers = CountCriticalMembers();

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (member.HitsMax > 0 && member.Hits < (member.HitsMax * 7) / 10)
                {
                    snapshot.InjuredMembers++;
                }

                AdventurePartyAIMemberInfo info = new AdventurePartyAIMemberInfo();

                info.Role = member.Role.ToString();
                info.Hits = member.Hits;
                info.HitsMax = member.HitsMax;
                info.Mana = member.Mana;
                info.ManaMax = member.ManaMax;
                info.DistanceToLeader = leader == null ? 0 : (int)Math.Round(member.GetDistanceToSqrt(leader));
                info.Critical = member.HitsMax > 0 && member.Hits < member.HitsMax / 3;
                info.NearbyEnemies = CountNearbyEnemies(member, member.Role == AdventurePartyRole.Fighter ? 3 : AdventurePartySettings.BacklineThreatRange);
                info.DirectTarget = IsDirectEnemyTarget(member, AdventurePartySettings.PerceptionRange);
                info.UnderPressure = info.DirectTarget || info.NearbyEnemies > 0;
                info.Build = member.CombatProfile.ToString();

                if (member.Role == AdventurePartyRole.Healer)
                {
                    snapshot.HealerThreats = info.NearbyEnemies;
                    snapshot.HealerUnderPressure = info.UnderPressure;
                }
                else if (member.Role == AdventurePartyRole.Fighter)
                {
                    snapshot.FighterThreats = info.NearbyEnemies;
                    snapshot.FighterUnderPressure = info.DirectTarget || info.NearbyEnemies >= 2;
                }
                else
                {
                    snapshot.BacklineThreats += info.NearbyEnemies;

                    if (info.UnderPressure)
                    {
                        snapshot.BacklineUnderPressure = true;
                    }
                }

                snapshot.Members.Add(info);
            }

            AddEnemyToSnapshot(snapshot, currentEnemy, leader);

            if (leader != null)
            {
                IPooledEnumerable eable = leader.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

                foreach (Mobile mobile in eable)
                {
                    if (snapshot.Enemies.Count >= AdventurePartyAIConfig.MaxEnemiesInSnapshot)
                    {
                        break;
                    }

                    if (IsValidEnemy(mobile, leader))
                    {
                        AddEnemyToSnapshot(snapshot, mobile, leader);
                    }
                }

                eable.Free();
            }

            FinalizeAISnapshot(snapshot);

            return snapshot;
        }

        private void AddEnemyToSnapshot(AdventurePartyAISnapshot snapshot, Mobile mobile, BaseAdventurer leader)
        {
            if (snapshot == null || mobile == null || mobile.Deleted || !mobile.Alive)
            {
                return;
            }

            string serial = mobile.Serial.ToString();

            for (int i = 0; i < snapshot.Enemies.Count; i++)
            {
                if (snapshot.Enemies[i].Serial == serial)
                {
                    return;
                }
            }

            AdventurePartyAIEnemyInfo info = new AdventurePartyAIEnemyInfo();

            info.TypeName = mobile.GetType().Name;
            info.Name = mobile.Name;
            info.Serial = serial;
            info.Hits = mobile.Hits;
            info.HitsMax = mobile.HitsMax;
            info.Distance = leader == null ? 0 : (int)Math.Round(leader.GetDistanceToSqrt(mobile));
            info.HardTarget = IsAIHardTarget(mobile);
            info.CasterLike = IsAICasterLike(mobile);

            BaseAdventurer target = mobile.Combatant as BaseAdventurer;

            if (target != null && IsPartyMember(target))
            {
                info.TargetingParty = true;
                info.TargetRole = target.Role.ToString();
            }
            else
            {
                info.TargetRole = "";
            }

            snapshot.Enemies.Add(info);

            if (info.HardTarget)
            {
                snapshot.HardTargets++;
            }
        }

        private void FinalizeAISnapshot(AdventurePartyAISnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            snapshot.EnemyCount = snapshot.Enemies.Count;
            snapshot.DistinctEnemyTypes = CountDistinctEnemyTypes(snapshot);
            snapshot.MixedEnemyTypes = snapshot.DistinctEnemyTypes > 1;
            snapshot.PartyHealthy = snapshot.LivingMembers >= 4 && snapshot.CriticalMembers == 0 && snapshot.InjuredMembers <= 1;
            snapshot.BurstRecommended = snapshot.PartyHealthy &&
                !snapshot.HealerUnderPressure &&
                !snapshot.BacklineUnderPressure &&
                (snapshot.HardTargets > 0 || snapshot.EnemyCount >= 3);
        }

        private int CountDistinctEnemyTypes(AdventurePartyAISnapshot snapshot)
        {
            List<string> types = new List<string>();

            for (int i = 0; snapshot != null && i < snapshot.Enemies.Count; i++)
            {
                string typeName = snapshot.Enemies[i].TypeName;
                bool found = false;

                for (int j = 0; j < types.Count; j++)
                {
                    if (types[j] == typeName)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    types.Add(typeName);
                }
            }

            return types.Count;
        }

        private int CountNearbyEnemies(BaseAdventurer member, int range)
        {
            if (!IsActive(member))
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = member.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, member))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountOtherEnemiesNearParty(Mobile ignoredTarget, int range)
        {
            int count = 0;
            List<Serial> seen = new List<Serial>();

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                IPooledEnumerable eable = member.GetMobilesInRange(range);

                foreach (Mobile mobile in eable)
                {
                    if (mobile == ignoredTarget || !IsValidEnemy(mobile, member) || seen.Contains(mobile.Serial))
                    {
                        continue;
                    }

                    seen.Add(mobile.Serial);
                    count++;
                }

                eable.Free();
            }

            return count;
        }

        private bool IsDirectEnemyTarget(BaseAdventurer member, int range)
        {
            if (!IsActive(member))
            {
                return false;
            }

            IPooledEnumerable eable = member.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, member) && mobile.Combatant == member)
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private bool IsPartyMember(Mobile mobile)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                if (m_Members[i] == mobile)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsAIHardTarget(Mobile mobile)
        {
            if (mobile == null)
            {
                return false;
            }

            int hitsMax = Math.Max(mobile.HitsMax, mobile.Hits);
            BaseCreature creature = mobile as BaseCreature;

            return hitsMax >= 220 || (creature != null && creature.Fame >= 8000);
        }

        private bool IsAICasterLike(Mobile mobile)
        {
            return mobile != null &&
                (mobile.Skills[SkillName.Magery].Value > 60.0 ||
                mobile.Skills[SkillName.EvalInt].Value > 60.0 ||
                mobile.Skills[SkillName.Necromancy].Value > 60.0 ||
                mobile.Skills[SkillName.Spellweaving].Value > 60.0 ||
                mobile.Skills[SkillName.Mysticism].Value > 60.0);
        }

        private void PruneMembers()
        {
            for (int i = m_Members.Count - 1; i >= 0; i--)
            {
                BaseAdventurer member = m_Members[i];

                if (member == null || member.Deleted)
                {
                    m_Members.RemoveAt(i);
                }
            }
        }

        private int CountLivingMembers()
        {
            int count = 0;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountRecoverableDeadMembers()
        {
            int count = 0;

            for (int i = 0; i < m_Members.Count; i++)
            {
                if (IsRecoverableDeadMember(m_Members[i]))
                {
                    count++;
                }
            }

            return count;
        }

        private int CountCriticalMembers()
        {
            int count = 0;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.HitsMax > 0 && member.Hits < member.HitsMax / 3)
                {
                    count++;
                }
            }

            return count;
        }

        private bool ShouldRest()
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.HitsMax > 0 && member.Hits < (member.HitsMax * 7) / 10)
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsActive(BaseAdventurer member)
        {
            return member != null && !member.Deleted && member.Alive && !member.IsDeadPet && member.Map == Map;
        }

        private bool IsRecoverableDeadMember(BaseAdventurer member)
        {
            return member != null && !member.Deleted && member.IsDeadPet && member.Map == Map;
        }

        private BaseAdventurer GetLeader()
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Role == AdventurePartyRole.Fighter)
                {
                    return member;
                }
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member))
                {
                    return member;
                }
            }

            return null;
        }

        private Mobile FindEnemy()
        {
            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return null;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && IsValidEnemy(member.Combatant as Mobile, member))
                {
                    return member.Combatant as Mobile;
                }
            }

            Mobile best = null;
            double bestDistance = double.MaxValue;
            IPooledEnumerable eable = leader.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, leader))
                {
                    double distance = leader.GetDistanceToSqrt(mobile);

                    if (distance < bestDistance)
                    {
                        best = mobile;
                        bestDistance = distance;
                    }
                }
            }

            eable.Free();

            return best;
        }

        private Mobile FindDirectPartyThreat(out BaseAdventurer threatenedMember)
        {
            threatenedMember = null;

            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return null;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                Mobile combatant = member.Combatant as Mobile;

                if (IsValidEnemy(combatant, member) && combatant.Combatant == member)
                {
                    ConsiderDirectPartyThreat(combatant, member, ref best, ref threatenedMember, ref bestScore);
                }
            }

            IPooledEnumerable eable = leader.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, leader))
                {
                    continue;
                }

                BaseAdventurer target = mobile.Combatant as BaseAdventurer;

                if (IsActive(target) && IsPartyMember(target))
                {
                    ConsiderDirectPartyThreat(mobile, target, ref best, ref threatenedMember, ref bestScore);
                }
            }

            eable.Free();
            return best;
        }

        private void ConsiderDirectPartyThreat(
            Mobile threat,
            BaseAdventurer target,
            ref Mobile best,
            ref BaseAdventurer threatenedMember,
            ref double bestScore)
        {
            if (!IsActive(target) || !IsValidEnemy(threat, target))
            {
                return;
            }

            double score = 1000.0 - Math.Max(0.0, target.GetDistanceToSqrt(threat));

            switch (target.Role)
            {
                case AdventurePartyRole.Healer:
                    score += 300.0;
                    break;
                case AdventurePartyRole.Mage:
                    score += 240.0;
                    break;
                case AdventurePartyRole.Archer:
                    score += 220.0;
                    break;
                default:
                    score += 100.0;
                    break;
            }

            if (target.HitsMax > 0)
            {
                score += Math.Max(0.0, 100.0 - ((double)target.Hits * 100.0 / target.HitsMax));
            }

            if (best == null || score > bestScore)
            {
                best = threat;
                threatenedMember = target;
                bestScore = score;
            }
        }

        private bool IsValidEnemy(Mobile mobile, BaseAdventurer observer)
        {
            if (mobile == null || observer == null || mobile.Deleted || !mobile.Alive || mobile.Map != Map)
            {
                return false;
            }

            if (mobile == observer || mobile.Player || mobile is PlayerMobile)
            {
                return false;
            }

            BaseAdventurer adventurer = mobile as BaseAdventurer;

            if (adventurer != null)
            {
                return false;
            }

            BaseCreature creature = mobile as BaseCreature;

            if (creature == null || creature.Controlled || creature.Summoned)
            {
                return false;
            }

            return observer.CanBeHarmful(mobile, false) && observer.IsEnemy(mobile);
        }

        private void ChangeState(AdventurePartyState state)
        {
            if (m_State == state)
            {
                return;
            }

            m_State = state;
            m_StateTicks = 0;

            switch (state)
            {
                case AdventurePartyState.Engaging:
                    PartyMessage("Contact ahead.");
                    break;
                case AdventurePartyState.Retreating:
                    PartyMessage("Fall back and regroup.");
                    break;
                case AdventurePartyState.Resting:
                    PartyMessage("Hold here. Catch your breath.");
                    break;
                default:
                    PartyMessage("Move out.");
                    break;
            }
        }

        private void Explore()
        {
            if (m_Waypoints == null || m_Waypoints.Length == 0)
            {
                BuildDefaultRoute(Location);
            }

            BaseAdventurer leader = GetLeader();

            if (leader == null)
            {
                return;
            }

            Point3D waypoint = m_Waypoints[m_WaypointIndex];

            bool compact = IsExplorationFormationCompact(leader);

            if (leader.InRange(waypoint, 2) && compact)
            {
                m_WaypointIndex = (m_WaypointIndex + 1) % m_Waypoints.Length;
                waypoint = m_Waypoints[m_WaypointIndex];
            }

            MovePartyExploring(leader, waypoint, compact);
            WatchForStuckLeader(leader);
        }

        private void Engage(Mobile enemy)
        {
            enemy = ResolveTacticTarget(enemy);

            if (enemy == null || enemy.Deleted || !enemy.Alive)
            {
                ChangeState(AdventurePartyState.Exploring);
                return;
            }

            switch (m_CurrentTactic)
            {
                case AdventurePartyTactic.ProtectHealer:
                    ProtectHealer(enemy);
                    return;
                case AdventurePartyTactic.Kite:
                    KiteEnemy(enemy);
                    return;
                case AdventurePartyTactic.AvoidAOE:
                    AvoidThreatArea(enemy);
                    return;
                case AdventurePartyTactic.HoldChokePoint:
                    HoldChokePoint(enemy);
                    return;
                case AdventurePartyTactic.PullToChokePoint:
                    PullToChokePoint(enemy);
                    return;
                case AdventurePartyTactic.PowerUp:
                    FocusEnemy(enemy);
                    return;
            }

            FocusEnemy(enemy);
        }

        private void FocusEnemy(Mobile enemy)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);

                    if (!AvoidNearbyBacklineThreat(member, enemy))
                    {
                        if (!TryMoveHealerTowardBandageSupport(member))
                        {
                            KeepNearLeader(member, 6);
                        }
                    }
                }
                else
                {
                    member.Combatant = enemy;

                    MaintainAttackRange(member, enemy);
                }
            }
        }

        private void ProtectHealer(Mobile enemy)
        {
            BaseAdventurer healer = GetHealer();

            if (healer == null)
            {
                FocusEnemy(enemy);
                return;
            }

            Mobile threat = FindThreatNearHealer() ?? enemy;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);

                    if (threat != null && member.InRange(threat, 5))
                    {
                        MoveMemberAwayFrom(member, threat.Location, 7);
                    }
                    else if (TryMoveHealerTowardBandageSupport(member))
                    {
                        continue;
                    }
                    else
                    {
                        KeepNearLeader(member, 7);
                    }
                }
                else
                {
                    member.Combatant = threat;

                    if (member.Role == AdventurePartyRole.Fighter)
                    {
                        MoveMemberToward(member, GetInterceptPoint(healer.Location, threat.Location), GetEngageMoveSteps(member, threat), false, true);
                    }
                    else
                    {
                        MaintainAttackRange(member, threat);
                    }
                }
            }
        }

        private void KiteEnemy(Mobile enemy)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);

                    if (!AvoidNearbyBacklineThreat(member, enemy))
                    {
                        if (!TryMoveHealerTowardBandageSupport(member))
                        {
                            MoveMemberAwayFrom(member, enemy.Location, 8);
                        }
                    }
                }
                else if (member.Role == AdventurePartyRole.Fighter)
                {
                    member.Combatant = enemy;

                    if (member.HitsMax > 0 && member.Hits < (member.HitsMax * 6) / 10)
                    {
                        MoveMemberAwayFrom(member, enemy.Location, 4);
                    }
                    else if (!member.InRange(enemy, 2))
                    {
                        MoveMemberToward(member, enemy.Location, true);
                    }
                }
                else
                {
                    member.Combatant = enemy;

                    if (member.Role == AdventurePartyRole.Archer && AdvancedCombatBrain.IsPlayingTheOddsActive(member))
                    {
                        if (member.InRange(enemy, 3))
                        {
                            MoveMemberAwayFrom(member, enemy.Location, 5);
                        }
                        else if (!member.InRange(enemy, 5))
                        {
                            MoveMemberToward(member, enemy.Location, true);
                        }
                    }
                    else if (member.InRange(enemy, 5))
                    {
                        MoveMemberAwayFrom(member, enemy.Location, 8);
                    }
                    else if (!member.InRange(enemy, 10))
                    {
                        MoveMemberToward(member, enemy.Location, true);
                    }
                }
            }
        }

        private void AvoidThreatArea(Mobile enemy)
        {
            BaseAdventurer leader = GetLeader();
            Point3D anchor = leader == null ? Location : leader.Location;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (enemy != null && member.InRange(enemy, 6))
                {
                    MoveMemberAwayFrom(member, enemy.Location, 8);
                }
                else
                {
                    MoveMemberToward(member, GetSpreadPoint(anchor, member.Role), true);
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);
                }
                else if (enemy != null)
                {
                    member.Combatant = enemy;
                }
            }
        }

        private void HoldChokePoint(Mobile enemy)
        {
            if (m_TacticPoint == Point3D.Zero)
            {
                BaseAdventurer leader = GetLeader();
                m_TacticPoint = leader == null ? Location : leader.Location;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);
                }
                else if (enemy != null)
                {
                    member.Combatant = enemy;
                    MaintainAttackRange(member, enemy);

                    if (member.Role == AdventurePartyRole.Fighter)
                    {
                        continue;
                    }
                }

                if (member.Role != AdventurePartyRole.Fighter && AvoidNearbyBacklineThreat(member, enemy))
                {
                    continue;
                }

                MoveMemberToward(member, GetFormationPoint(m_TacticPoint, member.Role), true);
            }
        }

        private void PullToChokePoint(Mobile enemy)
        {
            if (m_TacticPoint == Point3D.Zero)
            {
                BaseAdventurer leader = GetLeader();
                m_TacticPoint = FindPullAnchor(enemy, leader);
            }

            if (enemy == null || enemy.Deleted || !enemy.Alive)
            {
                ClearResetPullState();
                HoldChokePoint(enemy);
                return;
            }

            bool targetInKillZone = GetPointDistance(enemy.Location, m_TacticPoint) <= AdventurePartySettings.PullChokeKillZoneRange;
            BaseAdventurer pressureFighter = FindActiveMemberByRole(AdventurePartyRole.Fighter);

            if (ShouldFinishFleeingTarget(enemy, pressureFighter))
            {
                ClearResetPullState();
                SetTactic(AdventurePartyTactic.PowerUp, enemy);
                ChangeState(AdventurePartyState.Engaging);
                AssignFinishFleeingTarget(enemy);
                m_LastAIStatus = String.Format(
                    "Finish fleeing target: {0} is low and isolated; leaving PullToChokePoint to chase.",
                    GetDebugName(enemy));
                FocusEnemy(enemy);
                return;
            }

            if (ShouldInterceptIsolatedBacklineTarget(enemy, null))
            {
                ClearResetPullState();
                SetTactic(AdventurePartyTactic.PowerUp, enemy);
                ChangeState(AdventurePartyState.Engaging);
                AssignInterceptionTarget(enemy);
                m_LastAIStatus = String.Format(
                    "Isolated backline intercept: {0} is alone; leaving PullToChokePoint so fighter can take over.",
                    GetDebugName(enemy));
                FocusEnemy(enemy);
                return;
            }

            bool fighterPressureBreak = IsFighterPressureBreakNeeded(pressureFighter);
            BaseAdventurer puller = fighterPressureBreak ? pressureFighter : FindPuller();

            if ((targetInKillZone && !fighterPressureBreak) || puller == null)
            {
                ClearResetPullState();
                HoldChokePoint(enemy);
                return;
            }

            if (TryForceOverpulledReset(puller, enemy, fighterPressureBreak))
            {
                return;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                if (IsBreakPursuitSupportCaster(member))
                {
                    continue;
                }

                if (member == puller)
                {
                    if (!TryBreakPursuit(member, enemy))
                    {
                        PullTarget(member, enemy);
                    }

                    continue;
                }

                if (member.Role == AdventurePartyRole.Healer)
                {
                    HealMostInjured(member);

                    if (AvoidNearbyBacklineThreat(member, enemy))
                    {
                        continue;
                    }

                    MoveMemberToward(member, GetFormationPoint(m_TacticPoint, member.Role), true);
                    continue;
                }

                if (member.Role != AdventurePartyRole.Fighter && AvoidNearbyBacklineThreat(member, enemy))
                {
                    continue;
                }

                member.Combatant = null;
                MoveMemberToward(member, GetFormationPoint(m_TacticPoint, member.Role), true);
            }
        }

        private void PullTarget(BaseAdventurer puller, Mobile enemy)
        {
            if (!IsActive(puller) || enemy == null || enemy.Deleted || !enemy.Alive)
            {
                return;
            }

            puller.Combatant = enemy;

            double anchorDistance = GetPointDistance(puller.Location, m_TacticPoint);

            if (anchorDistance > AdventurePartySettings.PullerMaxDistanceFromAnchor ||
                puller.InRange(enemy, AdventurePartySettings.PullerDangerRange))
            {
                MoveMemberToward(puller, GetFormationPoint(m_TacticPoint, puller.Role), GetBreakPursuitMoveSteps(puller), true, true);
                return;
            }

            int desiredRange = GetDesiredAttackRange(puller);

            if (!puller.InRange(enemy, desiredRange))
            {
                MoveMemberToward(puller, enemy.Location, true);
            }
            else
            {
                MaintainAttackRange(puller, enemy);
            }
        }

        private bool TryBreakPursuit(BaseAdventurer puller, Mobile enemy)
        {
            if (!IsActive(puller) || enemy == null || enemy.Deleted || enemy.Map != Map)
            {
                return false;
            }

            if (ResolveBreakPursuitPendingTarget())
            {
                return true;
            }

            if (m_ResetPullActive)
            {
                return ContinueResetPull(puller, enemy);
            }

            int pursuers = CountPullPursuers(puller, enemy, AdventurePartySettings.PullBreakPursuitScanRange);
            bool fighterPressureBreak = IsFighterPressureBreakNeeded(puller);

            if (puller.Hidden)
            {
                puller.Combatant = null;
                puller.Warmode = false;

                if (CountDirectPursuers(puller, AdventurePartySettings.PullBreakPursuitScanRange) > 0)
                {
                    m_LastBreakPursuitStatus = "BreakPursuit: hidden and waiting for pursuers to lose target.";
                    m_LastAIStatus = m_LastBreakPursuitStatus;
                    return true;
                }

                BeginResetPull(puller, enemy);
                return true;
            }

            if (!fighterPressureBreak &&
                (pursuers < AdventurePartySettings.PullBreakPursuitEnemyCount ||
                GetPointDistance(enemy.Location, m_TacticPoint) <= AdventurePartySettings.PullChokeKillZoneRange + 2.0))
            {
                return false;
            }

            Point3D breakPoint = GetBreakPursuitPoint(puller, enemy);

            SetBreakPursuitCombatant(puller, enemy, fighterPressureBreak);

            if (!puller.InRange(breakPoint, 1))
            {
                MoveMemberToward(puller, breakPoint, GetBreakPursuitMoveSteps(puller), true, true);
                m_LastBreakPursuitStatus = fighterPressureBreak ?
                    String.Format("BreakPursuit: fighter pressure emergency with {0} pursuer(s), moving to reset point ({1},{2},{3}).", pursuers, breakPoint.X, breakPoint.Y, breakPoint.Z) :
                    String.Format("BreakPursuit: {0} pursuer(s), moving to reset point ({1},{2},{3}).", pursuers, breakPoint.X, breakPoint.Y, breakPoint.Z);
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            if (TrySupportBreakPursuitInvisibility(puller, pursuers))
            {
                return true;
            }

            if (TryUseBreakPursuitHiding(puller))
            {
                m_LastBreakPursuitStatus = String.Format(
                    "BreakPursuit: {0} pursuer(s), Hiding attempted at skill {1:0.0}, hidden={2}.",
                    pursuers,
                    puller.Skills[SkillName.Hiding].Value,
                    puller.Hidden ? "yes" : "no");
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            MoveMemberToward(puller, breakPoint, GetBreakPursuitMoveSteps(puller), true, true);
            m_LastBreakPursuitStatus = String.Format(
                "BreakPursuit: {0} pursuer(s), no reliable invisibility/hiding option yet; continuing to retreat.",
                pursuers);
            m_LastAIStatus = m_LastBreakPursuitStatus;
            return true;
        }

        private bool TryForceOverpulledReset(BaseAdventurer puller, Mobile enemy, bool fighterPressureBreak)
        {
            if (!IsActive(puller) || enemy == null || enemy.Deleted || enemy.Map != Map || m_ResetPullActive)
            {
                return false;
            }

            int scanRange = AdventurePartySettings.PullBreakPursuitScanRange + 4;
            int pursuers = CountPullPursuers(puller, enemy, scanRange);
            int directPartyTargets = CountPullPartyTargets(puller, scanRange, false);
            int backlineTargets = CountPullPartyTargets(puller, scanRange, true);
            int rangedThreats = CountRangedPressureThreats(
                puller,
                Math.Max(scanRange, AdventurePartySettings.PullRangedThreatScanRange));
            bool tooManyEnemies = pursuers >= AdventurePartySettings.PullHardResetEnemyCount;
            bool backlinePinned = backlineTargets >= AdventurePartySettings.PullHardResetBacklineTargets &&
                pursuers >= AdventurePartySettings.PullBreakPursuitEnemyCount;
            bool killZoneSwamped = CountPullEnemiesNearPoint(puller, m_TacticPoint, AdventurePartySettings.PullChokeKillZoneRange) >=
                AdventurePartySettings.PullHardResetEnemyCount;
            bool partyPinned = directPartyTargets >= AdventurePartySettings.PullHardResetDirectPartyTargets &&
                CountCriticalMembers() > 0;
            bool fighterEmergencyPack = fighterPressureBreak &&
                pursuers >= AdventurePartySettings.PullBreakPursuitEnemyCount;
            bool rangedCrossfire = rangedThreats >= AdventurePartySettings.PullHardResetRangedThreats;

            if (!tooManyEnemies && !backlinePinned && !killZoneSwamped && !partyPinned && !fighterEmergencyPack && !rangedCrossfire)
            {
                return false;
            }

            ForceOverpulledReset(puller, enemy, pursuers, directPartyTargets, backlineTargets, rangedThreats);
            return true;
        }

        private void ForceOverpulledReset(BaseAdventurer puller, Mobile enemy, int pursuers, int directPartyTargets, int backlineTargets, int rangedThreats)
        {
            Point3D breakPoint = GetBreakPursuitPoint(puller, enemy);
            Point3D threat = enemy == null || enemy.Map != Map ? Point3D.Zero : enemy.Location;

            BeginResetPull(puller, enemy);

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                member.Combatant = null;
                member.Warmode = false;

                if (member == puller || member.Role == AdventurePartyRole.Fighter)
                {
                    MoveMemberToward(member, breakPoint, AdventurePartySettings.PullHardResetRunSteps, true, true);
                    continue;
                }

                Mobile selfThreat = FindSelfPreservationThreat(member, enemy, AdventurePartySettings.PullBreakPursuitScanRange);

                if (selfThreat != null && member.InRange(selfThreat, AdventurePartySettings.BacklineMeleeThreatRange))
                {
                    MoveMemberAwayFrom(member, selfThreat.Location, AdventurePartySettings.BacklineThreatSafeRange + 2, true, true);
                    continue;
                }

                MoveMemberToward(
                    member,
                    GetPullFormationPoint(breakPoint, member.Role, threat),
                    AdventurePartySettings.BacklineEvasiveMoveSteps,
                    true,
                    true);
            }

            m_LastBreakPursuitStatus = String.Format(
                "Overpull reset: {0} pursuer(s), {1} party target(s), {2} backline target(s), {3} ranged threat(s); forcing ResetPull.",
                pursuers,
                directPartyTargets,
                backlineTargets,
                rangedThreats);
            m_LastAIStatus = m_LastBreakPursuitStatus;
        }

        private void SetBreakPursuitCombatant(BaseAdventurer puller, Mobile enemy, bool fighterPressureBreak)
        {
            if (!IsActive(puller))
            {
                return;
            }

            if (!fighterPressureBreak)
            {
                puller.Combatant = null;
                puller.Warmode = false;
                return;
            }

            Mobile threat = FindBestPursuitThreat(puller, enemy, AdventurePartySettings.PullBreakPursuitScanRange);

            if (threat != null)
            {
                puller.Combatant = threat;
                puller.Warmode = true;
            }
            else
            {
                puller.Combatant = null;
                puller.Warmode = false;
            }
        }

        private Mobile FindBestPursuitThreat(BaseAdventurer puller, Mobile fallback, int range)
        {
            if (!IsActive(puller))
            {
                return fallback;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = puller.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, puller))
                {
                    continue;
                }

                bool direct = mobile.Combatant == puller;
                bool fallbackTarget = mobile == fallback;

                if (!direct && !fallbackTarget)
                {
                    continue;
                }

                double distance = Math.Max(0.5, puller.GetDistanceToSqrt(mobile));
                double score = 30.0 - distance;

                if (direct)
                {
                    score += 25.0;
                }

                if (fallbackTarget)
                {
                    score += 8.0;
                }

                if (mobile.HitsMax > 0)
                {
                    score += (100.0 - ((double)mobile.Hits * 100.0 / (double)mobile.HitsMax)) * 0.1;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            eable.Free();
            return best ?? fallback;
        }

        private void BeginResetPull(BaseAdventurer puller, Mobile enemy)
        {
            m_ResetPullActive = true;
            m_ResetPullOriginalTarget = enemy == null ? Serial.Zero : enemy.Serial;
            m_ResetPullStarted = DateTime.UtcNow;
            m_ResetPullSettleUntil = m_ResetPullStarted + AdventurePartySettings.PullResetSettleTime;

            if (puller != null)
            {
                puller.Combatant = null;
                puller.Warmode = false;
            }

            m_LastBreakPursuitStatus = "ResetPull: pursuers lost direct target; waiting for the pack to turn back.";
            m_LastAIStatus = m_LastBreakPursuitStatus;
        }

        private bool ContinueResetPull(BaseAdventurer puller, Mobile enemy)
        {
            if (!IsActive(puller))
            {
                ClearResetPullState();
                return false;
            }

            puller.Combatant = null;
            puller.Warmode = false;

            Point3D breakPoint = GetBreakPursuitPoint(puller, enemy);

            if (!puller.InRange(breakPoint, 1))
            {
                MoveMemberToward(puller, breakPoint, GetBreakPursuitMoveSteps(puller), true, true);
                m_LastBreakPursuitStatus = String.Format("ResetPull: returning puller to reset point ({0},{1},{2}).", breakPoint.X, breakPoint.Y, breakPoint.Z);
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            int directPursuers = CountDirectPursuers(puller, AdventurePartySettings.PullBreakPursuitScanRange);
            int localEnemies = CountPullEnemiesNearPoint(puller, puller.Location, AdventurePartySettings.PullResetLocalEnemyRange);

            if (directPursuers > 0)
            {
                m_ResetPullSettleUntil = DateTime.UtcNow + AdventurePartySettings.PullResetSettleTime;
                m_LastBreakPursuitStatus = String.Format("ResetPull: waiting; {0} monster(s) still directly targeting puller.", directPursuers);
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            if (localEnemies > 0)
            {
                m_ResetPullSettleUntil = DateTime.UtcNow + AdventurePartySettings.PullResetSettleTime;
                m_LastBreakPursuitStatus = String.Format("ResetPull: waiting; {0} monster(s) still too close to reset point.", localEnemies);
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            if (DateTime.UtcNow < m_ResetPullSettleUntil)
            {
                double seconds = Math.Max(0.0, (m_ResetPullSettleUntil - DateTime.UtcNow).TotalSeconds);
                m_LastBreakPursuitStatus = String.Format("ResetPull: holding for {0:0.0}s before retagging.", seconds);
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            bool allowLoosePack = DateTime.UtcNow >= m_ResetPullStarted + AdventurePartySettings.PullResetMaxWaitTime;
            Mobile nextTarget = FindResetPullTarget(puller, enemy, allowLoosePack);

            if (nextTarget == null)
            {
                m_LastBreakPursuitStatus = allowLoosePack ?
                    "ResetPull: no good retag target found; staying hidden and waiting." :
                    "ResetPull: waiting for a more isolated retag target.";
                m_LastAIStatus = m_LastBreakPursuitStatus;
                return true;
            }

            m_TacticTarget = nextTarget.Serial.ToString();
            ClearResetPullState();

            if (puller.Hidden)
            {
                puller.RevealingAction();
            }

            puller.Combatant = nextTarget;
            m_LastBreakPursuitStatus = String.Format(
                "ResetPull: retagging {0} with {1} nearby packmate(s).",
                nextTarget.GetType().Name,
                CountResetPullNeighbors(puller, null, nextTarget, AdventurePartySettings.PullResetTargetNeighborRange));
            m_LastAIStatus = m_LastBreakPursuitStatus;
            return true;
        }

        private void ClearResetPullState()
        {
            m_ResetPullActive = false;
            m_ResetPullOriginalTarget = Serial.Zero;
            m_ResetPullStarted = DateTime.MinValue;
            m_ResetPullSettleUntil = DateTime.MinValue;
        }

        private bool ResolveBreakPursuitPendingTarget()
        {
            if (m_BreakPursuitPendingCaster == Serial.Zero)
            {
                return false;
            }

            if (DateTime.UtcNow > m_BreakPursuitPendingUntil)
            {
                ClearBreakPursuitPending();
                return false;
            }

            BaseAdventurer caster = World.FindMobile(m_BreakPursuitPendingCaster) as BaseAdventurer;
            BaseAdventurer target = World.FindMobile(m_BreakPursuitPendingTarget) as BaseAdventurer;

            if (!IsActive(caster) || !IsActive(target))
            {
                ClearBreakPursuitPending();
                return false;
            }

            caster.Combatant = null;
            caster.Warmode = false;
            target.Combatant = null;
            target.Warmode = false;

            if (caster.Target == null)
            {
                return true;
            }

            caster.Target.Invoke(caster, target);
            ClearBreakPursuitPending();
            m_LastBreakPursuitStatus = "BreakPursuit: Invisibility target applied to puller by support caster.";
            m_LastAIStatus = m_LastBreakPursuitStatus;
            return true;
        }

        private void ClearBreakPursuitPending()
        {
            m_BreakPursuitPendingCaster = Serial.Zero;
            m_BreakPursuitPendingTarget = Serial.Zero;
            m_BreakPursuitPendingUntil = DateTime.MinValue;
            m_BreakPursuitSupportCaster = Serial.Zero;
            m_BreakPursuitSupportUntil = DateTime.MinValue;
        }

        private bool IsBreakPursuitSupportCaster(BaseAdventurer member)
        {
            if (member == null)
            {
                return false;
            }

            DateTime now = DateTime.UtcNow;

            if ((m_BreakPursuitPendingCaster != Serial.Zero && now <= m_BreakPursuitPendingUntil && member.Serial == m_BreakPursuitPendingCaster) ||
                (m_BreakPursuitSupportCaster != Serial.Zero && now <= m_BreakPursuitSupportUntil && member.Serial == m_BreakPursuitSupportCaster))
            {
                return true;
            }

            if (m_BreakPursuitSupportCaster != Serial.Zero && now > m_BreakPursuitSupportUntil)
            {
                m_BreakPursuitSupportCaster = Serial.Zero;
                m_BreakPursuitSupportUntil = DateTime.MinValue;
            }

            return false;
        }

        private bool TrySupportBreakPursuitInvisibility(BaseAdventurer puller, int pursuers)
        {
            BaseAdventurer caster = FindBreakPursuitInvisibilityCaster(puller);

            if (caster == null)
            {
                return false;
            }

            caster.Combatant = null;
            caster.Warmode = false;

            if (!caster.InRange(puller, 10) || !caster.CanSee(puller))
            {
                if (caster != puller)
                {
                    MoveMemberToward(caster, puller.Location, true);
                    m_BreakPursuitSupportCaster = caster.Serial;
                    m_BreakPursuitSupportUntil = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
                    m_LastBreakPursuitStatus = String.Format(
                        "BreakPursuit: {0} pursuer(s), {1} moving to cast Invisibility on puller.",
                        pursuers,
                        caster.Role);
                    m_LastAIStatus = m_LastBreakPursuitStatus;
                    return true;
                }

                return false;
            }

            InvisibilitySpell spell = new InvisibilitySpell(caster, null);

            if (!spell.Cast())
            {
                return false;
            }

            m_BreakPursuitPendingCaster = caster.Serial;
            m_BreakPursuitPendingTarget = puller.Serial;
            m_BreakPursuitPendingUntil = DateTime.UtcNow + AdventurePartySettings.PullBreakPursuitPendingTargetTime;
            caster.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(4.0);
            puller.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
            m_LastBreakPursuitStatus = String.Format(
                "BreakPursuit: {0} pursuer(s), {1} casting Invisibility on puller.",
                pursuers,
                caster == puller ? "puller" : caster.Role.ToString());
            m_LastAIStatus = m_LastBreakPursuitStatus;
            return true;
        }

        private BaseAdventurer FindBreakPursuitInvisibilityCaster(BaseAdventurer puller)
        {
            if (CanCastBreakPursuitInvisibility(puller))
            {
                return puller;
            }

            BaseAdventurer caster = FindActiveMemberByRole(AdventurePartyRole.Mage);

            if (CanCastBreakPursuitInvisibility(caster))
            {
                return caster;
            }

            caster = FindActiveMemberByRole(AdventurePartyRole.Healer);

            if (CanCastBreakPursuitInvisibility(caster))
            {
                return caster;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                caster = m_Members[i];

                if (caster != puller && CanCastBreakPursuitInvisibility(caster))
                {
                    return caster;
                }
            }

            return null;
        }

        private static bool CanCastBreakPursuitInvisibility(BaseAdventurer caster)
        {
            return caster != null &&
                caster.NextPartyAction <= DateTime.UtcNow &&
                caster.Spell == null &&
                caster.Skills[SkillName.Magery].Value >= AdventurePartySettings.PullBreakPursuitMageryMinimum &&
                caster.Mana >= 40;
        }

        private bool TryUseBreakPursuitHiding(BaseAdventurer puller)
        {
            if (!IsActive(puller) || puller.NextPartyAction > DateTime.UtcNow ||
                puller.Skills[SkillName.Hiding].Value < AdventurePartySettings.PullBreakPursuitHidingMinimum ||
                WouldHidingBeBlockedByCombat(puller))
            {
                return false;
            }

            puller.Combatant = null;
            puller.Warmode = false;

            bool used = puller.UseSkill(SkillName.Hiding);

            if (used)
            {
                puller.NextPartyAction = DateTime.UtcNow + AdventurePartySettings.PullBreakPursuitAttemptDelay;
            }

            return used;
        }

        private bool WouldHidingBeBlockedByCombat(BaseAdventurer puller)
        {
            if (!IsActive(puller))
            {
                return true;
            }

            int range = GetHidingCombatCheckRange(puller);
            IPooledEnumerable eable = puller.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, puller) && mobile.Combatant == puller && mobile.InLOS(puller))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private static int GetHidingCombatCheckRange(BaseAdventurer puller)
        {
            int skill = Math.Min(100, (int)puller.Skills[SkillName.Hiding].Value);
            return Math.Min(((100 - skill) / 2) + 8, 18);
        }

        private Point3D GetBreakPursuitPoint(BaseAdventurer puller, Mobile enemy)
        {
            if (!IsActive(puller) || m_TacticPoint == Point3D.Zero)
            {
                return puller == null ? Point3D.Zero : puller.Location;
            }

            BaseAdventurer leader = GetLeader();
            List<Mobile> enemies = BuildPullEnemyList(leader == null ? puller : leader, enemy);
            Point3D threat = enemy == null || enemy.Map != Map ? Point3D.Zero : enemy.Location;
            int backX;
            int backY;

            GetPullFormationBackVector(m_TacticPoint, threat, out backX, out backY);

            int sideX = -backY;
            int sideY = backX;
            List<Point3D> candidates = new List<Point3D>();

            for (int distance = AdventurePartySettings.PullBreakPursuitBackDistance; distance <= AdventurePartySettings.PullBreakPursuitBackDistance + 8; distance += 2)
            {
                AddPullAnchorCandidate(candidates, new Point3D(m_TacticPoint.X + (backX * distance), m_TacticPoint.Y + (backY * distance), m_TacticPoint.Z));

                for (int side = 3; side <= 6; side += 3)
                {
                    AddPullAnchorCandidate(candidates, new Point3D(m_TacticPoint.X + (backX * distance) + (sideX * side), m_TacticPoint.Y + (backY * distance) + (sideY * side), m_TacticPoint.Z));
                    AddPullAnchorCandidate(candidates, new Point3D(m_TacticPoint.X + (backX * distance) - (sideX * side), m_TacticPoint.Y + (backY * distance) - (sideY * side), m_TacticPoint.Z));
                }
            }

            for (int i = 0; i < 8; i++)
            {
                int xOffset;
                int yOffset;

                GetDirectionOffset((Direction)i, out xOffset, out yOffset);
                AddPullAnchorCandidate(candidates, new Point3D(puller.X + (xOffset * 4), puller.Y + (yOffset * 4), puller.Z));
                AddPullAnchorCandidate(candidates, new Point3D(puller.X + (xOffset * 7), puller.Y + (yOffset * 7), puller.Z));
            }

            Point3D best = m_TacticPoint;
            double bestScore = Double.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                Point3D point;

                if (!TryCreatePullAnchorPoint(puller, candidates[i], out point))
                {
                    continue;
                }

                double score = ScoreBreakPursuitPoint(puller, enemy, enemies, point);

                if (score < bestScore)
                {
                    best = point;
                    bestScore = score;
                }
            }

            return best;
        }

        private double ScoreBreakPursuitPoint(BaseAdventurer puller, Mobile enemy, List<Mobile> enemies, Point3D point)
        {
            double score = GetPointDistance(puller.Location, point) * 2.5;
            double nearest = GetNearestPullEnemyDistance(enemies, point);
            int localEnemies = CountPullEnemiesNearPoint(puller, point, AdventurePartySettings.PullResetLocalEnemyRange);
            int noticingEnemies = CountPullEnemiesThatCanNoticePoint(enemies, point);

            score += localEnemies * 125.0;
            score += noticingEnemies * 55.0;

            if (nearest < AdventurePartySettings.PullAnchorMinimumEnemyDistance)
            {
                score += (AdventurePartySettings.PullAnchorMinimumEnemyDistance - nearest) * 38.0;
            }
            else
            {
                score -= Math.Min(35.0, nearest * 2.0);
            }

            if (enemy != null && enemy.Map == Map && !Map.LineOfSight(point, enemy.Location))
            {
                score -= 28.0;
            }

            score += Math.Max(0.0, GetPointDistance(m_TacticPoint, point) - (AdventurePartySettings.PullBreakPursuitBackDistance + 8)) * 5.0;

            return score;
        }

        private Mobile FindResetPullTarget(BaseAdventurer puller, Mobile previousEnemy, bool allowLoosePack)
        {
            BaseAdventurer leader = GetLeader();

            if (!IsActive(puller) || leader == null)
            {
                return null;
            }

            List<Mobile> enemies = BuildPullEnemyList(leader, previousEnemy);
            Mobile best = null;
            double bestScore = Double.MaxValue;
            int maxNeighbors = allowLoosePack ? AdventurePartySettings.PullResetFallbackMaxNeighbors : AdventurePartySettings.PullResetPreferredMaxNeighbors;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile candidate = enemies[i];

                if (!IsValidEnemy(candidate, leader))
                {
                    continue;
                }

                if (GetPointDistance(candidate.Location, m_TacticPoint) <= AdventurePartySettings.PullChokeKillZoneRange)
                {
                    continue;
                }

                int neighbors = CountResetPullNeighbors(leader, enemies, candidate, AdventurePartySettings.PullResetTargetNeighborRange);

                if (neighbors > maxNeighbors)
                {
                    continue;
                }

                double distanceToPuller = Math.Max(1.0, puller.GetDistanceToSqrt(candidate));
                double distanceToAnchor = GetPointDistance(candidate.Location, m_TacticPoint);
                double score = neighbors * 95.0;

                score += distanceToPuller * 2.0;
                score += Math.Abs(distanceToAnchor - AdventurePartySettings.PullAnchorPreferredEnemyDistance) * 1.5;

                if (candidate.Serial == m_ResetPullOriginalTarget)
                {
                    score += 8.0;
                }

                if (candidate.Combatant == puller)
                {
                    score += 35.0;
                }

                if (best == null || score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private int CountResetPullNeighbors(BaseAdventurer observer, List<Mobile> enemies, Mobile target, int range)
        {
            if (observer == null || target == null || target.Deleted)
            {
                return 0;
            }

            int count = 0;

            if (enemies != null)
            {
                for (int i = 0; i < enemies.Count; i++)
                {
                    Mobile mobile = enemies[i];

                    if (mobile != target && IsValidEnemy(mobile, observer) && GetPointDistance(mobile.Location, target.Location) <= range)
                    {
                        count++;
                    }
                }

                return count;
            }

            IPooledEnumerable eable = target.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (mobile != target && IsValidEnemy(mobile, observer))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountRangedPressureThreats(BaseAdventurer observer, int range)
        {
            if (!IsActive(observer))
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = observer.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer) || !IsRangedPressureEnemy(mobile))
                {
                    continue;
                }

                if (mobile.InLOS(observer) || CanEnemyNoticePoint(mobile, observer.Location))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private bool IsRangedPressureEnemy(Mobile mobile)
        {
            BaseCreature creature = mobile as BaseCreature;

            if (creature == null)
            {
                return false;
            }

            if (IsDragonBreathThreat(creature))
            {
                return true;
            }

            switch (creature.AI)
            {
                case AIType.AI_Archer:
                case AIType.AI_Mage:
                case AIType.AI_NecroMage:
                case AIType.AI_Necro:
                case AIType.AI_Spellbinder:
                case AIType.AI_Spellweaving:
                case AIType.AI_Mystic:
                    return true;
            }

            return creature.Skills[SkillName.Magery].Value >= 50.0 ||
                creature.Skills[SkillName.EvalInt].Value >= 50.0 ||
                creature.Skills[SkillName.Necromancy].Value >= 50.0 ||
                creature.Skills[SkillName.Spellweaving].Value >= 50.0 ||
                creature.Skills[SkillName.Mysticism].Value >= 50.0 ||
                creature.Skills[SkillName.Archery].Value >= 50.0;
        }

        private int CountPullPursuers(BaseAdventurer puller, Mobile enemy, int range)
        {
            int count = 0;
            IPooledEnumerable eable = puller.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, puller))
                {
                    continue;
                }

                if (mobile.Combatant == puller || mobile == enemy || CanEnemyNoticePoint(mobile, puller.Location))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountPullPartyTargets(BaseAdventurer observer, int range, bool backlineOnly)
        {
            if (!IsActive(observer))
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = observer.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer))
                {
                    continue;
                }

                BaseAdventurer target = mobile.Combatant as BaseAdventurer;

                if (!IsActive(target) || !IsPartyMember(target))
                {
                    continue;
                }

                if (backlineOnly && target.Role == AdventurePartyRole.Fighter)
                {
                    continue;
                }

                count++;
            }

            eable.Free();
            return count;
        }

        private int CountDirectPursuers(BaseAdventurer puller, int range)
        {
            int count = 0;
            IPooledEnumerable eable = puller.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, puller) && mobile.Combatant == puller)
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountDirectPartyPursuers(BaseAdventurer observer, int range)
        {
            if (!IsActive(observer))
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = observer.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer))
                {
                    continue;
                }

                BaseAdventurer target = mobile.Combatant as BaseAdventurer;

                if (IsActive(target) && IsPartyMember(target))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private int CountDirectPartyPursuersExcept(BaseAdventurer observer, int range, Mobile ignoredTarget)
        {
            if (!IsActive(observer))
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = observer.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (mobile == ignoredTarget || !IsValidEnemy(mobile, observer))
                {
                    continue;
                }

                BaseAdventurer target = mobile.Combatant as BaseAdventurer;

                if (IsActive(target) && IsPartyMember(target))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private bool ShouldFinishFleeingTarget(Mobile target, BaseAdventurer observer)
        {
            if (target == null || target.Deleted || !target.Alive || target.Map != Map || target.HitsMax <= 0)
            {
                return false;
            }

            if (GetHitPercent(target) > AdventurePartySettings.FleeingTargetFinishHitPercent)
            {
                return false;
            }

            if (m_State == AdventurePartyState.Retreating ||
                m_CurrentTactic == AdventurePartyTactic.AvoidAOE ||
                m_CurrentTactic == AdventurePartyTactic.Recovering ||
                m_ResetPullActive ||
                CountRecoverableDeadMembers() > 0 ||
                IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0)
            {
                return false;
            }

            if (!IsActive(observer))
            {
                observer = GetLeader();
            }

            if (!IsActive(observer))
            {
                return false;
            }

            if (observer.GetDistanceToSqrt(target) > AdventurePartySettings.FleeingTargetFinishMaxDistance)
            {
                return false;
            }

            if (CountOtherEnemiesNearParty(target, AdventurePartySettings.FleeingTargetFinishSafeRange) > 0)
            {
                return false;
            }

            return CountDirectPartyPursuersExcept(observer, AdventurePartySettings.FleeingTargetFinishSafeRange, target) == 0;
        }

        private bool ShouldInterceptIsolatedBacklineTarget(Mobile target, BaseAdventurer threatenedMember)
        {
            if (target == null || target.Deleted || !target.Alive || target.Map != Map)
            {
                return false;
            }

            if (m_State == AdventurePartyState.Retreating ||
                m_CurrentTactic == AdventurePartyTactic.AvoidAOE ||
                m_CurrentTactic == AdventurePartyTactic.Recovering ||
                m_ResetPullActive ||
                CountRecoverableDeadMembers() > 0 ||
                IsPostResurrectionRecoveryActive() ||
                CountCriticalMembers() > 0)
            {
                return false;
            }

            BaseAdventurer fighter = FindActiveMemberByRole(AdventurePartyRole.Fighter);

            if (!IsActive(fighter) || GetHitPercent(fighter) < AdventurePartySettings.FighterPressureBreakPursuitHitPercent)
            {
                return false;
            }

            if (fighter.GetDistanceToSqrt(target) > AdventurePartySettings.IsolatedBacklineInterceptMaxDistance)
            {
                return false;
            }

            if (CountOtherEnemiesNearParty(target, AdventurePartySettings.IsolatedBacklineInterceptSafeRange) > 0)
            {
                return false;
            }

            if (HasIsolatedBacklineInterceptPackRisk(target, fighter))
            {
                return false;
            }

            if (CountDirectPartyPursuersExcept(fighter, AdventurePartySettings.IsolatedBacklineInterceptSafeRange, target) > 0)
            {
                return false;
            }

            if (IsActive(threatenedMember) && threatenedMember.Role != AdventurePartyRole.Fighter)
            {
                return true;
            }

            return IsPressuringOrDuelingBackline(target);
        }

        private bool HasIsolatedBacklineInterceptPackRisk(Mobile target, BaseAdventurer fighter)
        {
            if (target == null || target.Deleted || !target.Alive || target.Map != Map || !IsActive(fighter))
            {
                return false;
            }

            int range = AdventurePartySettings.IsolatedBacklineInterceptPackScanRange;

            if (HasIsolatedBacklineInterceptPackRiskNearPoint(target, fighter, target.Location, range) ||
                HasIsolatedBacklineInterceptPackRiskNearPoint(target, fighter, fighter.Location, range))
            {
                return true;
            }

            BaseAdventurer leader = GetLeader();

            if (IsActive(leader) && HasIsolatedBacklineInterceptPackRiskNearPoint(target, fighter, leader.Location, range))
            {
                return true;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) || member.Role == AdventurePartyRole.Fighter)
                {
                    continue;
                }

                if (HasIsolatedBacklineInterceptPackRiskNearPoint(target, fighter, member.Location, range))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasIsolatedBacklineInterceptPackRiskNearPoint(Mobile target, BaseAdventurer observer, Point3D point, int range)
        {
            if (!IsActive(observer) || target == null || point == Point3D.Zero || Map == null || Map == Map.Internal)
            {
                return false;
            }

            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (mobile == target || !IsValidEnemy(mobile, observer) ||
                    GetPointDistance(point, mobile.Location) > range)
                {
                    continue;
                }

                BaseAdventurer combatant = mobile.Combatant as BaseAdventurer;

                if (IsRangedPressureEnemy(mobile) ||
                    IsAIHardTarget(mobile) ||
                    (IsActive(combatant) && IsPartyMember(combatant)))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private bool IsPressuringOrDuelingBackline(Mobile target)
        {
            if (target == null || target.Deleted || !target.Alive)
            {
                return false;
            }

            BaseAdventurer pressured = target.Combatant as BaseAdventurer;

            if (IsActive(pressured) && IsPartyMember(pressured) && pressured.Role != AdventurePartyRole.Fighter)
            {
                return true;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Role != AdventurePartyRole.Fighter && member.Combatant == target)
                {
                    return true;
                }
            }

            return false;
        }

        private void AssignFinishFleeingTarget(Mobile target)
        {
            if (target == null || target.Deleted || !target.Alive)
            {
                return;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) || member.Role == AdventurePartyRole.Healer)
                {
                    continue;
                }

                member.Combatant = target;
            }
        }

        private void AssignInterceptionTarget(Mobile target)
        {
            AssignFinishFleeingTarget(target);
        }

        private BaseAdventurer FindPuller()
        {
            BaseAdventurer puller = FindActiveMemberByRole(AdventurePartyRole.Archer);

            if (puller != null)
            {
                return puller;
            }

            puller = FindActiveMemberByRole(AdventurePartyRole.Mage);

            if (puller != null)
            {
                return puller;
            }

            return FindActiveMemberByRole(AdventurePartyRole.Fighter);
        }

        private BaseAdventurer FindActiveMemberByRole(AdventurePartyRole role)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Role == role)
                {
                    return member;
                }
            }

            return null;
        }

        private void Rest()
        {
            bool recovered = true;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                member.Combatant = null;

                if (member.Hits < member.HitsMax)
                {
                    recovered = false;
                    member.Hits = Math.Min(member.HitsMax, member.Hits + Utility.RandomMinMax(4, 8));
                }
            }

            BaseAdventurer healer = GetHealer();

            if (healer != null)
            {
                HealMostInjured(healer);
            }

            if (IsChokeTactic(m_CurrentTactic) && m_TacticPoint != Point3D.Zero)
            {
                MovePartyToward(m_TacticPoint);
            }

            if (!IsChokeTactic(m_CurrentTactic) && (recovered || m_StateTicks >= 8))
            {
                ChangeState(AdventurePartyState.Exploring);
            }
        }

        private void Retreat()
        {
            if (m_CurrentTactic == AdventurePartyTactic.AvoidAOE)
            {
                Mobile threat = ResolveTacticTarget(null);

                if (threat != null)
                {
                    AvoidThreatArea(threat);
                    return;
                }
            }

            if (m_Waypoints == null || m_Waypoints.Length == 0)
            {
                BuildDefaultRoute(Location);
            }

            Point3D fallback = m_Waypoints[0];
            MovePartyToward(fallback, true);

            BaseAdventurer leader = GetLeader();

            if (leader != null)
            {
                WatchForStuckLeader(leader);
            }

            if (leader != null && leader.InRange(fallback, 3) && CountCriticalMembers() == 0)
            {
                ChangeState(AdventurePartyState.Resting);
            }
        }

        private BaseAdventurer GetHealer()
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (IsActive(member) && member.Role == AdventurePartyRole.Healer)
                {
                    return member;
                }
            }

            return null;
        }

        public List<string> GetHealerMonitorLines()
        {
            return BuildHealerDebugLines(FindEnemy());
        }

        private void ReportHealerDebug(Mobile currentEnemy)
        {
            if (!AdventurePartyDebug.HealerDebugEnabled || DateTime.UtcNow < m_NextHealerDebug)
            {
                return;
            }

            m_NextHealerDebug = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);

            BaseAdventurer healer = GetHealer();
            List<string> lines = BuildHealerDebugLines(currentEnemy);

            for (int i = 0; i < lines.Count; i++)
            {
                AdventurePartyDebug.Healer(healer, lines[i]);
            }
        }

        private List<string> BuildHealerDebugLines(Mobile currentEnemy)
        {
            List<string> lines = new List<string>();
            BaseAdventurer healer = GetHealer();
            BaseAdventurer fighter = GetLeader();
            Mobile tacticTarget = ResolveTacticTarget(currentEnemy);

            lines.Add(String.Format(
                "healer check: state={0}, tactic={1}, enemy={2}, tacticTarget={3}.",
                m_State,
                m_CurrentTactic,
                GetDebugName(currentEnemy),
                GetDebugName(tacticTarget)));

            if (!IsActive(healer))
            {
                lines.Add("healer: inactive, dead, deleted, or off map.");
                return lines;
            }

            if (!IsActive(fighter) || fighter.Role != AdventurePartyRole.Fighter)
            {
                lines.Add("fighter: no active fighter leader found.");
                lines.Add(AdvancedCombatBrain.GetHealerDebugState(healer));
                return lines;
            }

            Mobile selfThreat = FindBandageSupportBacklineThreat(healer, fighter);
            bool supportWanted = ShouldHealerSupportFighterWithBandage(healer, fighter);
            bool inBandageRange = healer.InRange(fighter, Bandage.Range);
            bool hasLineOfSight = healer.InLOS(fighter);
            bool hasSupportPoint = false;
            Point3D supportPoint;

            if (!inBandageRange || !hasLineOfSight)
            {
                hasSupportPoint = TryFindHealerBandageSupportPoint(healer, fighter, out supportPoint);
            }

            lines.Add(String.Format(
                "fighter: hp={0}/{1} ({2}%), poisoned={3}, combatant={4}, pressure={5}.",
                fighter.Hits,
                fighter.HitsMax,
                GetHitPercent(fighter),
                fighter.Poisoned,
                GetDebugName(fighter.Combatant as Mobile),
                DescribeFighterBandagePressure(healer, fighter)));

            lines.Add(String.Format(
                "healer: hp={0}/{1} ({2}%), mana={3}/{4}, distToFighter={5:0.0}, bandageRange={6}, los={7}, selfThreat={8}.",
                healer.Hits,
                healer.HitsMax,
                GetHitPercent(healer),
                healer.Mana,
                healer.ManaMax,
                healer.GetDistanceToSqrt(fighter),
                inBandageRange,
                hasLineOfSight,
                GetDebugName(selfThreat)));

            lines.Add("controller decision: " + DescribeHealerBandageControllerDecision(healer, fighter, supportWanted, selfThreat, inBandageRange, hasLineOfSight, hasSupportPoint));
            lines.Add("last controller action: " + GetLastHealerSupportAction());
            lines.Add(AdvancedCombatBrain.GetHealerDebugState(healer));

            return lines;
        }

        private string GetLastHealerSupportAction()
        {
            if (String.IsNullOrEmpty(m_LastHealerSupportAction))
            {
                return "none recorded yet.";
            }

            double age = Math.Max(0.0, (DateTime.UtcNow - m_LastHealerSupportActionTime).TotalSeconds);
            return String.Format("{0} ({1:0.0}s ago)", m_LastHealerSupportAction, age);
        }

        private string DescribeHealerBandageControllerDecision(
            BaseAdventurer healer,
            BaseAdventurer fighter,
            bool supportWanted,
            Mobile selfThreat,
            bool inBandageRange,
            bool hasLineOfSight,
            bool hasSupportPoint)
        {
            if (!IsActive(healer))
            {
                return "blocked: healer is not active.";
            }

            if (!IsActive(fighter) || fighter.Role != AdventurePartyRole.Fighter)
            {
                return "blocked: no active fighter leader.";
            }

            if (!healer.CanBeBeneficial(fighter, false, true))
            {
                return "blocked: healer cannot perform beneficial actions on the fighter.";
            }

            if (!supportWanted)
            {
                return "idle: fighter has not crossed the bandage-support trigger yet.";
            }

            if (selfThreat != null)
            {
                return String.Format("blocked: healer safety first; nearby threat is {0}.", GetDebugName(selfThreat));
            }

            if (inBandageRange && hasLineOfSight)
            {
                return "ready: healer is already in bandage range and line of sight.";
            }

            if (healer.GetDistanceToSqrt(fighter) > AdventurePartySettings.HealerBandageApproachRange)
            {
                return String.Format("blocked: fighter is outside the {0}-tile bandage approach leash.", AdventurePartySettings.HealerBandageApproachRange);
            }

            if (!hasSupportPoint)
            {
                return "move: no final bandage point found yet; healer should try a safe staging step closer.";
            }

            return "move: healer should step toward a safe bandage support point.";
        }

        private string DescribeFighterBandagePressure(BaseAdventurer healer, BaseAdventurer fighter)
        {
            if (!IsActive(fighter))
            {
                return "inactive";
            }

            Mobile combatant = fighter.Combatant as Mobile;

            if (IsValidEnemy(combatant, fighter) && fighter.InRange(combatant, 10))
            {
                return String.Format("yes: fighter combatant {0} within 10 tiles", GetDebugName(combatant));
            }

            Mobile tacticTarget = ResolveTacticTarget(null);

            if (IsValidEnemy(tacticTarget, fighter) && fighter.InRange(tacticTarget, 10))
            {
                return String.Format("yes: tactic target {0} within 10 tiles", GetDebugName(tacticTarget));
            }

            IPooledEnumerable eable = fighter.GetMobilesInRange(6);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, fighter))
                {
                    continue;
                }

                double distance = fighter.GetDistanceToSqrt(mobile);

                if (mobile.Combatant == fighter)
                {
                    eable.Free();
                    return String.Format("yes: {0} targets fighter at {1:0.0} tiles", GetDebugName(mobile), distance);
                }

                if (distance <= 4.0 && mobile.Combatant != healer)
                {
                    eable.Free();
                    return String.Format("yes: {0} is near fighter at {1:0.0} tiles", GetDebugName(mobile), distance);
                }
            }

            eable.Free();
            return "no: no combatant/tactic target within 10 and no nearby monster pressure";
        }

        private static int GetHitPercent(Mobile mobile)
        {
            if (mobile == null || mobile.HitsMax <= 0)
            {
                return 0;
            }

            return (mobile.Hits * 100) / mobile.HitsMax;
        }

        private static string GetDebugName(Mobile mobile)
        {
            if (mobile == null)
            {
                return "none";
            }

            string name = String.IsNullOrEmpty(mobile.Name) ? mobile.GetType().Name : mobile.Name;
            return String.Format("{0}[0x{1:X}]", name, mobile.Serial.Value);
        }

        private void HealMostInjured(BaseAdventurer healer)
        {
            if (!IsActive(healer))
            {
                return;
            }

            if (TryPreserveHealerBeforeSupport(healer))
            {
                return;
            }

            if (healer.NextPartyAction > DateTime.UtcNow)
            {
                return;
            }

            if (TryResurrectDeadMember(healer))
            {
                return;
            }

            BaseAdventurer target = null;
            int missingHits = 0;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                int missing = member.HitsMax - member.Hits;

                if (missing > missingHits)
                {
                    target = member;
                    missingHits = missing;
                }
            }

            if (target == null || missingHits <= 0)
            {
                return;
            }

            if (healer.Role == AdventurePartyRole.Healer &&
                (m_State == AdventurePartyState.Engaging ||
                 m_State == AdventurePartyState.Retreating ||
                 m_CurrentTactic == AdventurePartyTactic.Recovering))
            {
                if (AdvancedCombatBrain.TryForceBandageHeal(healer, target))
                {
                    SetHealerSupportAction(String.Format("started: healer began a real bandage on {0}.", GetDebugName(target)));
                    return;
                }

                if (!healer.InRange(target, Bandage.Range) &&
                    healer.GetDistanceToSqrt(target) <= AdventurePartySettings.HealerBandageApproachRange)
                {
                    MoveMemberToward(healer, target.Location, true);
                    SetHealerSupportAction(String.Format("move: healer is closing to real bandage range on {0}.", GetDebugName(target)));
                    return;
                }

                return;
            }

            if (!healer.InRange(target, 8))
            {
                MoveMemberToward(healer, target.Location, true);
                return;
            }

            int amount = Utility.RandomMinMax(12, 22);
            target.Hits = Math.Min(target.HitsMax, target.Hits + amount);
            healer.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(8.0);

            healer.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Stay with me.");
        }

        private bool TryPreserveHealerBeforeSupport(BaseAdventurer healer)
        {
            if (!IsActive(healer) || healer.Role != AdventurePartyRole.Healer)
            {
                return false;
            }

            BaseAdventurer fighter = GetLeader();
            Mobile threat = FindBandageSupportBacklineThreat(healer, fighter) ??
                FindSelfPreservationThreat(healer, healer.Combatant as Mobile, AdventurePartySettings.BacklineThreatRange);

            if (threat == null)
            {
                return false;
            }

            int hitPercent = GetHitPercent(healer);
            bool directTarget = threat.Combatant == healer;
            bool meleeThreat = healer.InRange(threat, AdventurePartySettings.BacklineMeleeThreatRange);

            if (hitPercent >= 75 && !directTarget && !meleeThreat)
            {
                return false;
            }

            healer.Combatant = null;
            healer.Warmode = false;

            if (!TryCircleBacklineAroundFighter(healer, threat))
            {
                MoveMemberAwayFrom(healer, threat.Location, AdventurePartySettings.BacklineThreatSafeRange + 2, true, true);
            }

            healer.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(1.0);
            SetHealerSupportAction(String.Format("safety first: escaping {0} before support.", GetDebugName(threat)));
            m_LastAIStatus = String.Format("Healer safety: escaping {0} before support.", GetDebugName(threat));
            return true;
        }

        private bool TryMoveHealerTowardBandageSupport(BaseAdventurer healer)
        {
            if (!IsActive(healer) || healer.Role != AdventurePartyRole.Healer)
            {
                SetHealerSupportAction("not attempted: healer is inactive or not a healer.");
                return false;
            }

            BaseAdventurer fighter = GetLeader();

            if (!IsActive(fighter) || fighter == healer || fighter.Role != AdventurePartyRole.Fighter ||
                fighter.HitsMax <= 0 || !healer.CanBeBeneficial(fighter, false, true))
            {
                SetHealerSupportAction("blocked: no active beneficial fighter target.");
                return false;
            }

            if (!ShouldHealerSupportFighterWithBandage(healer, fighter))
            {
                SetHealerSupportAction("idle: fighter did not meet bandage-support trigger.");
                return false;
            }

            Mobile selfThreat = FindBandageSupportBacklineThreat(healer, fighter);

            if (selfThreat != null)
            {
                if (TryPreserveHealerBeforeSupport(healer))
                {
                    return true;
                }

                SetHealerSupportAction(String.Format("blocked: healer safety threat {0}.", GetDebugName(selfThreat)));
                return false;
            }

            if (healer.InRange(fighter, Bandage.Range) && healer.InLOS(fighter))
            {
                if (AdvancedCombatBrain.TryForceBandageHeal(healer, fighter))
                {
                    SetHealerSupportAction("started: healer began a bandage on the fighter from support range.");
                    return true;
                }

                SetHealerSupportAction("ready: healer already has bandage range and line of sight, but bandage start was unavailable.");
                return true;
            }

            if (healer.GetDistanceToSqrt(fighter) > AdventurePartySettings.HealerBandageApproachRange)
            {
                SetHealerSupportAction(String.Format("blocked: fighter is outside the {0}-tile approach leash.", AdventurePartySettings.HealerBandageApproachRange));
                return false;
            }

            Point3D supportPoint;

            if (!TryFindHealerBandageSupportPoint(healer, fighter, out supportPoint))
            {
                bool staged = TryStepHealerTowardBandageStaging(healer, fighter);
                SetHealerSupportAction(staged ? "move: no final bandage point yet; staged closer to the fighter." : "blocked: no safe final bandage point or staging step found.");
                return true;
            }

            bool moved = TryStepHealerTowardBandagePoint(healer, fighter, supportPoint);
            SetHealerSupportAction(moved ? "move: stepped toward bandage support point." : "blocked: wanted to move, but every immediate step failed.");
            return true;
        }

        private void SetHealerSupportAction(string action)
        {
            m_LastHealerSupportAction = action;
            m_LastHealerSupportActionTime = DateTime.UtcNow;
        }

        private bool ShouldHealerSupportFighterWithBandage(BaseAdventurer healer, BaseAdventurer fighter)
        {
            if (healer == null || fighter == null || fighter.HitsMax <= 0)
            {
                return false;
            }

            if (fighter.Poisoned || fighter.Hits < fighter.HitsMax * 85 / 100)
            {
                return true;
            }

            return fighter.Hits < fighter.HitsMax * 95 / 100 && IsFighterUnderBandagePressure(healer, fighter);
        }

        private bool IsFighterUnderBandagePressure(BaseAdventurer healer, BaseAdventurer fighter)
        {
            if (!IsActive(fighter))
            {
                return false;
            }

            Mobile target = fighter.Combatant as Mobile;

            if (IsValidEnemy(target, fighter) && fighter.InRange(target, 10))
            {
                return true;
            }

            target = ResolveTacticTarget(null);

            if (IsValidEnemy(target, fighter) && fighter.InRange(target, 10))
            {
                return true;
            }

            IPooledEnumerable eable = fighter.GetMobilesInRange(6);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, fighter))
                {
                    continue;
                }

                double distance = fighter.GetDistanceToSqrt(mobile);

                if (mobile.Combatant == fighter || (distance <= 4.0 && mobile.Combatant != healer))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private Mobile FindBandageSupportBacklineThreat(BaseAdventurer healer, BaseAdventurer fighter)
        {
            if (!IsActive(healer) || !IsActive(fighter))
            {
                return null;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = healer.GetMobilesInRange(AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, healer))
                {
                    continue;
                }

                double distance = Math.Max(0.5, healer.GetDistanceToSqrt(mobile));

                if (mobile.Combatant != healer && mobile.Combatant == fighter &&
                    distance > AdventurePartySettings.BacklineMeleeThreatRange)
                {
                    continue;
                }

                if (mobile.Combatant != healer && distance > AdventurePartySettings.BacklineMeleeThreatRange)
                {
                    continue;
                }

                double score = 20.0 - distance;

                if (mobile.Combatant == healer)
                {
                    score += 12.0;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private Mobile FindSelfPreservationThreat(BaseAdventurer member, Mobile currentTarget, int range)
        {
            if (!IsActive(member))
            {
                return null;
            }

            if (member.Role != AdventurePartyRole.Fighter)
            {
                Mobile backlineThreat = FindNearbyBacklineThreat(member, currentTarget);

                if (backlineThreat != null)
                {
                    return backlineThreat;
                }
            }

            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = member.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, member))
                {
                    continue;
                }

                double distance = Math.Max(0.5, member.GetDistanceToSqrt(mobile));
                bool directTarget = mobile.Combatant == member;
                bool meleeThreat = distance <= AdventurePartySettings.BacklineMeleeThreatRange;

                if (!directTarget && !meleeThreat)
                {
                    continue;
                }

                double score = 20.0 - distance;

                if (directTarget)
                {
                    score += 12.0;
                }

                if (mobile == currentTarget)
                {
                    score += 4.0;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private bool TryFindHealerBandageSupportPoint(BaseAdventurer healer, BaseAdventurer fighter, out Point3D supportPoint)
        {
            supportPoint = Point3D.Zero;

            if (!IsActive(healer) || !IsActive(fighter) || Map == null)
            {
                return false;
            }

            double bestScore = Double.MaxValue;

            for (int xOffset = -Bandage.Range; xOffset <= Bandage.Range; xOffset++)
            {
                for (int yOffset = -Bandage.Range; yOffset <= Bandage.Range; yOffset++)
                {
                    if (xOffset == 0 && yOffset == 0 ||
                        Math.Max(Math.Abs(xOffset), Math.Abs(yOffset)) > Bandage.Range)
                    {
                        continue;
                    }

                    Point3D candidate;

                    if (!TryCreateHealerBandageSupportPoint(healer, fighter, fighter.X + xOffset, fighter.Y + yOffset, out candidate))
                    {
                        continue;
                    }

                    double score = ScoreHealerBandageSupportPoint(healer, fighter, candidate);

                    if (score < bestScore)
                    {
                        supportPoint = candidate;
                        bestScore = score;
                    }
                }
            }

            return bestScore < Double.MaxValue;
        }

        private double ScoreHealerBandageSupportPoint(BaseAdventurer healer, BaseAdventurer fighter, Point3D point)
        {
            double score = healer.GetDistanceToSqrt(point) * 8.0;
            score += GetPointDistance(point, fighter.Location) * 2.0;
            score += ScoreHealerBandageEnemyPressure(healer, fighter, point) * 4.0;

            return score;
        }

        private bool TryCreateHealerBandageSupportPoint(BaseAdventurer healer, BaseAdventurer fighter, int x, int y, out Point3D point)
        {
            point = Point3D.Zero;

            Point3D preferred = new Point3D(x, y, fighter.Z);

            if (IsValidHealerBandageSupportPoint(healer, fighter, preferred))
            {
                point = preferred;
                return true;
            }

            int averageZ = Map.GetAverageZ(x, y);
            Point3D average = new Point3D(x, y, averageZ);

            if (averageZ != preferred.Z && IsValidHealerBandageSupportPoint(healer, fighter, average))
            {
                point = average;
                return true;
            }

            Point3D healerZ = new Point3D(x, y, healer.Z);

            if (healer.Z != preferred.Z && healer.Z != averageZ && IsValidHealerBandageSupportPoint(healer, fighter, healerZ))
            {
                point = healerZ;
                return true;
            }

            return false;
        }

        private bool IsValidHealerBandageSupportPoint(BaseAdventurer healer, BaseAdventurer fighter, Point3D point)
        {
            if (!fighter.InRange(point, Bandage.Range) ||
                !Map.CanFit(point.X, point.Y, point.Z, 16, false, true, true, healer) ||
                !Map.LineOfSight(point, fighter.Location))
            {
                return false;
            }

            return !IsHealerBandagePointDangerous(healer, fighter, point);
        }

        private bool TryStepHealerTowardBandagePoint(BaseAdventurer healer, BaseAdventurer fighter, Point3D supportPoint)
        {
            Direction bestDirection = Direction.North;
            double bestScore = Double.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                Direction direction = (Direction)i;
                Point3D next = GetPointInDirection(healer.Location, direction);

                if (!Map.CanFit(next.X, next.Y, next.Z, 16, false, true, true, healer) ||
                    IsHealerBandagePointDangerous(healer, fighter, next))
                {
                    continue;
                }

                double score = GetPointDistance(next, supportPoint) * 10.0;
                score += GetPointDistance(next, fighter.Location) * 2.0;
                score += ScoreHealerBandageEnemyPressure(healer, fighter, next) * 3.0;

                if (fighter.InRange(next, Bandage.Range) && Map.LineOfSight(next, fighter.Location))
                {
                    score -= 20.0;
                }

                if (score < bestScore)
                {
                    bestDirection = direction;
                    bestScore = score;
                }
            }

            return bestScore < Double.MaxValue && TryMove(healer, bestDirection, true);
        }

        private bool TryStepHealerTowardBandageStaging(BaseAdventurer healer, BaseAdventurer fighter)
        {
            if (!IsActive(healer) || !IsActive(fighter) || Map == null)
            {
                return false;
            }

            double currentDistance = healer.GetDistanceToSqrt(fighter);
            Direction bestDirection = Direction.North;
            double bestScore = Double.MaxValue;

            for (int i = 0; i < 8; i++)
            {
                Direction direction = (Direction)i;
                Point3D next = GetPointInDirection(healer.Location, direction);
                double nextDistance = GetPointDistance(next, fighter.Location);

                if (nextDistance > AdventurePartySettings.HealerBandageApproachRange ||
                    nextDistance > currentDistance + 0.25 ||
                    !Map.CanFit(next.X, next.Y, next.Z, 16, false, true, true, healer) ||
                    IsHealerBandagePointDangerous(healer, fighter, next))
                {
                    continue;
                }

                if (nextDistance <= Bandage.Range && !Map.LineOfSight(next, fighter.Location))
                {
                    continue;
                }

                double score = nextDistance * 10.0;
                score += ScoreHealerBandageEnemyPressure(healer, fighter, next) * 3.0;

                if (nextDistance < currentDistance)
                {
                    score -= 20.0;
                }

                if (Map.LineOfSight(next, fighter.Location))
                {
                    score -= 6.0;
                }

                if (fighter.InRange(next, Bandage.Range) && Map.LineOfSight(next, fighter.Location))
                {
                    score -= 30.0;
                }

                if (score < bestScore)
                {
                    bestDirection = direction;
                    bestScore = score;
                }
            }

            return bestScore < Double.MaxValue && TryMove(healer, bestDirection, true);
        }

        private bool IsHealerBandagePointDangerous(BaseAdventurer healer, BaseAdventurer fighter, Point3D point)
        {
            IPooledEnumerable eable = Map.GetMobilesInRange(point, AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, healer))
                {
                    continue;
                }

                double distance = GetPointDistance(point, mobile.Location);
                double fighterDistance = Math.Max(0.5, fighter.GetDistanceToSqrt(mobile));

                if (distance <= 1.5 ||
                    (mobile.Combatant == healer && distance <= AdventurePartySettings.BacklineThreatRange) ||
                    (mobile.Combatant != healer && distance <= AdventurePartySettings.BacklineMeleeThreatRange && distance + 0.25 < fighterDistance))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private double ScoreHealerBandageEnemyPressure(BaseAdventurer healer, BaseAdventurer fighter, Point3D point)
        {
            double score = 0.0;
            IPooledEnumerable eable = Map.GetMobilesInRange(point, AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, healer))
                {
                    continue;
                }

                double distance = Math.Max(0.5, GetPointDistance(point, mobile.Location));
                double fighterDistance = Math.Max(0.5, fighter.GetDistanceToSqrt(mobile));

                if (mobile.Combatant == healer)
                {
                    score += 100.0 / distance;
                }
                else if (distance < fighterDistance)
                {
                    score += 40.0 * (fighterDistance - distance);
                }
                else if (mobile.Combatant == fighter)
                {
                    score += 8.0 / distance;
                }
                else
                {
                    score += 16.0 / distance;
                }
            }

            eable.Free();
            return score;
        }

        private static Point3D GetPointInDirection(Point3D from, Direction direction)
        {
            int xOffset;
            int yOffset;

            GetDirectionOffset(direction, out xOffset, out yOffset);

            return new Point3D(from.X + xOffset, from.Y + yOffset, from.Z);
        }

        private static void GetDirectionOffset(Direction direction, out int xOffset, out int yOffset)
        {
            xOffset = 0;
            yOffset = 0;

            switch (direction & Direction.Mask)
            {
                case Direction.North:
                    yOffset = -1;
                    break;
                case Direction.Right:
                    xOffset = 1;
                    yOffset = -1;
                    break;
                case Direction.East:
                    xOffset = 1;
                    break;
                case Direction.Down:
                    xOffset = 1;
                    yOffset = 1;
                    break;
                case Direction.South:
                    yOffset = 1;
                    break;
                case Direction.Left:
                    xOffset = -1;
                    yOffset = 1;
                    break;
                case Direction.West:
                    xOffset = -1;
                    break;
                case Direction.Up:
                    xOffset = -1;
                    yOffset = -1;
                    break;
            }
        }

        private static double GetPointDistance(Point3D left, Point3D right)
        {
            int xDelta = left.X - right.X;
            int yDelta = left.Y - right.Y;
            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        private static double GetPointToSegmentDistance(Point3D point, Point3D start, Point3D end)
        {
            double x = point.X;
            double y = point.Y;
            double startX = start.X;
            double startY = start.Y;
            double deltaX = end.X - start.X;
            double deltaY = end.Y - start.Y;
            double lengthSquared = (deltaX * deltaX) + (deltaY * deltaY);

            if (lengthSquared <= 0.0)
            {
                return GetPointDistance(point, start);
            }

            double t = (((x - startX) * deltaX) + ((y - startY) * deltaY)) / lengthSquared;
            t = Math.Max(0.0, Math.Min(1.0, t));

            double projectionX = startX + (t * deltaX);
            double projectionY = startY + (t * deltaY);
            double xDelta = x - projectionX;
            double yDelta = y - projectionY;

            return Math.Sqrt((xDelta * xDelta) + (yDelta * yDelta));
        }

        private bool TryResurrectDeadMember(BaseAdventurer preferredResurrector)
        {
            BaseAdventurer resurrector = CanResurrectPartyMember(preferredResurrector) ? preferredResurrector : FindResurrector();

            if (resurrector == null)
            {
                return false;
            }

            BaseAdventurer target = FindDeadMemberForResurrection(resurrector);

            if (target == null)
            {
                return false;
            }

            Mobile enemy = ResolveTacticTarget(null) ?? FindBestEnemy(false);

            if (TryPreserveResurrectorBeforeResurrection(resurrector, target, enemy))
            {
                return true;
            }

            Point3D rally = GetResurrectionRallyPoint(resurrector, target, enemy);

            if (!IsResurrectionReady(resurrector, target, rally))
            {
                CoordinateResurrectionRecovery(resurrector, target, rally, enemy);
                return true;
            }

            if (!resurrector.InRange(target, Bandage.Range))
            {
                resurrector.Combatant = null;
                resurrector.Warmode = false;
                MoveMemberToward(resurrector, target.Location, true);
                resurrector.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(2.0);
                resurrector.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Stay close. I can bring you back while we move.");
                return true;
            }

            if (target.ResurrectAdventurer(resurrector))
            {
                BeginPostResurrectionRecovery(target);
                resurrector.NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(10.0);
                resurrector.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, "Back on your feet.");
                return true;
            }

            return false;
        }

        private bool TryHandleDeadMemberRecovery(Mobile enemy)
        {
            if (CountRecoverableDeadMembers() == 0 || CountLivingMembers() == 0)
            {
                return false;
            }

            BaseAdventurer resurrector = FindResurrectionCandidate();

            if (resurrector == null)
            {
                return false;
            }

            BaseAdventurer target = FindDeadMemberForResurrection(resurrector);

            if (target == null)
            {
                return false;
            }

            Point3D rally = GetResurrectionRallyPoint(resurrector, target, enemy);

            if (TryPreserveResurrectorBeforeResurrection(resurrector, target, enemy))
            {
                return true;
            }

            if (!CanResurrectPartyMember(resurrector) || !IsResurrectionReady(resurrector, target, rally))
            {
                CoordinateResurrectionRecovery(resurrector, target, rally, enemy ?? ResolveTacticTarget(null) ?? FindBestEnemy(false));
                return true;
            }

            if (TryResurrectDeadMember(resurrector))
            {
                ChangeState(IsPostResurrectionRecoveryActive() ?
                    (enemy == null ? AdventurePartyState.Resting : AdventurePartyState.Retreating) :
                    (enemy == null ? AdventurePartyState.Resting : AdventurePartyState.Engaging));
                return true;
            }

            return false;
        }

        private bool TryPrioritizeResurrectionRecovery(Mobile enemy)
        {
            if (CountRecoverableDeadMembers() == 0 || CountLivingMembers() == 0)
            {
                return false;
            }

            BaseAdventurer resurrector = FindResurrectionCandidate();

            if (resurrector == null)
            {
                return false;
            }

            BaseAdventurer target = FindDeadMemberForResurrection(resurrector);

            if (target == null)
            {
                return false;
            }

            if (CountDirectPursuers(resurrector, AdventurePartySettings.ResurrectionPullDistance) > 0 ||
                FindRangedResurrectionThreat(resurrector, target) != null)
            {
                return false;
            }

            Mobile immediateThreat =
                FindNearestResurrectionThreat(resurrector, resurrector.Location, AdventurePartySettings.ResurrectionDangerRange) ??
                FindNearestResurrectionThreat(resurrector, target.Location, AdventurePartySettings.ResurrectionDangerRange);

            if (immediateThreat != null && IsImmediateRecoverySafetyThreat(resurrector, immediateThreat))
            {
                return false;
            }

            if (m_RangedDisengageActive)
            {
                ClearRangedDisengageState();
            }

            return TryHandleDeadMemberRecovery(enemy);
        }

        private bool TryPreserveResurrectorBeforeResurrection(BaseAdventurer resurrector, BaseAdventurer target, Mobile enemy)
        {
            if (!IsActive(resurrector) || target == null)
            {
                return false;
            }

            Mobile threat = FindSelfPreservationThreat(resurrector, enemy, AdventurePartySettings.PullBreakPursuitScanRange);
            int hitPercent = GetHitPercent(resurrector);
            int directPursuers = CountDirectPursuers(resurrector, AdventurePartySettings.PullBreakPursuitScanRange);

            if (threat == null)
            {
                return false;
            }

            if (!IsImmediateRecoverySafetyThreat(resurrector, threat))
            {
                return false;
            }

            if (IsMovingResurrectionReady(resurrector, target))
            {
                return false;
            }

            bool urgent = hitPercent < 60 ||
                directPursuers >= 2 ||
                (threat != null && resurrector.InRange(threat, AdventurePartySettings.BacklineMeleeThreatRange));

            if (!urgent)
            {
                return false;
            }

            resurrector.Combatant = null;
            resurrector.Warmode = false;

            if (threat != null)
            {
                bool moved = resurrector.Role != AdventurePartyRole.Fighter && TryCircleBacklineAroundFighter(resurrector, threat);

                if (!moved)
                {
                    MoveMemberAwayFrom(resurrector, threat.Location, AdventurePartySettings.BacklineThreatSafeRange + 2, true, true);
                }
            }
            if (resurrector.Role == AdventurePartyRole.Healer)
            {
                SetHealerSupportAction(String.Format("safety first: delaying resurrection of {0}.", GetDebugName(target)));
            }

            m_LastAIStatus = String.Format(
                "Recovery delayed: {0} safety first before resurrecting {1}.",
                resurrector.Role,
                GetDebugName(target));
            return true;
        }

        private bool IsImmediateRecoverySafetyThreat(BaseAdventurer member, Mobile threat)
        {
            if (!IsActive(member) || threat == null || threat.Deleted || threat.Map != Map || !IsValidEnemy(threat, member))
            {
                return false;
            }

            double distance = member.GetDistanceToSqrt(threat);

            if (distance <= AdventurePartySettings.BacklineMeleeThreatRange)
            {
                return true;
            }

            return threat.Combatant == member && distance <= AdventurePartySettings.BacklineThreatSafeRange;
        }

        private bool IsPostResurrectionRecoveryActive()
        {
            return m_PostResurrectionMember != Serial.Zero &&
                m_PostResurrectionRecoveryUntil != DateTime.MinValue &&
                DateTime.UtcNow < m_PostResurrectionRecoveryUntil;
        }

        private void BeginPostResurrectionRecovery(BaseAdventurer member)
        {
            if (member == null)
            {
                ClearPostResurrectionRecovery();
                return;
            }

            DateTime now = DateTime.UtcNow;
            m_PostResurrectionMember = member.Serial;
            m_PostResurrectionRecoveryUntil = now + AdventurePartySettings.PostResurrectionRecoveryTime;

            if (m_ResurrectionRallyPoint == Point3D.Zero)
            {
                m_ResurrectionRallyPoint = member.Location;
                m_ResurrectionRallyCreated = now;
            }

            m_ResurrectionRallyExpires = now + AdventurePartySettings.PostResurrectionRecoveryTime + TimeSpan.FromSeconds(2.0);
            m_ResurrectionTarget = member.Serial;

            member.Combatant = null;
            member.Warmode = false;
            SetRecoveryTactic(member, m_ResurrectionRallyPoint);
        }

        private void ClearPostResurrectionRecovery()
        {
            m_PostResurrectionMember = Serial.Zero;
            m_PostResurrectionRecoveryUntil = DateTime.MinValue;
        }

        private bool TryHandlePostResurrectionRecovery(Mobile enemy)
        {
            if (m_PostResurrectionMember == Serial.Zero)
            {
                return false;
            }

            BaseAdventurer recovered = World.FindMobile(m_PostResurrectionMember) as BaseAdventurer;

            if (!IsActive(recovered))
            {
                ClearPostResurrectionRecovery();
                ClearResurrectionRally();
                ClearTactic();
                return false;
            }

            DateTime now = DateTime.UtcNow;
            Point3D rally = m_ResurrectionRallyPoint == Point3D.Zero ? recovered.Location : m_ResurrectionRallyPoint;
            Mobile threat = FindNearestResurrectionThreat(recovered, rally, AdventurePartySettings.ResurrectionPullDistance) ?? enemy;
            int localEnemies = CountResurrectionEnemiesNearPoint(recovered, rally, AdventurePartySettings.ResurrectionClearRange);
            bool timeExpired = m_PostResurrectionRecoveryUntil == DateTime.MinValue || now >= m_PostResurrectionRecoveryUntil;
            bool recoveredStable = GetHitPercent(recovered) >= AdventurePartySettings.PostResurrectionRecoveryHitPercent &&
                localEnemies == 0 &&
                CountDirectPartyPursuers(recovered, AdventurePartySettings.ResurrectionPullDistance) == 0;

            if (timeExpired && (recoveredStable || localEnemies == 0))
            {
                ClearPostResurrectionRecovery();
                ClearResurrectionRally();
                ClearTactic();
                m_LastAIStatus = String.Format("Post-resurrection recovery complete for {0}.", GetDebugName(recovered));
                return false;
            }

            ChangeState(threat == null && localEnemies == 0 ? AdventurePartyState.Resting : AdventurePartyState.Retreating);
            SetRecoveryTactic(recovered, rally);

            BaseAdventurer healer = GetHealer();

            if (IsActive(healer))
            {
                HealMostInjured(healer);
            }

            CoordinatePostResurrectionRecovery(recovered, rally, threat);

            int secondsLeft = Math.Max(0, (int)Math.Ceiling((m_PostResurrectionRecoveryUntil - now).TotalSeconds));
            m_LastAIStatus = String.Format(
                "Post-resurrection recovery: holding {0} near rally for {1}s; {2} hostile(s) near rally.",
                GetDebugName(recovered),
                secondsLeft,
                localEnemies);
            return true;
        }

        private void CoordinatePostResurrectionRecovery(BaseAdventurer recovered, Point3D rally, Mobile threat)
        {
            Point3D pullPoint = threat == null ? rally : GetPointAwayFrom(rally, threat.Location, AdventurePartySettings.ResurrectionPullDistance);

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                member.Combatant = null;
                member.Warmode = false;

                if (member == recovered)
                {
                    if (threat != null && member.InRange(threat, AdventurePartySettings.BacklineThreatRange))
                    {
                        MoveMemberAwayFrom(member, threat.Location, AdventurePartySettings.BacklineThreatSafeRange, true, true);
                    }
                    else if (!member.InRange(rally, 2))
                    {
                        MoveMemberToward(member, rally, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                    }

                    continue;
                }

                if (member.Role == AdventurePartyRole.Fighter && threat != null && IsValidEnemy(threat, member))
                {
                    member.Combatant = threat;

                    if (ShouldFighterPullAwayDuringRecovery(member, rally, threat))
                    {
                        MoveMemberToward(member, pullPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                    }
                    else if (!member.InRange(threat, 2))
                    {
                        MoveMemberToward(member, threat.Location, GetEngageMoveSteps(member, threat), false, true);
                    }
                    else
                    {
                        MoveMemberToward(member, pullPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                    }

                    continue;
                }

                if (GetPointDistance(member.Location, rally) > AdventurePartySettings.RecoveryHardRegroupDistance)
                {
                    MoveMemberToRecoveryFormation(member, rally);
                    continue;
                }

                Mobile selfThreat = FindSelfPreservationThreat(member, threat, AdventurePartySettings.BacklineThreatRange);

                if (selfThreat != null && member.InRange(selfThreat, AdventurePartySettings.BacklineThreatRange))
                {
                    MoveMemberAwayFrom(member, selfThreat.Location, AdventurePartySettings.BacklineThreatSafeRange, true, true);
                    continue;
                }

                MoveMemberToRecoveryFormation(member, rally);
            }
        }

        private void PrepareMemberForRetreat(BaseAdventurer member, Mobile enemy)
        {
            if (!IsActive(member))
            {
                return;
            }

            ResolveBreakPursuitPendingTarget();

            member.Combatant = null;
            member.Warmode = false;

            int pursuers = CountDirectPursuers(member, AdventurePartySettings.PullBreakPursuitScanRange);

            if (TryUseBreakPursuitHiding(member))
            {
                m_LastAIStatus = String.Format("Retreat: {0} used Hiding while recovery is blocked.", member.Role);
                return;
            }

            if (pursuers > 0 && TrySupportBreakPursuitInvisibility(member, pursuers))
            {
                return;
            }

            Mobile threat = FindSelfPreservationThreat(member, enemy, AdventurePartySettings.PullBreakPursuitScanRange) ?? enemy;

            if (threat != null && threat.Map == Map)
            {
                MoveMemberAwayFrom(member, threat.Location, AdventurePartySettings.BacklineThreatSafeRange + 3, true, true);
            }
        }

        private bool TryHandleRecoveryWithoutLivingResurrector(Mobile enemy)
        {
            if (CountRecoverableDeadMembers() == 0 || CountLivingMembers() == 0 || FindResurrectionCandidate() != null)
            {
                return false;
            }

            ClearTactic();
            ChangeState(AdventurePartyState.Retreating);

            BaseAdventurer observer = GetRangedDisengageObserver();
            List<Mobile> rangedThreats = IsActive(observer) ?
                FindRangedDisengageThreats(observer, enemy, AdventurePartySettings.NoResurrectorDisengageScanRange) :
                new List<Mobile>();

            if (rangedThreats.Count > 0)
            {
                ClearResetPullState();
                ClearBreakPursuitPending();
                m_RangedDisengageActive = true;
                m_RangedDisengageStarted = DateTime.UtcNow;
                m_RangedDisengageUntil = DateTime.UtcNow + AdventurePartySettings.RangedDisengageMaximumTime;
                m_RangedDisengageSettleUntil = DateTime.UtcNow + AdventurePartySettings.RangedDisengageSettleTime;
                m_RangedDisengagePoint = GetRangedDisengagePoint(observer, rangedThreats);
                m_RangedDisengageProgressAnchor = GetPartyDisengageAnchor(observer);
                m_RangedDisengageProgressChecked = DateTime.UtcNow;
                m_RangedDisengageStuckTicks = 0;
                m_CurrentTactic = AdventurePartyTactic.Recovering;
                m_TacticPoint = m_RangedDisengagePoint;
                m_TacticExpires = DateTime.UtcNow + AdventurePartySettings.RangedDisengageMaximumTime;

                ExecuteRangedDisengage(m_RangedDisengagePoint);
                m_LastAIStatus = String.Format(
                    "Recovery blocked: no living resurrector; hard disengage from {0} ranged threat(s) toward ({1},{2},{3}).",
                    rangedThreats.Count,
                    m_RangedDisengagePoint.X,
                    m_RangedDisengagePoint.Y,
                    m_RangedDisengagePoint.Z);

                return true;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                PrepareMemberForRetreat(member, enemy);
            }

            Retreat();
            m_LastAIStatus = enemy == null ?
                "Recovery blocked: no living resurrector; disengaging survivors." :
                String.Format("Recovery blocked: no living resurrector; disengaging survivors from {0}.", GetDebugName(enemy));

            return true;
        }

        private BaseAdventurer FindResurrectionCandidate()
        {
            BaseAdventurer resurrector = FindResurrectionCandidateByRole(AdventurePartyRole.Healer);

            if (resurrector != null)
            {
                return resurrector;
            }

            resurrector = FindResurrectionCandidateByRole(AdventurePartyRole.Mage);

            if (resurrector != null)
            {
                return resurrector;
            }

            resurrector = FindResurrectionCandidateByRole(AdventurePartyRole.Fighter);

            if (resurrector != null)
            {
                return resurrector;
            }

            return FindResurrectionCandidateByRole(AdventurePartyRole.Archer);
        }

        private BaseAdventurer FindResurrectionCandidateByRole(AdventurePartyRole role)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (member != null && member.Role == role && CanEventuallyResurrectPartyMember(member))
                {
                    return member;
                }
            }

            return null;
        }

        private bool CanEventuallyResurrectPartyMember(BaseAdventurer member)
        {
            if (!IsActive(member))
            {
                return false;
            }

            double magery = member.Skills[SkillName.Magery].Value;
            double healing = member.Skills[SkillName.Healing].Value;
            double anatomy = member.Skills[SkillName.Anatomy].Value;
            double chivalry = member.Skills[SkillName.Chivalry].Value;

            return magery >= 80.0 || (healing >= 80.0 && anatomy >= 80.0) || chivalry >= 80.0;
        }

        private Point3D GetResurrectionRallyPoint(BaseAdventurer resurrector, BaseAdventurer target, Mobile enemy)
        {
            if (IsResurrectionRallyUsable(resurrector, target))
            {
                return m_ResurrectionRallyPoint;
            }

            Point3D rally = FindResurrectionRallyPoint(resurrector, target, enemy);

            m_ResurrectionRallyCreated = DateTime.UtcNow;
            m_ResurrectionRallyPoint = rally;
            m_ResurrectionRallyExpires = DateTime.UtcNow + AdventurePartySettings.ResurrectionRallyHoldTime;
            m_ResurrectionTarget = target == null ? Serial.MinusOne : target.Serial;

            return rally;
        }

        private bool IsResurrectionRallyUsable(BaseAdventurer resurrector, BaseAdventurer target)
        {
            if (target == null || m_ResurrectionRallyPoint == Point3D.Zero ||
                m_ResurrectionTarget != target.Serial ||
                m_ResurrectionRallyExpires == DateTime.MinValue ||
                DateTime.UtcNow >= m_ResurrectionRallyExpires ||
                resurrector == null || resurrector.Map == null || resurrector.Map == Map.Internal)
            {
                return false;
            }

            if (!resurrector.Map.CanFit(m_ResurrectionRallyPoint.X, m_ResurrectionRallyPoint.Y, m_ResurrectionRallyPoint.Z, 16, false, true, true, resurrector))
            {
                return false;
            }

            int localEnemies = CountResurrectionEnemiesNearPoint(
                resurrector,
                m_ResurrectionRallyPoint,
                AdventurePartySettings.ResurrectionDangerRange);

            if (localEnemies >= AdventurePartySettings.ResurrectionRallyCriticalEnemyLimit)
            {
                return false;
            }

            bool minimumHoldActive = m_ResurrectionRallyCreated != DateTime.MinValue &&
                DateTime.UtcNow < m_ResurrectionRallyCreated + AdventurePartySettings.ResurrectionRallyMinimumHoldTime;

            return minimumHoldActive ||
                localEnemies < AdventurePartySettings.ResurrectionRallyUnsafeEnemyLimit;
        }

        private void ClearResurrectionRally()
        {
            m_ResurrectionRallyPoint = Point3D.Zero;
            m_ResurrectionRallyCreated = DateTime.MinValue;
            m_ResurrectionRallyExpires = DateTime.MinValue;
            m_ResurrectionTarget = Serial.MinusOne;
        }

        private Point3D FindResurrectionRallyPoint(BaseAdventurer resurrector, BaseAdventurer target, Mobile enemy)
        {
            Point3D fallback = target == null ? (resurrector == null ? Location : resurrector.Location) : target.Location;

            if (Map == null || Map == Map.Internal || resurrector == null || target == null)
            {
                return fallback;
            }

            List<Point3D> candidates = BuildResurrectionRallyCandidates(resurrector, target, enemy, fallback);
            Point3D best = fallback;
            double bestScore = ScoreResurrectionRallyPoint(resurrector, target, enemy, fallback);

            for (int i = 0; i < candidates.Count; i++)
            {
                Point3D candidate;

                if (!TryCreateResurrectionRallyPoint(resurrector, candidates[i], out candidate))
                {
                    continue;
                }

                double score = ScoreResurrectionRallyPoint(resurrector, target, enemy, candidate);

                if (score < bestScore)
                {
                    best = candidate;
                    bestScore = score;
                }
            }

            return best;
        }

        private List<Point3D> BuildResurrectionRallyCandidates(BaseAdventurer resurrector, BaseAdventurer target, Mobile enemy, Point3D fallback)
        {
            List<Point3D> candidates = new List<Point3D>();

            AddPullAnchorCandidate(candidates, fallback);
            AddPullAnchorCandidate(candidates, resurrector.Location);
            AddPullAnchorCandidate(candidates, new Point3D((resurrector.X + target.X) / 2, (resurrector.Y + target.Y) / 2, target.Z));

            if (m_TacticPoint != Point3D.Zero)
            {
                AddPullAnchorCandidate(candidates, m_TacticPoint);
            }

            if (m_Waypoints != null)
            {
                for (int i = 0; i < m_Waypoints.Length; i++)
                {
                    if (GetPointDistance(resurrector.Location, m_Waypoints[i]) <= AdventurePartySettings.ResurrectionAnchorSearchRadius + 8)
                    {
                        AddPullAnchorCandidate(candidates, m_Waypoints[i]);
                    }
                }
            }

            AddResurrectionRingCandidates(candidates, target.Location);
            AddResurrectionRingCandidates(candidates, resurrector.Location);

            if (enemy != null && !enemy.Deleted && enemy.Map == Map)
            {
                int awayX = Math.Sign(target.X - enemy.X);
                int awayY = Math.Sign(target.Y - enemy.Y);

                if (awayX == 0 && awayY == 0)
                {
                    awayY = 1;
                }

                for (int distance = 6; distance <= AdventurePartySettings.ResurrectionAnchorSearchRadius; distance += 4)
                {
                    AddPullAnchorCandidate(candidates, new Point3D(target.X + (awayX * distance), target.Y + (awayY * distance), target.Z));
                }
            }

            return candidates;
        }

        private void AddResurrectionRingCandidates(List<Point3D> candidates, Point3D center)
        {
            for (int distance = 4; distance <= AdventurePartySettings.ResurrectionAnchorSearchRadius; distance += 4)
            {
                for (int i = 0; i < 8; i++)
                {
                    int xOffset;
                    int yOffset;

                    GetDirectionOffset((Direction)i, out xOffset, out yOffset);
                    AddPullAnchorCandidate(candidates, new Point3D(center.X + (xOffset * distance), center.Y + (yOffset * distance), center.Z));
                }
            }
        }

        private bool TryCreateResurrectionRallyPoint(BaseAdventurer resurrector, Point3D raw, out Point3D point)
        {
            point = Point3D.Zero;

            if (resurrector == null || Map == null || Map == Map.Internal)
            {
                return false;
            }

            if (TryUseResurrectionRallyPoint(resurrector, raw, out point))
            {
                return true;
            }

            int averageZ = Map.GetAverageZ(raw.X, raw.Y);

            if (averageZ != raw.Z && TryUseResurrectionRallyPoint(resurrector, new Point3D(raw.X, raw.Y, averageZ), out point))
            {
                return true;
            }

            if (resurrector.Z != raw.Z && resurrector.Z != averageZ &&
                TryUseResurrectionRallyPoint(resurrector, new Point3D(raw.X, raw.Y, resurrector.Z), out point))
            {
                return true;
            }

            return false;
        }

        private bool TryUseResurrectionRallyPoint(BaseAdventurer resurrector, Point3D candidate, out Point3D point)
        {
            point = Point3D.Zero;

            if (!Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16, false, true, true, resurrector))
            {
                return false;
            }

            point = candidate;
            return true;
        }

        private double ScoreResurrectionRallyPoint(BaseAdventurer resurrector, BaseAdventurer target, Mobile enemy, Point3D point)
        {
            int localEnemies = CountResurrectionEnemiesNearPoint(resurrector, point, AdventurePartySettings.ResurrectionClearRange);
            double nearestEnemy = GetNearestResurrectionEnemyDistance(resurrector, point);
            double score = 0.0;

            score += localEnemies * 260.0;
            score += GetPointDistance(resurrector.Location, point) * 2.0;
            score += GetPointDistance(target.Location, point) * 3.0;

            if (nearestEnemy < AdventurePartySettings.ResurrectionDangerRange)
            {
                score += 300.0;
            }
            else if (nearestEnemy < AdventurePartySettings.ResurrectionClearRange)
            {
                score += 120.0;
            }

            if (enemy != null && !enemy.Deleted && enemy.Map == Map)
            {
                double enemyDistance = GetPointDistance(enemy.Location, point);

                if (enemyDistance < AdventurePartySettings.ResurrectionPullDistance)
                {
                    score += (AdventurePartySettings.ResurrectionPullDistance - enemyDistance) * 24.0;
                }

                if (!Map.LineOfSight(point, enemy.Location))
                {
                    score -= 18.0;
                }
            }

            return score;
        }

        private bool IsResurrectionReady(BaseAdventurer resurrector, BaseAdventurer target, Point3D rally)
        {
            return IsMovingResurrectionReady(resurrector, target);
        }

        private bool IsMovingResurrectionReady(BaseAdventurer resurrector, BaseAdventurer target)
        {
            return IsActive(resurrector) &&
                IsRecoverableDeadMember(target) &&
                resurrector.InRange(target, Bandage.Range) &&
                resurrector.InLOS(target) &&
                !HasRangedResurrectionPressure(resurrector, target) &&
                resurrector.CanBeBeneficial(target, false, true);
        }

        private bool HasRangedResurrectionPressure(BaseAdventurer resurrector, BaseAdventurer target)
        {
            return FindRangedResurrectionThreat(resurrector, target) != null;
        }

        private Mobile FindRangedResurrectionThreat(BaseAdventurer resurrector, BaseAdventurer target)
        {
            if (!IsActive(resurrector) || Map == null || Map == Map.Internal)
            {
                return null;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = resurrector.GetMobilesInRange(AdventurePartySettings.ResurrectionRangedThreatScanRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, resurrector) || !IsRangedPressureEnemy(mobile))
                {
                    continue;
                }

                bool seesResurrector = CanEnemyNoticePoint(mobile, resurrector.Location);
                bool seesTarget = IsRecoverableDeadMember(target) && CanEnemyNoticePoint(mobile, target.Location);

                if (!seesResurrector && !seesTarget)
                {
                    continue;
                }

                double score = 100.0 - resurrector.GetDistanceToSqrt(mobile);

                if (seesResurrector)
                {
                    score += 40.0;
                }

                if (seesTarget)
                {
                    score += 20.0;
                }

                if (mobile.Combatant == resurrector || mobile.Combatant == target)
                {
                    score += 30.0;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private void CoordinateResurrectionRecovery(BaseAdventurer resurrector, BaseAdventurer target, Point3D rally, Mobile enemy)
        {
            ChangeState(enemy == null ? AdventurePartyState.Resting : AdventurePartyState.Retreating);
            SetRecoveryTactic(target, rally);

            MoveGhostsToResurrectionRally(rally, resurrector, enemy);
            MoveResurrectorToMobileResurrection(resurrector, target, rally, enemy);
            PullThreatsAwayFromResurrectionRally(resurrector, target, rally, enemy);

            int localEnemies = CountResurrectionEnemiesNearPoint(resurrector, rally, AdventurePartySettings.ResurrectionClearRange);
            m_LastAIStatus = String.Format(
                "Recovery: moving resurrection pair toward ({0},{1},{2}); {3} hostile(s) near rally.",
                rally.X,
                rally.Y,
                rally.Z,
                localEnemies);
        }

        private void MoveGhostsToResurrectionRally(Point3D rally, BaseAdventurer resurrector, Mobile enemy)
        {
            int index = 0;
            BaseAdventurer observer = IsActive(resurrector) ? resurrector : GetLeader();

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsRecoverableDeadMember(member))
                {
                    continue;
                }

                member.Combatant = null;
                member.Warmode = false;

                Mobile threat = observer == null ? enemy :
                    FindNearestResurrectionThreat(observer, member.Location, AdventurePartySettings.ResurrectionGhostDangerRange) ??
                    FindNearestResurrectionThreat(observer, rally, AdventurePartySettings.ResurrectionGhostDangerRange) ??
                    enemy;
                Mobile rangedThreat = IsActive(resurrector) ? FindRangedResurrectionThreat(resurrector, member) : null;

                if (rangedThreat != null)
                {
                    threat = rangedThreat;
                }

                Point3D followPoint = GetResurrectionGhostFollowPoint(member, resurrector, rally, threat, index++);

                MoveMemberToward(member, followPoint, AdventurePartySettings.ResurrectionGhostRunSteps, true, true);
            }
        }

        private Point3D GetResurrectionGhostPoint(Point3D rally, int index)
        {
            switch (index % 4)
            {
                case 1:
                    return new Point3D(rally.X + 1, rally.Y, rally.Z);
                case 2:
                    return new Point3D(rally.X, rally.Y + 1, rally.Z);
                case 3:
                    return new Point3D(rally.X + 1, rally.Y + 1, rally.Z);
                default:
                    return rally;
            }
        }

        private Point3D GetResurrectionGhostFollowPoint(
            BaseAdventurer ghost,
            BaseAdventurer resurrector,
            Point3D rally,
            Mobile enemy,
            int index)
        {
            BaseAdventurer observer = IsActive(resurrector) ? resurrector : GetLeader();
            Point3D anchor = IsActive(resurrector) ? resurrector.Location : rally;
            Point3D fallback = GetResurrectionGhostPoint(anchor, index);

            if (ghost == null || observer == null || Map == null || Map == Map.Internal)
            {
                return fallback;
            }

            Point3D best = Point3D.Zero;
            double bestScore = Double.MinValue;

            for (int radius = 0; radius <= 4; radius++)
            {
                if (radius == 0)
                {
                    ScoreResurrectionGhostCandidate(ghost, observer, resurrector, rally, enemy, anchor, ref best, ref bestScore);
                    continue;
                }

                for (int i = 0; i < 8; i++)
                {
                    int xOffset;
                    int yOffset;

                    GetDirectionOffset((Direction)i, out xOffset, out yOffset);
                    Point3D raw = new Point3D(anchor.X + (xOffset * radius), anchor.Y + (yOffset * radius), anchor.Z);
                    ScoreResurrectionGhostCandidate(ghost, observer, resurrector, rally, enemy, raw, ref best, ref bestScore);
                }
            }

            return best == Point3D.Zero ? fallback : best;
        }

        private void ScoreResurrectionGhostCandidate(
            BaseAdventurer ghost,
            BaseAdventurer observer,
            BaseAdventurer resurrector,
            Point3D rally,
            Mobile enemy,
            Point3D raw,
            ref Point3D best,
            ref double bestScore)
        {
            Point3D point;

            if (!TryCreateResurrectionGhostPoint(ghost, raw, out point))
            {
                return;
            }

            double score = ScoreResurrectionGhostPoint(ghost, observer, resurrector, rally, enemy, point);

            if (best == Point3D.Zero || score > bestScore)
            {
                best = point;
                bestScore = score;
            }
        }

        private bool TryCreateResurrectionGhostPoint(BaseAdventurer ghost, Point3D raw, out Point3D point)
        {
            point = Point3D.Zero;

            if (ghost == null || Map == null || Map == Map.Internal)
            {
                return false;
            }

            if (TryUseResurrectionGhostPoint(ghost, raw, out point))
            {
                return true;
            }

            int averageZ = Map.GetAverageZ(raw.X, raw.Y);

            if (averageZ != raw.Z && TryUseResurrectionGhostPoint(ghost, new Point3D(raw.X, raw.Y, averageZ), out point))
            {
                return true;
            }

            if (ghost.Z != raw.Z && ghost.Z != averageZ &&
                TryUseResurrectionGhostPoint(ghost, new Point3D(raw.X, raw.Y, ghost.Z), out point))
            {
                return true;
            }

            return false;
        }

        private bool TryUseResurrectionGhostPoint(BaseAdventurer ghost, Point3D candidate, out Point3D point)
        {
            point = Point3D.Zero;

            if (!Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16, false, true, true, ghost))
            {
                return false;
            }

            point = candidate;
            return true;
        }

        private double ScoreResurrectionGhostPoint(
            BaseAdventurer ghost,
            BaseAdventurer observer,
            BaseAdventurer resurrector,
            Point3D rally,
            Mobile enemy,
            Point3D point)
        {
            double nearestEnemy = GetNearestResurrectionEnemyDistance(observer, point);
            int localEnemies = CountResurrectionEnemiesNearPoint(observer, point, AdventurePartySettings.ResurrectionDangerRange);
            Point3D followAnchor = IsActive(resurrector) ? resurrector.Location : rally;
            double followDistance = GetPointDistance(point, followAnchor);
            double score = 0.0;

            score -= ghost.GetDistanceToSqrt(point) * 2.0;
            score -= Math.Abs(followDistance - AdventurePartySettings.ResurrectionGhostFollowRange) * 45.0;
            score -= GetPointDistance(point, rally) * 1.0;
            score -= localEnemies * 180.0;

            if (nearestEnemy < AdventurePartySettings.ResurrectionDangerRange)
            {
                score -= (AdventurePartySettings.ResurrectionDangerRange - nearestEnemy) * 90.0;
            }
            else if (nearestEnemy < Double.MaxValue)
            {
                score += Math.Min(80.0, nearestEnemy * 8.0);
            }

            if (enemy != null && !enemy.Deleted && enemy.Map == Map)
            {
                double enemyDistance = GetPointDistance(point, enemy.Location);

                if (enemyDistance < AdventurePartySettings.ResurrectionGhostDangerRange)
                {
                    score -= (AdventurePartySettings.ResurrectionGhostDangerRange - enemyDistance) * 80.0;
                }

                if (!Map.LineOfSight(point, enemy.Location))
                {
                    score += 18.0;
                }
            }

            return score;
        }

        private void MoveResurrectorToMobileResurrection(BaseAdventurer resurrector, BaseAdventurer target, Point3D rally, Mobile enemy)
        {
            if (!IsActive(resurrector))
            {
                return;
            }

            resurrector.Combatant = null;
            resurrector.Warmode = false;

            if (IsRecoverableDeadMember(target) && !resurrector.InRange(target, Bandage.Range))
            {
                MoveMemberToward(resurrector, target.Location, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                return;
            }

            if (IsRecoverableDeadMember(target) && !resurrector.InLOS(target))
            {
                MoveMemberToward(resurrector, target.Location, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                return;
            }

            Mobile rangedThreat = FindRangedResurrectionThreat(resurrector, target);

            if (rangedThreat != null)
            {
                Point3D breakPoint = GetPointAwayFrom(
                    resurrector.Location,
                    rangedThreat.Location,
                    AdventurePartySettings.ResurrectionRangedThreatBreakDistance);

                MoveMemberToward(resurrector, breakPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                return;
            }

            Mobile threat = FindSelfPreservationThreat(resurrector, enemy, AdventurePartySettings.PullBreakPursuitScanRange);

            if (threat != null && resurrector.InRange(threat, AdventurePartySettings.BacklineMeleeThreatRange))
            {
                MoveMemberAwayFrom(resurrector, threat.Location, AdventurePartySettings.BacklineThreatSafeRange, true, true);
                return;
            }

            if (!resurrector.InRange(rally, 1))
            {
                MoveMemberToward(resurrector, rally, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
            }
        }

        private void PullThreatsAwayFromResurrectionRally(BaseAdventurer resurrector, BaseAdventurer target, Point3D rally, Mobile enemy)
        {
            Mobile threat = FindNearestResurrectionThreat(resurrector, rally, AdventurePartySettings.ResurrectionPullDistance);

            if (threat == null)
            {
                threat = enemy;
            }

            Point3D pullPoint = threat == null ? rally : GetPointAwayFrom(rally, threat.Location, AdventurePartySettings.ResurrectionPullDistance);

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) || member == resurrector)
                {
                    continue;
                }

                if (member.Role == AdventurePartyRole.Fighter)
                {
                    if (threat != null && IsValidEnemy(threat, member))
                    {
                        member.Combatant = threat;
                    }

                    if (threat != null && ShouldFighterPullAwayDuringRecovery(member, rally, threat))
                    {
                        MoveMemberToward(member, pullPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                    }
                    else if (threat != null && IsValidEnemy(threat, member) && !member.InRange(threat, 2))
                    {
                        MoveMemberToward(member, threat.Location, GetEngageMoveSteps(member, threat), false, true);
                    }
                    else
                    {
                        MoveMemberToward(member, pullPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
                    }
                }
                else
                {
                    member.Combatant = null;
                    member.Warmode = false;

                    Mobile selfThreat = FindSelfPreservationThreat(member, threat, AdventurePartySettings.BacklineThreatRange);

                    if (selfThreat != null && member.InRange(selfThreat, AdventurePartySettings.BacklineThreatRange) &&
                        member.InRange(rally, AdventurePartySettings.RecoveryHardRegroupDistance))
                    {
                        MoveMemberAwayFrom(member, selfThreat.Location, AdventurePartySettings.BacklineThreatSafeRange, true, true);
                    }
                    else
                    {
                        MoveMemberToRecoveryFormation(member, rally);
                    }
                }
            }
        }

        private void MoveMemberToRecoveryFormation(BaseAdventurer member, Point3D rally)
        {
            if (!IsActive(member))
            {
                return;
            }

            Point3D formationAnchor = GetRecoveryFormationAnchor(member, rally);
            Point3D formationPoint = GetFormationPoint(formationAnchor, member.Role);

            if (!member.InRange(formationPoint, 1))
            {
                MoveMemberToward(member, formationPoint, AdventurePartySettings.RecoveryRegroupRunSteps, true, true);
            }
        }

        private Point3D GetRecoveryFormationAnchor(BaseAdventurer member, Point3D rally)
        {
            BaseAdventurer leader = GetLeader();

            if (member != leader && IsActive(leader) &&
                !member.InRange(leader, AdventurePartySettings.RangedDisengageFormationRegroupDistance))
            {
                return leader.Location;
            }

            return rally;
        }

        private bool ShouldFighterPullAwayDuringRecovery(BaseAdventurer fighter, Point3D rally, Mobile threat)
        {
            if (!IsActive(fighter) || threat == null)
            {
                return false;
            }

            int hitPercent = fighter.HitsMax <= 0 ? 0 : (fighter.Hits * 100) / fighter.HitsMax;
            int nearbyEnemies = CountNearbyEnemies(fighter, AdventurePartySettings.BacklineThreatRange);
            int directPursuers = CountDirectPursuers(fighter, AdventurePartySettings.ResurrectionPullDistance);

            return fighter.InRange(rally, AdventurePartySettings.ResurrectionClearRange) ||
                hitPercent < 65 ||
                nearbyEnemies >= 3 ||
                directPursuers >= 2;
        }

        private Point3D GetPointAwayFrom(Point3D protectedPoint, Point3D danger, int distance)
        {
            int dx = protectedPoint.X - danger.X;
            int dy = protectedPoint.Y - danger.Y;

            if (dx == 0 && dy == 0)
            {
                dy = 1;
            }

            return new Point3D(
                protectedPoint.X + (Math.Sign(dx) * distance),
                protectedPoint.Y + (Math.Sign(dy) * distance),
                protectedPoint.Z);
        }

        private int CountResurrectionEnemiesNearPoint(BaseAdventurer observer, Point3D point, int range)
        {
            int count = 0;

            if (observer == null || Map == null || Map == Map.Internal)
            {
                return count;
            }

            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, observer))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private double GetNearestResurrectionEnemyDistance(BaseAdventurer observer, Point3D point)
        {
            double nearest = Double.MaxValue;

            if (observer == null || Map == null || Map == Map.Internal)
            {
                return nearest;
            }

            IPooledEnumerable eable = Map.GetMobilesInRange(point, AdventurePartySettings.ResurrectionPullDistance);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(mobile, observer))
                {
                    nearest = Math.Min(nearest, GetPointDistance(point, mobile.Location));
                }
            }

            eable.Free();
            return nearest;
        }

        private Mobile FindNearestResurrectionThreat(BaseAdventurer observer, Point3D point, int range)
        {
            Mobile best = null;
            double bestDistance = Double.MaxValue;

            if (observer == null || Map == null || Map == Map.Internal)
            {
                return null;
            }

            IPooledEnumerable eable = Map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, observer))
                {
                    continue;
                }

                double distance = GetPointDistance(point, mobile.Location);

                if (best == null || distance < bestDistance)
                {
                    best = mobile;
                    bestDistance = distance;
                }
            }

            eable.Free();
            return best;
        }

        private BaseAdventurer FindResurrector()
        {
            BaseAdventurer resurrector = FindResurrectorByRole(AdventurePartyRole.Healer);

            if (resurrector != null)
            {
                return resurrector;
            }

            resurrector = FindResurrectorByRole(AdventurePartyRole.Mage);

            if (resurrector != null)
            {
                return resurrector;
            }

            resurrector = FindResurrectorByRole(AdventurePartyRole.Fighter);

            if (resurrector != null)
            {
                return resurrector;
            }

            return FindResurrectorByRole(AdventurePartyRole.Archer);
        }

        private BaseAdventurer FindResurrectorByRole(AdventurePartyRole role)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (member != null && member.Role == role && CanResurrectPartyMember(member))
                {
                    return member;
                }
            }

            return null;
        }

        private bool CanResurrectPartyMember(BaseAdventurer member)
        {
            if (!IsActive(member) || member.NextPartyAction > DateTime.UtcNow)
            {
                return false;
            }

            double magery = member.Skills[SkillName.Magery].Value;
            double healing = member.Skills[SkillName.Healing].Value;
            double anatomy = member.Skills[SkillName.Anatomy].Value;
            double chivalry = member.Skills[SkillName.Chivalry].Value;

            return magery >= 80.0 || (healing >= 80.0 && anatomy >= 80.0) || chivalry >= 80.0;
        }

        private BaseAdventurer FindDeadMemberForResurrection(BaseAdventurer resurrector)
        {
            BaseAdventurer target = null;
            double bestDistance = Double.MaxValue;

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsRecoverableDeadMember(member))
                {
                    continue;
                }

                double distance = resurrector.GetDistanceToSqrt(member);

                if (target == null || distance < bestDistance)
                {
                    target = member;
                    bestDistance = distance;
                }
            }

            return target;
        }

        private void MovePartyToward(Point3D target)
        {
            MovePartyToward(target, false);
        }

        private void MovePartyToward(Point3D target, bool running)
        {
            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member))
                {
                    continue;
                }

                MoveMemberToward(member, GetFormationPoint(target, member.Role), running);
            }
        }

        private void MovePartyExploring(BaseAdventurer leader, Point3D waypoint, bool compact)
        {
            if (!IsActive(leader))
            {
                MovePartyToward(waypoint);
                return;
            }

            if (compact || !leader.InRange(waypoint, 2))
            {
                MoveMemberToward(leader, waypoint);
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) || member == leader)
                {
                    continue;
                }

                Point3D formationPoint = GetFormationPoint(leader.Location, member.Role);

                if (member.InRange(formationPoint, 1))
                {
                    continue;
                }

                if (!member.InRange(leader, AdventurePartySettings.ExplorationFormationRegroupDistance))
                {
                    MoveMemberToward(member, formationPoint, AdventurePartySettings.ExplorationRegroupRunSteps, true, true);
                }
                else
                {
                    MoveMemberToward(member, formationPoint);
                }
            }

            if (!compact)
            {
                m_LastAIStatus = "Explore: regrouping compact formation before advancing waypoint.";
            }
        }

        private bool IsExplorationFormationCompact(BaseAdventurer leader)
        {
            if (!IsActive(leader))
            {
                return true;
            }

            for (int i = 0; i < m_Members.Count; i++)
            {
                BaseAdventurer member = m_Members[i];

                if (!IsActive(member) || member == leader)
                {
                    continue;
                }

                if (!member.InRange(leader, AdventurePartySettings.ExplorationFormationMaxDistance))
                {
                    return false;
                }
            }

            return true;
        }

        private Point3D GetFormationPoint(Point3D target, AdventurePartyRole role)
        {
            if (m_CurrentTactic == AdventurePartyTactic.PullToChokePoint)
            {
                return GetPullFormationPoint(target, role);
            }

            if (IsChokeTactic(m_CurrentTactic))
            {
                switch (role)
                {
                    case AdventurePartyRole.Archer:
                        return new Point3D(target.X - 1, target.Y + 2, target.Z);
                    case AdventurePartyRole.Mage:
                        return new Point3D(target.X + 1, target.Y + 2, target.Z);
                    case AdventurePartyRole.Healer:
                        return new Point3D(target.X, target.Y + 4, target.Z);
                    default:
                        return target;
                }
            }

            if (m_CurrentTactic == AdventurePartyTactic.ProtectHealer)
            {
                switch (role)
                {
                    case AdventurePartyRole.Fighter:
                        return new Point3D(target.X, target.Y - 1, target.Z);
                    case AdventurePartyRole.Archer:
                        return new Point3D(target.X - 2, target.Y + 1, target.Z);
                    case AdventurePartyRole.Mage:
                        return new Point3D(target.X + 2, target.Y + 1, target.Z);
                    case AdventurePartyRole.Healer:
                        return new Point3D(target.X, target.Y + 2, target.Z);
                }
            }

            switch (role)
            {
                case AdventurePartyRole.Archer:
                    return new Point3D(target.X - 1, target.Y + 1, target.Z);
                case AdventurePartyRole.Mage:
                    return new Point3D(target.X + 1, target.Y + 1, target.Z);
                case AdventurePartyRole.Healer:
                    return new Point3D(target.X, target.Y + 2, target.Z);
                default:
                    return target;
            }
        }

        private Point3D GetPullFormationPoint(Point3D anchor, AdventurePartyRole role)
        {
            Mobile target = FindMobileBySerial(m_TacticTarget);
            Point3D threat = target == null || target.Map != Map ? Point3D.Zero : target.Location;

            return GetPullFormationPoint(anchor, role, threat);
        }

        private static Point3D GetPullFormationPoint(Point3D anchor, AdventurePartyRole role, Point3D threat)
        {
            int backX;
            int backY;

            GetPullFormationBackVector(anchor, threat, out backX, out backY);

            int sideX = -backY;
            int sideY = backX;

            switch (role)
            {
                case AdventurePartyRole.Archer:
                    return new Point3D(anchor.X + (backX * 2) - sideX, anchor.Y + (backY * 2) - sideY, anchor.Z);
                case AdventurePartyRole.Mage:
                    return new Point3D(anchor.X + (backX * 2) + sideX, anchor.Y + (backY * 2) + sideY, anchor.Z);
                case AdventurePartyRole.Healer:
                    return new Point3D(anchor.X + (backX * 4), anchor.Y + (backY * 4), anchor.Z);
                default:
                    return anchor;
            }
        }

        private static void GetPullFormationBackVector(Point3D anchor, Point3D threat, out int backX, out int backY)
        {
            backX = 0;
            backY = 1;

            if (threat == Point3D.Zero)
            {
                return;
            }

            int xDelta = anchor.X - threat.X;
            int yDelta = anchor.Y - threat.Y;

            if (Math.Abs(xDelta) >= Math.Abs(yDelta))
            {
                backX = Math.Sign(xDelta);
                backY = 0;
            }
            else
            {
                backX = 0;
                backY = Math.Sign(yDelta);
            }

            if (backX == 0 && backY == 0)
            {
                backY = 1;
            }
        }

        private Point3D GetSpreadPoint(Point3D target, AdventurePartyRole role)
        {
            switch (role)
            {
                case AdventurePartyRole.Fighter:
                    return new Point3D(target.X - 2, target.Y, target.Z);
                case AdventurePartyRole.Archer:
                    return new Point3D(target.X + 2, target.Y, target.Z);
                case AdventurePartyRole.Mage:
                    return new Point3D(target.X, target.Y - 2, target.Z);
                case AdventurePartyRole.Healer:
                    return new Point3D(target.X, target.Y + 2, target.Z);
                default:
                    return target;
            }
        }

        private Point3D GetInterceptPoint(Point3D protectedPoint, Point3D threatPoint)
        {
            int x = protectedPoint.X + Math.Sign(threatPoint.X - protectedPoint.X);
            int y = protectedPoint.Y + Math.Sign(threatPoint.Y - protectedPoint.Y);

            return new Point3D(x, y, protectedPoint.Z);
        }

        private void KeepNearLeader(BaseAdventurer member, int maxDistance)
        {
            BaseAdventurer leader = GetLeader();

            if (leader != null && !member.InRange(leader, maxDistance))
            {
                MoveMemberToward(member, GetFormationPoint(leader.Location, member.Role));
            }
        }

        private void MoveMemberAwayFrom(BaseAdventurer member, Point3D danger, int desiredDistance)
        {
            MoveMemberAwayFrom(member, danger, desiredDistance, false, true);
        }

        private void MoveMemberAwayFrom(BaseAdventurer member, Point3D danger, int desiredDistance, bool breakMasteryHold)
        {
            MoveMemberAwayFrom(member, danger, desiredDistance, breakMasteryHold, true);
        }

        private void MoveMemberAwayFrom(BaseAdventurer member, Point3D danger, int desiredDistance, bool breakMasteryHold, bool running)
        {
            if (member == null || member.Deleted || member.Map != Map)
            {
                return;
            }

            if (!breakMasteryHold && ShouldHoldPositionForMastery(member))
            {
                return;
            }

            if (member.GetDistanceToSqrt(danger) >= desiredDistance)
            {
                return;
            }

            int dx = member.X - danger.X;
            int dy = member.Y - danger.Y;

            if (dx == 0 && dy == 0)
            {
                dx = Utility.RandomBool() ? 1 : -1;
                dy = Utility.RandomBool() ? 1 : -1;
            }

            Point3D target = new Point3D(member.X + Math.Sign(dx) * 3, member.Y + Math.Sign(dy) * 3, member.Z);
            int steps = running ? AdventurePartySettings.BacklineEvasiveMoveSteps : 1;
            MoveMemberToward(member, target, steps, breakMasteryHold, running);
        }

        private void MoveMemberToward(BaseAdventurer member, Point3D target)
        {
            MoveMemberToward(member, target, 1, false);
        }

        private void MoveMemberToward(BaseAdventurer member, Point3D target, bool running)
        {
            MoveMemberToward(member, target, 1, false, running);
        }

        private void MoveMemberToward(BaseAdventurer member, Point3D target, int steps)
        {
            MoveMemberToward(member, target, steps, false);
        }

        private void MoveMemberToward(BaseAdventurer member, Point3D target, int steps, bool breakMasteryHold)
        {
            MoveMemberToward(member, target, steps, breakMasteryHold, false);
        }

        private void MoveMemberToward(BaseAdventurer member, Point3D target, int steps, bool breakMasteryHold, bool running)
        {
            if (member == null || member.Deleted || member.Map != Map)
            {
                return;
            }

            if (!breakMasteryHold && ShouldHoldPositionForMastery(member))
            {
                return;
            }

            steps = Math.Max(1, steps);

            if (TryMoveMemberOneStepToward(member, target, running) && steps > 1)
            {
                QueueMovePulse(member, target, steps - 1, breakMasteryHold, running);
            }
        }

        private bool TryMoveMemberOneStepToward(BaseAdventurer member, Point3D target, bool running)
        {
            if (member == null || member.Deleted || member.Map != Map || member.InRange(target, 0))
            {
                return false;
            }

            Direction direction = member.GetDirectionTo(target);
            Direction masked = direction & Direction.Mask;

            if (!ShouldAvoidRangedPressureOnMove(member, running))
            {
                if (TryMove(member, direction, running))
                {
                    return true;
                }

                return TryMove(member, (Direction)(((int)masked + 1) & 0x7), running) ||
                    TryMove(member, (Direction)(((int)masked + 7) & 0x7), running);
            }

            Direction[] candidates = new Direction[]
            {
                masked,
                (Direction)(((int)masked + 1) & 0x7),
                (Direction)(((int)masked + 7) & 0x7),
                (Direction)(((int)masked + 2) & 0x7),
                (Direction)(((int)masked + 6) & 0x7)
            };
            double[] scores = new double[candidates.Length];
            bool[] tried = new bool[candidates.Length];

            for (int i = 0; i < candidates.Length; i++)
            {
                Point3D candidatePoint = GetPointInDirection(member.Location, candidates[i]);
                scores[i] = GetPointDistance(candidatePoint, target) + GetMoveStepDangerPenalty(member, candidatePoint);
            }

            for (int attempt = 0; attempt < candidates.Length; attempt++)
            {
                int best = -1;
                double bestScore = Double.MaxValue;

                for (int i = 0; i < candidates.Length; i++)
                {
                    if (!tried[i] && scores[i] < bestScore)
                    {
                        best = i;
                        bestScore = scores[i];
                    }
                }

                if (best < 0)
                {
                    break;
                }

                tried[best] = true;

                if (TryMove(member, candidates[best], running))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ShouldAvoidRangedPressureOnMove(BaseAdventurer member, bool running)
        {
            return running &&
                (m_RangedDisengageActive ||
                 m_ResetPullActive ||
                 State == AdventurePartyState.Retreating ||
                 m_CurrentTactic == AdventurePartyTactic.Recovering);
        }

        private double GetMoveStepDangerPenalty(BaseAdventurer member, Point3D point)
        {
            if (member == null || member.Map != Map)
            {
                return 0.0;
            }

            double penalty = 0.0;
            IPooledEnumerable eable = member.GetMobilesInRange(AdventurePartySettings.BacklineThreatSafeRange + 4);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, member))
                {
                    continue;
                }

                double distance = GetPointDistance(point, mobile.Location);

                if (distance <= AdventurePartySettings.BacklineMeleeThreatRange)
                {
                    penalty += 600.0;
                }

                if (IsRangedPressureEnemy(mobile))
                {
                    if (distance <= AdventurePartySettings.RangedDisengageRouteThreatRange)
                    {
                        penalty += (AdventurePartySettings.RangedDisengageRouteThreatRange - distance + 1.0) * 160.0;
                    }

                    if (CanEnemyNoticePoint(mobile, point))
                    {
                        penalty += 120.0;
                    }

                    if (IsDragonBreathThreat(mobile))
                    {
                        penalty += 180.0;
                    }
                }
            }

            eable.Free();
            return penalty;
        }

        private void QueueMovePulse(BaseAdventurer member, Point3D target, int remainingSteps, bool breakMasteryHold, bool running)
        {
            if (!CanContinueMovePulse(member) || remainingSteps <= 0)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            DateTime queuedUntil;

            if (m_MovePulseUntil.TryGetValue(member.Serial, out queuedUntil) && queuedUntil > now)
            {
                return;
            }

            m_MovePulseUntil[member.Serial] = now + TimeSpan.FromMilliseconds((remainingSteps * AdventurePartySettings.MovePulseDelayMilliseconds) + 100);

            Serial serial = member.Serial;

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(AdventurePartySettings.MovePulseDelayMilliseconds),
                delegate { ContinueMovePulse(serial, target, remainingSteps, breakMasteryHold, running); });
        }

        private void ContinueMovePulse(Serial serial, Point3D target, int remainingSteps, bool breakMasteryHold, bool running)
        {
            BaseAdventurer member = World.FindMobile(serial) as BaseAdventurer;

            if (!CanContinueMovePulse(member) || member.Controller != this || remainingSteps <= 0 ||
                (!breakMasteryHold && ShouldHoldPositionForMastery(member)))
            {
                m_MovePulseUntil.Remove(serial);
                return;
            }

            bool moved = TryMoveMemberOneStepToward(member, target, running);

            if (!moved || remainingSteps <= 1)
            {
                m_MovePulseUntil.Remove(serial);
                return;
            }

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(AdventurePartySettings.MovePulseDelayMilliseconds),
                delegate { ContinueMovePulse(serial, target, remainingSteps - 1, breakMasteryHold, running); });
        }

        private bool CanContinueMovePulse(BaseAdventurer member)
        {
            return IsActive(member) || IsRecoverableDeadMember(member);
        }

        private bool TryMove(BaseAdventurer member, Direction direction)
        {
            return TryMove(member, direction, false);
        }

        private int GetBreakPursuitMoveSteps(BaseAdventurer puller)
        {
            return puller != null && puller.Role == AdventurePartyRole.Fighter ?
                AdventurePartySettings.FighterPressureBreakPursuitRunSteps :
                AdventurePartySettings.BacklineEvasiveMoveSteps;
        }

        private bool TryMove(BaseAdventurer member, Direction direction, bool running)
        {
            Direction masked = direction & Direction.Mask;
            member.Direction = masked;
            Direction moveDirection = GetMoveDirection(masked, running);

            if (member.Move(moveDirection))
            {
                return true;
            }

            if (TryOpenDoorForMove(member, masked))
            {
                return member.Move(moveDirection);
            }

            return false;
        }

        private static Direction GetMoveDirection(Direction direction, bool running)
        {
            Direction masked = direction & Direction.Mask;
            return running ? (masked | Direction.Running) : masked;
        }

        private bool TryOpenDoorForMove(BaseAdventurer member, Direction direction)
        {
            if (member == null || member.Deleted || member.Map == null || member.Map == Map.Internal)
            {
                return false;
            }

            Point3D next = GetPointInDirection(member.Location, direction & Direction.Mask);
            IPooledEnumerable eable = member.Map.GetItemsInRange(next, 1);
            BaseDoor doorToOpen = null;

            foreach (Item item in eable)
            {
                BaseDoor door = item as BaseDoor;

                if (door == null || door.Deleted || door.Open || door.Map != member.Map ||
                    door.X != next.X || door.Y != next.Y ||
                    !member.InRange(door.GetWorldLocation(), 1) ||
                    (door.Z + door.ItemData.Height) <= member.Z ||
                    (member.Z + 16) <= door.Z ||
                    (door.Locked && door.UseLocks()))
                {
                    continue;
                }

                doorToOpen = door;
                break;
            }

            eable.Free();

            if (doorToOpen == null)
            {
                return false;
            }

            doorToOpen.Use(member);
            return true;
        }

        private int GetDesiredAttackRange(BaseAdventurer member)
        {
            if (member == null)
            {
                return 8;
            }

            if (member.Role == AdventurePartyRole.Fighter)
            {
                return 1;
            }

            if (member.Role == AdventurePartyRole.Archer && AdvancedCombatBrain.IsPlayingTheOddsActive(member))
            {
                return 5;
            }

            return 8;
        }

        private int GetMinimumAttackRange(BaseAdventurer member)
        {
            if (member == null || member.Role == AdventurePartyRole.Fighter)
            {
                return 1;
            }

            if (member.Role == AdventurePartyRole.Archer && AdvancedCombatBrain.IsPlayingTheOddsActive(member))
            {
                return 3;
            }

            if (member.Role == AdventurePartyRole.Healer)
            {
                return 6;
            }

            return 5;
        }

        private void MaintainAttackRange(BaseAdventurer member, Mobile enemy)
        {
            if (member == null || enemy == null || enemy.Deleted || !enemy.Alive)
            {
                return;
            }

            if (member.Role != AdventurePartyRole.Fighter && AvoidNearbyBacklineThreat(member, enemy))
            {
                return;
            }

            int minimumRange = GetMinimumAttackRange(member);
            int desiredRange = GetDesiredAttackRange(member);

            if (member.InRange(enemy, minimumRange - 1))
            {
                MoveMemberAwayFrom(member, enemy.Location, minimumRange);
            }
            else if (!member.InRange(enemy, desiredRange))
            {
                MoveMemberToward(member, enemy.Location, GetEngageMoveSteps(member, enemy), false, true);
            }
        }

        private int GetEngageMoveSteps(BaseAdventurer member, Mobile enemy)
        {
            if (ShouldUseIsolatedBacklineInterceptMove(member, enemy))
            {
                return AdventurePartySettings.IsolatedBacklineInterceptMoveSteps;
            }

            if (member != null && enemy != null && member.Role == AdventurePartyRole.Fighter &&
                m_CurrentTactic == AdventurePartyTactic.PowerUp &&
                enemy.HitsMax > 0 &&
                GetHitPercent(enemy) <= AdventurePartySettings.FleeingTargetFinishHitPercent &&
                member.GetDistanceToSqrt(enemy) >= AdventurePartySettings.FighterFastEngageDistance)
            {
                return AdventurePartySettings.FleeingTargetFinishMoveSteps;
            }

            if (member != null && enemy != null && member.Role == AdventurePartyRole.Fighter &&
                member.GetDistanceToSqrt(enemy) >= AdventurePartySettings.FighterFastEngageDistance)
            {
                return AdventurePartySettings.FighterFastEngageSteps;
            }

            return 1;
        }

        private bool ShouldUseIsolatedBacklineInterceptMove(BaseAdventurer member, Mobile enemy)
        {
            if (member == null || enemy == null || enemy.Deleted || !enemy.Alive ||
                member.Role != AdventurePartyRole.Fighter ||
                m_CurrentTactic != AdventurePartyTactic.PowerUp ||
                member.GetDistanceToSqrt(enemy) < AdventurePartySettings.FighterFastEngageDistance)
            {
                return false;
            }

            if (CountOtherEnemiesNearParty(enemy, AdventurePartySettings.IsolatedBacklineInterceptSafeRange) > 0)
            {
                return false;
            }

            if (HasIsolatedBacklineInterceptPackRisk(enemy, member))
            {
                return false;
            }

            return IsPressuringOrDuelingBackline(enemy);
        }

        private bool AvoidNearbyBacklineThreat(BaseAdventurer member, Mobile currentTarget)
        {
            Mobile threat = FindNearbyBacklineThreat(member, currentTarget);

            if (threat == null)
            {
                return false;
            }

            if (!TryCircleBacklineAroundFighter(member, threat))
            {
                MoveMemberAwayFrom(member, threat.Location, AdventurePartySettings.BacklineThreatSafeRange, true);
            }

            member.Combatant = null;
            member.Warmode = false;
            return true;
        }

        private bool TryCircleBacklineAroundFighter(BaseAdventurer member, Mobile threat)
        {
            if (!IsActive(member) || member.Role == AdventurePartyRole.Fighter ||
                threat == null || threat.Deleted || !threat.Alive || threat.Map != Map)
            {
                return false;
            }

            BaseAdventurer fighter = FindActiveMemberByRole(AdventurePartyRole.Fighter);

            if (!IsActive(fighter) || fighter == member)
            {
                return false;
            }

            Point3D best = Point3D.Zero;
            double bestScore = Double.MaxValue;

            for (int radius = AdventurePartySettings.BacklineOrbitFighterRange - 1;
                 radius <= AdventurePartySettings.BacklineOrbitFighterRange + 2;
                 radius++)
            {
                for (int i = 0; i < 8; i++)
                {
                    int xOffset;
                    int yOffset;

                    GetDirectionOffset((Direction)i, out xOffset, out yOffset);

                    Point3D candidate = new Point3D(
                        fighter.X + (xOffset * radius),
                        fighter.Y + (yOffset * radius),
                        fighter.Z);

                    if (!Map.CanFit(candidate.X, candidate.Y, candidate.Z, 16, false, true, true, member))
                    {
                        continue;
                    }

                    double threatDistance = GetPointDistance(candidate, threat.Location);
                    double fighterDistance = GetPointDistance(candidate, fighter.Location);

                    if (fighterDistance > AdventurePartySettings.BacklineOrbitFighterMaxRange)
                    {
                        continue;
                    }

                    double score = member.GetDistanceToSqrt(candidate) * 2.0;
                    score += Math.Abs(fighterDistance - AdventurePartySettings.BacklineOrbitFighterRange) * 6.0;

                    if (threatDistance < AdventurePartySettings.BacklineThreatSafeRange)
                    {
                        score += (AdventurePartySettings.BacklineThreatSafeRange - threatDistance) * 40.0;
                    }
                    else
                    {
                        score -= Math.Min(24.0, (threatDistance - AdventurePartySettings.BacklineThreatSafeRange + 1.0) * 4.0);
                    }

                    score += CountPullEnemiesNearPoint(member, candidate, AdventurePartySettings.BacklineMeleeThreatRange) * 70.0;
                    score += CountPullEnemiesNearPoint(member, candidate, AdventurePartySettings.BacklineThreatRange) * 25.0;

                    if (GetPointDistance(candidate, threat.Location) > member.GetDistanceToSqrt(threat))
                    {
                        score -= 12.0;
                    }

                    if (best == Point3D.Zero || score < bestScore)
                    {
                        best = candidate;
                        bestScore = score;
                    }
                }
            }

            if (best == Point3D.Zero)
            {
                return false;
            }

            MoveMemberToward(member, best, AdventurePartySettings.BacklineEvasiveMoveSteps, true, true);
            return true;
        }

        private Mobile FindNearbyBacklineThreat(BaseAdventurer member, Mobile currentTarget)
        {
            if (!IsActive(member) || member.Role == AdventurePartyRole.Fighter)
            {
                return null;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = member.GetMobilesInRange(AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(mobile, member))
                {
                    continue;
                }

                double distance = Math.Max(0.5, member.GetDistanceToSqrt(mobile));

                if (mobile == currentTarget && !IsImmediateBacklineThreat(member, mobile, distance))
                {
                    continue;
                }

                double score = 20.0 - distance;

                if (mobile.Combatant == member)
                {
                    score += 10.0;
                }

                if (mobile == currentTarget)
                {
                    score += 6.0;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private bool IsImmediateBacklineThreat(BaseAdventurer member, Mobile mobile, double distance)
        {
            if (member == null || mobile == null || mobile.Deleted || !mobile.Alive)
            {
                return false;
            }

            if (mobile.Combatant == member)
            {
                return distance <= AdventurePartySettings.BacklineThreatRange;
            }

            return distance <= AdventurePartySettings.BacklineMeleeThreatRange;
        }

        private bool ShouldHoldPositionForMastery(BaseAdventurer member)
        {
            return m_State == AdventurePartyState.Engaging &&
                m_CurrentTactic != AdventurePartyTactic.AvoidAOE &&
                m_CurrentTactic != AdventurePartyTactic.PullToChokePoint &&
                m_CurrentTactic != AdventurePartyTactic.Recovering &&
                member != null &&
                member.Role == AdventurePartyRole.Mage &&
                FindNearbyBacklineThreat(member, member.Combatant as Mobile) == null &&
                AdvancedCombatBrain.IsChannelingDeathRay(member);
        }

        private static bool IsChokeTactic(AdventurePartyTactic tactic)
        {
            return tactic == AdventurePartyTactic.HoldChokePoint || tactic == AdventurePartyTactic.PullToChokePoint;
        }

        private void WatchForStuckLeader(BaseAdventurer leader)
        {
            if (leader == null)
            {
                return;
            }

            if (leader.Location == m_LastLeaderLocation)
            {
                m_StuckTicks++;
            }
            else
            {
                m_StuckTicks = 0;
                m_LastLeaderLocation = leader.Location;
            }

            if (m_StuckTicks >= 6 && m_Waypoints != null && m_Waypoints.Length > 0)
            {
                if (m_State == AdventurePartyState.Retreating && leader.GetDistanceToSqrt(m_Waypoints[0]) > 12.0)
                {
                    ClearTactic();

                    for (int i = 0; i < m_Members.Count; i++)
                    {
                        BaseAdventurer member = m_Members[i];

                        if (IsActive(member))
                        {
                            member.Combatant = null;
                        }
                    }

                    MoveMemberToward(leader, m_Waypoints[0]);
                }
                else
                {
                    m_WaypointIndex = (m_WaypointIndex + 1) % m_Waypoints.Length;
                }

                m_StuckTicks = 0;
            }
        }

        private void PartyMessage(string text)
        {
            BaseAdventurer speaker = GetLeader();

            if (speaker != null)
            {
                speaker.PublicOverheadMessage(MessageType.Regular, 0x3B2, false, text);
            }
        }

        private class AdventurePartyTimer : Timer
        {
            private readonly AdventurePartyController m_Controller;

            public AdventurePartyTimer(AdventurePartyController controller)
                : base(AdventurePartySettings.ControllerInitialDelay, AdventurePartySettings.ControllerThinkInterval)
            {
                m_Controller = controller;
                Priority = TimerPriority.OneSecond;
            }

            protected override void OnTick()
            {
                if (m_Controller == null || m_Controller.Deleted)
                {
                    Stop();
                    return;
                }

                m_Controller.OnTick();
            }
        }
    }
}

namespace Server.Mobiles
{
    using Server.Engines.AdvancedCombatAI;
    using Server.Engines.AdventureParty;
    using Server.Items;

    public abstract class BaseAdventurer : BaseCreature
    {
        private const int DeathShroudItemID = 0x204E;
        private AdventurePartyController m_Controller;
        private AdventurePartyRole m_Role;
        private Item m_GhostOuterTorso;

        [CommandProperty(AccessLevel.GameMaster)]
        public AdventurePartyController Controller
        {
            get { return m_Controller; }
            set { m_Controller = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public AdventurePartyRole Role
        {
            get { return m_Role; }
            set { m_Role = value; }
        }

        public DateTime NextPartyAction { get; set; }

        public override bool ClickTitle { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool AlwaysMurderer { get { return false; } }
        public override bool CanRummageCorpses { get { return false; } }
        public override bool KeepsItemsOnDeath { get { return true; } }
        public virtual AdvancedCombatProfile CombatProfile { get { return AdvancedCombatProfile.None; } }

        public BaseAdventurer(AdventurePartyRole role, AIType ai, int rangeFight)
            : base(ai, FightMode.Closest, AdventurePartySettings.PerceptionRange, rangeFight, 0.2, 0.4)
        {
            m_Role = role;
            Team = AdventurePartySettings.Team;
            SpeechHue = Utility.RandomDyedHue();
            Hue = Utility.RandomSkinHue();
            Karma = 1000;
            Fame = 500;
            SeeksHome = false;
            IsBonded = true;

            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }

            Utility.AssignRandomHair(this);
        }

        public BaseAdventurer(Serial serial)
            : base(serial)
        {
        }

        public override bool IsEnemy(Mobile mobile)
        {
            if (mobile == null || mobile == this)
            {
                return false;
            }

            BaseAdventurer adventurer = mobile as BaseAdventurer;

            if (adventurer != null && adventurer.Controller == Controller)
            {
                return false;
            }

            if (mobile.Player || mobile is PlayerMobile)
            {
                return false;
            }

            BaseCreature creature = mobile as BaseCreature;

            if (creature != null && (creature.Controlled || creature.Summoned))
            {
                return false;
            }

            return base.IsEnemy(mobile);
        }

        public override void OnThink()
        {
            base.OnThink();

            if (IsDeadPet)
            {
                return;
            }

            AdvancedCombatContext context = m_Controller == null ? null : m_Controller.GetAdvancedCombatContext(this);
            AdvancedCombatBrain.Think(this, CombatProfile, context);
        }

        public override void OnDeath(Container c)
        {
            base.OnDeath(c);

            if (IsDeadPet)
            {
                ApplyGhostAppearance();
            }
        }

        public override void OnAfterResurrect()
        {
            base.OnAfterResurrect();

            RestoreLivingAppearance();
        }

        public bool ResurrectAdventurer(BaseAdventurer healer)
        {
            if (!IsDeadPet || Deleted || Map == null || Map == Map.Internal)
            {
                return false;
            }

            if (healer != null && (healer.Deleted || healer.Map != Map || healer.IsDeadPet))
            {
                return false;
            }

            FixedEffect(0x376A, 10, 16);
            PlaySound(0x214);

            ResurrectPet();

            if (IsDeadPet)
            {
                return false;
            }

            Hits = Math.Max(Hits, Math.Max(10, HitsMax / 3));
            Stam = StamMax;
            Mana = Math.Min(ManaMax, Math.Max(Mana, ManaMax / 3));
            Combatant = null;
            Warmode = false;
            NextPartyAction = DateTime.UtcNow + TimeSpan.FromSeconds(3.0);

            if (healer != null)
            {
                healer.DoBeneficial(this);
            }

            if (m_Controller != null)
            {
                m_Controller.DeleteAfterWipe = DateTime.MinValue;
            }

            return true;
        }

        private void ApplyGhostAppearance()
        {
            BodyMod = 0;
            Body = Race.GhostBody(this);
            MoveOuterTorsoForGhost();
            SetEquipmentVisibility(false);
            EnsureDeathShroud();
            SendIncomingPacket();
        }

        private void RestoreLivingAppearance()
        {
            BodyMod = 0;
            Body = Race.AliveBody(this);
            RemoveDeathShrouds();
            RestoreOuterTorsoAfterGhost();
            SetEquipmentVisibility(true);
            SendIncomingPacket();
        }

        private void EnsureDeathShroud()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (IsAdventureDeathShroud(Items[i]))
                {
                    Items[i].Visible = true;
                    MoveEquippedItemToFront(Items[i]);
                    return;
                }
            }

            Item deathShroud = new Item(DeathShroudItemID);
            deathShroud.Movable = false;
            deathShroud.Layer = Layer.OuterTorso;
            deathShroud.LootType = LootType.Newbied;
            AddItem(deathShroud);
            MoveEquippedItemToFront(deathShroud);
        }

        private void MoveOuterTorsoForGhost()
        {
            if (m_GhostOuterTorso != null && !m_GhostOuterTorso.Deleted && m_GhostOuterTorso.Parent != this)
            {
                return;
            }

            Item outerTorso = FindItemOnLayer(Layer.OuterTorso);

            if (outerTorso == null || outerTorso.Deleted || IsAdventureDeathShroud(outerTorso))
            {
                return;
            }

            Container pack = EnsureAdventureBackpack();

            if (pack == null)
            {
                return;
            }

            m_GhostOuterTorso = outerTorso;
            pack.DropItem(outerTorso);
        }

        private void RestoreOuterTorsoAfterGhost()
        {
            Item outerTorso = m_GhostOuterTorso;
            m_GhostOuterTorso = null;

            if (outerTorso == null || outerTorso.Deleted)
            {
                return;
            }

            outerTorso.Visible = true;

            if (outerTorso.Parent == this)
            {
                return;
            }

            if (FindItemOnLayer(Layer.OuterTorso) == null)
            {
                AddItem(outerTorso);
            }
            else
            {
                PackItem(outerTorso);
            }
        }

        private void RemoveDeathShrouds()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                Item item = Items[i];

                if (IsAdventureDeathShroud(item))
                {
                    item.Delete();
                }
            }
        }

        private void SetEquipmentVisibility(bool visible)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                if (item == null || item.Deleted || item.Layer == Layer.Backpack || IsAdventureDeathShroud(item))
                {
                    continue;
                }

                item.Visible = visible;
            }
        }

        private static bool IsAdventureDeathShroud(Item item)
        {
            return item != null && !item.Deleted && item.ItemID == DeathShroudItemID && item.Layer == Layer.OuterTorso && !item.Movable;
        }

        private Container EnsureAdventureBackpack()
        {
            Container pack = Backpack;

            if (pack != null)
            {
                return pack;
            }

            pack = new Backpack();
            pack.Movable = false;
            AddItem(pack);

            return pack;
        }

        private void MoveEquippedItemToFront(Item item)
        {
            if (item == null || item.Deleted || item.Parent != this)
            {
                return;
            }

            Items.Remove(item);
            Items.Insert(0, item);
        }

        public override void OnAfterDelete()
        {
            if (m_Controller != null)
            {
                m_Controller.RemoveMember(this);
            }

            base.OnAfterDelete();
        }

        public override void GenerateLoot()
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version
            writer.Write((int)m_Role);
            writer.Write((Item)m_Controller);
            writer.Write((DateTime)NextPartyAction);
            writer.Write((Item)m_GhostOuterTorso);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Role = (AdventurePartyRole)reader.ReadInt();
            m_Controller = reader.ReadItem() as AdventurePartyController;
            NextPartyAction = reader.ReadDateTime();

            if (version >= 1)
            {
                m_GhostOuterTorso = reader.ReadItem();
            }

            IsBonded = true;
            ValidateAdventureEquipment();

            if (IsDeadPet)
            {
                ApplyGhostAppearance();
            }
        }

        public void ValidateAdventureEquipment()
        {
            Item oneHanded = FindItemOnLayer(Layer.OneHanded);
            Item twoHanded = FindItemOnLayer(Layer.TwoHanded);

            if (oneHanded is Spellbook && twoHanded is BaseWeapon && (m_Role == AdventurePartyRole.Healer || m_Role == AdventurePartyRole.Mage))
            {
                twoHanded.Delete();
            }
        }

        protected void Wear(Item item)
        {
            if (item == null)
            {
                return;
            }

            item.Movable = false;
            item.LootType = LootType.Newbied;
            AddItem(item);
        }

        protected void PackAdventureItem(Item item)
        {
            if (item == null)
            {
                return;
            }

            item.Movable = false;
            item.LootType = LootType.Newbied;

            Container pack = EnsureAdventureBackpack();

            if (pack != null)
            {
                pack.DropItem(item);
            }
        }

        protected void Wear(Item item, int hue)
        {
            if (item != null)
            {
                item.Hue = hue;
            }

            Wear(item);
        }

        // Keep generated gear inside ordinary item-family expectations: armor carries resists and stat-style bonuses,
        // weapons carry combat bonuses, jewelry carries skill/stat bonuses, and spellbooks carry caster bonuses.
        protected T TuneArmor<T>(T armor, int physical, int fire, int cold, int poison, int energy, int str, int dex, int intel, int hits, int stam, int mana) where T : BaseArmor
        {
            if (armor == null)
            {
                return null;
            }

            armor.PhysicalBonus += physical;
            armor.FireBonus += fire;
            armor.ColdBonus += cold;
            armor.PoisonBonus += poison;
            armor.EnergyBonus += energy;
            TuneAttributes(armor.Attributes, str, dex, intel, hits, stam, mana, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            return armor;
        }

        protected T TuneClothing<T>(T clothing, int str, int dex, int intel, int hits, int stam, int mana, int lmc, int lrc) where T : BaseClothing
        {
            if (clothing == null)
            {
                return null;
            }

            TuneAttributes(clothing.Attributes, str, dex, intel, hits, stam, mana, 0, 0, 0, 0, 0, lmc, lrc, 0, 0);

            return clothing;
        }

        protected T TuneJewel<T>(T jewel, int str, int dex, int intel, int hits, int stam, int mana, int attack, int defend, int spellDamage, int lmc) where T : BaseJewel
        {
            if (jewel == null)
            {
                return null;
            }

            TuneAttributes(jewel.Attributes, str, dex, intel, hits, stam, mana, 0, 0, attack, defend, spellDamage, lmc, 0, 0, 0);

            return jewel;
        }

        protected T TuneWeapon<T>(T weapon, int str, int dex, int intel, int weaponDamage, int weaponSpeed, int attack, int defend, int spellDamage, int lmc) where T : BaseWeapon
        {
            if (weapon == null)
            {
                return null;
            }

            TuneAttributes(weapon.Attributes, str, dex, intel, 0, 0, 0, weaponDamage, weaponSpeed, attack, defend, spellDamage, lmc, 0, 0, 0);

            return weapon;
        }

        protected T TuneWeaponEffects<T>(T weapon, int hitLeechHits, int hitLeechMana, int hitLowerAttack, int hitLowerDefend) where T : BaseWeapon
        {
            if (weapon == null)
            {
                return null;
            }

            weapon.WeaponAttributes.HitLeechHits += hitLeechHits;
            weapon.WeaponAttributes.HitLeechMana += hitLeechMana;
            weapon.WeaponAttributes.HitLowerAttack += hitLowerAttack;
            weapon.WeaponAttributes.HitLowerDefend += hitLowerDefend;

            return weapon;
        }

        protected Spellbook TuneSpellbook(Spellbook book, int intel, int mana, int spellDamage, int lmc, int lrc, int castSpeed, int castRecovery)
        {
            if (book == null)
            {
                return null;
            }

            TuneAttributes(book.Attributes, 0, 0, intel, 0, 0, mana, 0, 0, 0, 0, spellDamage, lmc, lrc, castSpeed, castRecovery);

            return book;
        }

        protected void FinishEquipmentProfile()
        {
            ApplyRandomAppearance();
            ValidateAdventureEquipment();
            Hits = HitsMax;
            Stam = StamMax;
            Mana = ManaMax;
        }

        private void ApplyRandomAppearance()
        {
            int primary;
            int secondary;
            int accent;

            ChooseAppearanceHues(out primary, out secondary, out accent);
            ApplyRandomAppearance(primary, secondary, accent);
        }

        public void ApplyPartyAppearance(int primary, int secondary, int accent)
        {
            ApplyRandomAppearance(primary, secondary, accent);
            ValidateAdventureEquipment();
        }

        private void ApplyRandomAppearance(int primary, int secondary, int accent)
        {
            RehueExistingVisibleGear(primary, secondary, accent);

            switch (m_Role)
            {
                case AdventurePartyRole.Fighter:
                    ApplyFighterAppearance(primary, secondary, accent);
                    break;
                case AdventurePartyRole.Archer:
                    ApplyArcherAppearance(primary, secondary, accent);
                    break;
                case AdventurePartyRole.Mage:
                    ApplyMageAppearance(primary, secondary, accent);
                    break;
                case AdventurePartyRole.Healer:
                    ApplyHealerAppearance(primary, secondary, accent);
                    break;
            }
        }

        private void ApplyFighterAppearance(int primary, int secondary, int accent)
        {
            ClearAppearanceLayer(Layer.Shoes);
            ClearAppearanceLayer(Layer.Shirt);
            ClearAppearanceLayer(Layer.MiddleTorso);
            ClearAppearanceLayer(Layer.Waist);
            ClearAppearanceLayer(Layer.Cloak);
            ClearAppearanceLayer(Layer.OuterTorso);

            Wear(CreateRandomBoots(secondary));

            if (Utility.Random(100) < 75)
            {
                Wear(CreateRandomWarriorMiddleTorso(accent));
            }

            if (Utility.Random(100) < 60)
            {
                Wear(CreateRandomWaist(accent));
            }

            if (Utility.Random(100) < 70)
            {
                Wear(CreateRandomCloak(primary));
            }

            if (Utility.Random(100) < 25)
            {
                Wear(CreateRandomSimpleOuterTorso(primary));
            }
        }

        private void ApplyArcherAppearance(int primary, int secondary, int accent)
        {
            ClearAppearanceLayer(Layer.Shoes);
            ClearAppearanceLayer(Layer.Helm);
            ClearAppearanceLayer(Layer.Shirt);
            ClearAppearanceLayer(Layer.MiddleTorso);
            ClearAppearanceLayer(Layer.Waist);
            ClearAppearanceLayer(Layer.Cloak);
            ClearAppearanceLayer(Layer.OuterTorso);

            Wear(CreateRandomBoots(secondary));
            Wear(CreateRandomRangerHat(accent));

            if (Utility.Random(100) < 75)
            {
                Wear(CreateRandomRangerMiddleTorso(primary));
            }

            if (Utility.Random(100) < 45)
            {
                Wear(CreateRandomWaist(accent));
            }

            if (Utility.Random(100) < 65)
            {
                Wear(CreateRandomCloak(primary));
            }

            if (Utility.Random(100) < 20)
            {
                Wear(CreateRandomSimpleOuterTorso(primary));
            }
        }

        private void ApplyMageAppearance(int primary, int secondary, int accent)
        {
            ClearAppearanceLayer(Layer.Shoes);
            ClearAppearanceLayer(Layer.Helm);
            ClearAppearanceLayer(Layer.Shirt);
            ClearAppearanceLayer(Layer.MiddleTorso);
            ClearAppearanceLayer(Layer.Waist);
            ClearAppearanceLayer(Layer.Cloak);
            ClearAppearanceLayer(Layer.OuterTorso);

            Wear(TuneClothing(CreateRandomCasterOuterTorso(primary), 0, 0, 3, 3, 0, 5, 5, 10));
            Wear(TuneClothing(CreateRandomMageHat(secondary), 0, 0, 3, 2, 0, 5, 3, 5));
            Wear(CreateRandomCasterShoes(accent));

            if (Utility.Random(100) < 65)
            {
                Wear(CreateRandomCasterMiddleTorso(secondary));
            }

            if (Utility.Random(100) < 45)
            {
                Wear(CreateRandomWaist(accent));
            }

            if (Utility.Random(100) < 55)
            {
                Wear(CreateRandomCloak(primary));
            }
        }

        private void ApplyHealerAppearance(int primary, int secondary, int accent)
        {
            ClearAppearanceLayer(Layer.Shoes);
            ClearAppearanceLayer(Layer.Helm);
            ClearAppearanceLayer(Layer.Shirt);
            ClearAppearanceLayer(Layer.MiddleTorso);
            ClearAppearanceLayer(Layer.Waist);
            ClearAppearanceLayer(Layer.Cloak);
            ClearAppearanceLayer(Layer.OuterTorso);

            Wear(TuneClothing(CreateRandomCasterOuterTorso(primary), 0, 0, 2, 4, 0, 4, 4, 10));
            Wear(CreateRandomCasterShoes(secondary));

            if (Utility.Random(100) < 55)
            {
                Wear(CreateRandomHealerHat(accent));
            }

            if (Utility.Random(100) < 75)
            {
                Wear(CreateRandomHealerMiddleTorso(accent));
            }

            if (Utility.Random(100) < 65)
            {
                Wear(CreateRandomWaist(accent));
            }

            if (Utility.Random(100) < 55)
            {
                Wear(CreateRandomCloak(primary));
            }
        }

        private void ChooseAppearanceHues(out int primary, out int secondary, out int accent)
        {
            int[] palette;

            switch (m_Role)
            {
                case AdventurePartyRole.Fighter:
                    palette = new int[] { 0x455, 0x497, 0x835, 0x845, 0x966, 0x973 };
                    break;
                case AdventurePartyRole.Archer:
                    palette = new int[] { 0x59C, 0x59B, 0x5A7, 0x6D3, 0x835, 0x89F };
                    break;
                case AdventurePartyRole.Mage:
                    palette = new int[] { 0x482, 0x497, 0x4F2, 0x546, 0x66D, 0x89F };
                    break;
                default:
                    palette = new int[] { 0x59B, 0x4F2, 0x6D3, 0x835, 0x8A5, 0x973 };
                    break;
            }

            primary = PickHue(palette);
            secondary = PickDifferentHue(palette, primary);
            accent = PickHue(new int[] { 0x47E, 0x482, 0x4F2, 0x59B, 0x66D, 0x6D3, 0x835, 0x89F, 0x973 });
        }

        public static void ChoosePartyAppearanceHues(out int primary, out int secondary, out int accent)
        {
            int[] palette = new int[] { 0x455, 0x497, 0x59B, 0x66D, 0x6D3, 0x835, 0x89F, 0x966, 0x973 };

            primary = PickHue(palette);
            secondary = PickDifferentHue(palette, primary);
            accent = PickHue(new int[] { 0x47E, 0x482, 0x4F2, 0x5A7, 0x6D3, 0x835, 0x89F, 0x973 });
        }

        private void RehueExistingVisibleGear(int primary, int secondary, int accent)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                Item item = Items[i];

                if (item == null || item.Deleted || item.Layer == Layer.Backpack ||
                    item.Layer == Layer.Hair || item.Layer == Layer.FacialHair ||
                    item.Layer == Layer.OneHanded || item.Layer == Layer.TwoHanded ||
                    item.Layer == Layer.Ring || item.Layer == Layer.Bracelet ||
                    item.Layer == Layer.Neck || item.Layer == Layer.Earrings ||
                    item.Layer == Layer.Talisman || IsAdventureDeathShroud(item))
                {
                    continue;
                }

                if (item is BaseArmor)
                {
                    item.Hue = primary;
                }
                else if (item is BaseClothing)
                {
                    item.Hue = Utility.RandomBool() ? secondary : accent;
                }
            }
        }

        private void ClearAppearanceLayer(Layer layer)
        {
            Item item = FindItemOnLayer(layer);

            if (item != null && !item.Deleted)
            {
                item.Delete();
            }
        }

        private static int PickHue(int[] hues)
        {
            if (hues == null || hues.Length == 0)
            {
                return 0;
            }

            return hues[Utility.Random(hues.Length)];
        }

        private static int PickDifferentHue(int[] hues, int avoid)
        {
            if (hues == null || hues.Length == 0)
            {
                return 0;
            }

            for (int i = 0; i < 6; i++)
            {
                int hue = PickHue(hues);

                if (hue != avoid)
                {
                    return hue;
                }
            }

            return PickHue(hues);
        }

        private BaseClothing CreateRandomSimpleOuterTorso(int hue)
        {
            switch (Utility.Random(Female ? 6 : 4))
            {
                case 0:
                    return new Robe(hue);
                case 1:
                    return new HakamaShita(hue);
                case 2:
                    return new Kamishimo(hue);
                case 3:
                    return Female ? (BaseClothing)new FemaleKimono(hue) : new MaleKimono(hue);
                case 4:
                    return new PlainDress(hue);
                default:
                    return new FancyDress(hue);
            }
        }

        private BaseClothing CreateRandomCasterOuterTorso(int hue)
        {
            switch (Utility.Random(Female ? 7 : 5))
            {
                case 0:
                    return new Robe(hue);
                case 1:
                    return new MonkRobe(hue);
                case 2:
                    return new HakamaShita(hue);
                case 3:
                    return new Kamishimo(hue);
                case 4:
                    return Female ? (BaseClothing)new FemaleKimono(hue) : new MaleKimono(hue);
                case 5:
                    return new PlainDress(hue);
                default:
                    return new FancyDress(hue);
            }
        }

        private BaseClothing CreateRandomWarriorMiddleTorso(int hue)
        {
            switch (Utility.Random(4))
            {
                case 0:
                    return new BodySash(hue);
                case 1:
                    return new Surcoat(hue);
                case 2:
                    return new JinBaori(hue);
                default:
                    return new Tunic(hue);
            }
        }

        private BaseClothing CreateRandomRangerMiddleTorso(int hue)
        {
            switch (Utility.Random(5))
            {
                case 0:
                    return new BodySash(hue);
                case 1:
                    return new JinBaori(hue);
                case 2:
                    return new Tunic(hue);
                case 3:
                    return new Doublet(hue);
                default:
                    return new Surcoat(hue);
            }
        }

        private BaseClothing CreateRandomCasterMiddleTorso(int hue)
        {
            switch (Utility.Random(4))
            {
                case 0:
                    return new BodySash(hue);
                case 1:
                    return new FullApron(hue);
                case 2:
                    return new JinBaori(hue);
                default:
                    return new FormalShirt(hue);
            }
        }

        private BaseClothing CreateRandomHealerMiddleTorso(int hue)
        {
            switch (Utility.Random(4))
            {
                case 0:
                    return new BodySash(hue);
                case 1:
                    return new FullApron(hue);
                case 2:
                    return new FormalShirt(hue);
                default:
                    return new JinBaori(hue);
            }
        }

        private BaseClothing CreateRandomWaist(int hue)
        {
            return Utility.RandomBool() ? (BaseClothing)new HalfApron(hue) : new Obi(hue);
        }

        private BaseClothing CreateRandomCloak(int hue)
        {
            return Utility.Random(100) < 75 ? (BaseClothing)new Cloak(hue) : new FurCape(hue);
        }

        private BaseClothing CreateRandomBoots(int hue)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    return new Boots(hue);
                case 1:
                    return new ThighBoots(hue);
                default:
                    return new Shoes(hue);
            }
        }

        private BaseClothing CreateRandomCasterShoes(int hue)
        {
            switch (Utility.Random(3))
            {
                case 0:
                    return new Sandals(hue);
                case 1:
                    return new Shoes(hue);
                default:
                    return new Boots(hue);
            }
        }

        private BaseClothing CreateRandomRangerHat(int hue)
        {
            switch (Utility.Random(6))
            {
                case 0:
                    return new Bandana(hue);
                case 1:
                    return new FeatheredHat(hue);
                case 2:
                    return new Kasa(hue);
                case 3:
                    return new WideBrimHat(hue);
                case 4:
                    return new Cap(hue);
                default:
                    return new SkullCap(hue);
            }
        }

        private BaseClothing CreateRandomMageHat(int hue)
        {
            switch (Utility.Random(5))
            {
                case 0:
                    return new WizardsHat(hue);
                case 1:
                    return new ClothNinjaHood(hue);
                case 2:
                    return new Kasa(hue);
                case 3:
                    return new WideBrimHat(hue);
                default:
                    return new SkullCap(hue);
            }
        }

        private BaseClothing CreateRandomHealerHat(int hue)
        {
            switch (Utility.Random(6))
            {
                case 0:
                    return new FlowerGarland(hue);
                case 1:
                    return new Bonnet(hue);
                case 2:
                    return new FeatheredHat(hue);
                case 3:
                    return new Bandana(hue);
                case 4:
                    return new WideBrimHat(hue);
                default:
                    return new Kasa(hue);
            }
        }

        public override int HitsMax
        {
            get { return Math.Max(1, base.HitsMax + AosAttributes.GetValue(this, AosAttribute.BonusHits)); }
        }

        public override int StamMax
        {
            get { return Math.Max(1, base.StamMax + AosAttributes.GetValue(this, AosAttribute.BonusStam)); }
        }

        public override int ManaMax
        {
            get { return Math.Max(1, base.ManaMax + AosAttributes.GetValue(this, AosAttribute.BonusMana)); }
        }

        private static void TuneAttributes(AosAttributes attributes, int str, int dex, int intel, int hits, int stam, int mana, int weaponDamage, int weaponSpeed, int attack, int defend, int spellDamage, int lmc, int lrc, int castSpeed, int castRecovery)
        {
            if (attributes == null)
            {
                return;
            }

            attributes.BonusStr += str;
            attributes.BonusDex += dex;
            attributes.BonusInt += intel;
            attributes.BonusHits += hits;
            attributes.BonusStam += stam;
            attributes.BonusMana += mana;
            attributes.WeaponDamage += weaponDamage;
            attributes.WeaponSpeed += weaponSpeed;
            attributes.AttackChance += attack;
            attributes.DefendChance += defend;
            attributes.SpellDamage += spellDamage;
            attributes.LowerManaCost += lmc;
            attributes.LowerRegCost += lrc;
            attributes.CastSpeed += castSpeed;
            attributes.CastRecovery += castRecovery;
        }
    }

    public class AdventurerFighter : BaseAdventurer
    {
        public override AdvancedCombatProfile CombatProfile { get { return AdvancedCombatProfile.SampireFighter; } }

        [Constructable]
        public AdventurerFighter()
            : base(AdventurePartyRole.Fighter, AIType.AI_Melee, 1)
        {
            Title = "the sampire fighter";

            SetStr(105, 120);
            SetDex(70, 85);
            SetInt(80, 95);
            SetHits(130, 150);
            SetDamage(8, 12);

            SetResistance(ResistanceType.Physical, 8, 12);
            SetResistance(ResistanceType.Fire, 6, 10);
            SetResistance(ResistanceType.Cold, 6, 10);
            SetResistance(ResistanceType.Poison, 5, 9);
            SetResistance(ResistanceType.Energy, 5, 9);

            SetSkill(SkillName.Swords, 92.0, 102.0);
            SetSkill(SkillName.Tactics, 90.0, 100.0);
            SetSkill(SkillName.Parry, 80.0, 92.0);
            SetSkill(SkillName.MagicResist, 70.0, 82.0);
            SetSkill(SkillName.Anatomy, 65.0, 78.0);
            SetSkill(SkillName.Bushido, 72.0, 84.0);
            SetSkill(SkillName.Chivalry, 60.0, 72.0);
            SetSkill(SkillName.Necromancy, 94.0, 102.0);
            SetSkill(SkillName.SpiritSpeak, 45.0, 58.0);

            BaseArmor chest = TuneArmor(new PlateChest(), 3, 2, 2, 1, 1, 3, 0, 0, 6, 0, 0);
            Wear(chest, 0x966);

            Wear(TuneArmor(new PlateArms(), 2, 1, 1, 1, 1, 2, 0, 0, 4, 0, 0), 0x966);
            Wear(TuneArmor(new PlateGloves(), 2, 1, 1, 1, 1, 2, 0, 0, 3, 0, 0), 0x966);
            Wear(TuneArmor(new PlateGorget(), 1, 1, 1, 1, 1, 0, 0, 0, 3, 0, 0), 0x966);
            Wear(TuneArmor(new PlateLegs(), 3, 1, 1, 1, 1, 2, 0, 0, 4, 0, 0), 0x966);
            Wear(TuneArmor(new CloseHelm(), 2, 1, 1, 1, 1, 1, 0, 0, 3, 0, 0), 0x966);
            Wear(new Boots());

            BaseWeapon doubleAxe = TuneWeaponEffects(TuneWeapon(new DoubleAxe(), 3, 0, 0, 35, 10, 5, 0, 0, 0), 30, 25, 0, 0);
            Wear(doubleAxe);

            PackAdventureItem(TuneWeaponEffects(TuneWeapon(new RadiantScimitar(), 4, 0, 0, 35, 15, 8, 0, 0, 0), 35, 30, 0, 0));
            PackAdventureItem(TuneWeaponEffects(TuneWeapon(new Broadsword(), 2, 0, 0, 35, 10, 6, 0, 0, 0), 0, 30, 0, 25));
            PackAdventureItem(TuneWeaponEffects(TuneWeapon(new Daisho(), 3, 0, 0, 30, 10, 5, 0, 0, 0), 30, 20, 25, 0));

            BaseJewel ring = TuneJewel(new GoldRing(), 2, 0, 0, 3, 0, 0, 5, 0, 0, 0);
            ring.SkillBonuses.SetValues(0, SkillName.Chivalry, 10.0);
            Wear(ring);

            BaseJewel bracelet = TuneJewel(new GoldBracelet(), 0, 3, 0, 2, 3, 0, 0, 5, 0, 0);
            bracelet.SkillBonuses.SetValues(0, SkillName.Bushido, 8.0);
            Wear(bracelet);

            FinishEquipmentProfile();
        }

        public AdventurerFighter(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class AdventurerArcher : BaseAdventurer
    {
        public override AdvancedCombatProfile CombatProfile { get { return AdvancedCombatProfile.DiscoProvoArcher; } }

        [Constructable]
        public AdventurerArcher()
            : base(AdventurePartyRole.Archer, AIType.AI_Archer, 10)
        {
            Title = "the chivalry bard archer";

            SetStr(80, 95);
            SetDex(105, 120);
            SetInt(75, 90);
            SetHits(95, 115);
            SetDamage(8, 11);

            SetResistance(ResistanceType.Physical, 6, 10);
            SetResistance(ResistanceType.Fire, 5, 9);
            SetResistance(ResistanceType.Cold, 5, 9);
            SetResistance(ResistanceType.Poison, 5, 9);
            SetResistance(ResistanceType.Energy, 5, 9);

            SetSkill(SkillName.Archery, 92.0, 102.0);
            SetSkill(SkillName.Tactics, 84.0, 96.0);
            SetSkill(SkillName.Anatomy, 73.0, 87.0);
            SetSkill(SkillName.MagicResist, 55.0, 70.0);
            SetSkill(SkillName.Chivalry, 55.0, 70.0);
            SetSkill(SkillName.Musicianship, 90.0, 100.0);
            SetSkill(SkillName.Discordance, 85.0, 95.0);
            SetSkill(SkillName.Provocation, 78.0, 90.0);

            BaseArmor leatherChest = TuneArmor(new LeatherChest(), 2, 2, 2, 2, 2, 0, 3, 0, 4, 3, 0);
            Wear(leatherChest, 0x59C);

            Wear(TuneArmor(new LeatherArms(), 1, 2, 1, 1, 1, 0, 2, 0, 2, 3, 0), 0x59C);
            Wear(TuneArmor(new LeatherGloves(), 1, 1, 1, 1, 1, 0, 2, 0, 2, 2, 0), 0x59C);
            Wear(TuneArmor(new LeatherLegs(), 2, 1, 1, 1, 1, 0, 3, 0, 3, 4, 0), 0x59C);
            Wear(new Bandana(), Utility.RandomNeutralHue());
            Wear(new Boots());

            BaseWeapon bow = TuneWeapon(new CompositeBow(), 0, 5, 0, 35, 20, 10, 0, 0, 0);
            Wear(bow);

            BaseJewel ring = TuneJewel(new SilverRing(), 0, 3, 0, 2, 3, 0, 5, 0, 0, 0);
            ring.SkillBonuses.SetValues(0, SkillName.Discordance, 10.0);
            Wear(ring);

            BaseJewel bracelet = TuneJewel(new SilverBracelet(), 0, 2, 0, 2, 2, 0, 0, 5, 0, 0);
            bracelet.SkillBonuses.SetValues(0, SkillName.Musicianship, 10.0);
            Wear(bracelet);

            PackItem(new Arrow(80));
            PackItem(new Lute());
            FinishEquipmentProfile();
        }

        public AdventurerArcher(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class AdventurerMage : BaseAdventurer
    {
        public override AdvancedCombatProfile CombatProfile { get { return AdvancedCombatProfile.NecroMageWeaver; } }

        [Constructable]
        public AdventurerMage()
            : base(AdventurePartyRole.Mage, AIType.AI_Mage, 10)
        {
            Title = "the necro weaver";

            SetStr(65, 80);
            SetDex(70, 85);
            SetInt(100, 115);
            SetHits(75, 95);
            SetDamage(3, 6);

            SetResistance(ResistanceType.Physical, 5, 9);
            SetResistance(ResistanceType.Fire, 8, 12);
            SetResistance(ResistanceType.Cold, 8, 12);
            SetResistance(ResistanceType.Poison, 5, 9);
            SetResistance(ResistanceType.Energy, 8, 12);

            SetSkill(SkillName.Magery, 82.0, 94.0);
            SetSkill(SkillName.EvalInt, 82.0, 96.0);
            SetSkill(SkillName.Meditation, 78.0, 92.0);
            SetSkill(SkillName.MagicResist, 68.0, 82.0);
            SetSkill(SkillName.Wrestling, 45.0, 60.0);
            SetSkill(SkillName.Necromancy, 80.0, 94.0);
            SetSkill(SkillName.SpiritSpeak, 80.0, 94.0);
            SetSkill(SkillName.Spellweaving, 85.0, 100.0);

            Spellbook book = TuneSpellbook(new Spellbook(), 5, 15, 18, 10, 15, 1, 2);
            book.Content = 0xFFFFFFFFFFFFFFFF;
            book.SkillBonuses.SetValues(0, SkillName.Magery, 10.0);
            Wear(book);

            BaseClothing robe = TuneClothing(new Robe(0x482), 0, 0, 3, 3, 0, 5, 5, 10);
            Wear(robe);

            Wear(new Sandals());
            Wear(TuneClothing(new WizardsHat(0x482), 0, 0, 3, 2, 0, 5, 3, 5));

            BaseJewel ring = TuneJewel(new GoldRing(), 0, 0, 3, 0, 0, 5, 0, 0, 5, 4);
            ring.SkillBonuses.SetValues(0, SkillName.Necromancy, 10.0);
            Wear(ring);

            BaseJewel bracelet = TuneJewel(new GoldBracelet(), 0, 0, 2, 0, 0, 5, 0, 3, 5, 4);
            bracelet.SkillBonuses.SetValues(0, SkillName.SpiritSpeak, 10.0);
            Wear(bracelet);

            PackReg(20);
            FinishEquipmentProfile();
        }

        public AdventurerMage(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class AdventurerHealer : BaseAdventurer
    {
        public override AdvancedCombatProfile CombatProfile { get { return AdvancedCombatProfile.BardHealer; } }

        [Constructable]
        public AdventurerHealer()
            : base(AdventurePartyRole.Healer, AIType.AI_Healer, 8)
        {
            Title = "the bard healer";

            SetStr(75, 90);
            SetDex(78, 95);
            SetInt(90, 105);
            SetHits(90, 110);
            SetDamage(4, 7);

            SetResistance(ResistanceType.Physical, 6, 10);
            SetResistance(ResistanceType.Fire, 6, 10);
            SetResistance(ResistanceType.Cold, 6, 10);
            SetResistance(ResistanceType.Poison, 6, 10);
            SetResistance(ResistanceType.Energy, 6, 10);

            SetSkill(SkillName.Healing, 82.0, 95.0);
            SetSkill(SkillName.Anatomy, 80.0, 92.0);
            SetSkill(SkillName.Magery, 70.0, 82.0);
            SetSkill(SkillName.Meditation, 65.0, 78.0);
            SetSkill(SkillName.MagicResist, 65.0, 78.0);
            SetSkill(SkillName.Musicianship, 98.0, 108.0);
            SetSkill(SkillName.Discordance, 92.0, 102.0);

            Spellbook book = TuneSpellbook(new Spellbook(), 3, 10, 8, 8, 15, 1, 1);
            book.Content = 0xFFFFFFFFFFFFFFFF;
            book.SkillBonuses.SetValues(0, SkillName.Magery, 8.0);
            Wear(book);

            BaseClothing robe = TuneClothing(new Robe(0x59B), 0, 0, 2, 4, 0, 4, 4, 10);
            Wear(robe);

            Wear(new Sandals());

            BaseJewel ring = TuneJewel(new SilverRing(), 0, 0, 2, 2, 0, 3, 0, 0, 0, 3);
            ring.SkillBonuses.SetValues(0, SkillName.Healing, 10.0);
            Wear(ring);

            BaseJewel bracelet = TuneJewel(new SilverBracelet(), 0, 2, 0, 2, 2, 0, 0, 4, 0, 0);
            bracelet.SkillBonuses.SetValues(0, SkillName.Anatomy, 10.0);
            Wear(bracelet);

            Wear(new SilverEarrings());
            Wear(new SilverNecklace());

            PackItem(new Bandage(150));
            PackItem(new Lute());
            PackReg(12);
            FinishEquipmentProfile();
        }

        public AdventurerHealer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}

namespace Server.Commands
{
    using Server.Engines.AdventureParty;
    using Server.Items;
    using Server.Mobiles;

    public static class AdventurePartyCommands
    {
        public static void Initialize()
        {
            CommandSystem.Register("AdventureParty", AccessLevel.GameMaster, new CommandEventHandler(AdventureParty_OnCommand));
            CommandSystem.Register("SpawnAdventureParty", AccessLevel.GameMaster, new CommandEventHandler(AdventureParty_OnCommand));
            CommandSystem.Register("SpawnAIAdventureParty", AccessLevel.GameMaster, new CommandEventHandler(SpawnAIAdventureParty_OnCommand));
            CommandSystem.Register("ClearAdventureParties", AccessLevel.GameMaster, new CommandEventHandler(ClearAdventureParties_OnCommand));
            CommandSystem.Register("AdventurePartyAI", AccessLevel.GameMaster, new CommandEventHandler(AdventurePartyAI_OnCommand));
        }

        private static void AdventureParty_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Map == null || from.Map == Map.Internal)
            {
                return;
            }

            AdventurePartyController controller = new AdventurePartyController(from.Location, from.Map);
            controller.SpawnParty();

            from.SendMessage("Spawned a four-member adventure party.");
        }

        private static void SpawnAIAdventureParty_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null || from.Map == null || from.Map == Map.Internal)
            {
                return;
            }

            AdventurePartyController controller = new AdventurePartyController(from.Location, from.Map);
            controller.SpawnParty();
            controller.AIEnabled = true;
            controller.LastAIStatus = "AI enabled by SpawnAIAdventureParty command.";

            from.SendMessage("Spawned a four-member adventure party with AI enabled. Provider={0}.", controller.AIProvider);
        }

        private static void ClearAdventureParties_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;
            List<AdventurePartyController> controllers = new List<AdventurePartyController>();
            List<BaseAdventurer> strays = new List<BaseAdventurer>();

            foreach (Item item in World.Items.Values)
            {
                AdventurePartyController controller = item as AdventurePartyController;

                if (controller != null)
                {
                    controllers.Add(controller);
                }
            }

            for (int i = 0; i < controllers.Count; i++)
            {
                if (!controllers[i].Deleted)
                {
                    controllers[i].Delete();
                }
            }

            foreach (Mobile mobile in World.Mobiles.Values)
            {
                BaseAdventurer adventurer = mobile as BaseAdventurer;

                if (adventurer != null && !adventurer.Deleted)
                {
                    strays.Add(adventurer);
                }
            }

            for (int i = 0; i < strays.Count; i++)
            {
                if (!strays[i].Deleted)
                {
                    strays[i].Delete();
                }
            }

            if (from != null)
            {
                from.SendMessage("Cleared {0} adventure party controller(s) and {1} stray adventurer(s).", controllers.Count, strays.Count);
            }
        }

        private static void AdventurePartyAI_OnCommand(CommandEventArgs e)
        {
            Mobile from = e.Mobile;

            if (from == null)
            {
                return;
            }

            bool all = e.Length > 0 && String.Equals(e.GetString(0), "all", StringComparison.OrdinalIgnoreCase);
            string action = all ? e.GetString(1) : e.GetString(0);

            if (String.IsNullOrEmpty(action))
            {
                action = "status";
            }

            if (String.Equals(action, "url", StringComparison.OrdinalIgnoreCase))
            {
                from.SendMessage("Use [AdventurePartyAI endpoint [url] instead.");
                return;
            }

            if (String.Equals(action, "endpoint", StringComparison.OrdinalIgnoreCase))
            {
                int valueIndex = all ? 2 : 1;
                string endpoint = e.GetString(valueIndex);

                if (!String.IsNullOrEmpty(endpoint))
                {
                    AdventurePartyAIConfig.HttpEndpointUrl = endpoint;
                    from.SendMessage("Adventure party AI endpoint set to: {0}", AdventurePartyAIConfig.HttpEndpointUrl);
                }
                else
                {
                    from.SendMessage("Adventure party AI endpoint: {0}", AdventurePartyAIConfig.HttpEndpointUrl);
                }

                return;
            }

            if (String.Equals(action, "config", StringComparison.OrdinalIgnoreCase))
            {
                from.SendMessage("Adventure party AI config: endpoint={0}, defaultProvider={1}, defaultEnabled={2}.",
                    AdventurePartyAIConfig.HttpEndpointUrl,
                    AdventurePartyAIConfig.DefaultProvider,
                    AdventurePartyAIConfig.EnabledByDefault);
                from.SendMessage("Cooldown={0}, failureCooldown={1}, timeout={2}, decisionMaxAge={3}.",
                    AdventurePartyAIConfig.RequestCooldown,
                    AdventurePartyAIConfig.FailureCooldown,
                    AdventurePartyAIConfig.RequestTimeout,
                    AdventurePartyAIConfig.DecisionMaxAge);
                return;
            }

            List<AdventurePartyController> targets = FindAdventurePartyAITargets(from, all);

            if (targets.Count == 0)
            {
                from.SendMessage(all ? "No adventure party controllers exist." : "No adventure party controller found nearby.");
                from.SendMessage("Usage: [AdventurePartyAI status|on|off|mock|http|config|endpoint [url]");
                from.SendMessage("Usage: [AdventurePartyAI all status|on|off|mock|http");
                return;
            }

            for (int i = 0; i < targets.Count; i++)
            {
                ApplyAdventurePartyAICommand(from, targets[i], action);
            }

            if (all)
            {
                from.SendMessage("AdventurePartyAI applied '{0}' to {1} controller(s).", action, targets.Count);
            }
        }

        private static List<AdventurePartyController> FindAdventurePartyAITargets(Mobile from, bool all)
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

                return list;
            }

            AdventurePartyController best = null;
            double bestDistance = 20.0;

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

        private static void ApplyAdventurePartyAICommand(Mobile from, AdventurePartyController controller, string action)
        {
            if (controller == null || controller.Deleted)
            {
                return;
            }

            if (String.Equals(action, "on", StringComparison.OrdinalIgnoreCase))
            {
                controller.AIEnabled = true;
                controller.LastAIStatus = "AI enabled by GM command.";
                from.SendMessage("Adventure party AI enabled. Provider={0}.", controller.AIProvider);
            }
            else if (String.Equals(action, "off", StringComparison.OrdinalIgnoreCase))
            {
                controller.AIEnabled = false;
                controller.LastAIStatus = "AI disabled by GM command.";
                from.SendMessage("Adventure party AI disabled.");
            }
            else if (String.Equals(action, "mock", StringComparison.OrdinalIgnoreCase))
            {
                controller.AIProvider = AdventurePartyAIProviderMode.Mock;
                controller.LastAIStatus = "AI provider set to Mock.";
                from.SendMessage("Adventure party AI provider set to Mock.");
            }
            else if (String.Equals(action, "http", StringComparison.OrdinalIgnoreCase))
            {
                controller.AIProvider = AdventurePartyAIProviderMode.HttpEndpoint;
                controller.LastAIStatus = "AI provider set to HttpEndpoint.";
                from.SendMessage("Adventure party AI provider set to HttpEndpoint. Endpoint={0}", AdventurePartyAIConfig.HttpEndpointUrl);
            }
            else
            {
                from.SendMessage(
                    "Adventure party AI: enabled={0}, provider={1}, pending={2}, requests={3}, decisions={4}, failures={5}, status={6}",
                    controller.AIEnabled,
                    controller.AIProvider,
                    controller.AIRequestPending,
                    controller.AIRequests,
                    controller.AIDecisions,
                    controller.AIFailures,
                    controller.LastAIStatus);
            }
        }
    }
}
