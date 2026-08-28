using UnityEngine;

public abstract class WeaponGadget : GadgetData
{
    public GameObject prefab;
    public int maxAmmo;
    public int damage;
    public float cooldown;

    public override void Equip(
        PlayerGadgetController controller, InventoryItem item)
    {
        GameObject obj = Instantiate(prefab, controller.HoldPoint.position, controller.HoldPoint.rotation, controller.HoldPoint);

        controller.SetCurrentObject(obj);
    }

    public override void Unequip(PlayerGadgetController controller, InventoryItem item)
    {
        controller.DestroyCurrentObject();
    }
}