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

/* Scripts/Mobiles/Monsters/Humanoid/Melee/Guardian.cs
 * ChangeLog
 *  8/16/06, Rhiannon
 *		Changed speed settings to match SpeedInfo table.
 */

using Server.Items;

namespace Server.Mobiles
{
    public class Guardian : BaseCreature
    {
        [Constructable]
        public Guardian()
            : base(AIType.AI_Archer, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
            InitStats(100, 125, 25);
            Title = "the guardian";

            SpeechHue = Utility.RandomSpeechHue();

            new ForestOstard().Rider = this;

            Skills[SkillName.Anatomy].Base = 120.0;
            Skills[SkillName.Tactics].Base = 120.0;
            Skills[SkillName.Archery].Base = 120.0;
            Skills[SkillName.MagicResist].Base = 120.0;
            Skills[SkillName.DetectHidden].Base = 100.0;

        }
        public override void InitBody()
        {
            if (Female = Utility.RandomBool())
            {
                Body = 0x191;
                Name = NameList.RandomName("female");
            }
            else
            {
                Body = 0x190;
                Name = NameList.RandomName("male");
            }
        }

        public override void InitOutfit()
        {
            Utility.WipeLayers(this);
            Hue = Utility.RandomSkinHue();

            PlateChest chest = new PlateChest();
            chest.Hue = 0x966;
            AddItem(chest);
            PlateArms arms = new PlateArms();
            arms.Hue = 0x966;
            AddItem(arms);
            PlateGloves gloves = new PlateGloves();
            gloves.Hue = 0x966;
            AddItem(gloves);
            PlateGorget gorget = new PlateGorget();
            gorget.Hue = 0x966;
            AddItem(gorget);
            PlateLegs legs = new PlateLegs();
            legs.Hue = 0x966;
            AddItem(legs);
            PlateHelm helm = new PlateHelm();
            helm.Hue = 0x966;
            AddItem(helm);

            Bow bow = new Bow();

            bow.Movable = false;
            bow.Crafter = this;
            bow.Quality = WeaponQuality.Exceptional;

            AddItem(bow);

            PackItem(new Arrow(250));
        }

        public Guardian(Serial serial)
            : base(serial)
        {
        }

        public override void GenerateLoot()
        {
            if (Core.RuleSets.AngelIslandRules())
            {
                // 'Spawning == true' blocked in BaseCreature for AngelIslandRules()
                PackGold(250, 500);
            }
            else
            {
                if (Core.RuleSets.AllShards)
                {   // none circa 02 2002
                    if (Spawning)
                    {
                        PackGold(0);
                    }
                    else
                    {
                    }
                }
                else
                {
                    PackGold(250, 500);
                }
            }
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