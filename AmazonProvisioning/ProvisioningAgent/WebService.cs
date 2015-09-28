//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class WebService : Service
    {
        private const string ArgumentNameMonitoringBehavior = "monitoringBehavior";
        private const string ArgumentNameProviderBehavior = "providerBehavior";

        private IMonitor monitor;
        private IProvider provider;

        public WebService(IMonitor monitoringBehavior, IProvider providerBehavior)
        {
            if (null == monitoringBehavior)
            {
                throw new ArgumentNullException(WebService.ArgumentNameMonitoringBehavior);
            }

            if (null == providerBehavior)
            {
                throw new ArgumentNullException(WebService.ArgumentNameProviderBehavior);
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
