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

/* Scripts/Mobiles/Monsters/Special/Christmas/Pagans/PaganPeasant.cs
 * ChangeLog
 *	12/13/23, Yoar
 *		Initial version.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class PaganPeasant : BaseCreature
    {
        [Constructable]
        public PaganPeasant()
            : base(AIType.AI_Melee, FightMode.Aggressor | FightMode.Closest, 10, 1, 0.25, 0.5)
        {
            Title = "the pagan";

            InitBody(this);
            InitOutfit(this);

            SetStr(201, 250);
            SetDex(201, 250);
            SetInt(100);

            SetDamage(10, 23);

            SetSkill(SkillName.Fencing, 66.0, 97.5);
            SetSkill(SkillName.Macing, 65.0, 87.5);
            SetSkill(SkillName.MagicResist, 25.0, 47.5);
            SetSkill(SkillName.Swords, 65.0, 87.5);
            SetSkill(SkillName.Tactics, 65.0, 87.5);
            SetSkill(SkillName.Wrestling, 15.0, 37.5);

            switch (Utility.Random(6))
            {
                case 0: AddItem(new Axe()); break;
                case 1: AddItem(new BattleAxe()); break;
                case 2: AddItem(new DoubleAxe()); break;
                case 3: AddItem(new Dagger()); break;
                case 4: AddItem(new Kryss()); break;
                case 5: AddItem(new Spear()); break;
            }

            PackItem(new Bandage(Utility.RandomMinMax(1, 15)), lootType: LootType.UnStealable);

            Fame = 3000;
        }

        #region The Pagan Look

        public static void InitBody(Mobile m)
        {
            m.SpeechHue = Utility.RandomSpeechHue();
            m.Hue = RandomSkinHue();

            if (m.Female = Utility.RandomBool())
            {
                m.Body = 0x191;
                m.Name = NameList.RandomName("female");
            }
            else
            {
                m.Body = 0x190;
                m.Name = NameList.RandomName("male");
            }

            Item hair = new Item(RandomHairItemID());
            hair.Hue = RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            ForceAddItem(m, hair);
        }

        public static void InitOutfit(Mobile m)
        {
            if (m.Female)
                ForceAddItem(m, new Skirt(RandomClothingHue()));
            else
                ForceAddItem(m, new ShortPants(RandomClothingHue()));

            ForceAddItem(m, new Boots(Utility.RandomNeutralHue()));

            if (Utility.RandomBool())
                ForceAddItem(m, new FancyShirt(RandomClothingHue()));
            else
                ForceAddItem(m, new Shirt(RandomClothingHue()));

            if (Utility.Random(3) == 0)
                ForceAddItem(m, new Bandana(RandomClothingHue()));

            if (Utility.Random(3) == 0)
                ForceAddItem(m, new HalfApron());
        }

        public static int RandomSkinHue()
        {
            return Utility.RandomList(
                1002, 1003, 1004,
                1009, 1010, 1011,
                1016, 1017, 1018) | 0x8000;
        }

        public static int RandomHairItemID()
        {
            return Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2049);
        }

        public static int RandomHairHue()
        {
            return Utility.RandomList(
                1810, 1816, 1818, // blond
                1846, 1852, 1854, // brunette
                1900, 1906, 1908); // dark
        }

        public static int RandomClothingHue()
        {
            switch (Utility.Random(10))
            {
                case 0:
                case 1:
                case 2:
                case 3: return Utility.RandomGreenHue();
                case 4:
                case 5:
                case 6:
                case 7: return Utility.RandomOrangeHue();
                default: return Utility.RandomBlueHue();
            }
        }

        public static void ForceAddItem(Mobile m, Item item)
        {
            Item existing = m.FindItemOnLayer(item.Layer);

            if (existing != null)
                existing.Delete();

            if (item.Layer == Layer.TwoHanded)
            {
                existing = m.FindItemOnLayer(Layer.OneHanded);

                if (existing != null)
                    existing.Delete();
            }

            m.AddItem(item);
        }

        #endregion

        public override bool AlwaysAttackable { get { return true; } }
        public override bool CanBandage { get { return true; } }
        public override bool CanRummageCorpses { get { return true; } }
        public override bool ClickTitle { get { return true; } }

        public override void AggressiveAction(Mobile aggressor, bool criminal, object source = null)
        {
            base.AggressiveAction(aggressor, criminal, source);

            PaganPeasant.HandleAggressiveAction(aggressor, this);
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            return (base.IsEnemy(m, filter) || PaganPeasant.IsPaganEnemy(m));
        }

        #region Pagan Enemy

        private static readonly Memory m_Enemies = new Memory();

        public static bool IsPaganEnemy(Mobile m)
        {
            return m_Enemies.Recall(m);
        }

        public static void MakePaganEnemy(Mobile m)
        {
            m_Enemies.Remember(m, 120.0);
        }

        public static void HandleAggressiveAction(Mobile source, Mobile target)
        {
            if (source != null && target != null && source != target && !IsPagan(source) && IsPagan(target))
                MakePaganEnemy(source);
        }

        private static bool IsPagan(Mobile m)
        {
            return (m is PaganPeasant || m is PaganDruid);
        }

        #endregion

        public override void GenerateLoot()
        {
            // SP custom

            if (m_Spawning)
            {
            }
            else
            {
            }
        }

        public PaganPeasant(Serial serial)
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