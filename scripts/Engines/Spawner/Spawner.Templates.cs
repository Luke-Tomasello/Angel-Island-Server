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

/* Scripts\Engines\Spawner\Spawner.Templates.cs
 * CHANGELOG:
 *   5/22/2026, Adam:
 *      - New Module checkin
 *  3/14/2024, Adam (CustomMobile())
 *      Disallow custom mobiles from being tamed.
 *  12/10/1023, Adam
 *      Undo Christmas event hack to spawner template model.
 *      Hack: Was leaving orphaned template mobiles/items if they are on one map vs another
 *      Fix: Add reference counting on template mobiles/items/loot packs, and carve packs. Now all of these can be shared across multiple spawners.
 *      Note: When the last linking spawner is deleted, the template is deleted, regardless of map.
 *  5/23/22, Adam (CopyLayers)
 *      Make sure the item to copy has a Parameterless Constructor
 *      i.e., StuddedGlovesOfMining don't have such a constructor
 *	6/27/2021, Adam
 *		Rewrite all the template stuff to actually work and not be crappy-buggy.
 *	11/1/07, Adam
 *		When using target mode to setup a Template Item or Mobile, set/clear the SpawnerTempMob or SpawnerTempItem for the template
 *		We need to clear it for the old Template so that it will decay as per usual, and set it for the new one so that it does not 
 *		decay. 
 *		Caution: Don't use anything spawned by a spawner (still on the spawner list) as a template as the spawner will delete it
 *		during a defrag.
 *  10/19/06, Kit
 *		Made template mobs spawner property be set to spawner they are on.
 *	9/15/06, Adam
 *		Add function to create a template object only if the Type of the existing template
 *			object has changed. The prevents complex templates from being lost when someone
 *			simply hits 'OK' on the Spawner gump
 *  8/16/06, Kit
 *		Made static copy of mobs copy layers and property of those layers to new destination mob.
 *  7/02/06, Kit
 *		Added, property DynamicCopy, if enabled call InitOutfit/InitBody routines of mob.
 *		Added set NameHue to -1(use notoriety) for vendors spawned with invulnerability turned off.
 *		Fixed bug with namehue being set on all spawners.
 *		Added call to InitOutfit() for normal template spawns to prevent cross dressing.
 *	6/30/06, Adam
 *		- remove evil code nulling templates on every spawn if Enable is not set
 *		- Have TemplateEnable invoke appropriate Template Creation / Destruction code
 *		- Propagate Lifespan for Mobiles and LastMove for items (manages decay/cleanup)
 *		- move template creation/management into Spawner class.
 *		- make sure template is only created on the first object specified.
 *		- make TemplateEnable readable by GMs, but writable by Seers
 *  06/27/06, Kit
 *		Added templated ability to spawners, full range of props setting for spawner entry.
 *		Set via TemplateMobile -> view props, or TemplateItem -> view props, packed out old data.
 */

using System;

