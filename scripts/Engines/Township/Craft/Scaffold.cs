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

/* Engines/Township/Craft/Scaffold.cs
 * CHANGELOG:
 *  5/21/2024, 
 *      Adam: Remove AFK check from building your township
 *  3/17/22, Adam (OnBuild)
 *      Update OnBuild to take the parm list
 *  3/12/22, Adam (PlaceAt)
 *      You can now customize the scaffold based on the item being placed.
 *      For instance, when placing a hedge, we use a 'dirt patch' as the scaffold. 
 *      Add animate and special 'dig' sound for placing 'scaffold'
 *  3/10/22, Adam (messaging)
 *      Add item appropriate messaging,:
 *      scaffold.SystemMessageWork, ie., "You begin planting.";
 *      scaffold.EmoteMesaage, ie., "*plants the bush*";
 *      scaffold.SystemMessageFinish, ie., "You plant the bush.";
 * 11/23/21, Yoar
 *	    Initial version.
 */

using Server.Commands;
using Server.ContextMenus;
using Server.Regions;
using System;
using System.Collections;

namespace Server.Township
{
    public class Scaffold : Item, IChopable
    {
        public static void Initialize()
        {
            TargetCommands.Register(new BuildScaffoldCommand());
        }

        private class BuildScaffoldCommand : BaseCommand
        {
            public BuildScaffoldCommand()
            {
                AccessLevel = AccessLevel.GameMaster;
                Supports = CommandSupport.AllItems;
                Commands = new string[] { "BuildScaffold" };
                ObjectTypes = ObjectTypes.Items;
                Usage = "BuildScaffold";
                Description = "Instantly builds the targeted township scaffold.";
            }

            public override void Execute(CommandEventArgs e, object obj)
            {
                if (obj is Scaffold)
                    ((Scaffold)obj).Build(e.Mobile);
                else
                    LogFailure("That is not a township scaffold.");
            }
        }

        private Item m_ToBuild;

        [CommandProperty(AccessLevel.GameMaster)]
        public Item ToBuild
        {
            get { return m_ToBuild; }
            set { m_ToBuild = value; InvalidateProperties(); }
        }

        public bool Buildable { get { return m_ToBuild != null && !m_ToBuild.Deleted; } }

        [Constructable]
        public Scaffold()
            : base(0xE3C)
        {
            Movable = false;
            IsLockedDown = true;
            Name = "scaffold";
        }

        public static void PlaceAt(Item item, Point3D loc, Map map)
        {
            item.MoveToIntStorage();

            Scaffold scaffold = new Scaffold();

            // customize your scaffold here
            if (item is BoxwoodHedge)
                scaffold.ItemID = 0x0914;

            scaffold.ToBuild = item;
            scaffold.MoveToWorld(loc, map);
        }

        public void Build(Mobile from)
        {
            if (m_ToBuild is ITownshipItem)
                ((ITownshipItem)m_ToBuild).OnBuild(from);

            m_ToBuild.RetrieveItemFromIntStorage(GetWorldLocation(), Map);
            TownshipItemHelper.SetOwnership(m_ToBuild, from);

            m_ToBuild = null;
            Delete();
        }

        public override void GetProperties(ObjectPropertyList list)
        {
            base.GetProperties(list);

            if (Buildable)
                list.Add(m_ToBuild.OldSchoolName());
        }

