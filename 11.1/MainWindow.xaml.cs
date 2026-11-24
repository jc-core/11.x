using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using PcPartsInventory.Data;
using PcPartsInventory.Models;

namespace PcPartsInventory;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadCategories();
        LoadParts();
    }

    private void LoadCategories()
    {
        using var context = new PcPartsContext();
        CategoryCombo.ItemsSource = context.Categories.OrderBy(category => category.CategoryName).ToList();
    }

    private void LoadParts()
    {
        using var context = new PcPartsContext();
        PartsGrid.ItemsSource = context.Parts
            .Include(part => part.Category)
            .OrderBy(part => part.PartId)
            .ToList();
    }

    private void PartsGrid_GotKeyboardFocus(object sender, System.Windows.Input.KeyboardFocusChangedEventArgs e)
    {
        if (e.OriginalSource != PartsGrid || PartsGrid.Items.Count == 0)
            return;

        if (PartsGrid.SelectedIndex < 0)
            PartsGrid.SelectedIndex = 0;

        var item = PartsGrid.SelectedItem;
        PartsGrid.CurrentCell = new DataGridCellInfo(item, PartsGrid.Columns[0]);
        PartsGrid.ScrollIntoView(item);
        PartsGrid.UpdateLayout();
        (PartsGrid.Columns[0].GetCellContent(item)?.Parent as DataGridCell)?.Focus();
    }

    private void PartsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PartsGrid.SelectedItem is Part part)
        {
            PartIDBox.Text = part.PartId.ToString();
            PartIDBox.IsReadOnly = true;
            CategoryCombo.SelectedValue = part.CategoryId;
            ManufacturerBox.Text = part.Manufacturer;
            ModelBox.Text = part.ModelName;
            SpecsBox.Text = part.Specs ?? "";
            PriceBox.Text = part.Price.ToString("0.00");
            QuantityBox.Text = part.QuantityInStock.ToString();
            YearBox.Text = part.ReleaseYear?.ToString() ?? "";
        }
    }

    private Part? ReadForm()
    {
        if (!int.TryParse(PartIDBox.Text, out int partID) || partID <= 0)
        {
            StatusText.Text = "Part ID must be a whole number, 1 or more.";
            return null;
        }
        if (CategoryCombo.SelectedValue == null)
        {
            StatusText.Text = "Pick a category.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(ManufacturerBox.Text) || string.IsNullOrWhiteSpace(ModelBox.Text))
        {
            StatusText.Text = "Manufacturer and model name are required.";
            return null;
        }
        if (!decimal.TryParse(PriceBox.Text, out decimal price) || price < 0)
        {
            StatusText.Text = "Price must be a number, 0 or more.";
            return null;
        }
        if (!int.TryParse(QuantityBox.Text, out int quantity) || quantity < 0)
        {
            StatusText.Text = "Quantity must be a whole number, 0 or more.";
            return null;
        }
        int? year = null;
        if (!string.IsNullOrWhiteSpace(YearBox.Text))
        {
            if (!int.TryParse(YearBox.Text, out int parsedYear))
            {
                StatusText.Text = "Release year must be a whole number or blank.";
                return null;
            }
            year = parsedYear;
        }

        return new Part
        {
            PartId = partID,
            CategoryId = (int)CategoryCombo.SelectedValue,
            Manufacturer = ManufacturerBox.Text.Trim(),
            ModelName = ModelBox.Text.Trim(),
            Specs = string.IsNullOrWhiteSpace(SpecsBox.Text) ? null : SpecsBox.Text.Trim(),
            Price = price,
            QuantityInStock = quantity,
            ReleaseYear = year
        };
    }

    private void AddButton_Click(object sender, RoutedEventArgs e)
    {
        Part? newPart = ReadForm();
        if (newPart == null) return;

        using (var context = new PcPartsContext())
        {
            if (context.Parts.Any(part => part.PartId == newPart.PartId))
            {
                StatusText.Text = $"Part ID {newPart.PartId} already exists.";
                return;
            }
            context.Parts.Add(newPart);
            context.SaveChanges();
        }
        LoadParts();
        ClearForm();
        StatusText.Text = $"Added {newPart.ModelName}.";
    }

    private void UpdateButton_Click(object sender, RoutedEventArgs e)
    {
        if (PartsGrid.SelectedItem is not Part selected)
        {
            StatusText.Text = "Select a part in the list first.";
            return;
        }
        Part? edited = ReadForm();
        if (edited == null) return;

        using (var context = new PcPartsContext())
        {
            Part? existing = context.Parts.Find(selected.PartId);
            if (existing == null)
            {
                StatusText.Text = "That part no longer exists.";
                return;
            }
            existing.CategoryId = edited.CategoryId;
            existing.Manufacturer = edited.Manufacturer;
            existing.ModelName = edited.ModelName;
            existing.Specs = edited.Specs;
            existing.Price = edited.Price;
            existing.QuantityInStock = edited.QuantityInStock;
            existing.ReleaseYear = edited.ReleaseYear;
            context.SaveChanges();
        }
        LoadParts();
        ClearForm();
        StatusText.Text = $"Updated {edited.ModelName}.";
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (PartsGrid.SelectedItem is not Part selected)
        {
            StatusText.Text = "Select a part in the list first.";
            return;
        }
        var answer = MessageBox.Show($"Delete {selected.Manufacturer} {selected.ModelName}?",
            "Confirm delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes) return;

        using (var context = new PcPartsContext())
        {
            Part? existing = context.Parts.Find(selected.PartId);
            if (existing != null)
            {
                context.Parts.Remove(existing);
                context.SaveChanges();
            }
        }
        LoadParts();
        ClearForm();
        StatusText.Text = "Deleted.";
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        StatusText.Text = "";
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        var categories = new List<Category>
        {
            new Category { CategoryId = 1, CategoryName = "CPU", Description = "Processors" },
            new Category { CategoryId = 2, CategoryName = "GPU", Description = "Graphics cards" },
            new Category { CategoryId = 3, CategoryName = "Memory", Description = "RAM kits" },
            new Category { CategoryId = 4, CategoryName = "Storage", Description = "SSDs and hard drives" },
            new Category { CategoryId = 5, CategoryName = "Motherboard", Description = "Main boards" },
            new Category { CategoryId = 6, CategoryName = "Power Supply", Description = "PSUs" }
        };
        var parts = new List<Part>
        {
            new Part { PartId = 1, CategoryId = 1, Manufacturer = "AMD", ModelName = "Ryzen 7 7800X3D", Specs = "8 cores / 16 threads, 4.2 GHz base, 96 MB L3 cache, AM5", Price = 399.00m, QuantityInStock = 4, ReleaseYear = 2023 },
            new Part { PartId = 2, CategoryId = 1, Manufacturer = "Intel", ModelName = "Core i5-13600K", Specs = "14 cores / 20 threads, 3.5 GHz base, LGA1700", Price = 279.99m, QuantityInStock = 6, ReleaseYear = 2022 },
            new Part { PartId = 3, CategoryId = 2, Manufacturer = "NVIDIA", ModelName = "GeForce RTX 4070", Specs = "12 GB GDDR6X, 5888 CUDA cores, 200 W", Price = 549.00m, QuantityInStock = 3, ReleaseYear = 2023 },
            new Part { PartId = 4, CategoryId = 2, Manufacturer = "AMD", ModelName = "Radeon RX 7800 XT", Specs = "16 GB GDDR6, 3840 stream processors, 263 W", Price = 479.99m, QuantityInStock = 2, ReleaseYear = 2023 },
            new Part { PartId = 5, CategoryId = 3, Manufacturer = "Corsair", ModelName = "Vengeance DDR5-6000", Specs = "32 GB (2 x 16 GB), CL36, 1.35 V", Price = 119.99m, QuantityInStock = 10, ReleaseYear = 2022 },
            new Part { PartId = 6, CategoryId = 4, Manufacturer = "Samsung", ModelName = "990 Pro 2TB", Specs = "NVMe M.2, PCIe 4.0, 7450 MB/s read", Price = 169.99m, QuantityInStock = 8, ReleaseYear = 2022 },
            new Part { PartId = 7, CategoryId = 5, Manufacturer = "ASUS", ModelName = "ROG Strix B650-A", Specs = "AM5, ATX, DDR5, PCIe 5.0, WiFi 6E", Price = 239.00m, QuantityInStock = 3, ReleaseYear = 2022 },
            new Part { PartId = 8, CategoryId = 6, Manufacturer = "Seasonic", ModelName = "Focus GX-850", Specs = "850 W, 80+ Gold, fully modular", Price = 149.90m, QuantityInStock = 5, ReleaseYear = 2020 }
        };

        var partIDs = parts.Select(part => part.PartId).ToList();
        var categoryIDs = categories.Select(category => category.CategoryId).ToList();
        int added = 0;
        int removed = 0;
        using (var context = new PcPartsContext())
        {
            var extraParts = context.Parts.Where(part => !partIDs.Contains(part.PartId)).ToList();
            removed = extraParts.Count;
            context.Parts.RemoveRange(extraParts);
            context.SaveChanges();
            context.Categories.RemoveRange(context.Categories.Where(category => !categoryIDs.Contains(category.CategoryId)).ToList());
            context.SaveChanges();
            foreach (Category category in categories)
            {
                if (!context.Categories.Any(existing => existing.CategoryId == category.CategoryId)) context.Categories.Add(category);
            }
            context.SaveChanges();
            foreach (Part part in parts)
            {
                if (!context.Parts.Any(existing => existing.PartId == part.PartId))
                {
                    context.Parts.Add(part);
                    added++;
                }
            }
            context.SaveChanges();
        }
        LoadCategories();
        LoadParts();
        ClearForm();
    }

    private void ClearForm()
    {
        PartsGrid.SelectedItem = null;
        PartIDBox.Text = "";
        PartIDBox.IsReadOnly = false;
        CategoryCombo.SelectedItem = null;
        ManufacturerBox.Text = "";
        ModelBox.Text = "";
        SpecsBox.Text = "";
        PriceBox.Text = "";
        QuantityBox.Text = "";
        YearBox.Text = "";
    }
}
