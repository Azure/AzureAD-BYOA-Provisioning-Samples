//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;

    public class JetCommaDelimitedFileAdapterFactory: TabularFileAdapterFactory
    {
        private const string ProviderNameValue = "Microsoft.Jet.OLEDB.4.0";

        public JetCommaDelimitedFileAdapterFactory(string filePath)
            :base(filePath)
        {
        }

        public override string ProviderName
        {
            get
            {
                return JetCommaDelimitedFileAdapterFactory.ProviderNameValue;
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
