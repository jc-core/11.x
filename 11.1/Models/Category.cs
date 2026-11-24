using System;
using System.Collections.Generic;

namespace PcPartsInventory.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Part> Parts { get; set; } = new List<Part>();
}
