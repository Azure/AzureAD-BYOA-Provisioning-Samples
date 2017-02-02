//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public class GroupFactory: ResourceFactory<WindowsAzureActiveDirectoryGroup>
    {
        public GroupFactory(IRow row)
            :base(row)
        {
        }

        public override WindowsAzureActiveDirectoryGroup ConstructResource()
        {
            WindowsAzureActiveDirectoryGroup result = new WindowsAzureActiveDirectoryGroup();
            return result;
        }   

        public override void Initialize(WindowsAzureActiveDirectoryGroup resource)
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
            if (this.Row.Columns.TryGetValue(AttributeNames.DisplayName, out value))
            {
                resource.DisplayName = value;
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.ElectronicMailAddresses, out value))
            {
                resource.ElectronicMailAddresses = 
                    ResourceFactory.Deserialize<ElectronicMailAddress[]>(value);
            }

            if (this.Row.Columns.TryGetValue(AttributeNames.ExternalIdentifier, out value))
            {
                resource.ExternalIdentifier = value;
            }
        }     
    }
}
