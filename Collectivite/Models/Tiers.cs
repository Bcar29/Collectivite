using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Collectivite.Models
{
    /// <summary>
    /// Type de tiers
    /// </summary>
    public enum TiersType
    {
        [Display(Name = "Contribuable")]
        Contribuable,

        [Display(Name = "Fournisseur")]
        Fournisseur,

        [Display(Name = "Salarié")]
        Salarie
    }

    /// <summary>
    /// Catégorie juridique
    /// </summary>
    public enum CategorieJuridique
    {
        [Display(Name = "Personne Physique")]
        PersonnePhysique,

        [Display(Name = "Personne Morale")]
        PersonneMorale
    }

    public class Tiers : INotifyPropertyChanged
    {
        private TiersType _type;
        private CategorieJuridique _categorie;
        private string _email = null!;
        private string? _telephone;
        private string? _adresse;
        private bool _isActif = true;
        private string? _nom;
        private string? _prenom;
        private string? _numeroPieceIdentite;
        private string? _typePieceIdentite;
        private string? _raisonSociale;
        private string? _rccm;
        private string? _nif;
        private string? _numeroTva;
        private string? _secteurActivite;

        [Key]
        public int Id { get; set; }

        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS COMMUNES À TOUS LES TYPES
        // ═══════════════════════════════════════════════════════════

        [Required(ErrorMessage = "Le type de tiers est obligatoire")]
        public TiersType Type
        {
            get => _type;
            set
            {
                if (_type != value)
                {
                    _type = value;
                    OnPropertyChanged();
                }
            }
        }

        [Required(ErrorMessage = "La catégorie juridique est obligatoire")]
        public CategorieJuridique Categorie
        {
            get => _categorie;
            set
            {
                if (_categorie != value)
                {
                    _categorie = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NomComplet));
                    OnPropertyChanged(nameof(CategorieDisplay));
                }
            }
        }

        [Required(ErrorMessage = "L'adresse email est obligatoire")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        [MaxLength(255)]
        public string Email
        {
            get => _email;
            set
            {
                if (_email != value)
                {
                    _email = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(20)]
        public string? Telephone
        {
            get => _telephone;
            set
            {
                if (_telephone != value)
                {
                    _telephone = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(500)]
        public string? Adresse
        {
            get => _adresse;
            set
            {
                if (_adresse != value)
                {
                    _adresse = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool IsActif
        {
            get => _isActif;
            set
            {
                if (_isActif != value)
                {
                    _isActif = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime DateCreation { get; set; } = DateTime.Now;

        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS SPÉCIFIQUES - PERSONNE PHYSIQUE
        // ═══════════════════════════════════════════════════════════

        [MaxLength(100)]
        public string? Nom
        {
            get => _nom;
            set
            {
                if (_nom != value)
                {
                    _nom = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NomComplet));
                }
            }
        }

        [MaxLength(100)]
        public string? Prenom
        {
            get => _prenom;
            set
            {
                if (_prenom != value)
                {
                    _prenom = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NomComplet));
                }
            }
        }

        [MaxLength(50)]
        public string? NumeroPieceIdentite
        {
            get => _numeroPieceIdentite;
            set
            {
                if (_numeroPieceIdentite != value)
                {
                    _numeroPieceIdentite = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(50)]
        public string? TypePieceIdentite
        {
            get => _typePieceIdentite;
            set
            {
                if (_typePieceIdentite != value)
                {
                    _typePieceIdentite = value;
                    OnPropertyChanged();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // INFORMATIONS SPÉCIFIQUES - PERSONNE MORALE
        // ═══════════════════════════════════════════════════════════

        [MaxLength(255)]
        public string? RaisonSociale
        {
            get => _raisonSociale;
            set
            {
                if (_raisonSociale != value)
                {
                    _raisonSociale = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(NomComplet));
                }
            }
        }

        [MaxLength(50)]
        public string? Rccm
        {
            get => _rccm;
            set
            {
                if (_rccm != value)
                {
                    _rccm = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(50)]
        public string? Nif
        {
            get => _nif;
            set
            {
                if (_nif != value)
                {
                    _nif = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(50)]
        public string? NumeroTva
        {
            get => _numeroTva;
            set
            {
                if (_numeroTva != value)
                {
                    _numeroTva = value;
                    OnPropertyChanged();
                }
            }
        }

        [MaxLength(200)]
        public string? SecteurActivite
        {
            get => _secteurActivite;
            set
            {
                if (_secteurActivite != value)
                {
                    _secteurActivite = value;
                    OnPropertyChanged();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        // RELATIONS
        // ═══════════════════════════════════════════════════════════

        public ICollection<DocumentTiers>? Documents { get; set; }
        public ICollection<CompteBancaire>? CompteBancaires { get; set; }
        public ICollection<Contrats>? Contrats { get; set; }
        public ICollection<Engagement>? Engagements { get; set; }
        public ICollection<Facture>? Factures { get; set; }
        public ICollection<Recensement>? Recensements { get; set; }

        // ═══════════════════════════════════════════════════════════
        // PROPRIÉTÉS CALCULÉES
        // ═══════════════════════════════════════════════════════════

        [NotMapped]
        public string NomComplet => Categorie == CategorieJuridique.PersonnePhysique
            ? $"{Nom} {Prenom}".Trim()
            : RaisonSociale ?? "N/A";

        [NotMapped]
        public string TypeDisplay => Type switch
        {
            TiersType.Contribuable => "Contribuable",
            TiersType.Fournisseur => "Fournisseur",
            TiersType.Salarie => "Salarié",
            _ => "Inconnu"
        };

        [NotMapped]
        public string CategorieDisplay => Categorie switch
        {
            CategorieJuridique.PersonnePhysique => "Personne Physique",
            CategorieJuridique.PersonneMorale => "Personne Morale",
            _ => "Inconnu"
        };

        // ═══════════════════════════════════════════════════════════
        // INotifyPropertyChanged
        // ═══════════════════════════════════════════════════════════

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}