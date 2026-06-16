using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace Core.GoogleSheets
{
    public sealed class GoogleWorksheet
    {
        private readonly SheetsService _service;
        public string SpreadsheetId { get; }
        public string SheetName { get; }
        private int? _sheetIdCache;

        public GoogleWorksheet(SheetsService service, string spreadsheetId, string sheetName)
        {
            _service = service;
            SpreadsheetId = spreadsheetId;
            SheetName = sheetName;
        }

        public static GoogleWorksheet Attach(SheetsService service, string spreadsheetId, string sheetName)
            => new GoogleWorksheet(service, spreadsheetId, sheetName);

        public SheetsService Service => _service;

        public async Task<string?> GetCellAsync(string a1, CancellationToken ct = default)
        {
            var req = _service.Spreadsheets.Values.Get(SpreadsheetId, $"{SheetName}!{a1}");
            var resp = await req.ExecuteAsync(ct);
            if (resp.Values == null || resp.Values.Count == 0 || resp.Values[0].Count == 0)
                return null;
            return resp.Values[0][0]?.ToString();
        }

        public async Task SetRangeAsync(string a1, IList<IList<object>> values, string valueInputOption = "USER_ENTERED", CancellationToken ct = default)
        {
            var body = new ValueRange { Range = $"{SheetName}!{a1}", Values = values };
            var req = _service.Spreadsheets.Values.Update(body, SpreadsheetId, body.Range);
            // Convert string to enum
            req.ValueInputOption = valueInputOption switch
            {
                "RAW" => SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.RAW,
                "USER_ENTERED" => SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED,
                _ => SpreadsheetsResource.ValuesResource.UpdateRequest.ValueInputOptionEnum.USERENTERED
            };
            await req.ExecuteAsync(ct);
        }

        public async Task AppendRowsAsync(string a1Start, IList<IList<object>> values, string valueInputOption = "USER_ENTERED", CancellationToken ct = default)
        {
            var req = _service.Spreadsheets.Values.Append(new ValueRange { Values = values }, SpreadsheetId, $"{SheetName}!{a1Start}");
            req.ValueInputOption = valueInputOption switch
            {
                "RAW" => SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.RAW,
                "USER_ENTERED" => SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED,
                _ => SpreadsheetsResource.ValuesResource.AppendRequest.ValueInputOptionEnum.USERENTERED
            };
            req.InsertDataOption = SpreadsheetsResource.ValuesResource.AppendRequest.InsertDataOptionEnum.INSERTROWS;
            await req.ExecuteAsync(ct);
        }

        public async Task<IList<IList<object>>> GetRangeAsync(string a1, CancellationToken ct = default)
        {
            var req = _service.Spreadsheets.Values.Get(SpreadsheetId, $"{SheetName}!{a1}");
            var resp = await req.ExecuteAsync(ct);
            return resp.Values ?? new List<IList<object>>();
        }

        public async Task<int> GetSheetIdAsync(CancellationToken ct = default)
        {
            if (_sheetIdCache.HasValue) return _sheetIdCache.Value;
            var ss = await _service.Spreadsheets.Get(SpreadsheetId).ExecuteAsync(ct);
            var sheet = ss.Sheets?.FirstOrDefault(s => s.Properties.Title == SheetName)
                       ?? throw new InvalidOperationException($"Sheet '{SheetName}' not found.");
            _sheetIdCache = sheet.Properties.SheetId;
            return _sheetIdCache!.Value;
        }

        public async Task BatchUpdateAsync(IList<Request> requests, CancellationToken ct = default)
        {
            var body = new BatchUpdateSpreadsheetRequest { Requests = requests };
            await _service.Spreadsheets.BatchUpdate(body, SpreadsheetId).ExecuteAsync(ct);
        }

        // Helpers for A1 ↔ index
        public static string ColIndexToLetters(int colIndexZeroBased)
        {
            int n = colIndexZeroBased + 1;
            var s = "";
            while (n > 0)
            {
                int rem = (n - 1) % 26;
                s = (char)('A' + rem) + s;
                n = (n - 1) / 26;
            }
            return s;
        }
    }
}
