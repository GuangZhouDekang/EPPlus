/*************************************************************************************************
  Required Notice: Copyright (C) EPPlus Software AB. 
  This software is licensed under PolyForm Noncommercial License 1.0.0 
  and may only be used for noncommercial purposes 
  https://polyformproject.org/licenses/noncommercial/1.0.0/

  A commercial license to use this software can be purchased at https://epplussoftware.com
 *************************************************************************************************
  Date               Author                       Change
 *************************************************************************************************
  01/27/2020         EPPlus Software AB       Initial release EPPlus 5
 *************************************************************************************************/
using OfficeOpenXml.Compatibility;
using OfficeOpenXml.LoadFunctions;
using OfficeOpenXml.LoadFunctions.Params;
using OfficeOpenXml.Table;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
#if !NET35 && !NET40
using System.Threading.Tasks;
#endif
namespace OfficeOpenXml
{
    public partial class ExcelRangeBase
    {
        #region LoadFromDataReader
        /// <summary>
        /// Load the data from the datareader starting from the top left cell of the range
        /// </summary>
        /// <param name="Reader">The datareader to loadfrom</param>
        /// <param name="PrintHeaders">Print the column caption property (if set) or the columnname property if not, on first row</param>
        /// <param name="TableName">The name of the table</param>
        /// <param name="TableStyle">The table style to apply to the data</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromDataReader(IDataReader Reader, bool PrintHeaders, string TableName, TableStyles TableStyle = TableStyles.None)
        {
            var r = LoadFromDataReader(Reader, PrintHeaders);
            
            int rows = r.Rows - 1;
            if (rows >= 0 && r.Columns > 0)
            {
                var tbl = _worksheet.Tables.Add(new ExcelAddressBase(_fromRow, _fromCol, _fromRow + (rows <= 0 ? 1 : rows), _fromCol + r.Columns - 1), TableName);
                tbl.ShowHeader = PrintHeaders;
                tbl.TableStyle = TableStyle;
            }
            return r;
        }

        /// <summary>
        /// Load the data from the datareader starting from the top left cell of the range
        /// </summary>
        /// <param name="Reader">The datareader to load from</param>
        /// <param name="PrintHeaders">Print the caption property (if set) or the columnname property if not, on first row</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromDataReader(IDataReader Reader, bool PrintHeaders)
        {
            if (Reader == null)
            {
                throw (new ArgumentNullException("Reader", "Reader can't be null"));
            }
            int fieldCount = Reader.FieldCount;

            int col = _fromCol, row = _fromRow;
            if (PrintHeaders)
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    // If no caption is set, the ColumnName property is called implicitly.
                    _worksheet.SetValueInner(row, col++, Reader.GetName(i));
                }
                row++;
                col = _fromCol;
            }            
            while (Reader.Read())
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    _worksheet.SetValueInner(row, col++, Reader.GetValue(i));
                }
                row++;
                col = _fromCol;
            }
            return _worksheet.Cells[_fromRow, _fromCol, row - 1, _fromCol + fieldCount - 1];
        }
