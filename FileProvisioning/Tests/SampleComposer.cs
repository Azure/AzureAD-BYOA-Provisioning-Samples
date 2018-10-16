//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.SystemForCrossDomainIdentityManagement;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    internal class SampleComposer: ISampleComposer
    {
        private const string Domain = "contoso.com";
        private const long FictitiousPhoneNumber = 5551234560;

        private const string ElectronicMailAddressTemplate = "{0}@" + SampleComposer.Domain;

        private const string FormatUniqueIdentifierCompressed = "N";

        // addresses[type eq "Work"].postalCode
        private const string PathExpressionPostalCode =
            AttributeNames.ElectronicMailAddresses +
            "[" + AttributeNames.Type +
            " eq \"" +
            ElectronicMailAddress.Work +
            "]." +
            AttributeNames.PostalCode;

        // emails[type eq "Work" and Primary eq true]
        private const string PathExpressionPrimaryWorkElectronicMailAddress =
            AttributeNames.ElectronicMailAddresses +
            "[" + AttributeNames.Type +
            " eq \"" +
            ElectronicMailAddress.Work +
            "\" and " +
            AttributeNames.Primary +
            " eq true]";

        private static readonly Lazy<ISampleComposer> Singleton =
            new Lazy<ISampleComposer>(
                () =>
                    new SampleComposer());

        private SampleComposer()
        {
        }

        public static ISampleComposer Instance
        {
            get
            {
                return SampleComposer.Singleton.Value;
            }
        }

        public GroupBase ComposeGroupResource()
        {
            string value = Guid.NewGuid().ToString(SampleComposer.FormatUniqueIdentifierCompressed);

            GroupBase result = new WindowsAzureActiveDirectoryGroup();
            result.Identifier = Guid.NewGuid().ToString();
            result.ExternalIdentifier = value;

            return result;
        }

        public PatchRequest2Legacy ComposeReferencePatch(
            string referenceAttributeName,
            string referencedObjectUniqueIdentifier,
            OperationName operationName)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(referenceAttributeName));
            Assert.IsFalse(string.IsNullOrWhiteSpace(referencedObjectUniqueIdentifier));

            IPath path;
            Assert.IsTrue(Path.TryParse(referenceAttributeName, out path));
            OperationValue operationValue =
                new OperationValue()
                {
                    Value = referencedObjectUniqueIdentifier
                };
            PatchOperation2 operation =
                new PatchOperation2()
                {
                    Name = operationName,
                    Path = path
                };
            operation.AddValue(operationValue);

            PatchRequest2Legacy result = new PatchRequest2Legacy();
            result.AddOperation(operation);
            return result;
        }

        public PatchRequest2Legacy ComposeUserPatch()
        {
            string value = Guid.NewGuid().ToString(SampleComposer.FormatUniqueIdentifierCompressed);

            IPath path;
            PatchOperation2 operation;
            OperationValue operationValue;

            PatchRequest2Legacy result = new PatchRequest2Legacy();

            Assert.IsTrue(Path.TryParse(AttributeNames.Active, out path));
            operationValue =
                new OperationValue()
                {
                    Value = bool.FalseString
                };
            operation =
                new PatchOperation2()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            Assert.IsTrue(Path.TryParse(AttributeNames.DisplayName, out path));
            operationValue =
                new OperationValue()
                {
                    Value = value
                };
            operation =
                new PatchOperation2()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            Assert.IsTrue(Path.TryParse(SampleComposer.PathExpressionPrimaryWorkElectronicMailAddress, out path));
            string electronicMailAddressValue =
                string.Format(
                    CultureInfo.InvariantCulture,
                    SampleComposer.ElectronicMailAddressTemplate,
                    value);
            operationValue =
                new OperationValue()
                {
                    Value = electronicMailAddressValue
                };
            operation =
                new PatchOperation2()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            Assert.IsTrue(Path.TryParse(SampleComposer.PathExpressionPostalCode, out path));
            operationValue =
                new OperationValue()
                {
                    Value = value
                };
            operation =
                new PatchOperation2()
                {
                    Name = OperationName.Replace,
                    Path = path
                };
            operation.AddValue(operationValue);
            result.AddOperation(operation);

            return result;
        }

        public Resource ComposeUserResource()
        {
            int countValues = 4;
            IList<string> values = new List<string>(countValues);
            for (int valueIndex = 0; valueIndex < countValues; valueIndex++)
            {
                string value = Guid.NewGuid().ToString(SampleComposer.FormatUniqueIdentifierCompressed);
                values.Add(value);
            }

            ElectronicMailAddress electronicMailAddress = new ElectronicMailAddress();
            electronicMailAddress.ItemType = ElectronicMailAddress.Work;
            electronicMailAddress.Primary = false;
            electronicMailAddress.Value =
                string.Format(
                    CultureInfo.InvariantCulture,
                    SampleComposer.ElectronicMailAddressTemplate,
                    values[1]);

            int countProxyAddresses = 2;
            IList<ElectronicMailAddress> proxyAddresses = new List<ElectronicMailAddress>(countProxyAddresses);
            for (int proxyAddressIndex = 0; proxyAddressIndex < countProxyAddresses; proxyAddressIndex++)
            {
                ElectronicMailAddress proxyAddress = new ElectronicMailAddress();
                proxyAddress.ItemType = ElectronicMailAddress.Other;
                proxyAddress.Primary = false;
                proxyAddress.Value =
                    string.Format(
                        CultureInfo.InvariantCulture,
                        SampleComposer.ElectronicMailAddressTemplate,
                        values[2 + proxyAddressIndex]);
                proxyAddresses.Add(proxyAddress);
            }

            Core2EnterpriseUser result = new Core2EnterpriseUser();

            result.Identifier = Guid.NewGuid().ToString();
            result.ExternalIdentifier = values[0];
            result.Active = true;
            result.DisplayName = values[0];

            result.Name = new Name();
            result.Name.FamilyName = values[0];
            result.Name.GivenName = values[0];

            Address workAddress = new Address();
            workAddress.ItemType = Address.Work;
            workAddress.StreetAddress = values[0];
            workAddress.PostalCode = values[0];

            Address officeLocation = new Address();
            officeLocation.ItemType = Address.Other;
            officeLocation.Primary = false;
            officeLocation.Formatted = values[0];

            PhoneNumber phoneNumberWork = new PhoneNumber();
            phoneNumberWork.ItemType = PhoneNumber.Work;
            phoneNumberWork.Value = SampleComposer.FictitiousPhoneNumber.ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberMobile = new PhoneNumber();
            phoneNumberMobile.ItemType = PhoneNumber.Mobile;
            phoneNumberMobile.Value = (SampleComposer.FictitiousPhoneNumber + 1).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberFacsimile = new PhoneNumber();
            phoneNumberFacsimile.ItemType = PhoneNumber.Fax;
            phoneNumberFacsimile.Value = (SampleComposer.FictitiousPhoneNumber + 2).ToString(CultureInfo.InvariantCulture);

            PhoneNumber phoneNumberPager = new PhoneNumber();
            phoneNumberPager.ItemType = PhoneNumber.Pager;
            phoneNumberPager.Value = (SampleComposer.FictitiousPhoneNumber + 3).ToString(CultureInfo.InvariantCulture);

            result.UserName =
                string.Format(
                    CultureInfo.InvariantCulture,
                    SampleComposer.ElectronicMailAddressTemplate,
                    values[0]);

            result.Addresses =
                new Address[]
                    {
                        workAddress,
                        officeLocation
                    };

            result.ElectronicMailAddresses =
                new ElectronicMailAddress[]
                    {
                        electronicMailAddress,
                    }
                    .Union(proxyAddresses)
                    .ToArray();

            result.PhoneNumbers =
                new PhoneNumber[]
                {
                    phoneNumberWork,
                    phoneNumberFacsimile,
                    phoneNumberMobile,
                    phoneNumberPager
                };

            return result;
        }
    }
}