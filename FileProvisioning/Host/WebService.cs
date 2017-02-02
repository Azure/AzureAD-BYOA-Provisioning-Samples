//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class WebService : Service
    {
        private IMonitor monitor;
        private IProvider provider;

        public WebService(IMonitor monitoringBehavior, IProvider providerBehavior)
        {
            if (null == monitoringBehavior)
            {
                throw new ArgumentNullException(nameof(monitoringBehavior));
            }

            if (null == providerBehavior)
            {
                throw new ArgumentNullException(nameof(providerBehavior));
            }

            this.monitor = monitoringBehavior;
            this.provider = providerBehavior;
        }

        public override IMonitor MonitoringBehavior
        {
            get
            {
                return this.monitor;
            }

            set
            {
                this.monitor = value;
            }
        }

        public override IProvider ProviderBehavior
        {
            get
            {
                return this.provider;
            }

            set
            {
                this.provider = value;
            }
        }
    }
}
