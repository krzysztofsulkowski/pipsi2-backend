using budget_api.Models;
using budget_api.Models.Dto;
using budget_api.Services.Errors;
using budget_api.Services.Interfaces;
using budget_api.Services.Results;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Linq.Dynamic.Core;

namespace budget_api.Services
{
    public class HistoryLogService : IHistoryLogService
    {
        private readonly BudgetApiDbContext _context;
        private readonly ILogger<HistoryLogService> _logger;
        public HistoryLogService(BudgetApiDbContext context, ILogger<HistoryLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ServiceResult<DataTableResponse<HistoryLogDto>>> GetHistoryLogs(DataTableRequest request)
        {
            try
            {
                string[] columnNames = { "CreationDate", "ObjectId", "ObjectType", "EventType" };

                string sortColumn = (request.OrderColumn >= 0 && request.OrderColumn < columnNames.Length) ? columnNames[request.OrderColumn] : "CreationDate";
                var baseQuery = _context.HistoryLogs.AsQueryable();
                var totalRecords = await baseQuery.CountAsync();

                if (!string.IsNullOrEmpty(request.SearchValue))
                {
                    string searchValueLower = request.SearchValue.ToLower();
                    baseQuery = baseQuery.Where(hl =>
                        (hl.ObjectId != null && hl.ObjectId.ToLower().Contains(searchValueLower)) ||
                        (hl.ObjectType != null && hl.ObjectType.ToLower().Contains(searchValueLower)) ||
                        (hl.EventType != null && hl.EventType.ToLower().Contains(searchValueLower)) ||
                        (hl.UserEmail != null && hl.UserEmail.ToLower().Contains(searchValueLower)));
                }

                var recordsFiltered = await baseQuery.CountAsync();

                if (!(string.IsNullOrEmpty(sortColumn) && string.IsNullOrEmpty(request.OrderDir)))
                {
                    baseQuery = baseQuery.OrderBy(sortColumn + " " + request.OrderDir);
                }


                var data = await baseQuery
                    .Skip(request.Start)
                    .Take(request.Length)
                    .Select(hl => new HistoryLogDto
                    {
                        CreationDate = hl.CreationDate,
                        EventType = hl.EventType,
                        ObjectId = hl.ObjectId,
                        ObjectType = hl.ObjectType,
                        Before = FormatJsonToHtml(hl.Before, _logger),
                        After = FormatJsonToHtml(hl.After, _logger),
                        UserEmail = hl.UserEmail,
                        UserId = hl.UserId,
                    })
                    .ToListAsync();

                return ServiceResult<DataTableResponse<HistoryLogDto>>.Success(new DataTableResponse<HistoryLogDto>
                {
                    Draw = request.Draw,
                    RecordsTotal = totalRecords,
                    RecordsFiltered = recordsFiltered,
                    Data = data
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Wystąpił błąd podczas pobierania logów historii");
                return ServiceResult<DataTableResponse<HistoryLogDto>>.Failure(CommonErrors.DataProcessingError());
            }
        }



        private static string FormatJsonToHtml(string json, ILogger<HistoryLogService> _logger)
        {
            if (string.IsNullOrEmpty(json))
                return string.Empty;

            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    string htmlOutput = "<dl>";
                    foreach (JsonProperty property in document.RootElement.EnumerateObject())
                    {
                        htmlOutput += $"<dt><b>{property.Name}:</b></dt>";
                        htmlOutput += $"<dd>{property.Value}</dd>";
                    }
                    htmlOutput += "</dl>";

                    return htmlOutput;
                }
            }
            catch (JsonException)
            {
                return json;
            }
        }
    }

}
