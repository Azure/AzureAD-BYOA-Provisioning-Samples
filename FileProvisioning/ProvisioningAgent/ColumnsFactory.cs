//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Web.Script.Serialization;
    using Microsoft.SystemForCrossDomainIdentityManagement;

    public abstract class ColumnsFactory<TResource>: ColumnsFactory where TResource: Resource
    {
        private const string ArgumentNameResource = "resource";

        protected ColumnsFactory(TResource resource)
        {
            if (null == resource)
            {
                throw new ArgumentNullException(ColumnsFactory<TResource>.ArgumentNameResource);
            }

            this.Resource = resource;
        }

        public TResource Resource
        {
            get;
            private set;
        }
    }

    public abstract class ColumnsFactory
    {
        private static readonly Lazy<JavaScriptSerializer> Serializer =
            new Lazy<JavaScriptSerializer>(
                () =>
                    new JavaScriptSerializer());

        public abstract IReadOnlyDictionary<string, string> CreateColumns();

        public static string Serialize(object value)
        {
            if (null == value)
            {
                return string.Empty;
            }

            string result = ColumnsFactory.Serializer.Value.Serialize(value);
            return result;
        }
    }
}
