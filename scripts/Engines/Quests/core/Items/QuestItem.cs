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

namespace Server.Engines.Quests
{
    public abstract class QuestItem : Item
    {
        public QuestItem(int itemID)
            : base(itemID)
        {
        }

        public QuestItem(Serial serial)
            : base(serial)
        {
        }

        public abstract bool CanDrop(PlayerMobile pm);

        public virtual bool Accepted { get { return Deleted; } }

        public override bool DropToWorld(Mobile from, Point3D p)
        {
            bool ret = base.DropToWorld(from, p);

            if (ret && !Accepted && Parent != from.Backpack)
            {
                if (from.AccessLevel > AccessLevel.Player)
                {
                    return true;
                }
                else if (!(from is PlayerMobile) || CanDrop((PlayerMobile)from))
                {
                    return true;
                }
                else
                {
                    from.SendLocalizedMessage(1049343); // You can only drop quest items into the top-most level of your backpack while you still need them for your quest.
                    return false;
                }
            }
            else
            {
                return ret;
            }
        }

        public override bool DropToMobile(Mobile from, Mobile target, Point3D p)
        {
            bool ret = base.DropToMobile(from, target, p);

            if (ret && !Accepted && Parent != from.Backpack)
            {
                if (from.AccessLevel > AccessLevel.Player)
                {
                    return true;
                }
                else if (!(from is PlayerMobile) || CanDrop((PlayerMobile)from))
                {
                    return true;
                }
                else
                {
                    from.SendLocalizedMessage(1049344); // You decide against trading the item.  You still need it for your quest.
                    return false;
                }
            }
            else
            {
                return ret;
            }
        }

        public override bool DropToItem(Mobile from, Item target, Point3D p)
        {
            bool ret = base.DropToItem(from, target, p);

            if (ret && !Accepted && Parent != from.Backpack)
            {
                if (from.AccessLevel > AccessLevel.Player)
                {
                    return true;
                }
                else if (!(from is PlayerMobile) || CanDrop((PlayerMobile)from))
                {
                    return true;
                }
                else
                {
                    from.SendLocalizedMessage(1049343); // You can only drop quest items into the top-most level of your backpack while you still need them for your quest.
                    return false;
                }
            }
            else
            {
                return ret;
            }
        }

        public override DeathMoveResult OnParentDeath(Mobile parent)
        {
            if (parent is PlayerMobile && !CanDrop((PlayerMobile)parent))
                return DeathMoveResult.MoveToBackpack;
            else
                return base.OnParentDeath(parent);
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