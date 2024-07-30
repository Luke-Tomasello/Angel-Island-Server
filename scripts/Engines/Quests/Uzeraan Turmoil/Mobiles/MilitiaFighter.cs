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
using Server.Network;
using System.Collections;

namespace Server.Engines.Quests.Haven
{
    public class MilitiaFighter : BaseCreature
    {
        [Constructable]
        public MilitiaFighter()
            : base(AIType.AI_Melee, FightMode.All | FightMode.Closest, 10, 1, 0.2, 0.4)
        {
            InitStats(40, 30, 5);
            Title = "the Militia Fighter";

            SpeechHue = Utility.RandomSpeechHue();

            Hue = Utility.RandomSkinHue();

            Female = false;
            Body = 0x190;
            Name = NameList.RandomName("male");

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

            AddItem(new ThighBoots(0x1BB));
            AddItem(new LeatherChest());
            AddItem(new LeatherArms());
            AddItem(new LeatherLegs());
            AddItem(new LeatherCap());
            AddItem(new LeatherGloves());
            AddItem(new LeatherGorget());

            Item weapon;
            switch (Utility.Random(6))
            {
                case 0: weapon = new Broadsword(); break;
                case 1: weapon = new Cutlass(); break;
                case 2: weapon = new Katana(); break;
                case 3: weapon = new Longsword(); break;
                case 4: weapon = new Scimitar(); break;
                default: weapon = new VikingSword(); break;
            }
            weapon.Movable = false;
            AddItem(weapon);

            Item shield = new BronzeShield();
            shield.Movable = false;
            AddItem(shield);

            SetSkill(SkillName.Swords, 20.0);
        }

        public override bool ClickTitle { get { return false; } }

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

        public MilitiaFighter(Serial serial)
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

    public class MilitiaFighterCorpse : Corpse
    {
        public MilitiaFighterCorpse(Mobile owner, ArrayList equipItems)
            : base(owner, equipItems)
        {
        }

        public override void AddNameProperty(ObjectPropertyList list)
        {
            if (ItemID == 0x2006) // Corpse form
            {
                list.Add("a human corpse");
                list.Add(1049318, this.Name); // the remains of ~1_NAME~ the militia fighter
            }
            else
            {
                list.Add(1049319); // the remains of a militia fighter
            }
        }

        public override void OnSingleClick(Mobile from)
        {
            int hue = Notoriety.GetHue(Server.Misc.NotorietyHandlers.CorpseNotoriety(from, this));

            if (ItemID == 0x2006) // Corpse form
                from.Send(new MessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049318, "", Name)); // the remains of ~1_NAME~ the militia fighter
            else
                from.Send(new MessageLocalized(Serial, ItemID, MessageType.Label, hue, 3, 1049319, "", "")); // the remains of a militia fighter
        }

        public override void Open(Mobile from, bool checkSelfLoot)
        {
            if (from.InRange(this.GetWorldLocation(), 2))
            {
                from.SendLocalizedMessage(1049661, "", 0x22); // Thinking about his sacrifice, you can't bring yourself to loot the body of this militia fighter.
            }
        }

        public MilitiaFighterCorpse(Serial serial)
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