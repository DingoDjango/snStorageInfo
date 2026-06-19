namespace StorageInfo
{
    internal static class StorageSlotInfo
    {
        internal static int GetUsedSlotCount(ItemsContainer container)
        {
            int usedSlots = 0;

            foreach (InventoryItem item in container)
            {
                usedSlots += item.width * item.height;
            }

            return usedSlots;
        }

        internal static int GetTotalSlotCount(ItemsContainer container)
        {
            return container.sizeX * container.sizeY;
        }

        internal static int GetFreeSlotCount(ItemsContainer container)
        {
            return GetTotalSlotCount(container) - GetUsedSlotCount(container);
        }
    }
}
