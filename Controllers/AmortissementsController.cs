using GestionFournituresAPI.Data;
using GestionFournituresAPI.Dtos;
using GestionFournituresAPI.Services;
using Microsoft.AspNetCore.Mvc;
using AutoMapper;

namespace GestionFournituresAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AmortissementsController : ControllerBase
    {
        private readonly AmortissementService _amortissementService;
        private readonly IMapper _mapper;

        public AmortissementsController(AmortissementService amortissementService, IMapper mapper)
        {
            _amortissementService = amortissementService;
            _mapper = mapper;
        }

        // GET: api/Amortissements
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AmortissementDto>>> GetAmortissements()
        {
            var amortissements = await _amortissementService.GetAllAsync();
            return _mapper.Map<List<AmortissementDto>>(amortissements);
        }

        // GET: api/Amortissements/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AmortissementDto>> GetAmortissement(int id)
        {
            var amortissement = await _amortissementService.GetByIdAsync(id);
            if (amortissement == null)
            {
                return NotFound();
            }
            return _mapper.Map<AmortissementDto>(amortissement);
        }

        // GET: api/Amortissements/Bien/5
        [HttpGet("Bien/{idBien}")]
        public async Task<ActionResult<IEnumerable<AmortissementDto>>> GetAmortissementsByBien(int idBien)
        {
            var amortissements = await _amortissementService.GetByBienAsync(idBien);
            if (amortissements == null || !amortissements.Any())
            {
                return NotFound("Le bien spécifié n'existe pas.");
            }
            return _mapper.Map<List<AmortissementDto>>(amortissements);
        }

        // GET: api/Amortissements/Annee/2023
        [HttpGet("Annee/{annee}")]
        public async Task<ActionResult<IEnumerable<AmortissementDto>>> GetAmortissementsByAnnee(int annee)
        {
            var amortissements = await _amortissementService.GetByAnneeAsync(annee);
            return _mapper.Map<List<AmortissementDto>>(amortissements);
        }

        // DELETE: api/Amortissements/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAmortissement(int id)
        {
            var success = await _amortissementService.DeleteAsync(id);
            if (!success)
            {
                return NotFound();
            }
            return NoContent();
        }
    }
}