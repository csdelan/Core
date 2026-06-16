using Google.Apis.Sheets.v4.Data;
using System.Globalization;
using System.Reflection;

namespace Core.GoogleSheets
{
    #region RowTable<T> : strongly-typed row CRUD over a worksheet

    public sealed class RowTable<T> where T : new()
    {
        private readonly GoogleWorksheet _ws;
        private readonly int _headerRow;      // 1-based
        private readonly int _dataStartRow;   // 1-based
        private readonly Mapping _map;
        private readonly bool _copyFormattingOnInsert;

        private sealed class Col
        {
            public string Header = "";
            public int Index; // 0-based column index in sheet
            public PropertyInfo Prop = default!;
            public bool IsKey;
        }

        private sealed class Mapping
        {
            public List<Col> Columns = new();
            public Col KeyCol => Columns.First(c => c.IsKey);
            public int MaxColIndex => Columns.Max(c => c.Index);
        }

        /// <summary>
        /// Creates a typed row table bound to a specific worksheet area.
        /// headerRow: which row contains headers (default 1)
        /// dataStartRow: first data row (default headerRow+1)
        /// copyFormattingOnInsert: if true, copies formatting from previous row when inserting new rows
        /// </summary>
        public RowTable(GoogleWorksheet ws, int headerRow = 1, int? dataStartRow = null, bool copyFormattingOnInsert = false)
        {
            _ws = ws ?? throw new ArgumentNullException(nameof(ws));
            _headerRow = headerRow;
            _dataStartRow = dataStartRow ?? (headerRow + 1);
            _copyFormattingOnInsert = copyFormattingOnInsert;
            _map = BuildMapping();
        }

        // ---------- Public API ----------

        /// <summary>Ensure headers exist and are in the right order; writes/repairs if needed.</summary>
        public async Task EnsureHeaderAsync(CancellationToken ct = default)
        {
            var expected = _map.Columns.OrderBy(c => c.Index).Select(c => (object)c.Header).ToList();
            var startCol = 0;
            var endCol = _map.MaxColIndex;
            var a1 = $"{GoogleWorksheet.ColIndexToLetters(startCol)}{_headerRow}:{GoogleWorksheet.ColIndexToLetters(endCol)}{_headerRow}";
            var existing = await _ws.GetRangeAsync(a1, ct);

            bool needsWrite = existing.Count == 0
                              || existing[0].Count < expected.Count
                              || !existing[0].Take(expected.Count).SequenceEqual(expected);

            if (needsWrite)
            {
                await _ws.SetRangeAsync(a1, new List<IList<object>> { expected }, "RAW", ct);
            }
        }

        /// <summary>Get all rows as T, skipping blank key rows.</summary>
        public async Task<IList<T>> GetAllAsync(CancellationToken ct = default)
        {
            var a1 = BuildDataRangeA1();
            var rows = await _ws.GetRangeAsync(a1, ct);
            var list = new List<T>();
            int rowOffset = 0;

            foreach (var row in rows)
            {
                var item = new T();
                bool hasKey = false;

                foreach (var col in _map.Columns)
                {
                    object? cell = col.Index < row.Count ? row[col.Index] : null;
                    var (converted, nonEmpty) = ConvertToType(cell, col.Prop.PropertyType);
                    if (col.IsKey && nonEmpty) hasKey = true;
                    col.Prop.SetValue(item, converted);
                }

                if (hasKey) list.Add(item);
                rowOffset++;
            }
            return list;
        }

        /// <summary>Add or update a single item by key (upsert).</summary>
        public async Task UpsertAsync(T item, CancellationToken ct = default)
        {
            var keyVal = _map.KeyCol.Prop.GetValue(item)?.ToString() ?? "";
            if (string.IsNullOrWhiteSpace(keyVal))
                throw new InvalidOperationException("Key property is null/empty; cannot upsert.");

            var existingRow = await FindRowIndexByKeyAsync(keyVal, ct); // 1-based row number or null
            var rowValues = ToRowValues(item);

            if (existingRow == null)
            {
                // Append at end - copy formatting if enabled
                if (_copyFormattingOnInsert)
                {
                    await AppendRowWithFormattingAsync(rowValues, ct);
                }
                else
                {
                    await _ws.AppendRowsAsync($"{GoogleWorksheet.ColIndexToLetters(0)}{_dataStartRow}",
                        new List<IList<object>> { rowValues }, "USER_ENTERED", ct);
                }
            }
            else
            {
                var startCol = 0;
                var endCol = _map.MaxColIndex;
                var a1 = $"{GoogleWorksheet.ColIndexToLetters(startCol)}{existingRow}:{GoogleWorksheet.ColIndexToLetters(endCol)}{existingRow}";
                await _ws.SetRangeAsync(a1, new List<IList<object>> { rowValues }, "USER_ENTERED", ct);
            }
        }

