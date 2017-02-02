//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class FileProviderBase : ProviderBase, IDisposable
    {
        protected virtual void Dispose(bool disposing)
        {
        }

        public abstract string FilePath { get; }
        
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}