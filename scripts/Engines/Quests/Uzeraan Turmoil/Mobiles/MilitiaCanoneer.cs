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
using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class MilitiaCanoneer : BaseQuester
    {
        private bool m_Active;

        [CommandProperty(AccessLevel.GameMaster)]
        public override bool Active
        {
            get { return m_Active; }
            //set { m_Active = value; }
        }

        [Constructable]
        public MilitiaCanoneer()
            : base("the Militia Canoneer")
        {
            m_Active = true;
        }

        public override void InitBody()
        {
            InitStats(100, 125, 25);

            Hue = Utility.RandomSkinHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");
        }

        public override void InitOutfit()
        {
            Item hair = new Item(Utility.RandomList(0x203B, 0x203C, 0x203D, 0x2044, 0x2045, 0x2047, 0x2049, 0x204A));
            hair.Hue = Utility.RandomHairHue();
            hair.Layer = Layer.Hair;
            hair.Movable = false;
            AddItem(hair);

            Item beard = new Item(Utility.RandomList(0x203E, 0x203F, 0x2040, 0x2041, 0x204B, 0x204C, 0x204D));
            beard.Hue = hair.Hue;
            beard.Layer = Layer.FacialHair;
            beard.Movable = false;
            AddItem(beard);

            AddItem(new PlateChest());
            AddItem(new PlateArms());
            AddItem(new PlateGloves());
            AddItem(new PlateLegs());

            Torch torch = new Torch();
            torch.Movable = false;
            AddItem(torch);
            torch.Ignite();
        }

        public override bool CanTalkTo(PlayerMobile to)
        {
            return false;
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
        }

        public override bool IsEnemy(Mobile m, RelationshipFilter filter)
        {
            if (m.Player)
                return false;

            if (m is BaseCreature)
            {
                BaseCreature bc = (BaseCreature)m;
                if (bc.Controlled && bc.ControlMaster != null)
                    return IsEnemy(bc.ControlMaster, filter);
                else if (bc.Summoned && bc.SummonMaster != null)
                    return IsEnemy(bc.SummonMaster, filter);
            }

            return m.Karma < 0;
        }

        public bool WillFire(Cannon cannon, Mobile target)
        {
            if (m_Active && IsEnemy(target, RelationshipFilter.None))
            {
                Direction = GetDirectionTo(target);
                Say(Utility.RandomList(500651, 1049098, 1049320, 1043149));
                return true;
            }

            return false;
        }

        public MilitiaCanoneer(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version

            writer.Write((bool)m_Active);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            m_Active = reader.ReadBool();
        }
    }
}