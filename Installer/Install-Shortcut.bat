@echo off
chcp 65001 >nul
echo ========================================================
echo    تثبيت واختصار برنامج Nayli Fashion ERP & POS
echo ========================================================
echo.
echo جاري إنشاء اختصار على سطح المكتب (Desktop Shortcut)...
powershell -Command " = New-Object -ComObject WScript.Shell;  = .CreateShortcut([System.IO.Path]::Combine([Environment]::GetFolderPath('Desktop'), 'Nayli Fashion POS.lnk')); .TargetPath = '%~dp0NayliFashion.Wpf.exe'; .WorkingDirectory = '%~dp0'; .Save()"
echo.
echo [✓] تم التثبيت بنجاح! تم وضع أيقونة البرنامج على سطح المكتب.
echo يمكنك الآن تشغيل البرنامج مباشرة من سطح المكتب أو من NayliFashion.Wpf.exe
echo.
pause
