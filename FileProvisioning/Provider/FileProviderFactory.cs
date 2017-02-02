//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class FileProviderFactory
    {
        protected FileProviderFactory(
            TabularFileAdapterFactory fileAdapterFactory,
            IMonitor monitoringBehavior)
        {
            if (null == fileAdapterFactory)
            {
                throw new ArgumentNullException(nameof(fileAdapterFactory));
            }

            if (null == monitoringBehavior)
            {
                throw new ArgumentNullException(nameof(monitoringBehavior));
            }

            this.FileAdapterFactory = fileAdapterFactory;
            this.MonitoringBehavior = monitoringBehavior;
        }

        private TabularFileAdapterFactory FileAdapterFactory
        {
            get;
            set;
        }

        private IMonitor MonitoringBehavior
        {
            get;
            set;
        }

        public virtual FileProviderBase CreateProvider()
        {
            FileProviderBase result = new FileProvider(this.FileAdapterFactory, this.MonitoringBehavior);
            return result;
        }
    }
}
