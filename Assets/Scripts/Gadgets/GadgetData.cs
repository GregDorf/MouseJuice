using UnityEngine;

public abstract class GadgetData : ScriptableObject
{
    [Header("Base")]
    public string gadgetName;
    public Sprite icon;

    public abstract void Equip(
        PlayerGadgetController controller,
        InventoryItem item);

    public abstract void PrimaryAction(
        PlayerGadgetController controller,
        InventoryItem item);

    public abstract void Unequip(
        PlayerGadgetController controller,
        InventoryItem item);
}