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

/* scripts\Engines\Apiculture\Items\ApicultureConsole.cs
 * CHANGELOG:
 *  8/8/23, Yoar
 *      Initial commit.
 */

using Server.Items;
using System;

namespace Server.Engines.Apiculture
{
    [FlipableAttribute(0x1f14, 0x1f15, 0x1f16, 0x1f17)]
    public class ApicultureConsole : Item
    {
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public bool Competition
        {
            get { return ApicultureSystem.Competition; }
            set { ApicultureSystem.Competition = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxPopulation
        {
            get { return ApicultureSystem.MaxPopulation; }
            set { ApicultureSystem.MaxPopulation = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxHoney
        {
            get { return ApicultureSystem.MaxHoney; }
            set { ApicultureSystem.MaxHoney = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int MaxWax
        {
            get { return ApicultureSystem.MaxWax; }
            set { ApicultureSystem.MaxWax = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int CompetitionRange
        {
            get { return ApicultureSystem.CompetitionRange; }
            set { ApicultureSystem.CompetitionRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SwarmMinSkill
        {
            get { return ApicultureSystem.SwarmMinSkill; }
            set { ApicultureSystem.SwarmMinSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int SwarmMaxSkill
        {
            get { return ApicultureSystem.SwarmMaxSkill; }
            set { ApicultureSystem.SwarmMaxSkill = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int PlantDoubleGrowRange
        {
            get { return ApicultureSystem.PlantDoubleGrowRange; }
            set { ApicultureSystem.PlantDoubleGrowRange = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public int PlantDoubleGrowChance
        {
            get { return ApicultureSystem.PlantDoubleGrowChance; }
            set { ApicultureSystem.PlantDoubleGrowChance = value; }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Administrator)]
        public TimeSpan GrowthDelay
        {
            get { return ApicultureSystem.GrowthDelay; }
            set { ApicultureSystem.GrowthDelay = value; }
        }

        [Constructable]
        public ApicultureConsole()
            : base(0x1F14)
        {
            Hue = 49;
            Name = "Apiculture Settings Console";
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel > AccessLevel.Player)
                from.SendGump(new Gumps.PropertiesGump(from, this));
        }

        public ApicultureConsole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            reader.ReadEncodedInt();
        }
    }
}