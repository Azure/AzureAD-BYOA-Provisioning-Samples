//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

using Owin;

namespace Microsoft.SystemForCrossDomainIdentityManagement
{
    using System;

    public class Startup
    {
        IWebApplicationStarter starter;

        public Startup()
        {
            IMonitor monitor = new DefaultMonitor();
            IProvider provider = new SampleProvider();
            this.starter = new WebApplicationStarter(provider, monitor);
        }

        public void Configuration(IAppBuilder builder)
        {
            this.starter.ConfigureApplication(builder);
        }

        private class DefaultMonitor : IMonitor
        {
            public void Inform(IInformationNotification notification)
            {
            }

            public void Report(IExceptionNotification notification)
            {
            }

            public void Warn(Notification<string> notification)
            {
            }

            public void Warn(Notification<Exception> notification)
            {
            }
        }
    }
}