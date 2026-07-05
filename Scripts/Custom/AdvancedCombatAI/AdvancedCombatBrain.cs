using System;
using System.Collections.Generic;
using Server.Engines.AdventureParty;
using Server.Items;
using Server.Mobiles;
using Server.SkillHandlers;
using Server.Spells;
using Server.Spells.Bushido;
using Server.Spells.Chivalry;
using Server.Spells.First;
using Server.Spells.Fourth;
using Server.Spells.Necromancy;
using Server.Spells.Second;
using Server.Spells.Seventh;
using Server.Spells.Sixth;
using Server.Spells.SkillMasteries;
using Server.Spells.Spellweaving;

namespace Server.Engines.AdvancedCombatAI
{
    public enum AdvancedCombatProfile
    {
        None,
        SampireFighter,
        DiscoProvoArcher,
        NecroMageWeaver,
        BardHealer
    }

    public class AdvancedCombatContext
    {
        public AdventurePartyState State { get; private set; }
        public AdventurePartyTactic Tactic { get; private set; }
        public Mobile Target { get; private set; }
        public Point3D Anchor { get; private set; }
        public DateTime Expires { get; private set; }

        public AdvancedCombatContext()
            : this(AdventurePartyState.Exploring, AdventurePartyTactic.Standard, null, Point3D.Zero, DateTime.MinValue)
        {
        }

        public AdvancedCombatContext(AdventurePartyState state, AdventurePartyTactic tactic, Mobile target, Point3D anchor, DateTime expires)
        {
            State = state;
            Tactic = tactic;
            Target = target;
            Anchor = anchor;
            Expires = expires;
        }

        public bool IsExpired
        {
            get { return Expires != DateTime.MinValue && DateTime.UtcNow >= Expires; }
        }

        public bool HasTacticalOrder
        {
            get { return Tactic != AdventurePartyTactic.Standard && !IsExpired; }
        }

        public bool IsTactic(AdventurePartyTactic tactic)
        {
            return Tactic == tactic && !IsExpired;
        }
    }

    public static class AdvancedCombatBrain
    {
        private const int BandageApproachRange = AdventurePartySettings.HealerBandageApproachRange;
        private const int EnemyOfOneScanRange = 8;
        private const int WitherMinimumTargets = 3;
        private const int WitherPocketEnemyScanRange = 10;
        private const int WitherPocketAllyScanRange = 14;
        private const int WitherPocketMaxActorDistance = 12;
        private const int WitherPocketUnsafeEnemyRange = 1;
        private const int WraithFormRecoveryDelay = 4500;
        private const int RecentSpellDebugLifetime = 60000;
        private const int RecentActionDebugLifetime = 20000;
        private const int SampireWeaponSwapCooldown = 3500;
        private const int ThinkDelay = 650;

        private enum SampireWeaponMode
        {
            Default,
            Crowd,
            ArmorIgnore,
            Defensive
        }

        private static readonly Direction[] MovementDirections =
        {
            Direction.North,
            Direction.Right,
            Direction.East,
            Direction.Down,
            Direction.South,
            Direction.Left,
            Direction.West,
            Direction.Up
        };

        private class BrainState
        {
            public long NextThink;
            public long NextSpell;
            public long NextSpecial;
            public long NextStance;
            public long NextBard;
            public long NextBandage;
            public long NextSpiritSpeak;
            public long NextMastery;
            public long NextMasteryDebug;
            public long NextWeaponSwap;
            public string LastSpell;
            public long LastSpellTime;
            public string LastAction;
            public long LastActionTime;
            public string LastMastery;
            public long LastMasteryTime;
            public Mobile PendingTarget;
            public long PendingTargetExpires;
        }

        private class WitherPocket
        {
            public Point3D Point;
            public int Targets;
            public double Score;
        }

        private static readonly Dictionary<Serial, BrainState> m_States = new Dictionary<Serial, BrainState>();
        private static readonly Dictionary<Serial, long> m_MovePulseUntil = new Dictionary<Serial, long>();

        public static void Think(BaseCreature actor, AdvancedCombatProfile profile)
        {
            Think(actor, profile, null);
        }

        public static void Think(BaseCreature actor, AdvancedCombatProfile profile, AdvancedCombatContext context)
        {
            if (actor == null || actor.Deleted || profile == AdvancedCombatProfile.None)
            {
                return;
            }

            BrainState state = GetState(actor);

            if (ResolvePendingTarget(actor, state))
            {
                return;
            }

            long now = Core.TickCount;

            if (now < state.NextThink || !actor.Alive || actor.Map == null || actor.Map == Map.Internal || actor.Paralyzed || actor.Frozen)
            {
                return;
            }

            state.NextThink = now + ThinkDelay;

            if (TryEscapeImmediateMeleeThreat(actor, profile, null, state, context))
            {
                return;
            }

            if (HandleRecoveryContext(actor, context, state))
            {
                return;
            }

            if (profile == AdvancedCombatProfile.NecroMageWeaver &&
                ShouldPrioritizeWitherBeforeSupport(actor, context) &&
                TryUseMageWither(actor, state, context, now))
            {
                return;
            }

            if (TryEmergencyHeal(actor, profile, state, context))
            {
                return;
            }

            if (TryMaintainActiveBandageDistance(actor, state))
            {
                return;
            }

            if (!AllowsOffense(actor, context))
            {
                Mobile threat = GetContextTarget(actor, context);

                actor.Combatant = null;
                actor.Warmode = false;

                if (threat != null && actor.InRange(threat, 8))
                {
                    if (StepAway(actor, threat))
                    {
                        RecordAction(state, String.Format("holding fire and stepping away from {0}.", GetDebugName(threat)));
                    }
                }
                else
                {
                    RecordAction(state, "holding fire during tactical disengage.");
                }

                return;
            }

            Mobile enemy = SelectEnemy(actor, profile, context);

            if (enemy != null)
            {
                actor.Combatant = enemy;
                actor.Direction = actor.GetDirectionTo(enemy);
            }

            if (TryEscapeImmediateMeleeThreat(actor, profile, enemy, state, context))
            {
                return;
            }

            if (IsChannelingDeathRay(actor))
            {
                return;
            }

            if (ApplyTacticalPositioning(actor, profile, enemy, context))
            {
                return;
            }

            switch (profile)
            {
                case AdvancedCombatProfile.SampireFighter:
                    ThinkSampire(actor, enemy, state, context);
                    break;
                case AdvancedCombatProfile.DiscoProvoArcher:
                    ThinkArcherBard(actor, enemy, state, context);
                    break;
                case AdvancedCombatProfile.NecroMageWeaver:
                    ThinkNecroMageWeaver(actor, enemy, state, context);
                    break;
                case AdvancedCombatProfile.BardHealer:
                    ThinkBardHealer(actor, enemy, state, context);
                    break;
            }
        }

        private static BrainState GetState(BaseCreature actor)
        {
            BrainState state;

            if (!m_States.TryGetValue(actor.Serial, out state))
            {
                state = new BrainState();
                m_States[actor.Serial] = state;
            }

            return state;
        }

        public static bool TryForceBandageHeal(BaseCreature actor, Mobile patient)
        {
            if (actor == null || patient == null || actor.Deleted || patient.Deleted ||
                !actor.Alive || actor.Map == null || actor.Map == Map.Internal ||
                actor.Map != patient.Map || actor.Paralyzed || actor.Frozen)
            {
                return false;
            }

            BrainState state = GetState(actor);
            return TryStartBandageHeal(actor, patient, state, Core.TickCount, IsUnderHeavyPressure(actor, patient));
        }

        private static bool ResolvePendingTarget(BaseCreature actor, BrainState state)
        {
            if (state.PendingTarget == null)
            {
                return false;
            }

            if (Core.TickCount > state.PendingTargetExpires || state.PendingTarget.Deleted || state.PendingTarget.Map != actor.Map)
            {
                state.PendingTarget = null;
                return false;
            }

            if (actor.Target == null)
            {
                return false;
            }

            Mobile target = state.PendingTarget;
            state.PendingTarget = null;
            actor.Target.Invoke(actor, target);
            return true;
        }

        private static void ThinkSampire(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;

            if (now >= state.NextSpell)
            {
                if (!TransformationSpellHelper.UnderTransformation(actor, typeof(VampiricEmbraceSpell)) &&
                    actor.Skills[SkillName.Necromancy].Value >= 99.0 && actor.Mana >= 23 &&
                    TryCast(actor, new VampiricEmbraceSpell(actor, null), null, state))
                {
                    state.NextSpell = now + 45000;
                    return;
                }

                if (actor.Hits < actor.HitsMax * 55 / 100 &&
                    actor.Skills[SkillName.Bushido].Value >= 25.0 &&
                    TryCast(actor, new Confidence(actor, null), null, state))
                {
                    state.NextSpell = now + 12000;
                    return;
                }

                if (enemy != null && actor.Skills[SkillName.Chivalry].Value >= 45.0 &&
                    !EnemyOfOneSpell.UnderEffect(actor) && ShouldUseEnemyOfOne(actor, enemy, context) &&
                    TryCast(actor, new EnemyOfOneSpell(actor, null), null, state))
                {
                    state.NextSpell = now + 10000;
                    return;
                }

                if (enemy != null && actor.Skills[SkillName.Chivalry].Value >= 15.0 &&
                    !ConsecrateWeaponSpell.IsUnderEffects(actor) &&
                    TryCast(actor, new ConsecrateWeaponSpell(actor, null), null, state))
                {
                    state.NextSpell = now + 6500;
                    return;
                }

                if (enemy != null && ShouldUseDivineFury(actor, enemy, context) &&
                    TryCast(actor, new DivineFurySpell(actor, null), null, state))
                {
                    state.NextSpell = now + 9000;
                    return;
                }

                if (enemy != null && (IsHardTarget(enemy) || actor.Hits < actor.HitsMax * 80 / 100) &&
                    actor.Skills[SkillName.Necromancy].Value >= 20.0 &&
                    actor.Mana >= 7 && TryCast(actor, new CurseWeaponSpell(actor, null), null, state))
                {
                    state.NextSpell = now + 18000;
                    return;
                }
            }

            if (enemy != null && TrySelectSampireWeapon(actor, enemy, state, context, now))
            {
                return;
            }

            if (enemy != null && TryUseSampireStance(actor, enemy, state, now))
            {
                return;
            }

            if (enemy != null && now >= state.NextSpecial)
            {
                if (ShouldUseSampireDefensiveAbility(actor, enemy, context) && TrySetWeaponAbility(actor, WeaponAbility.Feint))
                {
                    state.NextSpecial = now + 5500;
                }
                else if (TryUseSampireMastery(actor, enemy, state, context))
                {
                    return;
                }
                else if (CountEnemiesInRange(actor, 1) >= 2 && !IsTactic(context, AdventurePartyTactic.FocusFire) &&
                    TrySetWeaponAbility(actor, WeaponAbility.WhirlwindAttack, WeaponAbility.Bladeweave))
                {
                    state.NextSpecial = now + 5000;
                }
                else if (ShouldUseSampireWeaponBurst(actor, enemy, context) &&
                    TrySetWeaponAbility(actor, WeaponAbility.ArmorIgnore, WeaponAbility.CrushingBlow, WeaponAbility.Bladeweave))
                {
                    state.NextSpecial = now + 5000;
                }
                else if (TryUseLightningStrike(actor))
                {
                    state.NextSpecial = now + 2500;
                }
            }
        }

        private static void ThinkArcherBard(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;
            Mobile urgentProvokeTarget = SelectUrgentFighterProvocationTarget(actor, enemy);

            if (urgentProvokeTarget != null && TryProvoke(actor, urgentProvokeTarget, state, context, true))
            {
                return;
            }

            if (enemy != null && TryProvoke(actor, enemy, state, context, false))
            {
                return;
            }

            if (enemy != null && TryDiscord(actor, enemy, state))
            {
                return;
            }

            int desiredDistance = IsPlayingTheOddsActive(actor) ? 3 : IsTactic(context, AdventurePartyTactic.Kite) ? 6 : 5;

            if (enemy != null && actor.InRange(enemy, desiredDistance))
            {
                StepAway(actor, enemy);
            }

            if (enemy != null && TryUseArcherChivalry(actor, enemy, state, now, context))
            {
                return;
            }

            if (enemy != null && TryUseArcherMastery(actor, enemy, state, context))
            {
                return;
            }

            if (enemy != null && now >= state.NextSpecial)
            {
                if (enemy.Hits > enemy.HitsMax * 35 / 100 && TrySetWeaponAbility(actor, WeaponAbility.ArmorIgnore))
                {
                    state.NextSpecial = now + 4500;
                }
                else if (TrySetWeaponAbility(actor, WeaponAbility.MovingShot, WeaponAbility.DoubleShot))
                {
                    state.NextSpecial = now + 4500;
                }
            }
        }

