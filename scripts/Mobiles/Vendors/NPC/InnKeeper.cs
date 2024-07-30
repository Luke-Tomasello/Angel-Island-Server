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

/* Scripts\Mobiles\Vendors\NPC\InnKeeper.cs
 * ChangeLog
 *  1/25/2024, Adam
 *      initial creation of Stabling functionality
 *      InnKeepers will now stable hirelings, Minstrels and Fighters
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using Server.Targeting;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class InnKeeper : BaseVendor
    {
        #region Basic InnKeeper
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public InnKeeper()
            : base("the innkeeper")
        {
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBInnKeeper());
        }

        public override VendorShoeType ShoeType
        {
            get { return Utility.RandomBool() ? VendorShoeType.Sandals : VendorShoeType.Shoes; }
        }

        public InnKeeper(Serial serial)
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
        #endregion Basic InnKeeper
        #region Stable System
        public static new void Initialize()
        {
            InnStablerPersistence.EnsureExistence();
        }
        private static readonly Dictionary<Mobile, List<BaseCreature>> m_Table = new Dictionary<Mobile, List<BaseCreature>>();
        public static Dictionary<Mobile, List<BaseCreature>> Table { get { return m_Table; } }
        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool IsInvulnerable { get { return false; } }
        public IList<BaseCreature> GetStabledPets(Mobile m)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                return new BaseCreature[0];

            return list;
        }

        public virtual int GetStableSlots(Mobile m)
        {
            // TODO: Move the following method back into AnimalTrainer.cs?
            return BaseCreature.GetMaxStabled(m);
        }

        public int GetChargePerDay(Mobile m)
        {
            return UODayChargePerPet(m);
        }
        public bool CanStable(Mobile m, BaseCreature pet)
        {
            return true;
        }
        public static int UODayChargePerPet(Mobile from)
        {
            // base rate increased from OSI 30 gold per real week to 84 gold per real week
            return 1;   // 1 gp per UO day
            // return 10;  // 10 gp per UO day
        }
        public void BeginStable(Mobile from)
        {
            if (Deleted || !from.CheckAlive())
                return;

            if (GetStabledPets(from).Count >= GetStableSlots(from))
            {
                SayTo(from, "Sorry,  you have too many friends staying here already."); // You have too many pets in the stables!
            }
            else
            {
                /* I charge 30 gold per pet for a real week's stable time.
				 * I will withdraw it from thy bank account.
				 * Which animal wouldst thou like to stable here? */
                //SayTo(from, 1042558);	

                // adam: actually charge the players now.
                SayTo(from,
                    string.Format("I charge {0} gold per individual for a real day's stay. I will withdraw it from thy bank account. Which friend wouldst thou like to stay here?", GetChargePerDay(from) * 12)
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

            if (!pet.Controlled || pet.ControlMaster != from)
            {
                SayTo(from, "You do not own that!"); // You do not own that pet!
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
            else if (!pet.Body.IsHuman)
            {
                SayTo(from, "HA HA HA! Sorry, I am not a stable.");
            }
            else if (pet is not BaseHire)
            {
                SayTo(from, "Sorry, space is limited, we only accept minstrels and fighters.");
                SayTo(from, "You know, only the working class.");
            }
            //else if ((pet is PackLlama || pet is PackHorse || pet is Beetle) && (pet.Backpack != null && pet.Backpack.Items.Count > 0))
            //{
            //    SayTo(from, 1042563); // You need to unload your pet.
            //}
            else if (pet.Combatant != null && pet.InRange(pet.Combatant, 12) && pet.Map == pet.Combatant.Map)
            {
                SayTo(from, "I'm sorry.  Your friend seems to be busy.");
            }
            else if (GetStabledPets(from).Count >= GetStableSlots(from))
            {
                SayTo(from, "Sorry,  you have too many friends staying here already."); // You have too many pets in the stables!
            }
            else if (CanStable(from, pet))
            {
                int charge = GetChargePerDay(from);

                if (charge <= 0 || (from.BankBox != null && from.BankBox.ConsumeTotal(typeof(Gold), charge)))
                {
                    StablePet(from, pet);

                    SayTo(from, "Very well, thy friend may stay here. Thou mayst recover {0} by saying 'checkout' to me.", pet.Name);
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

            pet.IsInnStabled = true;
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
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late inn fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late inn fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetCreatureBool(CreatureBoolTable.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late inn fees for {1}.", pet.StableBackFees, pet.Name);
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
                NoAnimals(from);    // But I have no animals stabled with me at the moment!
        }
        public void AddStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                m_Table[m] = list = new List<BaseCreature>();

            if (!list.Contains(pet))
                list.Add(pet);

            pet.IsInnStabled = true;
        }
        public void RemoveStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                return;

            list.Remove(pet);

            pet.IsInnStabled = false;
            // don't remove from the pet cache here as the previous call to SetControlMaster just set it
        }
        public override bool HandlesOnSpeech(Mobile from)
        {
            return true;
        }
        public override void OnSpeech(SpeechEventArgs e)
        {
            if (!e.Handled && e.HasKeyword("checkin")) // *stable*
            {
                e.Handled = true;

                e.Mobile.CloseGump(typeof(ClaimListGump));

                BeginStable(e.Mobile);
            }
            else if (!e.Handled && e.HasKeyword("checkout"))    // *claim*
            {
                e.Handled = true;

                e.Mobile.CloseGump(typeof(ClaimListGump));

                if (!Insensitive.Equals(e.Speech, "checkout"))  // they said something in addition to "checkout", so we give them a list
                    BeginClaimList(e.Mobile);
                else
                    Claim(e.Mobile);
            }
            else
            {
                base.OnSpeech(e);
            }
        }
        private class ClaimListGump : Gump
        {
            private InnKeeper m_Trainer;
            private Mobile m_From;
            private ArrayList m_List;

            public ClaimListGump(InnKeeper trainer, Mobile from, ArrayList list) : base(50, 50)
            {
                m_Trainer = trainer;
                m_From = from;
                m_List = list;

                from.CloseGump(typeof(ClaimListGump));

                AddPage(0);

                AddBackground(0, 0, 325, 50 + (list.Count * 20), 9250);
                AddAlphaRegion(5, 5, 315, 40 + (list.Count * 20));

                AddHtml(15, 15, 275, 20, "<BASEFONT COLOR=#FFFFFF>Select a friend to retrieve from the inn:</BASEFONT>", false, false);

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
        private class StableTarget : Target
        {
            private InnKeeper m_Trainer;

            public StableTarget(InnKeeper trainer)
                : base(12, false, TargetFlags.None)
            {
                m_Trainer = trainer;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is BaseCreature)
                    m_Trainer.EndStable(from, (BaseCreature)targeted);
                else if (targeted == from)
                    m_Trainer.SayTo(from, "This is a poor man's inn, you would not like it."); // HA HA HA! Sorry, I am not an inn.
                else
                    m_Trainer.SayTo(from, "I beg your pardon? They can speak for themselves."); // You can't stable that!
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
                        Logger.Log(string.Format("{0} gold was taken from {2}'s bank to cover late inn fees for {1}.", pet.StableBackFees, pet.Name, from));
                        Logger.Finish();

                        SayTo(from, "{0} gold was taken from your bank cover late inn fees for {1}.", pet.StableBackFees, pet.Name);
                        pet.StableBackFees = 0;
                        pet.SetCreatureBool(CreatureBoolTable.StableHold, false);
                        goto add_pet;
                    }
                    else
                    {
                        SayTo(from, "You will need {0} gold in your bank to cover late inn fees for {1}.", pet.StableBackFees, pet.Name);
                        continue;
                    }
                }

            add_pet:
                claimable.Add(pet);
            }

            if (claimable.Count > 0)
                from.SendGump(new ClaimListGump(this, from, claimable));
            else
                NoAnimals(from); // But I have no animals stabled with me at the moment!
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
        public virtual void NoAnimals(Mobile from)
        {
            SayTo(from, "But I have none of your friends staying with with me at the moment!");    // But I have no animals stabled with me at the moment!
        }
        public virtual void AllowedFailMessage(Mobile from, BaseCreature pet, Allowed allowed)
        {
            SayTo(from, string.Format("{0} remained in the inn because you have too many followers.", pet.Name)); // ~1_NAME~ remained in the stables because you have too many followers.
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

            pet.IsInnStabled = false;
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
            Unredeemed      // you need to redeem another pet before claiming another (Stable Master)
        }
        public virtual Allowed CheckAllowed(Mobile from, BaseCreature pet)
        {
            return (from.FollowerCount + pet.ControlSlots) <= from.FollowersMax ? Allowed.Okay : Allowed.Followers;
        }
        #region Persistence
        public class InnStablerPersistence : Item, IPersistence
        {
            private static InnStablerPersistence m_Instance;
            public Item GetInstance() { return m_Instance; }
            public static void EnsureExistence()
            {
                if (m_Instance == null)
                {
                    m_Instance = new InnStablerPersistence();
                    m_Instance.IsIntMapStorage = true;
                }
            }
            public override string DefaultName
            {
                get { return "Inn Stabler Persistence - Internal"; }
            }
            [Constructable]
            public InnStablerPersistence()
                : base(0x1)
            {
                Movable = false;
            }
            public InnStablerPersistence(Serial serial)
                : base(serial)
            {
                m_Instance = this;
            }
            public override void Serialize(GenericWriter writer)
            {
                base.Serialize(writer);

                writer.Write((int)0);

                writer.Write((int)InnKeeper.Table.Count);

                foreach (KeyValuePair<Mobile, List<BaseCreature>> kvp in InnKeeper.Table)
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
                            pet.IsInnStabled = true;

                        InnKeeper.Table[m] = pets;
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
    }
}