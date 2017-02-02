//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;

    public class AccessConnectivityEngineCommaDelimitedFileAdapterFactory: TabularFileAdapterFactory
    {
        private const string ProviderNameValue = "Microsoft.ACE.OLEDB.12.0";

        public AccessConnectivityEngineCommaDelimitedFileAdapterFactory(string filePath)
            :base(filePath)
        {
        }

        public override string ProviderName
        {
            get
            {
                return AccessConnectivityEngineCommaDelimitedFileAdapterFactory.ProviderNameValue;
            }
        }

        public override ITabularFileAdapter CreateFileAdapter(IReadOnlyCollection<string> columnNames)
        {
            ITabularFileAdapter result =
                new CommaDelimitedFileAdapter(
                    this.FilePath,
                    this.ProviderName,
                    columnNames);
            return result;
        }
}
}
