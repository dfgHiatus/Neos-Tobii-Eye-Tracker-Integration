# Neos-Tobii-Eye-Tracker-Integration 

A [NeosModLoader](https://github.com/zkxs/NeosModLoader) mod for [Neos VR](https://neos.com/)  
Integrates the Vive Eye Devkit's Eye tracking into NeosVR.

## Usage
0. Install [NeosModLoader](https://github.com/zkxs/NeosModLoader).
1. Place the DLL under "nml_mods". This folder should be at `C:\Program Files (x86)\Steam\steamapps\common\NeosVR\nml_mods` for a default install. You can create it if it's missing, or if you launch the game once with NeosModLoader.
2. Additionally, unzip the archive into the Neos base directory, the same one with Neos.exe
3. Start the game!

If you want to verify that the mod is working you can check your Neos logs, or create an EmptyObject with an AvatarRawEyeData/AvatarRawMouthData Component (Found under Users -> Common Avatar System -> Face -> AvatarRawEyeData/AvatarRawMouthData).

Thanks to those who helped me test this!
