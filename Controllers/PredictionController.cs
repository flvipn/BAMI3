using Iepan_Flaviu_Lab4.Data;
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

        // 2. Creezi constructorul (FĂRĂ ASTA AI EROAREA CU _context)
        public PredictionController(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Price(ModelInput input)
        {
            // Load the model
            MLContext mlContext = new MLContext();
            // Create predection engine related to the loaded train model
            ITransformer mlModel =
           mlContext.Model.Load(@"C:\HDD\Facultate\An 1\Semestru 1\Covaci\Laboratoare\Iepan_Flaviu_Lab4\PricePredictionModel.mlnet", out var modelInputSchema);
            var predEngine = mlContext.Model.CreatePredictionEngine<ModelInput,
           ModelOutput>(mlModel);
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
        public async Task<IActionResult> History()
        {
            var history = await _context.PredictionHistories
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
            return View(history);
        }

        [HttpGet]
        public IActionResult Time()
        {
            // Folosim numele complet pentru a evita confuzia cu PricePredictionModel
            var model = new Iepan_Flaviu_Lab4.TimePredictionModel.ModelInput();
            return View(model);
        }

        // 2. ACESTA ESTE POST - Când apeși pe butonul de calcul
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Time(Iepan_Flaviu_Lab4.TimePredictionModel.ModelInput input)
        {
            // 1. Setăm manual valorile lipsă pentru ca motorul ML să nu primească null
            input.Payment_type = "CRD";
            // input.Fare_amount = 0; // Adaugă și asta dacă primești eroare și pentru fare_amount

            // 2. Eliminăm erorile de validare pentru câmpurile pe care le-am completat manual
            ModelState.Remove("Payment_type");
            // ModelState.Remove("Fare_amount");

            // 3. Acum verificăm validarea
            if (!ModelState.IsValid)
            {
                return View(input);
            }

            try
            {
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
    }
}

