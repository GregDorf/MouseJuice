using UnityEngine;

[CreateAssetMenu(menuName = "Gadgets/Effects/Cheese")]
public class CheeseGadget : EffectGadget
{
    [SerializeField]
    private int healAmount = 25;

    public override void Equip(PlayerGadgetController controller, InventoryItem item)
    {
        Debug.Log("Сыр выбран");
    }

    public override void PrimaryAction(PlayerGadgetController controller, InventoryItem item)
    {
        Debug.Log($"Игрок восстановил {healAmount} HP");

        // Позже:
        // controller.PlayerHealth.Heal(healAmount);

        Inventory inventory = controller.GetComponent<Inventory>();

        if (inventory != null)
        {
            inventory.RemoveItem(controller.GetCurrentSlot());
        }
    }

    public override void Unequip(PlayerGadgetController controller, InventoryItem item)
    {
        Debug.Log("Сыр убран");
    }
}