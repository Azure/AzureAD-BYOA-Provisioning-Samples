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
            public void Inform(string information, bool verbose, string correlationIdentifier)
            {
            }

            public void Inform(string information, bool verbose)
            {
            }

            public void Inform(string information, string correlationIdentifier)
            {
            }

            public void Inform(string information)
            {
            }

            public void Report(Exception exception, bool critical, string correlationIdentifier)
            {
            }

            public void Report(Exception exception, bool critical)
            {
            }

            public void Report(Exception exception, string correlationIdentifier)
            {
            }

            public void Report(Exception exception)
            {
            }

            public void Warn(Exception exception, string correlationIdentifier)
            {
            }

            public void Warn(Exception exception)
            {
            }

            public void Warn(string warning, string correlationIdentifier)
            {
            }

            public void Warn(string warning)
            {
            }
        }
    }
}