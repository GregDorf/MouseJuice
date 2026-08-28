using UnityEngine;

[CreateAssetMenu(menuName = "Gadgets/Throwable/Grenade")]
public class GrenadeGadget : ThrowableGadget
{
    [Header("Grenade")]
    [SerializeField] private GameObject grenadePrefab;
    [SerializeField] private float throwForce = 10f;

    public override void Equip(PlayerGadgetController controller, InventoryItem item)
    {
        GameObject grenade = Instantiate(
            grenadePrefab,
            controller.HoldPoint.position,
            controller.HoldPoint.rotation,
            controller.HoldPoint);

        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.simulated = false;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        controller.SetCurrentObject(grenade);
    }

    public override void PrimaryAction(PlayerGadgetController controller, InventoryItem item)
    {
        GameObject grenade = controller.CurrentObject;

        if (grenade == null)
            return;

        grenade.transform.parent = null;

        Rigidbody2D rb = grenade.GetComponent<Rigidbody2D>();

        if (rb != null)
        {
            rb.simulated = true;

            Vector3 mouse =
                Camera.main.ScreenToWorldPoint(Input.mousePosition);

            mouse.z = 0;

            Vector2 direction =
                (mouse - grenade.transform.position).normalized;

            rb.AddForce(direction * throwForce, ForceMode2D.Impulse);
        }

        controller.SetCurrentObject(null);
    }

    public override void Unequip(PlayerGadgetController controller, InventoryItem item)
    {
        controller.DestroyCurrentObject();
    }
}