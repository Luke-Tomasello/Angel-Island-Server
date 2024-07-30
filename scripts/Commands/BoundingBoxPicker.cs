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

/* Scripts/Commands/BoundingBoxPicker.cs
 *  9/11/22, Yoar
 *      Added cancel callback
 */

using Server.Targeting;

namespace Server
{
    public delegate void BoundingBoxCallback(Mobile from, Map map, Point3D start, Point3D end, object state);
    public delegate void BoundingBoxCancelCallback(Mobile from, object state);

    public class BoundingBoxPicker
    {
        public static void Begin(Mobile from, BoundingBoxCallback callback, object state)
        {
            Begin(from, callback, null, state);
        }

        public static void Begin(Mobile from, BoundingBoxCallback callback, BoundingBoxCancelCallback onCancel, object state)
        {
            from.SendMessage("Target the first location of the bounding box.");
            from.Target = new PickTarget(callback, onCancel, state);
        }

        private class PickTarget : Target
        {
            private Point3D m_Store;
            private bool m_First;
            private Map m_Map;
            private BoundingBoxCallback m_Callback;
            private BoundingBoxCancelCallback m_OnCancel;
            private object m_State;

            public PickTarget(BoundingBoxCallback callback, BoundingBoxCancelCallback onCancel, object state)
                : this(Point3D.Zero, true, null, callback, onCancel, state)
            {
            }

            public PickTarget(Point3D store, bool first, Map map, BoundingBoxCallback callback, BoundingBoxCancelCallback onCancel, object state)
                : base(-1, true, TargetFlags.None)
            {
                m_Store = store;
                m_First = first;
                m_Map = map;
                m_Callback = callback;
                m_OnCancel = onCancel;
                m_State = state;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                IPoint3D p = targeted as IPoint3D;

                if (p == null)
                    return;
                else if (p is Item)
                    p = ((Item)p).GetWorldTop();

                if (m_First)
                {
                    from.SendMessage("Target another location to complete the bounding box.");
                    from.Target = new PickTarget(new Point3D(p), false, from.Map, m_Callback, m_OnCancel, m_State);
                }
                else if (from.Map != m_Map)
                {
                    from.SendMessage("Both locations must reside on the same map.");
                }
                else if (m_Map != null && m_Map != Map.Internal && m_Callback != null)
                {
                    Point3D start = m_Store;
                    Point3D end = new Point3D(p);

                    Utility.FixPoints(ref start, ref end);

                    m_Callback(from, m_Map, start, end, m_State);
                }
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (m_OnCancel != null)
                    m_OnCancel(from, m_State);
            }
        }
    }
}