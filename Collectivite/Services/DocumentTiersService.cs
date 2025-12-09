using Collectivite.Models;
using Collectivite.Utils;
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
        // Dossier racine pour stocker les documents
        private readonly string _documentsBasePath;

        // Extensions de fichiers autorisées
        private readonly string[] _extensionsAutorisees = { ".pdf", ".jpg", ".jpeg", ".png", ".doc", ".docx" };

        // Taille maximale d'un fichier (10 Mo)
        private const long TailleMaxFichier = 10 * 1024 * 1024;

        public DocumentTiersService()
        {
            // Définir le chemin de base pour les documents
            _documentsBasePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Collectivite",
                "Documents",
                "Tiers"
            );

            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(_documentsBasePath))
            {
                Directory.CreateDirectory(_documentsBasePath);
            }
        }

        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        #region Récupération des données

        /// <summary>
        /// Récupère tous les documents d'un tiers
        /// </summary>
        public async Task<List<DocumentTiers>> GetDocumentsByTiersAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour consulter les documents.");

            using var context = CreateContext();

            return await context.DocumentTiers
                .Where(d => d.TiersId == tiersId)
                .AsNoTracking()
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère un document par son ID
        /// </summary>
        public async Task<DocumentTiers?> GetDocumentByIdAsync(int id)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour consulter ce document.");

            using var context = CreateContext();

            return await context.DocumentTiers
                .AsNoTracking()
                .FirstOrDefaultAsync(d => d.Id == id);
        }

        /// <summary>
        /// Récupère les documents par type
        /// </summary>
        public async Task<List<DocumentTiers>> GetDocumentsByTypeAsync(int tiersId, TypeDocument type)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour consulter les documents.");

            using var context = CreateContext();

            return await context.DocumentTiers
                .Where(d => d.TiersId == tiersId && d.Type == type)
                .AsNoTracking()
                .OrderByDescending(d => d.DateAjout)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les documents expirés
        /// </summary>
        public async Task<List<DocumentTiers>> GetDocumentsExpiresAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour consulter les documents expirés.");

            using var context = CreateContext();

            return await context.DocumentTiers
                .Where(d => d.TiersId == tiersId &&
                           d.DateExpiration.HasValue &&
                           d.DateExpiration.Value < DateTime.Now)
                .AsNoTracking()
                .OrderBy(d => d.DateExpiration)
                .ToListAsync();
        }

        /// <summary>
        /// Récupère les documents qui expirent bientôt (dans 30 jours)
        /// </summary>
        public async Task<List<DocumentTiers>> GetDocumentsExpireBientotAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour consulter les documents proches de l'expiration.");

            using var context = CreateContext();

            var dateLimit = DateTime.Now.AddDays(30);

            return await context.DocumentTiers
                .Where(d => d.TiersId == tiersId &&
                           d.DateExpiration.HasValue &&
                           d.DateExpiration.Value > DateTime.Now &&
                           d.DateExpiration.Value <= dateLimit)
                .AsNoTracking()
                .OrderBy(d => d.DateExpiration)
                .ToListAsync();
        }

        /// <summary>
        /// Vérifie si un document existe déjà pour ce tiers et ce type
        /// </summary>
        public async Task<bool> DocumentExistsAsync(int tiersId, TypeDocument type)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour vérifier l'existence d'un document.");

            using var context = CreateContext();

            return await context.DocumentTiers
                .AnyAsync(d => d.TiersId == tiersId && d.Type == type);
        }

        #endregion

        #region Ajout de document

        /// <summary>
        /// Ouvre une boîte de dialogue pour sélectionner un fichier et l'ajouter
        /// </summary>
        public async Task<(bool Success, string Message, DocumentTiers? Document)> AddDocumentAsync(
            int tiersId,
            TypeDocument type)
        {
            try
            {
                if (!SessionManager.HasPermission("DocumentTiers.Create"))
                    return (false, "Permission DocumentTiers.Create requise pour ajouter un document.", null);
                // Ouvrir la boîte de dialogue de sélection de fichier
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner un document",
                    Filter = "Tous les fichiers autorisés|*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx|" +
                             "PDF (*.pdf)|*.pdf|" +
                             "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|" +
                             "Word (*.doc;*.docx)|*.doc;*.docx",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return (false, "Aucun fichier sélectionné.", null);
                }

                var fichierSource = openFileDialog.FileName;

                // Valider le fichier
                var validationResult = ValidateFichier(fichierSource);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.Message, null);
                }

                // Créer le document
                return await AddDocumentFromFileAsync(tiersId, type, fichierSource);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'ajout du document : {ex.Message}", null);
            }
        }

        /// <summary>
        /// Ajoute un document à partir d'un fichier existant
        /// </summary>
        public async Task<(bool Success, string Message, DocumentTiers? Document)> AddDocumentFromFileAsync(
            int tiersId,
            TypeDocument type,
            string fichierSource,
            string? numeroDocument = null,
            DateTime? dateExpiration = null,
            DateTime? dateEmission = null,
            string? description = null)
        {
            if (!SessionManager.HasPermission("DocumentTiers.Create"))
                return (false, "Permission DocumentTiers.Create requise pour ajouter un document.", null);

            using var context = CreateContext();

            try
            {
                // Vérifier que le tiers existe
                var tiers = await context.Tiers.FindAsync(tiersId);
                if (tiers == null)
                {
                    return (false, "Tiers introuvable.", null);
                }

                // Valider le fichier
                var validationResult = ValidateFichier(fichierSource);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.Message, null);
                }

                // Créer le dossier du tiers s'il n'existe pas
                var tiersDossier = Path.Combine(_documentsBasePath, $"Tiers_{tiersId}");
                if (!Directory.Exists(tiersDossier))
                {
                    Directory.CreateDirectory(tiersDossier);
                }

                // Générer un nom de fichier unique
                var extension = Path.GetExtension(fichierSource);
                var nomFichier = $"{type}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var cheminDestination = Path.Combine(tiersDossier, nomFichier);

                // Copier le fichier
                File.Copy(fichierSource, cheminDestination, true);

                // Obtenir la taille du fichier
                var fileInfo = new FileInfo(cheminDestination);

                // Créer l'entité DocumentTiers
                var document = new DocumentTiers
                {
                    TiersId = tiersId,
                    Type = type,
                    NumeroDocument = numeroDocument,
                    NomFichier = nomFichier,
                    CheminFichier = cheminDestination,
                    Extension = extension,
                    TailleFichier = fileInfo.Length,
                    DateAjout = DateTime.Now,
                    DateExpiration = dateExpiration,
                    DateEmission = dateEmission,
                    Description = description,
                    IsValide = true
                };

                context.DocumentTiers.Add(document);
                await context.SaveChangesAsync();

                return (true, $"Document '{type}' ajouté avec succès.", document);
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de l'ajout du document : {ex.Message}", null);
            }
        }

        #endregion

        #region Validation

        /// <summary>
        /// Valide un fichier avant de l'ajouter
        /// </summary>
        private (bool IsValid, string Message) ValidateFichier(string cheminFichier)
        {
            // Vérifier que le fichier existe
            if (!File.Exists(cheminFichier))
            {
                return (false, "Le fichier n'existe pas.");
            }

            // Vérifier l'extension
            var extension = Path.GetExtension(cheminFichier).ToLower();
            if (!_extensionsAutorisees.Contains(extension))
            {
                return (false, $"Extension de fichier non autorisée. Extensions autorisées : {string.Join(", ", _extensionsAutorisees)}");
            }

            // Vérifier la taille
            var fileInfo = new FileInfo(cheminFichier);
            if (fileInfo.Length > TailleMaxFichier)
            {
                return (false, $"Le fichier est trop volumineux. Taille maximale : {TailleMaxFichier / (1024 * 1024)} Mo");
            }

            return (true, "Fichier valide");
        }

        #endregion

        #region Modification

        /// <summary>
        /// Met à jour les informations d'un document (sans changer le fichier)
        /// </summary>
        public async Task<(bool Success, string Message)> UpdateDocumentInfoAsync(
            int documentId,
            string? numeroDocument = null,
            DateTime? dateExpiration = null,
            DateTime? dateEmission = null,
            string? description = null)
        {
            if (!SessionManager.HasPermission("DocumentTiers.Edit"))
                return (false, "Permission DocumentTiers.Edit requise pour modifier un document.");

            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                {
                    return (false, "Document introuvable.");
                }

                // Mettre à jour les informations
                if (numeroDocument != null)
                    document.NumeroDocument = numeroDocument;

                if (dateExpiration.HasValue)
                    document.DateExpiration = dateExpiration;

                if (dateEmission.HasValue)
                    document.DateEmission = dateEmission;

                if (description != null)
                    document.Description = description;

                await context.SaveChangesAsync();

                return (true, "Informations du document mises à jour avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la mise à jour : {ex.Message}");
            }
        }

        /// <summary>
        /// Remplace un document existant par un nouveau fichier
        /// </summary>
        public async Task<(bool Success, string Message)> ReplaceDocumentAsync(int documentId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.Edit"))
                return (false, "Permission DocumentTiers.Edit requise pour remplacer un document.");

            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                {
                    return (false, "Document introuvable.");
                }

                // Ouvrir la boîte de dialogue
                var openFileDialog = new OpenFileDialog
                {
                    Title = "Sélectionner le nouveau document",
                    Filter = "Tous les fichiers autorisés|*.pdf;*.jpg;*.jpeg;*.png;*.doc;*.docx|" +
                             "PDF (*.pdf)|*.pdf|" +
                             "Images (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|" +
                             "Word (*.doc;*.docx)|*.doc;*.docx",
                    Multiselect = false
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return (false, "Aucun fichier sélectionné.");
                }

                var nouveauFichier = openFileDialog.FileName;

                // Valider le nouveau fichier
                var validationResult = ValidateFichier(nouveauFichier);
                if (!validationResult.IsValid)
                {
                    return (false, validationResult.Message);
                }

                // Supprimer l'ancien fichier
                if (File.Exists(document.CheminFichier))
                {
                    File.Delete(document.CheminFichier);
                }

                // Copier le nouveau fichier
                var extension = Path.GetExtension(nouveauFichier);
                var nomFichier = $"{document.Type}_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
                var tiersDossier = Path.GetDirectoryName(document.CheminFichier);
                var cheminDestination = Path.Combine(tiersDossier!, nomFichier);

                File.Copy(nouveauFichier, cheminDestination, true);

                // Mettre à jour l'entité
                var fileInfo = new FileInfo(cheminDestination);
                document.NomFichier = nomFichier;
                document.CheminFichier = cheminDestination;
                document.Extension = extension;
                document.TailleFichier = fileInfo.Length;
                document.DateAjout = DateTime.Now;

                await context.SaveChangesAsync();

                return (true, "Document remplacé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors du remplacement : {ex.Message}");
            }
        }

        /// <summary>
        /// Active ou désactive la validité d'un document
        /// </summary>
        public async Task<(bool Success, string Message)> ToggleValiditeAsync(int documentId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.Edit"))
                return (false, "Permission DocumentTiers.Edit requise pour modifier la validité d'un document.");

            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                {
                    return (false, "Document introuvable.");
                }

                document.IsValide = !document.IsValide;
                await context.SaveChangesAsync();

                var status = document.IsValide ? "validé" : "invalidé";
                return (true, $"Document {status} avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur : {ex.Message}");
            }
        }

        #endregion

        #region Suppression

        /// <summary>
        /// Supprime un document (base de données + fichier physique)
        /// </summary>
        public async Task<(bool Success, string Message)> DeleteDocumentAsync(int documentId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.Delete"))
                return (false, "Permission DocumentTiers.Delete requise pour supprimer un document.");

            using var context = CreateContext();

            try
            {
                var document = await context.DocumentTiers.FindAsync(documentId);

                if (document == null)
                {
                    return (false, "Document introuvable.");
                }

                // Supprimer le fichier physique
                if (File.Exists(document.CheminFichier))
                {
                    try
                    {
                        File.Delete(document.CheminFichier);
                    }
                    catch (Exception ex)
                    {
                        // Log l'erreur mais continue la suppression en base
                        System.Diagnostics.Debug.WriteLine($"Erreur suppression fichier : {ex.Message}");
                    }
                }

                // Supprimer de la base de données
                context.DocumentTiers.Remove(document);
                await context.SaveChangesAsync();

                return (true, $"Document '{document.TypeDisplay}' supprimé avec succès.");
            }
            catch (Exception ex)
            {
                return (false, $"Erreur lors de la suppression : {ex.Message}");
            }
        }

        #endregion

        #region Ouverture de document

        /// <summary>
        /// Ouvre un document avec l'application par défaut
        /// </summary>
        public (bool Success, string Message) OpenDocument(DocumentTiers document)
        {
            try
            {
                if (!SessionManager.HasPermission("DocumentTiers.View"))
                    return (false, "Permission DocumentTiers.View requise pour ouvrir le document.");
                if (document == null)
                {
                    return (false, "Document null.");
                }

                if (!File.Exists(document.CheminFichier))
                {
                    return (false, "Le fichier n'existe plus sur le disque.");
                }

                // Ouvrir le fichier avec l'application par défaut
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

        #region Documents obligatoires

        /// <summary>
        /// Retourne la liste des types de documents obligatoires selon le type de tiers
        /// </summary>
        public List<TypeDocument> GetDocumentsObligatoires(Tiers tiers)
        {
            var documentsObligatoires = new List<TypeDocument>();

            // Documents communs pour Personne Physique (Contribuable, Salarié, Fournisseur PP)
            if (tiers.Categorie == CategorieJuridique.PersonnePhysique)
            {
                documentsObligatoires.Add(TypeDocument.CarteIdentite);
                // ou Passeport comme alternative
            }

            // Documents spécifiques pour Personne Morale (Fournisseur PM)
            if (tiers.Categorie == CategorieJuridique.PersonneMorale)
            {
                documentsObligatoires.Add(TypeDocument.RCCM);
                documentsObligatoires.Add(TypeDocument.NIF);
                documentsObligatoires.Add(TypeDocument.QuitusFiscal);
                documentsObligatoires.Add(TypeDocument.AttestationTVA);
            }

            // Document spécifique pour Salarié
            if (tiers.Type == TiersType.Salarie)
            {
                documentsObligatoires.Add(TypeDocument.ContratTravail);
            }

            return documentsObligatoires;
        }

        /// <summary>
        /// Vérifie si tous les documents obligatoires sont présents
        /// </summary>
        public async Task<(bool AllPresent, List<TypeDocument> MissingDocuments)> CheckDocumentsObligatoiresAsync(int tiersId)
        {
            if (!SessionManager.HasPermission("DocumentTiers.View"))
                throw new UnauthorizedAccessException("Permission DocumentTiers.View requise pour vérifier les documents obligatoires.");

            using var context = CreateContext();

            try
            {
                var tiers = await context.Tiers
                    .Include(t => t.Documents)
                    .FirstOrDefaultAsync(t => t.Id == tiersId);

                if (tiers == null)
                {
                    return (false, new List<TypeDocument>());
                }

                var documentsObligatoires = GetDocumentsObligatoires(tiers);
                var documentsPresents = tiers.Documents?.Select(d => d.Type).ToList() ?? new List<TypeDocument>();

                var missingDocuments = documentsObligatoires
                    .Where(type => !documentsPresents.Contains(type))
                    .ToList();

                return (missingDocuments.Count == 0, missingDocuments);
            }
            catch (Exception)
            {
                return (false, new List<TypeDocument>());
            }
        }

        #endregion
    }
}