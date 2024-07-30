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

using Server.Mobiles;

namespace Server.Engines.Quests.Necro
{
    public class DarkTidesTeleporter : DynamicTeleporter
    {
        [Constructable]
        public DarkTidesTeleporter()
        {
        }

        public override bool GetDestination(PlayerMobile player, ref Point3D loc, ref Map map)
        {
            QuestSystem qs = player.Quest;

            if (qs is DarkTidesQuest)
            {
                if (qs.IsObjectiveInProgress(typeof(FindMaabusTombObjective)))
                {
                    loc = new Point3D(2038, 1263, -90);
                    map = Map.Malas;
                    qs.AddConversation(new RadarConversation());
                    return true;
                }
                else if (qs.IsObjectiveInProgress(typeof(FindCrystalCaveObjective)))
                {
                    loc = new Point3D(1194, 521, -90);
                    map = Map.Malas;
                    return true;
                }
                else if (qs.IsObjectiveInProgress(typeof(FindCityOfLightObjective)))
                {
                    loc = new Point3D(1091, 519, -90);
                    map = Map.Malas;
                    return true;
                }
                else if (qs.IsObjectiveInProgress(typeof(ReturnToCrystalCaveObjective)))
                {
                    loc = new Point3D(1194, 521, -90);
                    map = Map.Malas;
                    return true;
                }
                else if (DarkTidesQuest.HasLostCallingScroll(player))
                {
                    loc = new Point3D(1194, 521, -90);
                    map = Map.Malas;
                    return true;
                }
            }

            return false;
        }

        public DarkTidesTeleporter(Serial serial)
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