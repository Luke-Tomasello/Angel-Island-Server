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

/* Scripts\Engines\CoreManagement\ConsoleBook.cs
 * CHANGELOG:
 *  3/18/2026, Adam
 *  First time checkin
 *  Runebook-style manager for all BaseConsole-derived items.
 *  Auto-discovers every console via reflection.
 */

using Server.Gumps;
using Server.Items;
using Server.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Server.Engines.Consoles
{
    [Flipable(0xEFA, 0xEFF)]
    public class ConsoleBook : Item
    {
        internal List<ConsoleEntry> _consoles = new List<ConsoleEntry>();

        [Constructable]
        public ConsoleBook()
            : base(0xEFA)
        {
            Name = "Console Management Book";
            Hue = Utility.RandomSpecialHue(Name);
            Weight = 1.0;
            LootType = LootType.Blessed;

            BuildConsoleList();
        }

        public ConsoleBook(Serial serial) : base(serial) { }
        private void BuildConsoleList()
        {
            _consoles.Clear();

            var types = Assembly.GetExecutingAssembly().GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseConsole)) && !t.IsAbstract)
                .Select(t => new
                {
                    Type = t,
                    Ctor = t.GetConstructor(Type.EmptyTypes)
                })
                .Where(x => x.Ctor != null)
                .Select(x =>
                {
                    string displayName = Utility.LooksCamelCase(x.Type.Name)
                        ? Utility.SplitCamelCase(x.Type.Name)
                        : x.Type.Name;

                    return new ConsoleEntry(x.Type, displayName);
                })
                .OrderBy(x => x.Name);

            _consoles.AddRange(types);
        }
        public override void OnDoubleClick(Mobile from)
        {
            if (from.AccessLevel < AccessLevel.Administrator)
            {
                from.SendMessage("You are not authorized to use this book.");
                return;
            }

            from.SendGump(new ConsoleBookGump(this, from, 0));
        }

        public override void Serialize(GenericWriter writer)
        {
            base.Serialize(writer);
            writer.Write(0); // version
        }

        public override void Deserialize(GenericReader reader)
        {
            base.Deserialize(reader);
            int version = reader.ReadInt();
            BuildConsoleList();
        }

        internal class ConsoleEntry
        {
            public Type Type { get; }
            public string Name { get; }
            public ConsoleEntry(Type t, string n) { Type = t; Name = n; }
        }
    }

    // ====================== Clean GUMP ======================
    public class ConsoleBookGump : Gump
    {
        private readonly ConsoleBook _book;
        private readonly Mobile _from;
        private readonly int _page;

        private const int EntriesPerPage = 12;

        public ConsoleBookGump(ConsoleBook book, Mobile from, int page) : base(80, 40)
        {
            _book = book;
            _from = from;
            _page = page;

            int totalPages = (_book._consoles.Count + EntriesPerPage - 1) / EntriesPerPage;
            int currentPage = Math.Max(0, Math.Min(_page, totalPages - 1));

            int width = 320;
            int height = 480;   // Extra height for clean bottom row

            AddPage(0);
            AddBackground(0, 0, width, height, 5054);
            AddImageTiled(10, 10, width - 20, 20, 2624);

            AddHtml(15, 12, width - 30, 20, "<CENTER><BASEFONT COLOR=#FFFFFF>Console Management Book</BASEFONT></CENTER>", false, false);

            int startIndex = currentPage * EntriesPerPage;
            int y = 50;
            int buttonId = 1;

            for (int i = 0; i < EntriesPerPage && startIndex + i < _book._consoles.Count; i++)
            {
                var entry = _book._consoles[startIndex + i];

                AddButton(30, y, 4005, 4006, buttonId, GumpButtonType.Reply, 0);
                AddLabel(70, y, 0x481, entry.Name);

                y += 28;
                buttonId++;
            }

            // ====================== Bottom Navigation Row ======================
            int navY = height - 65;

            // Previous
            if (currentPage > 0)
                AddButton(30, navY, 0xFAE, 0xFAF, 1000, GumpButtonType.Reply, 0);

            // Close button (centered)
            AddButton(width / 2 - 35, navY, 0xFB1, 0xFB2, 0, GumpButtonType.Reply, 0);

            // Next
            if (currentPage < totalPages - 1)
                AddButton(width - 65, navY, 0xFA5, 0xFA6, 1001, GumpButtonType.Reply, 0);

            // Page indicator (centered below buttons)
            AddHtml(0, navY + 28, width, 20,
                $"<CENTER><BASEFONT COLOR=#FFFFFF>Page {currentPage + 1} of {totalPages}</BASEFONT></CENTER>",
                false, false);
        }

        public override void OnResponse(NetState sender, RelayInfo info)
        {
            int id = info.ButtonID;

            if (id == 0) return;                    // Close

            if (id == 1000)                         // Previous page
            {
                _from.SendGump(new ConsoleBookGump(_book, _from, _page - 1));
                return;
            }

            if (id == 1001)                         // Next page
            {
                _from.SendGump(new ConsoleBookGump(_book, _from, _page + 1));
                return;
            }

            // Console button pressed
            int consoleIndex = (_page * EntriesPerPage) + (id - 1);
            if (consoleIndex >= _book._consoles.Count) return;

            var entry = _book._consoles[consoleIndex];

            try
            {
                var console = (Item)Activator.CreateInstance(entry.Type);
                _from.SendGump(new PropertiesGump(_from, console));
                // delete this guy next server restart
                console.SetItemBool(Item.ItemBoolTable.DeleteOnRestart, true);
            }
            catch (Exception ex)
            {
                _from.SendMessage($"Failed to open {entry.Name}: {ex.Message}");
            }
        }
    }
}