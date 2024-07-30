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

#if false


/* Scripts/Regions/TransparentRegion.cs
 * CHANGELOG
 *	10/15/10, Adam
 *		Create a new region type that Delegates all unhandled events to the 'parent' region.
 *		The 'Parent' region being that region under this POINT with the highest priority.
 */

using Server.Targeting;
using System;

namespace Server.Regions
{
    public class TransparentRegion : Region
    {
        public TransparentRegion(Map map)
            : base("", "Transparent Region", map)
        {
        }

        private Region GetParent()
        {
            if (base.Coords.Count != 0)
            {
                Rectangle3D zone = base.Coords[0];
                Region parent = Region.Find(new Point3D(zone.Start, 0), base.Map, this);
                if (parent == Region.Find(new Point3D(zone.End, 0), base.Map, this))
                    return parent;
            }
            return null;
        }

        public override bool IsNoMurderZone
        {
            get
            {
                Region parent = GetParent();
                if (parent != null)
                    return parent.IsNoMurderZone;
                else
                    return base.IsNoMurderZone;
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.Serialize(writer);
            else
                base.Serialize(writer);
        }

        public override void Deserialize(GenericReader reader)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.Deserialize(reader);
            else
                base.Deserialize(reader);
        }

        public override void MakeGuard(Mobile focus)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.MakeGuard(focus);
            else
                base.MakeGuard(focus);
        }

