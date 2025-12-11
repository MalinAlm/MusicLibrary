using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicLibrary.Model
{
    class LibraryContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = new SqlConnectionStringBuilder()
            {
                ServerSPN = "localhost",
                InitialCatalog = "everyloop",
                TrustServerCertificate = true,
                IntegratedSecurity = true,
            }.ToString();

            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}
