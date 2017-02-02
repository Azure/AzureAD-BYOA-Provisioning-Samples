//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class JetFileProviderFactory : FileProviderFactory
    {
        private const string ProviderNameJet = "Microsoft.Jet.OLEDB.4.0";

        public JetFileProviderFactory(string filePath, IMonitor monitoringBehavior)
            : base(new JetCommaDelimitedFileAdapterFactory(filePath), monitoringBehavior)
        {
        }
    }
}