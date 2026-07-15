using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Threading.Tasks;

namespace FoodLens.Api.Services
{
    [ServiceContract(Namespace = "http://foodlens.api/legacy")]
    public interface IDietExportService
    {
        [OperationContract]
        [FaultContract(typeof(ExportFault))]
        Task<DietLogExportXml> ExportDietLog(string userId, DateTime from, DateTime to);
    }

    [DataContract(Namespace = "http://foodlens.api/legacy")]
    public class DietLogExportXml
    {
        [DataMember]
        public string UserId { get; set; } = string.Empty;

        [DataMember]
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

        [DataMember]
        public List<ExportedFoodLogEntry> Entries { get; set; } = new();
    }

    [DataContract(Namespace = "http://foodlens.api/legacy")]
    public class ExportedFoodLogEntry
    {
        [DataMember]
        public DateTime LoggedAt { get; set; }

        [DataMember]
        public string FoodName { get; set; } = string.Empty;

        [DataMember]
        public double Grams { get; set; }

        [DataMember]
        public double Calories { get; set; }

        [DataMember]
        public double Protein { get; set; }

        [DataMember]
        public double Carbs { get; set; }

        [DataMember]
        public double Fat { get; set; }
    }

    [DataContract(Namespace = "http://foodlens.api/legacy")]
    public class ExportFault
    {
        [DataMember]
        public string Message { get; set; } = string.Empty;
    }
}
