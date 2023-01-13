# **Storage Info for Subnautica**

### **Description:**

In Subnautica, only empty containers relay their status to the player. With this mod containers will always show if they are:
- Empty
- Full
- Have a certain amount of items, but aren't full

### **Installation:**

1. Install [BepInEx for Subnautica](https://www.nexusmods.com/subnautica/mods/1108)
2. Install [SMLHelper (Modding Helper)](https://www.nexusmods.com/subnautica/mods/113)
3. Download the latest zip file from the [Files tab](https://www.nexusmods.com/subnautica/mods/229/?tab=files)
4. Unzip the contents of the zip to the game's main directory (where Subnautica.exe can be found)

### **(Optional) Translation:**

1. Navigate to *...\Subnautica\BepInEx\plugins\StorageInfo\Languages*
2. Copy *English.json* and change the file name to match your language
    > Valid language names are found in *...\Subnautica\Subnautica_Data\StreamingAssets\SNUnmanagedData\LanguageFiles*
3. Translate the file. Do not touch the keys ("ContainerFull"), only the values ("full")
4. Share the file with me on GitHub or in a Nexus private message

### **FAQ:**

- **Q. Does this mod support the latest Subnautica update?**
- A. Tested on Subnautica version Dec-2022 71137 (Living Large update)
- **Q. Is this mod safe to add or remove from an existing save?**
- A. Yes
- **Q. Does this mod have any known conflicts?**
- A. No, unless some other mod patches *StorageContainer.OnHandHover* and doesn't play nice

[Source code can be found here.](https://github.com/DingoDjango/snStorageInfo)

### **Credits:**
- Powered by [Harmony](https://github.com/pardeike/Harmony)
- Using [SMLHelper](https://www.nexusmods.com/subnautica/mods/113)
- Translations by [acidscorch](https://github.com/acidscorch), [mstislavovich](https://forums.nexusmods.com/index.php?/user/23416669-mstislavovich/), [Yanuut](https://github.com/Yanuut), [Zemogiter](https://github.com/Zemogiter)
