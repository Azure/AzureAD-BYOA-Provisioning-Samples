//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Threading;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    internal class ProvisioningAgentMonitor : IMonitor
    {
        private const string ArgumentNameCorrelationIdentifier = "correlationIdentifier";
        private const string ArgumentNameException = "exception";
        private const string ArgumentNameInformation = "information";
        private const string ArgumentNameWarning = "warning";

        private static readonly string CorrelationIdentifierDefault = Guid.Empty.ToString();

        private static IMonitor instance;

        private IMonitor consoleMonitor;
        
        private ProvisioningAgentMonitor()
        {
            this.consoleMonitor = new ConsoleMonitor();
        }

        public static IMonitor Instance
        {
            get
            {
                IMonitor result =
                    LazyInitializer.EnsureInitialized<IMonitor>(
                        ref ProvisioningAgentMonitor.instance,
                        () =>
                            new ProvisioningAgentMonitor());
                return result;
            }
        }

        public void Inform(string information, bool verbose, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(information))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameInformation);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameCorrelationIdentifier);
            }

            this.consoleMonitor.Inform(information, verbose, correlationIdentifier);
            ProvisioningAgentMonitor.Prompt();
        }

        public void Inform(string information, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(information))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameInformation);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameCorrelationIdentifier);
            }

            this.Inform(information, false, correlationIdentifier);
        }

        public void Inform(string information, bool verbose)
        {
            if (string.IsNullOrWhiteSpace(information))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameInformation);
            }

            this.Inform(information, verbose, ProvisioningAgentMonitor.CorrelationIdentifierDefault);
        }

        public void Inform(string information)
        {
            if (string.IsNullOrWhiteSpace(information))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameInformation);
            }

            this.Inform(information, false);
        }

        private static void Prompt()
        {
            Console.WriteLine();
            Console.WriteLine(ProvisioningAgentResources.PromptTerminate);
        }

        public void Report(Exception exception, bool critical, string correlationIdentifier)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            this.consoleMonitor.Report(exception, critical, correlationIdentifier);
            ProvisioningAgentMonitor.Prompt();
        }

        public void Report(Exception exception, string correlationIdentifier)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameCorrelationIdentifier);
            }

            this.Report(exception, false, correlationIdentifier);
        }

        public void Report(Exception exception, bool critical)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            this.Report(exception, critical, ProvisioningAgentMonitor.CorrelationIdentifierDefault);
        }

        public void Report(Exception exception)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            this.Report(exception, false);
        }

        public void Warn(string warning, string correlationIdentifier)
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameWarning);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameCorrelationIdentifier);
            }

            this.consoleMonitor.Warn(warning, correlationIdentifier);
            ProvisioningAgentMonitor.Prompt();
        }

        public void Warn(string warning)
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameWarning);
            }

            this.Warn(warning, ProvisioningAgentMonitor.CorrelationIdentifierDefault);
        }

        public void Warn(Exception exception, string correlationIdentifier)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            if (string.IsNullOrWhiteSpace(correlationIdentifier))
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameCorrelationIdentifier);
            }

            this.consoleMonitor.Warn(exception, correlationIdentifier);
            ProvisioningAgentMonitor.Prompt();
        }

        public void Warn(Exception exception)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(ProvisioningAgentMonitor.ArgumentNameException);
            }

            this.Warn(exception, ProvisioningAgentMonitor.CorrelationIdentifierDefault);
        }
    }
}