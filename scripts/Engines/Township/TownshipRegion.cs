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

/* Engines/Township/TownshipRegion.cs
 * CHANGELOG:
 * 2/18/22, Yoar
 *      More township cleanups.
 * 1/12/22, Yoar
 *      Township cleanups.
 * 5-11-2010 - Pix
 *      Added IsPointInTownship helper function
 */

using Server.Items;

namespace Server.Regions
{
    public class TownshipRegion : CustomRegion
    {
        public static TownshipRegion GetTownshipAt(Mobile m)
        {
            if (m == null || m.Deleted)
                return null;

            return GetTownshipAt(m.Location, m.Map);
        }

        public static TownshipRegion GetTownshipAt(Item item)
        {
            if (item == null || item.Deleted)
                return null;

            return GetTownshipAt(item.GetWorldLocation(), item.Map);
        }

        public static TownshipRegion GetTownshipAt(Point3D loc, Map map)
        {
            return FindRegion<TownshipRegion>(loc, map);
        }

        public TownshipStone TStone
        {
            get { return this.Controller as TownshipStone; }
        }

        public TownshipRegion(CustomRegionControl rc)
            : base(rc)
        {
            Setup();
        }

        private void Setup()
        {
            this.IsGuarded = false;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            if (TStone != null)
                TStone.HandleSpeech(e);

            base.OnSpeech(e);
        }

        public override bool CheckAccessibility(Item item, Mobile from)
        {
            TownshipRegion tsr = GetTownshipAt(item);

            if (tsr != null && tsr.TStone != null && (tsr.TStone.ItemRegistry.Table.ContainsKey(item) || tsr.TStone.IsLockedDown(item)))
                return tsr.TStone.CheckAccessibility(item, from);

            return base.CheckAccessibility(item, from);
        }

        public override bool OnSingleClick(Mobile from, object o)
        {
            if (o is Item)
            {
                Item item = (Item)o;

                TownshipRegion tsr = GetTownshipAt(item);

                if (tsr != null && tsr.TStone != null && tsr.TStone.IsLockedDown(item))
                {
                    if (item.IsTSItemFreelyAccessible)
                        item.LabelTo(from, "[locked down, unrestricted]");
                    else
                        item.LabelTo(from, 501643); // [locked down]
                }
            }

            return true;
        }

        public override void OnEnter(Mobile m)
        {
            //if (this.Controller is TownshipStone)
            //    ((TownshipStone)this.Controller).OnEnter(m);

            base.OnEnter(m);
        }

        public override void OnExit(Mobile m)
        {
            //if (this.Controller is TownshipStone)
            //    ((TownshipStone)this.Controller).OnExit(m);

            base.OnExit(m);
        }

        public override bool IsNoMurderZone
        {
            get
            {
                if (this.Controller is TownshipStone)
                    return !((TownshipStone)this.Controller).MurderZone;

                return base.IsNoMurderZone;
            }
        }

        public override bool IsMobileCountable(Mobile aggressor)
        {
            if (this.Controller is TownshipStone)
                return ((TownshipStone)this.Controller).IsMobileCountable(aggressor);

            return base.IsMobileCountable(aggressor);
        }
    }
}