using System;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using FoodLens.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FoodLens.Api.Services
{
    [CoreWCF.ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class DietExportService : IDietExportService
    {
        private readonly AppDbContext _db;

        public DietExportService(AppDbContext db)
        {
            _db = db;
        }

        public async Task<DietLogExportXml> ExportDietLog(string userId, DateTime from, DateTime to)
        {
            // Normalize dates to ensure start of day and end of day ranges
            var startDate = from.Date;
            var endDate = to.Date.AddDays(1);

            if (startDate > endDate)
            {
                var msg = "Invalid date range: Start date cannot be after end date.";
                throw new FaultException<ExportFault>(
                    new ExportFault { Message = msg },
                    new FaultReason(msg));
            }

            if (startDate > DateTime.UtcNow.AddDays(1))
            {
                var msg = "Invalid date range: Start date cannot be in the future.";
                throw new FaultException<ExportFault>(
                    new ExportFault { Message = msg },
                    new FaultReason(msg));
            }

            // Fetch records from DB
            var logs = await _db.FoodLogs
                .Where(l => l.DeviceId == userId && l.LoggedAt >= startDate && l.LoggedAt < endDate)
                .OrderBy(l => l.LoggedAt)
                .ToListAsync();

            if (logs.Count == 0)
            {
                var msg = $"No diet log entries found for user '{userId}' between {from:yyyy-MM-dd} and {to:yyyy-MM-dd}.";
                throw new FaultException<ExportFault>(
                    new ExportFault { Message = msg },
                    new FaultReason(msg));
            }

            // Map to export DTOs
            var export = new DietLogExportXml
            {
                UserId = userId,
                ExportedAt = DateTime.UtcNow,
                Entries = logs.Select(l => new ExportedFoodLogEntry
                {
                    LoggedAt = l.LoggedAt,
                    FoodName = l.FoodName,
                    Grams = l.Grams,
                    Calories = l.Calories,
                    Protein = l.ProteinG,
                    Carbs = l.CarbsG,
                    Fat = l.FatG
                }).ToList()
            };

            return export;
        }
    }
}
