using UnityEngine;

public abstract class EffectGadget : GadgetData
{
    public GameObject effectPrefab;
    public float duration;

    public override void Equip(
        PlayerGadgetController controller,
        InventoryItem item)
    {

    }

    public override void Unequip(
        PlayerGadgetController controller,
        InventoryItem item)
    {

    }
}