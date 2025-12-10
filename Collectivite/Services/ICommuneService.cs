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
        Commune? GetCommuneById(int id);
       
    }
}
