namespace IdleGatherWebGame.Models;

public enum ItemCategory
{
    Currencies,
    Foods,
    Resources,
    Materials,
    Other
}

public enum ItemRarity { Common, Uncommon, Rare, Epic, Legendary }

public record InventoryItemVM(
    string Id, string Name, string IconPath,
    long Amount, ItemCategory Category, ItemRarity Rarity = ItemRarity.Common
);
