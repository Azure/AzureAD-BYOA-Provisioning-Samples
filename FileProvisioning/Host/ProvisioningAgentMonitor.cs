//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    internal class ProvisioningAgentMonitor : IMonitor
    {
        private static IMonitor instance;

        private IMonitor consoleMonitor;
        
        public ProvisioningAgentMonitor(string terminationPrompt)
        {
            if (string.IsNullOrWhiteSpace(terminationPrompt))
            {
                throw new ArgumentNullException(nameof(terminationPrompt));
            }

            this.TermationPrompt = terminationPrompt;

            this.consoleMonitor = new ConsoleMonitor();
        }

        private string TermationPrompt
        {
            get;
            set;
        }

        private void Prompt()
        {
            Console.WriteLine();
            Console.WriteLine(this.TermationPrompt);
        }

        public void Inform(IInformationNotification notification)
        {
            this.consoleMonitor.Inform(notification);
            this.Prompt();
        }

        public void Report(IExceptionNotification notification)
        {
            this.consoleMonitor.Report(notification);
            this.Prompt();
        }

        public void Warn(Notification<Exception> notification)
        {
            this.consoleMonitor.Warn(notification);
            this.Prompt();
        }

        public void Warn(Notification<string> notification)
        {
            this.consoleMonitor.Warn(notification);
            this.Prompt();
        }
    }
}