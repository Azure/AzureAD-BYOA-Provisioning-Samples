//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class AccessConnectivityEngineFileProviderFactory: FileProviderFactory
    {
        private const string ProviderNameAccessConnectivityEngine = "Microsoft.ACE.OLEDB.12.0";

        public AccessConnectivityEngineFileProviderFactory(string filePath, IMonitor monitoringBehavior)
            :base(
                 new AccessConnectivityEngineCommaDelimitedFileAdapterFactory(filePath), 
                 monitoringBehavior)
        {
        }
    }
}
