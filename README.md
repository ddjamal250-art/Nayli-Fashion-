# 👗 Nayli Fashion ERP & POS System (نظام نايلي فاشن المتكامل)

[![Nayli Fashion CI & Automated Testing](https://github.com/ddjamal250-art/Nayli-Fashion-/actions/workflows/build.yml/badge.svg)](https://github.com/ddjamal250-art/Nayli-Fashion-/actions/workflows/build.yml)
[![.NET 8.0](https://img.shields.io/badge/.NET-8.0-purple.svg)](https://dotnet.microsoft.com/)
[![WPF](https://img.shields.io/badge/UI-WPF%20XAML-blue.svg)](https://learn.microsoft.com/dotnet/desktop/wpf/)
[![EF Core 8 SQLite](https://img.shields.io/badge/ORM-EF%20Core%208%20SQLite-green.svg)](https://learn.microsoft.com/ef/core/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

نظام إدارة مؤسسات ونقاط بيع سطح مكتب احترافي متكامل (**ERP & POS**) مصمم خصيصاً لتلبية متطلبات تجارة الألبسة، الأحذية، المفروشات، الأقمشة بالأمتار والبكرات، عطور التعبئة، وكراء فساتين الأعراس والمناسبات في السوق الجزائري (وبالأخص بيئة التجارة بولاية الجلفة).

---

## 🌟 أبرز الميزات المتخصصة (Core Features)

1. **نظام الأقمشة والمفروشات بالبكرات والأمتار العشرية (Fabrics by Meter & Rolls)**:
   - دعم بيع الأمتار وكسورها بدقة تصل لثلاث خانات عشرية (مثل: 3.25 متر).
   - تتبع كل بكرة قماش (طاقة) برقم تسلسلي خاص (RollCode)، عرض القماش، والطول المتبقي مع تحديث حالة النفاد التلقائي.
2. **عطور التعبئة وقوائم المواد المركبة (Decanted Perfumes & Recipe BOM)**:
   - إدارة وصفات العطور التلقائية: خصم زيت العطر الخام بالمليلتر، كحول العطور، والزجاجة الفارغة فورياً عند البيع.
3. **ألبسة الصلاة والقميص الجزائري والتقليدي (Dual-Dimension Qamis)**:
   - دعم المقاسات المزدوجة الخاصة بأقمصة الصلاة (الطول بالبوصة مثل 52..62 × عرض الأكتاف S..3XL).
4. **دفتر ديون الزبائن وتنبيهات مواعيد الراتب (Carnet & Salary Virement Alerts)**:
   - إدارة سقف الائتمان لكل عميل (MaxCreditLimitDzd).
   - تنبيهات استباقية لمواعيد تقاضي الرواتب (متقاعدي CNR، موظفي الوظيف العمومي، والبريد).
   - تسجيل سندات القبض النقدية وتوليد كشف حساب تفصيلي للزبون.
5. **نظام كراء فساتين الأعراس والرهن البيومتري (Bridal Dress Rentals & CNI Custody)**:
   - فحص توفر القطعة العرائسية ومنع تضارب التواريخ.
   - تتبع وثيقة الضمان (بطاقة التعريف الوطنية البيومترية CNI) ورقمها ومكان حفظها بالخزنة.
   - حساب تلقائي لغرامات التأخير اليومية، وتكاليف المصبغة، وإصلاح الأضرار.
6. **إدارة الورديات وإغلاق الصناديق (Cash Shifts & Blind Z-Report)**:
   - إدارة العهدة الافتتاحية والمصروفات النثرية والسحوبات.
   - إغلاق أعمى (Blind Close) لمنع التلاعب وكشف العجز أو الفائض النقدي بدقة 0.00 دج.
7. **التفقيط المالي بالدينار والسنتيم الجزائري (Arabic Tafqeet & Algerian Centimes)**:
   - تفقيط لغوي فصيح للمبالغ بالفواتير وسندات القبض.
   - تحويل المبالغ للعامية الجزائرية والجلفاوية (مثال: 10,000 دج = 1 مليون سنتيم).
8. **الباركود والأمان والنسخ الاحتياطي**:
   - توليد باركود EAN-13 قياسي غير قابل للتصادم.
   - تشفير PBKDF2 لكلمات المرور بـ 100,000 تكرار.
   - نسخ احتياطي واستعادة بضغطة زر لقاعدة بيانات SQLite.

---

## 🏛 الهيكلية المعمارية (Solution Architecture)

المشروع مبني وفق أحدث معايير **Clean Architecture** وفصل المسؤوليات:

`
D:\repos\NayliFashion\
├── NayliFashion.Core/        # النماذج (Models)، الكيانات، والتعدادات (Enums)
├── NayliFashion.Data/        # سياق البيانات (AppDbContext)، تكوينات Fluent API، وزرع البيانات
├── NayliFashion.Services/    # منطق الأعمال (Business Logic)، الخدمات، الواجهات، و DTOs
├── NayliFashion.Wpf/         # واجهة المستخدم الحديثة (Dark Theme UI, MVVM, Views & ViewModels)
├── NayliFashion.Tests/       # اختبارات المحاكاة الشاملة والوحدات (xUnit & EF Core SQLite)
└── .github/workflows/        # خط أنابيب البناء والاختبار الآلي (CI/CD GitHub Actions)
`

---

## 🚀 تشغيل وبناء المشروع (Build & Run)

### المتطلبات الأساسية
- مثبت [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0).
- نظام تشغيل Windows (لواجهة WPF).

### أوامر البناء والاختبار
`ash
# استعادة الحزم
dotnet restore NayliFashion.slnx

# بناء المشروع في وضع Release
dotnet build NayliFashion.slnx --configuration Release

# تشغيل حزمة اختبارات المحاكاة
dotnet test NayliFashion.Tests/NayliFashion.Tests.csproj
`

---

## 👤 بيانات الدخول الافتراضية للتجربة (Default Login)

- **اسم المستخدم**: dmin
- **كلمة المرور**: dmin123
- **الدور**: المدير العام (SuperAdmin)

---

## 📄 الترخيص (License)
هذا المشروع مرخص تحت رخصة [MIT License](LICENSE).