#if !NET35 && !NET40
        /// <summary>
        /// Load the data from the datareader starting from the top left cell of the range
        /// </summary>
        /// <param name="Reader">The datareader to loadfrom</param>
        /// <param name="PrintHeaders">Print the column caption property (if set) or the columnname property if not, on first row</param>
        /// <param name="TableName">The name of the table</param>
        /// <param name="TableStyle">The table style to apply to the data</param>
        /// <param name="cancellationToken">The cancellation token to use</param>
        /// <returns>The filled range</returns>
        public async Task<ExcelRangeBase> LoadFromDataReaderAsync(DbDataReader Reader, bool PrintHeaders, string TableName, TableStyles TableStyle = TableStyles.None,  CancellationToken? cancellationToken=null)
        {
            cancellationToken = cancellationToken ?? CancellationToken.None;
            var r = await LoadFromDataReaderAsync(Reader, PrintHeaders, cancellationToken.Value).ConfigureAwait(false);

            if (cancellationToken.Value.IsCancellationRequested) return r;

            int rows = r.Rows - 1;
            if (rows >= 0 && r.Columns > 0)
            {
                var tbl = _worksheet.Tables.Add(new ExcelAddressBase(_fromRow, _fromCol, _fromRow + (rows <= 0 ? 1 : rows), _fromCol + r.Columns - 1), TableName);
                tbl.ShowHeader = PrintHeaders;
                tbl.TableStyle = TableStyle;
            }
            return r;
        }
        /// <summary>
        /// Load the data from the datareader starting from the top left cell of the range
        /// </summary>
        /// <param name="Reader">The datareader to load from</param>
        /// <param name="PrintHeaders">Print the caption property (if set) or the columnname property if not, on first row</param>
        /// <returns>The filled range</returns>
        public async Task<ExcelRangeBase> LoadFromDataReaderAsync(DbDataReader Reader, bool PrintHeaders)
        {
            return await LoadFromDataReaderAsync(Reader, PrintHeaders, CancellationToken.None);
        }
        /// <summary>
        /// Load the data from the datareader starting from the top left cell of the range
        /// </summary>
        /// <param name="Reader">The datareader to load from</param>
        /// <param name="PrintHeaders">Print the caption property (if set) or the columnname property if not, on first row</param>
        /// <param name="cancellationToken">The cancellation token to use</param>
        /// <returns>The filled range</returns>
        public async Task<ExcelRangeBase> LoadFromDataReaderAsync(DbDataReader Reader, bool PrintHeaders, CancellationToken cancellationToken)
        {
            if (Reader == null)
            {
                throw (new ArgumentNullException("Reader", "Reader can't be null"));
            }
            int fieldCount = Reader.FieldCount;

            int col = _fromCol, row = _fromRow;
            if (PrintHeaders)
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    // If no caption is set, the ColumnName property is called implicitly.
                    _worksheet.SetValueInner(row, col++, Reader.GetName(i));
                }
                row++;
                col = _fromCol;
            }

            while (await Reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                for (int i = 0; i < fieldCount; i++)
                {
                    _worksheet.SetValueInner(row, col++, Reader.GetValue(i));
                }
                row++;
                col = _fromCol;
                if (row % 100 == 0 && cancellationToken.IsCancellationRequested)    //Check every 100 rows
                {
                    return _worksheet.Cells[_fromRow, _fromCol, row - 1, _fromCol + fieldCount - 1];
                }
            }
            return _worksheet.Cells[_fromRow, _fromCol, row - 1, _fromCol + fieldCount - 1];
        }
#endif
        #endregion
        #region LoadFromDataTable
        /// <summary>
        /// Load the data from the datatable starting from the top left cell of the range
        /// </summary>
        /// <param name="Table">The datatable to load</param>
        /// <param name="PrintHeaders">Print the column caption property (if set) or the columnname property if not, on first row</param>
        /// <param name="TableStyle">The table style to apply to the data</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromDataTable(DataTable Table, bool PrintHeaders, TableStyles TableStyle)
        {
            var r = LoadFromDataTable(Table, PrintHeaders);

            int rows = (Table.Rows.Count == 0 ? 1 : Table.Rows.Count) + (PrintHeaders ? 1 : 0);
            if (rows >= 0 && Table.Columns.Count > 0)
            {
                var tbl = _worksheet.Tables.Add(new ExcelAddressBase(_fromRow, _fromCol, _fromRow + rows - 1, _fromCol + Table.Columns.Count - 1), Table.TableName);
                tbl.ShowHeader = PrintHeaders;
                tbl.TableStyle = TableStyle;
            }
            return r;
        }
        /// <summary>
        /// Load the data from the datatable starting from the top left cell of the range
        /// </summary>
        /// <param name="Table">The datatable to load</param>
        /// <param name="PrintHeaders">Print the caption property (if set) or the columnname property if not, on first row</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromDataTable(DataTable Table, bool PrintHeaders)
        {
            if (Table == null)
            {
                throw (new ArgumentNullException("Table can't be null"));
            }

            if (Table.Rows.Count == 0 && PrintHeaders == false)
            {
                return null;
            }

            //var rowArray = new List<object[]>();
            var row = _fromRow;
            if (PrintHeaders)
            {
                _worksheet._values.SetValueRow_Value(_fromRow, _fromCol, Table.Columns.Cast<DataColumn>().Select((dc) => { return dc.Caption; }).ToArray());
                row++;
            }
            foreach (DataRow dr in Table.Rows)
            {
                _worksheet._values.SetValueRow_Value(row++, _fromCol, dr.ItemArray);
            }
            if (row != _fromRow) row--;
            return _worksheet.Cells[_fromRow, _fromCol, row, _fromCol + Table.Columns.Count - 1];
        }
