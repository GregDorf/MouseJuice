using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class PlayerInteraction : MonoBehaviour
{
    private IInteractable currentObject;

    public Inventory Inventory { get; private set; }

    private void Awake()
    {
        Inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        if (currentObject != null && Input.GetKeyDown(KeyCode.E))
        {
            currentObject.Interact(this);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            currentObject = interactable;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent(out IInteractable interactable))
        {
            if (currentObject == interactable)
                currentObject = null;
        }
    }
}