using UnityEngine;

[RequireComponent(typeof(Inventory))]
public class PlayerGadgetController : MonoBehaviour
{
    [Header("Точка, где появляется экипированный гаджет")]
    [SerializeField] private Transform holdPoint;

    private Inventory inventory;

    private InventoryItem currentItem;
    private int currentSlot = -1;

    // Активный объект гаджета (оружие в руках, граната перед броском и т.п.)
    private GameObject currentObject;

    public Transform HoldPoint => holdPoint;

    public GameObject CurrentObject => currentObject;

    private void Awake()
    {
        inventory = GetComponent<Inventory>();
    }

    private void Update()
    {
        RotateCurrentObject();

        // Использование выбранного гаджета
        if (Input.GetMouseButtonDown(1))
        {
            UseCurrentItem();
        }
    }

    private void RotateCurrentObject()
    {
        if (currentObject == null)
            return;

        Vector3 mouse = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouse.z = 0;

        Vector2 direction = mouse - currentObject.transform.position;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        currentObject.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    // Выбрать предмет из указанного слота.
    public void SelectItem(InventoryItem item, int slotIndex)
    {
        // Уже выбран
        if (currentSlot == slotIndex)
            return;

        // Снимаем предыдущий
        if (currentItem != null)
        {
            currentItem.Data.Unequip(this, currentItem);
        }

        currentItem = item;
        currentSlot = slotIndex;

        // Экипируем новый
        currentItem.Data.Equip(this, currentItem);
    }

    // Использовать текущий предмет.
    public void UseCurrentItem()
    {
        if (currentItem == null)
            return;

        currentItem.Data.PrimaryAction(this, currentItem);
    }

    // Снять текущий предмет.
    public void UnequipCurrent()
    {
        if (currentItem == null)
            return;

        currentItem.Data.Unequip(this, currentItem);

        currentItem = null;
        currentSlot = -1;
    }

    // Назначить активный объект гаджета.
    public void SetCurrentObject(GameObject obj)
    {
        currentObject = obj;
    }

    // Удалить активный объект.
    public void DestroyCurrentObject()
    {
        if (currentObject != null)
        {
            Destroy(currentObject);
            currentObject = null;
        }
    }

    public InventoryItem GetCurrentItem()
    {
        return currentItem;
    }

    public int GetCurrentSlot()
    {
        return currentSlot;
    }
}