using UnityEngine;

public class GadgetPickup : MonoBehaviour, IInteractable
{
    [SerializeField]
    private GadgetData gadget;

    public void Interact(PlayerInteraction player)
    {
        if (player.Inventory.AddItem(gadget))
        {
            Destroy(gameObject);
        }
    }
}