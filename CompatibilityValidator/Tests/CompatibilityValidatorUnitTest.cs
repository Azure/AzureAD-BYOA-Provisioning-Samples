//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Tools
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CompabilityValidatorUnitTest
    {
        private const string AddressBase = "http://localhost:9000";

        private const string DomainName = "contoso.com";

        private static readonly Lazy<Uri> ResourceBase =
            new Lazy<Uri>(
                () =>
                    new Uri(CompabilityValidatorUnitTest.AddressBase));

        public static readonly object ServiceLock = new object();

        private static readonly string Token = Guid.NewGuid().ToString();

        private static Service service;

        [TestCleanup]
        public void Cleanup()
        {
            if (null != CompabilityValidatorUnitTest.service)
            {
                lock (CompabilityValidatorUnitTest.ServiceLock)
                {
                    if (null != CompabilityValidatorUnitTest.service)
                    {
                        CompabilityValidatorUnitTest.service.Dispose();
                        CompabilityValidatorUnitTest.service = null;
                    }
                }
            }
        }

        [TestInitialize]
        public void Initialize()
        {
            if (null != CompabilityValidatorUnitTest.service)
            {
                lock (CompabilityValidatorUnitTest.ServiceLock)
                {
                    if (null != CompabilityValidatorUnitTest.service)
                    {
                        this.Cleanup();
                    }
                }
            }

            CompabilityValidatorUnitTest.service = new TestService();
            CompabilityValidatorUnitTest.service.Start(CompabilityValidatorUnitTest.ResourceBase.Value);
        }

        [TestMethod]
        [TestCategory(TestCategory.Tools)]
        public void TestVersionTwoOhCompatibility()
        {
            string version = SpecificationVersion.VersionTwoOh.ToString();
            string[] arguments =
                new string[]
                {
                    CompabilityValidatorUnitTest.DomainName,
                    CompabilityValidatorUnitTest.AddressBase,
                    version,
                    CompabilityValidatorUnitTest.Token,
                    AttributeNames.ExternalIdentifier
                };
            Program.Main(arguments);
        }

        private class TestProvider : ProviderBase
        {
            private Resource Resource
            {
                get;
                set;
            }

            public override Task<Resource> CreateAsync(Resource resource, string correlationIdentifier)
            {
                this.Resource = resource;
                this.Resource.Identifier = Guid.NewGuid().ToString();
                Task<Resource> result = Task.FromResult<Resource>(this.Resource);
                return result;
            }

            public override Task DeleteAsync(IResourceIdentifier resourceIdentifier, string correlationIdentifier)
            {
                this.Resource = null;
                return Task.FromResult(0);
            }

            public override Task<Resource[]> QueryAsync(IQueryParameters parameters, string correlationIdentifier)
            {
                Resource[] resources = 
                    new Resource[] 
                    { 
                        this.Resource 
                    };
                Task<Resource[]> result = Task.FromResult<Resource[]>(resources);
                return result;
            }

            public override Task<Resource> RetrieveAsync(IResourceRetrievalParameters parameters, string correlationIdentifier)
            {
                Task<Resource> result = Task.FromResult<Resource>(this.Resource);
                return result;
            }

            public override Task UpdateAsync(IPatch patch, string correlationIdentifier)
            {
                return Task.FromResult(0);
            }
        }

        private sealed class TestService : Service
        {
            public TestService()
            {
                this.MonitoringBehavior = new ConsoleMonitor();
                this.ProviderBehavior = new TestProvider();
            }

            public override IMonitor MonitoringBehavior
            {
                get;
                set;
            }

            public override IProvider ProviderBehavior
            {
                get;
                set;
            }
        }
    }
}