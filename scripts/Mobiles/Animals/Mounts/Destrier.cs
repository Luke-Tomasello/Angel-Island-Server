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

/* Scripts/Mobiles/Animals/Mounts/Destrier.cs
 * ChangeLog
 *  11/22/2023, Adam
 *      Basically wild War Horses
 *      initial version.
 */

namespace Server.Mobiles
{
    public class Destrier : BaseDestrier
    {
        [Constructable]
        public Destrier()
            : base(Utility.Random(4), AIType.AI_Melee, FightMode.Aggressor, 10, 1, 0.25, 0.5)
        {
        }

        public Destrier(Serial serial)
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

    [CorpseName("a destrier corpse")]
    public abstract class BaseDestrier : BaseMount
    {
        private static int[] BodyIDs = new[] { 0x76, 0x77, 0x78, 0x79 };
        private static int[] ItemIDs = new[] { 0x3EB2, 0x3EB1, 0x3EAF, 0x3EB0 };
        public BaseDestrier(int selector, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : this(BodyIDs[selector], ItemIDs[selector], aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {

        }
        public BaseDestrier(int bodyID, int itemID, AIType aiType, FightMode fightMode, int rangePerception, int rangeFight, double activeSpeed, double passiveSpeed)
            : base("a destrier", bodyID, itemID, aiType, fightMode, rangePerception, rangeFight, activeSpeed, passiveSpeed)
        {
            BaseSoundID = 0xA8;

            // 100% of a FactionWarHorse
            Utility.CopyStats(typeof(Server.Factions.FactionWarHorse), this);

            Fame = 300;
            Karma = 300;

            Tamable = true;
            ControlSlots = 1;
            MinTameSkill = 29.1;
        }

        public override FoodType FavoriteFood { get { return FoodType.FruitsAndVegies | FoodType.GrainsAndHay; } }

        public BaseDestrier(Serial serial)
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