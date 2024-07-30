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
using System;
using Server.Network;

namespace Server.Items
{
	public class GMStone : Item
	{
		[Constructable]
		public GMStone() : base( 0xED4 )
		{
			Movable = false;
			Hue = 0x489;
			Name = "a GM stone";
		}

		public GMStone( Serial serial ) : base( serial )
		{
		}

		public override void Serialize( GenericWriter writer )
		{
			base.Serialize( writer );

			writer.Write( (int) 0 ); // version
		}

		public override void Deserialize( GenericReader reader )
		{
			base.Deserialize( reader );

			int version = reader.ReadInt();
		}

		public override void OnDoubleClick( Mobile from )
		{
			if ( from.AccessLevel < AccessLevel.GameMaster )
			{
				from.AccessLevel = AccessLevel.GameMaster;

				from.SendAsciiMessage( 0x482, "The command prefix is \"{0}\"", Server.Commands.CommandPrefix );
				Server.Commands.CommandHandlers.Help_OnCommand( new CommandEventArgs( from, "help", "", new string[0] ) );
			}
			else
			{
				from.SendMessage( "The stone has no effect." );
			}
		}
	}
}
#endif