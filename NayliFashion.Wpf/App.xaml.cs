using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.Implementations;
using NayliFashion.Services.Interfaces;
using NayliFashion.Wpf.ViewModels;
using NayliFashion.Wpf.Views;

namespace NayliFashion.Wpf;

/// <summary>
/// نقطة الدخول والتحكم الرئيسية لتطبيق Nayli Fashion
/// تهيئ حاوية حقن التبعيات (DI) وقاعدة البيانات وتسلسل النوافذ
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private MainWindow? _mainWindow;
    private LoginWindow? _loginWindow;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        // 1. تهيئة قاعدة البيانات وزرع البيانات الأولية تلقائياً
        try
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await contextFactory.CreateDbContextAsync();
            await DatabaseInitializer.InitializeDatabaseAsync(db);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"فشل تهيئة قاعدة البيانات: {ex.Message}", "خطأ تشغيلي", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 2. إظهار نافذة تسجيل الدخول
        ShowLoginWindow();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string appDirectory = @"D:\repos\NayliFashion";
        Directory.CreateDirectory(appDirectory);
        string dbPath = Path.Combine(appDirectory, "nayli_fashion.db");

        // مصنع سياق قاعدة البيانات الآمن لخيوط المعالجة (DbContextFactory)
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath}");
        });

        // الخدمات والأدوات المساعدة
        services.AddSingleton<IBarcodeService, BarcodeService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<IReceiptPrinterService, ReceiptPrinterService>();
        services.AddScoped<IInventoryService, InventoryService>();
        services.AddScoped<IPosService, PosService>();
        services.AddScoped<IRentalService, RentalService>();
        services.AddScoped<ICashShiftService, CashShiftService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IPurchasingService, PurchasingService>();

        // نماذج العرض (ViewModels)
        services.AddTransient<PosViewModel>();
        services.AddTransient<InventoryViewModel>();
        services.AddTransient<RentalsViewModel>();
        services.AddTransient<CustomersViewModel>();
        services.AddTransient<FinanceViewModel>();
        services.AddTransient<SettingsViewModel>();
    }

    private void ShowLoginWindow()
    {
        var authService = _serviceProvider!.GetRequiredService<IAuthService>();

        var loginVm = new LoginViewModel(authService, user =>
        {
            // عند نجاح تسجيل الدخول، نفتح النافذة الرئيسية
            _loginWindow?.Close();
            ShowMainWindow();
        });

        _loginWindow = new LoginWindow
        {
            DataContext = loginVm
        };

        _loginWindow.Show();
    }

    private void ShowMainWindow()
    {
        var authService = _serviceProvider!.GetRequiredService<IAuthService>();
        var cashShiftService = _serviceProvider!.GetRequiredService<ICashShiftService>();

        var mainVm = new MainViewModel(authService, cashShiftService, _serviceProvider!, () =>
        {
            // عند تسجيل الخروج
            _mainWindow?.Close();
            ShowLoginWindow();
        });

        _mainWindow = new MainWindow
        {
            DataContext = mainVm
        };

        _mainWindow.Show();
        _ = mainVm.InitializeAsync();
    }
}
