using DyrenesBeskyttelse.Components.Models;
using DyrenesBeskyttelse.Data;
using DyrenesBeskyttelse.Models;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
using System.Reflection;

namespace DyrenesBeskyttelse.Services;

public class ReportService
{
    private AppDbContext _db;
    
    public ReportService(AppDbContext db)
    {
        _db = db;
    }
    
    public async Task<List<Volunteer>> _GetVolunteersAsync()
    {
        return await _db.Volunteers.ToListAsync();
    }

    
    public async Task<List<Timesheet>> _GetTimesheetsAsync(DateTime start, DateTime end)
    {
        var volunteers = await _db.Volunteers
            .Include(v => v.Sessions)
            .ToListAsync();

        return volunteers.Select(volunteer =>
        {
            // 18/11: Slutdatoen tæller med hele dagen. Før blev fremmøde PÅ slutdatoen ikke talt med,
            // fordi datovælgeren giver slutdatoen kl. 00:00.
            var endExclusive = end.Date.AddDays(1);
            var sessions = volunteer.Sessions
                .Where(s => s.SessionStart >= start && s.SessionStart < endExclusive)
                .ToList();

            var timesheet = Timesheet.Create(volunteer.GetFullName(), sessions);

            // 18/11 (US-9): Overholder den frivillige aktivitetsreglen i perioden? (se Models/ActivityRule.cs)
            // Sidste fremmøde før perioden tæller med, så mellemrummet ved periodens start bliver rigtigt.
            var lastBefore = volunteer.Sessions
                .Where(s => s.SessionStart < start)
                .Select(s => (DateTime?)s.SessionStart)
                .Max();
            timesheet.StartedBefore2026 = volunteer.StartedBefore2026;
            timesheet.BreaksActivityRule = ActivityRule.BreaksRule(volunteer.StartedBefore2026,
                sessions.Select(s => s.SessionStart), start, end, lastBefore);

            return timesheet;
        }).ToList();
    }
    
    
public byte[] GenerateExcelReport(TimesheetReport report)
{
    using var workbook = new XLWorkbook();

    var worksheet = workbook.Worksheets.Add("Timesheet Report");

    // Report title
    worksheet.Cell("A1").Value = "Timesheet Report";
    worksheet.Cell("A1").Style.Font.Bold = true;
    worksheet.Cell("A1").Style.Font.FontSize = 16;

    // Report period
    worksheet.Cell("A2").Value = "Period:";
    worksheet.Cell("B2").Value = report.StartDate;
    worksheet.Cell("B2").Style.DateFormat.Format = "dd-MM-yyyy";

    worksheet.Cell("C2").Value = "to";

    worksheet.Cell("D2").Value = report.EndDate;
    worksheet.Cell("D2").Style.DateFormat.Format = "dd-MM-yyyy";

    // Get exportable properties from Timesheet
    var properties = typeof(Timesheet)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Where(p => p.CanRead && IsExcelType(p.PropertyType))
        .ToList();

    if (properties.Count == 0)
    {
        throw new InvalidOperationException(
            "No exportable Timesheet properties were found.");
    }

    // Column headers
    int headerRow = 4;

    for (int column = 0; column < properties.Count; column++)
    {
        worksheet.Cell(headerRow, column + 1).Value =
            properties[column].Name;
    }

    // Header styling
    var headerRange = worksheet.Range(
        headerRow, 1,
        headerRow, properties.Count
    );

    headerRange.Style.Font.Bold = true;
    headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

    // Populate rows
    int row = headerRow + 1;

    foreach (var timesheet in report.Timesheets)
    {
        for (int column = 0; column < properties.Count; column++)
        {
            var property = properties[column];

            var value = property.GetValue(timesheet);

            WriteExcelCell(
                worksheet.Cell(row, column + 1),
                value
            );
        }

        row++;
    }

    // Formatting
    worksheet.Columns().AdjustToContents();

    worksheet.SheetView.FreezeRows(headerRow);

    // Add filters to the headers
    if (report.Timesheets.Count > 0)
    {
        worksheet.Range(
            headerRow, 1,
            row - 1, properties.Count
        ).SetAutoFilter();
    }

    // Convert workbook to byte array
    using var stream = new MemoryStream();

    workbook.SaveAs(stream);

    return stream.ToArray();
}


// Determines which property types can be exported to Excel.
private static bool IsExcelType(Type type)
{
    type = Nullable.GetUnderlyingType(type) ?? type;

    return type.IsPrimitive
        || type.IsEnum
        || type == typeof(string)
        || type == typeof(decimal)
        || type == typeof(DateTime)
        || type == typeof(DateOnly)
        || type == typeof(TimeOnly)
        || type == typeof(TimeSpan)
        || type == typeof(Guid);
}

// Writes values while preserving their Excel data types.
private static void WriteExcelCell(
    IXLCell cell,
    object? value)
{
    switch (value)
    {
        case null:
            cell.Value = string.Empty;
            break;

        case string v:
            cell.Value = v;
            break;

        case int v:
            cell.Value = v;
            break;

        case long v:
            cell.Value = v;
            break;

        case decimal v:
            cell.Value = v;
            break;

        case double v:
            cell.Value = v;
            break;

        case float v:
            cell.Value = v;
            break;

        case bool v:
            cell.Value = v;
            break;

        case DateTime v:
            cell.Value = v;
            cell.Style.DateFormat.Format = "dd-MM-yyyy HH:mm";
            break;

        case DateOnly v:
            cell.Value = v.ToString("dd-MM-yyyy");
            break;

        case TimeOnly v:
            cell.Value = v.ToString("HH:mm");
            break;

        case Enum v:
            cell.Value = v.ToString();
            break;

        default:
            cell.Value = value.ToString() ?? string.Empty;
            break;
    }
}


    public async Task<TimesheetReport> GetReport(DateTime start, DateTime end)
    {
        List<Timesheet> timesheets = await this._GetTimesheetsAsync(start, end);
        
        return new TimesheetReport(start, end, timesheets);
    }
}