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

/* Scripts/Skills/Inscribe.cs
 * ChangeLog
 *	9/29/05, Adam
 *		add the "a moonstone for " prefix to the name of a moonstone
 *  09/14/05 TK
 *		Required Powder of Translocation for inscribing stones.
 *		Added a pass/fail formula instead of a 100/100 check
 *		Copied rune's name to moonstone
 *	09/13/05 Taran Kain
 *		Allowed copying of runes to moonstones.
 *	03/16/05, erlein
 *		Altered copy function so it also copies LastEdited serial.
 *  08/02/05 Taran Kain
 *		Changed Inscribe.Copy to use new BaseBook.AddPage
 *		Removed Adam's emergency tourniquet fix for crashes
 *	2/8/05, Adam
 *		force to always fail until we get basebook fixed (line 111)
 *		To undo my HacKoRz, remove the "|| true == true"
 *  6/5/04, Pix
 *		Merged in 1.0RC0 code.
 */

using Server.Items;
using Server.Targeting;
using System;
using System.Collections;

namespace Server.SkillHandlers
{
    public class Inscribe
    {
        public static void Initialize()
        {
            SkillInfo.Table[(int)SkillName.Inscribe].Callback = new SkillUseCallback(OnUse);
        }

        public static TimeSpan OnUse(Mobile m)
        {
            Target target = new InternalTargetSrc();
            m.Target = target;
            m.SendMessage("Target the item you wish to copy.");
            target.BeginTimeout(m, TimeSpan.FromMinutes(1.0));

            return TimeSpan.FromSeconds(1.0);
        }

        private static Hashtable m_UseTable = new Hashtable();

        private static void SetUser(object item, Mobile mob)
        {
            m_UseTable[item] = mob;
        }

        private static void CancelUser(Item item)
        {
            m_UseTable.Remove(item);
        }

        public static Mobile GetUser(Item item)
        {
            return (Mobile)m_UseTable[item];
        }

        public static bool IsEmpty(BaseBook book)
        {
            foreach (BookPageInfo page in book.Pages)
            {
                foreach (string line in page.Lines)
                {
                    if (line.Trim() != "")
                        return false;
                }
            }
            return true;
        }

        public static void CopyBook(BaseBook bookSrc, BaseBook bookDst)
        {
            if (bookSrc == null || bookDst == null)
                return;

            bookDst.Title = bookSrc.Title;
            bookDst.Author = bookSrc.Author;
            bookDst.LastEdited = bookSrc.LastEdited; //erl: LastEdited Serial

            bookDst.ClearPages();
            for (int i = 0; i < bookSrc.Pages.Length; i++)
                bookDst.AddPage(bookSrc.Pages[i]);
        }

        private class InternalTargetSrc : Target
        {
            public InternalTargetSrc()
                : base(3, false, TargetFlags.None)
            {
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (targeted is RecallRune)
                {
                    RecallRune rune = (RecallRune)targeted;

                    if (!rune.Marked)
                        from.SendMessage("You cannot copy an unmarked recall rune.");
                    else if (Inscribe.GetUser(rune) != null)
                        from.SendLocalizedMessage(501621); // Someone else is inscribing that item.
                    else
                    {
                        Target target = new InternalTargetDst(rune);
                        from.Target = target;
                        from.SendMessage("Select a moonstone to copy that to."); // reword
                        target.BeginTimeout(from, TimeSpan.FromMinutes(1.0));
                        Inscribe.SetUser(rune, from);
                    }
                }
                else if (targeted is BaseBook)
                {
                    BaseBook book = (BaseBook)targeted;

                    if (Inscribe.IsEmpty(book))
                        from.SendLocalizedMessage(501611); // Can't copy an empty book.
                    else if (Inscribe.GetUser(book) != null)
                        from.SendLocalizedMessage(501621); // Someone else is inscribing that item.
                    else if (!book.Copyable)
                        from.SendAsciiMessage("That book is not copyable.");
                    else
                    {
                        Target target = new InternalTargetDst(book);
                        from.Target = target;
                        from.SendLocalizedMessage(501612); // Select a book to copy this to.
                        target.BeginTimeout(from, TimeSpan.FromMinutes(1.0));
                        Inscribe.SetUser(book, from);
                    }
                }
                else
                    from.SendMessage("You cannot copy that.");
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Timeout)
                    from.SendLocalizedMessage(501619); // You have waited too long to make your inscribe selection, your inscription attempt has timed out.
            }
        }

