//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class FileProviderUnitTest: ProviderTestTemplate<FileProviderBase>
    {
        public override FileProviderBase CreateProvider()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            FileProviderBase result = null;
            try
            {
                result = new FileProvider(fileName);
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

        public override async Task RunTest(Func<ProviderBase, Task> testFunction)
        {
            Assert.IsNotNull(testFunction);
            FileProviderBase provider = null;
            string filePath = null;
            try
            {
                provider = this.CreateProvider();
                filePath = provider.FilePath;
                await testFunction(provider);
            }
            finally
            {
                if (provider != null)
                {
                    provider.Dispose();
                    provider = null;
                }

                if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
        }
    }
}