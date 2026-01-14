using Iepan_Flaviu_Lab4.Data;
using Iepan_Flaviu_Lab4.Models;
using Iepan_Flaviu_Lab4.Models.History;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;
using static Iepan_Flaviu_Lab4.PricePredictionModel;

namespace Iepan_Flaviu_Lab4.Controllers
{
    public class PredictionController : Controller
    {
        private readonly AppDbContext _context;

        public PredictionController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Price(ModelInput input)
        {
            // Load the model
            MLContext mlContext = new MLContext();

            // ATENTIE: Asigură-te că path-ul este corect pe mașina ta
            ITransformer mlModel = mlContext.Model.Load(@"C:\HDD\Facultate\An 1\Semestru 1\Covaci\Laboratoare\Iepan_Flaviu_Lab4\PricePredictionModel.mlnet", out var modelInputSchema);

            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput, ModelOutput>(mlModel);

            // Try model on sample data to predict fair price
            ModelOutput result = predEngine.Predict(input);
            ViewBag.Price = result.Score;

            var history = new PredictionHistory
            {
                PassengerCount = input.Passenger_count,
                TripTimeInSecs = input.Trip_time_in_secs,
                TripDistance = input.Trip_distance,
                PaymentType = input.Payment_type ?? "Unknown",
                PredictedPrice = result.Score,
                CreatedAt = DateTime.Now
            };
            _context.PredictionHistories.Add(history);
            await _context.SaveChangesAsync();

            return View(input);
        }

        [HttpGet]
        public async Task<IActionResult> History(string? paymentType, float? minPrice, float? maxPrice, DateTime? startDate, DateTime? endDate, string? sortOrder)
        {
            var query = _context.PredictionHistories.AsQueryable();
            if (!string.IsNullOrEmpty(paymentType))
            {
                query = query.Where(p => p.PaymentType == paymentType);
            }
            if (minPrice.HasValue)
            {
                query = query.Where(p => p.PredictedPrice >= minPrice.Value);
            }
            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.PredictedPrice <= maxPrice.Value);
            }
            if (startDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                DateTime endDateFixed = endDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(p => p.CreatedAt <= endDateFixed);
            }

            // --- SORTARE ---
            ViewBag.CurrentSort = sortOrder;
            switch (sortOrder)
            {
                case "price_asc":
                    query = query.OrderBy(x => x.PredictedPrice);
                    break;
                case "price_desc":
                    query = query.OrderByDescending(x => x.PredictedPrice);
                    break;
                case "DateAsc":
                    query = query.OrderBy(x => x.CreatedAt);
                    break;
                default:
                    query = query.OrderByDescending(x => x.CreatedAt);
                    break;
            }

            ViewBag.CurrentPaymentType = paymentType;
            ViewBag.CurrentMinPrice = minPrice;
            ViewBag.CurrentMaxPrice = maxPrice;
            ViewBag.CurrentStartDate = startDate;
            ViewBag.CurrentEndDate = endDate;
            var result = await query.ToListAsync();
            return View(result);
        }

        [HttpGet]
        public IActionResult Time()
        {
            var model = new Iepan_Flaviu_Lab4.TimePredictionModel.ModelInput();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Time(Iepan_Flaviu_Lab4.TimePredictionModel.ModelInput input)
        {
            input.Payment_type = "CRD";
            ModelState.Remove("Payment_type");

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            try
            {
                // ATENTIE: Asigură-te că path-ul este corect pe mașina ta
                string modelPath = @"C:\HDD\Facultate\An 1\Semestru 1\Covaci\Laboratoare\Iepan_Flaviu_Lab4\TimePredictionModel.mlnet";
                MLContext mlContext = new MLContext();
                ITransformer mlModel = mlContext.Model.Load(modelPath, out var modelInputSchema);

                var predEngine = mlContext.Model.CreatePredictionEngine<Iepan_Flaviu_Lab4.TimePredictionModel.ModelInput, Iepan_Flaviu_Lab4.TimePredictionModel.ModelOutput>(mlModel);

                var result = predEngine.Predict(input);
                ViewBag.Duration = result.Score;
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Eroare ML: " + ex.Message);
            }

            return View(input);
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard(DateTime? fromDate, DateTime? toDate)
        {
            // Construim interogarea de bază
            var query = _context.PredictionHistories.AsQueryable();

            // Aplicăm filtrele dacă există
            if (fromDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt.Date >= fromDate.Value.Date);
            }
            if (toDate.HasValue)
            {
                query = query.Where(p => p.CreatedAt.Date <= toDate.Value.Date);
            }

            // ATENȚIE: Folosim variabila 'query' pentru toate calculele de mai jos, NU _context.PredictionHistories direct.

            // 1. Numărul total de predicții (filtrat)
            var totalPredictions = await query.CountAsync();

            // 2. Preț mediu per tip de plată (filtrat)
            var paymentTypeStats = await query
                .GroupBy(p => p.PaymentType)
                .Select(g => new PaymentTypeStat
                {
                    PaymentType = g.Key,
                    AveragePrice = g.Average(x => x.PredictedPrice),
                    Count = g.Count()
                })
                .ToListAsync();

            // 3. Distribuția prețurilor pe intervale (filtrat)
            var allPredictions = await query
                .Select(p => p.PredictedPrice)
                .ToListAsync();

            var buckets = new List<PriceBucketStat>
            {
                new PriceBucketStat { Label = "0 - 10" },
                new PriceBucketStat { Label = "10 - 20" },
                new PriceBucketStat { Label = "20 - 30" },
                new PriceBucketStat { Label = "30 - 50" },
                new PriceBucketStat { Label = "> 50" }
            };

            foreach (var price in allPredictions)
            {
                if (price < 10)
                    buckets[0].Count++;
                else if (price < 20)
                    buckets[1].Count++;
                else if (price < 30)
                    buckets[2].Count++;
                else if (price < 50)
                    buckets[3].Count++;
                else
                    buckets[4].Count++;
            }

            // 4. Construim ViewModel-ul
            var vm = new DashboardViewModel
            {
                TotalPredictions = totalPredictions,
                PaymentTypeStats = paymentTypeStats,
                PriceBuckets = buckets,
                FromDate = fromDate,
                ToDate = toDate
            };

            return View(vm);
        }
    }
}