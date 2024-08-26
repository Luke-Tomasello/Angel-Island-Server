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

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ZLR.VM.IOFilters
{
    public sealed class InternalSaveFilter : FilterBase
    {
        private MemoryStream? saveData;

        public InternalSaveFilter([NotNull] IAsyncZMachineIO next)
            : base(next)
        {
        }

        [ItemNotNull]
        public override Task<Stream?> OpenSaveFileAsync(int size, CancellationToken cancellationToken = default)
        {
            saveData = new MemoryStream(size);
            return Task.FromResult<Stream?>(saveData);
        }

        [ItemNotNull]
        public override Task<Stream?> OpenRestoreFileAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Stream?>(saveData != null ? new MemoryStream(saveData.ToArray(), false) : null);
        }
    }
}