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

namespace Server.Mobiles
{
    public class Noble : BaseEscortable
    {
        [Constructable]
        public Noble()
        {
            Title = "the noble";

            SetSkill(SkillName.Parry, 80.0, 100.0);
            SetSkill(SkillName.Swords, 80.0, 100.0);
            SetSkill(SkillName.Tactics, 80.0, 100.0);
        }

        public override bool CanTeach { get { return Core.RuleSets.SiegeStyleRules() ? false : true; } }
        public override bool ClickTitle { get { return false; } } // Do not display 'the noble' when single-clicking

        public override void InitOutfit()
        {
            if (Female)
                AddItem(new FancyDress());
            else
                AddItem(new FancyShirt(GetRandomHue()));

            int lowHue = GetRandomHue();

            AddItem(new ShortPants(lowHue));

            if (Female)
                AddItem(new ThighBoots(lowHue));
            else
                AddItem(new Boots(lowHue));

            if (!Female)
                AddItem(new BodySash(lowHue));

            AddItem(new Cloak(GetRandomHue()));

            if (!Female)
                AddItem(new Longsword());

            switch (Utility.Random(4))
            {
                case 0: AddItem(new ShortHair(Utility.RandomHairHue())); break;
                case 1: AddItem(new TwoPigTails(Utility.RandomHairHue())); break;
                case 2: AddItem(new ReceedingHair(Utility.RandomHairHue())); break;
                case 3: AddItem(new KrisnaHair(Utility.RandomHairHue())); break;
            }

            PackGold(200, 250);
        }

        public Noble(Serial serial)
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