using Collectivite.Models;
using MaterialDesignThemes.Wpf;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public class ExerciceService
    {
        private readonly AppDbContext _context;
        public ExerciceService(AppDbContext context)
        {
            _context = context;
        }
        // Recuperer tous les exercices
        public async Task<List<Exercice>> GetAllExerciceAsync()
            {
            return await _context.Exercices
                
                .OrderByDescending(e => e.DateDebut)
                .ToListAsync();
        }
        



        // Recupler tous les exercices de la commune
        //public async Task<List<Exercice>> GetExercicesByCommuneIdAsync(int communeId)
        //{
        //    return await _context.Exercices
        //        .Include(e => e.Commune)
        //        .Where(e => e.IdCommune == communeId)
        //        .OrderByDescending(e => e.DateDebut)
        //        .ToListAsync();
        //}

        // Récupérer un exercice par son ID
        public async Task<Exercice?> GetExerciceByIdAsync(int exerciceId)
        {
            return await _context.Exercices
                .FirstOrDefaultAsync(e => e.Id == exerciceId);
        }

        // recuperer le dernier details commune qui n'est pas lie à un exercice
        public async Task<DetailCommune?> LastDetailCommune()
        {
            return await _context.DetailCommunes
                .Where(e => e.Exercice == null)
                .OrderByDescending(e => e.Id)
                .FirstOrDefaultAsync();
        }


        // Ajouter un nouvel exercice
        public async Task<(bool Success, string Message, Exercice? Exercice)> CreateAsync(Exercice exercice)
        {
            try
            {
                // Validation : Vérifier qu'il n'existe pas déjà un exercice pour cette année
                var existe = await _context.Exercices
                    .AnyAsync(e => e.Libelle == exercice.Libelle);

                if (existe)
                {
                    return (false, $"{exercice.Libelle} existe déjà .", null);
                }

                // Validation : Vérifier qu'il n'existe pas déjà un exercice non clôturé pour cette commune
                var notClosedExercice = await _context.Exercices
                    .AnyAsync(e => e.EstCloture == false);
                if (notClosedExercice)
                {
                    return (false, "Il existe déjà un exercice non clôturé pour cette commune.", null);
                }

                // Validation : Date de fin après date de début
                if (exercice.DateFin <= exercice.DateDebut)
                {
                    return (false, "La date de fin doit être après la date de début.", null);
                }

                // validation de la liaison avec details de la commune
                if (exercice.DetailCommune == null)
                {
                    DetailCommune? dt = await LastDetailCommune();
                    if (dt != null)
                    {
                        exercice.IdDetailCommune = dt.Id;
                    }
                    else
                    {
                    return (false, "La liaison au details de la commune .", null);
                    }
                    
                }

                _context.Exercices.Add(exercice);
                await _context.SaveChangesAsync();

                return (true, "Exercice créé avec succès.", exercice);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la création : {ex.Message}", null);
            }
        }

        //Mettre à jour un exercice
        public async Task<(bool Success, string Message)> UpdateAsync(Exercice exercice)
        {
            try
            {
                // verifier q'un autre exercice n'existe pas avec le meme libelle
                var existe = await _context.Exercices
                    .AnyAsync(e => e.Libelle == exercice.Libelle  && e.Id != exercice.Id);
                if (existe)
                {
                    return (false, $"{exercice.Libelle} existe déjà.");
                }

                var existingExercice = await _context.Exercices.FindAsync(exercice.Id);
                if (existingExercice == null)
                {
                    return (false, "Exercice non trouvé.");
                }
                // Validation : Date de fin après date de début
                if (exercice.DateFin <= exercice.DateDebut)
                {
                    return (false, "La date de fin doit être après la date de début.");
                }
                existingExercice.Libelle = exercice.Libelle;
                existingExercice.DateDebut = exercice.DateDebut;
                existingExercice.DateFin = exercice.DateFin;
                existingExercice.EstCloture = exercice.EstCloture;
                await _context.SaveChangesAsync();
                return (true, "Exercice mis à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour : {ex.Message}");
            }
        }

        // Supprimer un exercice
        public async Task<(bool Success, string Message)> DeleteAsync(int exerciceId)
        {
            try
            {
                var exercice = await _context.Exercices.FindAsync(exerciceId);
                if (exercice == null)
                {
                    return (false, "Exercice non trouvé.");
                }
                // Vérifier les dépendances (par exemple, BudgetsPrimitifs liés)
                var hasDependencies = await _context.BudgetsPrimitifs
                    .AnyAsync(bp => bp.ExerciceId == exerciceId);
                if (hasDependencies)
                {
                    return (false, "Impossible de supprimer l'exercice car il a des dépendances.");
                }
                _context.Exercices.Remove(exercice);
                await _context.SaveChangesAsync();
                return (true, "Exercice supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> CloturerAsync(int id)
        {
            try
            {
                var exercice = await _context.Exercices.FindAsync(id);

                if (exercice == null)
                {
                    return (false, "Exercice introuvable.");
                }

                if (exercice.EstCloture)
                {
                    return (false, "Cet exercice est déjà clôturé.");
                }

                exercice.EstCloture = true;
                await _context.SaveChangesAsync();

                return (true, $"L'exercice '{exercice.Libelle}' a été clôturé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la clôture : {ex.Message}");
            }
        }
    }
}
