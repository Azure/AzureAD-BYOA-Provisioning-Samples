//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Graph.Provisioning;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Samples.Properties;

    internal class Program
    {
        private const int NumberArguments = 5;

        private static bool IsOperationCancelledException(Exception exception)
        {
            if (null == exception)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            AggregateException aggregate = exception as AggregateException;
            if
            (
                aggregate
                .InnerExceptions
                .Any(
                    (Exception item) => 
                        Program.IsOperationCancelledException(item))
            )
            {
                return true;
            }

            bool result = (exception as OperationCanceledException) != null;
            return result;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "The variable, tokenFactory is disposed on all paths.")]
        private static void Main(string[] arguments)
        {
            if (null == arguments)
            {
                Console.WriteLine(FileProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            if (Program.NumberArguments != arguments.Length)
            {
                Console.WriteLine(FileProvisioningAgentResources.InformationCommandLineArguments);
                return;
            }

            Guid tenantIdentifier = new Guid(arguments[1]);
            Guid servicePrincipalIdentifier = new Guid(arguments[2]);
            string provisioningTaskIdentifier =
                new AzureToAgentTaskIdentifierFactory()
                    .Create(
                        tenantIdentifier,
                        servicePrincipalIdentifier);

            IMonitor monitor = new ProvisioningAgentMonitor(FileProvisioningAgentResources.PromptTerminate);

            FileProviderBase provider = null;
            try
            {
                provider =
                    new AccessConnectivityEngineFileProviderFactory(
                        arguments[0],
                        monitor)
                    .CreateProvider();

                TokenFactory tokenFactory = null;
                try
                {
                    tokenFactory = new AzureHubClientTokenFactory(arguments[3], arguments[4]);

                    IAgent agent = null;
                    try
                    {
                        agent =
                            new Agent(
                                provisioningTaskIdentifier, 
                                tokenFactory, 
                                provider, 
                                monitor);
                        tokenFactory = null;

                        CancellationTokenSource terminationTokenSource = null;
                        try
                        {
                            terminationTokenSource = new CancellationTokenSource();
                            CancellationToken terminationToken = terminationTokenSource.Token;
                            Func<Task> provisioningFunction =
                                new Func<Task>(
                                    () =>
                                        agent.ProvisionAsync(terminationToken));
                            Task provisioningTask = null;
                            try
                            {
                                provisioningTask = Task.Run(provisioningFunction, terminationToken);
                                Console.WriteLine(FileProvisioningAgentResources.PromptTerminate);
                                Console.ReadKey(true);
                                terminationTokenSource.Cancel();
                            }
                            catch (AggregateException exception)
                            {
                                if (!Program.IsOperationCancelledException(exception))
                                {
                                    throw;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                            }
                            finally
                            {
                                if (provisioningTask != null)
                                {
                                    provisioningTask.Wait();
                                    provisioningTask.Dispose();
                                    provisioningTask = null;
                                }
                            }
                        }
                        finally
                        {
                            if (terminationTokenSource != null)
                            {
                                terminationTokenSource.Dispose();
                                terminationTokenSource = null;
                            }
                        }
                    }
                    finally
                    {
                        if (agent != null)
                        {
                            agent.Dispose();
                            agent = null;
                        }
                    }
                }
                finally
                {
                    if (tokenFactory != null)
                    {
                        tokenFactory.Dispose();
                        tokenFactory = null;
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

        private class AzureHubClientTokenFactory : TokenFactory
        {
            private readonly object thisLock = new object();

            private TokenFactory innerFactory;
            private SecureString passwordValue;
            private SecureString userNameValue;

            public AzureHubClientTokenFactory(string userName, string password)
            {
                this.userNameValue = AzureHubClientTokenFactory.Secure(userName);
                this.passwordValue = AzureHubClientTokenFactory.Secure(password);
                this.innerFactory = this.InitializeTokenFactory();
            }

            private AzureHubClientTokenFactory(AzureHubClientTokenFactory other)
                : this(AzureHubClientTokenFactory.Read(other?.userNameValue), AzureHubClientTokenFactory.Read(other?.passwordValue))
            {
            }

            public override TokenFactory Clone()
            {
                TokenFactory result = null;
                try
                {
                    result = new AzureHubClientTokenFactory(this);
                    return result;
                }
                catch
                {
                    if (result != null)
                    {
                        result.Dispose();
                        result = null;
                    }

                    throw;
                }
            }

            protected override void Dispose(bool disposing)
            {
                try
                {
                    if (disposing)
                    {
                        try
                        {
                            if (this.innerFactory != null)
                            {
                                lock (this.thisLock)
                                {
                                    if (this.innerFactory != null)
                                    {
                                        this.innerFactory.Dispose();
                                        this.innerFactory = null;
                                    }
                                }
                            }
                        }
                        finally
                        {
                            try
                            {
                                if (this.userNameValue != null)
                                {
                                    lock (this.thisLock)
                                    {
                                        if (this.userNameValue != null)
                                        {
                                            this.userNameValue.Dispose();
                                            this.userNameValue = null;
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (this.passwordValue != null)
                                {
                                    lock (this.thisLock)
                                    {
                                        if (this.passwordValue != null)
                                        {
                                            this.passwordValue.Dispose();
                                            this.passwordValue = null;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                finally
                {
                    base.Dispose(true);
                }
            }

            public override IAzureTenantToken CreateTenantToken()
            {
                IAzureTenantToken result = null;
                try
                {
                    result = this.innerFactory.CreateTenantToken();
                    return result;
                }
                catch
                {
                    if (result != null)
                    {
                        result.Dispose();
                        result = null;
                    }

                    throw;
                }
            }

            public override SecureString CreateToken()
            {
                SecureString result = null;
                try
                {
                    result = this.innerFactory.CreateToken();
                    return result;
                }
                catch
                {
                    if (result != null)
                    {
                        result.Dispose();
                        result = null;
                    }

                    throw;
                }
            }

            private TokenFactory InitializeTokenFactory()
            {
                TokenFactory result = null;
                try
                {
                    result = new AzureActiveDirectoryTokenFactory(this.userNameValue, this.passwordValue);
                    return result;
                }
                catch
                {
                    if (result != null)
                    {
                        result.Dispose();
                        result = null;
                    }

                    throw;
                }

            }

            private static string Read(SecureString value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                IntPtr buffer = IntPtr.Zero;
                try
                {
                    buffer = Marshal.SecureStringToGlobalAllocUnicode(value);
                    return Marshal.PtrToStringUni(buffer, value.Length);
                }
                finally
                {
                    Marshal.ZeroFreeGlobalAllocUnicode(buffer);
                }
            }

            private static SecureString Secure(string value)
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                SecureString result = null;
                try
                {
                    result = new SecureString();

                    foreach (char character in value)
                    {
                        result.AppendChar(character);
                    }

                    result.MakeReadOnly();

                    return result;
                }
                catch (Exception)
                {
                    if (result != null)
                    {
                        result.Dispose();
                        result = null;
                    }

                    throw;
                }
            }
        }
    }
}
