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
 *      - Initial checkin
 *      - See Spawner.History.txt for a historical record
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
