using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    private Inventory inventory;

    [Header("Icons")]
    [SerializeField] private Image[] slotIcons = new Image[3];

    [Header("Selection Frames")]
    [SerializeField] private Image[] slotFrames = new Image[3];

    private void Awake()
    {
        inventory = FindObjectOfType<Inventory>();
    }

    private void OnEnable()
    {
        inventory.OnInventoryChanged += RefreshInventory;
        inventory.OnSelectedSlotChanged += RefreshSelection;
    }

    private void OnDisable()
    {
        inventory.OnInventoryChanged -= RefreshInventory;
        inventory.OnSelectedSlotChanged -= RefreshSelection;
    }

    private void Start()
    {
        RefreshInventory();
        RefreshSelection(inventory.SelectedSlot);
    }

    private void RefreshInventory()
    {
        for (int i = 0; i < slotIcons.Length; i++)
        {
            InventoryItem item = inventory.GetItem(i);

            bool hasItem = item != null && item.Data != null;

            slotIcons[i].enabled = hasItem;

            if (hasItem)
                slotIcons[i].sprite = item.Data.icon;
            else
                slotIcons[i].sprite = null;
        }

        RefreshSelection(inventory.SelectedSlot);
    }

    private void RefreshSelection(int selectedSlot)
    {
        for (int i = 0; i < slotFrames.Length; i++)
        {
            InventoryItem item = inventory.GetItem(i);

            bool showFrame =
                item != null &&
                item.Data != null &&
                i == selectedSlot;

            slotFrames[i].enabled = showFrame;
        }
    }
}