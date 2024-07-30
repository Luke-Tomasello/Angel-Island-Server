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

using Server.Gumps;
using Server.Items;
using Server.Mobiles;

namespace Server.Engines.Quests.Haven
{
    public class Schmendrick : BaseQuester
    {
        [Constructable]
        public Schmendrick()
            : base("the High Mage")
        {
        }

        public override void InitBody()
        {
            InitStats(100, 100, 25);

            Hue = 0x83F3;

            Female = false;
            Body = 0x190;
            Name = "Schmendrick";
        }

        public override void InitOutfit()
        {
            AddItem(new Robe(0x4DD));
            AddItem(new WizardsHat(0x482));
            AddItem(new Shoes(0x482));

            AddItem(new LongHair(0x455));
            AddItem(new LongBeard(0x455));

            // TODO: Glacial staff

            Backpack pack = new Backpack();
            pack.Movable = false;
            AddItem(pack);
        }

        public override int GetAutoTalkRange(PlayerMobile pm)
        {
            return 7;
        }

        public override bool CanTalkTo(PlayerMobile to)
        {
            UzeraanTurmoilQuest qs = to.Quest as UzeraanTurmoilQuest;

            return (qs != null && qs.FindObjective(typeof(FindSchmendrickObjective)) != null);
        }

        public override void OnTalk(PlayerMobile player, bool contextMenu)
        {
            QuestSystem qs = player.Quest;

            if (qs is UzeraanTurmoilQuest)
            {
                if (UzeraanTurmoilQuest.HasLostScrollOfPower(player))
                {
                    FocusTo(player);
                    qs.AddConversation(new LostScrollOfPowerConversation(false));
                }
                else
                {
                    QuestObjective obj = qs.FindObjective(typeof(FindSchmendrickObjective));

                    if (obj != null && !obj.Completed)
                    {
                        FocusTo(player);
                        obj.Complete();
                    }
                    else if (contextMenu)
                    {
                        FocusTo(player);
                        SayTo(player, 1049357); // I have nothing more for you at this time.
                    }
                }
            }
        }

        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (dropped is BlankScroll && UzeraanTurmoilQuest.HasLostScrollOfPower(from))
            {
                FocusTo(from);

                Item scroll = new SchmendrickScrollOfPower();

                if (!from.PlaceInBackpack(scroll))
                {
                    scroll.Delete();
                    from.SendLocalizedMessage(1046260); // You need to clear some space in your inventory to continue with the quest.  Come back here when you have more space in your inventory.
                    return false;
                }
                else
                {
                    dropped.Consume();
                    from.SendLocalizedMessage(1049346); // Schmendrick scribbles on the scroll for a few moments and hands you the finished product.
                    return dropped.Deleted;
                }
            }

            return base.OnDragDrop(from, dropped);
        }

        public override void OnMovement(Mobile m, Point3D oldLocation)
        {
            base.OnMovement(m, oldLocation);

            if (m is PlayerMobile && !m.Frozen && !m.Alive && InRange(m, 4) && !InRange(oldLocation, 4) && InLOS(m))
            {
                if (m.Map == null || !Utility.CanFit(m.Map, m.Location, 16, Utility.CanFitFlags.requireSurface))
                {
                    m.SendLocalizedMessage(502391); // Thou can not be resurrected there!
                }
                else
                {
                    Direction = GetDirectionTo(m);

                    m.PlaySound(0x214);
                    m.FixedEffect(0x376A, 10, 16);

                    m.CloseGump(typeof(ResurrectGump));
                    m.SendGump(new ResurrectGump(m, ResurrectMessage.Healer));
                }
            }
        }

        public Schmendrick(Serial serial)
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