        private class InternalTargetDst : Target
        {
            private Item m_ItemSrc;

            public InternalTargetDst(Item itemSrc)
                : base(3, false, TargetFlags.None)
            {
                m_ItemSrc = itemSrc;
            }

            protected override void OnTarget(Mobile from, object targeted)
            {
                if (m_ItemSrc.Deleted)
                    return;

                if (targeted is Moonstone)
                {
                    Moonstone stone = (Moonstone)targeted;

                    if (!(m_ItemSrc is RecallRune))
                        from.SendMessage("You cannot copy that to a moonstone.");
                    else if (!((RecallRune)m_ItemSrc).Marked)
                        from.SendMessage("You cannot copy an unmarked recall rune.");
                    else if (Inscribe.GetUser(stone) != null)
                        from.SendLocalizedMessage(501621);  // Someone else is inscribing that item.
                    else
                    {
                        if (from.Mana < 20)
                        {
                            from.SendMessage("You have insufficient mana to copy the recall rune.");
                            return;
                        }

                        Container pack = from.Backpack;
                        if (pack == null)
                        {
                            from.SendMessage("You have no backpack! Contact a GM immediately.");
                            return;
                        }
                        if (pack.ConsumeUpTo(typeof(PowderOfTranslocation), 1) != 1)
                        {
                            from.SendMessage("You must have some powder of translocation to mark the moonstone.");
                            return;
                        }

                        from.Mana -= 20;

                        if (Utility.RandomDouble() * 100 >= (100.0 - ((from.Skills[SkillName.Inscribe].Value + from.Skills[SkillName.Magery].Value) / 2.0)) * 2.0)
                        {
                            RecallRune rune = (RecallRune)m_ItemSrc;
                            stone.Marked = true;
                            stone.Description = "a moonstone for " + rune.Description;
                            stone.Destination = rune.Target;

                            from.SendMessage("You copy the rune.");

                            from.PlaySound(0x1FA);
                            Effects.SendLocationEffect(rune, rune.Map, 14201, 16);
                            Effects.SendLocationEffect(stone, stone.Map, 14201, 16);
                        }
                        else
                            from.SendMessage("You fail to copy the recall rune.");
                    }
                }
                else if (targeted is BaseBook)
                {
                    BaseBook bookDst = (BaseBook)targeted;

                    if (!(m_ItemSrc is BaseBook))
                        from.SendMessage("You cannot copy that to a book.");
                    else if (Inscribe.IsEmpty(m_ItemSrc as BaseBook))
                        from.SendLocalizedMessage(501611); // Can't copy an empty book.
                    else if (bookDst == m_ItemSrc)
                        from.SendLocalizedMessage(501616); // Cannot copy a book onto itself.
                    else if (!bookDst.Writable)
                        from.SendLocalizedMessage(501614); // Cannot write into that book.
                    else if (Inscribe.GetUser(bookDst) != null)
                        from.SendLocalizedMessage(501621); // Someone else is inscribing that item.
                    else
                    {
                        if (from.CheckTargetSkill(SkillName.Inscribe, bookDst, 0, 50, new object[2] /*contextObj*/))
                        {
                            Inscribe.CopyBook(m_ItemSrc as BaseBook, bookDst);

                            from.SendLocalizedMessage(501618); // You make a copy of the book.
                            from.PlaySound(0x249);
                        }
                        else
                        {
                            from.SendLocalizedMessage(501617); // You fail to make a copy of the book.
                        }
                    }
                }
                else
                    from.SendMessage("You are unable to copy those two items.");
            }

            protected override void OnTargetCancel(Mobile from, TargetCancelType cancelType)
            {
                if (cancelType == TargetCancelType.Timeout)
                    from.SendLocalizedMessage(501619); // You have waited too long to make your inscribe selection, your inscription attempt has timed out.
            }

            protected override void OnTargetFinish(Mobile from)
            {
                Inscribe.CancelUser(m_ItemSrc);
            }
        }
    }
}