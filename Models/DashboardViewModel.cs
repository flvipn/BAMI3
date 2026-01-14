using Iepan_Flaviu_Lab4.Models.History;

namespace Iepan_Flaviu_Lab4.Models
{
    public class DashboardViewModel
    {
        public int TotalPredictions { get; set; }
        public List<PaymentTypeStat> PaymentTypeStats { get; set; } = new();
        public List<PriceBucketStat> PriceBuckets { get; set; } = new();

        // Proprietăți noi pentru intervalul de date
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}