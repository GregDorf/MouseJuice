using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private const int SlotCount = 3;

    private InventorySlot[] slots;

    private PlayerGadgetController gadgetController;

    public InventorySlot[] Slots => slots;

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedSlotChanged;

    private int selectedSlot = -1;
    public int SelectedSlot => selectedSlot;

    private void Awake()
    {
        gadgetController = GetComponent<PlayerGadgetController>();

        slots = new InventorySlot[SlotCount];

        for (int i = 0; i < SlotCount; i++)
        {
            slots[i] = new InventorySlot();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            SelectSlot(0);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            SelectSlot(1);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            SelectSlot(2);
    }

    public bool AddItem(GadgetData gadget)
    {
        if (gadget == null)
            return false;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i].IsEmpty)
            {
                slots[i].Item = new InventoryItem(gadget);

                OnInventoryChanged?.Invoke();

                return true;
            }
        }

        Debug.Log("Инвентарь заполнен");
        return false;
    }

    public void RemoveItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        slots[slotIndex].Clear();

        if (selectedSlot == slotIndex)
        {
            selectedSlot = -1;

            gadgetController.UnequipCurrent();

            OnSelectedSlotChanged?.Invoke(selectedSlot);
        }

        OnInventoryChanged?.Invoke();
    }

    public InventoryItem GetItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return null;

        return slots[slotIndex].Item;
    }

    private void SelectSlot(int slotIndex)
    {
        InventoryItem item = GetItem(slotIndex);

        if (item == null)
            return;

        if (selectedSlot == slotIndex)
        {
            selectedSlot = -1;

            gadgetController.UnequipCurrent();

            OnSelectedSlotChanged?.Invoke(selectedSlot);

            return;
        }

        selectedSlot = slotIndex;

        gadgetController.SelectItem(item, slotIndex);

        OnSelectedSlotChanged?.Invoke(selectedSlot);
    }

    public void Clear()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slots[i].Clear();
        }

        selectedSlot = -1;

        gadgetController.UnequipCurrent();

        OnInventoryChanged?.Invoke();
        OnSelectedSlotChanged?.Invoke(selectedSlot);
    }
}