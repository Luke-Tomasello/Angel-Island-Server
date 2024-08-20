using System;
using JetBrains.Annotations;

namespace ZLR.VM.IOFilters
{
    public sealed class TeeFilter : FilterBase
    {
        private readonly IZMachineIO side;

        public TeeFilter([NotNull] IAsyncZMachineIO next, [NotNull] IZMachineIO side)
            : base(next)
        {
            this.side = side ?? throw new ArgumentNullException(nameof(side));
        }

        public bool PassSound { get; set; }

        public override bool DrawCustomStatusLine(string location, short hoursOrScore, short minsOrTurns, bool useTime)
        {
            side.DrawCustomStatusLine(location, hoursOrScore, minsOrTurns, useTime);
            return base.DrawCustomStatusLine(location, hoursOrScore, minsOrTurns, useTime);
        }

        public override void EraseLine()
        {
            side.EraseLine();
            base.EraseLine();
        }

        public override void EraseWindow(short num)
        {
            side.EraseWindow(num);
            base.EraseWindow(num);
        }

        public override bool ForceFixedPitch
        {
            set
            {
                side.ForceFixedPitch = value;
                base.ForceFixedPitch = value;
            }
        }

        public override void MoveCursor(short x, short y)
        {
            side.MoveCursor(x, y);
            base.MoveCursor(x, y);
        }

        public override void PlayBeep(bool highPitch)
        {
            if (PassSound)
                side.PlayBeep(highPitch);

            base.PlayBeep(highPitch);
        }

        public override void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
        {
            if (PassSound)
                side.PlaySoundSample(number, action, volume, repeats, callback);

            base.PlaySoundSample(number, action, volume, repeats, callback);
        }

        public override void PutChar(char ch)
        {
            side.PutChar(ch);
            base.PutChar(ch);
        }

        public override void PutString(string str)
        {
            side.PutString(str);
            base.PutString(str);
        }

        public override void PutTextRectangle(string[] lines)
        {
            side.PutTextRectangle(lines);
            base.PutTextRectangle(lines);
        }

        public override bool ScrollFromBottom
        {
            set
            {
                side.ScrollFromBottom = value;
                base.ScrollFromBottom = value;
            }
        }

        public override void SelectWindow(short num)
        {
            side.SelectWindow(num);
            base.SelectWindow(num);
        }

        public override void SetColors(short fg, short bg)
        {
            side.SetColors(fg, bg);
            base.SetColors(fg, bg);
        }

        public override short SetFont(short num)
        {
            side.SetFont(num);
            return base.SetFont(num);
        }

        public override void SetTextStyle(TextStyle style)
        {
            side.SetTextStyle(style);
            base.SetTextStyle(style);
        }

        public override void SplitWindow(short lines)
        {
            side.SplitWindow(lines);
            base.SplitWindow(lines);
        }
    }
}
