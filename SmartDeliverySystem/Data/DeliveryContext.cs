using Microsoft.EntityFrameworkCore;
using SmartDeliverySystem.Models;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection.Emit;

namespace SmartDeliverySystem.Data
{
    public class DeliveryContext : DbContext
    {
        public DeliveryContext(DbContextOptions<DeliveryContext> options) : base(options)
        {
        }

        public DbSet<Product> Products { get; set; }
        public DbSet<Store> Stores { get; set; }
        public DbSet<Vendor> Vendors { get; set; }
        public DbSet<Delivery> Deliveries { get; set; }
        public DbSet<DeliveryProduct> DeliveryProducts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");
            });

            // Store configuration
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            // Vendor configuration
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.ContactEmail).HasMaxLength(250);
                entity.Property(e => e.Address).HasMaxLength(500);
            });

            // Delivery configuration
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Vendor)
                    .WithMany()
                    .HasForeignKey(d => d.VendorId);

                entity.HasOne(d => d.Store)
                    .WithMany()
                    .HasForeignKey(d => d.StoreId);
            });

            // DeliveryProduct configuration
            modelBuilder.Entity<DeliveryProduct>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(dp => dp.Product)
                    .WithMany()
                    .HasForeignKey(dp => dp.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(dp => dp.Delivery)
                    .WithMany()
                    .HasForeignKey(dp => dp.DeliveryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}