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

using System;

namespace Server.Items
{
    public class LeftArm : Item, ICarvable
    {
        private IOBAlignment m_IOBAlignment = IOBAlignment.None;
        public IOBAlignment IOBAlignment { get { return m_IOBAlignment; } }
        public override TimeSpan DecayTime
        {
            get
            {
                return TimeSpan.FromMinutes(15.0);
            }
        }

        [Constructable]
        public LeftArm()
            : base(0x1DA1)
        {
        }

        public LeftArm(IOBAlignment iob)
            : this()
        {
            m_IOBAlignment = iob;
        }

        public LeftArm(Serial serial)
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

        #region ICarvable Members

        void ICarvable.Carve(Mobile from, Item item)
        {
            Point3D loc = this.Location;
            if (this.ParentContainer != null)
            {
                if (this.ParentMobile != null)
                {
                    if (this.ParentMobile != from)
                    {
                        from.SendMessage("You can't carve that there");
                        return;
                    }

                    loc = this.ParentMobile.Location;
                }
                else
                {
                    loc = this.ParentContainer.Location;
                    if (!from.InRange(loc, 1))
                    {
                        from.SendMessage("That is too far away.");
                        return;
                    }
                }
            }

            //add blood
            Blood blood = new Blood(Utility.Random(0x122A, 5), Utility.Random(15 * 60, 5 * 60));
            blood.MoveToWorld(loc, Map);
            //add meat
            Jerky jerky = new Jerky(m_IOBAlignment);
            if (this.ParentContainer == null)
            {
                jerky.MoveToWorld(loc, Map);
            }
            else
            {
                this.ParentContainer.DropItem(jerky);
            }

            this.Delete();
        }

        #endregion
    }
}