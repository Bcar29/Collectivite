using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Collectivite.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public string ActionTitle { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string PerformedBy { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; } = DateTime.Now;
        public string FormattedDate => PerformedAt.ToString("dd/MM/yyyy HH:mm");
        public string RelativeTime
        {
            get
            {
                var span = DateTime.Now - PerformedAt;
                if (span.TotalMinutes < 60)
                    return $"Il y a {(int)span.TotalMinutes} min";
                if (span.TotalHours < 24)
                    return $"Il y a {(int)span.TotalHours}h";
                if (span.TotalDays < 7)
                    return $"Il y a {(int)span.TotalDays}j";
                return FormattedDate;
            }
        }
    }
}
