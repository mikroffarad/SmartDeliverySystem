﻿using Microsoft.EntityFrameworkCore;
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
        public DbSet<StoreProduct> StoreProducts { get; set; }
        public DbSet<DeliveryLocationHistory> DeliveryLocationHistory { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {            // Product configuration
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
                entity.Property(e => e.Category).HasMaxLength(100);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                entity.Property(e => e.Weight).HasColumnType("decimal(18,2)");

                entity.HasOne(p => p.Vendor)
                    .WithMany(v => v.Products)
                    .HasForeignKey(p => p.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Store configuration
            modelBuilder.Entity<Store>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
            });

            // Vendor configuration
            modelBuilder.Entity<Vendor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(200);
            });            // Delivery configuration
            modelBuilder.Entity<Delivery>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");

                entity.HasOne(d => d.Vendor)
                    .WithMany()
                    .HasForeignKey(d => d.VendorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(d => d.Store)
                    .WithMany()
                    .HasForeignKey(d => d.StoreId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasMany(d => d.Products)
                    .WithOne(dp => dp.Delivery)
                    .HasForeignKey(dp => dp.DeliveryId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // DeliveryProduct configuration
            modelBuilder.Entity<DeliveryProduct>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                entity.HasOne(dp => dp.Delivery)
                    .WithMany(d => d.Products)
                    .HasForeignKey(dp => dp.DeliveryId)
                    .OnDelete(DeleteBehavior.Cascade); entity.HasOne(dp => dp.Product)
                    .WithMany()
                    .HasForeignKey(dp => dp.ProductId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // StoreProduct configuration
            modelBuilder.Entity<StoreProduct>(entity =>
            {
                entity.HasKey(e => new { e.StoreId, e.ProductId });

                entity.HasOne(sp => sp.Store)
                    .WithMany()
                    .HasForeignKey(sp => sp.StoreId)
                    .OnDelete(DeleteBehavior.Cascade); entity.HasOne(sp => sp.Product)
                    .WithMany()
                    .HasForeignKey(sp => sp.ProductId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(sp => sp.Quantity).IsRequired();
            });

            // DeliveryLocationHistory configuration
            modelBuilder.Entity<DeliveryLocationHistory>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(dlh => dlh.Delivery)
                    .WithMany()
                    .HasForeignKey(dlh => dlh.DeliveryId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.Property(dlh => dlh.Latitude).IsRequired();
                entity.Property(dlh => dlh.Longitude).IsRequired();
                entity.Property(dlh => dlh.Timestamp).IsRequired();
            });
        }
    }
}
