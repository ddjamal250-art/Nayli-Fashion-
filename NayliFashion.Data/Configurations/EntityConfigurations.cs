using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Inventory;
using NayliFashion.Core.Models.Purchasing;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Core.Models.System;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Name).IsRequired().HasMaxLength(250);
        builder.Property(p => p.CodeSku).HasMaxLength(100);

        builder.HasOne(p => p.Category)
               .WithMany(c => c.Products)
               .HasForeignKey(p => p.CategoryId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Brand)
               .WithMany(b => b.Products)
               .HasForeignKey(p => p.BrandId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(p => p.Variants)
               .WithOne(v => v.Product)
               .HasForeignKey(v => v.ProductId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.VariantName).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Barcode).IsRequired().HasMaxLength(100);
        builder.HasIndex(v => v.Barcode).IsUnique();

        // دقة الكميات العشرية للأقمشة (بالمتر) والعطور (بالمليلتر والغرام)
        builder.Property(v => v.StockQuantity).HasPrecision(18, 3);
        builder.Property(v => v.MinStockAlertQuantity).HasPrecision(18, 3);
        builder.Property(v => v.VolumeInMl).HasPrecision(18, 2);
        builder.Property(v => v.WeightInGrams).HasPrecision(18, 2);

        // دقة المبالغ المالية بالدينار الجزائري
        builder.Property(v => v.PurchaseCostPrice).HasPrecision(18, 2);
        builder.Property(v => v.RetailPrice).HasPrecision(18, 2);
        builder.Property(v => v.WholesalePrice).HasPrecision(18, 2);
        builder.Property(v => v.RentalDailyRate).HasPrecision(18, 2);
        builder.Property(v => v.RentalSecurityDeposit).HasPrecision(18, 2);
    }
}

public class CompositeRecipeItemConfiguration : IEntityTypeConfiguration<CompositeRecipeItem>
{
    public void Configure(EntityTypeBuilder<CompositeRecipeItem> builder)
    {
        builder.ToTable("CompositeRecipeItems");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.QuantityRequired).HasPrecision(18, 3);
        builder.Property(r => r.EstimatedCostShare).HasPrecision(18, 2);

        builder.HasOne(r => r.ParentVariant)
               .WithMany(v => v.CompositeRecipeItems)
               .HasForeignKey(r => r.ParentVariantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ComponentVariant)
               .WithMany()
               .HasForeignKey(r => r.ComponentVariantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FabricRollConfiguration : IEntityTypeConfiguration<FabricRoll>
{
    public void Configure(EntityTypeBuilder<FabricRoll> builder)
    {
        builder.ToTable("FabricRolls");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.RollCode).IsRequired().HasMaxLength(100);
        builder.HasIndex(f => f.RollCode).IsUnique();

        builder.Property(f => f.InitialMeters).HasPrecision(18, 3);
        builder.Property(f => f.RemainingMeters).HasPrecision(18, 3);
        builder.Property(f => f.WidthCm).HasPrecision(18, 2);
        builder.Property(f => f.CostPerMeter).HasPrecision(18, 2);

        builder.HasOne(f => f.ProductVariant)
               .WithMany(v => v.FabricRolls)
               .HasForeignKey(f => f.ProductVariantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}

public class SaleInvoiceConfiguration : IEntityTypeConfiguration<SaleInvoice>
{
    public void Configure(EntityTypeBuilder<SaleInvoice> builder)
    {
        builder.ToTable("SaleInvoices");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.InvoiceNumber).IsRequired().HasMaxLength(100);
        builder.HasIndex(s => s.InvoiceNumber).IsUnique();

        builder.Property(s => s.GrossTotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.DiscountAmount).HasPrecision(18, 2);
        builder.Property(s => s.TaxVatAmount).HasPrecision(18, 2);
        builder.Property(s => s.NetTotalAmount).HasPrecision(18, 2);
        builder.Property(s => s.PaidAmount).HasPrecision(18, 2);
        builder.Property(s => s.ChangeDueAmount).HasPrecision(18, 2);
        builder.Property(s => s.RemainingDebtAmount).HasPrecision(18, 2);

        builder.HasOne(s => s.Customer)
               .WithMany(c => c.SaleInvoices)
               .HasForeignKey(s => s.CustomerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.User)
               .WithMany()
               .HasForeignKey(s => s.UserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CashShift)
               .WithMany()
               .HasForeignKey(s => s.CashShiftId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class SaleItemConfiguration : IEntityTypeConfiguration<SaleItem>
{
    public void Configure(EntityTypeBuilder<SaleItem> builder)
    {
        builder.ToTable("SaleItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Quantity).HasPrecision(18, 3);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);
        builder.Property(i => i.UnitCostPrice).HasPrecision(18, 2);
        builder.Property(i => i.DiscountLineAmount).HasPrecision(18, 2);
        builder.Property(i => i.TotalLineAmount).HasPrecision(18, 2);
        builder.Property(i => i.NetProfitLineAmount).HasPrecision(18, 2);

        builder.HasOne(i => i.SaleInvoice)
               .WithMany(s => s.Items)
               .HasForeignKey(i => i.SaleInvoiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ProductVariant)
               .WithMany()
               .HasForeignKey(i => i.ProductVariantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.FabricRoll)
               .WithMany()
               .HasForeignKey(i => i.FabricRollId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}

public class RentalAssetItemConfiguration : IEntityTypeConfiguration<RentalAssetItem>
{
    public void Configure(EntityTypeBuilder<RentalAssetItem> builder)
    {
        builder.ToTable("RentalAssetItems");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.AssetSerialTag).IsRequired().HasMaxLength(100);
        builder.HasIndex(a => a.AssetSerialTag).IsUnique();

        builder.Property(a => a.PurchaseCost).HasPrecision(18, 2);

        builder.HasOne(a => a.ProductVariant)
               .WithMany(v => v.RentalAssets)
               .HasForeignKey(a => a.ProductVariantId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RentalOrderConfiguration : IEntityTypeConfiguration<RentalOrder>
{
    public void Configure(EntityTypeBuilder<RentalOrder> builder)
    {
        builder.ToTable("RentalOrders");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.ContractNumber).IsRequired().HasMaxLength(100);
        builder.HasIndex(o => o.ContractNumber).IsUnique();

        builder.Property(o => o.TotalRentFeeDzd).HasPrecision(18, 2);
        builder.Property(o => o.AdvancePaidDzd).HasPrecision(18, 2);
        builder.Property(o => o.RemainingRentFeeDzd).HasPrecision(18, 2);
        builder.Property(o => o.SecurityDepositCashDzd).HasPrecision(18, 2);
        builder.Property(o => o.LateFeePerDayDzd).HasPrecision(18, 2);
        builder.Property(o => o.CalculatedLateFeesDzd).HasPrecision(18, 2);
        builder.Property(o => o.DamageRepairCostDzd).HasPrecision(18, 2);
        builder.Property(o => o.DryCleaningFeeDzd).HasPrecision(18, 2);

        builder.HasOne(o => o.Customer)
               .WithMany(c => c.RentalOrders)
               .HasForeignKey(o => o.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.User)
               .WithMany()
               .HasForeignKey(o => o.UserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Username).IsRequired().HasMaxLength(100);
        builder.HasIndex(u => u.Username).IsUnique();
        builder.Property(u => u.FullName).IsRequired().HasMaxLength(200);
    }
}
