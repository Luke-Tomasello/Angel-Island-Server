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

/* Scripts\Items\Misc\SerpentPillar.cs
 * CHANGELOG:
 *  5/19/2023, Adam
 *      check AllowSerpentPillars()
 *      We need this instead of Core.T2A since Core.T2A is the expansion, and we are a T2A expansion,
 *      but we still don't allow access to the lost lands. 
 *	2/25/11, Adam
 *		check Core.T2A variable to tell us if T2A is available for this shard
 */

using Server.Multis;

namespace Server.Items
{
    public class SerpentPillar : Item
    {
        private bool m_Active;
        private string m_Word;
        private Rectangle2D m_Destination;

        [CommandProperty(AccessLevel.GameMaster)]
        public bool Active
        {
            get { return m_Active; }
            set { m_Active = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public string Word
        {
            get { return m_Word; }
            set { m_Word = value; }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public Rectangle2D Destination
        {
            get { return m_Destination; }
            set { m_Destination = value; }
        }

        [Constructable]
        public SerpentPillar()
            : this(null, new Rectangle2D(), false)
        {
        }

        public SerpentPillar(string word, Rectangle2D destination)
            : this(word, destination, true)
        {
        }

        public SerpentPillar(string word, Rectangle2D destination, bool active)
            : base(0x233F)
        {
            Movable = false;

            m_Active = active;
            m_Word = word;
            m_Destination = destination;
        }

        public override bool HandlesOnSpeech { get { return true; } }

        public override void OnSpeech(SpeechEventArgs e)
        {
            Mobile from = e.Mobile;

            if (!e.Handled && from.InRange(this, 10) && e.Speech.ToLower() == this.Word)
            {
                BaseBoat boat = BaseBoat.FindBoatAt(from, from.Map);

                if (boat == null)
                    return;

                if (!this.Active || !Core.RuleSets.AllowLostLandsAccess())
                {
                    if (boat.TillerMan != null)
                        boat.TillerMan.Say(502507); // Ar, Legend has it that these pillars are inactive! No man knows how it might be undone!

                    return;
                }

                Map map = from.Map;

                for (int i = 0; i < 5; i++) // Try 5 times
                {
                    int x = Utility.Random(Destination.X, Destination.Width);
                    int y = Utility.Random(Destination.Y, Destination.Height);
                    int z = map.GetAverageZ(x, y);

                    Point3D dest = new Point3D(x, y, z);

                    if (boat.CanFit(dest, map))
                    {
                        int xOffset = x - boat.X;
                        int yOffset = y - boat.Y;
                        int zOffset = z - boat.Z;

                        boat.Teleport(xOffset, yOffset, zOffset);

                        return;
                    }
                }

                if (boat.TillerMan != null)
                    boat.TillerMan.Say(502508); // Ar, I refuse to take that matey through here!
            }
        }

        public SerpentPillar(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);

            writer.WriteEncodedInt(0); // version

            writer.Write((bool)m_Active);
            writer.Write((string)m_Word);
            writer.Write((Rectangle2D)m_Destination);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);

            int version = reader.ReadEncodedInt();

            m_Active = reader.ReadBool();
            m_Word = reader.ReadString();
            m_Destination = reader.ReadRect2D();
        }
    }
}