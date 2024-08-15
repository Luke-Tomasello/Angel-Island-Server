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

/* Engines/Township/TSNPC.cs
 * CHANGELOG:
 *  9/3/2023, Adam (OnSpeech)
 *      allow Township vendors to be 'turned' with the 'cycle' keyword just like PlayerVendors
 * 6/4/2023, Adam
 *      Township NPCs are now Invulnerable
 *      This wasn't a problem on Angel Island, but having vulnerable township NPCs on Siege simply doesn't work. 
 * 2/19/22, Yoar
 *      Change to township NPC access: You must now either be the guild leader or a co-owner of the house to manage the NPC.
 *      Added 'OptionsCME': Opens the TownshipNPCGump.
 * 2/19/22, Yoar
 *      More township cleanups.
 * 1/12/22, Yoar
 *      Township cleanups.
 * 11/23/21, Yoar
 *      Replaced old township tools with TownshipBuilderTool.
 * 10/10/08, Pix
 *		Added #regions so I can keep my sanity.
 *		Added Wall tools to TSProvisioner.
 *	5/11/08, Adam
 *		Performance Conversion: Regions now use HashTables instead of ArrayLists
 *	12/11/07 Pix
 *		Now lookouts report to allies of the township.
*/

using Server.ContextMenus;
using Server.Guilds;
using Server.Items;
using Server.Multis;
using Server.Regions;
using Server.Township;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public interface ITownshipNPC
    {
        Mobile Owner { get; set; }
        int WanderRange { get; set; }
        void HandleTryTownshipExit();   // allows the mobile to for instance, explain the issue, and break 'follow'
        void OnPlacement(Mobile from);
    }

    public abstract class BaseTownshipNPC : BaseVendor, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public virtual void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        public override bool IsActiveVendor { get { return false; } }

        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo()
        {
        }

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        [Constructable]
        public BaseTownshipNPC(string title)
            : base(title)
        {
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        public BaseTownshipNPC(Serial serial)
            : base(serial)
        {
        }
        private const byte mask = 0x80;
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((byte)0x81); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = Utility.PeekByte(reader);

            if ((version & mask) == 0)
                return; // old version

            reader.ReadByte(); // consume version

            switch (version)
            {
                case 0x81:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 0x81)
                ValidationQueue<BaseTownshipNPC>.Enqueue(this);
        }

        public void Validate(object state)
        {
            FixOwner(this, ref m_Owner);
        }

        public static void FixOwner(Mobile m, ref Mobile owner)
        {
            BaseHouse h = BaseHouse.FindHouseAt(m);

            if (h != null && h.IsInside(m))
                owner = h.Owner;
        }
    }

    public static class TownshipNPCHelper
    {
        public static bool IsTownshipNPC(Mobile m)
        {
            return (m is ITownshipNPC);
        }

        public static bool IsOwner(Mobile npc, Mobile m)
        {
            if (m.AccessLevel >= AccessLevel.GameMaster)
                return true;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(npc);

            if (tsr == null || tsr.TStone == null)
                return false;

            if (tsr.TStone.HasAccess(m, TownshipAccess.CoLeader))
                return true;

            if (tsr.TStone.HasAccess(m, TownshipAccess.Member))
            {
                if (npc is ITownshipNPC && ((ITownshipNPC)npc).Owner == m)
                    return true;

#if false
                BaseHouse house = BaseHouse.FindHouseAt(npc.Location, npc.Map, 20);

                if (house != null && house.IsCoOwner(m))
                    return true;
#endif
            }

            return false;
        }

        public static bool CanInteractWith(Mobile npc, Mobile m, bool message = true)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(npc);

            if (tsr == null || tsr.TStone == null)
                return false;

            if (!tsr.TStone.OutsideNPCInteractionAllowed && BaseHouse.FindHouseAt(npc) != null && BaseHouse.FindHouseAt(npc) != BaseHouse.FindHouseAt(m))
            {
                if (message)
                    m.SendMessage("You must be inside the house to interact with this vendor.");

                return false;
            }

            if (tsr.TStone.IsEnemy(m))
            {
                if (message)
                    npc.SayTo(m, "Thou'rt an enemy of the township and I refuse to serve thee!");

                return false;
            }

            return true;
        }
        private static bool CanLeaveHouse(Mobile npc)
        {
            return (npc is BaseCreature bc && bc.ControlMaster != null && bc.ControlMaster.Map == bc.Map && bc.GetDistanceToSqrt(bc.ControlMaster) < 15);
        }
        public static bool CheckMovement(Mobile npc, Direction d, out int newZ)
        {
            int newX = npc.X;
            int newY = npc.Y;
            newZ = npc.Z;

            Movement.Movement.Offset(d, ref newX, ref newY);

            // 1/8/2024, Adam: hirelings may follow their control masters around
            if (!CanLeaveHouse(npc))
            {
                BaseHouse house = BaseHouse.FindHouseAt(npc.Location, npc.Map, 20);

                if (house != null && BaseHouse.FindHouseAt(new Point3D(newX, newY, newZ), npc.Map, 20) != house)
                    return false;
            }

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(npc.Location, npc.Map);

            if (tsr != null && TownshipRegion.GetTownshipAt(new Point3D(newX, newY, newZ), npc.Map) != tsr)
            {   // allows for the npc to 1) break off follow, and give an explanation
                if (npc is ITownshipNPC tsnpc)
                    tsnpc.HandleTryTownshipExit();
                return false;
            }

            return true;
        }

        public static void HandleSpeech(Mobile npc, SpeechEventArgs e)
        {
            if (e.Handled)
                return;

            Mobile from = e.Mobile;

            if (Insensitive.Equals(e.Speech, "npc manage") || Insensitive.Equals(e.Speech, "vendor manage") ||
                (Insensitive.EndsWith(e.Speech, " manage") && WasNamed(npc, e.Speech)))
            {
                if (IsOwner(npc, from))
                {
                    // 8/29/23, Yoar: Quick fix
                    TownshipRegion tsr = TownshipRegion.GetTownshipAt(npc);
                    if (tsr == null || tsr.TStone == null)
                    {
                        e.Handled = true;
                        return;
                    }

                    from.CloseGump(typeof(TownshipNPCGump));
                    from.SendGump(new TownshipNPCGump(npc, tsr.TStone));

                    e.Handled = true;
                }
            }
            else if (Insensitive.Equals(e.Speech, "npc cycle") || Insensitive.Equals(e.Speech, "vendor cycle") ||
              (Insensitive.EndsWith(e.Speech, " cycle") && WasNamed(npc, e.Speech)))
            {   // allow Township vendors to be 'turned' with the 'cycle' keyword just like PlayerVendors
                if (IsOwner(npc, from))
                {
                    if (IsOwner(npc, from))
                        npc.Direction = npc.GetDirectionTo(from);

                    e.Handled = true;
                }
            }
        }

        public static bool WasNamed(Mobile npc, string speech)
        {
            string name = npc.Name;

            return (name != null && Insensitive.StartsWith(speech, name));
        }

        public class OptionsCME : ContextMenuEntry
        {
            private BaseCreature m_NPC;

            public OptionsCME(BaseCreature npc)
                : base(6209) // Contract Options
            {
                m_NPC = npc;
            }

            public override void OnClick()
            {
                Mobile from = Owner.From;

                if (m_NPC.Deleted || !IsOwner(m_NPC, from))
                    return;

                // 8/29/23, Yoar: Quick fix
                TownshipRegion tsr = TownshipRegion.GetTownshipAt(m_NPC);
                if (tsr == null || tsr.TStone == null)
                    return;

                from.CloseGump(typeof(TownshipNPCGump));
                from.SendGump(new TownshipNPCGump(m_NPC, tsr.TStone));
            }
        }

        public static void PurchaseNPC(TownshipStone stone, Mobile from, Type type)
        {
            int charge = GetNPCCharge(type);

            if (stone.GoldHeld < charge)
            {
                from.SendMessage("You lack the necessary funds to purchase this NPC.");
                return;
            }

            if (!CheckPlaceNPC(from, type))
                return;

            stone.GoldHeld -= charge;

            stone.RecordWithdrawal(charge, string.Format("{0} purchased {1} NPC", from.Name, GetNPCName(type)));
        }

        public static void PurchaseDeed(TownshipStone stone, Mobile from, Type type)
        {
            int charge = GetNPCCharge(type);

            if (stone.GoldHeld < charge)
            {
                from.SendMessage("You lack the necessary funds to purchase this NPC.");
                return;
            }

            Type deedType = GetDeedType(type);

            if (deedType == null)
                return;

            TownshipNPCDeed deed = Construct<TownshipNPCDeed>(deedType);

            if (deed == null)
                return;

            deed.Guild = from.Guild as Guild;

            from.AddToBackpack(deed);

            stone.GoldHeld -= charge;

            stone.RecordWithdrawal(charge, string.Format("{0} purchased {1} deed", from.Name, GetNPCName(type)));

            from.SendMessage("A {0} deed has been placed in your backpack.", GetNPCName(type));
        }

        public enum PlaceNPCResult
        {
            Success,
            Invalid,
            NotInTownship,
            LackActivity,
            NotInHouse,
            NeutralHouse,
            PrivateHouse,
            TooManyInHouse,
            DesignatedBanker,
            DesignatedInnkeeper,
        }

        public static bool CheckPlaceNPC(Mobile from, Type type, Serial? doppelganger = null)
        {
            PlaceNPCResult result = CanPlaceNPC(from, type);

            if (result != PlaceNPCResult.Success)
            {
                from.SendMessage(GetMessage(result));
                return false;
            }

            return PlaceNPC(from, type, doppelganger);
        }

        public static PlaceNPCResult CanPlaceNPC(Mobile from, Type type)
        {
            return CanPlaceNPC(from, type, from.Location, from.Map);
        }

        public static PlaceNPCResult CanPlaceNPC(Mobile from, Type type, Point3D targetLoc, Map targetMap)
        {
            if (targetMap == null)
                return PlaceNPCResult.Invalid;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(targetLoc, targetMap);

            if (tsr == null || tsr.TStone == null || !tsr.TStone.HasAccess(from, TownshipAccess.Member))
                return PlaceNPCResult.NotInTownship;

            ActivityLevel activityReq = GetNPCActivityReq(type);

            if (tsr.TStone.ActivityLevel < activityReq)
                return PlaceNPCResult.LackActivity;

            BaseHouse house = BaseHouse.FindHouseAt(targetLoc, targetMap, 16);

            if (house == null)
            {
                if (RequiresHouse(type))
                    return PlaceNPCResult.NotInHouse;
            }
            else
            {
                if (!house.IsCoOwner(from))
                    return PlaceNPCResult.NotInHouse;

                if (house.Owner == null || !tsr.TStone.IsMember(house.Owner))
                    return PlaceNPCResult.NeutralHouse;

                if (!house.Public)
                    return PlaceNPCResult.PrivateHouse;

                List<Mobile> houseNPCs = GetHouseNPCs(house);

                if (houseNPCs.Count >= house.MaxSecures)
                    return PlaceNPCResult.TooManyInHouse;

                if ((type == typeof(TSBanker) && houseNPCs.Count != 0) || FindInListByType(houseNPCs, typeof(TSBanker)))
                    return PlaceNPCResult.DesignatedBanker;

                if ((type == typeof(TSInnKeeper) && houseNPCs.Count != 0) || FindInListByType(houseNPCs, typeof(TSInnKeeper)))
                    return PlaceNPCResult.DesignatedInnkeeper;
            }

            return PlaceNPCResult.Success;
        }

        public static List<Mobile> GetHouseNPCs(BaseHouse house)
        {
            List<Mobile> list = new List<Mobile>();

            foreach (Mobile m in house.Region.Mobiles.Values)
            {
                if (IsTownshipNPC(m))
                    list.Add(m);
            }

            return list;
        }

        private static bool FindInListByType(List<Mobile> list, Type type)
        {
            foreach (Mobile m in list)
            {
                if (m.GetType() == type)
                    return true;
            }

            return false;
        }

        public static string GetMessage(PlaceNPCResult result)
        {
            switch (result)
            {
                case PlaceNPCResult.Invalid:
                    return "You cannot place this NPC here.";
                case PlaceNPCResult.NotInTownship:
                    return "You can only place this NPC in your township.";
                case PlaceNPCResult.LackActivity:
                    return "Your township must have a higher activity level to place this NPC.";
                case PlaceNPCResult.NotInHouse:
                    return "You can only place this NPC in a house that you own.";
                case PlaceNPCResult.NeutralHouse:
                    return "You can only place this NPC in a house that is part of the township.";
                case PlaceNPCResult.PrivateHouse:
                    return "You can only place this NPC in a public house.";
                case PlaceNPCResult.TooManyInHouse:
                    return "You cannot place any more township NPCs in this house.";
                case PlaceNPCResult.DesignatedBanker:
                    return "A banker cannot share the same house with other NPCs.";
                case PlaceNPCResult.DesignatedInnkeeper:
                    return "An innkeeper cannot share the same house with other NPCs.";
            }

            return null;
        }

        public static bool PlaceNPC(Mobile from, Type type, Serial? doppelganger = null)
        {
            BaseCreature npc = null;
            if (doppelganger == Serial.Zero)
                npc = Construct<BaseCreature>(type);
            else
                npc = World.FindMobile(doppelganger.GetValueOrDefault()) as BaseCreature;

            if (npc == null)
                return false;

            npc.IsIntMapStorage = false;
            npc.Home = from.Location;
            npc.RangeHome = 3;                      // will this ever be a non ITownshipNPC?
            npc.Guild = from.Guild;
            npc.DisplayGuildTitle = true;

            if (npc is ITownshipNPC tsNpc)
            {
                npc.RangeHome = tsNpc.WanderRange;  // handled correctly here
                tsNpc.Owner = from;
                tsNpc.OnPlacement(from);
            }

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(from.Location, from.Map);

            if (tsr != null && tsr.TStone != null && !tsr.TStone.TownshipNPCs.Contains(npc))
                tsr.TStone.TownshipNPCs.Add(npc);

            npc.MoveToWorld(from.Location, from.Map);

            return true;
        }

        public static ActivityLevel GetNPCActivityReq(Type type)
        {
            if (type == typeof(TSEmissary))
                return ActivityLevel.MEDIUM;
            else if (type == typeof(TSEvocator))
                return ActivityLevel.MEDIUM;
            else if (type == typeof(TSBanker))
                return ActivityLevel.HIGH;
            else if (type == typeof(TSAnimalTrainer))
                return ActivityLevel.HIGH;
            else if (type == typeof(TSStableMaster))
                return ActivityLevel.HIGH;
            else if (type == typeof(TSAlchemist))
                return ActivityLevel.MEDIUM;
            else if (type == typeof(TSMage))
                return ActivityLevel.MEDIUM;
            else if (type == typeof(TSTownCrier))
                return ActivityLevel.MEDIUM;
            else
                return ActivityLevel.LOW;
        }

        public static int GetNPCCharge(Type type)
        {
            if (type == typeof(TSEmissary))
                return TownshipSettings.EmissaryCharge;
            else if (type == typeof(TSEvocator))
                return TownshipSettings.EvocatorCharge;
            else if (type == typeof(TSAlchemist))
                return TownshipSettings.AlchemistCharge;
            else if (type == typeof(TSAnimalTrainer))
                return TownshipSettings.AnimalTrainerCharge;
            else if (type == typeof(TSStableMaster))
                return TownshipSettings.StableMasterCharge;
            else if (type == typeof(TSBanker))
                return TownshipSettings.BankerCharge;
            else if (type == typeof(TSInnKeeper))
                return TownshipSettings.InnkeeperCharge;
            else if (type == typeof(TSMage))
                return TownshipSettings.MageCharge;
            else if (type == typeof(TSProvisioner))
                return TownshipSettings.ProvisionerCharge;
            else if (type == typeof(TSArmsTrainer))
                return TownshipSettings.ArmsTrainerCharge;
            else if (type == typeof(TSMageTrainer))
                return TownshipSettings.MageTrainerCharge;
            else if (type == typeof(TSRogueTrainer))
                return TownshipSettings.RogueTrainerCharge;
            else if (type == typeof(TSLookout))
                return TownshipSettings.LookoutCharge;
            else if (type == typeof(TSFightBroker))
                return TownshipSettings.FightBrokerCharge;
            else if (type == typeof(TSMinstrel))
                return TownshipSettings.MinstrelCharge;
            else if (type == typeof(TSTownCrier))
                return TownshipSettings.TownCrierCharge;
            else if (type == typeof(TSNecromancer))
                return TownshipSettings.NecromancerCharge;
            else if (type == typeof(TSRancher))
                return TownshipSettings.RancherCharge;
            else if (type == typeof(TSFarmer))
                return TownshipSettings.FarmerCharge;
            else
                return 20000; // default charge
        }

        public static int GetNPCFee(Type type)
        {
            if (type == typeof(TSAlchemist))
                return TownshipSettings.NPCType2Fee;
            else if (type == typeof(TSAnimalTrainer))
                return TownshipSettings.NPCType3Fee;
            else if (type == typeof(TSStableMaster))
                return TownshipSettings.NPCType3Fee;
            else if (type == typeof(TSBanker))
                return TownshipSettings.NPCType3Fee;
            else if (type == typeof(TSMage))
                return TownshipSettings.NPCType2Fee;
            else if (type == typeof(TSMinstrel))
                return TownshipSettings.NPCType1Fee;
            else
                return TownshipSettings.NPCType1Fee;
        }

        public static string GetNPCName(Type type)
        {
            string name = type.Name;

            if (name.StartsWith("TS") && name.Length > 2)
                name = name.Substring(2);

            return Utility.SplitCamelCase(name);
        }

        public static Type GetDeedType(Type type)
        {
            if (type == typeof(TSEmissary))
                return typeof(TSEmissaryDeed);
            else if (type == typeof(TSEvocator))
                return typeof(TSEvocatorDeed);
            else if (type == typeof(TSAlchemist))
                return typeof(TSAlchemistDeed);
            else if (type == typeof(TSAnimalTrainer))
                return typeof(TSAnimalTrainerDeed);
            else if (type == typeof(TSStableMaster))
                return typeof(TSStableMasterDeed);
            else if (type == typeof(TSBanker))
                return typeof(TSBankerDeed);
            else if (type == typeof(TSInnKeeper))
                return typeof(TSInnkeeperDeed);
            else if (type == typeof(TSMage))
                return typeof(TSMageDeed);
            else if (type == typeof(TSProvisioner))
                return typeof(TSProvisionerDeed);
            else if (type == typeof(TSArmsTrainer))
                return typeof(TSArmsTrainerDeed);
            else if (type == typeof(TSMageTrainer))
                return typeof(TSMageTrainerDeed);
            else if (type == typeof(TSRogueTrainer))
                return typeof(TSRogueTrainerDeed);
            else if (type == typeof(TSLookout))
                return typeof(TSLookoutDeed);
            else if (type == typeof(TSFightBroker))
                return typeof(TSFightBrokerDeed);
            else if (type == typeof(TSMinstrel))
                return typeof(TSMinstrelDeed);
            else if (type == typeof(TSTownCrier))
                return typeof(TSTownCrierDeed);
            else if (type == typeof(TSNecromancer))
                return typeof(TSNecromancerDeed);
            else if (type == typeof(TSRancher))
                return typeof(TSRancherDeed);
            else if (type == typeof(TSFarmer))
                return typeof(TSFarmerDeed);
            else
                return null;
        }

        public static bool HasNPCMenu(Type type)
        {
            if (type == typeof(TSEmissary))
                return true;
            else if (type == typeof(TSEvocator))
                return true;
            else if (type == typeof(TSTownCrier))
                return true;
            else if (type == typeof(TSRancher))
                return true;
            else
                return false;
        }

        public static void OpenNPCMenu(TownshipStone stone, Mobile from, Type type)
        {
            if (type == typeof(TSEmissary))
            {
                from.CloseGump(typeof(TSEmissaryGump));
                from.SendGump(new TSEmissaryGump(stone, from));
            }
            else if (type == typeof(TSEvocator))
            {
                from.CloseGump(typeof(TSEvocatorGump));
                from.SendGump(new TSEvocatorGump(stone, from));
            }
            else if (type == typeof(TSTownCrier))
            {
                from.CloseGump(typeof(TSTownCrierGump));
                from.SendGump(new TSTownCrierGump(stone, from));
            }
            else if (type == typeof(TSRancher))
            {
                from.CloseGump(typeof(TSRancherGump));
                from.SendGump(new TSRancherGump(stone, from));
            }
        }

        public static T Construct<T>(Type type)
        {
            if (!typeof(T).IsAssignableFrom(type))
                return default(T);

            T obj;

            try
            {
                obj = (T)Activator.CreateInstance(type);
            }
            catch
            {
                obj = default(T);
            }

            return obj;
        }

        private static Type[] m_BuyList = new Type[0];

        public static Type[] BuyList
        {
            get
            {
                if (m_BuyList.Length == 0)
                {
                    List<Type> buyList = new List<Type>();

                    buyList.Add(typeof(TSProvisioner));
                    buyList.Add(typeof(TSFarmer));
                    buyList.Add(typeof(TSRancher));
                    buyList.Add(typeof(TSMage));
                    buyList.Add(typeof(TSAlchemist));
                    buyList.Add(typeof(TSInnKeeper));
                    buyList.Add(typeof(TSAnimalTrainer));
                    buyList.Add(typeof(TSStableMaster));
                    buyList.Add(typeof(TSBanker));

                    buyList.Add(typeof(TSLookout));
                    buyList.Add(typeof(TSEmissary));
                    buyList.Add(typeof(TSEvocator));
                    buyList.Add(typeof(TSTownCrier));
                    buyList.Add(typeof(TSMinstrel));

                    if (!Core.RuleSets.SiegeRules())
                    {
                        buyList.Add(typeof(TSArmsTrainer));
                        buyList.Add(typeof(TSMageTrainer));
                        buyList.Add(typeof(TSRogueTrainer));
                    }

                    if (Core.RuleSets.AngelIslandRules() || Core.RuleSets.MortalisRules())
                    {
                        buyList.Add(typeof(TSFightBroker));
                    }
                    m_BuyList = buyList.ToArray();
                }

                return m_BuyList;
            }
        }

        public static bool RequiresHouse(Type type)
        {
            if (type == typeof(TSLookout))
                return false;
            else if (type == typeof(TSTownCrier))
                return false;
            else if (type == typeof(TSRancher))
                return false;
            else if (type == typeof(TSFarmer))
                return false;
            else if (type == typeof(TSMinstrel))
                return false;
            else
                return true;
        }
    }

    public class TSMageTrainer : BaseTownshipNPC
    {
        [Constructable]
        public TSMageTrainer()
            : base("the master arcane trainer")
        {
            SetSkill(SkillName.EvalInt, 85.0, 100.0);
            SetSkill(SkillName.Inscribe, 65.0, 100.0);
            SetSkill(SkillName.MagicResist, 64.0, 100.0);
            SetSkill(SkillName.Magery, 90.0, 100.0);
            SetSkill(SkillName.Wrestling, 60.0, 100.0);
            SetSkill(SkillName.Meditation, 85.0, 100.0);
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomBlueHue()));
        }

        #region Serialization

        public TSMageTrainer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion
    }

    public class TSArmsTrainer : BaseTownshipNPC
    {
        [Constructable]
        public TSArmsTrainer()
            : base("the master at-arms trainer")
        {
            SetSkill(SkillName.Fencing, 85.0, 100.0);
            SetSkill(SkillName.Swords, 65.0, 100.0);
            SetSkill(SkillName.Macing, 64.0, 100.0);
            SetSkill(SkillName.Parry, 90.0, 100.0);
            SetSkill(SkillName.Tactics, 60.0, 100.0);
            SetSkill(SkillName.Anatomy, 85.0, 100.0);
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
        }

        #region Serialization

        public TSArmsTrainer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion
    }

    public class TSRogueTrainer : BaseTownshipNPC
    {
        [Constructable]
        public TSRogueTrainer()
            : base("the master rogue trainer")
        {
            SetSkill(SkillName.Stealing, 85.0, 100.0);
            SetSkill(SkillName.Hiding, 65.0, 100.0);
            SetSkill(SkillName.Stealth, 64.0, 100.0);
            SetSkill(SkillName.Snooping, 90.0, 100.0);
            SetSkill(SkillName.Poisoning, 60.0, 100.0);
            SetSkill(SkillName.Lockpicking, 85.0, 100.0);
            SetSkill(SkillName.DetectHidden, 85.0, 100.0);
            SetSkill(SkillName.RemoveTrap, 85.0, 100.0);
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Shoes : VendorShoeType.Sandals; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            if (Utility.RandomBool())
                AddItem(new Kryss());
            else
                AddItem(new Dagger());
        }

        #region Serialization

        public TSRogueTrainer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion
    }

    public class TSEmissary : BaseTownshipNPC
    {
        [Constructable]
        public TSEmissary()
            : base("the emissary of Lord British")
        {
        }

        public override VendorShoeType ShoeType
        {
            get { return VendorShoeType.ThighBoots; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new GoldRing());

            Runebook runebook = new Runebook();
            runebook.Hue = 0x47E;
            runebook.Name = "Rules of Engagement";
            runebook.Movable = false;
            AddItem(runebook);
        }

        #region Serialization

        public TSEmissary(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion
    }

    public class TSEvocator : BaseTownshipNPC
    {
        [Constructable]
        public TSEvocator()
            : base("the evocator")
        {
        }

        public override VendorShoeType ShoeType
        {
            get { return VendorShoeType.ThighBoots; }
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new GoldRing());

            Runebook runebook = new Runebook();
            runebook.Hue = 0x47E;
            runebook.Name = "Writ of Travel";
            runebook.Movable = false;
            AddItem(runebook);
        }

        #region Serialization

        public TSEvocator(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion
    }

    public class TSBanker : Banker, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSBanker()
            : base()
        {
        }

        #region Serialization

        public TSBanker(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSBanker>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion

        public override void OnDelete()
        {
            CloseBankBoxes(Region);

            base.OnDelete();
        }

        private static void CloseBankBoxes(Region region)
        {
            foreach (Mobile m in region.Mobiles.Values)
            {
                BankBox bankBox = m.FindBankNoCreate();

                if (bankBox != null && bankBox.Opened)
                {
                    bankBox.Close();
                    m.Send(new Network.MobileUpdate(m)); // send a update packet to let client know BB is closed.
                }
            }
        }
    }

    public class TSAnimalTrainer : AnimalTrainer, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public static bool GiveTownshipDiscount(Mobile m)
        {
            foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
            {
                if (ts.IsMember(m) && ts.HasNPC(typeof(TSAnimalTrainer)))
                    return true;
            }

            return false;
        }

        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSAnimalTrainer()
            : base()
        {
        }

        #region Serialization

        public TSAnimalTrainer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSAnimalTrainer>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSStableMaster : StableMaster, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public static bool GiveTownshipDiscount(Mobile m)
        {
            foreach (TownshipStone ts in TownshipStone.AllTownshipStones)
            {
                if (ts.IsMember(m) && ts.HasNPC(typeof(TSStableMaster)))
                    return true;
            }

            return false;
        }

        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSStableMaster()
            : base()
        {
        }

        #region Serialization

        public TSStableMaster(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSStableMaster>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }
    public class TSMage : Mage, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSMage()
            : base()
        {
        }

        #region Serialization

        public TSMage(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSMage>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSAlchemist : Alchemist, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSAlchemist()
            : base()
        {
        }

        #region Serialization

        public TSAlchemist(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSAlchemist>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSProvisioner : Provisioner, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBTownshipProvisioner());

            base.InitSBInfo();
        }

        [Constructable]
        public TSProvisioner()
            : base()
        {
        }

        #region Serialization

        public TSProvisioner(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSProvisioner>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion

        private class SBTownshipProvisioner : SBInfo
        {
            private ArrayList m_BuyInfo = new InternalBuyInfo();
            private IShopSellInfo m_SellInfo = new InternalSellInfo();

            public override ArrayList BuyInfo { get { return m_BuyInfo; } }
            public override IShopSellInfo SellInfo { get { return m_SellInfo; } }

            public SBTownshipProvisioner()
            {
            }

            public class InternalBuyInfo : ArrayList
            {
                public InternalBuyInfo()
                {
                    Add(new GenericBuyInfo(typeof(TownshipBuilderTool), 12040, 20, 0xFC1, 0));
                }
            }

            public class InternalSellInfo : GenericSellInfo
            {
                public InternalSellInfo()
                {
                }
            }
        }
    }

    public class TSInnKeeper : InnKeeper, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public static bool IsInsideTownshipInn(Mobile m)
        {
            return IsInsideTownshipInn(m, BaseHouse.FindHouseAt(m));
        }

        public static bool IsInsideTownshipInn(Mobile m, BaseHouse house)
        {
            if (house == null)
                return false;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(m);

            if (tsr == null || tsr.TStone == null || tsr.TStone.IsEnemy(m))
                return false;

            foreach (Mobile npc in TownshipNPCHelper.GetHouseNPCs(house))
            {
                if (npc is TSInnKeeper)
                    return true;
            }

            return false;
        }

        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSInnKeeper()
            : base()
        {
        }

        #region Serialization

        public TSInnKeeper(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSInnKeeper>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSTownCrier : TownCrier, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSTownCrier()
            : base()
        {
        }

        #region Serialization

        public TSTownCrier(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSTownCrier>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSLookout : BaseTownshipNPC
    {
        [Constructable]
        public TSLookout()
            : base("the lookout")
        {

        }

        #region Serialization

        public TSLookout(Serial serial)
            : base(serial)
        {

        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
        }

        #endregion

        #region Lookout

        public const int LOOKOUT_RANGE = 20;
        public const double REPORT_INTERVAL = 30.0;

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            OnWatch(m);

            base.OnMovement(m, oldLocation);
        }

        private Memory m_Enemies = new Memory();

        private void OnWatch(Mobile m)
        {
            if (!m.Player || !InRange(m, LOOKOUT_RANGE) || !CanSee(m) || !InLOS(m))
                return;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(this);

            if (tsr == null || tsr.TStone == null || !tsr.TStone.IsEnemy(m))
                return;

            if (m_Enemies.Recall(m))
                return;

            m_Enemies.Remember(m, REPORT_INTERVAL);

            tsr.TStone.SendMessage(string.Format("{0} {1} reports that an enemy named {2} has been spotted near {3}.", Name, Title, m.Name, m.Location));
        }

        #endregion
    }

    public class TSFightBroker : FightBroker, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsActiveVendor { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSFightBroker()
            : base()
        {
        }

        #region Serialization

        public TSFightBroker(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSFightBroker>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            CantWalkLand = true;
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSMinstrel : HireMinstrel, ITownshipNPC
    {
        public int WanderRange { get; set; } = 10;
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        [Constructable]
        public TSMinstrel()
            : base()
        {
        }

        #region Serialization

        public TSMinstrel(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_Owner = reader.ReadMobile();
                        goto case 1;
                    }
                case 1:
                    {
                        break;
                    }
            }

            if (version < 2)
                ValidationQueue<TSMinstrel>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return ((base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from)));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }
        public override bool OnBoatTravel()
        {
            return false;
        }
        public override bool CheckMovement(Direction d, out int newZ)
        {
            return TownshipNPCHelper.CheckMovement(this, d, out newZ) && CheckTSMinstrelMovement(d, out newZ) && base.CheckMovement(d, out newZ);
        }
        public bool CheckTSMinstrelMovement(Direction d, out int newZ)
        {
            Mobile npc = this;
            int newX = npc.X;
            int newY = npc.Y;
            newZ = npc.Z;

            Movement.Movement.Offset(d, ref newX, ref newY);

            if (npc is ITownshipNPC)
                if (npc is BaseCreature bc && bc.ControlMaster != null && npc.GetDistanceToSqrt(bc.ControlMaster) < 15 && npc.Map == bc.ControlMaster.Map)
                    if (BaseBoat.FindBoatAt(bc.ControlMaster) != null || FindPlankAt(new Point3D(newX, newY, newZ)) != null)
                    {
                        Utility.SendOverheadMessage(npc, second_timeout: 10,
                            "Eww, I don't like boats.");

                        if (Utility.Chance(0.009))
                            Commands.EmoteCommand.OnCommand(new CommandEventArgs(this, "e", "puke", new string[] { "puke" }));

                        ControlOrder = OrderType.Stay;

                        return false;
                    }
            return true;
        }
        private Plank FindPlankAt(IPoint3D px)
        {
            Plank plank = (Plank)Utility.FindOneItemAt((Point3D)px, Map.Felucca, typeof(Plank), 2, false);
            if (plank != null)
                return plank;
            return null;
        }
        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion
    }

    public class TSNecromancer : BaseTownshipNPC
    {
        [Constructable]
        public TSNecromancer()
            : base("the necromancer")
        {
            Hue = 0x83E8;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new Robe(Utility.RandomBlueHue()));
            AddItem(new GnarledStaff());
        }

        public override void OnPlacement(Mobile from)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(this);

            if (tsr != null)
                tsr.Season = SeasonType.Desolation;

            base.OnPlacement(from);
        }

        #region Serialization

        public TSNecromancer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    break;
            }
        }

        #endregion
    }

    public class TSRancher : BaseTownshipNPC
    {
        public override bool IsActiveVendor { get { return true; } }

        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBTownshipRancher());
        }

        [Constructable]
        public TSRancher()
            : base("the rancher")
        {
            SetSkill(SkillName.AnimalLore, 55.0, 78.0);
            SetSkill(SkillName.AnimalTaming, 55.0, 78.0);
            SetSkill(SkillName.Herding, 64.0, 100.0);
            SetSkill(SkillName.Veterinary, 60.0, 83.0);
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(new ShepherdsCrook());
        }

        #region Serialization

        public TSRancher(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    break;
            }
        }

        #endregion

        public class SBTownshipRancher : SBInfo
        {
            private ArrayList m_BuyInfo = new InternalBuyInfo();
            private IShopSellInfo m_SellInfo = new InternalSellInfo();

            public override ArrayList BuyInfo { get { return m_BuyInfo; } }
            public override IShopSellInfo SellInfo { get { return m_SellInfo; } }

            public SBTownshipRancher()
            {
            }

            public class InternalBuyInfo : ArrayList
            {
                public InternalBuyInfo()
                {
                    Add(new AnimalBuyInfo(1, typeof(Chicken), BaseVendor.PlayerPays(typeof(Chicken)), 10, 0xD0, 0));
                    Add(new AnimalBuyInfo(1, typeof(Goat), BaseVendor.PlayerPays(typeof(Goat)), 10, 0xD1, 0));
                    Add(new AnimalBuyInfo(1, typeof(Pig), BaseVendor.PlayerPays(typeof(Pig)), 10, 0xCB, 0));
                    Add(new AnimalBuyInfo(1, typeof(Sheep), BaseVendor.PlayerPays(typeof(Sheep)), 10, 0xCF, 0));
                    Add(new AnimalBuyInfo(1, typeof(Cow), BaseVendor.PlayerPays(typeof(Cow)), 10, 0xD8, 0));
                }
            }

            public class InternalSellInfo : GenericSellInfo
            {
                public InternalSellInfo()
                {
                }
            }
        }
    }

    public class TSFarmer : Farmer, ITownshipNPC
    {
        public int WanderRange { get; set; } = 4;
        public void HandleTryTownshipExit() {; }
        public override bool CanOpenDoors { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        public override void InitSBInfo()
        {
            SBInfos.Add(new SBTownshipFarmer());

            base.InitSBInfo();
        }

        [Constructable]
        public TSFarmer()
            : base()
        {
        }

        #region Serialization

        public TSFarmer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            writer.Write((Mobile)m_Owner);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_Owner = reader.ReadMobile();
                        break;
                    }
            }

            if (version < 1)
                ValidationQueue<TSFarmer>.Enqueue(this);
        }

        public void Validate(object state)
        {
            BaseTownshipNPC.FixOwner(this, ref m_Owner);
        }

        #endregion

        #region Township NPC

        private Mobile m_Owner;

        [CommandProperty(AccessLevel.GameMaster)]
        public Mobile Owner
        {
            get { return m_Owner; }
            set { m_Owner = value; }
        }

        public override bool CheckNonlocalLift(Mobile from, Item item)
        {
            return (base.CheckNonlocalLift(from, item) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool AllowEquipFrom(Mobile from)
        {
            return (base.AllowEquipFrom(from) || TownshipNPCHelper.IsOwner(this, from));
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (base.HandlesOnSpeech(from) && TownshipNPCHelper.CanInteractWith(this, from));
        }

        public override bool CheckMovement(Direction d, out int newZ)
        {
            return (TownshipNPCHelper.CheckMovement(this, d, out newZ) && base.CheckMovement(d, out newZ));
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            TownshipNPCHelper.HandleSpeech(this, e);

            base.OnSpeech(e);
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            // do not add custom CMEs if we cannot access the vendor
            if (TownshipNPCHelper.CanInteractWith(this, from, false))
                base.AddCustomContextEntries(from, list);

            if (TownshipNPCHelper.IsOwner(this, from))
                list.Add(new TownshipNPCHelper.OptionsCME(this));
        }

        public virtual void OnPlacement(Mobile from)
        {
            if (CantWalkLand)
                RangeHome = 0;
            else
                RangeHome = WanderRange;
        }

        #endregion

        private class SBTownshipFarmer : SBInfo
        {
            private ArrayList m_BuyInfo = new InternalBuyInfo();
            private IShopSellInfo m_SellInfo = new InternalSellInfo();

            public override ArrayList BuyInfo { get { return m_BuyInfo; } }
            public override IShopSellInfo SellInfo { get { return m_SellInfo; } }

            public SBTownshipFarmer()
            {
            }

            public class InternalBuyInfo : ArrayList
            {
                public InternalBuyInfo()
                {
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Cabbage }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Carrot }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Cotton }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Flax }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Lettuce }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Onion }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Pumpkin }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Turnip }));
                    Add(new GenericBuyInfo(typeof(PackOfFarmSeeds), BaseVendor.PlayerPays(11), 20, 0x1045, 1191, new object[] { CropType.Wheat }));
                }
            }

            public class InternalSellInfo : GenericSellInfo
            {
                public InternalSellInfo()
                {
                }
            }
        }
    }
}