using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class CommuneInfoService
    {
        private readonly IDbContextFactory<AppDbContext> _contextFactory;

        public CommuneInfoService(IDbContextFactory<AppDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
        }

        private AppDbContext CreateContext() => _contextFactory.CreateDbContext();

        /// <summary>
        /// Récupère une commune selon son Id
        /// </summary>
        public async Task<Commune?> GetCommuneByIdAsync(int id)
        {
            using var context = CreateContext();
            return await context.Communes
                .Include(c => c.DetailCommunes)
                .Include(c => c.Users)
                .Include(c => c.Engagements)
                .Include(c => c.Recensements)
                .FirstOrDefaultAsync(c => c.Id == id);
        }
    }
}
