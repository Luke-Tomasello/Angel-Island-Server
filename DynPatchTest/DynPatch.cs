/***************************************************************************
 *
 *                         DynPatchTest
 *
 *  Prototype dynamic patch, and the template for future ones.
 *
 *      [Nuke Run DynPatchTest [arguments...]]
 *
 *  OVERVIEW
 *  --------
 *  A dynamic patch is a PatchV-style implementation that executes against
 *  the live, already-loaded world without rebuilding or restarting the
 *  server. [Nuke Run <name>] loads Patches\<name>.dll and calls its one
 *  entry point:
 *
 *      public static int? Run(Mobile from, string[] args)
 *
 *  'from' is the administrator who ran the command; 'args' are the words
 *  that followed the DLL name. As with a PatchV, return the number of
 *  objects updated, or null when the patch does not apply to this
 *  shard/server.
 *
 *  PORTING A PATCHV
 *  ----------------
 *  1. Copy the implementation body.
 *  2. Replace the PatchV signature with
 *         public static int? Run(Mobile from, string[] args)
 *  3. Remove patchid and any PatchComplete/Patched bookkeeping.
 *  4. Copy any private PatchLibrary helpers the body needs into this DLL.
 *  5. Make the operation rerun-safe: [Nuke Run] keeps no patch history,
 *     so the same DLL can be run any number of times.
 *
 *  BOUNDARY
 *  --------
 *  Live loaded world only. There are no PreWorldLoad semantics, no
 *  WorldScrubber semantics, and no patch history or FeatureBits
 *  bookkeeping. Anything that needs those is a normal PatchV.
 *
 *  WARNING: Run executes synchronously on the Nuke command/server thread.
 *  Keep it bounded: do not sleep, block, do long-running I/O, or otherwise
 *  stall the server.
 *
 *  DynPatchTest itself is read-only: it reports a few live-world numbers
 *  and the arguments it received, changes nothing, and returns 0.
 *
 ***************************************************************************/

using Server;
using Server.Mobiles;
using static Server.CoreAI;
using static Server.Patcher;
using static Server.Utility;

namespace DynPatchTest
{
    public static class DynPatch
    {
        public static int? Run(Mobile from, string[] args)
        {
            if (AllShards() && !LoginServer(quiet: true))
            {
                int patched = 0;
                #region Begin Implementation

                #region Read-only live-world report
                int players = 0, online = 0;

                foreach (Mobile m in World.Mobiles.Values)
                    if (m is PlayerMobile)
                    {
                        players++;
                        if (m.NetState != null)
                            online++;
                    }

                string[] report =
                {
                    string.Format("DynPatchTest: loaded from {0}", typeof(DynPatch).Assembly.Location),
                    string.Format("DynPatchTest: invoked by {0} ({1})", from.Name, from.Serial),
                    string.Format("DynPatchTest: {0} argument(s): [{1}]", args.Length, string.Join(", ", args)),
                    string.Format("DynPatchTest: World.Items = {0}, World.Mobiles = {1}", World.Items.Count, World.Mobiles.Count),
                    string.Format("DynPatchTest: players = {0} ({1} online)", players, online),
                };

                foreach (string line in report)
                {
                    from.SendMessage(line);
                    Console.WriteLine(line);
                }
                #endregion Read-only live-world report

                #endregion End Implementation
                return patched;
            }

            return null;
        }
    }
}
