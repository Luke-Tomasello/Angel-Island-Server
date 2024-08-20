using JetBrains.Annotations;

namespace ZLR.VM.IOFilters
{
    public sealed class NoSoundFilter : FilterBase
    {
        public NoSoundFilter([NotNull] IAsyncZMachineIO next)
            : base(next)
        {
        }

        public override void PlayBeep(bool highPitch)
        {
            // nada
        }

        public override void PlaySoundSample(ushort number, SoundAction action, byte volume, byte repeats, SoundFinishedCallback callback)
        {
            // nada
        }
    }
}
