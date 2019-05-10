## HKWE

### !!! Note !!!

The latest update requires Unity 2017.4.10f1 which you can download from the Unity download archive.

Look here for a demo:

https://www.youtube.com/watch?v=8hLyvyG6M4Q

### How to use

* Open the project in the unity editor
* Go to `HKEdit->Open Level` or `HKEdit->Open Scene By Name` for a list of scenes
* Open the levelxxx file in hollow_knight_Data
* Edit the scene
* Any time you add a new sprite into the scene, mark it to be added by pressing ctrl+g on it or going to `HKEdit->Add EditDiffer`
* Go to `HKEdit->Save Level` and save it in the HKWEDiffs folder (see below) as levelxxx where xxx is the number of level. (If the scene was loaded in the list of scenes, it will be in square brackets by the name, otherwise use the original file name)
* In the Hollow Knight root dir, if HKWEDiffs folder does not exist, create it and save it in that folder with the same name as the level you opened
* Delete the other generated files (HKWEDiffs, HKWEDiffs.manifest, levelxxx.manifest)
* Make sure the mod is installed and start the game. The level is automatically patched when loaded

### To-do

- [x] remove black squares by default
- [ ] remove flashes by default
- [ ] tk2d shader support
- [ ] duplication of existing assets
- [ ] add component editing of existing assets
- [ ] assetid rewriting in editor instead of mod for faster loading

### Screenshots

![Kingdom's Edge](https://user-images.githubusercontent.com/12544505/56695718-4ede1d00-66af-11e9-84df-db097371f862.png)
![Fog Canyon](https://user-images.githubusercontent.com/12544505/56695721-51407700-66af-11e9-957d-1ff9ff6483b9.png)