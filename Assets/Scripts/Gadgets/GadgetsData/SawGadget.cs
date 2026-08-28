using UnityEngine;

[CreateAssetMenu(menuName = "Gadgets/Weapons/Saw")]
public class SawGadget : WeaponGadget
{
    public override void PrimaryAction(PlayerGadgetController controller, InventoryItem item)
    {
        if (controller.CurrentObject == null)
            return;

        Debug.Log("Пила наносит урон");
    }
}
