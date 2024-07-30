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

/* Scripts\Mobiles\Vendors\NPC\AnimalTrainer.cs
 * ChangeLog
 * 3/22/23, Adam (FindMyPet)
 *      Called from BaseAI.DoIntelligentDialog(), animal trainers can now locate your lost pet. 
 *      Basically a poor man's TreeOfKnowledge (For Siege)
 * 5/10/10, adam
 *		Stable Fees: Redesign stable fees as follows:
 *		(1) up basic stabling fee from 30 gold per week (.357 gold per UO day) to 84 gold per week (1 gold per UO day)
 *		(2) Actually charge the above amount once per UO day
 *		(3) Allow virtually unlimited *additional* stable slots for GM herding (up to 256) for 10 gp per UO day
 *		(4) Allow #3 above at a 50% discount (5gp per UO day) if (A) you belong to a township, and (B) your township has a stable master
 *		(5) if the gold cannot be collected automatically, create a tab for the player and require payment when ANY pets are claimed.
 * 12/15/04, Pix
 *		Stopped bretheren from being stabled.
 * 10/7/04, Pix
 *		Let dead pets be stabled.
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 *	4/10/04 changes by mith
 *		When we put a GetMaxStabled function in BaseCreature for AI Auto-Stabling, the GetMaxStabled code here was overriden.
 *		Removed the GetMaxStabled function from here, as it was just a copy of what's in BaseCreature.
 */

