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

/* Scripts\Engines\Spawner\GridSpawner.cs
 * Changelog:
 *  9/5/2023, Adam
 *      Initial creation
 */

using Server.Mobiles;
using System;
using System.Collections.Generic;

namespace Server.Items
{
    public class GridSpawner : Spawner
    {
        private bool m_MustSteal = false;
        private int m_GridWidth = 0;
        private int m_GridHeight = 0;
        private double m_Sparsity = 0.0;
        [Constructable]
        public GridSpawner()
            : base()
        {
            Name = "Grid Spawner";
        }

        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public bool MustSteal { get { return m_MustSteal; } set { m_MustSteal = value; InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int GridWidth { get { return m_GridWidth; } set { m_GridWidth = value; ReCalcCount(); InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public int GridHeight { get { return m_GridHeight; } set { m_GridHeight = value; ReCalcCount(); InvalidateProperties(); } }
        [CommandProperty(AccessLevel.GameMaster, AccessLevel.Seer)]
        public double Sparsity
        {
            get { return m_Sparsity; }
            set
            {
                if (value == -1.0)  // get a random default
                    m_Sparsity = Utility.RandomMinMax(10, 100) / 100.00;
                else if (value < 0 || value > 1)
                    m_Sparsity = 0;
                else
                    m_Sparsity = value;

                ReCalcCount(); InvalidateProperties();
            }
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
        public override int Count { get { return base.Count; } set { base.Count = value; } }
        public override int HomeRange { get; set; }
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
        private int ReCalcCount()
        {
            double count = GridLocations().Count;
            if (m_Sparsity != 0)
                count *= m_Sparsity;

            base.Count = (int)count;

            // remove overspawn
            if (ObjectCount > base.Count)
                ;

            return base.Count;
        }
        #region IO
        public GridSpawner(Serial serial)
        : base(serial)
        {
        }
        public override void MoveToWorld(IEntity o, Point3D loc, Map map)
        {
            base.MoveToWorld(o, GetGridPosition(), map);
        }
        private Point3D GetGridPosition()
        {
            Point3D px = this.Location;
            List<Type> types = GetSpawnerTypes();
            List<Point3D> points = new List<Point3D>(GridLocations());
            Utility.Shuffle(points);
            foreach (Point3D loc in points)
                foreach (Type type in types)
                {
                    object o = (object)Utility.FindOneItemAt(loc, Map.Felucca, type, 2, false);
                    if (o == null)
                        return loc;
                }

            return px;
        }
        private List<Type> GetSpawnerTypes()
        {
            List<Type> types = new List<Type>();
            if (ObjectNamesRaw != null)
                foreach (object o in ObjectNamesRaw)
                    types.Add(SpawnerType.GetType(o as string));
            return types;
        }
        private List<Point3D> GridLocations()
        {
            Rectangle2D rect = new Rectangle2D(width: m_GridWidth, height: m_GridHeight, new Point2D(this.X, this.Y));
            return rect.PointsInRect3D(this.Map);
        }
        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.Write((int)0); // version
            writer.Write(m_MustSteal);
            writer.WriteEncodedInt(m_GridWidth);
            writer.WriteEncodedInt(m_GridHeight);
            writer.Write(m_Sparsity);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadInt();
            m_MustSteal = reader.ReadBool();
            m_GridWidth = reader.ReadEncodedInt();
            m_GridHeight = reader.ReadEncodedInt();
            m_Sparsity = reader.ReadDouble();
        }
        #endregion IO
    }
}