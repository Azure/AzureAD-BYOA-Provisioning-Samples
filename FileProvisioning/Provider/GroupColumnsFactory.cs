//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class GroupColumnsFactory: ColumnsFactory<WindowsAzureActiveDirectoryGroup>
    {
        public GroupColumnsFactory(WindowsAzureActiveDirectoryGroup group)
            :base(group)
        {
        }

        public override IReadOnlyDictionary<string, string> CreateColumns()
        {
            Dictionary<string, string> result = new Dictionary<string, string>();
            
            if (!string.IsNullOrWhiteSpace(this.Resource.DisplayName))
            {
                result.Add(AttributeNames.DisplayName, this.Resource.DisplayName);
            }

            if (this.Resource.ElectronicMailAddresses != null && this.Resource.ElectronicMailAddresses.Any())
            {
                ElectronicMailAddress[] electronicMailAddresses = this.Resource.ElectronicMailAddresses.ToArray();
                string serializedValue = 
                    ColumnsFactory.Serialize(electronicMailAddresses);
                result.Add(AttributeNames.ElectronicMailAddresses, serializedValue);
            }

            if (!string.IsNullOrWhiteSpace(this.Resource.ExternalIdentifier))
            {
                result.Add(AttributeNames.ExternalIdentifier, this.Resource.ExternalIdentifier);
            }

            result.Add(AttributeNames.Schemas, SchemaIdentifiers.WindowsAzureActiveDirectoryGroup);

            return result;
        }     
    }
}
