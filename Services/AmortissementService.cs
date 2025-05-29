using GestionFournituresAPI.Data;
using GestionFournituresAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace GestionFournituresAPI.Services
{
    public class AmortissementService
    {
        private readonly ApplicationDbContext _context;

        public AmortissementService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Amortissement>> GetAllAsync()
        {
            return await _context.Amortissements
                .Include(a => a.Immobilisation)
                .ToListAsync();
        }

        public async Task<Amortissement?> GetByIdAsync(int id)
        {
            return await _context.Amortissements
                .Include(a => a.Immobilisation)
                .FirstOrDefaultAsync(a => a.IdAmortissement == id);
        }

        public async Task<IEnumerable<Amortissement>> GetByBienAsync(int idBien)
        {
            if (!await _context.Immobilisations.AnyAsync(i => i.IdBien == idBien))
            {
                return Enumerable.Empty<Amortissement>();
            }
            return await _context.Amortissements
                .Where(a => a.IdBien == idBien)
                .OrderBy(a => a.Annee)
                .ToListAsync();
        }

        public async Task<IEnumerable<Amortissement>> GetByAnneeAsync(int annee)
        {
            return await _context.Amortissements
                .Include(a => a.Immobilisation)
                .Where(a => a.Annee == annee)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var amortissement = await _context.Amortissements.FindAsync(id);
            if (amortissement == null)
            {
                return false;
            }
            _context.Amortissements.Remove(amortissement);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}