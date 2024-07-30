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

/* Scripts/Multis/Tent/TentComponents.cs
 * ChangeLog:
 *	8/1/22, Yoar
 *	    - Cleaned up TentBedRoll.OnDoubleClick handler; no longer need the try-catch
 *		- Mortalis: Added inheritance mechanism that allows a new character to inherit the tent
 *		  previously owned by another character on the same account
 *	6/28/2021, Adam: Remove the ability of 'party members' to access the tent bag
 *	07/24/08, weaver
 *		Made sure only owners and their party members can drop items into the tent pack.
 *	09/05/07, plasma
 *		Implement ITelekinesisable in TentBackPack to prevent telekenisis from openeing backpack
 *		and bypassing the security.
 *	8/22/07, Adam
 *		Add Asserts to help diagnose periodic exceptions
 *	7/16/07, Adam
 *		Crash fix
 *		Add checks to make sure that the m_baseHouse.Owner, and m_baseHouse.Owner.Account are 
 *		'valid' before making use of them in OnSingleClick()
 *		In the case where the tent owner is deleted, .Owner is valid, but .Owner.Account is null
 *	12/06/06, Pix
 *		Cleaned up TentBackpack.OnDoubleClick - added null checks.
 *	9/15/06, Adam
 *		Add trycatch to stop server from crashing:
 *			System.NullReferenceException: Object reference not set to an instance of an object.
 *				at Server.Items.TentBackpack.OnDoubleClick(Mobile from)
 *	9/07/06, weaver
 *		Added initialision of Placed flag to true on deserialisation + public accessor.
 *	9/02/06, weaver
 *		Added bool to tentpack to indicate whether or not the tent has been placed.
 *		Added check to double click and dragdrop overrides.
 *	08/16/06, weaver
 *		Moved up null check to properly cover tent access (Owner should never be null
 *		due to the housing system - it's just a reference to m_BaseHouse.Owner - so
 *		this is purely a sanity check).
 *	08/16/06, weaver
 *		Added LinkedHouse as public accessor to m_BaseHouse.
 *	08/14/06, weaver
 *		- Altered security for tents to use house region
 *		- Allowed GMs to access packs of tents where owner is set to null
 *	08/03/06, weaver
 *		- Fixed so > accesslevel can access tent packs, not >=.
 *		- Removed incorrect message of access being denied when access is granted
 *	08/03/06, weaver
 *		Allowed Counselor+ access level users to pack all tents away.
 *	08/03/06, weaver
 *		- Altered decay labelling to match that of other houses.
 *		- Added public accessors for freezedecay and decayminutesstored.
 *		- Added removal of vendors on tent collapse.
 *	07/27/06, weaver
 *		Stopped siege tents from being packable.
 *	05/31/06, weaver
 *		- Added tent poles.
 *		- Added decay time to tent backpack.
 *	05/22/06, weaver
 *		- Added handling for Siege tents.
 *		- Added distance check to tent packup request.
 *	05/17/06, weaver
 *		Removed east side, added gaps in south side instead.
 *	05/07/06, weaver
 *		- Made TentBackPack accessible to >= as opposed to > AccessLevel mobiles.
 *		- Added public accessors to Owner mobile and Owner's account.
 *	05/01/06, weaver
 *		Initial creation. Graphically based on Dupre's tent scripts.
 */

using Server.Accounting;
using Server.Diagnostics;
using Server.Gumps;
using Server.Multis;
using Server.Network;
using System;

namespace Server.Items
{
    public enum TentStyle
    {
        Newbie,
        Siege
    }

    public class TentBedRoll : Item
    {
        private BaseHouse m_Structure;

        [Constructable]
        public TentBedRoll(BaseHouse bh)
            : base(2645)
        {
            m_Structure = bh;
            Name = "tent bedroll";
        }

