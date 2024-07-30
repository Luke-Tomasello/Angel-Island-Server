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

/* Scripts\Engines\Township\Mobiles\StableMaster.cs
 * ChangeLog
 *  3/28/2024, Adam
 *      1. Issue a warning for grandfather pets - pets that were allowed to stay in stable even though they are not fertile (were grandfathered in.)
 *      2. Make UseChecks==false checks invisible/immovable.
 *          I suspect some clients/macros are able to retrieve the check from the players Item table and move it to the players backpack (Whip was seeing this.)
 *          I was unable to reproduce this on my client while logged in as him. Hopefully the invisible/immovable attribs will prevent this
 *  3/1/2024, Adam: (UseChecks/BreedersOnly)
 *      Add support for a version where check use is transparent.
 *      UseChecks == true: A check is placed in the player's backpack and can be redeemed for the pet after X time has elapsed.
 *      UseChecks == false: A check is added to the player's Items with a zero delay.
 *  1/19/2024, Adam
 *      Initial version. Special stable master that has a time delay on retrieval
 *      The intention is to allow more pets per player, but to disallow quickly swapping pets for PvP/PvM for instance
 */

using Server.Items;
using System;
using System.Collections.Generic;
using System.Linq;
namespace Server.Mobiles
{
    public class StableMaster : AnimalTrainer
    {
        private static readonly Dictionary<Mobile, List<BaseCreature>> m_Table = new Dictionary<Mobile, List<BaseCreature>>();
        private bool m_UseChecks = false;
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool UseChecks { get { return m_UseChecks; } set { m_UseChecks = value; } }
        private bool m_BreedersOnly = true;
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public bool BreedersOnly { get { return m_BreedersOnly; } set { m_BreedersOnly = value; } }
        public new static Dictionary<Mobile, List<BaseCreature>> Table { get { return m_Table; } }

        public static new void Initialize()
        {
            StableMasterPersistence.EnsureExistence();
        }

        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }
        public override bool IsElfStabler { get { return false; } }

