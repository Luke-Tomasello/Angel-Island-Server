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

/* Scripts\Engines\CTFSystem\Regions\CTFRegion.cs
 * CHANGELOG:
 * 4/10/10, Adam
 *		Added new region overrides (for new region overridables)
 *		KeepsItemsOnDeath
 *			Yep, players keep all their loot
 *		OnAfterDeath
 *			We want to freeze the ghost after death so they can't ghost spy etc.
 *			We can't use OnDeath() because the frozen flag is cleared after we get called.
 * 4/10/10, adam
 *		initial framework.
 */

using Server.Items;
using Server.Regions;

namespace Server.Engines
{
    public class CTFRegion : CustomRegion
    {
        public CTFRegion(CustomRegionControl controller)
            : base(controller)
        {
            Setup();
        }

        private void Setup()
        {   // new regions default to guarded, turn it off here
            IsGuarded = false;
        }

        public override void OnRegionRegistrationChanged()
        {
            base.OnRegionRegistrationChanged();

            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc != null)
            {
                if (!this.Registered)
                    ctfc.CurrentState = CTFControl.States.Cancel;
            }
        }

        // process CTF commands
        public override void OnSpeech(SpeechEventArgs e)
        {
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc != null)
            {
                if (ctfc.OnRegionSpeech(e))
                    e.Handled = true;
            }
            base.OnSpeech(e);
        }

        public override bool OnDeath(Mobile m)
        {
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc == null) return base.OnDeath(m);
            ctfc.OnDeath(m);
            return base.OnDeath(m);
        }

        public override void OnAfterDeath(Mobile m)
        {
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc == null) return;
            ctfc.OnRegionAfterDeath(m);
        }

        public override bool KeepsItemsOnDeath()
        {   // everyone keeps their loot
            return true;
        }

        public override void OnPlayerAdd(Mobile m)
        {   // player is logging into the region
            base.OnPlayerAdd(m);
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc != null)
                ctfc.OnPlayerAdd(m);
        }

        public override bool CheckAccessibility(Item i, Mobile m)
        {
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc != null && m.AccessLevel == AccessLevel.Player)
                return ctfc.OnRegionCheckAccessibility(i, m);
            else
                return base.CheckAccessibility(i, m);
        }

        public override bool EquipItem(Mobile m, Item item)
        {
            bool result = true;
            CTFControl ctfc = this.Controller as CTFControl;
            if (ctfc != null)
                result = ctfc.OnRegionEquipItem(m, item);
            return base.EquipItem(m, item) && result;
        }
    }
}