        [CommandProperty(AccessLevel.Counselor, AccessLevel.Administrator)]
        public bool FreezeDecay
        {
            get
            {
                return m_Structure.NeverDecay;
            }
            set
            {
                m_Structure.NeverDecay = value;
            }
        }

        [CommandProperty(AccessLevel.GameMaster)]
        public double HouseDecayMinutesStored
        {
            get
            {
                return m_Structure.DecayMinutesStored;
            }
            set
            {
                m_Structure.DecayMinutesStored = value;
            }
        }

        public TentBedRoll(Serial serial)
            : base(serial)
        {
        }

        public override void OnSingleClick(Mobile from)
        {
            try
            {
                base.OnSingleClick(from);
                bool valid = m_Structure.Owner != null && m_Structure.Owner.Account != null;


                if (from.AccessLevel >= AccessLevel.GameMaster)
                {
                    base.LabelTo(from, "[GM Info Only: Collapse Time: {0}]", DateTime.UtcNow + TimeSpan.FromMinutes(m_Structure.DecayMinutesStored));
                }

                base.LabelTo(from, m_Structure.DecayState());

                //// Display decay info to owner or Counselor+
                //if (from.AccessLevel >= AccessLevel.Counselor)
                //{
                //    this.LabelTo(from, string.Format("[Staff Info Only] This tent will fall apart at {0}", DateTime.UtcNow + TimeSpan.FromMinutes(m_BaseHouse.DecayMinutesStored)));
                //}

                //base.LabelTo(from, m_BaseHouse.DecayState());

                //else if (valid && from.Account == m_BaseHouse.Owner.Account)
                //{
                //    TimeSpan decay = m_BaseHouse.StructureDecayTime - DateTime.UtcNow;
                //    int days = decay.Days;
                //    double hours = decay.Hours;
                //    hours += ((double)decay.Minutes) / 60;
                //    string decaystring = string.Format("{0} days, {1:0.0} hours", days, hours);

                //    base.LabelTo(from, "This tent will fall apart in " + decaystring);
                //}
                //else
                //{
                //    base.LabelTo(from, m_BaseHouse.DecayState());
                //}
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
            }
        }

        public override void OnDoubleClick(Mobile from)
        {
            if (m_Structure is Tent || m_Structure is SiegeTent)
            {
                if (!from.InRange(this.Location, 2))
                {
                    from.LocalOverheadMessage(MessageType.Regular, 0x3B2, 1019045); // I can't reach that.
                    return;
                }

                // Mortalis house inheritance - from one character to the next (of kin)
                if (Core.RuleSets.MortalisRules())
                {
                    if (m_Structure.Owner == null || m_Structure.Owner.Deleted)
                    {
                        if (m_Structure.CheckInheritance(from))
                        {
                            m_Structure.Owner = from;

                            if (m_Structure.Public == false)
                                m_Structure.ChangeLocks(from);
                        }
                    }

                    // fall through
                }

                if (!m_Structure.CheckAccount(from, m_Structure.Owner))
                {
                    from.SendMessage("Only the tent's owner can pack it away.");
                    return;
                }
                else if (m_Structure is SiegeTent && from.AccessLevel == AccessLevel.Player)
                {
                    from.SendMessage("Siege tents cannot be packed away. You must wait for it to decay.");
                    return;
                }

                Container pack;

                if (m_Structure is SiegeTent)
                    pack = ((SiegeTent)m_Structure).TentPack;
                else
                    pack = ((Tent)m_Structure).TentPack;

                if (pack != null && pack.Items.Count > 0)
                {
                    from.SendMessage("You must empty your tent's pack before you can pack it away.");
                    return;
                }

                from.SendGump(new TentPackGump(from, m_Structure));
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version

            writer.Write(m_Structure);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            m_Structure = (BaseHouse)reader.ReadItem();
        }
    }

    public class TentBackpack : Backpack, ITelekinesisable
    {
        // wea: 14/Aug/2006 Added BaseHouse and relinked so tent access handled via here
        private BaseHouse m_BaseHouse;

