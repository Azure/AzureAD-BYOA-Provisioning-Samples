//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class FileProviderFactory
    {
        protected FileProviderFactory(
            TabularFileAdapterFactory fileAdapterFactory,
            IMonitor monitoringBehavior, 
            IReadOnlyCollection<string> attributeNames)
        {
            if (null == fileAdapterFactory)
            {
                throw new ArgumentNullException(nameof(fileAdapterFactory));
            }

            if (null == monitoringBehavior)
            {
                throw new ArgumentNullException(nameof(monitoringBehavior));
            }

            if (null == attributeNames)
            {
                throw new ArgumentNullException(nameof(attributeNames));
            }

            this.FileAdapterFactory = fileAdapterFactory;
            this.MonitoringBehavior = monitoringBehavior;
            this.AttributeNames = attributeNames;
        }

        protected FileProviderFactory(
            TabularFileAdapterFactory fileAdapterFactory,
            IMonitor monitoringBehavior)
            :this(fileAdapterFactory, monitoringBehavior, new string[0])
        {
        }

        private IReadOnlyCollection<string> AttributeNames
        {
            get;
            set;
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
            FileProviderBase result = new FileProvider(
                this.FileAdapterFactory, 
                this.MonitoringBehavior, 
                this.AttributeNames);
            return result;
        }
    }
}
