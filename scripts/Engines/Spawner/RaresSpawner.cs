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

/* Scripts\Engines\Spawner\RaresSpawner.cs
 * Changelog:
 *  7/13/2023, Adam
 *      Initial creation
 *      Spawns rares using the Loot.RareFactoryItem() system.
 *      Additionally, the spawner also carries a MustSteal property.
 *          When this flag is set, even if the item is on the ground, the item must be stolen.
 *          See Also: Stealing.cs and Item.cs
 */

using Server.Mobiles;
using System;

namespace Server.Items
{
    public class RaresSpawner : Spawner
    {
        [Constructable]
        public RaresSpawner()
            : base()
        {
            Name = "Rares Spawner";
        }

        private Loot.RareType m_rareType = Loot.RareType.invalid;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public Loot.RareType RareType { get { return m_rareType; } set { m_rareType = value; InvalidateProperties(); } }

        private bool m_mustSteal = false;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool MustSteal { get { return m_mustSteal; } set { m_mustSteal = value; InvalidateProperties(); } }

        private int m_StackableAmount = -1;
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public string StackableAmount
        {
            get { return m_StackableAmount == -1 ? "Use default" : (m_StackableAmount = Math.Min(Math.Abs(m_StackableAmount), ushort.MaxValue)).ToString(); }
            set { m_StackableAmount = int.Parse(value); InvalidateProperties(); }
        }

        #region Hide
        public override TemplateMode TemplateStyle { get; set; }
        public override bool TemplateInternalize { get; set; }
        public override int TemplateMobileDefinition { get; set; }
        public override BoolFlags Tamable { get; set; }
        public override bool StaticCorpse { get; set; }
        public override string SetProp { get; set; }
        public override string SetSkill { get; set; }
        public override UInt32 QuestCodeValue { get; set; }
        public override double QuestCodeChance { get; set; }
        public override bool CarveOverride { get; set; }
        public override int GoodiesTotalMin { get; set; }
        public override int GoodiesTotalMax { get; set; }
        public override int GoodiesRadius { get; set; }
        public override int ArtifactCount { get; set; }
        public override Item ArtifactPack { get; set; }
        public override DebugFlags DebugMobile { get; set; }
        public override bool TemplateEnabled { get; }
        public override bool DynamicCopy { get; set; }
        public override Mobile TemplateMobile { get; set; }
        public override Item TemplateItem { get; set; }
        public override int GraphicID { get; set; }
        public override Item LootPack { get; set; }
        public override Item CarvePack { get; set; }
        public override string CarveMessage { get; set; }
        public override UInt32 PatchID { get; set; }
        public override DiceEntry GoldDice { get; set; }
        public override bool Invulnerable { get; set; }
        public override bool Exhibit { get; set; }
        public override string Counts { get; set; }
        public override WayPoint WayPoint { get; set; }
        public override string NavDestination { get; set; }
        public override Direction MobileDirection { get; set; }
        public override int WalkRange { get; set; }
        public override bool WalkRangeCalc { get; set; }
        public override int Team { get; set; }
        public override bool Group { get; set; }
        public override SpawnerModeAttribs Distro { get; set; }
        public override bool ModeAI { get; set; }
        public override bool ModeNeruns { get; set; }
        public override bool ModeMulti { get; set; }
        public override bool GuardIgnore { get; set; }
        public override bool Dummy { get; set; }
        public override bool Debug { get; set; }
        public override bool NeedsReview { get; set; }
        public override string Source { get; set; }
        public override ShardConfig Shard { get; set; }
        public override bool Concentric { get; set; }
        public override bool CoreSpawn { get; set; }

        #endregion
        public override bool OkayStart()
        {
            return (RareType != Loot.RareType.invalid);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel >= AccessLevel.Seer)
            {
                from.SendGump(new Server.Gumps.PropertiesGump(from, this));
            }
        }
        /*public object[] RareTypes()
        {
            if (RareType == Loot.RareType.invalid)
                return new Type[0];

            return Loot.RareFactoryTypes(RareType);
        }*/
        public override void Spawn()
        {
            if (RareType == Loot.RareType.invalid)
                // nothing to do
                return;

            if (IsFull)
                // nothing to do
                return;

            // defrag done here...
            int nelts = Math.Max(0, Count - ObjectCount);

            if (nelts == 0)
                // Logic Error: nothing to do
                return;

            for (int ix = 0; ix < nelts; ix++)
            {
                Item rare = Loot.RareFactoryItem(1.0, RareType);
                if (rare != null)
                {
                    Objects.Add(rare);
                    InvalidateProperties();
                    rare.MoveToWorld(GetSpawnPosition(rare), this.Map);
                    OnAfterItemSpawn(rare);
                    rare.OnAfterSpawn();

                    if (MustSteal)
                        rare.SetItemBool(ItemBoolTable.MustSteal, true);

                    // Adam: Warn if we're spawning stuff in a house.
                    //	This is a problem because the item will be orphaned by the spawner (? - not sure this is the case anymore)
                    //	which can lead to excessive item generation.
                    if (InHouse(rare) == true)
                    {
                        Console.WriteLine("Warning: House spawn: Item({0}, {1}, {2}), Spawner({3}, {4}, {5})",
                            rare.Location.X, rare.Location.Y, rare.Location.Z, this.Location.X, this.Location.Y, this.Location.Z);
                    }
                }
            }

        }
        protected override void OnAfterItemSpawn(Item item)
        {
            base.OnAfterItemSpawn(item);
            if (item.Stackable && m_StackableAmount != -1)
            {
                item.Amount = m_StackableAmount;
            }
        }
        #region IO
        public RaresSpawner(Serial serial)
        : base(serial)
        {
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)1); // version

            // version 1
            writer.Write(m_StackableAmount);

            // version 0
            writer.Write((int)m_rareType);
            writer.Write(m_mustSteal);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            switch (version)
            {
                case 1:
                    {
                        m_StackableAmount = reader.ReadInt();
                        goto case 0;
                    }
                case 0:
                    {
                        m_rareType = (Loot.RareType)reader.ReadInt();
                        m_mustSteal = reader.ReadBool();
                        break;
                    }
            }
        }
        #endregion IO
    }
}