        private bool m_Placed;

        [CommandProperty(AccessLevel.Counselor)]
        public bool Placed
        {
            get
            {
                return m_Placed;
            }
            set
            {
                m_Placed = value;
            }
        }

        // wea: 16/Aug/2006 Added LinkedTent as public accessor to m_BaseHouse
        [CommandProperty(AccessLevel.Counselor)]
        public BaseHouse Structure
        {
            get
            {
                return m_BaseHouse;
            }
            set
            {
                m_BaseHouse = value;
            }
        }

        [CommandProperty(AccessLevel.Counselor)]
        public Mobile Owner
        {
            get
            {
                if (m_BaseHouse != null)
                    return m_BaseHouse.Owner;
                else
                    return null;
            }
        }
        [CommandProperty(AccessLevel.Counselor)]
        public IAccount OwnerAccount
        {
            get
            {
                return m_BaseHouse.Owner.Account;
            }
        }

        [Constructable]
        public TentBackpack(BaseHouse bh)
        {
            Name = "tent pack";
            m_BaseHouse = bh;
        }

        public TentBackpack(Serial serial)
            : base(serial)
        {
        }

        public void OnTelekinesis(Mobile from)
        { //pla: 09/05/07
          //Do nothing, telekinesis doesn't work on a tent backpack.
        }

        // 6/28/2021, Adam: Remove the ability of 'party members' to access the tent bag
        // wea: 2/Sep/2006 - Prevent non placed tents having items dropped into them
        public override bool OnDragDrop(Mobile from, Item dropped)
        {
            if (Placed == false)
            {
                from.SendMessage("This tent has not yet been fully erected.");
                return false;
            }

            // wea: 24/Ju/2008 - Made sure only owners and their party members can drop items into the tent pack
            if ((Owner != null && (Owner == from || Owner.AccessLevel < from.AccessLevel || Owner.Account == from.Account /*|| (Owner.Party != null && Owner.Party == from.Party)*/))
                    || (Owner == null && from.AccessLevel > AccessLevel.Player))
            {
                return base.OnDragDrop(from, dropped);
            }

            //from.SendMessage("Only the owner and their party members have access to the tent bag.");
            from.SendMessage("That is secure.");
            return false;
        }

        // 6/28/2021, Adam: Remove the ability of 'party members' to access the tent bag
        // Override standard secure handling so only party members can access
        public override void OnDoubleClick(Mobile from)
        {
            try
            {
                if (from == null) return;

                bool bCanOpen = false;

                // wea: 2/Sep/2006 - Added "placed" check to make sure tent has been confirmed
                if (Placed == false)
                {
                    from.SendMessage("This tent has not yet been fully erected.");
                    return;
                }
                else if (Owner == null)
                {
                    if (from.AccessLevel > AccessLevel.Player)
                    {
                        bCanOpen = true;
                    }
                }
                else if (from.AccessLevel > Owner.AccessLevel)
                {
                    bCanOpen = true;
                }
                else if (Owner.Account == from.Account)
                {
                    bCanOpen = true;
                }
                /*else if (Owner.Party != null)
				{
					Party p = Server.Engines.PartySystem.Party.Get(Owner);

					if (p != null)
					{
						foreach (PartyMemberInfo mi in p.Members)
						{
							Mobile m = mi.Mobile;
							if (m != null && m == from)
							{
								bCanOpen = true;
							}
						}
					}
				}*/

                if (bCanOpen)
                {
                    base.OnDoubleClick(from);
                }
                else
                {
                    //from.SendMessage("Only the owner and their party members have access to the tent bag.");
                    from.SendMessage("That is secure.");
                }
            }
            catch (Exception e)
            {
                LogHelper.LogException(e);
                System.Console.WriteLine("Exception Caught in TentBackpack.OnDoubleClick() code: " + e.Message);
                System.Console.WriteLine(e.StackTrace);
            }
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)1); // version

