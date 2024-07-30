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

/* ChangeLog:
	6/5/04, Pix
		Merged in 1.0RC0 code.
*/

using Server.Commands;
using Server.Targeting;

namespace Server.Targets
{
    public class MoveTarget : Target
    {
        private object m_Object;

        public MoveTarget(object o)
            : base(-1, true, TargetFlags.None)
        {
            m_Object = o;
        }

        protected override void OnTarget(Mobile from, object o)
        {
            IPoint3D p = o as IPoint3D;

            if (p != null)
            {
                if (!BaseCommand.IsAccessible(from, m_Object))
                {
                    from.SendMessage("That is not accessible.");
                    return;
                }

                if (p is Item)
                    p = ((Item)p).GetWorldTop();

                Server.Commands.CommandLogging.WriteLine(from, "{0} {1} moving {2} to {3}", from.AccessLevel, Server.Commands.CommandLogging.Format(from), Server.Commands.CommandLogging.Format(m_Object), new Point3D(p));

                if (m_Object is Item)
                {
                    Item item = (Item)m_Object;

                    if (!item.Deleted)
                        item.MoveToWorld(new Point3D(p), from.Map);
                }
                else if (m_Object is Mobile)
                {
                    Mobile m = (Mobile)m_Object;

                    if (!m.Deleted)
                        m.MoveToWorld(new Point3D(p), from.Map);
                }
            }
        }
    }
}