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

namespace Server.Engines.Quests.Collector
{
    public class GabrielPiete : BaseQuester
    {
        [Constructable]
        public GabrielPiete()
            : base("the renowned minstrel")
        {
        }

        public GabrielPiete(Serial serial)
            : base(serial)
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83EF;

            Female = false;
            Body = 0x190;
            Name = "Gabriel Piete";
        }

        public override void InitOutfit()
        {
            AddItem(new FancyShirt());
            AddItem(new LongPants(0x5F7));
            AddItem(new Shoes(0x5F7));

            AddItem(new TwoPigTails(0x460));
            AddItem(new Mustache(0x460));
        }

        public override bool CanTalkTo(PlayerMobile to)
        {
            QuestSystem qs = to.Quest as CollectorQuest;

            if (qs == null)
                return false;

            return (qs.IsObjectiveInProgress(typeof(FindGabrielObjective))
                || qs.IsObjectiveInProgress(typeof(FindSheetMusicObjective))
                || qs.IsObjectiveInProgress(typeof(ReturnSheetMusicObjective))
                || qs.IsObjectiveInProgress(typeof(ReturnAutographObjective)));
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            QuestSystem qs = player.Quest;

            if (qs is CollectorQuest)
            {
                Direction = GetDirectionTo(player);

                QuestObjective obj = qs.FindObjective(typeof(FindGabrielObjective));

                if (obj != null && !obj.Completed)
                {
                    obj.Complete();
                }
                else if (qs.IsObjectiveInProgress(typeof(FindSheetMusicObjective)))
                {
                    qs.AddConversation(new GabrielNoSheetMusicConversation());
                }
                else
                {
                    obj = qs.FindObjective(typeof(ReturnSheetMusicObjective));

                    if (obj != null && !obj.Completed)
                    {
                        obj.Complete();
                    }
                    else if (qs.IsObjectiveInProgress(typeof(ReturnAutographObjective)))
                    {
                        qs.AddConversation(new GabrielIgnoreConversation());
                    }
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