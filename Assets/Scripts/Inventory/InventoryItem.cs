using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public GadgetData Data;

    private int currentAmmo;
    public int stackCount = 1;

    public InventoryItem(GadgetData data)
    {
        Data = data;

        if (data is WeaponGadget weapon)
        {
            currentAmmo = weapon.maxAmmo;
        }
    }
}