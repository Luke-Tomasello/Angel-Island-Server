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

using Server.Items;
using System.Collections;

namespace Server.Mobiles
{
    public class KeeperOfChivalry : BaseVendor
    {
        private ArrayList m_SBInfos = new ArrayList();
        protected override ArrayList SBInfos { get { return m_SBInfos; } }

        [Constructable]
        public KeeperOfChivalry()
            : base("the Keeper of Chivalry")
        {
            SetSkill(SkillName.Fencing, 75.0, 85.0);
            SetSkill(SkillName.Macing, 75.0, 85.0);
            SetSkill(SkillName.Swords, 75.0, 85.0);
            //SetSkill(SkillName.Chivalry, 100.0);
        }

        public override void InitSBInfo()
        {
            m_SBInfos.Add(new SBKeeperOfChivalry());
        }

        public override void InitOutfit()
        {
            AddItem(new PlateArms());
            AddItem(new PlateChest());
            AddItem(new PlateGloves());
            AddItem(new StuddedGorget());
            AddItem(new PlateLegs());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new PlateHelm()); break;
                case 1: AddItem(new NorseHelm()); break;
                case 2: AddItem(new CloseHelm()); break;
                case 3: AddItem(new Helmet()); break;
            }

            switch (Utility.Random(3))
            {
                case 0: AddItem(new BodySash(0x482)); break;
                case 1: AddItem(new Doublet(0x482)); break;
                case 2: AddItem(new Tunic(0x482)); break;
            }

            AddItem(new Broadsword());

            Item shield = new MetalKiteShield();

            shield.Hue = Utility.RandomNondyedHue();

            AddItem(shield);

            switch (Utility.Random(2))
            {
                case 0: AddItem(new Boots()); break;
                case 1: AddItem(new ThighBoots()); break;
            }

            PackGold(100, 200);
        }

        public KeeperOfChivalry(Serial serial)
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