#endregion
        #region LoadFromArrays
        /// <summary>
        /// Loads data from the collection of arrays of objects into the range, starting from
        /// the top-left cell.
        /// </summary>
        /// <param name="Data">The data.</param>
        public ExcelRangeBase LoadFromArrays(IEnumerable<object[]> Data)
        {
            //thanx to Abdullin for the code contribution
            if (!(Data?.Any() ?? false)) return null;

            var maxColumn = 0;
            var row = _fromRow;
            foreach (object[] item in Data)
            {
                _worksheet._values.SetValueRow_Value(row, _fromCol, item);
                if (maxColumn < item.Length) maxColumn = item.Length;
                row++;
            }

            return _worksheet.Cells[_fromRow, _fromCol, row - 1, _fromCol + maxColumn - 1];
        }
#endregion
        #region LoadFromCollection
        /// <summary>
        /// Load a collection into a the worksheet starting from the top left row of the range.
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="Collection">The collection to load</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromCollection<T>(IEnumerable<T> Collection)
        {
            return LoadFromCollection<T>(Collection, false, TableStyles.None, BindingFlags.Public | BindingFlags.Instance, null);
        }
        /// <summary>
        /// Load a collection of T into the worksheet starting from the top left row of the range.
        /// Default option will load all public instance properties of T
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="Collection">The collection to load</param>
        /// <param name="PrintHeaders">Print the property names on the first row. If the property is decorated with a <see cref="DisplayNameAttribute"/> or a <see cref="DescriptionAttribute"/> that attribute will be used instead of the reflected member name.</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromCollection<T>(IEnumerable<T> Collection, bool PrintHeaders)
        {
            return LoadFromCollection<T>(Collection, PrintHeaders, TableStyles.None, BindingFlags.Public | BindingFlags.Instance, null);
        }
        /// <summary>
        /// Load a collection of T into the worksheet starting from the top left row of the range.
        /// Default option will load all public instance properties of T
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="Collection">The collection to load</param>
        /// <param name="PrintHeaders">Print the property names on the first row. If the property is decorated with a <see cref="DisplayNameAttribute"/> or a <see cref="DescriptionAttribute"/> that attribute will be used instead of the reflected member name.</param>
        /// <param name="TableStyle">Will create a table with this style. If set to TableStyles.None no table will be created</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromCollection<T>(IEnumerable<T> Collection, bool PrintHeaders, TableStyles TableStyle)
        {
            return LoadFromCollection<T>(Collection, PrintHeaders, TableStyle, BindingFlags.Public | BindingFlags.Instance, null);
        }
        /// <summary>
        /// Load a collection into the worksheet starting from the top left row of the range.
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="Collection">The collection to load</param>
        /// <param name="PrintHeaders">Print the property names on the first row. Any underscore in the property name will be converted to a space. If the property is decorated with a <see cref="DisplayNameAttribute"/> or a <see cref="DescriptionAttribute"/> that attribute will be used instead of the reflected member name.</param>
        /// <param name="TableStyle">Will create a table with this style. If set to TableStyles.None no table will be created</param>
        /// <param name="memberFlags">Property flags to use</param>
        /// <param name="Members">The properties to output. Must be of type T</param>
        /// <returns>The filled range</returns>
        public ExcelRangeBase LoadFromCollection<T>(IEnumerable<T> Collection, bool PrintHeaders, TableStyles TableStyle, BindingFlags memberFlags, MemberInfo[] Members)
        {
            if(Collection is IEnumerable<IDictionary<string, object>>)
            {
                if(Members == null)
                    return LoadFromDictionaries(Collection as IEnumerable<IDictionary<string, object>>, PrintHeaders, TableStyle);
                return LoadFromDictionaries(Collection as IEnumerable<IDictionary<string, object>>, PrintHeaders, TableStyle, Members.Select(x => x.Name));
            }
            var param = new LoadFromCollectionParams
            {
                PrintHeaders = PrintHeaders,
                TableStyle = TableStyle,
                BindingFlags = memberFlags,
                Members = Members
            };
            var func = new LoadFromCollection<T>(this, Collection, param);
            return func.Load();
        }

        /// <summary>
        /// Load a collection into the worksheet starting from the top left row of the range.
        /// </summary>
        /// <typeparam name="T">The datatype in the collection</typeparam>
        /// <param name="collection">The collection to load</param>
        /// <param name="paramConfig"><see cref="Action{LoacFromCollectionParams}"/> to provide parameters to the function</param>
        /// <example>
        /// sheet.Cells["C1"].LoadFromCollection(items, c =>
        /// {
        ///     c.PrintHeaders = true;
        ///     c.TableStyle = TableStyles.Dark1;
        /// });
        /// </example>
        public ExcelRangeBase LoadFromCollection<T>(IEnumerable<T> collection, Action<LoadFromCollectionParams> paramConfig)
        {
            var param = new LoadFromCollectionParams();
            paramConfig.Invoke(param);
            if (collection is IEnumerable<IDictionary<string, object>>)
            {
                if (param.Members == null)
                    return LoadFromDictionaries(collection as IEnumerable<IDictionary<string, object>>, param.PrintHeaders, param.TableStyle);
                return LoadFromDictionaries(collection as IEnumerable<IDictionary<string, object>>, param.PrintHeaders, param.TableStyle, param.Members.Select(x => x.Name));
            }
            var func = new LoadFromCollection<T>(this, collection, param);
            return func.Load();
        }
        #endregion
        #region LoadFromText
        /// <summary>
        /// Loads a CSV text into a range starting from the top left cell.
        /// Default settings is Comma separation
        /// </summary>
        /// <param name="Text">The Text</param>
        /// <returns>The range containing the data</returns>
        public ExcelRangeBase LoadFromText(string Text)
        {
            return LoadFromText(Text, new ExcelTextFormat());
        }
        /// <summary>
        /// Loads a CSV text into a range starting from the top left cell.
        /// </summary>
        /// <param name="Text">The Text</param>
        /// <param name="Format">Information how to load the text</param>
        /// <returns>The range containing the data</returns>
        public ExcelRangeBase LoadFromText(string Text, ExcelTextFormat Format)
        {
            if (string.IsNullOrEmpty(Text))
            {
                var r = _worksheet.Cells[_fromRow, _fromCol];
                r.Value = "";
                return r;
            }

            if (Format == null) Format = new ExcelTextFormat();


            string[] lines;
            if (Format.TextQualifier == 0)
            {
                lines = SplitLines(Text, Format.EOL);
            }
            else
            {
                lines = GetLines(Text, Format);
            }

            int row = 0;
            int col = 0;
            int maxCol = col;
            int lineNo = 1;
            //var values = new List<object>[lines.Length];
            foreach (string line in lines)
            {
                var items = new List<object>();
                //values[row] = items;

                if (lineNo > Format.SkipLinesBeginning && lineNo <= lines.Length - Format.SkipLinesEnd)
                {
                    col = 0;
                    string v = "";
                    bool isText = false, isQualifier = false;
                    int QCount = 0;
                    int lineQCount = 0;
                    foreach (char c in line)
                    {
                        if (Format.TextQualifier != 0 && c == Format.TextQualifier)
                        {
                            if (!isText && v != "")
                            {
                                throw (new Exception(string.Format("Invalid Text Qualifier in line : {0}", line)));
                            }
                            isQualifier = !isQualifier;
                            QCount += 1;
                            lineQCount++;
                            isText = true;
                        }
                        else
                        {
                            if (QCount > 1 && !string.IsNullOrEmpty(v))
                            {
                                v += new string(Format.TextQualifier, QCount / 2);
                            }
                            else if (QCount > 2 && string.IsNullOrEmpty(v))
                            {
                                v += new string(Format.TextQualifier, (QCount - 1) / 2);
                            }

                            if (isQualifier)
                            {
                                v += c;
                            }
                            else
                            {
                                if (c == Format.Delimiter)
                                {
                                    items.Add(ConvertData(Format, v, col, isText));
                                    v = "";
                                    isText = false;
                                    col++;
                                }
                                else
                                {
                                    if (QCount % 2 == 1)
                                    {
                                        throw (new Exception(string.Format("Text delimiter is not closed in line : {0}", line)));
                                    }
                                    v += c;
                                }
                            }
                            QCount = 0;
                        }
                    }
                    if (QCount > 1 && (v != "" && QCount == 2))
                    {
                        v += new string(Format.TextQualifier, QCount / 2);
                    }
                    if (lineQCount % 2 == 1)
                        throw (new Exception(string.Format("Text delimiter is not closed in line : {0}", line)));
                    items.Add(ConvertData(Format, v, col, isText));

                    _worksheet._values.SetValueRow_Value(_fromRow + row, _fromCol, items);

                    if (col > maxCol) maxCol = col;
                    row++;
                }
                lineNo++;
            }

            if(row<=0)
            {
                return null;
            }
            return _worksheet.Cells[_fromRow, _fromCol, _fromRow + row - 1, _fromCol + maxCol];
        }

        private string[] SplitLines(string text, string EOL)
        {
            var lines=Regex.Split(text, EOL);
            for(int i=0;i<lines.Length;i++)
            {
                if (EOL == "\n" && lines[i].EndsWith("\r")) lines[i] = lines[i].Substring(0, lines[i].Length - 1); //If EOL char is lf and last chart cr then we remove the trailing cr.
                if (EOL == "\r" && lines[i].StartsWith("\n")) lines[i] = lines[i].Substring(1); //If EOL char is cr and last chart lf then we remove the heading lf.
            }
            return lines;
        }

        private string[] GetLines(string text, ExcelTextFormat Format)
        {
            if (Format.EOL == null || Format.EOL.Length == 0) return new string[] { text };
            var eol = Format.EOL;
            var list = new List<string>();
            var inTQ = false;
            var prevLineStart = 0;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == Format.TextQualifier)
                {
                    inTQ = !inTQ;
                }
                else if (!inTQ)
                {
                    if (IsEOL(text, i, eol))
                    {
                        var s = text.Substring(prevLineStart, i - prevLineStart);
                        if (eol == "\n" && s.EndsWith("\r")) s = s.Substring(0, s.Length - 1); //If EOL char is lf and last chart cr then we remove the trailing cr.
                        if (eol == "\r" && s.StartsWith("\n")) s = s.Substring(1); //If EOL char is cr and last chart lf then we remove the heading lf.
                        list.Add(s);
                        i += eol.Length - 1;
                        prevLineStart = i + 1;
                    }
                }
            }

            if (inTQ)
            {
                throw (new ArgumentException(string.Format("Text delimiter is not closed in line : {0}", list.Count)));
            }

            if (prevLineStart >= Format.EOL.Length && IsEOL(text, prevLineStart - Format.EOL.Length, Format.EOL))
            {
                //list.Add(text.Substring(prevLineStart- Format.EOL.Length, Format.EOL.Length));
                list.Add("");
            }
            else
            {
                list.Add(text.Substring(prevLineStart));
            }
            return list.ToArray();
        }
        private bool IsEOL(string text, int ix, string eol)
        {
            for (int i = 0; i < eol.Length; i++)
            {
                if (text[ix + i] != eol[i])
                    return false;
            }
            return ix + eol.Length <= text.Length;
        }

        /// <summary>
        /// Loads a CSV text into a range starting from the top left cell.
        /// </summary>
        /// <param name="Text">The Text</param>
        /// <param name="Format">Information how to load the text</param>
        /// <param name="TableStyle">Create a table with this style</param>
        /// <param name="FirstRowIsHeader">Use the first row as header</param>
        /// <returns></returns>
        public ExcelRangeBase LoadFromText(string Text, ExcelTextFormat Format, TableStyles TableStyle, bool FirstRowIsHeader)
        {
            var r = LoadFromText(Text, Format);

            if (r != null)
            {
                var tbl = _worksheet.Tables.Add(r, "");
                tbl.ShowHeader = FirstRowIsHeader;
                tbl.TableStyle = TableStyle;
            }
            return r;
        }
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <returns></returns>
        public ExcelRangeBase LoadFromText(FileInfo TextFile)
        {
            return LoadFromText(File.ReadAllText(TextFile.FullName, Encoding.ASCII));
        }
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <param name="Format">Information how to load the text</param>
        /// <returns></returns>
        public ExcelRangeBase LoadFromText(FileInfo TextFile, ExcelTextFormat Format)
        {
            if (TextFile.Exists == false)
            {
                throw (new ArgumentException($"File does not exist {TextFile.FullName}"));
            }

            return LoadFromText(File.ReadAllText(TextFile.FullName, Format.Encoding), Format);
        }
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <param name="Format">Information how to load the text</param>
        /// <param name="TableStyle">Create a table with this style</param>
        /// <param name="FirstRowIsHeader">Use the first row as header</param>
        /// <returns></returns>
        public ExcelRangeBase LoadFromText(FileInfo TextFile, ExcelTextFormat Format, TableStyles TableStyle, bool FirstRowIsHeader)
        {
            if (TextFile.Exists == false)
            {
                throw (new ArgumentException($"File does not exist {TextFile.FullName}"));
            }

            return LoadFromText(File.ReadAllText(TextFile.FullName, Format.Encoding), Format, TableStyle, FirstRowIsHeader);
        }