        public override void OnSingleClick(Mobile from)
        {
            if (m_ToBuild != null && ToBuild is BoxwoodHedge)
            {
                if (Buildable)
                    LabelTo(from, Utility.GetRootName(ItemID));
                else
                    base.OnSingleClick(from);
            }
            else
            {
                base.OnSingleClick(from);

                if (Buildable)
                    LabelTo(from, m_ToBuild.OldSchoolName());
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            TownshipRegion tsr = TownshipRegion.GetTownshipAt(this.GetWorldLocation(), this.Map);

            if (Buildable && tsr != null && tsr.TStone != null && tsr.TStone.AllowBuilding(from))
            {
                // hacky, but we need to start building *after* we've set from's last action time
                Timer.DelayCall(TimeSpan.Zero, delegate
                {
                    BeginBuild(from, this);
                });
            }
        }

        public override void GetContextMenuEntries(Mobile from, ArrayList list)
        {
            base.GetContextMenuEntries(from, list);

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(this.GetWorldLocation(), this.Map);

            if (Buildable && tsr != null && tsr.TStone != null && tsr.TStone.AllowBuilding(from))
                list.Add(new BuildCME());

            list.Add(new DestroyCME());
        }

        #region Build

        private class BuildCME : ContextMenuEntry
        {
            public BuildCME()
                : base(6138, 2) // Craft Item
            {
            }

            public override void OnClick()
            {
                BeginBuild(Owner.From, (Scaffold)Owner.Target);
            }
        }

        public static void BeginBuild(Mobile m, Scaffold scaffold)
        {
            if (scaffold.ToBuild == null || scaffold.ToBuild.Deleted)
                return;

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(scaffold.GetWorldLocation(), scaffold.Map);

            if (tsr == null || tsr.TStone == null || !tsr.TStone.AllowBuilding(m))
                return;

            // 5/21/2024, Adam: Remove AFK check from building your township
            //if (TownshipItemHelper.AFKCheck(m))
            //    return;

            if (!m.BeginAction(typeof(BuildTimer)))
            {
                m.SendLocalizedMessage(500119); // You must wait to perform another action.
                return;
            }

            m.RevealingAction();
            m.Direction = m.GetDirectionTo(scaffold);

            m.SendMessage("You begin {0} the {1}.", FormatVerb(scaffold.ToBuild, VerbForm.PresentParticiple), FormatItem(scaffold.ToBuild));

            new BuildTimer(m, scaffold).Start();
        }

        private class BuildTimer : WorkTimer
        {
            private Scaffold m_Scaffold;
            private int m_Count;

            public BuildTimer(Mobile m, Scaffold scaffold)
                : base(m, TownshipSettings.WallBuildTicks)
            {
                m_Scaffold = scaffold;

                DoAnimation();
            }

            protected override bool Validate()
            {
                return (!m_Scaffold.Deleted && m_Scaffold.ToBuild != null && !m_Scaffold.ToBuild.Deleted);
            }

            protected override void OnWork()
            {
                DoAnimation();
            }

            protected override void OnFinished()
            {
                Mobile.SendMessage("You {0} the {1}.", FormatVerb(m_Scaffold.ToBuild, VerbForm.PastParticiple), FormatItem(m_Scaffold.ToBuild));

                m_Scaffold.Build(Mobile);

                Mobile.EndAction(typeof(BuildTimer));
            }

            protected override void OnFailed()
            {
                Mobile.EndAction(typeof(BuildTimer));
            }

            private void DoAnimation()
            {
                Mobile.RevealingAction();
                Mobile.Direction = Mobile.GetDirectionTo(m_Scaffold);

                Animate(11, 5, 1, true, false, 0);
                Emote("*{0} the {1}*", FormatVerb(m_Scaffold.ToBuild, VerbForm.ThirdPersonSingular), FormatItem(m_Scaffold.ToBuild));

                if (m_Scaffold.ToBuild is BoxwoodHedge)
                    PlaySound(0x125 + (m_Count++ % 2)); // dig sound
            }
        }

        private enum VerbForm
        {
            BaseForm = 1,
            PastSimple,
            PastParticiple,
            ThirdPersonSingular,
            PresentParticiple,
        }

        private static string FormatVerb(Item item, VerbForm form)
        {
            if (item is BoxwoodHedge)
            {
                switch (form)
                {
                    case VerbForm.PastParticiple: return "plant";
                    case VerbForm.ThirdPersonSingular: return "plants";
                    case VerbForm.PresentParticiple: return "planting";
                }
            }
            else
            {
                switch (form)
                {
                    case VerbForm.PastParticiple: return "built";
                    case VerbForm.ThirdPersonSingular: return "builds";
                    case VerbForm.PresentParticiple: return "building";
                }
            }

            return null;
        }

        private static string FormatItem(Item item)
        {
            return item.GetBaseOldName();
        }

        #endregion

        private class DestroyCME : ContextMenuEntry
        {
            public DestroyCME()
                : base(6275, 2) // Demolish
            {
            }

            public override void OnClick()
            {
                ((Scaffold)Owner.Target).OnChop(Owner.From);
            }
        }

        public void OnChop(Mobile from)
        {
            from.CloseGump(typeof(ConfirmDestroyItemGump));
            from.SendGump(new ConfirmDestroyItemGump(this));
        }

        public override void OnAfterDelete()
        {
            base.OnAfterDelete();

            if (m_ToBuild != null)
                m_ToBuild.Delete();
        }

        #region Registration

        private Items.TownshipStone m_TStone;

        public override void OnMapChange()
        {
            base.OnMapChange();

            CheckRegistry();
        }

        public void CheckRegistry()
        {
            Unregister();

            TownshipRegion tsr = TownshipRegion.GetTownshipAt(GetWorldLocation(), Map);

            if (tsr != null && tsr.TStone != null)
            {
                m_TStone = tsr.TStone;

                if (!tsr.TStone.Scaffolds.Contains(this))
                    tsr.TStone.Scaffolds.Add(this);
            }
        }

        public void Unregister()
        {
            if (m_TStone != null)
            {
                m_TStone.Scaffolds.Remove(this);
                m_TStone = null;
            }
        }

        #endregion

        public Scaffold(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)2); // version

            writer.Write((Item)m_ToBuild);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();

            switch (version)
            {
                case 2:
                case 1:
                    {
                        if (version < 2)
                        {
                            reader.ReadString();
                            reader.ReadString();
                            reader.ReadString();
                        }

                        goto case 0;
                    }
                case 0:
                    {
                        m_ToBuild = reader.ReadItem();
                        break;
                    }
            }

            ValidationQueue<Scaffold>.Enqueue(this);
        }

        public void Validate(object state)
        {
            CheckRegistry();
        }
    }
}