            //writer.Write( m_BaseHouse );
            writer.Write(m_BaseHouse);
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();

            // wea: 14/Aug/2006 Added code to calculate and link to tent

            switch (version)
            {
                case 1:
                    Placed = true; // wea: 09/07/2006 initialise this to true on server re-load (gump would die)
                    m_BaseHouse = (BaseHouse)reader.ReadItem();
                    goto case 0;

                case 0:

                    if (version == 0)
                    {   /*
						Region r = (Region.Find( this.Location, Map.Felucca));
						if( r is HouseRegion )
						{	
							m_BaseHouse = ((HouseRegion) r).House;
							Console.WriteLine("HOUSE REGION DETECTED!! Location is {0}", this.Location);
						}
						else
						{
							Console.WriteLine("HOUSE REGION NOT DETECTED! Location is {0}", this.Location);
						}
						*/
                        // Old security data
                        reader.ReadMobile();
                    }

                    break;
            }
        }
    }

    public class TentWalls : BaseAddon
    {

        [Constructable]
        public TentWalls(TentStyle style)
        {
            Name = "tent walls";

            int wallmod = 0;

            if (style == TentStyle.Siege)
                wallmod = -174;

            // Walls
            // Corners - Clockwise from SE
            AddComponent(new AddonComponent(0x2DE + wallmod), 3, 3, 0);
            AddComponent(new AddonComponent(0x2E2 + wallmod), -2, 3, 0);
            AddComponent(new AddonComponent(0x2E1 + wallmod), -2, -2, 0);
            AddComponent(new AddonComponent(0x2E3 + wallmod), 3, -2, 0);

            // East Side
            AddComponent(new AddonComponent(0x2E0 + wallmod), 3, 2, 0);   // o
            AddComponent(new AddonComponent(0x2E0 + wallmod), 3, 1, 0);
            AddComponent(new AddonComponent(0x2E0 + wallmod), 3, 0, 0);
            AddComponent(new AddonComponent(0x2E0 + wallmod), 3, -1, 0);  // o


            // South Side
            AddComponent(new AddonComponent(0x2DF + wallmod), 2, 3, 0);
            AddComponent(new AddonComponent(0x2DF + wallmod), -1, 3, 0);

            // West Side
            AddComponent(new AddonComponent(0x2E5 + wallmod), -2, 2, 0);
            AddComponent(new AddonComponent(0x2E5 + wallmod), -2, 1, 0);
            AddComponent(new AddonComponent(0x2E5 + wallmod), -2, 0, 0);
            AddComponent(new AddonComponent(0x2E5 + wallmod), -2, -1, 0);

            // North Side
            AddComponent(new AddonComponent(0x2E4 + wallmod), -1, -2, 0);
            AddComponent(new AddonComponent(0x2E4 + wallmod), 0, -2, 0);
            AddComponent(new AddonComponent(0x2E4 + wallmod), 1, -2, 0);
            AddComponent(new AddonComponent(0x2E4 + wallmod), 2, -2, 0);


        }

        public TentWalls(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }


    public class TentRoof : BaseAddon
    {

        [Constructable]
        public TentRoof(int roofhue)
        {
            Name = "tent roof";

            // ROOF
            AddComponent(new AddonComponent(0x604), 3, 3, 20);
            AddComponent(new AddonComponent(0x601), 2, 3, 20);
            AddComponent(new AddonComponent(0x601), 1, 3, 20);
            AddComponent(new AddonComponent(0x601), 0, 3, 20);
            AddComponent(new AddonComponent(0x607), -1, 3, 20);

            AddComponent(new AddonComponent(0x602), 3, 2, 20);
            AddComponent(new AddonComponent(0x604), 2, 2, 23);
            AddComponent(new AddonComponent(0x601), 1, 2, 23);
            AddComponent(new AddonComponent(0x607), 1, 3, 34);
            AddComponent(new AddonComponent(0x5FF), -1, 2, 20);

            AddComponent(new AddonComponent(0x602), 3, 1, 20);
            AddComponent(new AddonComponent(0x602), 2, 1, 23);
            AddComponent(new AddonComponent(0x608), 1, 1, 31);
            AddComponent(new AddonComponent(0x5FF), 0, 1, 23);
            AddComponent(new AddonComponent(0x5FF), -1, 1, 20);

            AddComponent(new AddonComponent(0x602), 3, 0, 20);
            AddComponent(new AddonComponent(0x605), 2, 0, 23);
            AddComponent(new AddonComponent(0x600), 1, 0, 23);
            AddComponent(new AddonComponent(0x606), 0, 0, 23);
            AddComponent(new AddonComponent(0x5FF), -1, 0, 20);

            AddComponent(new AddonComponent(0x605), 3, -1, 20);
            AddComponent(new AddonComponent(0x600), 2, -1, 20);
            AddComponent(new AddonComponent(0x600), 1, -1, 20);
            AddComponent(new AddonComponent(0x600), 0, -1, 20);
            AddComponent(new AddonComponent(0x606), -1, -1, 20);

            Hue = roofhue;
        }

        public TentRoof(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TentFloor : BaseAddon
    {
        [Constructable]
        public TentFloor()
        {
            Name = "tent floor";

            // CARPET 5997.1682.0 - 3 3 0
            AddComponent(new AddonComponent(0xABA), 2, 2, 0);
            AddComponent(new AddonComponent(0xAB6), 1, 2, 0);
            AddComponent(new AddonComponent(0xAB6), 0, 2, 0);
            AddComponent(new AddonComponent(0xABB), -1, 2, 0);

            AddComponent(new AddonComponent(0xAB5), 2, 1, 0);
            AddComponent(new AddonComponent(0xAB3), 1, 1, 0);
            AddComponent(new AddonComponent(0xAB3), 0, 1, 0);
            AddComponent(new AddonComponent(0xAB7), -1, 1, 0);

            AddComponent(new AddonComponent(0xAB5), 2, 0, 0);
            AddComponent(new AddonComponent(0xAB3), 1, 0, 0);
            AddComponent(new AddonComponent(0xAB3), 0, 0, 0);
            AddComponent(new AddonComponent(0xAB7), -1, 0, 0);

            AddComponent(new AddonComponent(0xAB9), 2, -1, 0);
            AddComponent(new AddonComponent(0xAB4), 1, -1, 0);
            AddComponent(new AddonComponent(0xAB4), 0, -1, 0);
            AddComponent(new AddonComponent(0xAB8), -1, -1, 0);
        }

        public TentFloor(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TentTrim : BaseAddon
    {
        [Constructable]
        public TentTrim()
        {

            Name = "tent frills";

            // EDGES

            // East side
            AddComponent(new AddonComponent(0x379), 4, 2, -5);
            AddComponent(new AddonComponent(0x379), 4, 1, -5);
            AddComponent(new AddonComponent(0x379), 4, 0, -5);
            AddComponent(new AddonComponent(0x379), 4, -1, -5);
            AddComponent(new AddonComponent(0x379), 4, -2, -5);

            // South side
            AddComponent(new AddonComponent(0x378), 2, 4, -5);
            AddComponent(new AddonComponent(0x378), 1, 4, -5);
            AddComponent(new AddonComponent(0x378), 0, 4, -5);
            AddComponent(new AddonComponent(0x378), -1, 4, -5);
            AddComponent(new AddonComponent(0x378), -2, 4, -5);
        }

        public TentTrim(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }

    public class TentPole : Item
    {
        [Constructable]
        public TentPole()
            : base(4758)
        {
            Name = "tent pole";
        }

        public TentPole(Serial serial)
            : base(serial)
        {
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write((int)0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
        }
    }
}