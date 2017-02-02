//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class UserFactory: ResourceFactory<Core2EnterpriseUser>
    {
        public UserFactory(IRow row)
            :base(row)
        {
        }

        public override Core2EnterpriseUser ConstructResource()
        {
            Core2EnterpriseUser result = new Core2EnterpriseUser();
            return result;
        }

        public override void Initialize(Core2EnterpriseUser resource)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(nameof(resource));
            }

            if (null == this.Row.Columns)
            {
                return;
            }

            string value = null;
            if (this.Row.Columns.TryGetValue(AttributeNames.Active, out value))
            {
                resource.Active = bool.Parse(value);
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.Addresses, out value))
            {
                resource.Addresses = ResourceFactory.Deserialize<Address[]>(value);
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.Department, out value))
            {
                resource.Department = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.DisplayName, out value))
            {
                resource.DisplayName = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.ElectronicMailAddresses, out value))
            {
                resource.ElectronicMailAddresses = ResourceFactory.Deserialize<ElectronicMailAddress[]>(value);
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.ExternalIdentifier, out value))
            {
                resource.ExternalIdentifier = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.Manager, out value))
            {
                resource.Manager = 
                    new Manager()
                        {
                            Value = value
                        };
            }

            string familyName;
            if (this.Row.Columns.TryGetValue(AttributeNames.ExternalIdentifier, out value))
            {
                familyName = value;
            }
            else
            {
                familyName = null;
            }

            string formattedName;
            if (this.Row.Columns.TryGetValue(AttributeNames.Formatted, out value))
            {
                formattedName = value;
            }
            else
            {
                formattedName = null;
            }

            string givenName;
            if (this.Row.Columns.TryGetValue(AttributeNames.GivenName, out value))
            {
                givenName = value;
            }
            else
            {
                givenName = null;
            }

            if 
            (
                    !string.IsNullOrWhiteSpace(familyName)
                ||  !string.IsNullOrWhiteSpace(formattedName)
                ||  !string.IsNullOrWhiteSpace(givenName)
            )
            {
                resource.Name = new Name();
                
                if (!string.IsNullOrWhiteSpace(familyName))
                {
                    resource.Name.FamilyName = familyName;
                }

                if (!string.IsNullOrWhiteSpace(formattedName))
                {
                    resource.Name.Formatted = formattedName;
                }

                if (!string.IsNullOrWhiteSpace(givenName))
                {
                    resource.Name.GivenName = givenName;
                }
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.PhoneNumbers, out value))
            {
                resource.PhoneNumbers = ResourceFactory.Deserialize<PhoneNumber[]>(value);
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.PreferredLanguage, out value))
            {
                resource.PreferredLanguage = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.Title, out value))
            {
                resource.Title = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.UserName, out value))
            {
                resource.UserName = value;
            }
        }     
    }
}
