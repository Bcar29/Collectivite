using Collectivite.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public interface ICommuneService
    {
        
        /// <summary>
        /// Récupère une commune par son ID avec toutes ses relations
        /// </summary>
        Task<Commune?> GetCommuneByIdWithRelationsAsync(int id);

    }
}
