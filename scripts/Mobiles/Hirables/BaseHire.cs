/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
 *   email                   : luke@tomasello.com
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

/* scripts\Mobiles\Hirables\BaseHire.cs
 * ChangeLog
 *  11/15/2026, Adam (vanish when dismissed)
 *      VanishWhenDismissed (virtual, default false): a spawned hireling dismissed by its master (released, or out of
 *      pay: both come through OnBeforeRelease) is marked HireDismissed and leaves the world once no player can see it
 *      (18 tiles; hidden staff don't count) and it isn't busy (BusyBeforeVanish: a minstrel's song); its spawner makes
 *      a new one back home. Checked every 15 seconds, no deadline; the mark survives a restart; rehiring clears it.
 *      A hireling with no spawner (placed by hand, a township's) stays. TryVanish is the check (harnesses call it).
 *  1/10/24, Adam
 *      Now hirelings will remain until ALL gold is consumed, not just a multiple of their required pay.
 *      Cleanup timers on delete (it's been a timer leak forever.)
 *      allow incremental pay in any amount, even less than requiredPay if they are already hired
 *      Add callback for handling incremental pay. The minstrel has special 'thank you' messages for instance
 *  7/6/23, Yoar
 *      Complete refactor
 *      Can now drop more gold onto your own hireling
 *      Can no longer be fed
 *      Will remain wonderfully happy while hired
 *  11/8/22, Adam
 *      Hireling NPCs charge 3x more for their services
 *      https://www.uoguide.com/Siege_Perilous
 *      triple price for SiegeRules().
 * 1/26/05 - Albatross:  
 *  Changed the amount of gold the NPC started out with from 8 to 0, and changed the paytimer from 30 minutes to a full UO day.
 * 6/12/04 - Old Salty:  
 *  Added CanBeRenamedBy override so that player's cant rename the npc
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Items;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Server.Mobiles
{
    public abstract class BaseHire : BaseCreature
    {
        public static List<BaseHire> Registry = new List<BaseHire>();
        public new static void Initialize()
        {
            TargetCommands.Register(new HirePayTimerCommand());
        }
        private int m_HoldGold;
        private Timer? m_PayTimer;
        public Timer? HirePayTimer { get { return m_PayTimer; } }
        [CommandProperty(AccessLevel.Administrator)]
        public int HoldGold
        {
            get { return m_HoldGold; }
            set { m_HoldGold = value; }
        }
        [CommandProperty(AccessLevel.Administrator)]
        public bool Manual_Pay
        {
            get { return false; }
            set
            {
                if (value)
                    ProcessPay();
            }
        }
        public BaseHire(AIType AI)
            : this(AI, FightMode.Aggressor, 10, 1, 0.1, 4.0)
        {
        }
        public BaseHire()
            : this(AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.1, 4.0)
        {
        }
        public BaseHire(AIType ai, FightMode mode, int iRangePerception, int iRangeFight, double dActiveSpeed, double dPassiveSpeed)
            : base(ai, mode, iRangePerception, iRangeFight, dActiveSpeed, dPassiveSpeed)
        {
            Registry.Add(this);
        }
        public BaseHire(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version 

            writer.Write((int)m_HoldGold);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                case 0:
                    {
                        if (version < 1)
                            reader.ReadBool(); // IsHired

                        m_HoldGold = reader.ReadInt();
                        break;
                    }
            }

            if (Controlled)
            {
                m_PayTimer = new PayTimer(this);
                m_PayTimer.Start();
            }
            else if (Dismissed)
                StartVanishTimer();     // dismissed before the restart: still waiting to vanish

            Registry.Add(this);
        }
        public override bool KeepsItemsOnDeath { get { return true; } }
        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_PayTimer != null)
                m_PayTimer.Stop();

            m_PayTimer = null;

            StopVanishTimer();

            if (Registry.Contains(this)) ;
            Registry.Remove(this);
        }
        public abstract int RequiredPay();
        public virtual void HandleIncrementalPayMessage(Mobile from, Item dropped, int requiredPay)
        {
            return;
        }
        private bool FromMyMaster(Mobile from)
        {
            return Controlled && ControlMaster == from;
        }

        private bool IsHighterFighterBlocked()
        {
            return Core.RuleSets.SeasonsRules(2) && this is HireFighter;
        }
        public override bool OnGoldGiven(Mobile from, Gold dropped)
        {
            if (IsHighterFighterBlocked())
            {
                return base.OnGoldGiven(from, dropped);
            }

            if (!FromMyMaster(from) && from.FollowerCount + ControlSlots > from.FollowersMax)
            {
                SayTo(from, 1049672); // Thou must reduce thine followers before I will work for thee!
            }
            else if (Controlled && ControlMaster != from)
            {
                SayTo(from, 1042495); // I have already been hired. 
            }
            else
            {
                int requiredPay = RequiredPay();

                // 1/10/2024: Adam, allow incremental payments
                if (FromMyMaster(from))
                {
                    HandleIncrementalPayMessage(from, dropped, requiredPay);
                    GotGoldFrom(from, dropped);
                    return true;
                }
                else if (dropped.Amount < requiredPay)
                {
                    SayTo(from, 502062); // Thou must pay me more than this!
                }
                else
                {
                    if (!Controlled)
                    {
                        int max_minstrels = Core.RuleSets.AngelIslandRules() ? 1 : 1;
                        int max_fighters = Core.RuleSets.AngelIslandRules() ? 1 : from.FollowersMax;
                        int count_minstrels = 0;
                        int count_fighters = 0;
                        foreach (Mobile mobile in from.Pets)
                        {
                            if (mobile is BaseCreature pet)
                            {
                                if (pet.Controlled && pet.ControlMaster == from && pet.Deleted == false)
                                {
                                    if (pet is HireFighter)
                                        count_fighters++;
                                    if (pet is HireMinstrel)
                                        count_minstrels++;
                                }

                                if ((this is HireFighter && count_fighters >= max_fighters) || (this is HireMinstrel && count_minstrels >= max_minstrels))
                                {
                                    Mobile? m = null;
                                    if (this is HireFighter)
                                        m = from.Pets.Where(o => o.GetType() == typeof(HireFighter) && !o.Deleted).FirstOrDefault();
                                    else
                                        m = from.Pets.Where(o => o.GetType() == typeof(HireMinstrel) && !o.Deleted).FirstOrDefault();

                                    SayTo(from, "I see you have already hired {0}.", m.Name);
                                    return false;
                                }

                                if (!CheckControlChance(from))
                                {
                                    SayTo(from, "You do not possess enough leadership to command any more fighters.");
                                    return false;
                                }
                            }
                        }
                    }

                    if (ControlMaster == from || SetControlMaster(from))
                    {
                        GotGoldFrom(from, dropped);
                        return true;
                    }
                }
            }

            return false;
        }
        public override bool CheckControlChance(Mobile m)
        {
            if (this is HireMinstrel)
                return true;
            else if (this is HireFighter)
            {
                int fighters = m.Pets.Where(o => o.GetType() == typeof(HireFighter) && !o.Deleted).ToList().Count;
                if (fighters < m.Int / 10)
                    return true;
                else if (this.ControlMaster == m)   // we're already controlling this guy
                    return true;                    // so don't fail the INT test
                else
                    return false;                   // we're not controlling this guy and cannot add
            }
            else
                return base.CheckControlChance(m);
        }
        private void GotGoldFrom(Mobile from, Gold dropped)
        {
            m_HoldGold += dropped.Amount;
            dropped.Delete();

            // hired again before it vanished: it stays
            SetCreatureBool(CreatureBoolTable.HireDismissed, false);
            StopVanishTimer();

            SayDaysPaid(from);

            if (m_PayTimer != null && m_PayTimer.Running)
                m_PayTimer.Stop();

            m_PayTimer = new PayTimer(this);
            m_PayTimer.Start();
        }
        public void SayHireCost(Mobile from)
        {
            int pay = RequiredPay();

            SayTo(from, 1043256, pay.ToString()); // I am available for hire for ~1_AMOUNT~ gold coins a day. If thou dost give me gold, I will work for thee.
        }
        public void SayDaysPaid(Mobile from)
        {
            int pay = RequiredPay();

            int days;

            if (pay <= 0)
                days = 0;
            else
                days = m_HoldGold / pay;

            SayTo(from, 1043258, days.ToString()); // I thank thee for paying me. I will work for thee for ~1_NUMBER~ days.
        }
        private static string[] Queries = new string[] { "hire", "greetings" };
        public override string[] HandlesQuery { get { return Queries; } }
        public override void OnQuery(SpeechEventArgs e)
        {
            if (e.Mobile == this.ControlMaster)
                SayTo(e.Mobile, "I am already working for thee, but my daily fee is now {0} gp", RequiredPay());
            else if (this.ControlMaster != null)
                SayTo(e.Mobile, "I am already working for {0}, but my daily is fee {1} gp", this.ControlMaster.Name, RequiredPay());
            else
                SayHireCost(e.Mobile);
            return;
        }
        public override bool HandlesOnSpeech(Mobile from)   // needed for OnQuery
        {
            return !IsHighterFighterBlocked();
        }
        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            if (!IsHighterFighterBlocked())
                if (!Controlled)
                    list.Add(new HireEntry(this));
        }
        private class PayTimer : Timer
        {
            private BaseHire m_Hire;

            public PayTimer(BaseHire hire)
                : base(TimeSpan.FromMinutes(Clock.MinutesPerUODay), TimeSpan.FromMinutes(Clock.MinutesPerUODay))
            {
                m_Hire = hire;
                Priority = TimerPriority.OneMinute;
            }

            protected override void OnTick()
            {
                m_Hire.ProcessPay();
            }
        }
        public virtual void ProcessPay()
        {
            int pay = RequiredPay();

            // 1/10/2020: Adam, changed from HoldGold < pay to HoldGold <= 0. This is because we now allow incremental pay
            if (HoldGold <= 0)
            {
                if (this.Mounted)
                    CommandDismount();
            }
            // don't charge while stabled
            else if (!IsAnyStabled)
                HoldGold -= pay;
            else
                ;
        }
        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            if (IsOwner(from))
            {
                if (item.GetItemBool(Item.ItemBoolTable.DeleteOnLift))       // don't allow players to farm fighters for their items
                    Timer.DelayCall(TimeSpan.FromSeconds(1), new TimerStateCallback(Tick), new object[] { item });
                return true;
            }
            else
            {
                return false;
            }
        }
        private void Tick(object state)
        {
            object[] aState = (object[])state;

            if (aState[0] != null && aState[0] is Item item && item.Parent != this)
                item.Delete();
        }
        public override bool AllowEquipFrom(Mobile from)
        {
            if (IsOwner(from) && from.InRange(this, 3) && from.InLOS(this))
                return true;

            return base.AllowEquipFrom(from);
        }
        public override bool IsOwner(Mobile from)
        {
            return ControlMaster == from && Controlled || from.AccessLevel >= AccessLevel.GameMaster;
        }
        public override bool OnBeforeRelease(Mobile controlMaster)
        {
            SetControlMaster(null);
            if (HirePayTimer != null && HirePayTimer.Running)
                HirePayTimer.Stop();

            if (this.Mounted)
                CommandDismount();

            HoldGold = 0;

            // a spawned hireling that vanishes when dismissed: its spawner makes a new one back home. One with no spawner
            //  (placed by hand, a township's) stays: nothing would replace it.
            if (VanishWhenDismissed && Spawner != null)
            {
                SetCreatureBool(CreatureBoolTable.HireDismissed, true);
                StartVanishTimer();
            }

            return base.OnBeforeRelease(controlMaster);
        }

        #region Vanish when dismissed
        /// <summary>
        /// Dismissed (released, or out of pay), this hireling leaves the world once no player can see it, instead of
        /// lingering wherever its master left it. Default false: the fighter keeps his own exit (DoFighterDeath).
        /// </summary>
        public virtual bool VanishWhenDismissed => false;

        /// <summary>Something that should finish before the hireling vanishes (a minstrel's song).</summary>
        public virtual bool BusyBeforeVanish => false;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Dismissed => GetCreatureBool(CreatureBoolTable.HireDismissed);

        private Timer? m_VanishTimer;
        private static readonly TimeSpan VanishCheck = TimeSpan.FromSeconds(15);
        public const int VanishSightRange = 18;

        private void StartVanishTimer()
        {
            StopVanishTimer();
            m_VanishTimer = Timer.DelayCall(VanishCheck, VanishCheck, new TimerCallback(VanishTick));
        }
        private void StopVanishTimer()
        {
            if (m_VanishTimer != null)
                m_VanishTimer.Stop();
            m_VanishTimer = null;
        }
        private void VanishTick()
        {
            TryVanish();
        }

        /// <summary>
        /// The vanish check (the timer's tick; harnesses call it directly): a dismissed hireling no player can see, and
        /// not busy, is deleted. Rehired or no longer dismissed: the timer stops. True when it vanished.
        /// </summary>
        public bool TryVanish()
        {
            if (Deleted || Controlled || !Dismissed)
            {
                StopVanishTimer();
                return false;
            }

            if (PlayerCanSee(this) || BusyBeforeVanish)
                return false;

            StopVanishTimer();
            Delete();
            return true;
        }

        /// <summary>A player within sight of m (hidden staff don't count; visible staff do).</summary>
        public static bool PlayerCanSee(Mobile m)
        {
            if (m == null || m.Map == null || m.Map == Map.Internal)
                return false;

            IPooledEnumerable eable = m.GetClientsInRange(VanishSightRange);
            try
            {
                foreach (Network.NetState ns in eable)
                {
                    Mobile who = ns.Mobile;

                    if (who == null || who.Deleted)
                        continue;

                    if (who.AccessLevel > AccessLevel.Player && who.Hidden)
                        continue;

                    return true;
                }
            }
            finally
            {
                eable.Free();
            }

            return false;
        }
        #endregion Vanish when dismissed
        public override void OnDelete()
        {
            if (m_PayTimer != null && m_PayTimer.Running)
                m_PayTimer.Stop();
            if (Mounted)
                CommandDismount();
            base.OnDelete();
        }
        public override bool CanBeRenamedBy(Mobile from)
        {
            return base.CanBeRenamedBy(from);
        }
        public override bool CheckFeed(Mobile from, Item dropped)
        {
            return false;
        }
        public override void OnThink()
        {
            // as long as we're paid, we're happy
            if (Controlled)
                LoyaltyValue = PetLoyalty.WonderfullyHappy;

            base.OnThink();
        }
        public class HireEntry : ContextMenuEntry
        {
            private BaseHire m_Hire;

            public HireEntry(BaseHire hire)
                : base(6120, 3)
            {
                m_Hire = hire;
            }

            public override void OnClick()
            {
                m_Hire.SayHireCost(Owner.From);
            }
        }
        private class HirePayTimerCommand : BaseCommand
        {
            public HirePayTimerCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllNPCs;
                Commands = new string[] { "PayTimerTick" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "PayTimerTick";
                Description = "Process a pay time tick for the targeted BaseHire.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is BaseHire hireling)
                {
                    if (hireling.m_PayTimer == null || hireling.m_PayTimer.Running == false)
                        LogFailure("Their pay timer is not running.");
                    else
                        hireling.ProcessPay();
                }
                else
                    LogFailure("That is not a BaseHire.");
            }
        }
    }
}