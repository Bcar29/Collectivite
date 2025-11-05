using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Collectivite.Models;
using Microsoft.EntityFrameworkCore;

namespace Collectivite.Services
{
   public class NommenclatureService
    {
        private readonly AppDbContext _context;
        public NommenclatureService(AppDbContext context)
        {
            _context = context;
        }

        // recuperer tous les nommenclatures
        public async Task<List<Nommenclature>> GetAllNommenclatureAsync()
        {
            return await _context.Nommenclatures
                .OrderBy(n => n.Section)
                .ToListAsync();
        }

        // ajouter une nommenclature
        public async Task<(bool Success, string Message, Nommenclature? Nommenclature)> CreateNommenclatureAsync(Nommenclature nommenclature)
        {
            try
            {
                // Validation : Vérifier qu'il n'existe pas déjà une nommenclature avec le même intitulé
                var existe = await _context.Nommenclatures
                    .AnyAsync(n => n.Intitule == nommenclature.Intitule);
                if (existe)
                {
                    return (false, $"{nommenclature.Intitule} existe déjà.", null);
                }
                _context.Nommenclatures.Add(nommenclature);
                await _context.SaveChangesAsync();
                return (true, "Nommenclature créée avec succès.", nommenclature);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création de la nommenclature : {ex.Message}", null);
            }
        }

        // mettre à jour une nommenclature
        public async Task<(bool Success, string Message)> UpdateNommenclatureAsync(Nommenclature nommenclature)
        {
            try
            {
                var existingNommenclature = await _context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Id == nommenclature.Id);
                if (existingNommenclature == null)
                {
                    return (false, "Nommenclature non trouvée.");
                }
                // Validation : Vérifier qu'il n'existe pas déjà une nommenclature avec le même intitulé
                var existe = await _context.Nommenclatures
                    .AnyAsync(n => n.Intitule == nommenclature.Intitule && n.Id != nommenclature.Id);
                if (existe)
                {
                    return (false, $"{nommenclature.Intitule} existe déjà.");
                }
                existingNommenclature.Chapitre = nommenclature.Chapitre;
                existingNommenclature.Article = nommenclature.Article;
                existingNommenclature.Paragraphe = nommenclature.Paragraphe;
                existingNommenclature.SousParagraphe = nommenclature.SousParagraphe;
                existingNommenclature.Intitule = nommenclature.Intitule;
                existingNommenclature.Nature = nommenclature.Nature;
                existingNommenclature.Section = nommenclature.Section;
                existingNommenclature.ParentId = nommenclature.ParentId;
                await _context.SaveChangesAsync();
                return (true, "Nommenclature mise à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour de la nommenclature : {ex.Message}");
            }
        }

        // supprimer une nommenclature
        public async Task<(bool Success, string Message)> DeleteNommenclatureAsync(int nommenclatureId)
        {
            try
            {
                var existingNommenclature = await _context.Nommenclatures
                    .FirstOrDefaultAsync(n => n.Id == nommenclatureId);
                if (existingNommenclature == null)
                {
                    return (false, "Nommenclature non trouvée.");
                }
                _context.Nommenclatures.Remove(existingNommenclature);
                await _context.SaveChangesAsync();
                return (true, "Nommenclature supprimée avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression de la nommenclature : {ex.Message}");
            }
        }
    }
}
