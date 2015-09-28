//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Globalization;
    using System.IdentityModel.Tokens;
    using Microsoft.Owin.Security.ActiveDirectory;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Samples.Properties;

    internal static class Program
    {
        private const string ArgumentNameTenantIdentifier = "tenantIdentifier";

        private const string AuthenticationEndpointAddressAzureActiveDirectory = "https://login.windows.net";
        
        private const int NumberArguments = 3;
        private const int NumberArgumentsRequired = 2;

        private const string FederationMetadataAddressTemplate = "{0}/federationmetadata/2007-06/federationmetadata.xml";

        private const string ServiceApplicationIdentifierDefault = "00000002-0000-0000-c000-000000000000";

        private readonly static Lazy<Uri> AuthenticationEndpointAddress =
            new Lazy<Uri>(
                () =>
                    new Uri(Program.AuthenticationEndpointAddressAzureActiveDirectory));

        private static WindowsAzureActiveDirectoryBearerAuthenticationOptions ComposeAuthenticationOptions(string tenantIdentifier)
        {
            if (string.IsNullOrWhiteSpace(tenantIdentifier))
            {
                throw new ArgumentNullException(Program.ArgumentNameTenantIdentifier);
            }

            string informationTenantIdentifier =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProvisioningAgentResources.InformationTenantIdentifierTemplate,
                    tenantIdentifier);
            Console.WriteLine(informationTenantIdentifier);

            TokenValidationParameters tokenValidationParameters =
                new TokenValidationParameters()
                {
                    ValidAudience = Program.ServiceApplicationIdentifierDefault
                };
            
            string tenantMetadataAddress =
                string.Format(
                    CultureInfo.InvariantCulture,
                    Program.FederationMetadataAddressTemplate,
                    tenantIdentifier);
            Uri metadataAddressRelative = new Uri(tenantMetadataAddress, UriKind.Relative);
            Uri metadataAddress =
                new Uri(
                    Program.AuthenticationEndpointAddress.Value,
                    metadataAddressRelative);

            WindowsAzureActiveDirectoryBearerAuthenticationOptions result =
                new WindowsAzureActiveDirectoryBearerAuthenticationOptions()
                {
                    MetadataAddress = metadataAddress.AbsoluteUri,
                    TokenValidationParameters = tokenValidationParameters,
                    Tenant = tenantIdentifier
                };
            return result;
        }

        private static void Main(string[] arguments)
        {
            if (null == arguments)
            {
                Console.WriteLine(AmazonProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            if (Program.NumberArgumentsRequired > arguments.Length)
            {
                Console.WriteLine(AmazonProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            if (arguments.Length > Program.NumberArguments)
            {
                Console.WriteLine(AmazonProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            string credentialsProfileName = arguments[0];
            string informationCredentialsProfileName =
                string.Format(
                    CultureInfo.InvariantCulture,
                    AmazonProvisioningAgentResources.InformationCredentialsProfileNameTemplate,
                    credentialsProfileName);
            Console.WriteLine(informationCredentialsProfileName);

            string argumentBaseAddress = arguments[1];
            Uri baseAddress = null;
            if (!Uri.TryCreate(argumentBaseAddress, UriKind.Absolute, out baseAddress))
            {
                Console.WriteLine(AmazonProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            string informationBaseAddress =
                string.Format(
                    CultureInfo.InvariantCulture,
                    ProvisioningAgentResources.InformationBaseAddressTemplate,
                    argumentBaseAddress);
            Console.WriteLine(informationBaseAddress);

            IProvider provider;
            
            if (arguments.Length > Program.NumberArgumentsRequired)
            {
                string tenantIdentifier = arguments[1];

                WindowsAzureActiveDirectoryBearerAuthenticationOptions authenticationOptions =
                    Program.ComposeAuthenticationOptions(tenantIdentifier);
                provider = new AmazonWebServicesProvider(credentialsProfileName, authenticationOptions);
            }
            else
            {
                provider = new AmazonWebServicesProvider(credentialsProfileName);
            }

            IMonitor monitor = new ConsoleMonitor();

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
    }
}
