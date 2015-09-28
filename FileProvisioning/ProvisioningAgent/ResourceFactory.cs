//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Web.Script.Serialization;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class ResourceFactory<TResource>: ResourceFactory where TResource: Resource
    {
        private const string ArgumentNameResource = "resource";
        private const string ArgumentNameRow = "row";

        protected ResourceFactory(IRow row)
        {
            if (null == row)
            {
                throw new ArgumentNullException(ResourceFactory<TResource>.ArgumentNameRow);
            }

            this.Row = row;
        }

        public TResource Resource
        {
            get;
            private set;
        }

        public IRow Row
        {
            get;
            private set;
        }

        public abstract TResource ConstructResource();

        public TResource Create()
        {
            TResource result = this.ConstructResource();
            this.Identify(result);
            this.Initialize(result);
            return result;
        }
        
        public override Resource CreateResource()
        {
            Resource result = this.Create();
            return result;
        }

        public virtual void Identify(TResource resource)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(ResourceFactory<TResource>.ArgumentNameResource);
            }

            resource.Identifier = this.Row.Key;
        }

        public abstract void Initialize(TResource resource); 
    }

    public abstract class ResourceFactory
    {
        private static readonly Lazy<JavaScriptSerializer> Serializer =
            new Lazy<JavaScriptSerializer>(
                () =>
                    new JavaScriptSerializer());

        public abstract Resource CreateResource();

        public static TResult Deserialize<TResult>(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return default(TResult);
            }

            Type type = typeof(TResult);

            TResult result = (TResult)ResourceFactory.Serializer.Value.Deserialize(value, type);
            return result;
        }
    }
}
