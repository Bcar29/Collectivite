using Collectivite.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Collectivite.Services
{
    public class AuditService
    {
        private static AuditService? _instance;
        public static AuditService Instance => _instance ??= new AuditService();

        public AuditService() { }

        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        /// <summary>
        /// Récupère tous les logs d’audit
        /// </summary>
        public async Task<List<AuditLog>> GetAllLogsAsync()
        {
            using var context = CreateContext();

            return await context.AuditLogs
                .OrderByDescending(a => a.PerformedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        /// <summary>
        /// Ajoute un log d’audit
        /// </summary>
        public async Task LogAsync(string title, string description, string username)
        {
            using var context = CreateContext();

            var log = new AuditLog
            {
                ActionTitle = title,
                Description = description,
                PerformedBy = username,
                PerformedAt = DateTime.Now
            };

            context.AuditLogs.Add(log);
            await context.SaveChangesAsync();
        }
    }
}