#region LoadFromText async
#if !NET35 && !NET40
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <returns></returns>
        public async Task<ExcelRangeBase> LoadFromTextAsync(FileInfo TextFile)
        {
            if (TextFile.Exists == false)
            {
                throw (new ArgumentException($"File does not exist {TextFile.FullName}"));
            }

            var fs = new FileStream(TextFile.FullName, FileMode.Open, FileAccess.Read);
            var sr = new StreamReader(fs, Encoding.ASCII);            
            return LoadFromText(await sr.ReadToEndAsync().ConfigureAwait(false));
        }
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <param name="Format">Information how to load the text</param>
        /// <returns></returns>
        public async Task<ExcelRangeBase> LoadFromTextAsync(FileInfo TextFile, ExcelTextFormat Format)
        {
            if (TextFile.Exists == false)
            {
                throw (new ArgumentException($"File does not exist {TextFile.FullName}"));
            }

            var fs = new FileStream(TextFile.FullName, FileMode.Open, FileAccess.Read);
            var sr = new StreamReader(fs, Format.Encoding);
            return LoadFromText(await sr.ReadToEndAsync().ConfigureAwait(false), Format);
        }
        /// <summary>
        /// Loads a CSV file into a range starting from the top left cell.
        /// </summary>
        /// <param name="TextFile">The Textfile</param>
        /// <param name="Format">Information how to load the text</param>
        /// <param name="TableStyle">Create a table with this style</param>
        /// <param name="FirstRowIsHeader">Use the first row as header</param>
        /// <returns></returns>
        public async Task<ExcelRangeBase> LoadFromTextAsync(FileInfo TextFile, ExcelTextFormat Format, TableStyles TableStyle, bool FirstRowIsHeader)
        {
            if (TextFile.Exists == false)
            {
                throw (new ArgumentException($"File does not exist {TextFile.FullName}"));
            }

            var fs = new FileStream(TextFile.FullName, FileMode.Open, FileAccess.Read);
            var sr = new StreamReader(fs, Format.Encoding);
            return LoadFromText(await sr.ReadToEndAsync().ConfigureAwait(false), Format, TableStyle, FirstRowIsHeader);
        }
