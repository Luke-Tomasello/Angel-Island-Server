/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *   
 *   Angel Island UO Shard   : March 25, 2004
 *   portions copyright      : (C) 2004-2024 Tomasello Software LLC.
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

/* Scripts/Engines/BulkOrders/BulkOrderSystem.cs
 * CHANGELOG:
 *  10/4/2023, Adam
 *      Serialize LargeEnabled
 *  11/15/21, Yoar
 *      Added logging to LBOD exchange mechanism.
 *  11/7/21, Yoar
 *      Added LBOD exchange mechanism: Drop an empty LBOD onto a BOD vendor
 *      in order to exchange it for a random SBOD.
 *  10/31/21, Yoar
 *      The BOD system now remembers BOD offers.
 *      
 *      If you accidentally close the BOD accept gump, the same offer will
 *      re-appear the next time you talk to the BOD vendor.
 *      
 *      Pending offers can only be removed by:
 *      1. Pressing "OK" in the BOD accept gump, claiming the BOD.
 *      2. Pressing "CANCEL" in the BOD accept gump, deleting the BOD.
 *  10/28/21, Yoar
 *      Added AFK check.
 *  10/27/21, Yoar
 *      Added AccountWideDelays: If enabled, BOD delays are shared over the player's account.
 *  10/26/21, Yoar
 *      Added EnabledFlags to enable/disable specific BOD skills.
 *  10/25/21, Yoar
 *      Added more configurations.
 *      Added serialization for the configurations.
 *      Doubled the small BOD banking rate from 2% to 4%.
 *  10/25/21, Yoar
 *      Added logging.
 *  10/25/21, Adam
 *      Add support for BOD Leaderboard
 * 10/24/21, Yoar
 *      CoreAI: Added a BulkOrdersEnabled bit
 * 10/14/21, Yoar
 *      Initial version.
 *      
 *      This class deals with various mechanics related to the Bulk Order System.
 *      
 *      To enable the Bulk Order System, Ensure that BulkOrderSystem.Enabled returns True.
 *      To enable gold rewards from BODs, set BulkOrderSystem.GoldRewards to True.
 *      To enable fame rewards from BODs, set BulkOrderSystem.FameRewards to True.
 *      To enable the new reward mechanic (OSI Pub95), set BulkOrderSystem.RewardsGump to True.
 *      
 *      Pre-Pub95 configuation:
 *      - BulkOrderSystem.GoldRewards = True
 *      - BulkOrderSystem.FameRewards = True
 *      - BulkOrderSystem.RewardsGump = False
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Diagnostics;
using Server.Engines.Craft;
using Server.Gumps;
using Server.Items;
using Server.Misc;
using Server.Mobiles;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Server.Engines.BulkOrders
{
    public enum BulkOrderType : sbyte
    {
        Invalid = -1,

        Smith,
        Tailor,
        Tinker,
        Carpenter,
        Fletcher,
        Alchemist,
        Scribe,
        Cook,
    }

    [Flags]
    public enum BulkOrderFlags : byte
    {
        None = 0x00,

        Smith = 0x01,
        Tailor = 0x02,
        Tinker = 0x04,
        Carpenter = 0x08,
        Fletcher = 0x10,
        Alchemist = 0x20,
        Scribe = 0x40,
        Cook = 0x80,

        Mask = 0xFF,
    }

    public enum BankingSetting : byte
    {
        Enabled,
        Disabled,
        Automatic
    }

    public abstract class BulkOrderSystem
    {
        public static bool Enabled
        {
            get { return Core.RuleSets.BulkOrderSystemRules(); }
        }

        public static BulkOrderFlags EnabledFlags = BulkOrderFlags.Smith; // enable/disable specific BOD skills
        public static bool GoldRewards = false; // do we get gold from BODs?
        public static bool FameRewards = true; // do we get fame from BODs?
        public static bool RewardsGump = true; // can we select a reward using the reward gump? (SA)
        public static bool OfferOnTurnIn = true; // do we get an instant offer when we turn in a completed BOD? (SE)
        public static bool LargeEnabled = true;  // do we support LBODs?
        public static bool ExchangeLargeBODs = true; // can we exchange an empty LBOD for a SBOD?
        public static double GoldScalar = 1.0; // in case gold rewards are enabled, scale gold by this much
        public static int SmallBankPerc = 4; // what percentage of points can we bank for small BODs?
        public static int LargeBankPerc = 20; // what percentage of points can we bank for large BODs?
        public static bool AccountWideDelays = false; // are BOD delays account-wide?
        public static TimeSpan TinyDelay = TimeSpan.FromHours(1.0); // wait time after a "tiny" offer, skill <= 50.0
        public static TimeSpan SmallDelay = TimeSpan.FromHours(2.0); // wait time after a small offer, skill <= 70.0
        public static TimeSpan LargeDelay = TimeSpan.FromHours(6.0); // wait time after a large offer, skill > 70.0
        public static TimeSpan TurnInDelay = TimeSpan.FromSeconds(10.0); // how long must we wait before turning in our next BOD? (ML)

        private static readonly List<BulkOrderSystem> m_Systems = new List<BulkOrderSystem>();

        public static List<BulkOrderSystem> Systems { get { return m_Systems; } }

        static BulkOrderSystem()
        {
            #region Dynamic Registration

            foreach (Assembly asm in ScriptCompiler.Assemblies)
            {
                foreach (Type type in asm.GetTypes())
                {
                    if (type.IsSubclassOf(typeof(BulkOrderSystem)))
                    {
                        PropertyInfo prop = type.GetProperty("System", BindingFlags.Static | BindingFlags.Public);

                        if (prop == null)
                            continue;

                        MethodInfo accessor = prop.GetGetMethod();

                        if (accessor == null)
                            continue;

                        m_Systems.Add((BulkOrderSystem)accessor.Invoke(null, null));
                    }
                }
            }

            #endregion

            InitResourceValues();
        }

        public static void Configure()
        {
            EventSink.WorldSave += new WorldSaveEventHandler(EventSink_WorldSave);
            EventSink.WorldLoad += new WorldLoadEventHandler(EventSink_WorldLoad);
        }

        public static void Initialize()
        {
            TargetCommands.Register(new ViewContextCommand());
            TargetCommands.Register(new FillBODCommand());
            TargetCommands.Register(new CraftValueCommand());
        }

        private static void EventSink_WorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        private static void EventSink_WorldLoad()
        {
            Load();
        }

        #region Commands

        private class ViewContextCommand : BaseCommand
        {
            public ViewContextCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "BODContext" };
                ObjectTypes = ObjectTypes.Mobiles;
                Usage = "BODContext";
                Description = "Opens the bulk order context for a targeted player.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (e.Arguments.Length == 0)
                {
                    LogFailure("Must specify a bulk order type.");
                    return;
                }

                BulkOrderType type;

                if (!Enum.TryParse(e.GetString(0), true, out type))
                {
                    LogFailure("That does not name a valid bulk order type.");
                    return;
                }

                BulkOrderSystem system = GetSystem(type);

                if (system == null)
                {
                    LogFailure("That does not name a valid bulk order type.");
                    return;
                }

                BulkOrderContext context = system.GetContext((Mobile)obj, false);

                if (context == null)
                {
                    LogFailure("They have no bulk order context.");
                    return;
                }

                e.Mobile.SendGump(new PropertiesGump(e.Mobile, context));
            }
        }

        private class FillBODCommand : BaseCommand
        {
            public FillBODCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Simple;
                Commands = new string[] { "FillBOD" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "FillBOD";
                Description = "Fills the targeted BOD.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is SmallBOD)
                {
                    SmallBOD sbod = (SmallBOD)obj;

                    sbod.AmountCur = sbod.AmountMax;
                }
                else if (obj is LargeBOD)
                {
                    LargeBOD lbod = (LargeBOD)obj;

                    foreach (LargeBulkEntry entry in lbod.Entries)
                        entry.Amount = lbod.AmountMax;
                }
                else
                {
                    LogFailure("That is not a bulk order deed.");
                }
            }
        }

        private class CraftValueCommand : BaseCommand
        {
            public CraftValueCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.Single;
                Commands = new string[] { "CraftValue" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "CraftValue";
                Description = "Estimates the craft value (in gp) of the targeted item.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                Item item = obj as Item;

                if (item == null)
                {
                    LogFailure("That is not an item.");
                    return;
                }

                Type itemType = item.GetType();

                CraftResource resource = SmallBOD.GetResource(item);

                bool exceptional = SmallBOD.GetExceptional(item);

                bool any = false;

                foreach (CraftSystem craftSystem in CraftSystem.Instances)
                {
                    bool isCraftable = false;

                    foreach (CraftItem craftItem in craftSystem.CraftItems)
                    {
                        if (craftItem.ItemType == itemType)
                        {
                            isCraftable = true;
                            break;
                        }
                    }

                    if (isCraftable)
                    {
                        any = true;
                        e.Mobile.SendMessage("{0}: {1} gp", craftSystem.MainSkill, EstimateCraftValue(craftSystem, itemType, resource, exceptional));
                    }
                }

                if (!any)
                    LogFailure("That is not a craftable item.");
            }
        }

        #endregion

        public static BulkOrderSystem GetSystem(BulkOrderType type)
        {
            foreach (BulkOrderSystem system in m_Systems)
            {
                if (system.BulkOrderType == type)
                    return system;
            }

            return null;
        }

        public static BulkOrderFlags GetFlag(BulkOrderType type)
        {
            return (BulkOrderFlags)(0x1 << (int)type);
        }

        public abstract BulkOrderType BulkOrderType { get; }
        public abstract CraftSystem CraftSystem { get; }

        private SkillName m_Skill;
        private int m_DeedHue;
        private Dictionary<Mobile, BulkOrderContext> m_Contexts;

        public SkillName Skill { get { return m_Skill; } set { m_Skill = value; } }
        public int DeedHue { get { return m_DeedHue; } set { m_DeedHue = value; } }
        public Dictionary<Mobile, BulkOrderContext> Contexts { get { return m_Contexts; } }

        public BulkOrderSystem()
        {
            m_Contexts = new Dictionary<Mobile, BulkOrderContext>();
        }

        public bool IsEnabled()
        {
            return (Enabled && ((EnabledFlags & GetFlag(BulkOrderType)) != 0));
        }

        public SmallBOD ConstructSBOD()
        {
            return ConstructSBOD(0, false, BulkMaterialType.None, 0, null, 0, 0);
        }

        public abstract SmallBOD ConstructSBOD(int amountMax, bool requireExceptional, BulkMaterialType material, int amountCur, Type type, int number, int graphic);

        public LargeBOD ConstructLBOD()
        {
            return ConstructLBOD(0, false, BulkMaterialType.None, new LargeBulkEntry[0]);
        }

        public abstract LargeBOD ConstructLBOD(int amountMax, bool requireExceptional, BulkMaterialType material, LargeBulkEntry[] entries);

        public virtual bool SupportsBulkOrders(Mobile from)
        {
            return (from.Skills[m_Skill].Base > 0.0);
        }

        public virtual bool UsesQuality(Type type)
        {
            return false;
        }

        public virtual bool UsesMaterial(Type type)
        {
            return false;
        }

        public virtual int GetMaterialMessage(bool isItem)
        {
            if (isItem)
                return 1157310; // The item is not made from the requested resource.
            else
                return 1157311; // Both orders must use the same resource type.
        }

        public abstract SmallBulkEntry[][] GetSmallEntries();
        public abstract SmallBulkEntry[][] GetLargeEntries();
        public abstract BulkMaterial[] GetMaterials();
        public abstract RewardEntry[] GetRewardEntries();
        public abstract RewardOption[] GetRewardOptions();

        public abstract int ComputePoints(BaseBOD bod);

        public DateTime GetNextBOD(Mobile from)
        {
            BulkOrderContext context = GetContext(from, false);

            DateTime next = (context == null ? DateTime.MinValue : context.NextBOD);

            if (AccountWideDelays && from.Account != null)
            {
                for (int i = 0; i < from.Account.Length; i++)
                {
                    Mobile alt = from.Account[i];

                    if (alt != null && alt != from)
                    {
                        BulkOrderContext otherContext = GetContext(from, false);

                        if (otherContext != null && otherContext.NextBOD > next)
                            next = otherContext.NextBOD;
                    }
                }
            }

            return next;
        }

        #region Offer

        public void OfferBulkOrder(Mobile from, BaseVendor vendor, bool fromContextMenu)
        {
            if (from is PlayerMobile && !((PlayerMobile)from).RTT("AFK bulk order check."))
                return;

            BulkOrderContext context = GetContext(from, true);

            if (context == null)
                return;

            double theirSkill = from.Skills[m_Skill].Base;

            BaseBOD bod;

            if (LargeEnabled && theirSkill >= 70.1 && Utility.RandomDouble() < (theirSkill - 40.0) / 300.0)
                bod = ConstructLBOD();
            else
                bod = ConstructSBOD();

            if (bod == null)
                return;

            bod.Randomize(from);

            TimeSpan delay;

            if (Core.UOTC_CFG || Core.Debug)
                delay = TimeSpan.FromSeconds(3.0); // instant BODs
            else if (theirSkill >= 70.1)
                delay = LargeDelay;
            else if (theirSkill >= 50.1)
                delay = SmallDelay;
            else
                delay = TinyDelay;

            context.NextBOD = DateTime.UtcNow + delay;

            if (context.CurrentOffer != null)
                context.CurrentOffer.Delete();

            context.CurrentOffer = bod;

            if (bod is SmallBOD)
                from.SendGump(new SmallBODAcceptGump(from, (SmallBOD)bod));
            else if (bod is LargeBOD)
                from.SendGump(new LargeBODAcceptGump(from, (LargeBOD)bod));

            LogHelper logger = new LogHelper("BulkOrderOffers.log", false, true);
            logger.Log(LogType.Mobile, from, String.Format("Was offered {0} ({1}) and must now wait {2}.", bod, FormatBulkOrder(bod), delay));
            logger.Finish();
        }

        public void UnregisterOffer(Mobile from)
        {
            BulkOrderContext context = GetContext(from, false);

            if (context != null)
                context.CurrentOffer = null;
        }

        public SmallBulkEntry GetRandomSmallEntry()
        {
            SmallBulkEntry[][] allEntries = GetSmallEntries();

            int total = 0;

            for (int i = 0; i < allEntries.Length; i++)
                total += allEntries[i].Length;

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < allEntries.Length; i++)
            {
                SmallBulkEntry[] entries = allEntries[i];

                if (rnd < entries.Length)
                    return entries[rnd];
                else
                    rnd -= entries.Length;
            }

            return null;
        }

        public SmallBulkEntry GetRandomSmallEntry(Mobile from)
        {
            SmallBulkEntry[][] allEntries = GetSmallEntries();

            int total = 0;

            for (int i = 0; i < allEntries.Length; i++)
            {
                SmallBulkEntry[] entries = allEntries[i];

                for (int j = 0; j < entries.Length; j++)
                {
                    SmallBulkEntry e = entries[j];

                    if (IsCraftableBy(from, e.Type, false))
                        total++;
                }
            }

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < allEntries.Length; i++)
            {
                SmallBulkEntry[] entries = allEntries[i];

                for (int j = 0; j < entries.Length; j++)
                {
                    SmallBulkEntry e = entries[j];

                    if (IsCraftableBy(from, e.Type, false))
                    {
                        if (rnd == 0)
                            return e;
                        else
                            rnd--;
                    }
                }
            }

            return null;
        }

        public SmallBulkEntry[] GetRandomLargeEntries()
        {
            SmallBulkEntry[][] allEntries = GetLargeEntries();

            if (allEntries.Length == 0)
                return new SmallBulkEntry[0];

            return allEntries[Utility.Random(allEntries.Length)];
        }

        public BulkMaterialType RandomMaterial(double skillValue)
        {
            BulkMaterial[] materials = GetMaterials();

            int total = 0;

            for (int i = 0; i < materials.Length; i++)
            {
                BulkMaterial bulkMaterial = materials[i];

                if (skillValue >= bulkMaterial.ReqSkill)
                    total += bulkMaterial.Weight;
            }

            if (total <= 0)
                return BulkMaterialType.None;

            int rnd = Utility.Random(total);

            for (int i = 0; i < materials.Length; i++)
            {
                BulkMaterial bulkMaterial = materials[i];

                if (skillValue >= bulkMaterial.ReqSkill)
                {
                    if (rnd < bulkMaterial.Weight)
                        return bulkMaterial.Type;
                    else
                        rnd -= bulkMaterial.Weight;
                }
            }

            return BulkMaterialType.None;
        }

        public bool IsCraftableBy(Mobile m, Type itemType, bool reqExceptional)
        {
            if (CraftSystem == null)
                return true;

            CraftItem craftItem = CraftSystem.CraftItems.SearchFor(itemType);

            if (craftItem == null)
                return false;

            bool allRequiredSkills = true;
            double chance = craftItem.GetSuccessChance(m, null, CraftSystem, false, ref allRequiredSkills);

            if (!allRequiredSkills || chance < 0.0)
                return false;

            return (!reqExceptional || craftItem.GetExceptionalChance(CraftSystem, chance, m) > 0.0);
        }

        #endregion

        #region Turn-In

        public bool HandleBulkOrderDropped(Mobile from, BaseVendor vendor, BaseBOD bod)
        {
            BulkOrderContext context = GetContext(from, false);

            if (context != null && DateTime.UtcNow < context.NextTurnIn)
            {
                vendor.SayTo(from, "You'll have to wait a few seconds while I inspect the last order.");
                return false;
            }
            else if (vendor.BulkOrderSystem != bod.System)
            {
                vendor.SayTo(from, 1045130); // That order is for some other shopkeeper.
                return false;
            }
            else if (ExchangeLargeBODs && bod is LargeBOD && ((LargeBOD)bod).IsEmpty())
            {
                BeginExchange(from, vendor, (LargeBOD)bod);
                return false;
            }
            else if (!bod.IsComplete())
            {
                vendor.SayTo(from, 1045131); // You have not completed the order yet.
                return false;
            }
            else if (context != null && context.Pending.Points > 0)
            {
                vendor.SayTo(from, "You must claim your last turn-in reward in order for us to continue doing business.");
                from.SendMessage("Say \"claim\" to the vendor in order to claim your last turn-in.");
                return false;
            }

            if (context == null)
                context = GetContext(from, true);

            if (context == null)
                return false; // sanity

            int points = ComputePoints(bod);

            int gold = ComputeGold(bod, points);
            int fame = ComputeFame(bod, points);

            from.SendSound(0x3D);

            if (RewardsGump)
            {
                vendor.SayTo(from, "Thank you so much!  Select a reward for your effort.");

                // call our data recorder to attribute these points to a player for our leader board. 
                DataRecorder.DataRecorder.RecordBODPoints(from, points);

                context.Pending = new PendingReward(points, bod is LargeBOD);

                BeginClaimReward(from, vendor);

                LogHelper logger = new LogHelper("BulkOrders.log", false, true);
                logger.Log(LogType.Mobile, from, String.Format("Handed in {0} ({1}) for {2} points.", bod.ToString(), FormatBulkOrder(bod), points));
                logger.Finish();
            }
            else
            {
                vendor.SayTo(from, 1045132); // Thank you so much!  Here is a reward for your effort.

                Item reward = GetRandomReward(points);

                if (reward != null)
                {   // describe to the system whence this item originated
                    reward.Origin = Item.Genesis.BOD;
                    from.AddToBackpack(reward);
                }
            }

            if (GoldRewards && gold > 0)
            {
                gold = Math.Max(1, (int)(gold * GoldScalar));

                if (gold > 1000)
                    from.AddToBackpack(new BankCheck(gold));
                else if (gold > 0)
                    from.AddToBackpack(new Gold(gold));

                // call our data recorder to attribute this gold earned to a player for our leader board. 
                DataRecorder.DataRecorder.RecordBODGold(from as Mobile, gold);

                LogHelper logger = new LogHelper("BulkOrderGold.log", false, true);
                logger.Log(LogType.Mobile, from, String.Format("Handed in {0} ({1}) for {2} gp.", bod.ToString(), FormatBulkOrder(bod), gold));
                logger.Finish();
            }

            if (FameRewards)
                Titles.AwardFame(from, fame, true);

            if (OfferOnTurnIn)
                context.NextBOD = DateTime.UtcNow;

            if (TurnInDelay > TimeSpan.Zero)
                context.NextTurnIn = DateTime.UtcNow + TurnInDelay;

            bod.Delete();
            return true;
        }

        private Item GetRandomReward(int points)
        {
            RewardEntry[] group = GetRewardGroup(points);

            int total = 0;

            for (int i = 0; i < group.Length; i++)
                total += group[i].Weight;

            if (total <= 0)
                return null;

            int rnd = Utility.Random(total);

            for (int i = 0; i < group.Length; i++)
            {
                if (rnd < group[i].Weight)
                    return group[i].Reward.Construct();
                else
                    rnd -= group[i].Weight;
            }

            return null;
        }

        private RewardEntry[] GetRewardGroup(int points)
        {
            RewardEntry[] rewards = GetRewardEntries();

            List<RewardEntry> group = new List<RewardEntry>();
            int price = -1;

            for (int i = rewards.Length - 1; i >= 0; i--)
            {
                RewardEntry reward = rewards[i];

                if (price == -1)
                {
                    if (points >= reward.Points)
                    {
                        price = reward.Points;
                        group.Add(reward);
                    }
                }
                else if (price == reward.Points)
                {
                    group.Add(reward);
                }
                else
                {
                    break;
                }
            }

            return group.ToArray();
        }

        public virtual int ComputeFame(BaseBOD bod, int points)
        {
            if (points < 0)
                return 0;

            int v = points / 50;

            return v * v;
        }

        #endregion

        #region Reward Points

        public void BeginClaimReward(Mobile from, BaseVendor vendor)
        {
            BulkOrderContext context = GetContext(from, false);

            if (context == null)
                return;

            switch (context.BankingSetting)
            {
                case BankingSetting.Enabled:
                    {
                        if (context.Pending.Points == 0)
                            goto case BankingSetting.Automatic;

                        from.CloseGump(typeof(RewardsGump));
                        from.SendGump(new RewardsGump(this, from));
                        break;
                    }
                case BankingSetting.Disabled:
                    {
                        from.CloseGump(typeof(RewardsGump));
                        from.SendGump(new RewardsGump(this, from, false));
                        break;
                    }
                case BankingSetting.Automatic:
                    {
                        SavePoints(from);
                        from.CloseGump(typeof(RewardsGump));
                        from.SendGump(new RewardsGump(this, from, true));
                        break;
                    }
            }
        }

        public void SavePoints(Mobile from)
        {
            BulkOrderContext context = GetContext(from, false);

            if (context == null)
                return;

            context.Banked += (context.Pending.Large ? LargeBankPerc : SmallBankPerc) * context.Pending.Points / 100.0;
            context.Pending = PendingReward.Zero;
        }

        public static bool ClaimReward(Mobile from, RewardOption reward)
        {
            Item toGive = reward.Reward.Construct();

            if (toGive == null)
                return false;

            if (!from.PlaceInBackpack(toGive))
            {
                from.SendLocalizedMessage(1078837); // Your backpack is full! Please make room and try again.
                toGive.Delete();
                return false;
            }

            from.SendLocalizedMessage(1073621); // Your reward has been placed in your backpack.
            from.PlaySound(0x5A7);

            LogHelper logger = new LogHelper("BulkOrderRewards.log", false, true);
            logger.Log(LogType.Mobile, from, String.Format("Claimed reward {0} (Label={1}) for {2} points.", toGive, reward.Label, reward.Points));
            logger.Finish();

            return true;
        }

        #endregion

        #region Exchange Large BOD

        public void BeginExchange(Mobile from, BaseVendor vendor, LargeBOD lbod)
        {
            vendor.SayTo(from, "So you are not up to the task?");

            from.CloseGump(typeof(ExchangeBODGump));
            from.SendGump(new ExchangeBODGump(vendor, lbod));
        }

        public void Exchange(Mobile from, BaseVendor vendor, LargeBOD lbod)
        {
            if (!lbod.IsChildOf(from.Backpack))
            {
                from.SendLocalizedMessage(1042001); // That must be in your pack for you to use it.
                return;
            }

            if (lbod.Entries.Length <= 0)
                return;

            SmallBulkEntry e = lbod.Entries[Utility.Random(lbod.Entries.Length)].Details;

            SmallBOD sbod = ConstructSBOD(lbod.AmountMax, lbod.RequireExceptional, lbod.Material, 0, e.Type, e.Number, e.Graphic);

            if (sbod == null)
                return;

            lbod.Delete();

            from.AddToBackpack(sbod);

            Titles.AwardFame(from, -20, true);

            vendor.SayTo(from, "You'll find this one much easier to fulfil.");

            LogHelper logger = new LogHelper("BulkOrderExchange.log", false, true);
            logger.Log(LogType.Mobile, from, String.Format("Exchanged LBOD ({0}) for SBOD ({1}).", FormatBulkOrder(lbod), FormatBulkOrder(sbod)));
            logger.Finish();
        }

        #endregion

        public void AddContextMenuEntries(Mobile from, BaseVendor vendor, ArrayList list)
        {
            list.Add(new BulkOrderInfoCME(vendor));
        }

        #region Bulk Order Info

        private class BulkOrderInfoCME : ContextMenuEntry
        {
            private BaseVendor m_Vendor;

            public BulkOrderInfoCME(BaseVendor vendor)
                : base(6152, 6) // Bulk Order Info
            {
                m_Vendor = vendor;
            }

            public override void OnClick()
            {
                if (m_Vendor.BulkOrderSystem != null)
                    m_Vendor.BulkOrderSystem.RequestInfo(Owner.From, m_Vendor);
            }
        }

        public void RequestInfo(Mobile from, BaseVendor vendor)
        {
            if (!SupportsBulkOrders(from))
                return;

            // if we have a pending offer, show it
            BulkOrderContext context = GetContext(from, false);

            if (context != null)
            {
                Item offer = context.CurrentOffer;

                if (offer != null && !offer.Deleted)
                {
                    if (offer is SmallBOD)
                        from.SendGump(new SmallBODAcceptGump(from, (SmallBOD)offer));
                    else if (offer is LargeBOD)
                        from.SendGump(new LargeBODAcceptGump(from, (LargeBOD)offer));

                    return;
                }
            }

            TimeSpan ts = GetNextBOD(from) - DateTime.UtcNow;

            int totalSeconds = (int)ts.TotalSeconds;

            if (totalSeconds > 0)
            {
                int min = (totalSeconds + 59) / 60;
                int hrs = 0;

                if (min >= 60)
                {
                    hrs = min / 60;
                    min %= 60;
                }

                int oldSpeechHue = vendor.SpeechHue;
                vendor.SpeechHue = 0x3B2;

                if (hrs == 0)
                    vendor.SayTo(from, "An offer may be available in {0} minute{1}.", min, min == 1 ? "" : "s");
                else if (min == 0)
                    vendor.SayTo(from, "An offer may be available in {0} hour{1}.", hrs, hrs == 1 ? "" : "s");
                else
                    vendor.SayTo(from, "An offer may be available in {0} hour{1} and {2} minute{3}.", hrs, hrs == 1 ? "" : "s", min, min == 1 ? "" : "s");

                vendor.SpeechHue = oldSpeechHue;
            }
            else
            {
                from.SendLocalizedMessage(1049038); // You can get an order now.

                if (/*Core.RuleSets.AOSRules()*/ true)
                    OfferBulkOrder(from, vendor, true);
            }
        }

        #endregion

        public void HandleSpeech(Mobile from, BaseVendor vendor, SpeechEventArgs e)
        {
            if (e.Handled || !from.Alive || !from.InRange(vendor, 6))
                return;

            if (RewardsGump && e.HasKeyword(0x9)) // *claim*
            {
                e.Handled = true;

                BeginClaimReward(from, vendor);

                vendor.FocusMob = from;
            }
            else if (Utility.Contains(to_test: e.Speech, pattern: "bulk order"))
            {
                e.Handled = true;

                RequestInfo(from, vendor);
            }
        }

        #region Data Management

        private const string CfgFileName = @"Saves\BulkOrderConfig.xml";
        private const string BinFileName = @"Saves\BulkOrderContexts.bin";

        private static void Save()
        {
            SaveConfig();
            SaveContexts();
        }

        private static void Load()
        {
            LoadConfig();
            LoadContexts();
        }

        private static void SaveConfig()
        {
            Console.WriteLine("Bulk Order Config Saving...");

            try
            {
                XmlTextWriter writer = new XmlTextWriter(CfgFileName, Encoding.Default);
                writer.Formatting = Formatting.Indented;

                writer.WriteStartDocument(true);
                writer.WriteStartElement("BulkOrderConfig");
                writer.WriteAttributeString("version", "3");

                try
                {   // version 3
                    writer.WriteElementString("LargeEnabled", LargeEnabled.ToString());

                    writer.WriteElementString("AccountWideDelays", AccountWideDelays.ToString());

                    writer.WriteElementString("EnabledFlags", ((byte)EnabledFlags).ToString());

                    writer.WriteElementString("GoldRewards", GoldRewards.ToString());
                    writer.WriteElementString("FameRewards", FameRewards.ToString());
                    writer.WriteElementString("RewardsGump", RewardsGump.ToString());
                    writer.WriteElementString("OfferOnTurnIn", OfferOnTurnIn.ToString());

                    writer.WriteElementString("GoldScalar", GoldScalar.ToString());

                    writer.WriteElementString("SmallBankPerc", SmallBankPerc.ToString());
                    writer.WriteElementString("LargeBankPerc", LargeBankPerc.ToString());

                    writer.WriteElementString("TinyDelay", TinyDelay.ToString());
                    writer.WriteElementString("SmallDelay", SmallDelay.ToString());
                    writer.WriteElementString("LargeDelay", LargeDelay.ToString());

                    writer.WriteElementString("TurnInDelay", TurnInDelay.ToString());
                }
                finally { writer.WriteEndDocument(); }

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("BulkOrderSystem Error: {0}", e);
            }
        }

        private static void LoadConfig()
        {
            Console.WriteLine("Bulk Order Config Loading...");

            try
            {
                if (!File.Exists(CfgFileName))
                    return; // nothing to read

                XmlTextReader reader = new XmlTextReader(CfgFileName);
                reader.WhitespaceHandling = WhitespaceHandling.None;
                reader.MoveToContent();

                int verison = Int32.Parse(reader.GetAttribute("version"));
                reader.ReadStartElement("BulkOrderConfig");

                switch (verison)
                {
                    case 3:
                        {
                            LargeEnabled = Boolean.Parse(reader.ReadElementString("LargeEnabled"));
                            goto case 2;
                        }
                    case 2:
                        {
                            AccountWideDelays = Boolean.Parse(reader.ReadElementString("AccountWideDelays"));
                            goto case 1;
                        }
                    case 1:
                        {
                            EnabledFlags = (BulkOrderFlags)Byte.Parse(reader.ReadElementString("EnabledFlags"));
                            goto case 0;
                        }
                    case 0:
                        {
                            GoldRewards = Boolean.Parse(reader.ReadElementString("GoldRewards"));
                            FameRewards = Boolean.Parse(reader.ReadElementString("FameRewards"));
                            RewardsGump = Boolean.Parse(reader.ReadElementString("RewardsGump"));
                            OfferOnTurnIn = Boolean.Parse(reader.ReadElementString("OfferOnTurnIn"));

                            GoldScalar = Double.Parse(reader.ReadElementString("GoldScalar"));

                            SmallBankPerc = Int32.Parse(reader.ReadElementString("SmallBankPerc"));
                            LargeBankPerc = Int32.Parse(reader.ReadElementString("LargeBankPerc"));

                            TinyDelay = TimeSpan.Parse(reader.ReadElementString("TinyDelay"));
                            SmallDelay = TimeSpan.Parse(reader.ReadElementString("SmallDelay"));
                            LargeDelay = TimeSpan.Parse(reader.ReadElementString("LargeDelay"));

                            TurnInDelay = TimeSpan.Parse(reader.ReadElementString("TurnInDelay"));

                            break;
                        }
                }

                reader.ReadEndElement();

                reader.Close();
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        private static void SaveContexts()
        {
            Console.WriteLine("Bulk Order Contexts Saving...");

            try
            {
                BinaryFileWriter writer = new BinaryFileWriter(BinFileName, true);

                writer.Write((int)1); // version

                writer.Write((int)Systems.Count);

                foreach (BulkOrderSystem system in Systems)
                {
                    writer.Write((sbyte)system.BulkOrderType);
                    writer.Write((int)system.Contexts.Count);

                    foreach (KeyValuePair<Mobile, BulkOrderContext> kvp in system.Contexts)
                    {
                        writer.Write((Mobile)kvp.Key);
                        kvp.Value.Serialize(writer);
                    }
                }

                writer.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("BulkOrderSystem Error: {0}", e);
            }
        }

        private static void LoadContexts()
        {
            Console.WriteLine("Bulk Order Contexts Loading...");

            try
            {
                if (!File.Exists(BinFileName))
                    return; // nothing to read

                BinaryFileReader reader = new BinaryFileReader(new BinaryReader(new FileStream(BinFileName, FileMode.Open, FileAccess.Read)));

                int version = reader.ReadInt();

                switch (version)
                {
                    case 1:
                        {
                            int systemsCount = reader.ReadInt();

                            for (int i = 0; i < systemsCount; i++)
                            {
                                BulkOrderType type = (BulkOrderType)reader.ReadSByte();
                                int contextsCount = reader.ReadInt();

                                BulkOrderSystem system = GetSystem(type);

                                for (int j = 0; j < contextsCount; j++)
                                {
                                    Mobile m = reader.ReadMobile();

                                    BulkOrderContext context = new BulkOrderContext(m);

                                    context.Deserialize(reader);

                                    if (system != null && m != null)
                                        system.Contexts[m] = context;
                                }
                            }

                            break;
                        }
                    case 0:
                        {
                            int count = reader.ReadInt();

                            for (int i = 0; i < count; i++)
                            {
                                Mobile m = reader.ReadMobile();

                                LoadLegacyContext(reader, m);
                            }

                            break;
                        }
                }

                reader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("BulkOrderSystem Error: {0}", e);
            }
        }

        [Flags]
        private enum LegacyFlag : uint
        {
            None = 0x0000,
            Smith = 0x0001,
            Tailor = 0x0002,
            CurrentOffer = 0x0100,
            NextTurnIn = 0x0200,
        }

        private static void LoadLegacyContext(BinaryFileReader reader, Mobile m)
        {
            reader.ReadInt(); // legacy version

            LegacyFlag flags = (LegacyFlag)reader.ReadUInt();

            if (flags.HasFlag(LegacyFlag.Smith))
                LoadLegacyInstance(reader, m, BulkOrderType.Smith);

            if (flags.HasFlag(LegacyFlag.Tailor))
                LoadLegacyInstance(reader, m, BulkOrderType.Tailor);

            if (flags.HasFlag(LegacyFlag.CurrentOffer))
            {
                Item currentOffer = reader.ReadItem();

                if (currentOffer != null)
                    currentOffer.Delete();
            }

            if (flags.HasFlag(LegacyFlag.NextTurnIn))
                reader.ReadDeltaTime(); // next turn in - forget
        }

        private static void LoadLegacyInstance(BinaryFileReader reader, Mobile m, BulkOrderType type)
        {
            reader.ReadInt(); // legacy version

            DateTime nextBOD = reader.ReadDeltaTime();
            double banked = reader.ReadDouble();
            PendingReward pending = new PendingReward(reader);

            BulkOrderSystem system = GetSystem(type);

            if (system == null)
                return;

            BulkOrderContext context = system.GetContext(m, true);

            if (context == null)
                return;

            context.NextBOD = nextBOD;
            context.Banked = banked;
            context.Pending = pending;
        }

        #endregion

        #region Compute Gold

        public static bool RewardGoldByCraftValue = false;
        public static int RewardGoldRandomization = 10;

        public virtual int ComputeGold(BaseBOD bod, int points)
        {
            if (bod is SmallBOD)
            {
                SmallBOD sbod = (SmallBOD)bod;

                CraftResource resource = BulkMaterialInfo.Lookup(sbod.Material).Resource;

                int value;

                // 1/16/24, Yoar: Reward gold can be estimated based on crafting requirements
                if (RewardGoldByCraftValue)
                {
                    value = GetCraftValue(CraftSystem, sbod.Type, resource, sbod.RequireExceptional);
                }
                else
                {
                    /* The rewards are threefold. The first reward is gold, equal to half its vendor's value in gold for non-large bulk deeds. 
                     *  So, if an object is valued at 200 gold outside, then it would yield 100 gold multiplied by the requested amount. 
                     *  A check will be given to the person fulfilling the order if the amount is more than the person can carry. 
                     * https://uo.stratics.com/content/basics/bulk_orders_archive.shtml#rewards 
                     */
                    value = GetVendorPrice(sbod.Type, resource, sbod.RequireExceptional);

                    // half the value
                    value /= 2;
                }

                value *= bod.AmountMax;

                if (RewardGoldRandomization > 0)
                    value = value * (100 + Utility.RandomMinMax(-RewardGoldRandomization, +RewardGoldRandomization)) / 100;

                if (value < 1)
                    value = 1;

                return value;
            }
            else if (bod is LargeBOD)
            {
                return bod.AmountMax * GetGold(points);
            }

            return 0;
        }

        private static int GetVendorPrice(Type itemType, CraftResource resource, bool exceptional)
        {
            const int qualityBonus = 50; // percentage bonus for an exceptional requirement
            const int materialBonus = 50; // percentage bonus for a non-standard resource requirement
            const int materialTierBonus = 10; // percentage bonus per resource tier

            int vendorPrice = BaseVendor.PlayerPays(itemType);

            if (vendorPrice >= BaseVendor.PlayerPaysFailsafe)
                vendorPrice = 2; // we don't have this item in our StandardPricingDictionary

            double dvalue = vendorPrice;

            if (exceptional)
                dvalue *= (100 + qualityBonus) / 100.0;

            CraftResource start = CraftResources.GetStart(resource);

            // make sure we have a valid 'start' value
            if (CraftResources.GetType(resource) == CraftResources.GetType(start))
            {
                int materialIndex = (int)resource - (int)start;

                if (materialIndex > 0)
                    dvalue *= (100 + materialBonus + materialIndex * materialTierBonus) / 100.0;
            }

            return (int)Math.Round(dvalue);
        }

        private int GetGold(int points)
        {
            if (points < 0)
                return 0;

            int index = points / 100;

            if (index >= m_GoldTable.Length)
                index = m_GoldTable.Length - 1;

            return m_GoldTable[index];
        }

        private static readonly int[] m_GoldTable = new int[]
            {
                100, 200, 300, 400, 500, 750, 1000, 1500, 2500, 5000, 10000
            };

        #endregion

        public BulkOrderContext GetContext(Mobile from, bool create)
        {
            BulkOrderContext context;

            if (!m_Contexts.TryGetValue(from, out context))
            {
                if (!create)
                    return null;

                m_Contexts[from] = context = new BulkOrderContext(from);
            }

            return context;
        }

        public static void SetNextBOD(Mobile from, BulkOrderType type, DateTime value)
        {
            BulkOrderSystem system = GetSystem(type);

            if (system == null)
                return;

            BulkOrderContext context = system.GetContext(from, true);

            if (context == null)
                return;

            context.NextBOD = value;
        }

        public static string FormatBulkOrder(BaseBOD bod)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("AmountMax={0}, ", bod.AmountMax);
            sb.AppendFormat("RequireExceptional={0}, ", bod.RequireExceptional);
            sb.AppendFormat("Material={0}, ", bod.Material);

            if (bod is SmallBOD)
            {
                SmallBOD sbod = (SmallBOD)bod;

                sb.AppendFormat("ItemCount=1, ");
                sb.AppendFormat("ItemType={0}", sbod.Type);
            }
            else if (bod is LargeBOD)
            {
                LargeBOD lbod = (LargeBOD)bod;

                sb.AppendFormat("ItemCount={0}, ", lbod.Entries.Length);

                if (lbod.Entries.Length != 0)
                    sb.AppendFormat("ItemType={0}", lbod.Entries[0].Details.Type);
            }

            return sb.ToString();
        }

        public ArrayList GetAllRewards(int points)
        {
            RewardEntry[] group = GetRewardGroup(points);

            ArrayList rewards = new ArrayList();

            for (int i = 0; i < group.Length; i++)
            {
                Item reward = group[i].Reward.Construct();

                if (reward != null)
                    rewards.Add(reward);
            }

            return rewards;
        }

        #region Craft Value

        private const int FailSafeValue = 2;

        private static readonly Dictionary<Type, double> m_ResourceValues = new Dictionary<Type, double>();

        public static Dictionary<Type, double> ResourceValues { get { return m_ResourceValues; } }

        private static void AddResourceValue(Type type, double value)
        {
            m_ResourceValues[type] = value;
        }

        private static double GetResourceValue(Type type)
        {
            foreach (Type equivType in CraftItem.GetEquivalentResources(type))
            {
                double value;

                if (m_ResourceValues.TryGetValue(equivType, out value))
                    return value;
            }

            return FailSafeValue;
        }

        private static void InitResourceValues()
        {
            AddResourceValue(typeof(IronIngot), 10);
            AddResourceValue(typeof(DullCopperIngot), 11);
            AddResourceValue(typeof(ShadowIronIngot), 12);
            AddResourceValue(typeof(CopperIngot), 13);
            AddResourceValue(typeof(BronzeIngot), 14);
            AddResourceValue(typeof(GoldIngot), 15);
            AddResourceValue(typeof(AgapiteIngot), 16);
            AddResourceValue(typeof(VeriteIngot), 17);
            AddResourceValue(typeof(ValoriteIngot), 18);

            AddResourceValue(typeof(Leather), 8);
            AddResourceValue(typeof(SpinedLeather), 10);
            AddResourceValue(typeof(HornedLeather), 12);
            AddResourceValue(typeof(BarbedLeather), 14);

            AddResourceValue(typeof(Log), 7);
            AddResourceValue(typeof(OakLog), 8);
            AddResourceValue(typeof(AshLog), 9);
            AddResourceValue(typeof(YewLog), 10);
            AddResourceValue(typeof(HeartwoodLog), 12);
            AddResourceValue(typeof(BloodwoodLog), 14);
            AddResourceValue(typeof(FrostwoodLog), 16);
        }

        public static int GetCraftValue(CraftSystem craftSystem, Type itemType, CraftResource resource, bool exceptional)
        {
            int value = EstimateCraftValue(craftSystem, itemType, resource, exceptional);

            if (value <= 0)
                return 2; // fail safe

            return value;
        }

        // TODO: Return a double?
        public static int EstimateCraftValue(CraftSystem craftSystem, Type itemType, CraftResource resource, bool exceptional)
        {
            if (craftSystem == null)
                return FailSafeValue;

            CraftItem craftItem = null;

            foreach (CraftItem ci in craftSystem.CraftItems)
            {
                if (ci.ItemType == itemType)
                {
                    craftItem = ci;
                    break;
                }
            }

            if (craftItem == null)
                return FailSafeValue;

            double dvalue = 0.0;

            foreach (CraftRes craftRes in craftItem.Resources)
            {
                Type resType = MutateResource(craftSystem, craftItem, craftRes.ItemType, resource);

                dvalue += GetResourceValue(resType) * craftRes.Amount;
            }

            double valSkill = 100.0; // assume a not quite GM crafter

            double successChance = GetSuccessChance(craftSystem, craftItem, valSkill);

            if (exceptional)
                successChance *= GetExceptionalChance(craftSystem, craftItem, valSkill, successChance);

            double successScalar = Math.Max(1.0, Math.Min(2.0, 1.0 / successChance));

            dvalue *= successScalar;

            return (int)Math.Round(dvalue);
        }

        private static Type MutateResource(CraftSystem craftSystem, CraftItem craftItem, Type baseType, CraftResource resource)
        {
            CraftSubResCol resCol = (craftItem.UseSubRes2 ? craftSystem.CraftSubRes2 : craftSystem.CraftSubRes);

            if (baseType != resCol.ResType)
                return baseType;

            CraftResourceInfo baseInfo = CraftResources.GetInfo(CraftResources.GetFromType(baseType));

            if (baseInfo == null)
                return baseType;

            CraftResourceInfo mutateInfo = CraftResources.GetInfo(resource);

            if (mutateInfo == null)
                return baseType;

            int index = Array.IndexOf(baseInfo.ResourceTypes, baseType);

            if (index >= 0 && index < mutateInfo.ResourceTypes.Length)
                return mutateInfo.ResourceTypes[index];

            return baseType;
        }

        // Formulas taken from CraftItem. No bonuses are applied.
        private static double GetSuccessChance(CraftSystem craftSystem, CraftItem craftItem, double valSkill)
        {
            double minSkill = 0.0;
            double maxSkill = 0.0;

            foreach (CraftSkill craftSkill in craftItem.Skills)
            {
                if (craftSkill.SkillToMake == craftSystem.MainSkill)
                {
                    minSkill = craftSkill.MinSkill;
                    maxSkill = craftSkill.MaxSkill;
                    break;
                }
            }

            double chanceAtMin = craftSystem.GetChanceAtMin(craftItem);

            return chanceAtMin + ((valSkill - minSkill) / (maxSkill - minSkill) * (1.0 - chanceAtMin));
        }

        // Formulas taken from CraftItem. No bonuses are applied.
        private static double GetExceptionalChance(CraftSystem craftSystem, CraftItem craftItem, double valSkill, double chance)
        {
            switch (craftSystem.ECA)
            {
                default:
                case CraftECA.ChanceMinusSixty:
                    {
                        return chance - 0.6;
                    }
                case CraftECA.FiftyPercentChanceMinusTenPercent:
                    {
                        return (chance * 0.5) - 0.1;
                    }
                case CraftECA.ChanceMinusSixtyToFourtyFive:
                    {
                        return chance - Math.Max(0.45, Math.Min(0.60, 0.60 - ((valSkill - 95.0) * 0.03)));
                    }
            }
        }

        #endregion
    }
}