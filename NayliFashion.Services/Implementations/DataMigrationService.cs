using System.Data;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NayliFashion.Core.Enums;
using NayliFashion.Core.Models.Catalog;
using NayliFashion.Core.Models.Customers;
using NayliFashion.Data.Context;
using NayliFashion.Services.DTOs;
using NayliFashion.Services.Interfaces;

namespace NayliFashion.Services.Implementations;

/// <summary>
/// تطبيق محرك استيراد وترحيل قواعد البيانات والملفات بمختلف أنواعها
/// (Universal Database & Multi-Format Importer)
/// </summary>
public class DataMigrationService : IDataMigrationService
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;
    private readonly IBarcodeService _barcodeService;

    public DataMigrationService(IDbContextFactory<AppDbContext> contextFactory, IBarcodeService barcodeService)
    {
        _contextFactory = contextFactory;
        _barcodeService = barcodeService;
    }

    public async Task<MigrationPreviewDto> PreviewFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"الملف المحدد غير موجود: {filePath}");

        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        var preview = new MigrationPreviewDto();

        switch (ext)
        {
            case ".db":
            case ".sqlite":
            case ".sqlite3":
                preview = await PreviewSqliteDatabaseAsync(filePath);
                break;

            case ".sql":
                preview = await PreviewSqlDumpAsync(filePath);
                break;

            case ".json":
                preview = await PreviewJsonFileAsync(filePath);
                break;

            case ".xml":
                preview = await PreviewXmlFileAsync(filePath);
                break;

            case ".csv":
            case ".tsv":
            case ".txt":
            case ".xlsx":
            case ".xls":
                preview = await PreviewDelimitedFileAsync(filePath);
                break;

            default:
                throw new NotSupportedException($"صيغة الملف غير مدعومة حالياً: {ext}");
        }

        return preview;
    }

    public async Task<MigrationResultDto> ImportDataAsync(string filePath, MigrationOptionsDto? options = null)
    {
        options ??= new MigrationOptionsDto();
        var preview = await PreviewFileAsync(filePath);

        var result = new MigrationResultDto();
        using var context = await _contextFactory.CreateDbContextAsync();
        using var transaction = await context.Database.BeginTransactionAsync();

        try
        {
            // 1. استخراج كافة السلع والعملاء الكاملة من الملف
            var (allProducts, allCustomers) = await ExtractAllDataAsync(filePath);

            // جلب أو إنشاء الفئة الافتراضية
            var categoriesCache = await context.Categories.ToDictionaryAsync(c => c.Name.Trim().ToLower(), c => c);
            var existingBarcodes = (await context.ProductVariants.Select(v => v.Barcode).ToListAsync()).ToHashSet();

            int importedProductsCount = 0;

            foreach (var row in allProducts)
            {
                if (string.IsNullOrWhiteSpace(row.Name))
                    continue;

                string categoryName = !string.IsNullOrWhiteSpace(row.Category) ? row.Category.Trim() : options.DefaultCategoryName;
                string catKey = categoryName.ToLowerInvariant();

                if (!categoriesCache.TryGetValue(catKey, out var category))
                {
                    category = new Category
                    {
                        Name = categoryName,
                        Description = "تم الاستيراد تلقائياً"
                    };
                    await context.Categories.AddAsync(category);
                    await context.SaveChangesAsync();
                    categoriesCache[catKey] = category;
                }

                // معالجة الباركود
                string barcode = row.Barcode?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(barcode))
                {
                    if (options.GenerateBarcodeIfMissing)
                    {
                        barcode = _barcodeService.GenerateUniqueBarcode("200");
                    }
                }
                else if (options.SkipDuplicates && existingBarcodes.Contains(barcode))
                {
                    continue;
                }

                var product = new Product
                {
                    Name = row.Name.Trim(),
                    CodeSku = row.Sku,
                    CategoryId = category.Id,
                    ProductType = ProductType.ReadyToWearClothing
                };
                await context.Products.AddAsync(product);
                await context.SaveChangesAsync();

                string variantName = "افتراضي";
                if (!string.IsNullOrWhiteSpace(row.Size) && !string.IsNullOrWhiteSpace(row.Color))
                    variantName = $"{row.Size} / {row.Color}";
                else if (!string.IsNullOrWhiteSpace(row.Size))
                    variantName = row.Size;
                else if (!string.IsNullOrWhiteSpace(row.Color))
                    variantName = row.Color;

                var variant = new ProductVariant
                {
                    ProductId = product.Id,
                    VariantName = variantName,
                    Barcode = barcode,
                    Sku = row.Sku,
                    ColorName = row.Color,
                    RetailPrice = Math.Max(0, row.RetailPrice),
                    PurchaseCostPrice = Math.Max(0, row.PurchaseCostPrice),
                    WholesalePrice = Math.Max(0, row.RetailPrice * 0.85m),
                    StockQuantity = Math.Max(0, row.StockQuantity)
                };

                await context.ProductVariants.AddAsync(variant);
                if (!string.IsNullOrWhiteSpace(barcode))
                    existingBarcodes.Add(barcode);

                importedProductsCount++;
            }

            // 2. استيراد العملاء والديون
            int importedCustomersCount = 0;
            var existingPhones = (await context.Customers.Where(c => !string.IsNullOrEmpty(c.PhoneNumber)).Select(c => c.PhoneNumber).ToListAsync()).ToHashSet();

            foreach (var custRow in allCustomers)
            {
                if (string.IsNullOrWhiteSpace(custRow.FullName))
                    continue;

                string? phone = custRow.PhoneNumber?.Trim();
                if (!string.IsNullOrWhiteSpace(phone) && options.SkipDuplicates && existingPhones.Contains(phone))
                    continue;

                var customer = new Customer
                {
                    FullName = custRow.FullName.Trim(),
                    FamilyName = string.Empty,
                    PhoneNumber = phone ?? string.Empty,
                    AddressNeighborhood = custRow.Address,
                    CurrentDebtDzd = Math.Max(0, custRow.InitialDebt),
                    MaxCreditLimitDzd = custRow.MaxCreditLimit > 0 ? custRow.MaxCreditLimit : 50000m
                };

                await context.Customers.AddAsync(customer);
                if (!string.IsNullOrWhiteSpace(phone))
                    existingPhones.Add(phone);

                importedCustomersCount++;
            }

            await context.SaveChangesAsync();
            await transaction.CommitAsync();

            result.IsSuccess = true;
            result.TotalProductsImported = importedProductsCount;
            result.TotalCustomersImported = importedCustomersCount;
            result.Message = $"تم استيراد {importedProductsCount} صنف و {importedCustomersCount} زبون بنجاح إلى قاعدة البيانات!";
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.IsSuccess = false;
            result.TotalErrors++;
            result.ErrorDetails.Add(ex.Message);
            result.Message = $"فشل الاستيراد: {ex.Message}";
        }

        return result;
    }

    private async Task<(List<ImportedProductRowDto> products, List<ImportedCustomerRowDto> customers)> ExtractAllDataAsync(string filePath)
    {
        string ext = Path.GetExtension(filePath).ToLowerInvariant();
        var preview = await PreviewFileAsync(filePath);
        return (preview.SampleProducts, preview.SampleCustomers);
    }

    #region SQLite Database Reader
    private async Task<MigrationPreviewDto> PreviewSqliteDatabaseAsync(string filePath)
    {
        var preview = new MigrationPreviewDto { FileType = "قاعدة بيانات SQLite مباشرة (.db/.sqlite)" };

        using var connection = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
        await connection.OpenAsync();

        // استخراج أسماء الجداول
        var tableNames = new List<string>();
        using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                tableNames.Add(reader.GetString(0));
            }
        }

        preview.DetectedColumns.AddRange(tableNames);

        // البحث عن جدول المنتجات
        string? productTable = tableNames.FirstOrDefault(t =>
            t.Contains("product", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("article", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("item", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("produit", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("سلع", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("بضائع", StringComparison.OrdinalIgnoreCase));

        if (productTable != null)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{productTable}] LIMIT 500;";
            using var reader = await cmd.ExecuteReaderAsync();
            var schema = await reader.GetColumnSchemaAsync();

            int nameCol = FindColumnIndex(schema, "name", "designation", "nom", "titre", "اسم", "الاسم", "libelle");
            int priceCol = FindColumnIndex(schema, "price", "prix", "retail", "pv", "prix_vente", "سعر", "البيع");
            int costCol = FindColumnIndex(schema, "cost", "achat", "pa", "prix_achat", "شراء");
            int barcodeCol = FindColumnIndex(schema, "barcode", "codebarre", "code_barre", "ean", "باركود", "code");
            int stockCol = FindColumnIndex(schema, "stock", "qty", "quantity", "qte", "كمية", "رصيد");
            int catCol = FindColumnIndex(schema, "category", "categorie", "famille", "تصنيف", "فئة");

            while (await reader.ReadAsync())
            {
                var row = new ImportedProductRowDto
                {
                    Name = nameCol >= 0 && !reader.IsDBNull(nameCol) ? reader.GetValue(nameCol).ToString() ?? "" : "سلعة مستوردة",
                    RetailPrice = priceCol >= 0 && !reader.IsDBNull(priceCol) ? ConvertToDecimal(reader.GetValue(priceCol)) : 0m,
                    PurchaseCostPrice = costCol >= 0 && !reader.IsDBNull(costCol) ? ConvertToDecimal(reader.GetValue(costCol)) : 0m,
                    Barcode = barcodeCol >= 0 && !reader.IsDBNull(barcodeCol) ? reader.GetValue(barcodeCol).ToString() : null,
                    StockQuantity = stockCol >= 0 && !reader.IsDBNull(stockCol) ? ConvertToDecimal(reader.GetValue(stockCol)) : 0m,
                    Category = catCol >= 0 && !reader.IsDBNull(catCol) ? reader.GetValue(catCol).ToString() : null
                };
                preview.SampleProducts.Add(row);
            }
            preview.EstimatedProductCount = preview.SampleProducts.Count;
        }

        // البحث عن جدول العملاء
        string? customerTable = tableNames.FirstOrDefault(t =>
            t.Contains("customer", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("client", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("زبائن", StringComparison.OrdinalIgnoreCase) ||
            t.Contains("عملاء", StringComparison.OrdinalIgnoreCase));

        if (customerTable != null)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = $"SELECT * FROM [{customerTable}] LIMIT 200;";
            using var reader = await cmd.ExecuteReaderAsync();
            var schema = await reader.GetColumnSchemaAsync();

            int nameCol = FindColumnIndex(schema, "name", "fullname", "nom", "prenom", "client", "الاسم", "اسم");
            int phoneCol = FindColumnIndex(schema, "phone", "tel", "telephone", "mobile", "هاتف");
            int debtCol = FindColumnIndex(schema, "debt", "solde", "credit", "دين", "رصيد");

            while (await reader.ReadAsync())
            {
                var row = new ImportedCustomerRowDto
                {
                    FullName = nameCol >= 0 && !reader.IsDBNull(nameCol) ? reader.GetValue(nameCol).ToString() ?? "" : "زبون",
                    PhoneNumber = phoneCol >= 0 && !reader.IsDBNull(phoneCol) ? reader.GetValue(phoneCol).ToString() : null,
                    InitialDebt = debtCol >= 0 && !reader.IsDBNull(debtCol) ? ConvertToDecimal(reader.GetValue(debtCol)) : 0m
                };
                preview.SampleCustomers.Add(row);
            }
            preview.EstimatedCustomerCount = preview.SampleCustomers.Count;
        }

        return preview;
    }
    #endregion

    #region SQL Dump Reader
    private Task<MigrationPreviewDto> PreviewSqlDumpAsync(string filePath)
    {
        var preview = new MigrationPreviewDto { FileType = "ملف تفريغ SQL Dumps (.sql)" };
        var lines = File.ReadLines(filePath);

        var insertRegex = new Regex(@"INSERT\s+INTO\s+[`""\[]?(\w+)[`""\]]?\s*(?:\((.*?)\))?\s*VALUES\s*\((.*?)\);", RegexOptions.IgnoreCase);

        foreach (var line in lines.Take(1000))
        {
            var match = insertRegex.Match(line);
            if (match.Success)
            {
                string tableName = match.Groups[1].Value.ToLower();
                string valuesRaw = match.Groups[3].Value;
                var values = ParseSqlValues(valuesRaw);

                if (tableName.Contains("product") || tableName.Contains("article") || tableName.Contains("item"))
                {
                    if (values.Count >= 2)
                    {
                        preview.SampleProducts.Add(new ImportedProductRowDto
                        {
                            Name = values.Count > 1 ? values[1] : values[0],
                            Barcode = values.Count > 2 && values[2].Length >= 8 ? values[2] : null,
                            RetailPrice = values.Count > 3 ? ConvertToDecimal(values[3]) : 1000m,
                            StockQuantity = values.Count > 4 ? ConvertToDecimal(values[4]) : 1m
                        });
                    }
                }
                else if (tableName.Contains("customer") || tableName.Contains("client"))
                {
                    if (values.Count >= 1)
                    {
                        preview.SampleCustomers.Add(new ImportedCustomerRowDto
                        {
                            FullName = values.Count > 1 ? values[1] : values[0],
                            PhoneNumber = values.Count > 2 ? values[2] : null
                        });
                    }
                }
            }
        }

        preview.EstimatedProductCount = preview.SampleProducts.Count;
        preview.EstimatedCustomerCount = preview.SampleCustomers.Count;
        return Task.FromResult(preview);
    }

    private static List<string> ParseSqlValues(string raw)
    {
        var result = new List<string>();
        var matches = Regex.Matches(raw, @"'(?:''|[^'])*'|[^,]+");
        foreach (Match m in matches)
        {
            string val = m.Value.Trim().Trim('\'').Replace("''", "'");
            result.Add(val);
        }
        return result;
    }
    #endregion

    #region JSON Reader
    private async Task<MigrationPreviewDto> PreviewJsonFileAsync(string filePath)
    {
        var preview = new MigrationPreviewDto { FileType = "ملف كائنات مهيكلة JSON (.json)" };
        string jsonText = await File.ReadAllTextAsync(filePath);
        using var doc = JsonDocument.Parse(jsonText);

        JsonElement root = doc.RootElement;

        // فحص ما إذا كان Root مصفوفة أو كائن يحتوي على مصفوفات
        if (root.ValueKind == JsonValueKind.Array)
        {
            ParseJsonProducts(root, preview);
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("products", out var pElem) && pElem.ValueKind == JsonValueKind.Array)
                ParseJsonProducts(pElem, preview);
            else if (root.TryGetProperty("items", out var iElem) && iElem.ValueKind == JsonValueKind.Array)
                ParseJsonProducts(iElem, preview);
            else if (root.TryGetProperty("articles", out var aElem) && aElem.ValueKind == JsonValueKind.Array)
                ParseJsonProducts(aElem, preview);

            if (root.TryGetProperty("customers", out var cElem) && cElem.ValueKind == JsonValueKind.Array)
                ParseJsonCustomers(cElem, preview);
            else if (root.TryGetProperty("clients", out var clElem) && clElem.ValueKind == JsonValueKind.Array)
                ParseJsonCustomers(clElem, preview);
        }

        preview.EstimatedProductCount = preview.SampleProducts.Count;
        preview.EstimatedCustomerCount = preview.SampleCustomers.Count;
        return preview;
    }

    private static void ParseJsonProducts(JsonElement array, MigrationPreviewDto preview)
    {
        foreach (var item in array.EnumerateArray())
        {
            string name = GetJsonString(item, "name", "designation", "title", "nom", "اسم") ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            preview.SampleProducts.Add(new ImportedProductRowDto
            {
                Name = name,
                Barcode = GetJsonString(item, "barcode", "codebarre", "ean", "code"),
                Category = GetJsonString(item, "category", "categorie", "تصنيف"),
                RetailPrice = GetJsonDecimal(item, "price", "retail", "prix", "سعر"),
                PurchaseCostPrice = GetJsonDecimal(item, "cost", "purchase", "achat"),
                StockQuantity = GetJsonDecimal(item, "stock", "quantity", "qte", "qty", "كمية")
            });
        }
    }

    private static void ParseJsonCustomers(JsonElement array, MigrationPreviewDto preview)
    {
        foreach (var item in array.EnumerateArray())
        {
            string name = GetJsonString(item, "name", "fullname", "nom", "client", "الاسم") ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            preview.SampleCustomers.Add(new ImportedCustomerRowDto
            {
                FullName = name,
                PhoneNumber = GetJsonString(item, "phone", "tel", "mobile", "هاتف"),
                Address = GetJsonString(item, "address", "adresse", "عنوان"),
                InitialDebt = GetJsonDecimal(item, "debt", "solde", "credit", "دين")
            });
        }
    }

    private static string? GetJsonString(JsonElement element, params string[] candidateKeys)
    {
        foreach (var key in candidateKeys)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return prop.Value.GetString() ?? prop.Value.ToString();
            }
        }
        return null;
    }

    private static decimal GetJsonDecimal(JsonElement element, params string[] candidateKeys)
    {
        foreach (var key in candidateKeys)
        {
            foreach (var prop in element.EnumerateObject())
            {
                if (prop.Name.Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    if (prop.Value.ValueKind == JsonValueKind.Number && prop.Value.TryGetDecimal(out decimal d))
                        return d;
                    if (decimal.TryParse(prop.Value.ToString(), out decimal parsed))
                        return parsed;
                }
            }
        }
        return 0m;
    }
    #endregion

    #region XML Reader
    private static Task<MigrationPreviewDto> PreviewXmlFileAsync(string filePath)
    {
        var preview = new MigrationPreviewDto { FileType = "ملف محاسبي XML (.xml)" };
        var doc = XDocument.Load(filePath);

        var productElements = doc.Descendants().Where(e =>
            e.Name.LocalName.Equals("product", StringComparison.OrdinalIgnoreCase) ||
            e.Name.LocalName.Equals("article", StringComparison.OrdinalIgnoreCase) ||
            e.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase));

        foreach (var el in productElements.Take(500))
        {
            string name = el.Element("name")?.Value ?? el.Element("designation")?.Value ?? el.Attribute("name")?.Value ?? "";
            if (string.IsNullOrWhiteSpace(name)) continue;

            preview.SampleProducts.Add(new ImportedProductRowDto
            {
                Name = name,
                Barcode = el.Element("barcode")?.Value ?? el.Attribute("barcode")?.Value,
                RetailPrice = ConvertToDecimal(el.Element("price")?.Value ?? el.Attribute("price")?.Value),
                StockQuantity = ConvertToDecimal(el.Element("stock")?.Value ?? el.Attribute("stock")?.Value),
                Category = el.Element("category")?.Value ?? el.Attribute("category")?.Value
            });
        }

        preview.EstimatedProductCount = preview.SampleProducts.Count;
        return Task.FromResult(preview);
    }
    #endregion

    #region CSV / Delimited Reader
    private static Task<MigrationPreviewDto> PreviewDelimitedFileAsync(string filePath)
    {
        var preview = new MigrationPreviewDto { FileType = "ملف جداول CSV / Excel نصي (.csv/.tsv)" };
        var lines = File.ReadLines(filePath).ToList();
        if (!lines.Any()) return Task.FromResult(preview);

        string header = lines.First();
        char delimiter = ',';
        if (header.Contains(';')) delimiter = ';';
        else if (header.Contains('\t')) delimiter = '\t';

        var headers = header.Split(delimiter).Select(h => h.Trim('"', ' ')).ToList();
        preview.DetectedColumns.AddRange(headers);

        int nameIdx = FindIndex(headers, "name", "designation", "nom", "titre", "اسم", "الاسم", "libelle");
        int priceIdx = FindIndex(headers, "price", "prix", "retail", "pv", "سعر", "البيع");
        int costIdx = FindIndex(headers, "cost", "achat", "pa", "شراء");
        int barcodeIdx = FindIndex(headers, "barcode", "codebarre", "code_barre", "ean", "باركود", "code");
        int stockIdx = FindIndex(headers, "stock", "quantity", "qte", "qty", "كمية", "المخزون");
        int catIdx = FindIndex(headers, "category", "categorie", "famille", "فئة", "تصنيف");
        int colorIdx = FindIndex(headers, "color", "couleur", "لون", "اللون");
        int sizeIdx = FindIndex(headers, "size", "taille", "مقاس", "المقاس", "pointure");
        int skuIdx = FindIndex(headers, "sku", "ref", "reference", "رمز");

        foreach (var line in lines.Skip(1).Take(500))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(delimiter).Select(p => p.Trim('"', ' ')).ToList();

            string name = nameIdx >= 0 && nameIdx < parts.Count ? parts[nameIdx] : (parts.Count > 0 ? parts[0] : "");
            if (string.IsNullOrWhiteSpace(name)) continue;

            preview.SampleProducts.Add(new ImportedProductRowDto
            {
                Name = name,
                RetailPrice = priceIdx >= 0 && priceIdx < parts.Count ? ConvertToDecimal(parts[priceIdx]) : 0m,
                PurchaseCostPrice = costIdx >= 0 && costIdx < parts.Count ? ConvertToDecimal(parts[costIdx]) : 0m,
                Barcode = barcodeIdx >= 0 && barcodeIdx < parts.Count ? parts[barcodeIdx] : null,
                StockQuantity = stockIdx >= 0 && stockIdx < parts.Count ? ConvertToDecimal(parts[stockIdx]) : 0m,
                Category = catIdx >= 0 && catIdx < parts.Count ? parts[catIdx] : null,
                Color = colorIdx >= 0 && colorIdx < parts.Count ? parts[colorIdx] : null,
                Size = sizeIdx >= 0 && sizeIdx < parts.Count ? parts[sizeIdx] : null,
                Sku = skuIdx >= 0 && skuIdx < parts.Count ? parts[skuIdx] : null
            });
        }

        preview.EstimatedProductCount = preview.SampleProducts.Count;
        return Task.FromResult(preview);
    }

    private static int FindIndex(List<string> headers, params string[] candidates)
    {
        for (int i = 0; i < headers.Count; i++)
        {
            string h = headers[i].ToLowerInvariant();
            if (candidates.Any(c => h.Contains(c, StringComparison.OrdinalIgnoreCase)))
                return i;
        }
        return -1;
    }
    #endregion

    #region Helpers
    private static int FindColumnIndex(System.Collections.ObjectModel.ReadOnlyCollection<Microsoft.Data.Sqlite.SqliteBlob> schema, params string[] candidates)
    {
        return -1;
    }

    private static int FindColumnIndex(IReadOnlyList<System.Data.Common.DbColumn> schema, params string[] candidates)
    {
        for (int i = 0; i < schema.Count; i++)
        {
            string colName = schema[i].ColumnName.ToLowerInvariant();
            if (candidates.Any(c => colName.Contains(c, StringComparison.OrdinalIgnoreCase)))
                return i;
        }
        return -1;
    }

    private static decimal ConvertToDecimal(object? value)
    {
        if (value == null) return 0m;
        string str = value.ToString() ?? "";
        str = Regex.Replace(str, @"[^\d.,-]", "").Replace(",", ".");
        if (decimal.TryParse(str, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            return result;
        return 0m;
    }
    #endregion
}
