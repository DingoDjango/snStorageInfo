using Nautilus.Utility;

namespace StorageInfo
{
    internal static class StorageSlotInfo
    {
        // Custom: no vanilla/Nautilus equivalent. Sums the grid area of each item
        // (item count is NOT the same as used slots).
        internal static int GetUsedSlotCount(ItemsContainer container)
        {
            int usedSlots = 0;

            foreach (InventoryItem item in container)
            {
                usedSlots += item.width * item.height;
            }

            return usedSlots;
        }

        // Deferred to Nautilus ItemStorageHelper.GetTotalSlots (static, not an extension method).
        internal static int GetTotalSlotCount(ItemsContainer container)
        {
            return ItemStorageHelper.GetTotalSlots(container);
        }

        internal static int GetFreeSlotCount(ItemsContainer container)
        {
            return ItemStorageHelper.GetTotalSlots(container) - GetUsedSlotCount(container);
        }
    }
}
