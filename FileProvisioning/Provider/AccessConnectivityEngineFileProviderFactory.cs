//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Collections.Generic;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class AccessConnectivityEngineFileProviderFactory: FileProviderFactory
    {
        private const string ProviderNameAccessConnectivityEngine = "Microsoft.ACE.OLEDB.12.0";

        public AccessConnectivityEngineFileProviderFactory(
            string filePath, 
            IMonitor monitoringBehavior, 
            IReadOnlyCollection<string> attributeNames)
            : base(
                 new AccessConnectivityEngineCommaDelimitedFileAdapterFactory(filePath),
                 monitoringBehavior, 
                 attributeNames)
        {
        }

        public AccessConnectivityEngineFileProviderFactory(string filePath, IMonitor monitoringBehavior)
            :base(
                 new AccessConnectivityEngineCommaDelimitedFileAdapterFactory(filePath), 
                 monitoringBehavior)
        {
        }
    }
}