        private static void ThinkNecroMageWeaver(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;

            if (enemy == null || now < state.NextSpell)
            {
                return;
            }

            int desiredDistance = IsTactic(context, AdventurePartyTactic.Kite) || IsChokeTactic(context) ? 6 : 5;

            if (!TransformationSpellHelper.UnderTransformation(actor, typeof(WraithFormSpell)) &&
                actor.Skills[SkillName.Necromancy].Value >= 20.0 && actor.Mana >= 17 &&
                TryCast(actor, new WraithFormSpell(actor, null), null, state))
            {
                state.NextSpell = now + WraithFormRecoveryDelay;
                return;
            }

            if (TryUseMageSupport(actor, state, now))
            {
                return;
            }

            if (TryUseMageWither(actor, state, context, now))
            {
                return;
            }

            bool movedForSafety = false;

            if (actor.InRange(enemy, desiredDistance))
            {
                StepAway(actor, enemy);
                movedForSafety = true;
            }

            if (movedForSafety)
            {
                TryUseMobileNecromancyAfterMove(actor, enemy, state, now);
                return;
            }

            if (TryUseMageDeathRay(actor, enemy, state, context))
            {
                return;
            }

            if (TryUseMageDefensiveMastery(actor, enemy, state, context))
            {
                return;
            }

            if (ShouldUseWordOfDeath(actor, enemy) &&
                TryCast(actor, new WordOfDeathSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 8000;
                return;
            }

            if (!EvilOmenSpell.UnderEffects(enemy) && actor.Skills[SkillName.Necromancy].Value >= 20.0 &&
                Utility.RandomDouble() < 0.35 && TryCast(actor, new EvilOmenSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 7000;
                return;
            }

            if (actor.Skills[SkillName.Necromancy].Value >= 40.0 && Utility.RandomDouble() < 0.35 &&
                TryCast(actor, new CorpseSkinSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 7000;
                return;
            }

            if (actor.Mana >= 40 && TryCast(actor, new FlameStrikeSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 7000;
            }
            else if (actor.Mana >= 20 && TryCast(actor, new PainSpikeSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 6000;
            }
            else if (actor.Mana >= 20 && TryCast(actor, new EnergyBoltSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 6000;
            }
        }

        private static bool TryUseMobileNecromancyAfterMove(BaseCreature actor, Mobile enemy, BrainState state, long now)
        {
            if (actor == null || enemy == null || state == null || actor.Mana < 20 ||
                actor.Skills[SkillName.Necromancy].Value < 20.0)
            {
                return false;
            }

            if (!EvilOmenSpell.UnderEffects(enemy) && Utility.RandomDouble() < 0.35 &&
                TryCast(actor, new EvilOmenSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 7000;
                return true;
            }

            if (actor.Skills[SkillName.Necromancy].Value >= 40.0 && Utility.RandomDouble() < 0.35 &&
                TryCast(actor, new CorpseSkinSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 7000;
                return true;
            }

            if (TryCast(actor, new PainSpikeSpell(actor, null), enemy, state))
            {
                state.NextSpell = now + 6000;
                return true;
            }

            return false;
        }

        private static bool ShouldUseWordOfDeath(BaseCreature actor, Mobile enemy)
        {
            if (actor == null || enemy == null || enemy.Player || enemy.HitsMax <= 0 ||
                actor.Mana < 50 || actor.Skills[SkillName.Spellweaving].Value < 83.0)
            {
                return false;
            }

            int focusLevel = Math.Max(0, Math.Min(6, ArcanistSpell.GetFocusLevel(actor)));

            if (focusLevel <= 0)
            {
                return false;
            }

            double threshold = 0.05 * focusLevel;
            double currentPercent = (double)enemy.Hits / (double)enemy.HitsMax;

            if (currentPercent >= threshold)
            {
                return false;
            }

            int practicalFinishCap = 300 + Math.Min(60, (int)(actor.Skills[SkillName.Spellweaving].Value / 2.0));

            return enemy.Hits <= practicalFinishCap;
        }

        private static void ThinkBardHealer(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;

            if (TryEmergencyHeal(actor, AdvancedCombatProfile.BardHealer, state, context))
            {
                return;
            }

            if ((IsTactic(context, AdventurePartyTactic.ProtectHealer) || IsTactic(context, AdventurePartyTactic.Kite) ||
                 IsChokeTactic(context)) && enemy != null && actor.InRange(enemy, 6))
            {
                StepAway(actor, enemy);
            }

            if (enemy != null && TryDiscord(actor, enemy, state))
            {
                return;
            }

            if (enemy != null && now >= state.NextSpell && actor.Mana >= 45)
            {
                if (TryCast(actor, new CurseSpell(actor, null), enemy, state))
                {
                    state.NextSpell = now + 9000;
                }
            }
        }

        private static bool TryUseSampireStance(BaseCreature actor, Mobile enemy, BrainState state, long now)
        {
            if (actor == null || enemy == null || now < state.NextStance || !actor.InRange(enemy, 1) ||
                actor.HitsMax <= 0 || actor.Hits < actor.HitsMax * 70 / 100 ||
                actor.Mana < 8 || actor.Skills[SkillName.Bushido].Value < 40.0 ||
                CounterAttack.IsCountering(actor) || Confidence.IsConfident(actor) || Evasion.IsEvading(actor))
            {
                return false;
            }

            if (TryCast(actor, new CounterAttack(actor, null), null, state))
            {
                state.NextStance = now + 18000;
                return true;
            }

            state.NextStance = now + 4000;
            return false;
        }

        private static bool TryUseLightningStrike(BaseCreature actor)
        {
            if (actor == null || actor.Mana < 10 || actor.Skills[SkillName.Bushido].Value < 50.0 ||
                WeaponAbility.GetCurrentAbility(actor) != null || SpecialMove.GetCurrentMove(actor) != null)
            {
                return false;
            }

            return SpecialMove.SetCurrentMove(actor, new LightningStrike());
        }

        private static bool TrySelectSampireWeapon(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context, long now)
        {
            if (actor == null || enemy == null || state == null || now < state.NextWeaponSwap ||
                actor.Spell != null || WeaponAbility.GetCurrentAbility(actor) != null || SpecialMove.GetCurrentMove(actor) != null)
            {
                return false;
            }

            SampireWeaponMode mode = SelectSampireWeaponMode(actor, enemy, context);
            BaseWeapon current = GetEquippedWeapon(actor);
            BaseWeapon weapon = FindBestSampireWeapon(actor, mode);

            if (weapon == null || weapon == current ||
                GetSampireWeaponPreference(current, mode) >= GetSampireWeaponPreference(weapon, mode))
            {
                return false;
            }

            if (!StowEquippedWeapon(actor, actor.FindItemOnLayer(Layer.OneHanded) as BaseWeapon, weapon) ||
                !StowEquippedWeapon(actor, actor.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon, weapon))
            {
                state.NextWeaponSwap = now + 1500;
                return false;
            }

            if (actor.EquipItem(weapon))
            {
                state.NextWeaponSwap = now + SampireWeaponSwapCooldown;
                RecordAction(state, String.Format("switched to {0} weapon ({1}).", GetSampireWeaponModeName(mode), weapon.GetType().Name));
                return true;
            }

            if (current != null && !current.Deleted && current.Parent != actor)
            {
                actor.EquipItem(current);
            }

            state.NextWeaponSwap = now + 1500;
            return false;
        }

        private static SampireWeaponMode SelectSampireWeaponMode(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            int adjacentEnemies = CountEnemiesInRange(actor, 1);
            int closeEnemies = CountEnemiesInRange(actor, 3);
            bool underPressure = CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0;
            bool lowHealth = actor.HitsMax > 0 && actor.Hits < actor.HitsMax * 60 / 100;

            if (lowHealth && (underPressure || closeEnemies > 0 || IsHardTarget(enemy)))
            {
                return SampireWeaponMode.Defensive;
            }

            if (adjacentEnemies >= 2 || (IsChokeTactic(context) && closeEnemies >= 2))
            {
                return SampireWeaponMode.Crowd;
            }

            if ((IsTactic(context, AdventurePartyTactic.PowerUp) || IsTactic(context, AdventurePartyTactic.FocusFire) || IsHardTarget(enemy)) &&
                CountEnemiesNearPoint(actor, enemy.Location, 4) <= 1)
            {
                return SampireWeaponMode.ArmorIgnore;
            }

            if (closeEnemies >= 2)
            {
                return SampireWeaponMode.Crowd;
            }

            return SampireWeaponMode.Default;
        }

        private static BaseWeapon FindBestSampireWeapon(BaseCreature actor, SampireWeaponMode mode)
        {
            BaseWeapon best = null;
            int bestScore = 0;

            ConsiderSampireWeapon(GetEquippedWeapon(actor), mode, ref best, ref bestScore);

            Container pack = actor == null ? null : actor.Backpack;

            if (pack == null)
            {
                return best;
            }

            for (int i = 0; i < pack.Items.Count; i++)
            {
                ConsiderSampireWeapon(pack.Items[i] as BaseWeapon, mode, ref best, ref bestScore);
            }

            return best;
        }

        private static void ConsiderSampireWeapon(BaseWeapon weapon, SampireWeaponMode mode, ref BaseWeapon best, ref int bestScore)
        {
            int score = GetSampireWeaponPreference(weapon, mode);

            if (score > bestScore)
            {
                best = weapon;
                bestScore = score;
            }
        }

        private static bool StowEquippedWeapon(BaseCreature actor, BaseWeapon weapon, BaseWeapon target)
        {
            if (actor == null || weapon == null || weapon == target || weapon.Parent != actor)
            {
                return true;
            }

            Container pack = actor.Backpack;

            if (pack == null)
            {
                return false;
            }

            pack.DropItem(weapon);
            return weapon.Parent == pack;
        }

        private static BaseWeapon GetEquippedWeapon(BaseCreature actor)
        {
            if (actor == null)
            {
                return null;
            }

            BaseWeapon weapon = actor.FindItemOnLayer(Layer.OneHanded) as BaseWeapon;

            if (weapon == null)
            {
                weapon = actor.FindItemOnLayer(Layer.TwoHanded) as BaseWeapon;
            }

            return weapon;
        }

        private static bool IsSampireWeaponForMode(BaseWeapon weapon, SampireWeaponMode mode)
        {
            return GetSampireWeaponPreference(weapon, mode) > 0;
        }

        private static int GetSampireWeaponPreference(BaseWeapon weapon, SampireWeaponMode mode)
        {
            if (weapon == null)
            {
                return 0;
            }

            switch (mode)
            {
                case SampireWeaponMode.Crowd:
                    if (weapon is DoubleAxe)
                    {
                        return 120;
                    }

                    if (weapon is RadiantScimitar)
                    {
                        return 100;
                    }

                    if (HasWeaponAbility(weapon, WeaponAbility.WhirlwindAttack))
                    {
                        return 80;
                    }

                    return HasWeaponAbility(weapon, WeaponAbility.Bladeweave) ? 60 : 0;

                case SampireWeaponMode.ArmorIgnore:
                    if (weapon is Broadsword)
                    {
                        return 100;
                    }

                    return HasWeaponAbility(weapon, WeaponAbility.ArmorIgnore) ? 80 : 0;

                case SampireWeaponMode.Defensive:
                    if (weapon is Daisho)
                    {
                        return 100;
                    }

                    return HasWeaponAbility(weapon, WeaponAbility.Feint) ? 80 : 0;

                default:
                    if (weapon is DoubleAxe)
                    {
                        return 120;
                    }

                    if (weapon is RadiantScimitar)
                    {
                        return 80;
                    }

                    return HasWeaponAbility(weapon, WeaponAbility.WhirlwindAttack) ? 60 : 0;
            }
        }

        private static bool HasWeaponAbility(BaseWeapon weapon, WeaponAbility ability)
        {
            return weapon != null && ability != null &&
                (weapon.PrimaryAbility == ability || weapon.SecondaryAbility == ability);
        }

        private static string GetSampireWeaponModeName(SampireWeaponMode mode)
        {
            switch (mode)
            {
                case SampireWeaponMode.Crowd:
                    return "crowd-control";
                case SampireWeaponMode.ArmorIgnore:
                    return "armor-ignore";
                case SampireWeaponMode.Defensive:
                    return "defensive";
                default:
                    return "default";
            }
        }

        private static bool ShouldUseSampireDefensiveAbility(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (!IsValidEnemy(actor, enemy) || actor.HitsMax <= 0 || actor.Mana < 20)
            {
                return false;
            }

            bool lowHealth = actor.Hits < actor.HitsMax * 65 / 100;
            bool directPressure = enemy.Combatant == actor ||
                CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0 ||
                CountEnemiesInRange(actor, 2) >= 2;

            if (lowHealth && directPressure)
            {
                return true;
            }

            return IsChokeTactic(context) && actor.Hits < actor.HitsMax * 80 / 100 && CountEnemiesInRange(actor, 2) >= 2;
        }

        private static bool ShouldUseSampireWeaponBurst(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (!IsValidEnemy(actor, enemy) || enemy.HitsMax <= 0)
            {
                return false;
            }

            if (IsTactic(context, AdventurePartyTactic.PowerUp) || IsTactic(context, AdventurePartyTactic.FocusFire))
            {
                return true;
            }

            if (IsHardTarget(enemy))
            {
                return true;
            }

            return actor.Mana > 35 && enemy.Hits > enemy.HitsMax * 60 / 100;
        }

        private static bool TryUseArcherChivalry(BaseCreature actor, Mobile enemy, BrainState state, long now, AdvancedCombatContext context)
        {
            if (actor == null || enemy == null || now < state.NextSpell || actor.Skills[SkillName.Chivalry].Value < 15.0)
            {
                return false;
            }

            if (actor.Skills[SkillName.Chivalry].Value >= 45.0 && !EnemyOfOneSpell.UnderEffect(actor) &&
                ShouldUseEnemyOfOne(actor, enemy, context) && TryCast(actor, new EnemyOfOneSpell(actor, null), null, state))
            {
                state.NextSpell = now + 10000;
                return true;
            }

            if (!ConsecrateWeaponSpell.IsUnderEffects(actor) && TryCast(actor, new ConsecrateWeaponSpell(actor, null), null, state))
            {
                state.NextSpell = now + 6500;
                return true;
            }

            if (ShouldUseDivineFury(actor, enemy, context) && TryCast(actor, new DivineFurySpell(actor, null), null, state))
            {
                state.NextSpell = now + 9000;
                return true;
            }

            return false;
        }

        private static bool TryUseMageSupport(BaseCreature actor, BrainState state, long now)
        {
            if (actor == null || now < state.NextSpell || actor.Mana < 35 ||
                actor.Skills[SkillName.Spellweaving].Value < 60.0)
            {
                return false;
            }

            Mobile ally = FindMostInjuredAlly(actor, 8);

            if (ally == null || ally == actor || ally.HitsMax <= 0 ||
                (!ally.Poisoned && ally.Hits >= ally.HitsMax * 75 / 100) ||
                GiftOfRenewalSpell.m_Table.ContainsKey(ally) ||
                !actor.InRange(ally, 10) || !actor.InLOS(ally) ||
                !actor.CanBeBeneficial(ally, false, true))
            {
                return false;
            }

            if (TryCast(actor, new GiftOfRenewalSpell(actor, null), ally, state))
            {
                state.NextSpell = now + 6500;
                return true;
            }

            return false;
        }

        private static bool TryUseMageWither(BaseCreature actor, BrainState state, AdvancedCombatContext context, long now)
        {
            if (actor == null || state == null || now < state.NextSpell ||
                actor.Mana < 23 || actor.Skills[SkillName.Necromancy].Value < 60.0)
            {
                return false;
            }

            int range = GetWitherRange();
            int currentTargets = CountEnemiesNearPoint(actor, actor.Location, range);
            bool currentSafe = CountEnemiesNearPoint(actor, actor.Location, WitherPocketUnsafeEnemyRange) == 0 &&
                CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) == 0;

            if (currentTargets >= WitherMinimumTargets && currentSafe &&
                TryCast(actor, new WitherSpell(actor, null), null, state))
            {
                state.NextSpell = now + 7000;
                RecordAction(state, String.Format("cast Wither in place on {0} target(s).", currentTargets));
                return true;
            }

            WitherPocket pocket;

            if (!TryFindWitherPocket(actor, range, out pocket) || pocket == null ||
                pocket.Targets < WitherMinimumTargets)
            {
                return false;
            }

            if (GetDistance(actor.Location, pocket.Point) <= 0.5)
            {
                if (TryCast(actor, new WitherSpell(actor, null), null, state))
                {
                    state.NextSpell = now + 7000;
                    RecordAction(state, String.Format("cast Wither from pocket on {0} target(s).", pocket.Targets));
                    return true;
                }

                return false;
            }

            if (GetDistance(actor.Location, pocket.Point) > WitherPocketMaxActorDistance)
            {
                return false;
            }

            if (MoveToward(actor, pocket.Point, true, state,
                String.Format("moving to Wither pocket {0} covering {1} target(s).", FormatPoint(pocket.Point), pocket.Targets)))
            {
                return true;
            }

            return false;
        }

        private static bool ShouldPrioritizeWitherBeforeSupport(BaseCreature actor, AdvancedCombatContext context)
        {
            if (actor == null || actor.Mana < 23 || actor.Skills[SkillName.Necromancy].Value < 60.0 ||
                IsTactic(context, AdventurePartyTactic.Recovering) ||
                CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0)
            {
                return false;
            }

            if (HasCriticalAllyNeedingMageHeal(actor))
            {
                return false;
            }

            BaseAdventurer fighter = FindFighterAlly(actor);

            if (fighter == null)
            {
                return false;
            }

            List<Mobile> enemies = GetEnemiesNear(actor, fighter.Location, WitherPocketEnemyScanRange);

            return enemies.Count >= WitherMinimumTargets;
        }

        private static bool HasCriticalAllyNeedingMageHeal(BaseCreature actor)
        {
            Mobile patient = FindMostInjuredAlly(actor, 10);

            if (patient == null || patient.HitsMax <= 0)
            {
                return false;
            }

            if (patient.Poisoned)
            {
                return true;
            }

            int threshold = IsFighterPatient(patient) ? 45 : 55;
            return patient.Hits < patient.HitsMax * threshold / 100;
        }

        private static bool TryFindWitherPocket(BaseCreature actor, int witherRange, out WitherPocket pocket)
        {
            pocket = null;

            BaseAdventurer fighter = FindFighterAlly(actor);

            if (fighter == null)
            {
                return false;
            }

            List<Mobile> enemies = GetEnemiesNear(actor, fighter.Location, WitherPocketEnemyScanRange);

            if (enemies.Count < WitherMinimumTargets)
            {
                return false;
            }

            if (CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0)
            {
                return false;
            }

            int backX;
            int backY;

            GetBackVectorFromEnemies(fighter.Location, enemies, out backX, out backY);

            int sideX = -backY;
            int sideY = backX;
            WitherPocket best = null;

            for (int back = 1; back <= 5; back++)
            {
                for (int side = -3; side <= 3; side++)
                {
                    Point3D raw = new Point3D(
                        fighter.X + (backX * back) + (sideX * side),
                        fighter.Y + (backY * back) + (sideY * side),
                        fighter.Z);

                    Point3D point;

                    if (!TryNormalizeStandPoint(actor, raw, out point))
                    {
                        continue;
                    }

                    int targets = CountEnemiesNearPoint(enemies, point, witherRange);

                    if (targets < WitherMinimumTargets)
                    {
                        continue;
                    }

                    double score = ScoreWitherPocket(actor, fighter, enemies, point, targets);

                    if (score <= Double.MinValue / 2.0)
                    {
                        continue;
                    }

                    if (best == null || score > best.Score)
                    {
                        best = new WitherPocket();
                        best.Point = point;
                        best.Targets = targets;
                        best.Score = score;
                    }
                }
            }

            pocket = best;
            return pocket != null;
        }

        private static double ScoreWitherPocket(BaseCreature actor, BaseAdventurer fighter, List<Mobile> enemies, Point3D point, int targets)
        {
            double nearestEnemy = GetNearestEnemyDistance(enemies, point);

            if (nearestEnemy <= WitherPocketUnsafeEnemyRange + 0.1)
            {
                return Double.MinValue;
            }

            double fighterDistance = GetDistance(point, fighter.Location);
            double actorDistance = GetDistance(point, actor.Location);
            int tankedTargets = CountEnemiesTargeting(enemies, fighter);
            int actorTargets = CountEnemiesTargeting(enemies, actor);
            double score = targets * 100.0;

            score += tankedTargets * 20.0;
            score -= actorTargets * 80.0;
            score -= Math.Abs(fighterDistance - 3.0) * 9.0;
            score -= actorDistance * 2.0;

            if (nearestEnemy < 2.5)
            {
                score -= (2.5 - nearestEnemy) * 45.0;
            }
            else
            {
                score += Math.Min(18.0, nearestEnemy * 3.0);
            }

            if (fighterDistance > 5.0)
            {
                score -= (fighterDistance - 5.0) * 30.0;
            }

            return score;
        }

        private static BaseAdventurer FindFighterAlly(BaseCreature actor)
        {
            BaseAdventurer self = actor as BaseAdventurer;
            BaseAdventurer best = null;
            double bestDistance = Double.MaxValue;
            IPooledEnumerable eable = actor.GetMobilesInRange(WitherPocketAllyScanRange);

            foreach (Mobile mobile in eable)
            {
                BaseAdventurer adventurer = mobile as BaseAdventurer;

                if (adventurer == null || adventurer == actor || adventurer.Deleted || !adventurer.Alive ||
                    adventurer.Map != actor.Map || adventurer.Role != AdventurePartyRole.Fighter)
                {
                    continue;
                }

                if (self != null && adventurer.Controller != self.Controller)
                {
                    continue;
                }

                double distance = actor.GetDistanceToSqrt(adventurer);

                if (best == null || distance < bestDistance)
                {
                    best = adventurer;
                    bestDistance = distance;
                }
            }

            eable.Free();
            return best;
        }

        private static List<Mobile> GetEnemiesNear(BaseCreature actor, Point3D point, int range)
        {
            List<Mobile> enemies = new List<Mobile>();
            Map map = actor == null ? null : actor.Map;

            if (map == null || map == Map.Internal)
            {
                return enemies;
            }

            IPooledEnumerable eable = map.GetMobilesInRange(point, range);

            foreach (Mobile mobile in eable)
            {
                if (IsValidEnemy(actor, mobile))
                {
                    enemies.Add(mobile);
                }
            }

            eable.Free();
            return enemies;
        }

        private static void GetBackVectorFromEnemies(Point3D fighter, List<Mobile> enemies, out int backX, out int backY)
        {
            double x = 0.0;
            double y = 0.0;

            for (int i = 0; i < enemies.Count; i++)
            {
                x += enemies[i].X;
                y += enemies[i].Y;
            }

            x /= Math.Max(1, enemies.Count);
            y /= Math.Max(1, enemies.Count);

            double frontX = x - fighter.X;
            double frontY = y - fighter.Y;

            if (Math.Abs(frontX) >= Math.Abs(frontY))
            {
                backX = -Math.Sign(frontX);
                backY = 0;
            }
            else
            {
                backX = 0;
                backY = -Math.Sign(frontY);
            }

            if (backX == 0 && backY == 0)
            {
                backY = 1;
            }
        }

        private static bool TryNormalizeStandPoint(BaseCreature actor, Point3D raw, out Point3D point)
        {
            point = raw;

            if (actor == null || actor.Map == null || actor.Map == Map.Internal)
            {
                return false;
            }

            int z = actor.Map.GetAverageZ(raw.X, raw.Y);
            point = new Point3D(raw.X, raw.Y, z);

            return actor.Map.CanFit(point.X, point.Y, point.Z, 16, false, true, true, actor);
        }

        private static int GetWitherRange()
        {
            return Core.ML ? 4 : 5;
        }

        private static bool HandleRecoveryContext(BaseCreature actor, AdvancedCombatContext context, BrainState state)
        {
            if (!IsTactic(context, AdventurePartyTactic.Recovering))
            {
                return false;
            }

            Mobile threat = actor == null ? null : actor.Combatant as Mobile;

            if (!IsValidEnemy(actor, threat))
            {
                threat = GetContextTarget(actor, context);
            }

            if (ShouldSuppressBacklineOffense(actor, context))
            {
                actor.Combatant = null;
                actor.Warmode = false;
            }

            if (IsValidEnemy(actor, threat) && actor.InRange(threat, 8))
            {
                if (StepAway(actor, threat))
                {
                    RecordAction(state, String.Format("recovery safety step away from {0}.", GetDebugName(threat)));
                }
            }

            return true;
        }

        private static bool TryMaintainActiveBandageDistance(BaseCreature actor, BrainState state)
        {
            if (actor == null || state == null)
            {
                return false;
            }

            BandageContext context = BandageContext.GetContext(actor);

            if (context == null || context.Patient == null || context.Patient.Deleted ||
                actor.Map == null || actor.Map == Map.Internal || actor.Map != context.Patient.Map)
            {
                return false;
            }

            Mobile patient = context.Patient;

            if (actor.InRange(patient, Bandage.Range) && actor.InLOS(patient))
            {
                return false;
            }

            if (FindImmediateMeleeThreat(actor, null) != null)
            {
                return false;
            }

            bool moved = TryApproachBandageTarget(actor, patient, state, true);

            if (moved)
            {
                RecordAction(state, String.Format("maintaining bandage range on {0}.", GetDebugName(patient)));
            }

            return moved;
        }

        private static bool TryEmergencyHeal(BaseCreature actor, AdvancedCombatProfile profile, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;

            Mobile patient = FindEmergencyHealPatient(actor, profile, profile == AdvancedCombatProfile.BardHealer ? 10 : 6);

            if (patient == null)
            {
                return false;
            }

            bool heavyPressure = IsUnderHeavyPressure(actor, patient);

            if (!IsEmergencyHealUrgent(patient, profile, heavyPressure))
            {
                return false;
            }

            if (ShouldLayerBandageBeforeSpell(actor, patient, profile, heavyPressure) &&
                TryStartBandageHeal(actor, patient, state, now, heavyPressure))
            {
                return true;
            }

            if (TryCastEmergencyHealSpell(actor, patient, state, now, heavyPressure))
            {
                return true;
            }

            if (TryUseEmergencySpiritSpeak(actor, patient, state, now, context))
            {
                return true;
            }

            return TryStartBandageHeal(actor, patient, state, now, heavyPressure);
        }

        private static bool ShouldLayerBandageBeforeSpell(BaseCreature actor, Mobile patient, AdvancedCombatProfile profile, bool heavyPressure)
        {
            if (actor == null || patient == null || profile != AdvancedCombatProfile.BardHealer ||
                !heavyPressure || !IsFighterPatient(patient))
            {
                return false;
            }

            if (actor.Mana <= AdventurePartySettings.HealerLowManaBandageMana)
            {
                return true;
            }

            return actor.InRange(patient, Bandage.Range) &&
                actor.InLOS(patient) &&
                patient.HitsMax > 0 &&
                patient.Hits < patient.HitsMax * 95 / 100;
        }

        private static bool TryCastEmergencyHealSpell(BaseCreature actor, Mobile patient, BrainState state, long now, bool heavyPressure)
        {
            if (now < state.NextSpell)
            {
                return false;
            }

            if (patient.Poisoned && actor.Mana >= 10 && actor.Skills[SkillName.Magery].Value >= 50.0 &&
                TryCast(actor, new CureSpell(actor, null), patient, state))
            {
                state.NextSpell = now + 5000;
                return true;
            }

            int threshold = GetEmergencySpellHealThreshold(patient, heavyPressure);

            if (patient.Hits < patient.HitsMax * threshold / 100 && actor.Mana >= 20 &&
                actor.Skills[SkillName.Magery].Value >= 65.0 && TryCast(actor, new GreaterHealSpell(actor, null), patient, state))
            {
                state.NextSpell = now + 5500;
                return true;
            }

            return false;
        }

        private static int GetEmergencySpellHealThreshold(Mobile patient, bool heavyPressure)
        {
            if (IsFighterPatient(patient))
            {
                return heavyPressure ? 90 : 70;
            }

            return heavyPressure ? 75 : 55;
        }

        private static bool TryStartBandageHeal(BaseCreature actor, Mobile patient, BrainState state, long now, bool heavyPressure)
        {
            if (now < state.NextBandage || BandageContext.GetContext(actor) != null ||
                actor.Skills[SkillName.Healing].Value < 60.0 || actor.Skills[SkillName.Anatomy].Value < 60.0)
            {
                return false;
            }

            int threshold = GetBandageStartThreshold(patient, heavyPressure);

            if (!patient.Poisoned && patient.Hits >= patient.HitsMax * threshold / 100)
            {
                return false;
            }

            Bandage bandage = FindBandage(actor);

            if (bandage == null)
            {
                return false;
            }

            if (!actor.InRange(patient, Bandage.Range) || !actor.InLOS(patient) || !actor.CanBeBeneficial(patient, false, true))
            {
                if (ShouldDeferBandageApproachForSpellSafety(actor, patient, heavyPressure))
                {
                    RecordAction(state, String.Format("holding bandage approach for safer magic healing on {0}.", GetDebugName(patient)));
                    return false;
                }

                return TryApproachBandageTarget(actor, patient, state, heavyPressure);
            }

            BandageContext context = BandageContext.BeginHeal(actor, patient);

            if (context == null)
            {
                return false;
            }

            bandage.Consume();

            TimeSpan delay = BandageContext.GetDelay(actor, patient);
            state.NextBandage = now + Math.Max(1000, (int)delay.TotalMilliseconds + 500);
            RecordAction(state, String.Format("started bandage on {0}.", GetDebugName(patient)));
            return true;
        }

        private static bool ShouldDeferBandageApproachForSpellSafety(BaseCreature actor, Mobile patient, bool heavyPressure)
        {
            if (actor == null || patient == null || !heavyPressure || !IsFighterPatient(patient))
            {
                return false;
            }

            if (actor.InRange(patient, Bandage.Range) && actor.InLOS(patient))
            {
                return false;
            }

            if (actor.Mana <= AdventurePartySettings.HealerLowManaBandageMana)
            {
                return false;
            }

            if (patient.HitsMax > 0 &&
                patient.Hits < patient.HitsMax * AdventurePartySettings.HealerCriticalBandageHitPercent / 100)
            {
                return false;
            }

            if (CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0)
            {
                return true;
            }

            return CountEnemiesNearPoint(actor, patient.Location, AdventurePartySettings.BacklineThreatRange) >= 2 &&
                patient.HitsMax > 0 &&
                patient.Hits < patient.HitsMax * 65 / 100;
        }

        private static bool TryApproachBandageTarget(BaseCreature actor, Mobile patient, BrainState state, bool heavyPressure)
        {
            if (!ShouldApproachForBandage(actor, patient, heavyPressure) ||
                actor == patient || actor.Map != patient.Map ||
                actor.GetDistanceToSqrt(patient) > BandageApproachRange ||
                !actor.CanBeBeneficial(patient, false, true) ||
                FindImmediateMeleeThreat(actor, null) != null)
            {
                return false;
            }

            Point3D standPoint;

            if (!TryFindBandageStandPoint(actor, patient, out standPoint))
            {
                bool staged = TryStepTowardBandageStaging(actor, patient);

                if (staged)
                {
                    RecordAction(state, String.Format("moving to bandage staging for {0}.", GetDebugName(patient)));
                }

                return staged;
            }

            bool moved = TryStepTowardBandagePoint(actor, patient, standPoint);

            if (moved)
            {
                RecordAction(state, String.Format("moving to bandage {0}.", GetDebugName(patient)));
            }

            return moved;
        }

        private static bool ShouldApproachForBandage(BaseCreature actor, Mobile patient, bool heavyPressure)
        {
            if (actor == null || patient == null || patient.HitsMax <= 0)
            {
                return false;
            }

            if (IsFighterPatient(patient))
            {
                int fighterThreshold = heavyPressure ? 95 : 85;
                return patient.Poisoned || patient.Hits < patient.HitsMax * fighterThreshold / 100;
            }

            return heavyPressure && (patient.Poisoned || patient.Hits < patient.HitsMax * 70 / 100);
        }

        private static bool IsEmergencyHealUrgent(Mobile patient, AdvancedCombatProfile profile, bool heavyPressure)
        {
            if (patient == null || patient.HitsMax <= 0)
            {
                return false;
            }

            if (patient.Poisoned)
            {
                return true;
            }

            int threshold = heavyPressure ? 85 : 65;

            if (profile == AdvancedCombatProfile.BardHealer && IsFighterPatient(patient))
            {
                threshold = heavyPressure ? 95 : 85;
            }

            return patient.Hits < patient.HitsMax * threshold / 100;
        }

        private static int GetBandageStartThreshold(Mobile patient, bool heavyPressure)
        {
            if (IsFighterPatient(patient))
            {
                return heavyPressure ? 95 : 85;
            }

            return heavyPressure ? 90 : 78;
        }

        private static bool IsFighterPatient(Mobile patient)
        {
            BaseAdventurer adventurer = patient as BaseAdventurer;
            return adventurer != null && adventurer.Role == AdventurePartyRole.Fighter;
        }

        private static bool TryFindBandageStandPoint(BaseCreature actor, Mobile patient, out Point3D standPoint)
        {
            standPoint = Point3D.Zero;

            if (actor == null || patient == null || actor.Map == null || actor.Map != patient.Map)
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

                    if (!TryCreateBandageStandPoint(actor, patient, patient.X + xOffset, patient.Y + yOffset, out candidate))
                    {
                        continue;
                    }

                    double score = ScoreBandageStandPoint(actor, patient, candidate);

                    if (score < bestScore)
                    {
                        standPoint = candidate;
                        bestScore = score;
                    }
                }
            }

            return bestScore < Double.MaxValue;
        }

        private static bool TryCreateBandageStandPoint(BaseCreature actor, Mobile patient, int x, int y, out Point3D point)
        {
            point = Point3D.Zero;

            if (actor == null || patient == null || actor.Map == null)
            {
                return false;
            }

            Point3D preferred = new Point3D(x, y, patient.Z);

            if (IsValidBandageStandPoint(actor, patient, preferred))
            {
                point = preferred;
                return true;
            }

            int averageZ = actor.Map.GetAverageZ(x, y);
            Point3D average = new Point3D(x, y, averageZ);

            if (averageZ != preferred.Z && IsValidBandageStandPoint(actor, patient, average))
            {
                point = average;
                return true;
            }

            Point3D currentZ = new Point3D(x, y, actor.Z);

            if (actor.Z != preferred.Z && actor.Z != averageZ && IsValidBandageStandPoint(actor, patient, currentZ))
            {
                point = currentZ;
                return true;
            }

            return false;
        }

        private static bool IsValidBandageStandPoint(BaseCreature actor, Mobile patient, Point3D point)
        {
            if (actor == null || patient == null || actor.Map == null || actor.Map != patient.Map ||
                !patient.InRange(point, Bandage.Range) ||
                !actor.Map.CanFit(point.X, point.Y, point.Z, 16, false, true, true, actor) ||
                !actor.Map.LineOfSight(point, patient.Location))
            {
                return false;
            }

            return !IsBandagePointTooDangerous(actor, patient, point);
        }

        private static bool IsBandagePointTooDangerous(BaseCreature actor, Mobile patient, Point3D point)
        {
            IPooledEnumerable eable = actor.Map.GetMobilesInRange(point, AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                double distance = GetDistance(point, mobile.Location);
                double patientDistance = Math.Max(0.5, patient.GetDistanceToSqrt(mobile));

                if (distance <= 1.5 ||
                    (mobile.Combatant == actor && distance <= AdventurePartySettings.BacklineThreatRange) ||
                    (mobile.Combatant != actor && distance <= AdventurePartySettings.BacklineMeleeThreatRange && distance + 0.25 < patientDistance))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private static double ScoreBandageStandPoint(BaseCreature actor, Mobile patient, Point3D point)
        {
            double score = actor.GetDistanceToSqrt(point) * 6.0;

            double patientDistance = patient.GetDistanceToSqrt(point);

            if (patientDistance < 1.5)
            {
                score += 8.0;
            }

            score += ScoreEnemyPressureAtPoint(actor, patient, point) * 4.0;

            return score;
        }

        private static bool TryStepTowardBandagePoint(BaseCreature actor, Mobile patient, Point3D standPoint)
        {
            List<Direction> directions = new List<Direction>(MovementDirections);

            directions.Sort(delegate(Direction left, Direction right)
            {
                double leftScore = ScoreBandageMoveDirection(actor, patient, standPoint, left);
                double rightScore = ScoreBandageMoveDirection(actor, patient, standPoint, right);

                return leftScore.CompareTo(rightScore);
            });

            for (int i = 0; i < directions.Count; i++)
            {
                Direction direction = directions[i];

                if (ScoreBandageMoveDirection(actor, patient, standPoint, direction) >= Double.MaxValue / 2.0)
                {
                    continue;
                }

                if (Move(actor, direction, true))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryStepTowardBandageStaging(BaseCreature actor, Mobile patient)
        {
            List<Direction> directions = new List<Direction>(MovementDirections);

            directions.Sort(delegate(Direction left, Direction right)
            {
                double leftScore = ScoreBandageStagingDirection(actor, patient, left);
                double rightScore = ScoreBandageStagingDirection(actor, patient, right);

                return leftScore.CompareTo(rightScore);
            });

            for (int i = 0; i < directions.Count; i++)
            {
                Direction direction = directions[i];

                if (ScoreBandageStagingDirection(actor, patient, direction) >= Double.MaxValue / 2.0)
                {
                    continue;
                }

                if (Move(actor, direction, true))
                {
                    return true;
                }
            }

            return false;
        }

        private static double ScoreBandageMoveDirection(BaseCreature actor, Mobile patient, Point3D standPoint, Direction direction)
        {
            Point3D next = GetPointInDirection(actor.Location, direction);

            if (GetDistance(next, patient.Location) > BandageApproachRange ||
                IsBandageMoveStepTooDangerous(actor, patient, next))
            {
                return Double.MaxValue;
            }

            double score = GetDistance(next, standPoint) * 10.0;
            score += ScoreEnemyPressureAtPoint(actor, patient, next) * 3.0;

            if (patient.InRange(next, Bandage.Range) && actor.Map.LineOfSight(next, patient.Location))
            {
                score -= 20.0;
            }

            if (GetDistance(next, patient.Location) > actor.GetDistanceToSqrt(patient) + 1.0)
            {
                score += 30.0;
            }

            return score;
        }

        private static double ScoreBandageStagingDirection(BaseCreature actor, Mobile patient, Direction direction)
        {
            Point3D next = GetPointInDirection(actor.Location, direction);
            double currentDistance = actor.GetDistanceToSqrt(patient);
            double nextDistance = GetDistance(next, patient.Location);

            if (nextDistance > BandageApproachRange ||
                nextDistance > currentDistance + 0.25 ||
                IsBandageMoveStepTooDangerous(actor, patient, next))
            {
                return Double.MaxValue;
            }

            if (nextDistance <= Bandage.Range && !actor.Map.LineOfSight(next, patient.Location))
            {
                return Double.MaxValue;
            }

            double score = nextDistance * 10.0;
            score += ScoreEnemyPressureAtPoint(actor, patient, next) * 3.0;

            if (nextDistance < currentDistance)
            {
                score -= 20.0;
            }

            if (actor.Map.LineOfSight(next, patient.Location))
            {
                score -= 6.0;
            }

            if (patient.InRange(next, Bandage.Range) && actor.Map.LineOfSight(next, patient.Location))
            {
                score -= 30.0;
            }

            return score;
        }

        private static bool IsBandageMoveStepTooDangerous(BaseCreature actor, Mobile patient, Point3D point)
        {
            IPooledEnumerable eable = actor.Map.GetMobilesInRange(point, AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                double distance = GetDistance(point, mobile.Location);
                double patientDistance = Math.Max(0.5, patient.GetDistanceToSqrt(mobile));

                if (distance <= 1.5 ||
                    (mobile.Combatant == actor && distance <= AdventurePartySettings.BacklineThreatRange) ||
                    (mobile.Combatant != actor && distance <= AdventurePartySettings.BacklineMeleeThreatRange && distance + 0.25 < patientDistance))
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private static double ScoreEnemyPressureAtPoint(BaseCreature actor, Mobile patient, Point3D point)
        {
            double score = 0.0;
            IPooledEnumerable eable = actor.Map.GetMobilesInRange(point, AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                double distance = Math.Max(0.5, GetDistance(point, mobile.Location));
                double patientDistance = Math.Max(0.5, patient.GetDistanceToSqrt(mobile));

                if (mobile.Combatant == actor)
                {
                    score += 80.0 / distance;
                }
                else if (distance < patientDistance)
                {
                    score += 35.0 * (patientDistance - distance);
                }
                else if (mobile.Combatant == patient)
                {
                    score += 12.0 / distance;
                }
                else
                {
                    score += 35.0 / distance;
                }
            }

            eable.Free();
            return score;
        }

        private static Point3D GetPointInDirection(Point3D from, Direction direction)
        {
            int x = from.X;
            int y = from.Y;

            switch (direction & Direction.Mask)
            {
                case Direction.North:
                    y--;
                    break;
                case Direction.Right:
                    x++;
                    y--;
                    break;
                case Direction.East:
                    x++;
                    break;
                case Direction.Down:
                    x++;
                    y++;
                    break;
                case Direction.South:
                    y++;
                    break;
                case Direction.Left:
                    x--;
                    y++;
                    break;
                case Direction.West:
                    x--;
                    break;
                case Direction.Up:
                    x--;
                    y--;
                    break;
            }

            return new Point3D(x, y, from.Z);
        }

        private static Bandage FindBandage(BaseCreature actor)
        {
            Container pack = actor == null ? null : actor.Backpack;

            if (pack == null)
            {
                return null;
            }

            return pack.FindItemByType(typeof(Bandage), true) as Bandage;
        }

        private static bool TryUseEmergencySpiritSpeak(BaseCreature actor, Mobile patient, BrainState state, long now, AdvancedCombatContext context)
        {
            if (now < state.NextSpiritSpeak)
            {
                return false;
            }

            if (patient == actor && actor.Skills[SkillName.SpiritSpeak].Value >= 50.0 &&
                actor.Hits < actor.HitsMax * 55 / 100)
            {
                if (ShouldAvoidSpiritSpeakFreeze(actor, context))
                {
                    RecordAction(state, "skipped Spirit Speak to keep moving under pressure.");
                    return false;
                }

                actor.UseSkill(SkillName.SpiritSpeak);
                state.NextSpiritSpeak = now + 6000;
                RecordAction(state, String.Format("used Spirit Speak emergency heal on {0}.", GetDebugName(patient)));
                return true;
            }

            return false;
        }

        private static bool ShouldAvoidSpiritSpeakFreeze(BaseCreature actor, AdvancedCombatContext context)
        {
            if (actor == null)
            {
                return true;
            }

            int directThreats = CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange);
            int adjacentThreats = CountEnemiesInRange(actor, AdventurePartySettings.BacklineMeleeThreatRange);

            if (context != null &&
                (context.State == AdventurePartyState.Retreating ||
                 context.IsTactic(AdventurePartyTactic.Recovering) ||
                 context.IsTactic(AdventurePartyTactic.PullToChokePoint) ||
                 context.IsTactic(AdventurePartyTactic.Kite) ||
                 context.IsTactic(AdventurePartyTactic.AvoidAOE)))
            {
                return directThreats > 0 || adjacentThreats > 0;
            }

            return directThreats >= 2 || adjacentThreats >= 2;
        }

        private static bool IsUnderHeavyPressure(BaseCreature actor, Mobile patient)
        {
            if (actor == null || patient == null || patient.HitsMax <= 0)
            {
                return false;
            }

            bool fighterPatient = IsFighterPatient(patient);
            IPooledEnumerable eable = patient.GetMobilesInRange(fighterPatient ? 6 : 3);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                if (mobile.Combatant == patient)
                {
                    eable.Free();
                    return true;
                }

                if (fighterPatient && mobile.Combatant != actor && patient.GetDistanceToSqrt(mobile) <= 4.0)
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        private static Mobile SelectUrgentFighterProvocationTarget(BaseCreature actor, Mobile fallback)
        {
            BaseAdventurer archer = actor as BaseAdventurer;

            if (archer == null || archer.Role != AdventurePartyRole.Archer || archer.Controller == null ||
                actor.Skills[SkillName.Provocation].Value < 70.0 || actor.Skills[SkillName.Musicianship].Value < 70.0)
            {
                return null;
            }

            BaseAdventurer fighter = null;
            double fighterDistance = Double.MaxValue;
            IPooledEnumerable allies = actor.GetMobilesInRange(AdventurePartySettings.PerceptionRange);

            foreach (Mobile mobile in allies)
            {
                BaseAdventurer adventurer = mobile as BaseAdventurer;

                if (adventurer == null || adventurer == archer || adventurer.Controller != archer.Controller ||
                    adventurer.Role != AdventurePartyRole.Fighter || adventurer.Deleted || !adventurer.Alive ||
                    adventurer.Map != actor.Map)
                {
                    continue;
                }

                double distance = actor.GetDistanceToSqrt(adventurer);

                if (fighter == null || distance < fighterDistance)
                {
                    fighter = adventurer;
                    fighterDistance = distance;
                }
            }

            allies.Free();

            if (fighter == null)
            {
                return null;
            }

            int directThreats = 0;
            int bardRange = BaseInstrument.GetBardRange(actor, SkillName.Provocation);
            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable enemies = fighter.GetMobilesInRange(AdventurePartySettings.PullBreakPursuitScanRange);

            foreach (Mobile mobile in enemies)
            {
                BaseCreature creature = mobile as BaseCreature;

                if (creature == null || !IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                double distanceToFighter = fighter.GetDistanceToSqrt(mobile);

                if (mobile.Combatant == fighter)
                {
                    directThreats++;
                }

                if (!actor.InRange(mobile, bardRange) || !actor.InLOS(mobile))
                {
                    continue;
                }

                double score = 30.0 - distanceToFighter;

                if (mobile.Combatant == fighter)
                {
                    score += 20.0;
                }

                if (mobile == fallback)
                {
                    score += 5.0;
                }

                if (best == null || score > bestScore)
                {
                    best = mobile;
                    bestScore = score;
                }
            }

            enemies.Free();

            if (directThreats < AdventurePartySettings.FighterPressureBreakPursuitDirectThreats)
            {
                return null;
            }

            return best;
        }

        private static bool TryProvoke(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context, bool urgent)
        {
            long now = Core.TickCount;
            BaseCreature first = enemy as BaseCreature;

            if (actor == null || first == null || state == null || now < state.NextBard ||
                IsTactic(context, AdventurePartyTactic.FocusFire) || IsTactic(context, AdventurePartyTactic.PowerUp) ||
                actor.Skills[SkillName.Provocation].Value < 70.0 || actor.Skills[SkillName.Musicianship].Value < 70.0)
            {
                return false;
            }

            int bardRange = BaseInstrument.GetBardRange(actor, SkillName.Provocation);

            if (!urgent && CountEnemiesNearPoint(actor, enemy.Location, bardRange) < 2)
            {
                return false;
            }

            if (!actor.InRange(enemy, bardRange) || !actor.InLOS(enemy))
            {
                return false;
            }

            if (actor.GetSecondTarget(first) == null)
            {
                return false;
            }

            actor.Combatant = enemy;
            bool used = actor.DoProvoke();
            state.NextBard = now + (urgent ? Utility.RandomMinMax(6500, 9000) : Utility.RandomMinMax(9000, 13000));

            if (used)
            {
                RecordAction(state, String.Format("{0} Provocation around {1}.", urgent ? "used urgent" : "used", GetDebugName(enemy)));
            }

            return used;
        }

        private static bool TryDiscord(BaseCreature actor, Mobile enemy, BrainState state)
        {
            long now = Core.TickCount;

            if (enemy == null || now < state.NextBard || Discordance.UnderEffects(enemy) ||
                actor.Skills[SkillName.Discordance].Value < 60.0 || actor.Skills[SkillName.Musicianship].Value < 60.0)
            {
                return false;
            }

            if (!actor.InRange(enemy, BaseInstrument.GetBardRange(actor, SkillName.Discordance)) || !actor.InLOS(enemy))
            {
                return false;
            }

            actor.Combatant = enemy;
            bool used = actor.DoDiscord();
            state.NextBard = now + Utility.RandomMinMax(8000, 13000);
            return used;
        }

        private static bool TryUseSampireMastery(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;
            bool report = ShouldReportMasteryDebug(context);

            if (now < state.NextMastery)
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: mastery cooldown.", false, report);
                return false;
            }

            if (!ShouldUseOffensiveMastery(actor, enemy, context))
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: no burst condition.", false, report);
                return false;
            }

            if (actor.Mana < 20)
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: mana below 20.", false, report);
                return false;
            }

            if (actor.Skills[SkillName.Swords].Value < 90.0 || actor.Skills[SkillName.Tactics].Value < 80.0)
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: Swords or Tactics too low.", false, report);
                return false;
            }

            if (WeaponAbility.GetCurrentAbility(actor) != null || SpecialMove.GetCurrentMove(actor) != null)
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: another weapon ability is already queued.", false, report);
                return false;
            }

            if (!EnsureMastery(actor, SkillName.Swords, 3))
            {
                DebugMastery(state, actor, "skipped Swords Mastery Onslaught: Swords mastery unavailable.", false, report);
                return false;
            }

            if (SpecialMove.SetCurrentMove(actor, new OnslaughtSpell()))
            {
                state.NextMastery = now + 18000;
                DebugMastery(state, actor, String.Format("used Swords Mastery Onslaught on {0}.", GetDebugName(enemy)), true, true);
                return true;
            }

            state.NextMastery = now + 5000;
            DebugMastery(state, actor, "failed to ready Swords Mastery Onslaught.", true, true);
            return false;
        }

        private static bool TryUseArcherMastery(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;
            bool report = ShouldReportMasteryDebug(context);

            if (now < state.NextMastery)
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: mastery cooldown.", false, report);
                return false;
            }

            if (!ShouldUseOffensiveMastery(actor, enemy, context))
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: no burst condition.", false, report);
                return false;
            }

            if (actor.Mana < 25)
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: mana below 25.", false, report);
                return false;
            }

            if (actor.Skills[SkillName.Archery].Value < 90.0 || actor.Skills[SkillName.Tactics].Value < 80.0)
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: Archery or Tactics too low.", false, report);
                return false;
            }

            if (SkillMasterySpell.UnderPartyEffects(actor, typeof(PlayingTheOddsSpell)))
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: effect already active.", false, report);
                return false;
            }

            if (!EnsureMastery(actor, SkillName.Archery, 3))
            {
                DebugMastery(state, actor, "skipped Archery Mastery Playing the Odds: Archery mastery unavailable.", false, report);
                return false;
            }

            if (TryCast(actor, new PlayingTheOddsSpell(actor, null), null, state))
            {
                state.NextMastery = now + 95000;
                state.NextSpell = now + 3500;
                DebugMastery(state, actor, String.Format("used Archery Mastery Playing the Odds near {0}.", GetDebugName(enemy)), true, true);
                return true;
            }

            state.NextMastery = now + 5000;
            DebugMastery(state, actor, "failed to cast Archery Mastery Playing the Odds.", true, true);
            return false;
        }

        private static bool TryUseMageDeathRay(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;
            bool report = ShouldReportMasteryDebug(context);

            if (now < state.NextMastery)
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: mastery cooldown.", false, report);
                return false;
            }

            if (!ShouldUseDeathRay(actor, enemy, context))
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: no safe burst window.", false, report);
                return false;
            }

            if (actor.Mana < 85)
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: mana below 85.", false, report);
                return false;
            }

