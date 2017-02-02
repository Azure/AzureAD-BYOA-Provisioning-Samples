//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;

    public abstract class TabularFileAdapterFactory
    {
        protected TabularFileAdapterFactory(
            string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            this.FilePath = filePath;
        }

        public string FilePath
        {
            get;
            private set;
        }

        public abstract string ProviderName
        {
            get;
        }

        public abstract ITabularFileAdapter CreateFileAdapter(IReadOnlyCollection<string> columnNames);
    }
}
