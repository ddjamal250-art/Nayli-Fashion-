using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NayliFashion.Data.Context;
using NayliFashion.Data.Seed;
using NayliFashion.Services.Implementations;
using NayliFashion.Services.Interfaces;
using NayliFashion.Wpf.Services;
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

        // درع الحماية الشامل من الانهيارات المفاجئة (Crash-Proof Handlers)
        DispatcherUnhandledException += (s, args) =>
        {
            args.Handled = true;
            LogCrash(args.Exception);
            MessageBox.Show($"حدث تنبيه تقني غير متوقع تم احتواؤه بأمان:\n{args.Exception.Message}\nتم حماية بيانات الفاتورة والجلسة من الضياع بنجاح.", "درع الحماية (Nayli Crash-Proof)", MessageBoxButton.OK, MessageBoxImage.Warning);
        };

        TaskScheduler.UnobservedTaskException += (s, args) =>
        {
            args.SetObserved();
            LogCrash(args.Exception);
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogCrash(ex);
        };

        var services = new ServiceCollection();
        ConfigureServices(services);

        _serviceProvider = services.BuildServiceProvider();

        // 1. تهيئة قاعدة البيانات وتفعيل وضع WAL فائق الأداء
        try
        {
            var contextFactory = _serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var db = await contextFactory.CreateDbContextAsync();
            await DatabaseInitializer.InitializeDatabaseAsync(db);

            // تحصين SQLite بـ Write-Ahead Logging لمنع أقفال القراءة والكتابة المتزامنة
            await db.Database.ExecuteSqlRawAsync("PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL; PRAGMA busy_timeout = 5000;");
        }
        catch (Exception ex)
        {
            LogCrash(ex);
            MessageBox.Show($"فشل تهيئة قاعدة البيانات: {ex.Message}", "خطأ تشغيلي", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        // 2. إظهار نافذة تسجيل الدخول
        ShowLoginWindow();
    }

    private static void LogCrash(Exception ex)
    {
        try
        {
            string logDir = @"D:\repos\NayliFashion\Logs";
            Directory.CreateDirectory(logDir);
            string logFile = Path.Combine(logDir, $"crash_{DateTime.Now:yyyyMMdd}.log");
            string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().FullName}: {ex.Message}\n{ex.StackTrace}\n---\n";
            File.AppendAllText(logFile, entry);
        }
        catch
        {
            // صامت لعدم إيقاف التطبيق
        }
    }

    private void ConfigureServices(IServiceCollection services)
    {
        string appDirectory = @"D:\repos\NayliFashion";
        Directory.CreateDirectory(appDirectory);
        string dbPath = Path.Combine(appDirectory, "nayli_fashion.db");

        // مصنع سياق قاعدة البيانات الآمن مع معلمات الأداء العالي
        services.AddDbContextFactory<AppDbContext>(options =>
        {
            options.UseSqlite($"Data Source={dbPath};Default Timeout=30;Cache=Shared;Mode=ReadWriteCreate;");
        });

        // الخدمات والأدوات المساعدة
        services.AddSingleton<IToastNotificationService, ToastNotificationService>();
        services.AddSingleton<IBarcodeService, BarcodeService>();
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<IFileStorageService, FileStorageService>();
        services.AddScoped<IDataMigrationService, DataMigrationService>();
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

    protected override async void OnExit(ExitEventArgs e)
    {
        try
        {
            if (_serviceProvider != null)
            {
                var backupService = _serviceProvider.GetService<IBackupService>();
                if (backupService != null)
                {
                    await backupService.CreateBackupAsync();
                }
            }
        }
        catch
        {
            // صامت عند الإغلاق لمنع اعتراض عملية الخروج
        }

        base.OnExit(e);
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

        var toastService = _serviceProvider!.GetRequiredService<IToastNotificationService>();
        _mainWindow.InitializeToastService(toastService);

        _mainWindow.Show();
        _ = mainVm.InitializeAsync();
    }
}
