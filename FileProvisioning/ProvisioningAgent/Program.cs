//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Globalization;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Samples.Properties;    

    internal class Program
    {
        private const byte NumberArguments = 2;

        private static void Main(string[] arguments)
        {
            if (null == arguments)
            {
                Console.WriteLine(FileProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            if (Program.NumberArguments > arguments.Length)
            {
                Console.WriteLine(FileProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            string argumentBaseAddress = arguments[0];
            Uri baseAddress = null;
            if (!Uri.TryCreate(argumentBaseAddress, UriKind.Absolute, out baseAddress))
            {
                Console.WriteLine(FileProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            string informationBaseAddress =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProvisioningAgentResources.InformationBaseAddressTemplate,
                    argumentBaseAddress);
            Console.WriteLine(informationBaseAddress);

            IMonitor monitor = new ConsoleMonitor();
            FileProviderBase provider = null;
            try
            {
                provider = new FileProvider(arguments[1]);
                Service webService = null;
                try
                {
                    webService = new WebService(monitor, provider);
                    webService.Start(baseAddress);

                    string informationStarted =
                        string.Format(
                            CultureInfo.InvariantCulture,
                            ProvisioningAgentResources.InformationAgentStartedTemplate,
                            argumentBaseAddress);
                    Console.WriteLine(informationStarted);

                    Console.WriteLine(ProvisioningAgentResources.PromptTerminate);
                    Console.ReadKey(true);
                }
                finally
                {
                    if (webService != null)
                    {
                        webService.Dispose();
                        webService = null;
                    }
                }
            }
            finally
            {
                if (provider != null)
                {
                    provider.Dispose();
                    provider = null;
                }
            }
        }
    }
}
