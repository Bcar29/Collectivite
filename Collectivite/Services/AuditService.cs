using Collectivite.Models;
using DocumentFormat.OpenXml.InkML;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Collectivite.Services
{
    public class AuditService
    {
        private AppDbContext CreateContext()
        {
            return new AppDbContext();
        }

        // recuperer tous les logs
        public async Task<List<AuditLog>> GetAllLogsAsync()
        {
            using var context = CreateContext();
            return await context.AuditLogs
                .OrderByDescending(a => a.PerformedAt)
                .ToListAsync();
        }

        public async Task LogAsync(string title, string description, string user)
        {
            try
            {
                using var context = CreateContext();
                var log = new AuditLog
                {
                    ActionTitle = title,
                    Description = description,
                    PerformedBy = user,
                    PerformedAt = DateTime.Now
                };

                context.AuditLogs.Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex) {
                MessageBox.Show($"{ex.Message}"); 
            }
        }

    }
}
