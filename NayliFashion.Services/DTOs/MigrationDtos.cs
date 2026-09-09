namespace NayliFashion.Services.DTOs;

public class ImportedProductRowDto
{
    public string Name { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Barcode { get; set; }
    public string? Sku { get; set; }
    public decimal RetailPrice { get; set; }
    public decimal PurchaseCostPrice { get; set; }
    public decimal StockQuantity { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
}

public class ImportedCustomerRowDto
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public decimal InitialDebt { get; set; }
    public decimal MaxCreditLimit { get; set; } = 50000m;
}

public class MigrationPreviewDto
{
    public string FileType { get; set; } = string.Empty;
    public int EstimatedProductCount { get; set; }
    public int EstimatedCustomerCount { get; set; }
    public List<ImportedProductRowDto> SampleProducts { get; set; } = new();
    public List<ImportedCustomerRowDto> SampleCustomers { get; set; } = new();
    public List<string> DetectedColumns { get; set; } = new();
}

public class MigrationOptionsDto
{
    public string DefaultCategoryName { get; set; } = "ألبسة عامة مستوردة";
    public bool GenerateBarcodeIfMissing { get; set; } = true;
    public bool SkipDuplicates { get; set; } = true;
}

public class MigrationResultDto
{
    public bool IsSuccess { get; set; }
    public int TotalProductsImported { get; set; }
    public int TotalCustomersImported { get; set; }
    public int TotalErrors { get; set; }
    public List<string> ErrorDetails { get; set; } = new();
    public string Message { get; set; } = string.Empty;
}
