//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class UserColumnsFactory: ColumnsFactory<Core2EnterpriseUser>
    {
        public UserColumnsFactory(Core2EnterpriseUser user)
            :base(user)
        {
        }

        public override IReadOnlyDictionary<string, string> CreateColumns()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();

            string activeValue = this.Resource.Active.ToString(CultureInfo.InvariantCulture);
            result.Add(AttributeNames.Active, activeValue);

            if (this.Resource.Addresses != null && this.Resource.Addresses.Any())
            {
                Address[] addresses = this.Resource.Addresses.ToArray();
                string serializedValue = ColumnsFactory.Serialize(addresses);
                result.Add(AttributeNames.Addresses, serializedValue);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.Department))
            {
                result.Add(AttributeNames.Department, this.Resource.Department);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.DisplayName))
            {
                result.Add(AttributeNames.DisplayName, this.Resource.DisplayName);
            }

            if (this.Resource.ElectronicMailAddresses != null && this.Resource.ElectronicMailAddresses.Any())
            {
                ElectronicMailAddress[] electronicMailAddresses = this.Resource.ElectronicMailAddresses.ToArray();
                string serializedValue = ColumnsFactory.Serialize(electronicMailAddresses);
                result.Add(AttributeNames.ElectronicMailAddresses, serializedValue);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.ExternalIdentifier))
            {
                result.Add(AttributeNames.ExternalIdentifier, this.Resource.ExternalIdentifier);
            }

            if (this.Resource.Manager != null && !string.IsNullOrWhiteSpace(this.Resource.Manager.Value))
            {
                result.Add(AttributeNames.Manager, this.Resource.Manager.Value);
            }

            if (this.Resource.Name != null)
            {
                if (!string.IsNullOrWhiteSpace(this.Resource.Name.FamilyName))
                {
                    result.Add(AttributeNames.FamilyName, this.Resource.Name.FamilyName);
                }

                if (!string.IsNullOrWhiteSpace(this.Resource.Name.Formatted))
                {
                    result.Add(AttributeNames.Formatted, this.Resource.Name.Formatted);
                }

                if (!string.IsNullOrWhiteSpace(this.Resource.Name.GivenName))
                {
                    result.Add(AttributeNames.GivenName, this.Resource.Name.GivenName);
                }
            }

            if (this.Resource.PhoneNumbers != null && this.Resource.PhoneNumbers.Any())
            {
                PhoneNumber[] phoneNumbers = this.Resource.PhoneNumbers.ToArray();
                string serializedValue = ColumnsFactory.Serialize(phoneNumbers);
                result.Add(AttributeNames.PhoneNumbers, serializedValue);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.PreferredLanguage))
            {
                result.Add(AttributeNames.PreferredLanguage, this.Resource.PreferredLanguage);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.Title))
            {
                result.Add(AttributeNames.Title, this.Resource.Title);
            }

            result.Add(AttributeNames.Schemas, SchemaIdentifiers.Core2EnterpriseUser);

            return result;
        }     
    }
}
