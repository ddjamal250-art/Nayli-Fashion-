using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Models.System;
using NayliFashion.Data.Context;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Wpf.ViewModels;

/// <summary>
/// نموذج عرض إعدادات النظام، بيانات المحل الجبائية، الطابعة الحرارية، والنسخ الاحتياطي
/// </summary>
public partial class SettingsViewModel : ViewModelBase
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBackupService _backupService;

    [ObservableProperty]
    private AppSetting _settings = new();

    [ObservableProperty]
    private ObservableCollection<string> _availableBackups = new();

    public SettingsViewModel(IDbContextFactory<AppDbContext> contextFactory, IBackupService backupService)
    {
        _contextFactory = contextFactory;
        _backupService = backupService;
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
}
