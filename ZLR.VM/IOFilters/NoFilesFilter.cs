using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZLR.VM.IOFilters
{
    public sealed class NoFilesFilter : FilterBase
    {
        public NoFilesFilter([NotNull] IAsyncZMachineIO next)
            : base(next)
        {
        }

        public override Task<Stream?> OpenAuxiliaryFileAsync(string name, int size, bool writing, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public override Task<Stream?> OpenCommandFileAsync(bool writing, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public override Task<Stream?> OpenRestoreFileAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }

        public override Task<Stream?> OpenSaveFileAsync(int size, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(null);
        }
    }
}
