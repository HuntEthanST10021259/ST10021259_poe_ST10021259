
using Microsoft.EntityFrameworkCore;
using PROG7311_POE_ST10021259.Models;
using System.Reflection.Emit;

namespace PROG7311_POE_ST10021259.Data
{
    public class GlmsDbContext : DbContext
    {
        public GlmsDbContext(DbContextOptions<GlmsDbContext> options) : base(options) { }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Client to Contracts: one-to-many
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.Client)
                .WithMany(cl => cl.Contracts)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            // Contract to ServiceRequests: one-to-many
            modelBuilder.Entity<ServiceRequest>()
                .HasOne(sr => sr.Contract)
                .WithMany(c => c.ServiceRequests)
                .HasForeignKey(sr => sr.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed data
            modelBuilder.Entity<Client>().HasData(
                new Client { Id = 1, Name = "Abda Logistics Ltd", ContactDetails = "abda@logistics.com | 011 555 0001", Region = "Africa" },
                new Client { Id = 2, Name = "Global shipping Inc", ContactDetails = "gsi@freight.com | 212 555 0023", Region = "North America" },
                new Client { Id = 3, Name = "EuroShip", ContactDetails = "info@euroship.de | 040 555 0099", Region = "Europe" }
            );

            modelBuilder.Entity<Contract>().HasData(
                new Contract
                {
                    Id = 1,
                    ClientId = 1,
                    StartDate = new DateTime(2025, 1, 1),
                    EndDate = new DateTime(2026, 12, 31),
                    Status = ContractStatus.Active,
                    ServiceLevel = "Premium Air Freight"
                },
                new Contract
                {
                    Id = 2,
                    ClientId = 2,
                    StartDate = new DateTime(2024, 6, 1),
                    EndDate = new DateTime(2025, 5, 31),
                    Status = ContractStatus.Expired,
                    ServiceLevel = "Standard Sea Freight"
                },
                new Contract
                {
                    Id = 3,
                    ClientId = 3,
                    StartDate = new DateTime(2025, 3, 1),
                    EndDate = new DateTime(2027, 2, 28),
                    Status = ContractStatus.Draft,
                    ServiceLevel = "Land Transport Express"
                }
            );
        }
    }
}