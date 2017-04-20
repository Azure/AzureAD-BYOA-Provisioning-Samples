//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Data.Common;
    using System.Data.OleDb;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.Threading;

    public class CommaDelimitedFileAdapter: ITabularFileAdapter
    {
        private const string DelimiterFilter = " AND ";
        private const string DelimiterKeyColumnName = "_";
        private const string ColumnNameKey = "Key";

        private const string Comma = ",";
        
        private const string ConnectionStringTemplate = 
            @"Provider={0};Data Source={1};Extended Properties=""text;HDR=YES;FMT=Delimited""";

        private const string FilterTemplate =  "[{0}] = {1}";
        private const string StringCriterionTemplate = "\"{0}\"";

        private const string QueryTemplate = "SELECT * FROM [{0}]";
        private const string QueryByKeyTemplate = 
            CommaDelimitedFileAdapter.QueryTemplate + 
            CommaDelimitedFileAdapter.WhereClausePrefix + 
            "[{1}] = \"{2}\"";

        private const string SchemaFileName = "Schema.ini";
        private const string SchemaFileTemplate = @"[{0}]
Format=CSVDelimited";

        private const string WhereClausePrefix = " WHERE ";

        private readonly string header;
        private readonly IReadOnlyCollection<string> headers;
        private readonly IReadOnlyCollection<string> headersNormalized;
        private readonly string fileName;
        private readonly string pathFolder;
        private readonly string pathSchema;
        private readonly string schema;
        private readonly object thisLock = new object();

        private OleDbConnection connection;
        private string columnNameKey;
        private AsyncSemaphore semaphore;

        public CommaDelimitedFileAdapter(string filePath, string providerName, IReadOnlyCollection<string> columnNames)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (string.IsNullOrWhiteSpace(providerName))
            {
                throw new ArgumentNullException(nameof(providerName));
            }

            if (null == columnNames)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            this.headers = this.ComposeHeaders(columnNames);
            this.headersNormalized =
                this
                .headers
                .Select(
                    (string item) =>
                        item.ToUpperInvariant())
                .ToArray();
            this.header = string.Join(CommaDelimitedFileAdapter.Comma, this.headers);
            this.FilePath = System.IO.Path.GetFullPath(filePath);
            this.pathFolder = System.IO.Path.GetDirectoryName(this.FilePath);
            if (!Directory.Exists(this.pathFolder))
            {
                Directory.CreateDirectory(pathFolder);
            }
            this.fileName = System.IO.Path.GetFileName(FilePath);
            this.pathSchema = System.IO.Path.Combine(this.pathFolder, CommaDelimitedFileAdapter.SchemaFileName);
            this.schema = 
                string.Format(
                    CultureInfo.InvariantCulture,
                    CommaDelimitedFileAdapter.SchemaFileTemplate,
                    this.fileName);

            this.ValidateFile(columnNames);

            string connectionString =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CommaDelimitedFileAdapter.ConnectionStringTemplate,
                    providerName,
                    pathFolder);

            this.connection = new OleDbConnection(connectionString);
            this.connection.Open();

            this.semaphore = new AsyncSemaphore(1);
        }

        public string FilePath
        {
            get;
            private set;
        }

        private static string ComposeCriterion(string criterion)
        {
            if (string.IsNullOrWhiteSpace(criterion))
            {
                throw new ArgumentNullException(nameof(criterion));
            }

            bool boolean;
            if (bool.TryParse(criterion, out boolean))
            {
                return criterion;
            }

            if
            (
                criterion
                .All(
                    (char item) => 
                        Char.IsDigit(item))
            )
            {
                return criterion;
            }

            string result = 
                string.Format(
                    CultureInfo.InvariantCulture, 
                    CommaDelimitedFileAdapter.StringCriterionTemplate, 
                    criterion);
            return result;
        }

        private IReadOnlyCollection<string> ComposeHeaders(IReadOnlyCollection<string> columnNames)
        {
            if (null == columnNames)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            IReadOnlyCollection<string> validColumnNames =
                columnNames
                .Where(
                    (string item) =>
                        !string.IsNullOrWhiteSpace(item))
                .Select(
                    (string item) => 
                        new Value(item).ToString())
                .ToArray();

            this.columnNameKey = CommaDelimitedFileAdapter.ColumnNameKey;
            while (true)
            {
                if
                (
                    validColumnNames
                    .Any(
                        (string item) =>
                            string.Equals(item, this.columnNameKey, StringComparison.OrdinalIgnoreCase))
                )
                {
                    this.columnNameKey = CommaDelimitedFileAdapter.DelimiterKeyColumnName + this.columnNameKey + DelimiterKeyColumnName;
                }
                else
                {
                    break;
                }
            }

            List<string> allColumnNames = new List<string>(columnNames.Count + 1);
            allColumnNames.Add(this.columnNameKey);
            allColumnNames.AddRange(validColumnNames);

            IReadOnlyCollection<string> result = allColumnNames.ToArray();
            return result;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            try
            {
                if (this.connection != null)
                {
                    lock (this.thisLock)
                    {
                        if (this.connection != null)
                        {
                            this.connection.Close();
                            this.connection = null;
                        }
                    }
                }
            }
            finally
            {
                try
                {
                    if (this.semaphore != null)
                    {
                        this.semaphore.Dispose();
                        this.semaphore = null;
                    }
                }
                finally
                {
                    if (File.Exists(this.pathSchema))
                    {
                        File.Delete(this.pathSchema);
                    }
                }
            }
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IRow> InsertRow(string key, IReadOnlyDictionary<string, string> columns)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (null == columns)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            IDictionary<string, string> valuesByNormalizedNames =
                columns
                .ToDictionary(
                    (KeyValuePair<string, string> item) =>
                        new Value(item.Key).ToString().ToUpperInvariant(),
                    (KeyValuePair<string, string> item) =>
                        item.Value);

            IList<string> values = new List<string>(this.headers.Count);

            string keyNormalized = new Value(key).ToString();
            values.Add(keyNormalized);

            IEnumerable<string> normalizedHeaders = this.headersNormalized.Skip(1);
            foreach (string normalizedHeader in normalizedHeaders)
            {
                string value = null;
                if (!valuesByNormalizedNames.TryGetValue(normalizedHeader, out value))
                {
                    values.Add(string.Empty);
                    continue;
                }

                string normalizedValue = new Value(value).ToString();
                values.Add(normalizedValue);
            }

            string row =
                string.Join(CommaDelimitedFileAdapter.Comma, values);

            AsyncSemaphore.Releaser? releaser = null;
            try
            {
                releaser = await this.semaphore.EnterAsync();

                StreamWriter writer = null;
                try
                {
                    writer = File.AppendText(this.FilePath);
                    await writer.WriteLineAsync(row);
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                        writer = null;
                    }
                }
            }
            finally
            {
                if (releaser.HasValue)
                {
                    releaser.Value.Dispose();
                    releaser = null;
                }
            }

            IRow result = new Row(keyNormalized, columns);
            return result;
        }

        public async Task<IRow> InsertRow(IReadOnlyDictionary<string, string> columns)
        {
            if (null == columns)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            string key = Guid.NewGuid().ToString();
            IRow result = await this.InsertRow(key, columns);
            return result;
        }

        private void PrepareFile(IReadOnlyCollection<string> columnNames)
        {
            if (null == columnNames)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            if (!File.Exists(this.pathSchema))
            {
                File.WriteAllText(this.pathSchema, this.schema);
            }

            if (File.Exists(this.FilePath))
            {
                return;
            }

            string text = this.header + Environment.NewLine;
            File.WriteAllText(this.FilePath, text);
        }

        public async Task<IRow[]> Query(IReadOnlyDictionary<string, string> columns)
        {
            if (null == columns)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            string query = 
                string.Format(
                    CultureInfo.InvariantCulture,
                    CommaDelimitedFileAdapter.QueryTemplate,
                    this.fileName);
            if (columns.Any())
            {
                IList<KeyValuePair<string, string>> criteria = new List<KeyValuePair<string, string>>(columns.Count);
                foreach (KeyValuePair<string, string> column in columns)
                {
                    string criterionValue = CommaDelimitedFileAdapter.ComposeCriterion(column.Value);
                    KeyValuePair<string, string> criterion =
                        new KeyValuePair<string, string>(column.Key, criterionValue);
                    criteria.Add(criterion);
                }
                IReadOnlyCollection<string> filters =
                    criteria
                    .Select(
                        (KeyValuePair<string, string> item) =>
                            string.Format(
                                CultureInfo.InvariantCulture,
                                CommaDelimitedFileAdapter.FilterTemplate,
                                item.Key,
                                item.Value))
                    .ToArray();
                string filter =
                    string.Join(CommaDelimitedFileAdapter.DelimiterFilter, filters);

                query = string.Concat(query, CommaDelimitedFileAdapter.WhereClausePrefix, filter);
            }

            OleDbCommand commandQuery = null;
            try
            {
                commandQuery = new OleDbCommand(query, connection);
                DbDataReader reader = null;
                try
                {
                    reader = await commandQuery.ExecuteReaderAsync();
                    IList<IRow> rows = new List<IRow>();

                    while (reader.Read())
                    {
                        string rowKey = (string)reader[0];
                        Dictionary<string, string> rowColumns = 
                            new Dictionary<string, string>(this.headers.Count - 1);
                        for (int indexColumn = 1; indexColumn < this.headers.Count; indexColumn++)
                        {
                            object columnValue = reader[indexColumn];
                            if (null == columnValue)
                            {
                                continue;
                            }

                            string value = columnValue.ToString();
                            if (string.IsNullOrWhiteSpace(value))
                            {
                                continue;
                            }

                            string columnHeader = this.headers.ElementAt(indexColumn);

                            rowColumns.Add(columnHeader, value);
                        }

                        IRow row = new Row(rowKey, rowColumns);
                        rows.Add(row);
                    }

                    IRow[] results = rows.ToArray();
                    return results;
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }
                }
            }
            finally
            {
                if (commandQuery != null)
                {
                    commandQuery.Dispose();
                    commandQuery = null;
                }
            }
        }

        public async Task<IRow> ReadRow(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string keyNormalized = new Value(key).ToString();

            Dictionary<string, string> columns =
                new Dictionary<string, string>()
                    {
                        {
                            this.columnNameKey,
                            keyNormalized
                        }
                    };
            IReadOnlyCollection<IRow> queryResults = await this.Query(columns);
            if (null == queryResults)
            {
                return null;
            }
            IRow result = queryResults.SingleOrDefault();
            return result;
        }

        public async Task RemoveRow(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentNullException(nameof(key));
            }

            string keyNormalized = new Value(key).ToString();
            string keyDelimited = string.Concat(keyNormalized, CommaDelimitedFileAdapter.Comma);
            
            AsyncSemaphore.Releaser? releaser = null;
            try
            {
                releaser = await this.semaphore.EnterAsync();

                string fileNameTemporary = Path.GetTempFileName();

                TextReader reader = null;
                try
                {
                    reader = File.OpenText(this.FilePath);
                    
                    FileStream output = null;
                    try
                    {
                        output = File.OpenWrite(fileNameTemporary);

                        TextWriter writer = null;
                        try
                        {
                            writer = new StreamWriter(output);
                            output = null;

                            string line = await reader.ReadLineAsync();
                            await writer.WriteLineAsync(line);

                            while (true)
                            {
                                line = await reader.ReadLineAsync();
                                if (null == line)
                                {
                                    break;
                                }

                                if (line.StartsWith(keyDelimited, StringComparison.OrdinalIgnoreCase))
                                {
                                    continue;
                                }

                                await writer.WriteLineAsync(line);
                            }
                        }
                        finally
                        {
                            if (writer  != null)
                            {
                                writer.Close();
                                writer = null;
                            }
                        }
                    }
                    finally
                    {
                        if (output != null)
                        {
                            output.Close();
                            output = null;
                        }
                    }
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }
                }
                
                File.Delete(this.FilePath);
                File.Move(fileNameTemporary, this.FilePath);
            }
            finally
            {
                if (releaser.HasValue)
                {
                    releaser.Value.Dispose();
                    releaser = null;
                }
            }
        }

        public async Task ReplaceRow(IRow row)
        {
            if (null == row)
            {
                throw new ArgumentNullException(nameof(row));
            }

            IRow rowCurrent = await this.ReadRow(row.Key);
            if (null == rowCurrent)
            {
                return;
            }

            await this.RemoveRow(row.Key);
            await this.InsertRow(row.Key, row.Columns);
        }

        private void ValidateFile(IReadOnlyCollection<string> columnNames)
        {
            if (null == columnNames)
            {
                throw new ArgumentNullException(nameof(columnNames));
            }

            if (File.Exists(this.pathSchema))
            {
                string existingSchema = File.ReadAllText(this.pathSchema);
                if (existingSchema.IndexOf(this.schema, 0, StringComparison.Ordinal) < 0)
                {
                    File.Delete(this.pathSchema);
                }
            }

            if (File.Exists(this.FilePath))
            {
                string existingHeader;
                StreamReader reader = null;
                try
                {
                    reader = File.OpenText(this.FilePath);
                    existingHeader = reader.ReadLine();
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                        reader = null;
                    }
                }

                if (!string.Equals(existingHeader, this.header, StringComparison.Ordinal))
                {
                    File.Delete(this.FilePath);
                }
            }

            if (!File.Exists(this.FilePath) || !File.Exists(this.pathSchema))
            {
                this.PrepareFile(columnNames);
            }
        }

        private class Value
        {
            private const string Comma = ",";
            private const string QuotationMarkEscaped = "\"\"";
            private const string QuotationMark = "\"";
            private const char QuotationMarkCharacter = '"';
            
            private string valueMember;

            public Value(string value)
            {
                this.valueMember = value ?? string.Empty;

                if
                (
                        (
                                this.valueMember.IndexOf(Value.Comma, StringComparison.OrdinalIgnoreCase) >= 0
                            ||  this.valueMember.IndexOf(Value.QuotationMark, StringComparison.OrdinalIgnoreCase) >= 0
                        )
                    && !this.IsQuoted()
                )
                {
                    this.Quote();
                }
            }

            private bool IsQuoted()
            {
                if (null == this.valueMember)
                {
                    return false;
                }

                int countCharacters = this.valueMember.Length;
                if
                (
                        countCharacters < 1
                    || !this.valueMember.StartsWith(Value.QuotationMark, StringComparison.OrdinalIgnoreCase)
                    || !this.valueMember.EndsWith(Value.QuotationMark, StringComparison.OrdinalIgnoreCase)
                )
                {
                    return false;
                }

                int indexCharacters = 1;
                while (true)
                {
                    if (indexCharacters >= countCharacters - 1)
                    {
                        break;
                    }

                    int countQuotes = 0;
                    while (Value.QuotationMarkCharacter == this.valueMember[indexCharacters + countQuotes])
                    {
                        countQuotes += 1;
                    }

                    if (countQuotes % 2 != 0)
                    {
                        return false;
                    }

                    indexCharacters += (0 == countQuotes ? 1 : countQuotes);
                }

                return true;
            }

            private void Quote()
            {
                this.valueMember = 
                    this.valueMember ?? string.Empty;
                this.valueMember =
                    this.valueMember.Replace(Value.QuotationMark, Value.QuotationMarkEscaped);
                this.valueMember =
                    Value.QuotationMark + this.valueMember + Value.QuotationMark;
            }

            public override string ToString()
            {
                return this.valueMember;
            }
        }
    }
}
