using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    /// <summary>
    /// Service pour la gestion des documents des tiers
    /// </summary>
    public class DocumentTiersService
    {
        private readonly string _documentsPath;

        public DocumentTiersService()
        {
            // Dossier de stockage des documents
            _documentsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Documents", "Tiers");

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(_documentsPath))
            {
                Directory.CreateDirectory(_documentsPath);
            }
        }

        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération

        /// <summary>
        /// Récupère tous les documents d'un tiers
        /// </summary>
        public async Task<List<DocumentTiers>> GetDocumentsByTiersAsync(int tiersId)
        {
            using var context = CreateContext();

            return await context.DocumentTiers
                .Where(d => d.TiersId == tiersId)
                .AsNoTracking()
                .OrderBy(d => d.Type)
                .ThenBy(d => d.DateAjout)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un document par son ID
        /// </summary>
        public async Task<DocumentTiers?> GetDocumentByIdAsync(int id)
        {
            using var context = CreateContext();

            return await context.DocumentTiers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        #endregion

        #region Ajout de documents

        /// <summary>
        /// Ajoute un nouveau document avec upload du fichier
        /// </summary>
        public async Task<(bool Success, string Message, DocumentTiers? Document)> AddDocumentAsync(
            int tiersId,
            TypeDocument type,
            string? description = null,
            DateTime? dateExpiration = null)
        {
            try
            {
                // Ouvrir le dialog de sélection de fichier
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner un document",
                    Filter = "Tous les fichiers (*.pdf;*.jpg;*.jpeg;*.png)|*.pdf;*.jpg;*.jpeg;*.png|" +
                            "Documents PDF (*.pdf)|*.pdf|" +
                            "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return (false, "Aucun fichier sélectionné.", null);
                }

                var sourceFilePath = openFileDialog.FileName;
                var fileInfo = new FileInfo(sourceFilePath);

                // Vérifier la taille du fichier (max 10 MB)
                if (fileInfo.Length > 10 * 1024 * 1024)
                {
                    return (false, "Le fichier est trop volumineux (maximum 10 MB).", null);
                }

                // Générer un nom de fichier unique
                var fileName = $"{tiersId}_{type}_{DateTime.Now:yyyyMMdd_HHmmss}{fileInfo.Extension}";
                var destinationPath = Path.Combine(_documentsPath, fileName);

                // Copier le fichier
                File.Copy(sourceFilePath, destinationPath, true);

                // Créer l'entité document
                using var context = CreateContext();

                var document = new DocumentTiers
                {
                    TiersId = tiersId,
                    Type = type,
                    NomFichier = fileInfo.Name,
                    CheminFichier = destinationPath,
                    Extension = fileInfo.Extension,
                    TailleFichier = fileInfo.Length,
                    DateAjout = DateTime.Now,
                    Description = description,
                    DateExpiration = dateExpiration,
                    IsValide = true
                };

                context.DocumentTiers.Add(document);
                await context.SaveChangesAsync();

                return (true, "Document ajouté avec succès.", document);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'ajout du document : {ex.Message}", null);
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un document (fichier et entrée DB)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteDocumentAsync(int documentId)
        {
            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                    return (false, "Document introuvable.");

                // Supprimer le fichier physique
                if (File.Exists(document.CheminFichier))
                {
                    try
                    {
                        File.Delete(document.CheminFichier);
                    }
                    catch
                    {
                        // Continuer même si la suppression du fichier échoue
                    }
                }

                // Supprimer l'entrée en base
                context.DocumentTiers.Remove(document);
                await context.SaveChangesAsync();

                return (true, "Document supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Ouverture de documents

        /// <summary>
        /// Ouvre un document dans l'application par défaut
        /// </summary>
        public (bool Success, string Message) OpenDocument(DocumentTiers document)
        {
            try
            {
                if (!File.Exists(document.CheminFichier))
                {
                    return (false, "Le fichier n'existe pas sur le disque.");
                }

                var processStartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = document.CheminFichier,
                    UseShellExecute = true
                };

                System.Diagnostics.Process.Start(processStartInfo);

                return (true, "Document ouvert.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'ouverture : {ex.Message}");
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Marque un document comme valide ou invalide
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleValiditeAsync(int documentId)
        {
            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                    return (false, "Document introuvable.");

                document.IsValide = !document.IsValide;
                await context.SaveChangesAsync();

                var status = document.IsValide ? "valide" : "invalide";
                return (true, $"Document marqué comme {status}.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion
    }
}