namespace Server.Mobiles
{
    public partial class Spawner
    {
        #region Templates
        public enum TemplateMode : byte
        {
            CopyProperties,
            Dupe
        }
        //private TemplateMode m_TemplateMode = TemplateMode.CopyProperties;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual TemplateMode TemplateStyle
        {
            get { return GetFlag(SpawnerAttribs.TemplateMode) ? TemplateMode.Dupe : TemplateMode.CopyProperties; }
            set
            {
                TemplateMode old_style = TemplateStyle;
                if (value == TemplateMode.Dupe)
                    SetFlag(SpawnerAttribs.TemplateMode, true);
                else
                    SetFlag(SpawnerAttribs.TemplateMode, false);
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual bool TemplateEnabled
        {
            get
            {
                return m_TemplateMobile != null || m_TemplateItem != null;
            }
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public virtual bool DynamicCopy
        {
            get
            {
                return m_DynamicCopy;
            }
            set
            {
                m_DynamicCopy = value;
                InvalidateProperties();
            }
        }

        //hold object for templated spawner use.
        [CopyableAttribute(CopyType.DoNotRelink)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Mobile TemplateMobile
        {
            get { return m_TemplateMobile; }
            set
            {
                if (m_TemplateMobile == value)
                    return;

                if (value != null && value.Spawner != null)
                {
                    try
                    {
                        value.Spawner.Objects.Remove(value);
                        value.Spawner = null;
                    }
                    catch
                    {
                        SendMessageNearby("Failed to unlink Mobile from its spawner.");
                        return;
                    }
                }

                if (m_TemplateMobile != null)
                {
                    m_TemplateMobile.SpawnerTempRefCount--;

                    if (CanDeleteTemplate(m_TemplateMobile))
                        m_TemplateMobile.Delete();
                }

                m_TemplateMobile = value;

                if (m_TemplateMobile != null)
                {
                    m_TemplateMobile.SpawnerTempMob = true;
                    m_TemplateMobile.IsStaffOwned = true; // prevents pre-launch shard wipes from deleting these

                    if (m_TemplateInternalize)
                        m_TemplateMobile.MoveToIntStorage(true);

                    m_TemplateMobile.SpawnerTempRefCount++;
                }
            }
        }

        public bool CustomMobile()
        {
            bool template_mobile = m_TemplateMobile != null;
            bool custom_skills = !string.IsNullOrEmpty(m_SetSkill);
            bool custom_props = !string.IsNullOrEmpty(m_SetProp);
            bool loot_pack = m_LootPack != null;
            bool artifact_pack = m_ArtifactPack != null;
            bool carve_pack = m_CarvePack != null;
            bool loot_string = m_LootString != null;
            if (template_mobile || custom_skills || custom_props || loot_pack || artifact_pack || carve_pack || loot_string || BlockDamage)
                return true;
            return false;
        }
        public enum BoolFlags : byte { Default, True, False }
        private BoolFlags m_Tamable = BoolFlags.Default;
        [CommandProperty(AccessLevel.GameMaster)]
        public virtual BoolFlags Tamable
        {
            get
            {
                if (!CustomMobile() || TamableOverride)
                    return m_Tamable;
                else
                    return BoolFlags.False;
            }
            set
            {
                if (CustomMobile() && !TamableOverride)
                    this.SendMessageNearby("You cannot set the tamability of a custom creature");
                else
                    m_Tamable = value;
            }

        }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool TamableOverride
        {
            get { return GetFlag(SpawnerAttribs.TamableOverride); }
            set { SetFlag(SpawnerAttribs.TamableOverride, value); }
        }
        //hold object for templated spawner use.
        [CopyableAttribute(CopyType.DoNotRelink)]
        [CommandProperty(AccessLevel.Seer)]
        public virtual Item TemplateItem
        {
            get { return m_TemplateItem; }
            set
            {
                if (m_TemplateItem == value)
                    return;

                if (value != null && value.Spawner != null)
                {
                    try
                    {
                        value.Spawner.Objects.Remove(value);
                        value.Spawner = null;
                    }
                    catch
                    {
                        SendMessageNearby("Failed to unlink Item from its spawner.");
                        return;
                    }
                }

                if (m_TemplateItem != null)
                {
                    m_TemplateItem.SpawnerTempRefCount--;

                    if (CanDeleteTemplate(m_TemplateItem))
                        m_TemplateItem.Delete();
                }

                m_TemplateItem = value;

                if (m_TemplateItem != null)
                {
                    m_TemplateItem.SpawnerTempItem = true;

                    if (m_TemplateInternalize)
                        m_TemplateItem.MoveToIntStorage(true);

                    m_TemplateItem.SpawnerTempRefCount++;
                }
            }
        }

        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public virtual bool TemplateInternalize
        {   // abused by staff resulting in a mess of crap sprinkled all around the surface map, and incompatible with the freeze dry system.
            get { return m_TemplateInternalize; }
            set { m_TemplateInternalize = value; }
        }

        private int m_TemplateMobileDefinition;
        [CopyableAttribute(CopyType.DoNotCopy)]
        [CommandProperty(AccessLevel.Owner, AccessLevel.Owner)]
        public virtual int TemplateMobileDefinition
        {
            get { return m_TemplateMobileDefinition; }
            set { m_TemplateMobileDefinition = value; }
        }
        #endregion

        public bool CheckTemplate()
        {
            if (!TemplateEnabled)
                return true;

            foreach (string name in ObjectNamesRaw)
            {
                Type? t = ScriptCompiler.FindTypeByName(name);
                if (t == null)
                    return false;

                if (t.IsAssignableTo(typeof(Item)) && TemplateItem == null)
                    return false;

                if (t.IsAssignableTo(typeof(Mobile)) && TemplateMobile == null)
                    return false;
            }

            return true;
        }
    }
}
