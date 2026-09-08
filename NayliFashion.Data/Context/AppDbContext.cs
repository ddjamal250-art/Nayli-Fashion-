using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Common;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Core.Models.Finance;
using NayliFashion.Core.Models.Inventory;
using NayliFashion.Core.Models.Purchasing;
using NayliFashion.Core.Models.Rentals;
using NayliFashion.Core.Models.Sales;
using NayliFashion.Core.Models.System;
using NayliFashion.Core.Models.Users;

namespace NayliFashion.Data.Context;

/// <summary>
/// سياق قاعدة بيانات نظام Nayli Fashion (EF Core 8 مع SQLite)
/// مهيأ لدعم التعديل المتزامن الآمن ومعالجة دقة الأرقام العشرية والحذف الناعم
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public AppDbContext()
    {
    }

    // --- كتالوج المنتجات والأقمشة والعطور ---
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<FabricRoll> FabricRolls => Set<FabricRoll>();
    public DbSet<CompositeRecipeItem> CompositeRecipeItems => Set<CompositeRecipeItem>();

    // --- المخزون والجرد ---
    public DbSet<StockAdjustment> StockAdjustments => Set<StockAdjustment>();
    public DbSet<StockAdjustmentItem> StockAdjustmentItems => Set<StockAdjustmentItem>();

    // --- الموردين والمشتريات ---
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<SupplierTransaction> SupplierTransactions => Set<SupplierTransaction>();
    public DbSet<PurchaseInvoice> PurchaseInvoices => Set<PurchaseInvoice>();
    public DbSet<PurchaseItem> PurchaseItems => Set<PurchaseItem>();

    // --- العملاء ودفتر الديون ---
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerTransaction> CustomerTransactions => Set<CustomerTransaction>();

    // --- المبيعات ونقطة البيع ---
    public DbSet<SaleInvoice> SaleInvoices => Set<SaleInvoice>();
    public DbSet<SaleItem> SaleItems => Set<SaleItem>();
    public DbSet<ActiveDraftCart> ActiveDraftCarts => Set<ActiveDraftCart>();

    // --- الكراء وتتبع الأصول ---
    public DbSet<RentalOrder> RentalOrders => Set<RentalOrder>();
    public DbSet<RentalItem> RentalItems => Set<RentalItem>();
    public DbSet<RentalAssetItem> RentalAssetItems => Set<RentalAssetItem>();

    // --- المالية والورديات والمصاريف ---
    public DbSet<CashShift> CashShifts => Set<CashShift>();
    public DbSet<CashTransaction> CashTransactions => Set<CashTransaction>();
    public DbSet<Expense> Expenses => Set<Expense>();

    // --- المستخدمين والإعدادات ---
    public DbSet<User> Users => Set<User>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // مسار قاعدة بيانات SQLite الافتراضي
            string dbPath = Path.Combine(@"D:\repos\NayliFashion", "nayli_fashion.db");
            optionsBuilder.UseSqlite($"Data Source={dbPath}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // تطبيق كافة إعدادات الكيانات والفهارس ودقة الأرقام تلقائياً
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // تطبيق فلتر الحذف الناعم (Soft Delete Query Filter) على جميع الكيانات الموروثة من BaseEntity
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(BaseEntity.IsDeleted));
                var filter = Expression.Lambda(Expression.Not(property), parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(filter);
            }
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // التحديث التلقائي لتواريخ الإنشاء والتعديل
        var entries = ChangeTracker.Entries<BaseEntity>();
        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = DateTime.UtcNow;
                entry.Entity.IsActive = true;
                entry.Entity.IsDeleted = false;
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = DateTime.UtcNow;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