        /// <summary>Bulk upsert. Minimizes round-trips versus calling UpsertAsync in a loop.</summary>
        public async Task UpsertRangeAsync(IEnumerable<T> items, CancellationToken ct = default)
        {
            // Read key column once, map key->rowIndex
            var keyColA1 = BuildKeyColumnRangeA1();
            var keyColValues = await _ws.GetRangeAsync(keyColA1, ct);
            var keyToRow = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < keyColValues.Count; i++)
            {
                var v = keyColValues[i].Count > 0 ? keyColValues[i][0]?.ToString() : null;
                if (!string.IsNullOrWhiteSpace(v))
                {
                    int row = _dataStartRow + i; // 1-based row
                    keyToRow[v!] = row;
                }
            }

            var updates = new List<(string a1, IList<object> row)>();
            var appends = new List<IList<object>>();

            foreach (var item in items)
            {
                var key = _map.KeyCol.Prop.GetValue(item)?.ToString() ?? "";
                if (string.IsNullOrWhiteSpace(key)) continue;
                var rowVals = ToRowValues(item);

                if (keyToRow.TryGetValue(key, out int row))
                {
                    var a1 = $"{GoogleWorksheet.ColIndexToLetters(0)}{row}:{GoogleWorksheet.ColIndexToLetters(_map.MaxColIndex)}{row}";
                    updates.Add((a1, rowVals));
                }
                else
                {
                    appends.Add(rowVals);
                }
            }

            // Batch updates
            foreach (var (a1, row) in updates)
                await _ws.SetRangeAsync(a1, new List<IList<object>> { row }, "USER_ENTERED", ct);

            if (appends.Count > 0)
            {
                if (_copyFormattingOnInsert)
                {
                    await AppendRowsWithFormattingAsync(appends, ct);
                }
                else
                {
                    await _ws.AppendRowsAsync($"{GoogleWorksheet.ColIndexToLetters(0)}{_dataStartRow}", appends, "USER_ENTERED", ct);
                }
            }
        }

        /// <summary>Delete a row by key value.</summary>
        public async Task DeleteByKeyAsync(string keyValue, CancellationToken ct = default)
        {
            var row = await FindRowIndexByKeyAsync(keyValue, ct);
            if (row == null) return;

            var sheetId = await _ws.GetSheetIdAsync(ct);
            var req = new Request
            {
                DeleteDimension = new DeleteDimensionRequest
                {
                    Range = new DimensionRange
                    {
                        SheetId = sheetId,
                        Dimension = "ROWS",
                        StartIndex = row.Value - 1, // zero-based inclusive
                        EndIndex = row.Value // exclusive
                    }
                }
            };
            await _ws.BatchUpdateAsync(new List<Request> { req }, ct);
        }

        // ---------- Internals ----------

        private Mapping BuildMapping()
        {
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                 .Where(p => p.CanRead && p.CanWrite)
                                 .Select(p => new
                                 {
                                     Prop = p,
                                     ColAttr = p.GetCustomAttribute<SheetColumnAttribute>(),
                                     KeyAttr = p.GetCustomAttribute<SheetKeyAttribute>()
                                 })
                                 .Where(x => x.ColAttr != null)
                                 .ToList();

            if (props.Count == 0)
                throw new InvalidOperationException($"Type {typeof(T).Name} has no [SheetColumn] properties.");

            if (props.Count(x => x.KeyAttr != null) != 1)
                throw new InvalidOperationException($"Type {typeof(T).Name} must have exactly one [SheetKey] property.");

            // Determine 0-based column indices
            var explicitIdx = props.Where(x => x.ColAttr!.Index.HasValue).ToList();
            var implicitIdx = props.Where(x => !x.ColAttr!.Index.HasValue).ToList();

            var used = new HashSet<int>();
            foreach (var x in explicitIdx)
            {
                if (!used.Add(x.ColAttr!.Index!.Value))
                    throw new InvalidOperationException("Duplicate Index in [SheetColumn] attributes.");
            }

            int next = 0;
            foreach (var x in implicitIdx)
            {
                while (used.Contains(next)) next++;
                x.ColAttr!.GetType().GetProperty("Index")!.SetValue(x.ColAttr, next);
                used.Add(next);
                next++;
            }

            var cols = props.Select(x => new Col
            {
                Header = x.ColAttr!.Header,
                Index = x.ColAttr!.Index!.Value,
                Prop = x.Prop,
                IsKey = x.KeyAttr != null
            })
            .OrderBy(c => c.Index)
            .ToList();

            return new Mapping { Columns = cols };
        }

