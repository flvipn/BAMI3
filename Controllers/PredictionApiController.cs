using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using Iepan_Flaviu_Lab4.Data;
using Iepan_Flaviu_Lab4.Models.History;

namespace Iepan_Flaviu_Lab4.Controllers
{
    // api/PredictionApi
    [Route("api/[controller]")]
    [ApiController]
    public class PredictionApiController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PredictionApiController(AppDbContext context)
        {
            _context = context;
        }


        // GET: api/PredictionApi
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PredictionHistory>>> GetPredictionHistory()
        {

            return await _context.PredictionHistories.ToListAsync();
        }

        // DELETE: api/PredictionApi/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePrediction(int id)
        {

            var prediction = await _context.PredictionHistories.FindAsync(id);

            if (prediction == null)
            {
                return NotFound();
            }

            _context.PredictionHistories.Remove(prediction);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}