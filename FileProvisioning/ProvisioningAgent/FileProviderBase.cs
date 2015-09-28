//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class FileProviderBase : ProviderBase, IDisposable
    {
        private const string ArgumentNameFilePath = "filePath";
        
        protected FileProviderBase(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(FileProviderBase.ArgumentNameFilePath);
            }

            this.FilePath = filePath;
        }

        public string FilePath
        {
            get;
            set;
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}