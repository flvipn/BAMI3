using static Iepan_Flaviu_Lab4.PricePredictionModel;
public class BatchPredictionViewModel
{
    public IFormFile? File { get; set; }
    // Rezultatele (predicțiile) pentru fiecare rând din fișier
    public List<ModelOutput>? Predictions { get; set; }
    public string? ErrorMessage { get; set; }
}