using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace Collectivite.Models
{
    public class DetailBonCommande : INotifyPropertyChanged
    {
        private int _quantite;
        private double _prixUnitaire;

        [Key]
        public int Id { get; set; }

        public int BonCommandeId { get; set; }

        public BonCommande BonCommande { get; set; } = null!;

        public string Designation { get; set; } = null!;

        public int Quantite
        {
            get => _quantite;
            set
            {
                if (_quantite != value)
                {
                    _quantite = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        public double PrixUnitaire
        {
            get => _prixUnitaire;
            set
            {
                if (Math.Abs(_prixUnitaire - value) > 0.01)
                {
                    _prixUnitaire = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(Total));
                }
            }
        }

        [NotMapped] // ✅ Important : cette propriété n'est pas dans la base de données
        public double Total => Quantite * PrixUnitaire;

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}