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

/* ChangeLog
 * Scripts\Items\Special\Special Rewards\SPOneYearAnniversary.cs
 *  5/9/2024, Adam
 *      Created
 */

namespace Server.Items
{
    public class SPOneYearAnniversary : HoodedShroudOfShadows
    {
        [Constructable]
        public SPOneYearAnniversary(int hue)
            : base()
        {
            Hue = hue;
            Dyable = false;
        }
        public override string DefaultName { get { return "Celebrating 1 Year of Siege Perilous"; } }
        public SPOneYearAnniversary(Serial serial)
            : base(serial)
        {
        }
        public override void OnSingleClick(Mobile from)
        {
            base.OnSingleClick(from);

            from.SendMessage("Siege Perilous");
            from.SendMessage("Launched May 6, 2023");
        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Owner)]
        public override int Hue
        {
            get { return base.Hue; }
            set { base.Hue = value; }
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