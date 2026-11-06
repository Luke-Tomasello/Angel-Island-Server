using Server.Diagnostics;
using System;
using TextCopy;

namespace Server.Commands
{
    // ===================================================================
    // [YANK] Command
    // ===================================================================
    public class YankCommand : BaseCommand
    {
        public YankCommand()
        {
            AccessLevel = AccessLevel.Owner;
            Supports = CommandSupport.Simple;
            Commands = new string[] { "Yank" };
            ObjectTypes = ObjectTypes.Both;
            Usage = "Yank <target>";
            Description = "Yanks properties and equipped layers of a mobile or item to the clipboard as XML.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj == null)
            {
                LogFailure("Target not found.");
                return;
            }

            try
            {
                string xml = Utility.YankToXml(obj);
                new Clipboard().SetText(xml);

                string typeName = obj is Mobile m ? m.GetType().Name
                                : obj is Item i ? i.GetType().Name
                                : "object";

                AddResponse($"Yanked {typeName} to clipboard ({xml.Length:N0} characters).");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                LogFailure("Failed to yank target to clipboard.");
            }
        }
    }

    // ===================================================================
    // [PUT] Command
    // ===================================================================
    public class PutCommand : BaseCommand
    {
        public PutCommand()
        {
            AccessLevel = AccessLevel.Owner;
            Supports = CommandSupport.Simple;
            Commands = new string[] { "Put" };
            ObjectTypes = ObjectTypes.Both;
            Usage = "Put <target>";
            Description = "Applies yanked XML data from the clipboard to the targeted mobile or item.";
        }

        public override void Execute(CommandEventArgs e, object obj)
        {
            if (obj == null)
            {
                LogFailure("Target not found.");
                return;
            }

            try
            {
                string? xml = new Clipboard().GetText();

                if (string.IsNullOrWhiteSpace(xml))
                {
                    LogFailure("Clipboard is empty or does not contain valid XML.");
                    return;
                }

                Utility.ApplyYankXml(obj, xml);
                AddResponse($"Successfully applied yanked data to {obj.GetType().Name}.");
            }
            catch (Exception ex)
            {
                LogHelper.LogException(ex);
                LogFailure("Failed to apply yanked data. Check logs for details.");
            }
        }
    }
}