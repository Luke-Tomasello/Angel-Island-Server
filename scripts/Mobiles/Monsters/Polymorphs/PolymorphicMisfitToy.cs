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

/* Scripts\Mobiles\Monsters\Polymorphs\PolymorphicMisfitToy.cs
 * ChangeLog
 *  12/11/23, Adam
 *      Created.
 */

using System;

namespace Server.Mobiles
{
    public class PolymorphicMisfitToy : BasePolymorphic
    {
        [Constructable]
        public PolymorphicMisfitToy()
            : base()
        {

        }

        #region basic props
        public override bool AlwaysMurderer { get { return false; } }
        public override bool ShowFameTitle { get { return false; } }
        public override bool CanBandage { get { return false; } }
        public override bool CanRummageCorpses { get { return false; } }
        #endregion basic props

        public PolymorphicMisfitToy(Serial serial)
            : base(serial)
        {
        }

        public override void InitStats()
        {
            bool mini = MaxLevels == 4;
            if (mini)
                switch (Level)
                {
                    default:
                    case 0:
                        {
                            Utility.CopyConstruction(typeof(Drake), this);
                            Utility.CopyStats(typeof(Drake), this);
                            MyType = typeof(Drake);
                            break;
                        }
                    case 1:
                        {
                            Utility.CopyConstruction(typeof(Drake), this);  // don't want dragon magic
                            Utility.CopyStats(typeof(Dragon), this);
                            MyType = typeof(Dragon);
                            break;
                        }
                    case 2:
                        {
                            if (Utility.RandomChance(80))
                            {
                                Utility.CopyConstruction(typeof(Drake), this);  // don't want dragon magic
                                Utility.CopyStats(typeof(Dragon), this);
                                MyType = typeof(Dragon);
                            }
                            else
                            {
                                Utility.CopyConstruction(typeof(OgreLord), this);
                                Utility.CopyStats(typeof(OgreLord), this);
                                MyType = typeof(OgreLord);
                            }
                            break;
                        }

                }
            else
                switch (Level)
                {
                    // Configure was not set, so go with the basics
                    default:
                    // sub level 1
                    case 1:
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        {
                            Utility.CopyConstruction(typeof(Drake), this);
                            Utility.CopyStats(typeof(Drake), this);
                            MyType = typeof(Drake);
                            break;
                        }
                    // sublevel 2
                    case 6:
                    case 7:
                    case 8:
                    case 9:
                        {
                            Utility.CopyConstruction(typeof(Dragon), this);
                            Utility.CopyStats(typeof(Dragon), this);
                            MyType = typeof(Dragon);
                            break;
                        }
                    // subLevel 3
                    case 10:
                    case 11:
                    case 12:
                    case 13:
                        {
                            if (Utility.RandomChance(80))
                            {
                                Utility.CopyConstruction(typeof(Dragon), this);
                                Utility.CopyStats(typeof(Dragon), this);
                                MyType = typeof(Dragon);
                            }
                            else
                            {
                                Utility.CopyConstruction(typeof(OgreLord), this);
                                Utility.CopyStats(typeof(OgreLord), this);
                                MyType = typeof(OgreLord);
                            }
                            break;
                        }
                    // sublevel 4
                    case 14:
                    case 15:
                    case 16:
                        {
                            if (Utility.RandomChance(80))
                            {
                                Utility.CopyConstruction(typeof(OgreLord), this);
                                Utility.CopyStats(typeof(OgreLord), this);
                                MyType = typeof(OgreLord);
                            }
                            else
                            {
                                Utility.CopyConstruction(typeof(Dragon), this);
                                Utility.CopyStats(typeof(Dragon), this);
                                MyType = typeof(Dragon);
                            }
                            break;
                        }
                }
        }
        public override void InitBody()
        {
            int[] interesting_hues = new int[] { 0xB85, 0xB87, 0xB89, 0xB8c, 0xB8e, 0xB8f, 0xB93, 0xB97, 0xB98, 0xB9A, 0x482 };
            int[] mixedup_sounds = new int[] { 0x6E/*chicken*/, 0x85/*dog*/, 0xC9/*cat*/, 0x78/*cow*/, 0xC4/*pig*/, 0xA8/*horse*/, 0x9E/*gorilla*/, 0x188/*giant rat*/};
            Type[] mixedup_bodies = new Type[] { typeof(Chicken), typeof(Dog), typeof(Cat), typeof(Cow), typeof(Pig), typeof(BrownBear), typeof(JackRabbit), typeof(GiantRat) };
            Hue = 3 + (Utility.Random(20) * 5);
            if (Utility.Chance(0.32))
                Hue = Utility.RandomList(interesting_hues);
            BaseSoundID = Utility.RandomList(mixedup_sounds);
            Type me = Utility.RandomList(mixedup_bodies);
            Utility.CopyBody(me, this);
            Utility.CopyConstruction(me, this);
            Name = "a misfit toy";
        }
        public override void InitOutfit()
        {
        }
        public override void GenerateLoot()
        {
            base.GenerateLoot();
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            int version = 0;
            writer.Write(version);
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }

        }
        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            switch (version)
            {
                case 1:
                    goto case 0;
                case 0:
                    break;
            }
        }
    }
}