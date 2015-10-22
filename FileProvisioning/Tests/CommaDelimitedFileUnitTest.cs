//------------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//------------------------------------------------------------

namespace Samples
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class CommaDelimitedFileUnitTest
    {
        private const string CommaDelimitedFileNameTemplate = "{0}.csv";

        private const int CountColumns = 5;
        private const int CountInsertIterations = 2;
        private const int CountRows = 3;

        public static string ComposeFileName()
        {
            string fileNamePrefix = Guid.NewGuid().ToString();
            string result =
                string.Format(
                    CultureInfo.InvariantCulture,
                    CommaDelimitedFileUnitTest.CommaDelimitedFileNameTemplate,
                    fileNamePrefix);
            return result;
        }
 
        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.TabularFile)]
        public async Task TestInserting()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            try
            {
                IReadOnlyCollection<string> columnNames =
                    Enumerable.Range(0, CommaDelimitedFileUnitTest.CountColumns)
                    .Select(
                        (int item) =>
                            item.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                IList<string> keys = new List<string>(CommaDelimitedFileUnitTest.CountRows);

                for (int indexIterations = 0; indexIterations < CommaDelimitedFileUnitTest.CountInsertIterations; indexIterations++)
                {
                    ITabularFileAdapter fileStore = null;
                    try
                    {
                        fileStore = new CommaDelimitedFileAdapter(fileName, columnNames);
                        Assert.IsTrue(File.Exists(fileName));

                        for (int indexRows = 0; indexRows < CommaDelimitedFileUnitTest.CountRows; indexRows++)
                        {
                            Dictionary<string, string> columns =
                                columnNames
                                .ToDictionary(
                                    (string item) =>
                                        item,
                                    (string item) =>
                                        Guid.NewGuid().ToString());

                            IRow row = await fileStore.InsertRow(columns);
                            Assert.IsNotNull(row);
                            Assert.IsFalse(string.IsNullOrWhiteSpace(row.Key));
                            keys.Add(row.Key);
                        }
                    }
                    finally
                    {
                        if (fileStore != null)
                        {
                            fileStore.Dispose();
                            fileStore = null;
                        }
                    }
                }

                Assert.AreEqual(keys.LongCount(), keys.Distinct().LongCount());
                Assert.AreEqual(keys.LongCount() + 1, File.ReadAllLines(fileName).Count());
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.TabularFile)]
        public async Task TestQuerying()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            try
            {
                IReadOnlyCollection<string> columnNames =
                    Enumerable.Range(0, CommaDelimitedFileUnitTest.CountColumns)
                    .Select(
                        (int item) =>
                            item.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                ITabularFileAdapter fileStore = null;
                try
                {
                    fileStore = new CommaDelimitedFileAdapter(fileName, columnNames);
                    Assert.IsTrue(File.Exists(fileName));

                    Dictionary<string, string> columnsWritten =
                        columnNames
                        .ToDictionary(
                            (string item) =>
                                item,
                            (string item) =>
                                Guid.NewGuid().ToString());

                    IList<string> keys = new List<string>(CommaDelimitedFileUnitTest.CountRows);
                    for (int rowIndex = 0; rowIndex < CommaDelimitedFileUnitTest.CountRows; rowIndex++)
                    {
                        IRow rowWritten = await fileStore.InsertRow(columnsWritten);
                        Assert.IsNotNull(rowWritten);
                        Assert.IsFalse(string.IsNullOrWhiteSpace(rowWritten.Key));
                        Assert.IsFalse(
                            keys
                            .Any(
                                (string item) => 
                                    string.Equals(item, rowWritten.Key, StringComparison.OrdinalIgnoreCase)));
                        keys.Add(rowWritten.Key);
                    }

                    IReadOnlyCollection<IRow> rowsRead = await fileStore.Query(columnsWritten);
                    
                    Assert.IsNotNull(rowsRead);
                    Assert.AreEqual(3, rowsRead.Count);
                    Assert.IsTrue(
                            keys
                            .All(
                                (string keyItem) =>
                                    rowsRead
                                    .Any(
                                        (IRow rowItem) => 
                                            string.Equals(keyItem, rowItem.Key, StringComparison.OrdinalIgnoreCase))));
                }
                finally
                {
                    if (fileStore != null)
                    {
                        fileStore.Dispose();
                        fileStore = null;
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.TabularFile)]
        public async Task TestReading()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            try
            {
                IReadOnlyCollection<string> columnNames =
                    Enumerable.Range(0, CommaDelimitedFileUnitTest.CountColumns)
                    .Select(
                        (int item) =>
                            item.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                ITabularFileAdapter fileStore = null;
                try
                {
                    fileStore = new CommaDelimitedFileAdapter(fileName, columnNames);
                    Assert.IsTrue(File.Exists(fileName));

                    Dictionary<string, string> columnsWritten =
                        columnNames
                        .ToDictionary(
                            (string item) =>
                                item,
                            (string item) =>
                                Guid.NewGuid().ToString());

                    IRow rowWritten = await fileStore.InsertRow(columnsWritten);
                    Assert.IsNotNull(rowWritten);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rowWritten.Key));

                    IRow rowRead = await fileStore.ReadRow(rowWritten.Key);
                    Assert.IsNotNull(rowRead.Columns);
                    Assert.AreEqual(CommaDelimitedFileUnitTest.CountColumns, rowRead.Columns.Count);

                    foreach (string columnName in columnsWritten.Keys)
                    {
                        Assert.IsTrue(
                            string.Equals(
                                columnsWritten[columnName], 
                                rowRead.Columns[columnName], 
                                StringComparison.OrdinalIgnoreCase));
                    }
                }
                finally
                {
                    if (fileStore != null)
                    {
                        fileStore.Dispose();
                        fileStore = null;
                    }
                }
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.TabularFile)]
        public async Task TestRemoving()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            try
            {
                IReadOnlyCollection<string> columnNames =
                    Enumerable.Range(0, CommaDelimitedFileUnitTest.CountColumns)
                    .Select(
                        (int item) =>
                            item.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                IRow rowOne;
                ITabularFileAdapter fileStore = null;
                try
                {
                    fileStore = new CommaDelimitedFileAdapter(fileName, columnNames);
                    Assert.IsTrue(File.Exists(fileName));

                    Dictionary<string, string> columnsWritten =
                        columnNames
                        .ToDictionary(
                            (string item) =>
                                item,
                            (string item) =>
                                Guid.NewGuid().ToString());

                    rowOne = await fileStore.InsertRow(columnsWritten);
                    Assert.IsNotNull(rowOne);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rowOne.Key));

                    IRow rowTwo = await fileStore.InsertRow(columnsWritten);
                    Assert.IsNotNull(rowTwo);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rowTwo.Key));

                    Assert.IsFalse(string.Equals(rowOne.Key, rowTwo.Key, StringComparison.OrdinalIgnoreCase));

                    await fileStore.RemoveRow(rowOne.Key);
                }
                finally
                {
                    if (fileStore != null)
                    {
                        fileStore.Dispose();
                        fileStore = null;
                    }
                }

                IReadOnlyCollection<string> lines = File.ReadAllLines(fileName);
                Assert.AreEqual(2, lines.LongCount());
                Assert.IsFalse(
                    lines
                    .Any(
                        (string item) => 
                            item.StartsWith(rowOne.Key, StringComparison.OrdinalIgnoreCase)));
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }

        [TestMethod]
        [TestCategory(TestCategory.Sample)]
        [TestCategory(TestCategory.TabularFile)]
        public async Task TestReplacing()
        {
            string fileName = CommaDelimitedFileUnitTest.ComposeFileName();

            try
            {
                IReadOnlyCollection<string> columnNames =
                    Enumerable.Range(0, CommaDelimitedFileUnitTest.CountColumns)
                    .Select(
                        (int item) =>
                            item.ToString(CultureInfo.InvariantCulture))
                    .ToArray();

                IRow rowOne;
                ITabularFileAdapter fileStore = null;
                string replacementValue;
                try
                {
                    fileStore = new CommaDelimitedFileAdapter(fileName, columnNames);
                    Assert.IsTrue(File.Exists(fileName));

                    Dictionary<string, string> columnsWritten =
                        columnNames
                        .ToDictionary(
                            (string item) =>
                                item,
                            (string item) =>
                                Guid.NewGuid().ToString());

                    rowOne = await fileStore.InsertRow(columnsWritten);
                    Assert.IsNotNull(rowOne);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rowOne.Key));

                    IRow rowTwo = await fileStore.InsertRow(columnsWritten);
                    Assert.IsNotNull(rowTwo);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(rowTwo.Key));

                    Assert.IsFalse(string.Equals(rowOne.Key, rowTwo.Key, StringComparison.OrdinalIgnoreCase));

                    string columnName = columnsWritten.Keys.Last();
                    replacementValue = Guid.NewGuid().ToString();
                    columnsWritten[columnName] = replacementValue;

                    IRow rowReplacment = new Row(rowOne.Key, columnsWritten);
                    await fileStore.ReplaceRow(rowReplacment);
                }
                finally
                {
                    if (fileStore != null)
                    {
                        fileStore.Dispose();
                        fileStore = null;
                    }
                }

                IReadOnlyCollection<string> lines = File.ReadAllLines(fileName);
                Assert.AreEqual(3, lines.LongCount());
                Assert.AreEqual(
                    1,
                    lines
                    .Where(
                        (string item) =>
                                item.StartsWith(rowOne.Key, StringComparison.OrdinalIgnoreCase)
                            &&  item.EndsWith(replacementValue, StringComparison.OrdinalIgnoreCase))
                    .LongCount());
            }
            finally
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }
        }
    }
}
