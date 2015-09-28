//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public interface ITabularFileAdapter: IDisposable
    {
        string FilePath { get; }

        Task<IRow> InsertRow(IReadOnlyDictionary<string, string> columns);
        Task<IRow[]> Query(IReadOnlyDictionary<string, string> columns);
        Task<IRow> ReadRow(string key);
        Task RemoveRow(string key);
        Task ReplaceRow(IRow row);
    }
}
