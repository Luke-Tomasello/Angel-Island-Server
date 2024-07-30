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

/* Scripts\Mobiles\Special\ElfStabler.cs
 * ChangeLog
 *  1/20/2024, Adam
 *      Make consistent use of IsStabled
 *      Add stable domain, i.e., IsElfStabled = true;
 *      Add these stabled pets to the global pet cache. I.e., Mobile.PetCache.add(pet))
 *      Move ElfStabler pet-checks out of Animal Trainer and place in an override for handling implementation specific checks.
 *  1/4/24, Yoar
 *      Elf stablers no longer charge stable fees.
 *  12/9/23, Yoar
 *      Initial version. Special stable master that only keeps elf stablable pets.
 */

using Server.Items;
using System.Collections.Generic;

namespace Server.Mobiles
{
    public class ElfStabler : AnimalTrainer
    {
        private static readonly Dictionary<Mobile, List<BaseCreature>> m_Table = new Dictionary<Mobile, List<BaseCreature>>();

        public new static Dictionary<Mobile, List<BaseCreature>> Table { get { return m_Table; } }

        public static new void Initialize()
        {
            ElfStablerPersistence.EnsureExistence();
        }

        public override bool ClickTitle { get { return true; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool IsInvulnerable { get { return true; } }

        public override bool IsElfStabler { get { return true; } }

        [Constructable]
        public ElfStabler()
            : base()
        {
            Title = "the reindeer keeper";

            ElfHelper.InitBody(this);
            ElfHelper.InitOutfit(this);

            NameHue = CalcInvulNameHue();
            // 1/9/2024: Adam, no blessed. Use Invulnerable instead
            //Blessed = true;
        }

        [Constructable]
        public ElfStabler(string title)
            : base(title)
        {

        }

        public override int GetChargePerDay(Mobile m)
        {
            return 0;
        }

        public override IList<BaseCreature> GetStabledPets(Mobile m, BaseCreature bc = null)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                return new BaseCreature[0];

            return list;
        }

        public override void AddStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                m_Table[m] = list = new List<BaseCreature>();

            if (!list.Contains(pet))
                list.Add(pet);

            pet.IsElfStabled = true;
        }

        public override void RemoveStabledPet(Mobile m, BaseCreature pet)
        {
            List<BaseCreature> list;

            if (!m_Table.TryGetValue(m, out list))
                return;

            list.Remove(pet);

            pet.IsElfStabled = false;
            // don't remove from the pet cache here as the previous call to SetControlMaster just set it
        }
        public override bool CheckPetCondidtion(Mobile from, BaseCreature pet, bool message = true)
        {
            if (from.AccessLevel >= AccessLevel.GameMaster)
            {
                SayTo(from, "It's from a {0}, i will comply.", from.AccessLevel);
                return true;
            }

            if (pet.IsWinterHolidayPet && !IsElfStabler)
            {
                if (message)
                    SayTo(from, "I'm sorry but I cannot accommodate such exotic creatures.");
                return false;
            }
            else if (!pet.IsWinterHolidayPet && IsElfStabler)
            {
                if (message)
                    SayTo(from, "I'm sorry, but I will not keep Britannian beasts in my stables!");
                return false;
            }
            return true;
        }
        public ElfStabler(Serial serial)
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

    public class ElfStablerPersistence : Item, IPersistence
    {
        private static ElfStablerPersistence m_Instance;
        public Item GetInstance() { return m_Instance; }
        public static void EnsureExistence()
        {
            if (m_Instance == null)
            {
                m_Instance = new ElfStablerPersistence();
                m_Instance.IsIntMapStorage = true;
            }
        }
        public override string DefaultName
        {
            get { return "Elf Stabler Persistence - Internal"; }
        }
        [Constructable]
        public ElfStablerPersistence()
            : base(0x1)
        {
            Movable = false;
        }
        public ElfStablerPersistence(Serial serial)
            : base(serial)
        {
            m_Instance = this;
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0);

            writer.Write((int)ElfStabler.Table.Count);

            foreach (KeyValuePair<Mobile, List<BaseCreature>> kvp in ElfStabler.Table)
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
                        pet.IsElfStabled = true;

                    ElfStabler.Table[m] = pets;
                }
                else
                {
                    foreach (BaseCreature pet in pets)
                        pet.Delete();
                }
            }
        }
    }
}