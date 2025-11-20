using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;

namespace Collectivite.Services
{
    public class BonCommandeDetailService
    {
        private readonly AppDbContext _appDbContext;
        public BonCommandeDetailService(AppDbContext appDbContext)
        {
            _appDbContext = appDbContext;
        }

        // recuperer les bon de commandes avec leurs details
        public async Task<List<BonCommande>> GetBonCommandeWithDetailsAsync()
        {
            return await _appDbContext.BonCommandes
                .Include(bc => bc.Details)
                .ToListAsync();
        }

        // ajouter un bon de commande avec ses details
        public async Task<(bool Succes, string Message, BonCommande? BonCommande, DetailBonCommande? Detailbon)> CreateBonCommandeWithDetailsAsync(BonCommande bonCommande)
        {
            try
            {
                _appDbContext.BonCommandes.Add(bonCommande);
                await _appDbContext.SaveChangesAsync();
                return (true, "Bon de commande ajouté avec succès", bonCommande, null);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création du bon de commande: {ex.Message}", null, null);
            }
        }

    }
}
