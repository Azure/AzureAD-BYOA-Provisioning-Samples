//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using Microsoft.SystemForCrossDomainIdentityManagement;

    internal interface ISampleComposer
    {
        GroupBase ComposeGroupResource();
        PatchRequest2 ComposeReferencePatch(
            string referenceAttributeName,
            string referencedObjectUniqueIdentifier,
            OperationName operationName);
        PatchRequest2 ComposeUserPatch();
        Resource ComposeUserResource();
    }
}