        public override Type GetResource(Type type)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.GetResource(type);
            else
                return base.GetResource(type);
        }

        public override bool KeepsItemsOnDeath()
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.KeepsItemsOnDeath();
            else
                return base.KeepsItemsOnDeath();
        }

        public override bool CanUseStuckMenu(Mobile m, bool quiet = false)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.CanUseStuckMenu(m);
            else
                return base.CanUseStuckMenu(m);
        }

        public override void OnAggressed(Mobile aggressor, Mobile aggressed, bool criminal)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnAggressed(aggressor, aggressed, criminal);
            else
                base.OnAggressed(aggressor, aggressed, criminal);
        }

        public override void OnDidHarmful(Mobile harmer, Mobile harmed)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnDidHarmful(harmer, harmed);
            else
                base.OnDidHarmful(harmer, harmed);
        }

        public override void OnGotHarmful(Mobile harmer, Mobile harmed)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnGotHarmful(harmer, harmed);
            else
                base.OnGotHarmful(harmer, harmed);
        }

        public override void OnPlayerAdd(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnPlayerAdd(m);
            else
                base.OnPlayerAdd(m);
        }

        public override void OnPlayerRemove(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnPlayerRemove(m);
            else
                base.OnPlayerRemove(m);
        }

        public override void OnMobileAdd(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnMobileAdd(m);
            else
                base.OnMobileAdd(m);
        }

        public override void OnMobileRemove(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnMobileRemove(m);
            else
                base.OnMobileRemove(m);
        }

        public override bool OnMoveInto(Mobile m, Direction d, Point3D newLocation, Point3D oldLocation)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnMoveInto(m, d, newLocation, oldLocation);
            else
                return base.OnMoveInto(m, d, newLocation, oldLocation);
        }

        public override void OnLocationChanged(Mobile m, Point3D oldLocation)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnLocationChanged(m, oldLocation);
            else
                base.OnLocationChanged(m, oldLocation);
        }

        public override void PlayMusic(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.PlayMusic(m);
            else
                base.PlayMusic(m);
        }

        public override void StopMusic(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.StopMusic(m);
            else
                base.StopMusic(m);
        }

        public override void OnEnter(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnEnter(m);
            else
                base.OnEnter(m);
        }

        public override void OnExit(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnExit(m);
            else
                base.OnExit(m);
        }

        public override bool OnTarget(Mobile m, Target t, object o)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnTarget(m, t, o);
            else
                return base.OnTarget(m, t, o);
        }

        public override bool OnCombatantChange(Mobile m, Mobile Old, Mobile New)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnCombatantChange(m, Old, New);
            else
                return base.OnCombatantChange(m, Old, New);
        }

        public override bool AllowHousing(Point3D p)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.AllowHousing(p);
            else
                return base.AllowHousing(p);
        }

        public override bool SendInaccessibleMessage(Item item, Mobile from)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.SendInaccessibleMessage(item, from);
            else
                return base.SendInaccessibleMessage(item, from);
        }

        public override bool CheckAccessibility(Item item, Mobile from)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.CheckAccessibility(item, from);
            else
                return base.CheckAccessibility(item, from);
        }

        public override bool OnDecay(Item item)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnDecay(item);
            else
                return base.OnDecay(item);
        }

        public override bool AllowHarmful(Mobile from, Mobile target)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.AllowHarmful(from, target);
            else
                return base.AllowHarmful(from, target);
        }

        public override void OnCriminalAction(Mobile m, bool message)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnCriminalAction(m, message);
            else
                base.OnCriminalAction(m, message);
        }

        public override bool AllowBenificial(Mobile from, Mobile target)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.AllowBenificial(from, target);
            else
                return base.AllowBenificial(from, target);
        }

        public override void OnBenificialAction(Mobile helper, Mobile target)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnBenificialAction(helper, target);
            else
                base.OnBenificialAction(helper, target);
        }

        public override void OnGotBenificialAction(Mobile helper, Mobile target)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnGotBenificialAction(helper, target);
            else
                base.OnGotBenificialAction(helper, target);
        }

        public override bool IsInInn(Point3D p)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.IsInInn(p);
            else
                return base.IsInInn(p);
        }

        public override TimeSpan GetLogoutDelay(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.GetLogoutDelay(m);
            else
                return base.GetLogoutDelay(m);
        }

        public override void AlterLightLevel(Mobile m, ref int global, ref int personal)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.AlterLightLevel(m, ref global, ref personal);
            else
                base.AlterLightLevel(m, ref global, ref personal);
        }

        public override void SpellDamageScalar(Mobile caster, Mobile target, ref double damage)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.SpellDamageScalar(caster, target, ref damage);
            else
                base.SpellDamageScalar(caster, target, ref damage);
        }

        public override void OnSpeech(SpeechEventArgs args)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnSpeech(args);
            else
                base.OnSpeech(args);
        }

        public override bool AllowSpawn()
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.AllowSpawn();
            else
                return base.AllowSpawn();
        }

        public override bool OnSkillUse(Mobile m, int Skill)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnSkillUse(m, Skill);
            else
                return base.OnSkillUse(m, Skill);
        }

        public override bool OnBeginSpellCast(Mobile m, ISpell s)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnBeginSpellCast(m, s);
            else
                return base.OnBeginSpellCast(m, s);
        }

        public override void OnSpellCast(Mobile m, ISpell s)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnSpellCast(m, s);
            else
                base.OnSpellCast(m, s);
        }

        public override bool EquipItem(Mobile m, Item item)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.EquipItem(m, item);
            else
                return base.EquipItem(m, item);
        }

        public override bool OnResurrect(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnResurrect(m);
            else
                return base.OnResurrect(m);
        }

        public override bool OnDeath(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnDeath(m);
            else
                return base.OnDeath(m);
        }

        public override void OnAfterDeath(Mobile m)
        {
            Region parent = GetParent();
            if (parent != null)
                parent.OnAfterDeath(m);
            else
                base.OnAfterDeath(m);
        }

        public override bool OnDamage(Mobile m, ref int Damage)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnDamage(m, ref Damage);
            else
                return base.OnDamage(m, ref Damage);
        }

        public override bool OnHeal(Mobile m, ref int Heal)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnHeal(m, ref Heal);
            else
                return base.OnHeal(m, ref Heal);
        }

        public override bool OnDoubleClick(Mobile m, object o)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnDoubleClick(m, o);
            else
                return base.OnDoubleClick(m, o);
        }

        public override bool OnSingleClick(Mobile m, object o)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.OnSingleClick(m, o);
            else
                return base.OnSingleClick(m, o);
        }

        public override bool Saves
        {
            get
            {
                Region parent = GetParent();
                if (parent != null)
                    return parent.Saves;
                else
                    return base.Saves;
            }
        }

        /*public override bool Contains(Point3D p)
		{
			//Region parent = GetParent();
			//if (parent != null)
				//return parent.Contains(p);
			//else
				return base.Contains(p);
		}*/

        /*public override void Unregister()
		{
			//Region parent = GetParent();
			//if (parent != null)
				//parent.Unregister();
			//else
				base.Unregister();
		}

		public override void Register()
		{
			//Region parent = GetParent();
			//if (parent != null)
				//parent.Register();
			//else
				base.Register();
		}*/

        public override bool IsMobileCountable(Mobile aggressor)
        {
            Region parent = GetParent();
            if (parent != null)
                return parent.IsMobileCountable(aggressor);
            else
                return base.IsMobileCountable(aggressor);
        }
    }
}
#endif