        [Constructable]
        public StableMaster()
            : base("the stable master")
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
            NameHue = CalcInvulNameHue();
            InitOutfit();
        }
        [Constructable]
        public StableMaster(string title)
            : base(title)
        {
            SetSkill(SkillName.AnimalLore, 64.0, 100.0);
            SetSkill(SkillName.AnimalTaming, 90.0, 100.0);
            SetSkill(SkillName.Veterinary, 65.0, 88.0);
            NameHue = CalcInvulNameHue();
            InitOutfit();
        }
        public override bool CheckPetCondidtion(Mobile from, BaseCreature pet, bool message = true)
        {
            if (BreedersOnly && pet.BreedingParticipant)
                return true;

            SayTo(from, "{0} must be fertile to be stabled here", pet.SafeName);

            return false;
        }
        private bool DidTheyGetTheCheck(Mobile from)
        {
            if (!UseChecks)
            {
                foreach (Item item in from.Items)
                    if (item.GetType() == typeof(StableMasterClaimCheck))
                        return true;
                return false;
            }
            return from.Backpack.FindItemByType(typeof(StableMasterClaimCheck)) != null;
        }

        private bool FindPet(Mobile from, Mobile pet)
        {
            foreach (Item item in from.Items)
                if (item.GetType() == typeof(StableMasterClaimCheck))
                    if ((item as StableMasterClaimCheck).Pet == pet)
                        return true;

            return false;
        }
        private StableMasterClaimCheck GetCheck(Mobile from)
        {
            System.Diagnostics.Debug.Assert(!UseChecks);    // GetCheck not used when UseChecks == true
            if (!UseChecks)
            {
                // backwards compatibility. If they come up to us with a check in their backpack, convert it to a 'no checks' model
                Item temp = from.Backpack.FindItemByType(typeof(StableMasterClaimCheck));
                if (temp != null)
                {
                    from.Backpack.RemoveItem(temp);
                    if (!FindPet(from, (temp as StableMasterClaimCheck).Pet))
                    {
                        StableMasterClaimCheck smc = new StableMasterClaimCheck((temp as StableMasterClaimCheck).Pet, DateTime.UtcNow);
                        System.Diagnostics.Debug.Assert(smc != null);
                        if (smc != null)
                        {
                            smc.Visible = false;
                            smc.Movable = false;
                            from.AddItem(smc);
                        }
                    }
                    temp.Delete();
                }

                foreach (Item item in from.Items)
                    if (item.GetType() == typeof(StableMasterClaimCheck))
                        return item as StableMasterClaimCheck;
                return null;
            }

            return null;
        }
        public override void HereYouGo(Mobile from)
        {
            TimeSpan delay = TimeSpan.Zero;
            if (UseChecks)
            {
                if (Core.Debug)
                    delay = TimeSpan.FromMinutes(2);
                else
                    TimeSpan.FromMinutes(10);
            }

            if (UseChecks)
            {
                //SayTo(from, 1042559); // Here you go... and good day to you!
                if (DidTheyGetTheCheck(from))
                {
                    SayTo(from, "I've placed a claim check for your pet in your backpack");
                    SayTo(from, "Just bring it back to me in about {0} minutes and I'll have your pet ready", delay.Minutes.ToString("0.0"));
                }
                else
                {
                    SayTo(from, "I tried to placed a claim check in your backpack but it was full");
                    SayTo(from, "I dropped at your feet instead");
                }
            }
            else
            {
                SayTo(from, 1042559); // Here you go... and good day to you!
                OnDragDrop(from, GetCheck(from));
            }
        }
        public override void StablePet(Mobile from, BaseCreature pet)
        {
            base.StablePet(from, pet);
            pet.SetCreatureBool(CreatureBoolTable.IsStableMasterStabled, true);
        }
        public override object PetObject(Mobile from, BaseCreature pet)
        {
            TimeSpan delay = TimeSpan.Zero;
            if (UseChecks)
            {
                if (Core.Debug)
                    delay = TimeSpan.FromMinutes(2);
                else
                    TimeSpan.FromMinutes(10);
            }
            StableMasterClaimCheck check = null;
            if (!UseChecks)
            {
                check = GetCheck(from);
                if (check == null)
                {
                    check = new StableMasterClaimCheck(pet, DateTime.UtcNow + delay);
                    pet.SetCreatureBool(CreatureBoolTable.IsSMDeeded, true);
                }
            }
            else
            {
                check = new StableMasterClaimCheck(pet, DateTime.UtcNow + delay);
                pet.SetCreatureBool(CreatureBoolTable.IsSMDeeded, true);
            }
            return check;
        }
        public override void GivePetObject(Mobile from, object pet)
        {
            if (UseChecks)
            {
                if (from.AddToBackpack(pet as StableMasterClaimCheck) == false)
                {
                    ; // error debug
                }
            }
            else
            {
                StableMasterClaimCheck smc = pet as StableMasterClaimCheck;
                System.Diagnostics.Debug.Assert(smc != null);
                if (smc != null)
                {
                    smc.Visible = false;
                    smc.Movable = false;
                    from.AddItem(smc);
                }
            }
        }
        public override Allowed CheckAllowed(Mobile from, BaseCreature pet)
        {
            Allowed unredeemed = from.Pets.Where(p => (p as BaseCreature).GetCreatureBool(CreatureBoolTable.IsSMDeeded)).FirstOrDefault<Mobile>() == null ? Allowed.Okay : Allowed.Unredeemed;
            Allowed followers = (from.FollowerCount + pet.ControlSlots) <= from.FollowersMax ? Allowed.Okay : Allowed.Followers;
            if (followers != Allowed.Okay)
                return followers;

            if (UseChecks)
                if (unredeemed != Allowed.Okay)
                    return unredeemed;

            return Allowed.Okay;
        }
        public override void AllowedFailMessage(Mobile from, BaseCreature pet, Allowed allowed)
        {
            if (allowed == Allowed.Followers)
                SayTo(from, 1049612, pet.Name); // ~1_NAME~ remained in the stables because you have too many followers.
            else if (allowed == Allowed.Unredeemed)
            {
                Mobile m = from.Pets.Where(p => (p as BaseCreature).GetCreatureBool(CreatureBoolTable.IsSMDeeded)).FirstOrDefault<Mobile>();
                SayTo(from, "You must first redeem your claim check for {0}", m.Name);
            }
            else
            {
                SayTo(from, "Unknown condition: {0}", allowed);
            }
        }
        public override int GetChargePerDay(Mobile m)
        {
            return UODayChargePerPet(m);
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
            Item item = Utility.RandomBool() ? (Item)new QuarterStaff() : (Item)new ShepherdsCrook();
            AddItem(item);
            EquipItem(item);
        }
        public override int GetStableSlots(Mobile m, BaseCreature bc = null)
        {
            return base.GetStableSlots(m);
        }
        public override IList<BaseCreature> GetStabledPets(Mobile m, BaseCreature bc = null)
        {

            return GetStabledPets(m, StableMaster.Table, bc);

            //List<BaseCreature> list;

            //if (!m_Table.TryGetValue(m, out list))
            //    return new BaseCreature[0];

            //return list;
        }
        public override void AddStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                m_Table[m] = list = new List<BaseCreature>();

            if (!list.Contains(pet))
                list.Add(pet);

            //IsStabled = true;
            pet.IsStableMasterStabled = true;
        }
        public override void RemoveStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                return;

            list.Remove(pet);

            pet.IsStableMasterStabled = false;
            // don't remove from the pet cache here as the previous call to SetControlMaster just set it
        }
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            StableMasterClaimCheck check = dropped as StableMasterClaimCheck;
            if (check != null)
            {
                if (DateTime.UtcNow >= check.RedeemableOn || !UseChecks)
                {
                    if (check.Pet != null && check.Pet.ControlMasterGUID == from.GUID)
                    {
                        // Warn the player about grandfathered pets
                        if (check.Pet.BreedingParticipant == false)
                            SayTo(from, "This pet was grandfathered into my stables and won't be allowed back in until {0} is fertile", check.Pet.Female ? "she" : "he");

                        // configure the pet, and move to world
                        base.GivePetObject(from, base.PetObject(from, check.Redeem(from)));
                        //base.HereYouGo(from);
                        check.Delete();
                        return true;
                    }
                    else if (check.Pet != null && check.Pet.ControlMasterGUID != from.GUID)
                    {
                        SayTo(from, "I'm sorry, but this is not your pet.");
                        return false;
                    }
                    else
                    {
                        from.SendMessage("Something happened to your pet. Please contact a GM.");
                        return false;
                    }
                }
                else if (dropped is StableMasterClaimCheck badCheck)
                {
                    TimeSpan ts = badCheck.RedeemableOn - DateTime.UtcNow;
                    CuteChatter(badCheck, from);
                    SayWhen(ts, from);
                    return false;
                }
            }
            return base.OnDragDrop(from, dropped);
        }
        private void CuteChatter(StableMasterClaimCheck check, Mobile from)
        {
            string name = check.Pet.Name == check.Pet.GetType().Name ? "your pet" : check.Pet.Name;
            switch (Utility.Random(5))
            {
                case 0:
                    SayTo(from, "We're just finishing up on {0}.", name);
                    break;
                case 1:
                    SayTo(from, "{0}'s still gotta get brushed out.", name);
                    break;
                case 2:
                    SayTo(from, "{0}'s still gotta eat.", name);
                    break;
                case 3:
                    SayTo(from, "just watering {0} now.", name);
                    break;
                case 4:
                    SayTo(from, "The stable boy is on his way back now with {0}.", name);
                    break;
            }
        }
        private void SayWhen(TimeSpan ts, Mobile from)
        {
            //TimeSpan ts = m_RedeemableOn - DateTime.UtcNow;
            //if (DateTime.UtcNow > m_RedeemableOn)
            //    from.SendMessage("Your pet is ready now.");
            //else if (ts.TotalMinutes > 0)
            //    from.SendMessage("Your pet will be ready in {0:f2} minutes.", ts.TotalMinutes);

            if (ts.TotalMinutes > 0)
                from.SendMessage("Your pet will be ready in {0:f2} minutes.", ts.TotalMinutes);
            else
                from.SendMessage("Your pet is ready now.");
        }
        public StableMaster(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 2;
            writer.Write(version);

            // version 2
            writer.Write(m_UseChecks);

            // version 1
            writer.Write(m_BreedersOnly);
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                    {
                        m_UseChecks = reader.ReadBool();
                        goto case 1;
                    }
                case 1:
                    {
                        m_BreedersOnly = reader.ReadBool();
                        goto case 0;
                    }
                case 0:
                    break;
            }
        }
    }
    public class StableMasterPersistence : Item, IPersistence
    {
        private static StableMasterPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureExistence()
        {
            if (m_Instance == null)
            {
                m_Instance = new StableMasterPersistence();
                m_Instance.IsIntMapStorage = true;
            }
        }
        public override string DefaultName
        {
            get { return "Stabler Master Persistence - Internal"; }
        }
        [Constructable]
        public StableMasterPersistence()
            : base(0x1)
        {
            Movable = false;
        }
        public StableMasterPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write((int)StableMaster.Table.Count);

            foreach (KeyValuePair<Mobile, List<BaseCreature>> kvp in StableMaster.Table)
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
                        pet.IsStableMasterStabled = true;

                    StableMaster.Table[m] = pets;
                }
                else
                {
                    foreach (BaseCreature pet in pets)
                        pet.Delete();
                }
            }
        }
    }
    public class StableMasterClaimCheck : Item
    {
        public override string DefaultName { get { return "a pet claim check"; } }

        private BaseCreature m_Pet;
        private DateTime m_RedeemableOn = DateTime.MaxValue;
        public DateTime RedeemableOn => m_RedeemableOn;
        [CommandProperty(AccessLevel.GameMaster)]
        public BaseCreature Pet
        {
            get { return m_Pet; }
            set { m_Pet = value; }
        }
        [Constructable]
        public StableMasterClaimCheck(BaseCreature pet, DateTime redeemableOn)
            : base(0x14F0)
        {
            Hue = 0x6C9;
            LootType = LootType.Newbied;
            m_Pet = pet;
            m_RedeemableOn = redeemableOn;
        }
        [Constructable]
        public StableMasterClaimCheck()
            : this(null, DateTime.MaxValue)
        {
        }
        public BaseCreature Redeem(Mobile from)
        {
            if (m_Pet != null)
            {
                BaseCreature temp = m_Pet;
                m_Pet = null;
                m_RedeemableOn = DateTime.MaxValue;
                temp.SetCreatureBool(CreatureBoolTable.IsStableMasterStabled, false);
                temp.SetCreatureBool(CreatureBoolTable.IsSMDeeded, false);
                return temp;
            }
            else
                ; // error

            return null;
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            if (m_Pet != null)
            {
                LabelTo(from, String.Format("a claim check for {0}", m_Pet.Name));
                SayWhen(from);
            }
            else
                LabelTo(from, "an unassigned pet claim check");
        }
        public override void OnDoubleClick(Mobile from)
        {
            from.SendMessage("Give this to a stable master to claim your pet.");
            SayWhen(from);
        }
        private void SayWhen(Mobile from)
        {
            if (DateTime.UtcNow > m_RedeemableOn)
                from.SendMessage("Your pet is ready now.");
            else
            {   // not serializing DateTime.UtcNow?
                TimeSpan ts = m_RedeemableOn - DateTime.UtcNow;
                from.SendMessage("Your pet will be ready in {0:f2} minutes.", ts.TotalMinutes);
            }
        }
        public StableMasterClaimCheck(Serial serial)
            : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);

            switch (version)
            {
                case 0:
                    {
                        writer.Write(m_Pet);
                        break;
                    }
            }
        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_Pet = (BaseCreature)reader.ReadMobile();
                        if (m_Pet == null)
                            m_RedeemableOn = DateTime.MaxValue;
                        break;
                    }
            }
        }
    }
}