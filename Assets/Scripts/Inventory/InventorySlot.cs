using System;

[Serializable]
public class InventorySlot
{
    public InventoryItem Item;

    public bool IsEmpty => Item == null;

    public void Clear()
    {
        Item = null;
    }
}