        private string BuildDataRangeA1()
        {
            var startCol = 0;
            var endCol = _map.MaxColIndex;
            var start = $"{GoogleWorksheet.ColIndexToLetters(startCol)}{_dataStartRow}";
            var end = $"{GoogleWorksheet.ColIndexToLetters(endCol)}";
            // open-ended rows; Sheets API will return compact range
            return $"{start}:{end}";
        }

        private string BuildKeyColumnRangeA1()
        {
            var c = _map.KeyCol.Index;
            var colLetter = GoogleWorksheet.ColIndexToLetters(c);
            return $"{colLetter}{_dataStartRow}:{colLetter}";
        }

        private async Task<int?> FindRowIndexByKeyAsync(string key, CancellationToken ct)
        {
            var colA1 = BuildKeyColumnRangeA1();
            var vals = await _ws.GetRangeAsync(colA1, ct);
            for (int i = 0; i < vals.Count; i++)
            {
                var v = vals[i].Count > 0 ? vals[i][0]?.ToString() : null;
                if (string.Equals(v, key, StringComparison.OrdinalIgnoreCase))
                    return _dataStartRow + i; // 1-based row
            }
            return null;
        }

        private IList<object> ToRowValues(T item)
        {
            var arr = new object[_map.MaxColIndex + 1];
            foreach (var col in _map.Columns)
            {
                var v = col.Prop.GetValue(item);
                arr[col.Index] = ToCellValue(v);
            }
            return arr.ToList();
        }

        private static object ToCellValue(object? v)
        {
            if (v == null) return "";
            var t = Nullable.GetUnderlyingType(v.GetType()) ?? v.GetType();

            if (t == typeof(DateTime))
            {
                // Sheets understands ISO 8601 when USER_ENTERED is used; you can switch to serial if you prefer
                return ((DateTime)v).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            }
            if (t == typeof(bool)) return ((bool)v) ? "TRUE" : "FALSE";
            if (t.IsEnum) return v.ToString() ?? string.Empty;
            return v;
        }

