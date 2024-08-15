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

/* Scripts\Mobiles\Base\BaseHuman.cs
 * ChangeLog
 *  7/21/08, Adam
 *      Thread is not user-suspended; it cannot be resumed.
 *          at System.Threading.Thread.ResumeInternal()
 *          at System.Threading.Thread.Resume()
 *          at Server.Mobiles.BaseHuman.OnThink()
 *          at Server.Mobiles.BaseAI.AITimer.OnTick()
 *          at Server.Timer.Slice()
 *      Solution: Replace Suspend and Resume with WaitHandles.
 *      Unlike Thread.Sleep, Thread.Suspend does not cause a thread to immediately
 *      stop execution. The common language runtime must wait until the thread has
 *      reached a safe point before it can suspend the thread.
 *      Fixes exception
 *      Do not use the Suspend and Resume methods to synchronize the activities of threads. 
 *      You have no way of knowing what code a thread is executing when you suspend it. 
 *      If you suspend a thread while it holds locks during a security permission evaluation, other threads 
 *      in the AppDomain might be blocked. If you suspend a thread while it is executing a class constructor, 
 *      other threads in the AppDomain that attempt to use that class are blocked. Deadlocks can occur very easily.
 *  7/20/08, Adam
 *      - Flush waiting text before suspending the ZLR thread.
 *          There won't USUALLY be text here unless it was a prompt without a newline (like the quit prompt)
 *      - make ParseSpeech() virtual so that the Human NPC can decide how to process text
 *	7/8/08, Adam
 *		Initial checkin
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using ZLR.VM;

namespace Server.Mobiles
{
    public class BaseHuman : BaseCreature
    {
        private Dictionary<string, string> m_ConversationTriggers = new Dictionary<string, string>();
        protected Dictionary<string, string> ConversationTriggers { get { return m_ConversationTriggers; } }
        private Dictionary<Serial, Conversation> m_Conversations = new Dictionary<Serial, Conversation>();

        public BaseHuman(AIType ai, FightMode mode, int iRangePerception, int iRangeFight, double dActiveSpeed, double dPassiveSpeed)
            : base(ai, mode, iRangePerception, iRangeFight, dActiveSpeed, dPassiveSpeed)
        {
            InitTriggers();
        }

        public BaseHuman(Serial serial)
            : base(serial)
        {
            InitTriggers();
        }

        protected virtual void InitTriggers()
        { // override this to establish keyword/conversation combinations
        }

        protected string IsTrigger(string text)
        {   //is the text a recognized trigger?
            foreach (string key in m_ConversationTriggers.Keys)
                if (text.Contains(key))
                    return key;

            return null;
        }

        public override bool HandlesOnSpeech(Mobile from)
        {
            return (from.InRange(this, RangePerception));
        }

        protected virtual bool ParseSpeech(Mobile from, string text)
        {
            // return true to continue processing speech
            return true;
        }

        public override void OnSpeech(SpeechEventArgs e)
        {
            // are we having a private conversation with this mobile?
            if (HavingAConversationWith(e.Mobile) == true)
            {   // pass this text to the z-machine for processing
                Conversation cnv = m_Conversations[e.Mobile.Serial];
                // fill the buffer that the z-machine will read from
                cnv.FillReadBuffer(e.Speech);
            }
            else
            {   // if a story is not being invoked, and we are not currently in a conversation with this mobile...
                //  do normal OnSpeech() processing
                base.OnSpeech(e);
                if (e.Handled == false)
                {   // make sure the player is talking in a reasonable fashion to us
                    if (ParseSpeech(e.Mobile, e.Speech.ToLower()) == false)
                        return;

                    // we are not currently having a private conversation with this mobile
                    // does this text contain a conversation trigger?
                    string key = IsTrigger(e.Speech.ToLower());

                    // invoke a conversation if we are not already conversing
                    if (key != null)
                    {   // create a z-machine and attach to this player (this conversation will be private)
                        CreateDialog(e.Mobile, m_ConversationTriggers[key], true);
                    }
                }
            }
        }

        public override void OnThink()
        {

            // delete old and obsolete conversations
            TerminateConversations();

            // talk to the players I'm having a conversation with
            foreach (Conversation cnv in m_Conversations.Values)
                if (!cnv.Stopped)
                    FlushWriteBuffer(cnv);

            // Allow the z-machines with pending input to OnThink()
            foreach (Conversation cnv in m_Conversations.Values)
                if (!cnv.Stopped && cnv.DataReady == true)
                {   // allow the thread to think
                    cnv.Resume();
                }

            base.OnThink();
        }

        private bool HavingAConversationWith(Mobile m)
        {
            return m_Conversations.ContainsKey(m.Serial);
        }

        protected virtual void TerminateConversations()
        {
            List<Serial> delete = new List<Serial>();
            foreach (Serial serial in m_Conversations.Keys)
            {
                Mobile m = World.FindMobile(serial);
                if (m == null)
                {                                   // um, wtf
                    delete.Add(serial);
                    continue;
                }
                if (m.Deleted == true)
                {                                   // deleted char
                    delete.Add(serial);
                    continue;
                }
                if (GetDistanceToSqrt(m) > 25.0)
                {                                   // left the area
                    delete.Add(serial);
                    continue;
                }
                if (m.NetState == null)
                {                                   // logged out
                    delete.Add(serial);
                    continue;
                }
                if (m_Conversations[serial].Stopped == true)
                {                                   // they typed 'quit' and 'y'
                    delete.Add(serial);
                    continue;
                }
            }

            // now delete all conversation tht matched criteria
            for (int ix = 0; ix < delete.Count; ix++)
            {   // Make sure the Engine shuts down by sending a 'Quit' and 'Yes' response.
                if (!m_Conversations[delete[ix]].Stopped)
                    m_Conversations[delete[ix]].Stop();
                m_Conversations.Remove(delete[ix]);                                             // now remove it from the registry
            }
        }

        private void FlushWriteBuffer(Conversation cnv)
        {
            lock (cnv.SyncRoot)
            {
                // empty (say) the buffer the z-machine writes to
                for (int ix = 0; ix < cnv.GetWriteBuffer.Count; ix++)
                {
                    if (cnv.Private == true)
                        this.SayTo(cnv.With, cnv.GetWriteBuffer[0]);
                    else
                        this.Say(cnv.GetWriteBuffer[0]);
                    cnv.GetWriteBuffer.RemoveAt(0);
                }
            }
        }

        public virtual bool CreateDialog(Mobile with, string StoryFile, bool isPrivate)
        {
            // Data/toyshop.z5
            string filename = string.Format("{0}.z5", StoryFile);
            if (!System.IO.File.Exists(Path.Combine(Core.DataDirectory, filename)))
            {
                Console.WriteLine("Error: {0} does not exist", Path.Combine(Core.DataDirectory, filename));
                return false;
            }
            // allocate a conversation, thread, and start it
            m_Conversations.Add(with.Serial, new Conversation(with, filename, isPrivate));
            return true;
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

            switch (version)
            {
                case 0:
                    {
                        break;
                    }
            }
        }

        private class Conversation
        {
            public object SyncRoot = new object();
            private WaitHandle[] m_waitHandles = new WaitHandle[] { new AutoResetEvent(false) };
            //public WaitHandle[] WitHandles { get { return m_waitHandles; } }
            private List<string> m_consoleRead = new List<string>();
            public string ReadLine { get { lock (SyncRoot) { string temp = m_consoleRead[0]; m_consoleRead.RemoveAt(0); return temp; } } }
            public char ReadKey { get { lock (SyncRoot) { string temp = m_consoleRead[0]; m_consoleRead.RemoveAt(0); if (temp.Length > 1) m_consoleRead.Insert(0, temp.Substring(1)); return temp[0]; } } }
            public bool DataReady { get { lock (SyncRoot) { return m_consoleRead.Count > 0; } } }
            public void FillReadBuffer(string line) { lock (SyncRoot) { m_consoleRead.Add(line); } }
            public void FillReadBuffer(string[] lines) { lock (SyncRoot) { for (int ix = 0; ix < lines.Length; ix++) FillReadBuffer(lines[ix]); } }
            private List<string> m_consoleWrite = new List<string>();
            public void WriteLine(string line) { lock (SyncRoot) { m_consoleWrite.Add(line); } }
            public List<string> GetWriteBuffer { get { lock (SyncRoot) { return m_consoleWrite; } } }
            private string m_storyFile;
            public string StoryFile { get { return m_storyFile; } }
            private Thread m_dialog;
            public void Resume() { (m_waitHandles[0] as AutoResetEvent).Set(); }
            public void Suspend() { if (m_stopping == false) WaitHandle.WaitAny(m_waitHandles); }
            private Mobile m_with;
            public Mobile With { get { return m_with; } }
            private bool m_stopped = false;
            public bool Stopped { get { return m_stopped; } }
            private bool m_stopping = false;
            private bool m_private = false;
            public bool Private { get { return m_private; } }

            public Conversation(Mobile with, string StoryFile, bool isPrivate)
            {
                m_storyFile = StoryFile;            // the story/dialog
                m_with = with;                      // the mobile I'm conversing with
                m_private = isPrivate;              // is this a private convesation?

                m_dialog = new Thread(this.Main);
                m_dialog.Name = "Conversation.Main";
                m_dialog.IsBackground = true;
                m_dialog.Priority = ThreadPriority.Lowest;
                m_dialog.Start();
                return;
            }

            public void Main()
            {
                try
                {   // will block on zm.Run() until the story file completes
                    Stream gameStream = new FileStream(Path.Combine(Core.DataDirectory, m_storyFile), FileMode.Open, FileAccess.Read);
                    DumbIO io = new DumbIO(this);
                    ZMachine zm = new ZMachine(gameStream, io);
                    zm.Run();
                }
                catch (Exception ex) { EventSink.InvokeLogException(new LogExceptionEventArgs(ex)); }
                m_stopped = true;
                return;
            }

            // Called from mobile.OnThink to kill a conversation thread.
            //  The m_stopping variable prevents the top-level logic from sustending this thread any more
            //  since we are in the process of shutting it down, and it needs to be running to shut down
            //  (It needs to process the "quit", "y")
            public void Stop()
            {
                this.FillReadBuffer(new string[] { "quit", "y", }); // tell the story processor to exit: Quit/Yes
                this.m_stopping = true;                             // do not allow suspend while we try to shut down
                this.Resume();                                      // make sure it's running before we disconnect
            }
        }

        class DumbIO : IZMachineIO
        {
            Conversation m_conversation;
            string m_temp = "";
            short m_window = 0;

            public DumbIO(Conversation conversation)
            {
                m_conversation = conversation;
            }

            public string ReadLine(string initial, int time, TimedInputCallback callback, byte[] terminatingKeys, out byte terminator)
            {   // flush any waiting input and pause thie thread while it is looking for input
                if (m_temp != null && m_temp.Length > 0) { m_conversation.WriteLine(m_temp); m_temp = ""; }
                this.m_conversation.Suspend();

                // gather input
                terminator = 13;
                while (true)
                {
                    lock (m_conversation.SyncRoot)
                    {
                        if (m_conversation.DataReady == false)
                        {   // should never happen because this thread is only resumed when this mobile.OnThink()
                            //  sees that there is DataReady
                            Thread.Sleep(250);
                            continue;
                        }

                        return m_conversation.ReadLine;
                    }
                }
            }

            public short ReadKey(int time, TimedInputCallback callback, CharTranslator translator)
            {   // flush any waiting input and pause thie thread while it is looking for input
                if (m_temp != null && m_temp.Length > 0) { m_conversation.WriteLine(m_temp); m_temp = ""; }
                this.m_conversation.Suspend();

                // gather input
                short ch = 0;
                ConsoleKeyInfo info;
                do
                {
                    lock (m_conversation.SyncRoot)
                    {
                        if (m_conversation.DataReady == false)
                        {   // should never happen because this thread is only resumed when this mobile.OnThink()
                            //  sees that there is DataReady
                            Thread.Sleep(250);
                            continue;
                        }

                        info = new ConsoleKeyInfo(m_conversation.ReadKey, ConsoleKey.Y, false, false, false);
                    }

                    ch = translator(info.KeyChar);
                } while (ch == 0);
                return ch;
            }

            public void PutCommand(string command)
            {
                // nada
            }

            public void PutChar(char ch)
            {
                // ignore the 'moves, score' output
                if (m_window == 1)
                    return;

                // eat the '>' prompt - I believe this is the correct context
                if (ch == '>' && m_temp.Length == 0)
                    return;

                //Console.Write(ch);
                lock (m_conversation.SyncRoot)
                {
                    if (ch == '\n' || ch == '\r')
                    {
                        m_conversation.WriteLine(m_temp);
                        m_temp = "";
                    }
                    else
                        m_temp += ch;
                }
            }

            public void PutString(string xstr)
            {
                // ignore the 'moves, score' output
                if (m_window == 1)
                    return;

                // rather than modify this string (we don't know what impact that wil have on the caller)
                //  we'll work on a copy
                string str;

                // eat the '>' prompt - I believe this is the correct context
                if (xstr == "\n>") str = "\n";
                else str = xstr;

                //Console.Write(str);
                lock (m_conversation.SyncRoot)
                {
                    foreach (char ch in str)
                    {
                        if (ch == '\n' || ch == '\r')
                        {
                            m_conversation.WriteLine(m_temp);
                            m_temp = "";
                        }
                        else
                            m_temp += ch;
                    }
                }
            }

            public void PutTextRectangle(string[] lines)
            {
                // ignore the 'moves, score' output
                if (m_window == 1)
                    return;

                //foreach (string str in lines)
                //Console.WriteLine(str);

                lock (m_conversation.SyncRoot)
                {
                    foreach (string str in lines)
                        PutString(str);
                }
            }

            public bool Buffering
            {
                get { return false; }
                set { /* nada */ }
            }

            public bool Transcripting
            {
                get { return false; }
                set { /* nada */ }
            }

            public void PutTranscriptChar(char ch)
            {
                // not implemented
            }

            public void PutTranscriptString(string str)
            {
                // not implemented
            }

            public System.IO.Stream OpenSaveFile(int size)
            {
                PlayerMobile pm = (m_conversation.With as PlayerMobile);
                if (pm == null) return null;
                pm.ZCodeMiniGameData = new byte[size];
                pm.ZCodeMiniGameID = m_conversation.StoryFile.GetHashCode();
                return new MemoryStream(pm.ZCodeMiniGameData);
            }

            public System.IO.Stream OpenRestoreFile()
            {
                PlayerMobile pm = (m_conversation.With as PlayerMobile);
                if (pm == null) return null;
                if (pm.ZCodeMiniGameID == 0)
                {
                    m_conversation.With.SendMessage("You do not have a saved game file.");
                    return null;
                }
                if (pm.ZCodeMiniGameID != m_conversation.StoryFile.GetHashCode())
                {
                    m_conversation.With.SendMessage("You have a saved game file, but it is not for this game.");
                    return null;
                }
                return new MemoryStream(pm.ZCodeMiniGameData);
            }

            public System.IO.Stream OpenAuxiliaryFile(string name, int size, bool writing)
            {
                // not implemented
                return null;
            }

            public System.IO.Stream OpenCommandFile(bool writing)
            {
                // not implemented
                return null;
            }

            public void SetTextStyle(TextStyle style)
            {
                // nada
            }

            public void SplitWindow(short lines)
            {
                // nada
            }

            public void SelectWindow(short num)
            {
                m_window = num;
            }

            public void EraseWindow(short num)
            {
                // nada
            }

            public void EraseLine()
            {
                // nada
            }

            public void MoveCursor(short x, short y)
            {
                // nada
            }

            public void GetCursorPos(out short x, out short y)
            {
                x = 1;
                y = 1;
            }

            public void SetColors(short fg, short bg)
            {
                // nada
            }

            public short SetFont(short num)
            {
                return 0;
            }

            public void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
            {
                // nada
            }

            public void PlayBeep(bool highPitch)
            {
                // nada
            }

            public bool ForceFixedPitch
            {
                get { return false; }
                set { /* nada */ }
            }

            public bool BoldAvailable
            {
                get { return false; }
            }

            public bool ItalicAvailable
            {
                get { return false; }
            }

            public bool FixedPitchAvailable
            {
                get { return false; }
            }

            public bool GraphicsFontAvailable
            {
                get { return false; }
            }

            public bool TimedInputAvailable
            {
                get { return false; }
            }

            public bool SoundSamplesAvailable
            {
                get { return false; }
            }

            public byte WidthChars
            {
                get { return 80; }
            }

            public short WidthUnits
            {
                get { return 80; }
            }

            public byte HeightChars
            {
                get { return 25; }
            }

            public short HeightUnits
            {
                get { return 25; }
            }

            public byte FontHeight
            {
                get { return 1; }
            }

            public byte FontWidth
            {
                get { return 1; }
            }

            public event EventHandler SizeChanged
            {
                add { /* nada */ }
                remove { /* nada */ }
            }

            public bool ColorsAvailable
            {
                get { return false; }
            }

            public byte DefaultForeground
            {
                get { return 9; }
            }

            public byte DefaultBackground
            {
                get { return 2; }
            }

            public UnicodeCaps CheckUnicode(char ch)
            {
                return UnicodeCaps.CanInput | UnicodeCaps.CanPrint;
            }
        }

    }
}