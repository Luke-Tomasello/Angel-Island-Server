/***************************************************************************
 *
 *   RunUO                   : May 1, 2002
 *   portions copyright      : (C) The RunUO Software Team
 *   email                   : info@runuo.com
 *
 *   Angel Island UO Shards  : March 25, 2004
 *   portions copyright      : (C) 2004-2026 Tomasello Software LLC.
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

/* Scripts\Engines\Spawner\Spawner.PostSpawn.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
 */

using Server.Diagnostics;
using Server.Multis;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        public virtual void MoveToWorld(IEntity o, Point3D loc, Map map)
        {
            if (o is Mobile)
                (o as Mobile).MoveToWorld(loc, map);
            if (o is Item)
                (o as Item).MoveToWorld(loc, map);
        }
        protected virtual void OnAfterMobileSpawn(Mobile m)
        {
            if (m is BaseCreature bc)
            {
                // the interesting thing about Concentric spawned creatures is that their home becomes their spawn point
                //  and they get a new default RangeHome based upon the radius of the spawner
                //  Less 'clumping'!
                if (Concentric)
                {
                    bc.Home = m.Location;
                    bc.RangeHome = new Circle(m_HomeRange, new Vector2(this.Location.X, this.Location.Y)).GetConcentricHomeRange();
                }

                // template mobiles may have been constructed at location (0,0,0) and will be moved to there final spawn point
                //      when this happens, they will have been deactivated at (0,0,0) because that sector is not active.
                //      We therefore activate here, then deactivate as appropriate based on their new sector.
                if (bc.AIObject != null && !bc.Active)
                    bc.AIObject.Activate();

                // kill the template to expose the corpse
                if (bc.Spawner is Spawner spawner && spawner.StaticCorpse)
                    Timer.DelayCall(TimeSpan.FromSeconds(2), bc.Kill);

                // set clear mobile debugging
                bc.DebugMode = DebugMobile;

                // Set the EquipmentDisposition for this mobile here
                bc.MutateEquipment = MutateEquipment;

                // attach followers
                List<BaseCreature> followers = ComposeFollowerList();

                foreach (BaseCreature follower in followers)
                {
                    // place near primary
                    follower.MoveToWorld(bc.Home, bc.Map);
                    // set Controlled / ControlMaster
                    follower.Controlled = true;
                    follower.ControlMaster = bc;
                    // set follow order
                    follower.ControlOrder = OrderType.Come; // follow needs a target, we'll just use Come
                    // establish loyalty
                    follower.LoyaltyValue = (PetLoyalty)follower.MaxLoyalty;
                }

                bc.DeactivateIfAppropriate();
            }
            EventSink.InvokeSpawnedMobileCreated(new SpawnedMobileCreatedEventArgs(m));

            // SetProp 
            if (m_SetProp != null)
                SetObjectProp(m, m_SetProp);

            // SetSkill 
            if (m_SetSkill != null)
                SetObjectSkill(m, m_SetSkill);
        }
        protected virtual void OnAfterItemSpawn(Item item)
        {
            // give the spawned item a ref to this spawner
            item.Spawner = this;
            // QuestCode
            if (m_QuestCode != 0 && m_QuestCodeChance > Utility.RandomDouble())
                item.QuestCode = m_QuestCode;
            //GraphicID
            if (m_GraphicID != 0)
                item.ItemID = m_GraphicID;
            // OnSpawner
            item.SetItemBool(Item.ItemBoolTable.OnSpawner, true);
            // SetProp 
            if (m_SetProp != null)
                SetObjectProp(item, m_SetProp);
            if (item is BaseCamp bc)
            {   // the camp should decay at roughly the rate we have specified.
                //  internally though, camps refresh themselves OnEnter/OnExit
                //  but stop refreshing if corrupted. I.e, some component missing or killed.
                bc.Spawner = this;
            }
        }
        public static void SetObjectProp(object o, string input)
        {
            if (string.IsNullOrEmpty(input)) return;
            try
            {
                foreach (string prop_string in input.Split(";"))
                {
                    var chunks = prop_string.Trim().Split(' ', 2);
                    string name = chunks[0];  // Movable 
                    string value = chunks[1]; // False
                    value = value.Replace("\"", "");

                    if (o is Item item)
                        item.SetObjectProp(name, value, message: false);
                    if (o is Mobile mob)
                        mob.SetObjectProp(name, value, message: false);
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        public static void SetObjectSkill(object o, string input)
        {
            try
            {
                foreach (string prop_string in input.Split(";"))
                {
                    var chunks = prop_string.Trim().Split(' ', 2);
                    string name = chunks[0];  // Swords 
                    string value = chunks[1]; // 100.3
                    value = value.Replace("\"", "");
                    bool result = SetMobileSkill(o as Mobile, name, value);
                    if (result != true)
                    {
                        if (o is Mobile mob)
                            mob.SendSystemMessage(string.Format("Could not set {0}: {1}", name, value));
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
            }
        }
        private static bool SetMobileSkill(Mobile m, string name, string value)
        {
            SkillName skill;
            Server.Skills skills = m.Skills;
            if (SkillName.TryParse(name, ignoreCase: true, out skill))
            {
                double result = 0;
                if (!double.TryParse(value, out result))
                    return false;

                skills[skill].Base = result;

                return true;
            }
            else
                return false;
        }
    }
}
