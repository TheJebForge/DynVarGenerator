# DynVar Generator

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/) that adds a wizard to quickly generate huge amounts of dynamic fields/variables/drivers for slots, blend shapes, bones or anything else

## Installation
1. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
2. Place [DynVarGenerator.dll](https://github.com/TheJebForge/DynVarGenerator/releases/latest/download/DynVarGenerator.dll) into your `nml_mods` folder. This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader installed it will create the folder for you.
3. Start the game. If you want to verify that the mod is working you can check your Neos logs.

## How to use this mod
Wizard can be found in DevTool's Create New -> Editor -> DynVar Generator
![image](screenshot/panel.png)

Panel on left is list of elements that dynamic vars will be generated for. The drop target accepts any world element. Dropping lists into the target will add all elements of the lists.

If "Add all children" is checked, dropping a slot into the drop target will add all children of the slot as well

## Explanation of the format
- ### Element name
  - Slot name if the element is a slot
  - Field name if element is a field
  - Index if element is a field of a list
  - Blend shape name if element is a blend shape under SkinnedMeshRenderer
- ### Slot name
  - Name of parent slot if element is a slot
  - Name of the slot that the component is under if element is a field
- ### Current value/Element name
  - Value of the field (eg. int for fields that hold integers, slot name for fields that hold slots)
  - Empty for anything else that is not a field
- ### Current value slot
  - Slot name of the value (eg. name of the slot that holds the material)
  - Name of parent if field holds a slot reference
  - Empty for anything else that is not a field
- ### Target slot name
  - Slot name of where the dyn vars will be created