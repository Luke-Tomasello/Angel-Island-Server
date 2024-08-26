/***************************************************************************
 *
 *   ZLR                     : May 1, 2007
 *   implementation          : (C) 2007-2023 Tara McGrew
 *   repository url          : https://foss.heptapod.net/zilf/zlr
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

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZLR.VM.IOFilters
{
    public abstract class FilterBase : IAsyncZMachineIO
    {
        protected readonly IAsyncZMachineIO next;

        protected FilterBase([NotNull] IAsyncZMachineIO next)
        {
            this.next = next ?? throw new ArgumentNullException(nameof(next));
        }

        #region IAsyncZMachineIO Members

        public ReadLineResult ReadLine(string initial, int time, TimedInputCallback callback,
            byte[] terminatingKeys, bool allowDebuggerBreak) => throw new NotSupportedException();

        public short ReadKey(int time, TimedInputCallback callback, CharTranslator translator) =>
            throw new NotSupportedException();

        public virtual void PutCommand(string command) => next.PutCommand(command);

        public virtual void PutChar(char ch) => next.PutChar(ch);

        public virtual void PutString(string str) => next.PutString(str);

        public virtual void PutTextRectangle(string[] lines) => next.PutTextRectangle(lines);

        public virtual bool Buffering
        {
            get => next.Buffering;
            set => next.Buffering = value;
        }

        public virtual bool Transcripting
        {
            get => next.Transcripting;
            set => next.Transcripting = value;
        }

        public virtual void PutTranscriptChar(char ch) => next.PutTranscriptChar(ch);

        public virtual void PutTranscriptString(string str) => next.PutTranscriptString(str);

        public Stream OpenSaveFile(int size) => throw new NotSupportedException();

        public Stream OpenRestoreFile() => throw new NotSupportedException();

        public Stream OpenAuxiliaryFile(string name, int size, bool writing) =>
            throw new NotSupportedException();

        public Stream OpenCommandFile(bool writing) => throw new NotSupportedException();

        public virtual void SetTextStyle(TextStyle style) => next.SetTextStyle(style);

        public virtual void SplitWindow(short lines) => next.SplitWindow(lines);

        public virtual void SelectWindow(short num) => next.SelectWindow(num);

        public virtual void EraseWindow(short num) => next.EraseWindow(num);

        public virtual void EraseLine() => next.EraseLine();

        public virtual void MoveCursor(short x, short y) => next.MoveCursor(x, y);

        public virtual void GetCursorPos(out short x, out short y) => next.GetCursorPos(out x, out y);

        public virtual void SetColors(short fg, short bg) => next.SetColors(fg, bg);

        public virtual short SetFont(short num) => next.SetFont(num);

        public virtual bool DrawCustomStatusLine(string location, short hoursOrScore, short minsOrTurns,
            bool useTime) =>
            next.DrawCustomStatusLine(location, hoursOrScore, minsOrTurns, useTime);

        public virtual void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats,
            SoundFinishedCallback callback) =>
            next.PlaySoundSample(number, action, volume, repeats, callback);

        public virtual void PlayBeep(bool highPitch) => next.PlayBeep(highPitch);

        public virtual bool ForceFixedPitch
        {
            get => next.ForceFixedPitch;
            set => next.ForceFixedPitch = value;
        }

        public virtual bool VariablePitchAvailable => next.VariablePitchAvailable;

        public virtual bool ScrollFromBottom
        {
            get => next.ScrollFromBottom;
            set => next.ScrollFromBottom = value;
        }

        public virtual bool BoldAvailable => next.BoldAvailable;

        public virtual bool ItalicAvailable => next.ItalicAvailable;

        public virtual bool FixedPitchAvailable => next.FixedPitchAvailable;

        public virtual bool GraphicsFontAvailable => next.GraphicsFontAvailable;

        public bool TimedInputAvailable => throw new NotSupportedException();

        public virtual bool SoundSamplesAvailable => next.SoundSamplesAvailable;

        public virtual byte WidthChars => next.WidthChars;

        public virtual short WidthUnits => next.WidthUnits;

        public virtual byte HeightChars => next.HeightChars;

        public virtual short HeightUnits => next.HeightUnits;

        public virtual byte FontHeight => next.FontHeight;

        public virtual byte FontWidth => next.FontWidth;

        public virtual event EventHandler SizeChanged
        {
            add => next.SizeChanged += value;
            remove => next.SizeChanged -= value;
        }

        public virtual bool ColorsAvailable => next.ColorsAvailable;

        public virtual byte DefaultForeground => next.DefaultForeground;

        public virtual byte DefaultBackground => next.DefaultBackground;

        public virtual UnicodeCaps CheckUnicode(char ch) => next.CheckUnicode(ch);

        public virtual Task<ReadLineResult> ReadLineAsync(string initial, byte[] terminatingKeys,
            bool allowDebuggerBreak, CancellationToken cancellationToken = default) =>
            next.ReadLineAsync(initial, terminatingKeys, allowDebuggerBreak, cancellationToken);

        public virtual Task<short> ReadKeyAsync(CharTranslator translator,
            CancellationToken cancellationToken = default) =>
            next.ReadKeyAsync(translator, cancellationToken);

        public virtual Task<Stream?> OpenSaveFileAsync(int size, CancellationToken cancellationToken = default) =>
            next.OpenSaveFileAsync(size, cancellationToken);

        public virtual Task<Stream?> OpenRestoreFileAsync(CancellationToken cancellationToken = default) =>
            next.OpenRestoreFileAsync(cancellationToken);

        public virtual Task<Stream?> OpenAuxiliaryFileAsync(string name, int size, bool writing,
            CancellationToken cancellationToken = default) =>
            next.OpenAuxiliaryFileAsync(name, size, writing, cancellationToken);

        public virtual Task<Stream?> OpenCommandFileAsync(bool writing, CancellationToken cancellationToken = default) =>
            next.OpenCommandFileAsync(writing, cancellationToken);

        #endregion
    }
}