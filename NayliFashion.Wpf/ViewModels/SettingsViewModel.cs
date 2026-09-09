using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.System;
using NayliFashion.Core.Models.Users;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

public class RoleItemWrapper
{
    public UserRole Role { get; set; }
    public string DisplayName { get; set; } = string.Empty;
}

/// <summary>
/// نموذج عرض إعدادات النظام المتقدمة، إدارة الموظفين، معالج استيراد قواعد البيانات، والنسخ الاحتياطي التفاعلي
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBackupService _backupService;
    private readonly IAuthService _authService;
    private readonly IDataMigrationService _dataMigrationService;

    [ObservableProperty]
    private AppSetting _settings = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableBackups = new();

    // --- إدارة الموظفين والحسابات ---
    [ObservableProperty]
    private ObservableCollection<User> _usersList = new();

    [ObservableProperty]
    private User? _selectedUser;

    [ObservableProperty]
    private string _newUsername = string.Empty;

    [ObservableProperty]
    private string _newFullName = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string? _newPhone;

    [ObservableProperty]
    private RoleItemWrapper _selectedNewRole;

    [ObservableProperty]
    private string _resetPasswordValue = string.Empty;

    public List<RoleItemWrapper> AvailableRoles { get; } = new()
    {
        new RoleItemWrapper { Role = UserRole.Cashier, DisplayName = "كاشير (نقطة البيع والكراء والقبض)" },
        new RoleItemWrapper { Role = UserRole.StockKeeper, DisplayName = "أمين المخزن (المخزون والجرد والأقمشة)" },
        new RoleItemWrapper { Role = UserRole.StoreManager, DisplayName = "مسؤول المحل (المشتريات والتقارير)" },
        new RoleItemWrapper { Role = UserRole.SuperAdmin, DisplayName = "مدير النظام (كامل الصلاحيات)" }
    };

    // --- معالج استيراد وترحيل قواعد البيانات والملفات ---
    [ObservableProperty]
    private string? _migrationFilePath;

    [ObservableProperty]
    private MigrationPreviewDto? _migrationPreview;

    [ObservableProperty]
    private bool _hasMigrationPreview;

    [ObservableProperty]
    private string? _migrationStatusMessage;

    public SettingsViewModel(
        IDbContextFactory<AppDbContext> contextFactory,
        IBackupService backupService,
        IAuthService authService,
        IDataMigrationService dataMigrationService)
    {
        _contextFactory = contextFactory;
        _backupService = backupService;
        _authService = authService;
        _dataMigrationService = dataMigrationService;

        _selectedNewRole = AvailableRoles[0]; // Cashier default
    }

    public async Task InitializeAsync()
    {
        IsBusy = true;
        BusyMessage = "جاري تحميل إعدادات النظام...";

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            var setting = await context.AppSettings.FirstOrDefaultAsync();
            if (setting != null)
            {
                Settings = setting;
            }

            await LoadUsersAsync();
            LoadBackupsList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"خطأ أثناء التحميل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #region Staff Management
    private async Task LoadUsersAsync()
    {
        var users = await _authService.GetAllUsersAsync();
        UsersList = new ObservableCollection<User>(users);
    }

    [RelayCommand]
    private async Task AddUserAsync()
    {
        ClearMessages();

        if (string.IsNullOrWhiteSpace(NewUsername) || string.IsNullOrWhiteSpace(NewFullName) || string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "يرجى ملء اسم المستخدم، الاسم الكامل، وكلمة المرور!";
            return;
        }

        try
        {
            await _authService.CreateUserAsync(NewUsername, NewFullName, NewPassword, SelectedNewRole.Role, NewPhone);
            SuccessMessage = $"تم إنشاء حساب الموظف ({NewFullName}) بنجاح!";
            NewUsername = string.Empty;
            NewFullName = string.Empty;
            NewPassword = string.Empty;
            NewPhone = null;

            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إنشاء الحساب: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ToggleUserStatusAsync(User? user)
    {
        if (user == null) return;
        ClearMessages();

        try
        {
            bool newStatus = !user.IsActive;
            await _authService.UpdateUserStatusAsync(user.Id, newStatus);
            user.IsActive = newStatus;
            SuccessMessage = $"تم تحديث حالة حساب {user.FullName} إلى: {(newStatus ? "نشط" : "معطل")}";
            await LoadUsersAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل تحديث الحالة: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task ResetUserPasswordAsync()
    {
        if (SelectedUser == null)
        {
            ErrorMessage = "يرجى تحديد الموظف من القائمة أولاً!";
            return;
        }

        if (string.IsNullOrWhiteSpace(ResetPasswordValue))
        {
            ErrorMessage = "يرجى كتابة كلمة المرور الجديدة للموظف!";
            return;
        }

        ClearMessages();

        try
        {
            await _authService.ResetUserPasswordAsync(SelectedUser.Id, ResetPasswordValue);
            SuccessMessage = $"تمت إعادة تعيين كلمة المرور للموظف ({SelectedUser.FullName}) بنجاح!";
            ResetPasswordValue = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إعادة تعيين كلمة المرور: {ex.Message}";
        }
    }
    #endregion

    #region Universal Database & Multi-Format Migration Wizard
    [RelayCommand]
    private async Task BrowseMigrationFileAsync()
    {
        ClearMessages();

        var openDlg = new OpenFileDialog
        {
            Title = "اختر قاعدة البيانات أو ملف البيانات القديم لاستيراده",
            Filter = "جميع الملفات المدعومة (*.db;*.sqlite;*.sql;*.json;*.xml;*.csv;*.tsv;*.xlsx)|*.db;*.sqlite;*.sqlite3;*.sql;*.json;*.xml;*.csv;*.tsv;*.xlsx|" +
                     "قواعد بيانات SQLite (*.db;*.sqlite)|*.db;*.sqlite;*.sqlite3|" +
                     "ملفات تفريغ SQL Dumps (*.sql)|*.sql|" +
                     "ملفات JSON (*.json)|*.json|" +
                     "ملفات XML (*.xml)|*.xml|" +
                     "ملفات جداول CSV / Excel (*.csv;*.tsv)|*.csv;*.tsv;*.txt|" +
                     "كافة الملفات (*.*)|*.*"
        };

        if (openDlg.ShowDialog() == true)
        {
            MigrationFilePath = openDlg.FileName;
            IsBusy = true;
            BusyMessage = "جاري فحص ومعاينة بنية البيانات في الملف...";

            try
            {
                MigrationPreview = await _dataMigrationService.PreviewFileAsync(MigrationFilePath);
                HasMigrationPreview = true;
                MigrationStatusMessage = $"تم فحص الملف بنجاح ({MigrationPreview.FileType}): تم رصد حوالي {MigrationPreview.EstimatedProductCount} صنف و {MigrationPreview.EstimatedCustomerCount} زبون جاهزة للترحيل.";
            }
            catch (Exception ex)
            {
                HasMigrationPreview = false;
                MigrationStatusMessage = null;
                ErrorMessage = $"فشل فحص الملف: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task ExecuteMigrationAsync()
    {
        if (string.IsNullOrWhiteSpace(MigrationFilePath))
        {
            ErrorMessage = "يرجى اختيار ملف البيانات أولاً!";
            return;
        }

        ClearMessages();
        IsBusy = true;
        BusyMessage = "جاري ترحيل واستيراد البيانات إلى قاعدة بيانات Nayli Fashion...";

        try
        {
            var options = new MigrationOptionsDto
            {
                DefaultCategoryName = "بضاعة مستوردة",
                GenerateBarcodeIfMissing = true,
                SkipDuplicates = true
            };

            var res = await _dataMigrationService.ImportDataAsync(MigrationFilePath, options);
            if (res.IsSuccess)
            {
                SuccessMessage = res.Message;
                MigrationStatusMessage = $"اكتمل الترحيل بنجاح! تم حفظ {res.TotalProductsImported} صنف و {res.TotalCustomersImported} زبون.";
            }
            else
            {
                ErrorMessage = res.Message;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"حدث خطأ غير متوقع أثناء الترحيل: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
    #endregion

    #region Custom Path Backup & Restore
    private void LoadBackupsList()
    {
        var list = _backupService.GetAvailableBackups();
        AvailableBackups = new ObservableCollection<string>(list);
    }

    [RelayCommand]
    private async Task SaveSettingsAsync()
    {
        ClearMessages();

        try
        {
            using var context = await _contextFactory.CreateDbContextAsync();
            context.AppSettings.Update(Settings);
            await context.SaveChangesAsync();

            SuccessMessage = "تم حفظ الإعدادات وبيانات المحل بنجاح!";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل حفظ الإعدادات: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task CreateBackupNowAsync()
    {
        ClearMessages();

        try
        {
            IsBusy = true;
            BusyMessage = "جاري أخذ نسخة احتياطية آمنة لقاعدة البيانات...";

            string path = await _backupService.CreateBackupAsync(Settings.BackupFolderPath);
            SuccessMessage = $"تم إنشاء النسخة الاحتياطية بنجاح في:\n{path}";
            LoadBackupsList();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"فشل إنشاء النسخة الاحتياطية: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BrowseAndCreateBackupAsync()
    {
        ClearMessages();

        var saveDlg = new SaveFileDialog
        {
            Title = "اختر مكان حفظ النسخة الاحتياطية (فلاش ديسك، قرص خارجي، أو سحابي)",
            FileName = $"NayliFashion_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
            Filter = "قاعدة بيانات SQLite النسخة الاحتياطية (*.db)|*.db"
        };

        if (saveDlg.ShowDialog() == true)
        {
            try
            {
                IsBusy = true;
                BusyMessage = "جاري نسخ قاعدة البيانات إلى المسار المختار...";

                string savedPath = await _backupService.CreateBackupAsync(saveDlg.FileName);
                SuccessMessage = $"تم حفظ النسخة الاحتياطية بنجاح في:\n{savedPath}";
                LoadBackupsList();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل حفظ النسخة في المسار المحدد: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }

    [RelayCommand]
    private async Task BrowseAndRestoreBackupAsync()
    {
        ClearMessages();

        var openDlg = new OpenFileDialog
        {
            Title = "اختر ملف النسخة الاحتياطية لاستعادتها",
            Filter = "قاعدة بيانات SQLite النسخة الاحتياطية (*.db)|*.db|كافة الملفات (*.*)|*.*"
        };

        if (openDlg.ShowDialog() == true)
        {
            var confirm = MessageBox.Show(
                "تحذير هام:\nاسترجاع نسخة احتياطية سيستبدل قاعدة البيانات الحالية بالكامل بالبيانات المسترجعة!\nهل أنت متأكد من الاستمرار؟",
                "تأكيد استرجاع قاعدة البيانات",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm != MessageBoxResult.Yes)
                return;

            try
            {
                IsBusy = true;
                BusyMessage = "جاري استعادة قاعدة البيانات بأمان تام...";

                bool restored = await _backupService.RestoreBackupAsync(openDlg.FileName);
                if (restored)
                {
                    SuccessMessage = "تم استرجاع قاعدة البيانات بنجاح! يرجى إعادة تشغيل التطبيق لتحديث كافة الجلسات.";
                }
                else
                {
                    ErrorMessage = "تعذر استرجاع ملف النسخة الاحتياطية.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"فشل استرجاع النسخة: {ex.Message}";
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
    #endregion
}
