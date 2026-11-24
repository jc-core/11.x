using System;
using System.Collections.Generic;

namespace PcPartsInventory.Models;

public partial class Part
{
    public int PartId { get; set; }

    public int CategoryId { get; set; }

    public string Manufacturer { get; set; } = null!;

    public string ModelName { get; set; } = null!;

    public string? Specs { get; set; }

    public decimal Price { get; set; }

    public int QuantityInStock { get; set; }

    public int? ReleaseYear { get; set; }

    public virtual Category Category { get; set; } = null!;
}