#endif
        #endregion
        #endregion
        #region LoadFromDictionaries
        /// <summary>
        /// Load a collection of dictionaries (or dynamic/ExpandoObjects) into the worksheet starting from the top left row of the range.
        /// These dictionaries should have the same set of keys.
        /// </summary>
        /// <param name="items">A list of dictionaries/></param>
        /// <returns>The filled range</returns>
        /// <example>
        /// <code>
        ///  var items = new List&lt;IDictionary&lt;string, object&gt;&gt;()
        ///    {
        ///        new Dictionary&lt;string, object&gt;()
        ///        { 
        ///            { "Id", 1 },
        ///            { "Name", "TestName 1" }
        ///        },
        ///        new Dictionary&lt;string, object&gt;()
        ///        {
        ///            { "Id", 2 },
        ///            { "Name", "TestName 2" }
        ///        }
        ///    };
        ///    using(var package = new ExcelPackage())
        ///    {
        ///        var sheet = package.Workbook.Worksheets.Add("test");
        ///        var r = sheet.Cells["A1"].LoadFromDictionaries(items);
        ///    }
        /// </code>
        /// </example>
        public ExcelRangeBase LoadFromDictionaries(IEnumerable<IDictionary<string, object>> items)
        {
            return LoadFromDictionaries(items, false, TableStyles.None, null);
        }

        /// <summary>
        /// Load a collection of dictionaries (or dynamic/ExpandoObjects) into the worksheet starting from the top left row of the range.
        /// These dictionaries should have the same set of keys.
        /// </summary>
        /// <param name="items">A list of dictionaries/></param>
        /// <param name="printHeaders">If true the key names from the first instance will be used as headers</param>
        /// <returns>The filled range</returns>
        /// <example>
        /// <code>
        ///  var items = new List&lt;IDictionary&lt;string, object&gt;&gt;()
        ///    {
        ///        new Dictionary&lt;string, object&gt;()
        ///        { 
        ///            { "Id", 1 },
        ///            { "Name", "TestName 1" }
        ///        },
        ///        new Dictionary&lt;string, object&gt;()
        ///        {
        ///            { "Id", 2 },
        ///            { "Name", "TestName 2" }
        ///        }
        ///    };
        ///    using(var package = new ExcelPackage())
        ///    {
        ///        var sheet = package.Workbook.Worksheets.Add("test");
        ///        var r = sheet.Cells["A1"].LoadFromDictionaries(items, true);
        ///    }
        /// </code>
        /// </example>
        public ExcelRangeBase LoadFromDictionaries(IEnumerable<IDictionary<string, object>> items, bool printHeaders)
        {
            return LoadFromDictionaries(items, printHeaders, TableStyles.None, null);
        }

        /// <summary>
        /// Load a collection of dictionaries (or dynamic/ExpandoObjects) into the worksheet starting from the top left row of the range.
        /// These dictionaries should have the same set of keys.
        /// </summary>
        /// <param name="items">A list of dictionaries/></param>
        /// <param name="printHeaders">If true the key names from the first instance will be used as headers</param>
        /// <param name="tableStyle">Will create a table with this style. If set to TableStyles.None no table will be created</param>
        /// <returns>The filled range</returns>
        /// <example>
        /// <code>
        ///  var items = new List&lt;IDictionary&lt;string, object&gt;&gt;()
        ///    {
        ///        new Dictionary&lt;string, object&gt;()
        ///        { 
        ///            { "Id", 1 },
        ///            { "Name", "TestName 1" }
        ///        },
        ///        new Dictionary&lt;string, object&gt;()
        ///        {
        ///            { "Id", 2 },
        ///            { "Name", "TestName 2" }
        ///        }
        ///    };
        ///    using(var package = new ExcelPackage())
        ///    {
        ///        var sheet = package.Workbook.Worksheets.Add("test");
        ///        var r = sheet.Cells["A1"].LoadFromDictionaries(items, true, TableStyles.None);
        ///    }
        /// </code>
        /// </example>
        public ExcelRangeBase LoadFromDictionaries(IEnumerable<IDictionary<string, object>> items, bool printHeaders, TableStyles tableStyle)
        {
            return LoadFromDictionaries(items, printHeaders, tableStyle, null);
        }

        /// <summary>
        /// Load a collection of dictionaries (or dynamic/ExpandoObjects) into the worksheet starting from the top left row of the range.
        /// These dictionaries should have the same set of keys.
        /// </summary>
        /// <param name="items">A list of dictionaries/></param>
        /// <param name="printHeaders">If true the key names from the first instance will be used as headers</param>
        /// <param name="tableStyle">Will create a table with this style. If set to TableStyles.None no table will be created</param>
        /// <param name="keys">Keys that should be used, keys omitted will not be included</param>
        /// <returns>The filled range</returns>
        /// <example>
        /// <code>
        ///  var items = new List&lt;IDictionary&lt;string, object&gt;&gt;()
        ///    {
        ///        new Dictionary&lt;string, object&gt;()
        ///        { 
        ///            { "Id", 1 },
        ///            { "Name", "TestName 1" }
        ///        },
        ///        new Dictionary&lt;string, object&gt;()
        ///        {
        ///            { "Id", 2 },
        ///            { "Name", "TestName 2" }
        ///        }
        ///    };
        ///    using(var package = new ExcelPackage())
        ///    {
        ///        var sheet = package.Workbook.Worksheets.Add("test");
        ///        var r = sheet.Cells["A1"].LoadFromDictionaries(items, true, TableStyles.None, null);
        ///    }
        /// </code>
        /// </example>
        public ExcelRangeBase LoadFromDictionaries(IEnumerable<IDictionary<string, object>> items, bool printHeaders, TableStyles tableStyle, IEnumerable<string> keys)
        {
            var param = new LoadFromDictionariesParams
            {
                PrintHeaders = printHeaders,
                TableStyle = tableStyle,
                Keys = keys
            };
            var func = new LoadFromDictionaries(this, items, param);
            return func.Load();
        }
        #endregion
    }
}