            if (actor.Skills[SkillName.Magery].Value < 90.0 || actor.Skills[SkillName.EvalInt].Value < 90.0)
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: Magery or EvalInt too low.", false, report);
                return false;
            }

            if (!actor.InRange(enemy, 10) || !actor.InLOS(enemy))
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: target out of range or line of sight.", false, report);
                return false;
            }

            if (SkillMasterySpell.GetSpell(actor, typeof(DeathRaySpell)) != null)
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: already channeling.", false, report);
                return false;
            }

            if (!EnsureMastery(actor, SkillName.Magery, 3))
            {
                DebugMastery(state, actor, "skipped Magery Mastery Death Ray: Magery mastery unavailable.", false, report);
                return false;
            }

            if (TryCast(actor, new DeathRaySpell(actor, null), enemy, state))
            {
                state.NextMastery = now + 30000;
                state.NextSpell = now + 4500;
                DebugMastery(state, actor, String.Format("used Magery Mastery Death Ray on {0}.", GetDebugName(enemy)), true, true);
                return true;
            }

            state.NextMastery = now + 10000;
            DebugMastery(state, actor, "failed to cast Magery Mastery Death Ray.", true, true);
            return false;
        }

        private static bool TryUseMageDefensiveMastery(BaseCreature actor, Mobile enemy, BrainState state, AdvancedCombatContext context)
        {
            long now = Core.TickCount;
            bool report = ShouldReportMasteryDebug(context);

            if (now < state.NextMastery)
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: mastery cooldown.", false, report);
                return false;
            }

            if (!ShouldUseDefensiveMastery(actor, enemy, context))
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: no defensive condition.", false, report);
                return false;
            }

            if (actor.Mana < 40)
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: mana below 40.", false, report);
                return false;
            }

            if (actor.Skills[SkillName.Spellweaving].Value < 90.0 || actor.Skills[SkillName.Meditation].Value < 70.0)
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: Spellweaving or Meditation too low.", false, report);
                return false;
            }

            if (SkillMasterySpell.GetSpell(actor, typeof(ManaShieldSpell)) != null)
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: effect already active.", false, report);
                return false;
            }

            if (!EnsureMastery(actor, SkillName.Spellweaving, 3))
            {
                DebugMastery(state, actor, "skipped Spellweaving Mastery Mana Shield: Spellweaving mastery unavailable.", false, report);
                return false;
            }

            if (TryCast(actor, new ManaShieldSpell(actor, null), null, state))
            {
                state.NextMastery = now + 610000;
                state.NextSpell = now + 3500;
                DebugMastery(state, actor, String.Format("used Spellweaving Mastery Mana Shield near {0}.", GetDebugName(enemy)), true, true);
                return true;
            }

            state.NextMastery = now + 10000;
            DebugMastery(state, actor, "failed to cast Spellweaving Mastery Mana Shield.", true, true);
            return false;
        }

        private static void DebugMastery(BrainState state, BaseCreature actor, string message, bool force, bool allowed)
        {
            if (!allowed || state == null)
            {
                return;
            }

            long now = Core.TickCount;
            state.LastMastery = message;
            state.LastMasteryTime = now;

            if (!AdventurePartyDebug.MasteryDebugEnabled)
            {
                return;
            }

            if (!force && now < state.NextMasteryDebug)
            {
                return;
            }

            state.NextMasteryDebug = now + 10000;
            AdventurePartyDebug.Mastery(actor, message);
        }

        private static bool ShouldReportMasteryDebug(AdvancedCombatContext context)
        {
            return IsTactic(context, AdventurePartyTactic.PowerUp) ||
                IsTactic(context, AdventurePartyTactic.FocusFire) ||
                IsChokeTactic(context) ||
                IsTactic(context, AdventurePartyTactic.Kite);
        }

        private static string GetDebugName(Mobile mobile)
        {
            if (mobile == null)
            {
                return "no target";
            }

            return String.IsNullOrEmpty(mobile.Name) ? mobile.GetType().Name : mobile.Name;
        }

        public static string GetHealerDebugState(BaseCreature actor)
        {
            if (actor == null || actor.Deleted)
            {
                return "local brain: healer is missing or deleted.";
            }

            BrainState state = GetState(actor);
            long now = Core.TickCount;
            Mobile patient = FindEmergencyHealPatient(actor, AdvancedCombatProfile.BardHealer, 10);
            Bandage bandage = FindBandage(actor);
            BandageContext bandageContext = BandageContext.GetContext(actor);
            Mobile meleeThreat = FindImmediateMeleeThreat(actor, null);
            string bandagePatient = bandageContext == null ? "none" : GetDebugName(bandageContext.Patient);

            if (patient == null)
            {
                return String.Format(
                    "local brain: no urgent healing patient; bandages={0}, bandageContext={1}, bandagePatient={2}, nextBandage={3}, meleeThreat={4}, lastAction={5}.",
                    bandage == null ? 0 : bandage.Amount,
                    bandageContext != null,
                    bandagePatient,
                    FormatDebugDelay(state.NextBandage, now),
                    GetDebugName(meleeThreat),
                    FormatRecentDebug(state.LastAction, state.LastActionTime, now, RecentActionDebugLifetime));
            }

            bool heavyPressure = IsUnderHeavyPressure(actor, patient);
            bool urgent = IsEmergencyHealUrgent(patient, AdvancedCombatProfile.BardHealer, heavyPressure);
            int bandageThreshold = GetBandageStartThreshold(patient, heavyPressure);
            bool inRange = actor.InRange(patient, Bandage.Range);
            bool lineOfSight = actor.InLOS(patient);
            bool canBeneficial = actor.CanBeBeneficial(patient, false, true);

            return String.Format(
                "local brain: patient={0}, hp={1}/{2} ({3}%), urgent={4}, pressure={5}, bandageThreshold={6}%, inRange={7}, los={8}, canBenefit={9}, bandages={10}, bandageContext={11}, bandagePatient={12}, nextBandage={13}, nextSpell={14}, meleeThreat={15}, lastAction={16}.",
                GetDebugName(patient),
                patient.Hits,
                patient.HitsMax,
                patient.HitsMax <= 0 ? 0 : (patient.Hits * 100) / patient.HitsMax,
                urgent,
                heavyPressure,
                bandageThreshold,
                inRange,
                lineOfSight,
                canBeneficial,
                bandage == null ? 0 : bandage.Amount,
                bandageContext != null,
                bandagePatient,
                FormatDebugDelay(state.NextBandage, now),
                FormatDebugDelay(state.NextSpell, now),
                GetDebugName(meleeThreat),
                FormatRecentDebug(state.LastAction, state.LastActionTime, now, RecentActionDebugLifetime));
        }

        public static string GetCombatDebugState(BaseCreature actor)
        {
            if (actor == null || actor.Deleted)
            {
                return "brain=missing";
            }

            BrainState state = GetState(actor);
            long now = Core.TickCount;
            string currentMastery = actor.Skills == null ? "none" : actor.Skills.CurrentMastery.ToString();
            BaseWeapon weapon = GetEquippedWeapon(actor);

            return String.Format(
                "weapon={0};currentMastery={1};nextSpell={2};nextMastery={3};nextWeaponSwap={4};lastSpell={5};lastAction={6};lastMastery={7};skills=magery:{8:0.0},eval:{9:0.0},necro:{10:0.0},spw:{11:0.0}",
                weapon == null ? "none" : weapon.GetType().Name,
                currentMastery,
                FormatDebugDelay(state.NextSpell, now),
                FormatDebugDelay(state.NextMastery, now),
                FormatDebugDelay(state.NextWeaponSwap, now),
                FormatRecentDebug(state.LastSpell, state.LastSpellTime, now, RecentSpellDebugLifetime),
                FormatRecentDebug(state.LastAction, state.LastActionTime, now, RecentActionDebugLifetime),
                FormatRecentDebug(state.LastMastery, state.LastMasteryTime, now, RecentActionDebugLifetime),
                GetSkillValue(actor, SkillName.Magery),
                GetSkillValue(actor, SkillName.EvalInt),
                GetSkillValue(actor, SkillName.Necromancy),
                GetSkillValue(actor, SkillName.Spellweaving));
        }

        private static double GetSkillValue(BaseCreature actor, SkillName skill)
        {
            return actor == null || actor.Skills == null ? 0.0 : actor.Skills[skill].Value;
        }

        private static string FormatRecentDebug(string text, long at, long now, int lifetime)
        {
            if (String.IsNullOrEmpty(text) || at <= 0)
            {
                return "none";
            }

            long age = Math.Max(0, now - at);

            if (lifetime > 0 && age > lifetime)
            {
                return "none";
            }

            return String.Format("{0}@{1:0.0}s", text.Replace(' ', '_'), age / 1000.0);
        }

        private static string FormatDebugDelay(long due, long now)
        {
            if (due <= now)
            {
                return "ready";
            }

            return String.Format("{0:0.0}s", (due - now) / 1000.0);
        }

        private static bool EnsureMastery(BaseCreature actor, SkillName skillName, int volume)
        {
            if (actor == null || actor.Skills == null)
            {
                return false;
            }

            Skill skill = actor.Skills[skillName];

            if (skill == null || !skill.IsMastery)
            {
                return false;
            }

            if (skill.VolumeLearned < volume)
            {
                skill.LearnMastery(volume);
            }

            if (skill.VolumeLearned < volume)
            {
                return false;
            }

            actor.Skills.CurrentMastery = skillName;
            return true;
        }

        private static bool ShouldUseOffensiveMastery(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (!IsValidEnemy(actor, enemy))
            {
                return false;
            }

            if (IsTactic(context, AdventurePartyTactic.PowerUp))
            {
                return true;
            }

            if (IsTactic(context, AdventurePartyTactic.FocusFire) && IsHardTarget(enemy))
            {
                return true;
            }

            if (IsChokeTactic(context) && CountEnemiesInRange(actor, 8) >= 2)
            {
                return true;
            }

            return IsHardTarget(enemy) && actor.HitsMax > 0 && actor.Hits > actor.HitsMax * 50 / 100;
        }

        private static bool ShouldUseDefensiveMastery(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (actor == null || enemy == null || !IsValidEnemy(actor, enemy))
            {
                return false;
            }

            bool directPressure = CountEnemiesTargeting(actor, AdventurePartySettings.BacklineThreatRange) > 0 ||
                CountEnemiesInRange(actor, AdventurePartySettings.BacklineMeleeThreatRange) > 0;

            if (IsTactic(context, AdventurePartyTactic.PowerUp) || IsTactic(context, AdventurePartyTactic.FocusFire))
            {
                return actor.HitsMax > 0 && actor.Hits < actor.HitsMax * 50 / 100 && directPressure;
            }

            if (actor.HitsMax > 0 && actor.Hits < actor.HitsMax * 60 / 100)
            {
                return true;
            }

            if (actor.HitsMax > 0 && actor.Hits < actor.HitsMax * 85 / 100 && directPressure)
            {
                return true;
            }

            return directPressure && IsHardTarget(enemy) && actor.HitsMax > 0 && actor.Hits < actor.HitsMax;
        }

        private static bool ShouldUseDeathRay(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (!IsValidEnemy(actor, enemy))
            {
                return false;
            }

            if (!IsTactic(context, AdventurePartyTactic.PowerUp) && !IsTactic(context, AdventurePartyTactic.FocusFire))
            {
                return false;
            }

            if (actor.HitsMax > 0 && actor.Hits < actor.HitsMax * 70 / 100)
            {
                return false;
            }

            if (enemy.Combatant == actor || CountEnemiesInRange(actor, 3) > 0)
            {
                return false;
            }

            if (CountEnemiesNearPoint(actor, enemy.Location, 8) > 1)
            {
                return false;
            }

            return IsHardTarget(enemy) || enemy.HitsMax >= actor.HitsMax;
        }

        private static bool ShouldUseEnemyOfOne(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (!IsValidEnemy(actor, enemy) || HasMixedEnemyTypes(actor, enemy, EnemyOfOneScanRange))
            {
                return false;
            }

            if (IsTactic(context, AdventurePartyTactic.PowerUp) || IsTactic(context, AdventurePartyTactic.FocusFire))
            {
                return true;
            }

            return IsHardTarget(enemy) || CountEnemiesInRange(actor, EnemyOfOneScanRange) <= 1;
        }

        private static bool ShouldUseDivineFury(BaseCreature actor, Mobile enemy, AdvancedCombatContext context)
        {
            if (actor == null || enemy == null || actor.Skills[SkillName.Chivalry].Value < 25.0 ||
                actor.Mana < 15 || DivineFurySpell.UnderEffect(actor) ||
                actor.HitsMax <= 0 || actor.Hits < actor.HitsMax * 70 / 100)
            {
                return false;
            }

            if (CountEnemiesInRange(actor, 3) >= 3 && !IsTactic(context, AdventurePartyTactic.PowerUp))
            {
                return false;
            }

            return IsTactic(context, AdventurePartyTactic.PowerUp) ||
                IsTactic(context, AdventurePartyTactic.FocusFire) ||
                actor.Stam < actor.StamMax * 70 / 100;
        }

        private static bool HasMixedEnemyTypes(BaseCreature actor, Mobile primary, int range)
        {
            if (actor == null || primary == null)
            {
                return false;
            }

            Type primaryType = primary.GetType();
            IPooledEnumerable eable = actor.GetMobilesInRange(range);

            foreach (Mobile mobile in eable)
            {
                if (mobile != primary && IsValidEnemy(actor, mobile) && mobile.GetType() != primaryType)
                {
                    eable.Free();
                    return true;
                }
            }

            eable.Free();
            return false;
        }

        public static bool IsChannelingDeathRay(Mobile actor)
        {
            return actor != null && SkillMasterySpell.GetSpell(actor, typeof(DeathRaySpell)) != null;
        }

        public static bool IsPlayingTheOddsActive(Mobile actor)
        {
            return actor != null && SkillMasterySpell.UnderPartyEffects(actor, typeof(PlayingTheOddsSpell));
        }

        private static bool IsHardTarget(Mobile enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            int hitsMax = Math.Max(enemy.HitsMax, enemy.Hits);
            BaseCreature creature = enemy as BaseCreature;

            return hitsMax >= 220 || (creature != null && creature.Fame >= 8000);
        }

        private static bool TryCast(BaseCreature actor, Spell spell, Mobile target, BrainState state)
        {
            if (actor.Spell != null || spell == null)
            {
                return false;
            }

            if (target != null && (!target.Alive || target.Deleted || target.Map != actor.Map))
            {
                return false;
            }

            if (!spell.Cast())
            {
                return false;
            }

            RecordSpell(state, spell);

            if (target != null)
            {
                state.PendingTarget = target;
                state.PendingTargetExpires = Core.TickCount + 5000;
            }

            return true;
        }

        private static bool TrySetWeaponAbility(BaseCreature actor, params WeaponAbility[] abilities)
        {
            if (actor == null || actor.Weapon == null || WeaponAbility.GetCurrentAbility(actor) != null)
            {
                return false;
            }

            for (int i = 0; i < abilities.Length; i++)
            {
                WeaponAbility ability = abilities[i];

                if (ability != null && WeaponAbility.SetCurrentAbility(actor, ability))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool AllowsOffense(BaseCreature actor, AdvancedCombatContext context)
        {
            if (context == null)
            {
                return true;
            }

            if (ShouldSuppressBacklineOffense(actor, context))
            {
                return false;
            }

            if (context.State == AdventurePartyState.Retreating || context.State == AdventurePartyState.Wiped)
            {
                return false;
            }

            if (context.IsTactic(AdventurePartyTactic.Recovering))
            {
                return false;
            }

            if (context.State == AdventurePartyState.Resting && !IsChokeTactic(context))
            {
                return false;
            }

            return true;
        }

        private static bool ApplyTacticalPositioning(BaseCreature actor, AdvancedCombatProfile profile, Mobile enemy, AdvancedCombatContext context)
        {
            if (actor == null || context == null || context.IsExpired)
            {
                return false;
            }

            if (ShouldSuppressBacklineOffense(actor, context))
            {
                actor.Combatant = null;
                actor.Warmode = false;
                return true;
            }

            if (context.Tactic == AdventurePartyTactic.AvoidAOE)
            {
                if (enemy != null && actor.InRange(enemy, 8))
                {
                    StepAway(actor, enemy);
                }

                return true;
            }

            if (IsChokeTactic(context) && context.Anchor != Point3D.Zero &&
                !IsSafeFleeingTargetFinish(actor, context) &&
                GetDistance(context.Anchor, actor.Location) > 8.0)
            {
                return true;
            }

            if (context.Tactic == AdventurePartyTactic.PullToChokePoint && context.Anchor != Point3D.Zero &&
                !IsSafeFleeingTargetFinish(actor, context) &&
                enemy != null && actor.Combatant != enemy &&
                GetDistance(context.Anchor, enemy.Location) > AdventurePartySettings.PullChokeKillZoneRange)
            {
                return true;
            }

            return false;
        }

        private static bool ShouldSuppressBacklineOffense(BaseCreature actor, AdvancedCombatContext context)
        {
            if (actor == null || context == null || context.IsExpired)
            {
                return false;
            }

            BaseAdventurer adventurer = actor as BaseAdventurer;

            if (adventurer == null || adventurer.Role == AdventurePartyRole.Fighter)
            {
                return false;
            }

            if (IsSafeFleeingTargetFinish(actor, context))
            {
                return false;
            }

            if (context.State == AdventurePartyState.Retreating || context.State == AdventurePartyState.Wiped ||
                context.IsTactic(AdventurePartyTactic.Recovering))
            {
                return true;
            }

            if (!context.IsTactic(AdventurePartyTactic.PullToChokePoint))
            {
                return false;
            }

            Mobile target = context.Target;

            if (target == null || target.Deleted || target.Map != actor.Map)
            {
                return true;
            }

            return actor.Combatant != target;
        }

        private static Mobile SelectEnemy(BaseCreature actor, AdvancedCombatProfile profile, AdvancedCombatContext context)
        {
            Mobile contextTarget = GetContextTarget(actor, context);

            if (contextTarget != null)
            {
                return contextTarget;
            }

            Mobile current = actor.Combatant as Mobile;

            if (IsValidEnemy(actor, current))
            {
                return current;
            }

            Mobile best = null;
            double bestScore = Double.MinValue;

            IPooledEnumerable eable = actor.GetMobilesInRange(actor.RangePerception);

            foreach (Mobile m in eable)
            {
                if (!IsValidEnemy(actor, m))
                {
                    continue;
                }

                double score = ScoreEnemy(actor, m, profile, context);

                if (score > bestScore)
                {
                    best = m;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private static Mobile GetContextTarget(BaseCreature actor, AdvancedCombatContext context)
        {
            if (actor == null || context == null || context.IsExpired || !IsValidEnemy(actor, context.Target))
            {
                return null;
            }

            if (context.IsTactic(AdventurePartyTactic.HoldChokePoint) && context.Anchor != Point3D.Zero &&
                GetDistance(context.Anchor, context.Target.Location) > 14.0)
            {
                return null;
            }

            if (context.IsTactic(AdventurePartyTactic.PullToChokePoint) && context.Anchor != Point3D.Zero &&
                !IsSafeFleeingTargetFinish(actor, context) &&
                GetDistance(context.Anchor, context.Target.Location) > AdventurePartySettings.PullChokeKillZoneRange)
            {
                return null;
            }

            return context.Target;
        }

        private static bool IsSafeFleeingTargetFinish(BaseCreature actor, AdvancedCombatContext context)
        {
            if (actor == null || context == null || context.IsExpired ||
                !context.IsTactic(AdventurePartyTactic.PullToChokePoint))
            {
                return false;
            }

            Mobile target = context.Target;

            if (!IsValidEnemy(actor, target) || target.HitsMax <= 0 ||
                target.Hits * 100 > target.HitsMax * AdventurePartySettings.FleeingTargetFinishHitPercent)
            {
                return false;
            }

            if (actor.GetDistanceToSqrt(target) > AdventurePartySettings.FleeingTargetFinishMaxDistance)
            {
                return false;
            }

            IPooledEnumerable eable = actor.GetMobilesInRange(AdventurePartySettings.FleeingTargetFinishSafeRange);

            foreach (Mobile mobile in eable)
            {
                if (mobile != target && IsValidEnemy(actor, mobile))
                {
                    eable.Free();
                    return false;
                }
            }

            eable.Free();
            return true;
        }

        private static bool IsChokeTactic(AdvancedCombatContext context)
        {
            return IsTactic(context, AdventurePartyTactic.HoldChokePoint) || IsTactic(context, AdventurePartyTactic.PullToChokePoint);
        }

        private static bool IsTactic(AdvancedCombatContext context, AdventurePartyTactic tactic)
        {
            return context != null && context.IsTactic(tactic);
        }

        private static double ScoreEnemy(BaseCreature actor, Mobile target, AdvancedCombatProfile profile, AdvancedCombatContext context)
        {
            double distance = actor.GetDistanceToSqrt(target);
            double wounded = target.HitsMax > 0 ? 1.0 - ((double)target.Hits / target.HitsMax) : 0.0;
            double score = wounded * 40.0 - distance;

            if (context != null && context.IsTactic(AdventurePartyTactic.FocusFire))
            {
                score += wounded * 30.0;
            }

            if (target.Skills[SkillName.Magery].Value > 60.0 || target.Skills[SkillName.EvalInt].Value > 60.0)
            {
                score += profile == AdvancedCombatProfile.DiscoProvoArcher || profile == AdvancedCombatProfile.NecroMageWeaver ? 20.0 : 10.0;
            }

            if (target.Combatant == actor)
            {
                score += 8.0;
            }

            if (profile == AdvancedCombatProfile.BardHealer && target.Combatant is BaseAdventurer)
            {
                BaseAdventurer threatened = (BaseAdventurer)target.Combatant;

                if (threatened.Role == Server.Engines.AdventureParty.AdventurePartyRole.Healer)
                {
                    score += 25.0;
                }
            }

            return score;
        }

        private static double GetDistance(Point3D a, Point3D b)
        {
            int dx = a.X - b.X;
            int dy = a.Y - b.Y;

            return Math.Sqrt((dx * dx) + (dy * dy));
        }

        private static bool IsValidEnemy(BaseCreature actor, Mobile target)
        {
            if (actor == null || target == null || target == actor || target.Deleted || !target.Alive || target.Map != actor.Map)
            {
                return false;
            }

            if (target.Player || target is PlayerMobile)
            {
                return false;
            }

            BaseCreature bc = target as BaseCreature;

            if (bc != null && (bc.Controlled || bc.Summoned || bc.Team == actor.Team))
            {
                return false;
            }

            return actor.IsEnemy(target) && actor.CanBeHarmful(target, false);
        }

        private static bool TryEscapeImmediateMeleeThreat(BaseCreature actor, AdvancedCombatProfile profile, Mobile contextTarget, BrainState state, AdvancedCombatContext context)
        {
            if (actor == null || profile == AdvancedCombatProfile.SampireFighter)
            {
                return false;
            }

            Mobile threat = FindImmediateMeleeThreat(actor, contextTarget);

            if (threat == null)
            {
                return false;
            }

            bool suppressOffense = ShouldSuppressBacklineOffense(actor, context);

            if (suppressOffense)
            {
                actor.Combatant = null;
                actor.Warmode = false;
            }
            else
            {
                actor.Combatant = threat;
            }

            if (StepAway(actor, threat))
            {
                RecordAction(state, String.Format(
                    suppressOffense ? "escaping melee threat from {0} without re-engaging." : "escaping melee threat from {0}.",
                    GetDebugName(threat)));
            }

            return true;
        }

        private static Mobile FindImmediateMeleeThreat(BaseCreature actor, Mobile contextTarget)
        {
            Mobile best = null;
            double bestScore = Double.MinValue;
            IPooledEnumerable eable = actor.GetMobilesInRange(AdventurePartySettings.BacklineThreatRange);

            foreach (Mobile mobile in eable)
            {
                if (!IsValidEnemy(actor, mobile))
                {
                    continue;
                }

                double distance = Math.Max(0.5, actor.GetDistanceToSqrt(mobile));

                if (mobile.Combatant != actor && distance > AdventurePartySettings.BacklineMeleeThreatRange)
                {
                    continue;
                }

                double score = 30.0 - distance;

                if (mobile.Combatant == actor)
                {
                    score += 12.0;
                }

                if (mobile == contextTarget)
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

        private static Mobile FindMostInjuredAlly(BaseCreature actor, int range)
        {
            Mobile best = null;
            double bestScore = 0.0;

            if (actor.HitsMax > 0 && (actor.Hits < actor.HitsMax || actor.Poisoned))
            {
                best = actor;
                bestScore = InjuryScore(actor);
            }

            IPooledEnumerable eable = actor.GetMobilesInRange(range);

            foreach (Mobile m in eable)
            {
                BaseCreature bc = m as BaseCreature;

                if (bc == null || bc == actor || bc.Deleted || !bc.Alive || bc.Map != actor.Map || bc.Team != actor.Team)
                {
                    continue;
                }

                if (!actor.CanBeBeneficial(bc, false, true))
                {
                    continue;
                }

                double score = InjuryScore(bc);

                if (score > bestScore)
                {
                    best = bc;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private static Mobile FindEmergencyHealPatient(BaseCreature actor, AdvancedCombatProfile profile, int range)
        {
            if (profile != AdvancedCombatProfile.BardHealer)
            {
                return FindMostInjuredAlly(actor, range);
            }

            Mobile best = null;
            double bestScore = 0.0;

            if (actor.HitsMax > 0)
            {
                bestScore = ScoreEmergencyHealPatient(actor, actor, profile);

                if (bestScore > 0.0)
                {
                    best = actor;
                }
            }

            IPooledEnumerable eable = actor.GetMobilesInRange(range);

            foreach (Mobile m in eable)
            {
                BaseCreature bc = m as BaseCreature;

                if (bc == null || bc == actor || bc.Deleted || !bc.Alive || bc.Map != actor.Map || bc.Team != actor.Team)
                {
                    continue;
                }

                if (!actor.CanBeBeneficial(bc, false, true))
                {
                    continue;
                }

                double score = ScoreEmergencyHealPatient(actor, bc, profile);

                if (score > bestScore)
                {
                    best = bc;
                    bestScore = score;
                }
            }

            eable.Free();
            return best;
        }

        private static double ScoreEmergencyHealPatient(BaseCreature actor, Mobile patient, AdvancedCombatProfile profile)
        {
            if (actor == null || patient == null || patient.HitsMax <= 0)
            {
                return 0.0;
            }

            bool heavyPressure = IsUnderHeavyPressure(actor, patient);

            if (!IsEmergencyHealUrgent(patient, profile, heavyPressure))
            {
                return 0.0;
            }

            double score = InjuryScore(patient);

            if (heavyPressure)
            {
                score += 0.35;
            }

            if (IsFighterPatient(patient))
            {
                score += 0.45;

                if (heavyPressure)
                {
                    score += 0.35;
                }

                if (actor.InRange(patient, BandageApproachRange))
                {
                    score += 0.15;
                }
            }
            else if (patient == actor)
            {
                score += patient.Hits < patient.HitsMax * 40 / 100 ? 0.85 : 0.15;
            }

            return score;
        }

        private static double InjuryScore(Mobile m)
        {
            if (m == null || m.HitsMax <= 0)
            {
                return 0.0;
            }

            double missing = 1.0 - ((double)m.Hits / m.HitsMax);
            return missing + (m.Poisoned ? 0.35 : 0.0);
        }

        private static int CountEnemiesNearPoint(BaseCreature actor, Point3D point, int range)
        {
            if (actor == null || actor.Map == null || actor.Map == Map.Internal)
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = actor.Map.GetMobilesInRange(point, range);

            foreach (Mobile m in eable)
            {
                if (IsValidEnemy(actor, m))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private static int CountEnemiesNearPoint(List<Mobile> enemies, Point3D point, int range)
        {
            int count = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy != null && !enemy.Deleted && enemy.Alive && GetDistance(enemy.Location, point) <= range)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountEnemiesTargeting(BaseCreature actor, int range)
        {
            if (actor == null)
            {
                return 0;
            }

            int count = 0;
            IPooledEnumerable eable = actor.GetMobilesInRange(range);

            foreach (Mobile m in eable)
            {
                if (IsValidEnemy(actor, m) && m.Combatant == actor)
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private static int CountEnemiesTargeting(List<Mobile> enemies, Mobile target)
        {
            int count = 0;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy != null && !enemy.Deleted && enemy.Alive && enemy.Combatant == target)
                {
                    count++;
                }
            }

            return count;
        }

        private static double GetNearestEnemyDistance(List<Mobile> enemies, Point3D point)
        {
            double nearest = Double.MaxValue;

            for (int i = 0; i < enemies.Count; i++)
            {
                Mobile enemy = enemies[i];

                if (enemy != null && !enemy.Deleted && enemy.Alive)
                {
                    nearest = Math.Min(nearest, GetDistance(enemy.Location, point));
                }
            }

            return nearest == Double.MaxValue ? 99.0 : nearest;
        }

        private static int CountEnemiesInRange(BaseCreature actor, int range)
        {
            int count = 0;
            IPooledEnumerable eable = actor.GetMobilesInRange(range);

            foreach (Mobile m in eable)
            {
                if (IsValidEnemy(actor, m))
                {
                    count++;
                }
            }

            eable.Free();
            return count;
        }

        private static bool MoveToward(BaseCreature actor, Point3D point, bool running, BrainState state, string action)
        {
            if (actor == null || actor.Map == null || actor.Map == Map.Internal)
            {
                return false;
            }

            Direction direction = actor.GetDirectionTo(point);

            if (Move(actor, direction, running))
            {
                RecordAction(state, action);
                return true;
            }

            Direction masked = direction & Direction.Mask;

            if (Move(actor, (Direction)(((int)masked + 1) & 0x7), running) ||
                Move(actor, (Direction)(((int)masked + 7) & 0x7), running))
            {
                RecordAction(state, action);
                return true;
            }

            return false;
        }

        private static bool StepAway(BaseCreature actor, Mobile threat)
        {
            return StepAway(actor, threat, AdventurePartySettings.BacklineEvasiveMoveSteps);
        }

        private static bool StepAway(BaseCreature actor, Mobile threat, int steps)
        {
            if (actor == null || threat == null)
            {
                return false;
            }

            steps = Math.Max(1, steps);
            bool moved = TryStepAwayOnce(actor, threat);

            if (moved && steps > 1)
            {
                QueueStepAwayPulse(actor, threat, steps - 1);
            }

            return moved;
        }

        private static bool TryStepAwayOnce(BaseCreature actor, Mobile threat)
        {
            if (actor == null || threat == null || !IsValidEnemy(actor, threat))
            {
                return false;
            }

            Direction away = actor.GetDirectionTo(threat);
            away = (Direction)(((int)away + 4) & 0x7);

            return Move(actor, away, true) ||
                Move(actor, (Direction)(((int)away + 1) & 0x7), true) ||
                Move(actor, (Direction)(((int)away + 7) & 0x7), true);
        }

        private static void QueueStepAwayPulse(BaseCreature actor, Mobile threat, int remainingSteps)
        {
            if (actor == null || threat == null || remainingSteps <= 0)
            {
                return;
            }

            long now = Core.TickCount;
            long queuedUntil;

            if (m_MovePulseUntil.TryGetValue(actor.Serial, out queuedUntil) && queuedUntil > now)
            {
                return;
            }

            m_MovePulseUntil[actor.Serial] = now + (remainingSteps * AdventurePartySettings.MovePulseDelayMilliseconds) + 100;

            Serial actorSerial = actor.Serial;
            Serial threatSerial = threat.Serial;

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(AdventurePartySettings.MovePulseDelayMilliseconds),
                delegate { ContinueStepAwayPulse(actorSerial, threatSerial, remainingSteps); });
        }

        private static void ContinueStepAwayPulse(Serial actorSerial, Serial threatSerial, int remainingSteps)
        {
            BaseCreature actor = World.FindMobile(actorSerial) as BaseCreature;
            Mobile threat = World.FindMobile(threatSerial);

            if (actor == null || actor.Deleted || !actor.Alive || actor.Map == null || actor.Map == Map.Internal ||
                actor.Paralyzed || actor.Frozen || remainingSteps <= 0 || !IsValidEnemy(actor, threat))
            {
                m_MovePulseUntil.Remove(actorSerial);
                return;
            }

            bool moved = TryStepAwayOnce(actor, threat);

            if (!moved || remainingSteps <= 1)
            {
                m_MovePulseUntil.Remove(actorSerial);
                return;
            }

            Timer.DelayCall(
                TimeSpan.FromMilliseconds(AdventurePartySettings.MovePulseDelayMilliseconds),
                delegate { ContinueStepAwayPulse(actorSerial, threatSerial, remainingSteps - 1); });
        }

        private static bool Move(BaseCreature actor, Direction direction, bool running)
        {
            Direction masked = direction & Direction.Mask;
            actor.Direction = masked;
            return actor.Move(running ? (masked | Direction.Running) : masked);
        }

        private static void RecordSpell(BrainState state, Spell spell)
        {
            if (state == null || spell == null)
            {
                return;
            }

            state.LastSpell = spell.GetType().Name;
            state.LastSpellTime = Core.TickCount;
        }

        private static void RecordAction(BrainState state, string action)
        {
            if (state == null || String.IsNullOrEmpty(action))
            {
                return;
            }

            state.LastAction = action;
            state.LastActionTime = Core.TickCount;
        }

        private static string FormatPoint(Point3D point)
        {
            return String.Format("({0},{1},{2})", point.X, point.Y, point.Z);
        }
    }
}