        private static (object? value, bool nonEmpty) ConvertToType(object? cell, Type targetType)
        {
            string s = cell?.ToString() ?? "";
            bool nonEmpty = !string.IsNullOrWhiteSpace(s);

            if (!nonEmpty)
            {
                if (IsNullable(targetType)) return (null, false);
                return (GetDefault(targetType), false);
            }

            var t = Nullable.GetUnderlyingType(targetType) ?? targetType;

            try
            {
                if (t == typeof(string)) return (s, true);
                if (t == typeof(int)) return (int.Parse(s, CultureInfo.InvariantCulture), true);
                if (t == typeof(long)) return (long.Parse(s, CultureInfo.InvariantCulture), true);
                if (t == typeof(double)) return (double.Parse(s, CultureInfo.InvariantCulture), true);
                if (t == typeof(decimal)) return (decimal.Parse(s, CultureInfo.InvariantCulture), true);
                if (t == typeof(bool))
                {
                    if (string.Equals(s, "TRUE", StringComparison.OrdinalIgnoreCase)) return (true, true);
                    if (string.Equals(s, "FALSE", StringComparison.OrdinalIgnoreCase)) return (false, true);
                    return (bool.Parse(s), true);
                }
                if (t == typeof(DateTime))
                {
                    if (DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
                        return (dt, true);
                    // DateTime parsing failed - return appropriate default instead of string
                    if (IsNullable(targetType)) return (null, false);
                    return (GetDefault(targetType), false);
                }
                if (t.IsEnum) return (Enum.Parse(t, s, ignoreCase: true), true);
                return (Convert.ChangeType(s, t, CultureInfo.InvariantCulture), true);
            }
            catch
            {
                // Fallback: return appropriate default value instead of original string
                if (IsNullable(targetType)) return (null, false);
                return (GetDefault(targetType), false);
            }
        }

        private static bool IsNullable(Type t) =>
            !t.IsValueType || Nullable.GetUnderlyingType(t) != null;

        private static object? GetDefault(Type t) =>
            t.IsValueType ? Activator.CreateInstance(t) : null;

        /// <summary>Append a single row with formatting copied from the previous row</summary>
        private async Task AppendRowWithFormattingAsync(IList<object> rowValues, CancellationToken ct)
        {
            var lastRowIndex = await FindLastDataRowAsync(ct);
            
            // First append the data
            await _ws.AppendRowsAsync($"{GoogleWorksheet.ColIndexToLetters(0)}{_dataStartRow}",
                new List<IList<object>> { rowValues }, "USER_ENTERED", ct);

            // Then copy formatting if there's a previous row to copy from
            if (lastRowIndex.HasValue)
            {
                var newRowIndex = lastRowIndex.Value + 1;
                await CopyRowFormattingAsync(lastRowIndex.Value, newRowIndex, ct);
            }
        }

        /// <summary>Copy formatting from one row to another</summary>
        private async Task CopyRowFormattingAsync(int sourceRow, int targetRow, CancellationToken ct)
        {
            var request = await CreateCopyFormattingRequestAsync(sourceRow, targetRow, ct);
            await _ws.BatchUpdateAsync(new List<Request> { request }, ct);
        }

        /// <summary>Append multiple rows with formatting copied from the previous row</summary>
        private async Task AppendRowsWithFormattingAsync(IList<IList<object>> rowsValues, CancellationToken ct)
        {
            if (rowsValues.Count == 0) return;

            var lastRowIndex = await FindLastDataRowAsync(ct);
            
            // First append all the data
            await _ws.AppendRowsAsync($"{GoogleWorksheet.ColIndexToLetters(0)}{_dataStartRow}", rowsValues, "USER_ENTERED", ct);

            // Then copy formatting if there's a previous row to copy from
            if (lastRowIndex.HasValue)
            {
                var requests = new List<Request>();
                
                for (int i = 0; i < rowsValues.Count; i++)
                {
                    var newRowIndex = lastRowIndex.Value + 1 + i;
                    requests.Add(await CreateCopyFormattingRequestAsync(lastRowIndex.Value, newRowIndex, ct));
                }

                if (requests.Count > 0)
                {
                    await _ws.BatchUpdateAsync(requests, ct);
                }
            }
        }

        /// <summary>Find the last row with data in the table</summary>
        private async Task<int?> FindLastDataRowAsync(CancellationToken ct)
        {
            var keyColA1 = BuildKeyColumnRangeA1();
            var vals = await _ws.GetRangeAsync(keyColA1, ct);
            
            // Find the last non-empty row
            for (int i = vals.Count - 1; i >= 0; i--)
            {
                var v = vals[i].Count > 0 ? vals[i][0]?.ToString() : null;
                if (!string.IsNullOrWhiteSpace(v))
                {
                    return _dataStartRow + i;
                }
            }
            
            return null;
        }

        /// <summary>Create a request to copy formatting from source row to target row</summary>
        private async Task<Request> CreateCopyFormattingRequestAsync(int sourceRow, int targetRow, CancellationToken ct)
        {
            var sheetId = await _ws.GetSheetIdAsync(ct);
            
            var request = new Request
            {
                CopyPaste = new CopyPasteRequest
                {
                    Source = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = sourceRow - 1, // 0-based
                        EndRowIndex = sourceRow,       // 0-based exclusive
                        StartColumnIndex = 0,
                        EndColumnIndex = _map.MaxColIndex + 1
                    },
                    Destination = new GridRange
                    {
                        SheetId = sheetId,
                        StartRowIndex = targetRow - 1, // 0-based
                        EndRowIndex = targetRow,       // 0-based exclusive
                        StartColumnIndex = 0,
                        EndColumnIndex = _map.MaxColIndex + 1
                    },
                    PasteType = "PASTE_FORMAT" // Copy only formatting, not values
                }
            };
            
            return request;
        }
    }

    #endregion
}