using Server.ContextMenus;
using Server.Engines;
using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Server.Mobiles
{
    public class AnimalTrainer : BaseVendor
    {
        #region Stable System
        public static new void Initialize()
        {
            AnimalTrainerPersistence.EnsureExistence();
        }
        private static readonly Dictionary<Mobile, List<BaseCreature>> m_Table = new Dictionary<Mobile, List<BaseCreature>>();
        public static Dictionary<Mobile, List<BaseCreature>> Table { get { return m_Table; } }
        #region Persistence
        public class AnimalTrainerPersistence : Item, IPersistence
        {
            private static AnimalTrainerPersistence m_Instance;
            public Item GetInstance() { return m_Instance; }
            public static void EnsureExistence()
            {
                if (m_Instance == null)
                {
                    m_Instance = new AnimalTrainerPersistence();
                    m_Instance.IsIntMapStorage = true;
                }
            }
            public override string DefaultName
            {
                get { return "AnimalTrainer Persistence - Internal"; }
            }
            [Constructable]
            public AnimalTrainerPersistence()
                : base(0x1)
            {
                Movable = false;
            }
            public AnimalTrainerPersistence(Serial serial)
                : base(serial)
            {
                m_Instance = this;
            }
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0);

                writer.Write((int)AnimalTrainer.Table.Count);

                foreach (KeyValuePair<Mobile, List<BaseCreature>> kvp in AnimalTrainer.Table)
                {
                    writer.Write((Mobile)kvp.Key);
                    writer.WriteMobileList(kvp.Value);
                }
            }
            public override void Deserialize(GenericReader reader)
            {
                base.Deserialize(reader);

                int version = reader.ReadInt();

                int count = reader.ReadInt();

                for (int i = 0; i < count; i++)
                {
                    Mobile m = reader.ReadMobile();
                    List<BaseCreature> pets = reader.ReadStrongMobileList<BaseCreature>();

                    if (m != null)
                    {
                        foreach (BaseCreature pet in pets)
                            pet.IsAnimalTrainerStabled = true;

                        AnimalTrainer.Table[m] = pets;
                    }
                    else
                    {
                        foreach (BaseCreature pet in pets)
                            pet.Delete();
                    }
                }
            }
        }
        #endregion Persistence
        #endregion Stable System

        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        /* Publish 4 on March 8, 2000
         * Shopkeeper Changes
         * NPC shopkeepers will no longer have colored sandals. Evil NPC Mages will carry these items.
         * NPC shopkeepers will give a murder count when they die unless they are criminal or evil. The issue with murder counts from NPCs not decaying (as reported on Siege Perilous) will also be addressed.
         * If a shopkeeper is killed, a new shopkeeper will appear as soon as another player (other than the one that killed it) approaches.
         * Any shopkeeper that is currently [invulnerable] will lose that status except for stablemasters.
         * https://www.uoguide.com/Publish_4
         */
        public override bool IsInvulnerable { get { return true; } }
        public virtual bool IsElfStabler { get { return false; } }

        [Constructable]
        public AnimalTrainer()
            : base("the animal trainer")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
        }
        [Constructable]
        public AnimalTrainer(string title)
            : base(title)
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
        }
        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBAnimalTrainer());
        }

        public override VendorShoeType ShoeType
        {
            get { return Female ? VendorShoeType.ThighBoots : VendorShoeType.Boots; }
        }

        public override int GetShoeHue()
        {
            return 0;
        }

        public override void InitOutfit()
        {
            base.InitOutfit();

            AddItem(Utility.RandomBool() ? (Item)new QuarterStaff() : (Item)new ShepherdsCrook());
        }

        private class StableEntry : ContextMenuEntry
        {
            private AnimalTrainer m_Trainer;
            private Mobile m_From;

            public StableEntry(AnimalTrainer trainer, Mobile from)
                : base(6126, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.BeginStable(m_From);
            }
        }

        private class ClaimListGump : Gump
        {
            private AnimalTrainer m_Trainer;
            private Mobile m_From;
            private ArrayList m_List;

            public ClaimListGump(AnimalTrainer trainer, Mobile from, ArrayList list) : base(50, 50)
            {
                m_Trainer = trainer;
                m_From = from;
                m_List = list;

                from.CloseGump(typeof(ClaimListGump));

                AddPage(0);

                AddBackground(0, 0, 325, 50 + (list.Count * 20), 9250);
                AddAlphaRegion(5, 5, 315, 40 + (list.Count * 20));

                AddHtml(15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a pet to retrieve from the stables:</BASEFONT>", false, false);

                for (int i = 0; i < list.Count; ++i)
                {
                    BaseCreature pet = list[i] as BaseCreature;

                    if (pet == null || pet.Deleted)
                        continue;

                    AddButton(15, 39 + (i * 20), 10006, 10006, i + 1, GumpButtonType.Reply, 0);
                    AddHtml(32, 35 + (i * 20), 275, 18, String.Format("<BASEFONT COLOR=#C0C0EE>{0}</BASEFONT>", pet.Name), false, false);
                }
            }

            public override void OnResponse(NetState sender, RelayInfo info)
            {
                int index = info.ButtonID - 1;

                if (index >= 0 && index < m_List.Count)
                    m_Trainer.EndClaimList(m_From, m_List[index] as BaseCreature);
            }
        }

        private class ClaimAllEntry : ContextMenuEntry
        {
            private AnimalTrainer m_Trainer;
            private Mobile m_From;

            public ClaimAllEntry(AnimalTrainer trainer, Mobile from)
                : base(6127, 12)
            {
                m_Trainer = trainer;
                m_From = from;
            }

            public override void OnClick()
            {
                m_Trainer.Claim(m_From);
            }
        }

        public override void AddCustomContextEntries(Mobile from, ArrayList list)
        {
            if (from.Alive)
            {
                list.Add(new StableEntry(this, from));

                if (GetStabledPets(from).Count > 0)
                    list.Add(new ClaimAllEntry(this, from));
            }

            base.AddCustomContextEntries(from, list);
        }

        private class StableTarget : Target
        {
            private AnimalTrainer m_Trainer;

            public StableTarget(AnimalTrainer trainer)
                : base(12, false, TargetFlags.None)
            {
                m_Trainer = trainer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature)
                    m_Trainer.EndStable(from, (BaseCreature)targeted);
                else if (targeted == from)
                    m_Trainer.SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
                else
                    m_Trainer.SayTo(from, 1048053); // You can't stable that!
            }
        }

        public void BeginClaimList(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            IList<BaseCreature> list = GetStabledPets(from);

            ArrayList claimable = new ArrayList();

            for (int i = 0; i < list.Count; ++i)
            {
                BaseCreature pet = list[i];

                // tell the player they need to pay the back stable fees
                if (pet.GetCreatureBool(CreatureBoolTable.StableHold))
                {
                    // charge the player back stable fees
                    if ((from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), pet.StableBackFees) == true))
                    {
                        Server.Diagnostics.LogHelper Logger = new Server.Diagnostics.LogHelper("PetHoldFromStables.log", false, true);
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetCreatureBool(CreatureBoolTable.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        continue;
                    }
                }

            add_pet:
                claimable.Add(pet);
            }

            if (claimable.Count > 0)
                from.SendGump(new ClaimListGump(this, from, claimable));
            else
                NoAnimals(from);        // But I have no animals stabled with me at the moment!
        }

        public void EndClaimList(Mobile from, BaseCreature pet)
        {
            if (pet == null || pet.Deleted || pet.GetCreatureBool(CreatureBoolTable.StableHold) || from.Map != this.Map || !from.InRange(this, 14) || !GetStabledPets(from).Contains(pet) || !from.CheckAlive())
                return;

            Allowed status = CheckAllowed(from, pet);
            if (status == Allowed.Okay)
            {
                // configure the pet, and move to world
                GivePetObject(from, PetObject(from, pet));

                HereYouGo(from); // Here you go... and good day to you!
            }
            else
            {
                AllowedFailMessage(from, pet, status); // ~1_NAME~ remained in the stables because you have too many followers.
            }
        }

        /*
		 * Non-Township Stable Bonus:
		 * Tamer w/ Herding = Unlimited stable slots
		 * Cost = 10gp per pet, per UO day for pets over normal slot count.
		 * Township Stable Bonus/Incentive:
		 * Tamer w/ Herding = Unlimited stable slots
		 * Cost = 5gp per pet, per UO day for pets over normal slot count.
		 */
        public static int UODayChargePerPet(Mobile from)
        {
            List<BaseCreature> list = new List<BaseCreature>();
            if (AnimalTrainer.Table.ContainsKey(from))
                list = AnimalTrainer.Table[from];

            // base rate increased from OSI 30 gold per real week to 84 gold per real week
            if (list.Count < GetOSIMaxStabled(from))
                return 1;   // 1 gp per UO day

            // township discount?
            if (TSAnimalTrainer.GiveTownshipDiscount(from) || TSStableMaster.GiveTownshipDiscount(from))
                return 5;   // 5 gp per UO day

            return 10;  // 10 gp per UO day
        }

        public static int UODayChargePerPet(Mobile from, int slot)
        {
            // base rate increased from OSI 30 gold per real week to 84 gold per real week
            if (slot < GetOSIMaxStabled(from))
                return 1;   // 1 gp per UO day

            // township discount?
            if (TSAnimalTrainer.GiveTownshipDiscount(from) || TSStableMaster.GiveTownshipDiscount(from))
                return 5;   // 5 gp per UO day

            return 10;  // 10 gp per UO day
        }

        public void BeginStable(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            //if (GetStabledPets(from).Count >= GetStableSlots(from))
            //{
            //    SayTo(from, 1042565); // You have too many pets in the stables!
            //}
            //else
            {
                /* I charge 30 gold per pet for a real week's stable time.
				 * I will withdraw it from thy bank account.
				 * Which animal wouldst thou like to stable here? */
                //SayTo(from, 1042558);	

                // adam: actually charge the players now.
                SayTo(from,
                    string.Format("I charge {0} gold per pet for a real day's stable time. I will withdraw it from thy bank account. Which animal wouldst thou like to stable here?", GetChargePerDay(from) * 12)
                    );

                from.Target = new StableTarget(this);
            }
        }
        public virtual bool CheckPetCondidtion(Mobile from, BaseCreature pet, bool message = true)
        {
            return true;
        }
        public void EndStable(Mobile from, BaseCreature pet)
        {
            if (Deleted || !from.CheckAlive())
                return;

            if (GetStabledPets(from, pet).Count >= GetStableSlots(from, pet))
            {
                SayTo(from, 1042565); // You have too many pets in the stables!
            }
            else if (!pet.Controlled || pet.ControlMaster != from)
            {
                SayTo(from, 1042562); // You do not own that pet!
            }
            else if (pet is IMount && ((IMount)pet).Rider != null)
            {
                SayTo(from, 1042561); // Please dismount first.
            }
            //Pix: 10/7/2004 - allow dead pets to be stabled.
            //else if ( pet.IsDeadPet )
            //{
            //	SayTo( from, 1049668 ); // Living pets only, please.
            //}
            else if (pet.Summoned)
            {
                SayTo(from, 502673); // I can not stable summoned creatures.
            }
            //else if (pet.IOBFollower) // Don't stable IOB brethren
            //{
            //    SayTo(from, "You can't stable your brethren!");
            //}
            else if (!CheckPetCondidtion(from, pet, message: true))
            {
                // message already shown
            }
            else if (pet.Body.IsHuman)
            {
                SayTo(from, 502672); // HA HA HA! Sorry, I am not an inn.
            }
            else if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && (pet.Backpack != null && pet.Backpack.Items.Count > 0))
            {
                SayTo(from, 1042563); // You need to unload your pet.
            }
            else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
            {
                SayTo(from, 1042564); // I'm sorry.  Your pet seems to be busy.
            }
            //else if (GetStabledPets(from).Count >= GetStableSlots(from))
            //{
            //    SayTo(from, 1042565); // You have too many pets in the stables!
            //}
            else if (CanStable(from, pet))
            {
                int charge = GetChargePerDay(from);

                if (charge <= 0 || (from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), charge)))
                {
                    StablePet(from, pet);

                    SayTo(from, "Very well, thy pet is stabled. Thou mayst recover it by saying 'claim' to me.");
                    // We don't 'sell off' pets after one week.
                    // SayTo(from, 502679); // Very well, thy pet is stabled. Thou mayst recover it by saying 'claim' to me. In one real world week, I shall sell it off if it is not claimed!
                }
                else
                {
                    SayTo(from, 502677); // But thou hast not the funds in thy bank account!
                }
            }
        }

        public virtual void StablePet(Mobile from, BaseCreature pet)
        {
            pet.ControlTarget = null;
            pet.ControlOrder = OrderType.Stay;

            pet.Internalize();

            pet.SetControlMaster(null);
            pet.SummonMaster = null;

            pet.IsAnimalTrainerStabled = true;

            AddStabledPet(from, pet);

            pet.LastStableChargeTime = DateTime.UtcNow;
        }

        public void Claim(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            IList<BaseCreature> list = GetStabledPets(from);

            BaseCreature[] array = new BaseCreature[list.Count];

            list.CopyTo(array, 0);

            bool claimed = false;
            int stabled = 0;
            bool indebt = false;
            for (int i = 0; i < array.Length; ++i)
            {
                BaseCreature pet = array[i];

                // tell the player they need to pay the back stable fees
                if (pet.GetCreatureBool(CreatureBoolTable.StableHold))
                {
                    // charge the player back stable fees
                    if ((from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), pet.StableBackFees) == true))
                    {
                        Server.Diagnostics.LogHelper Logger = new Server.Diagnostics.LogHelper("PetHoldFromStables.log", false, true);
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetCreatureBool(CreatureBoolTable.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late stable fees for {1}.", pet.StableBackFees, pet.Name);
                        indebt = true;
                        continue;
                    }
                }

            add_pet:
                ++stabled;

                Allowed status = CheckAllowed(from, pet);
                if (status == Allowed.Okay)
                {
                    // configure the pet, and move to world
                    GivePetObject(from, PetObject(from, pet));

                    claimed = true;
                }
                else
                {
                    AllowedFailMessage(from, pet, status); // ~1_NAME~ remained in the stables because you have too many followers.
                }
            }

            if (claimed)
                HereYouGo(from);        // Here you go... and good day to you!
            else if (stabled == 0 && !indebt)
                NoAnimals(from);        // But I have no animals stabled with me at the moment!
        }
        public virtual void NoAnimals(Mobile from)
        {
            SayTo(from, 502671);    // But I have no animals stabled with me at the moment!
        }
        public virtual void AllowedFailMessage(Mobile from, BaseCreature pet, Allowed allowed)
        {
            SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
        }
        public virtual void HereYouGo(Mobile from)
        {
            SayTo(from, 1042559); // Here you go... and good day to you!
        }
        public virtual object PetObject(Mobile from, BaseCreature pet)
        {
            pet.SetControlMaster(from);

            if (pet.Summoned)
                pet.SummonMaster = from;

            pet.ControlTarget = from;
            pet.ControlOrder = OrderType.Follow;

            pet.IsAnimalTrainerStabled = false;

            RemoveStabledPet(from, pet);

            pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize

            return pet;
        }
        public virtual void GivePetObject(Mobile from, object pet)
        {
            (pet as BaseCreature).MoveToWorld(from.Location, from.Map);
        }
        public enum Allowed
        {
            Okay,           // all is well
            Followers,      // too many followers
            Unredeemed,     // you need to redeem another pet before claiming another (Stable Master)
            BreedersOnly    // only fertile pets may be stabled 
        }
        public virtual Allowed CheckAllowed(Mobile from, BaseCreature pet)
        {
            return (from.FollowerCount + pet.ControlSlots) <= from.FollowersMax ? Allowed.Okay : Allowed.Followers;
        }
        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.HasKeyword(0x0008))
            {
                e.Handled = true;

                e.Mobile.CloseGump(typeof(ClaimListGump));

                BeginStable(e.Mobile);
            }
            else if (!e.Handled && e.HasKeyword(0x0009))
            {
                e.Handled = true;

                e.Mobile.CloseGump(typeof(ClaimListGump));

                if (!Insensitive.Equals(e.Speech, "claim"))
                    BeginClaimList(e.Mobile);
                else
                    Claim(e.Mobile);
            }
            else
            {
                base.OnSpeech(e);
            }
        }

        public static void FindMyPet(Mobile me, SpeechEventArgs e)
        {
            var match_condition = new Regex(".*pet\\s+(.*)$").Match(e.Speech);
            if (!match_condition.Success || match_condition.Groups.Count != 2)
            {   // missing pet name
                me.Say("I am sorry, what?");
            }
            else
            {
                string petName = match_condition.Groups[1].ToString().Trim();
                BaseCreature bc = Utility.FindPet(e.Mobile, petName);
                if (bc == null)
                    me.Say("I am sorry, but I could not find {0}.", petName);
                else
                {
                    // TODO: Seems like creatures should also have a notion of WorldLocation.
                    // Add GetWorldLocation() to IEntity?
                    Point3D worldLoc = bc.Location;

                    if (bc is IMount)
                    {
                        IMount mount = (IMount)bc;

                        if (mount.Rider != null)
                            worldLoc = mount.Rider.Location;
                    }

                    // public static void ConvoExit(Point3D closest, Mobile ConvoMob, Mobile ConvoPlayer, string thing, string NPCName)
                    IntelligentDialogue.ConvoExit(worldLoc, me, e.Mobile, petName, null);
                }
            }
        }

        public virtual IList<BaseCreature> GetStabledPets(Mobile m, BaseCreature bc = null)
        {
            return GetStabledPets(m, AnimalTrainer.Table, bc);
        }
        public virtual IList<BaseCreature> GetStabledPets(Mobile m, Dictionary<Mobile, List<BaseCreature>> Table, BaseCreature bc = null)
        {
            List<BaseCreature> list = new List<BaseCreature>();
            bool hasPets = Table.ContainsKey(m);
            for (int i = 0; hasPets && i < Table[m].Count; i++)
            {
                BaseCreature pet = Table[m][i] as BaseCreature;

                if (pet == null)
                {
                    Table[m].RemoveAt(i);
                    i--;
                    continue;
                }

                if (pet.Deleted)
                {
                    pet.IsAnimalTrainerStabled = false;
                    pet.LastStableChargeTime = DateTime.MinValue;   // When set to MinValue, we don't serialize
                    Table[m].RemoveAt(i);
                    i--;
                    continue;
                }

                list.Add(pet);
            }

            // don't count chickens and golems against normal pet slot counts
            if (bc is not Golem && bc is not Chicken)
            {   // list oldest pets first (old friends)
                list = list.OrderByDescending(x => x.Created).ToList();
                // remove chickens and golems from the list
                if (bc != null)
                    list = list.Where(el => el.GetType() != typeof(Golem) && el.GetType() != typeof(Chicken)).ToList();
                return list;
            }

            // Golems and Chickens
            List<BaseCreature> special = list.Where(el => el.GetType() == bc.GetType()).ToList();
            return special;
        }
        public virtual void AddStabledPet(Mobile m, BaseCreature pet)
        {
            if (AnimalTrainer.Table.ContainsKey(m))
            {
                if (!AnimalTrainer.Table[m].Contains(pet))
                    AnimalTrainer.Table[m].Add(pet);
            }
            else
                AnimalTrainer.Table.Add(m, new List<BaseCreature>() { pet });
        }

        public virtual void RemoveStabledPet(Mobile m, BaseCreature pet)
        {
            if (AnimalTrainer.Table.ContainsKey(m))
                if (AnimalTrainer.Table[m].Contains(pet))
                    AnimalTrainer.Table[m].Remove(pet);
        }

        public virtual int GetStableSlots(Mobile m, BaseCreature bc = null)
        {
            // TODO: Move the following method back into AnimalTrainer.cs?
            return BaseCreature.GetMaxStabled(m, bc);
        }

        public virtual int GetChargePerDay(Mobile m)
        {
            return UODayChargePerPet(m);
        }

        public virtual bool CanStable(Mobile m, BaseCreature pet)
        {
            return true;
        }

        public AnimalTrainer(Serial serial)
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
    }
}