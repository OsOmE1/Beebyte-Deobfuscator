"Obfuscation, however, is not reversed." - https://www.beebyte.co.uk/

# Beebyte-Deobfusctator 
A plugin for Il2CppInspector to deobfuscate types

## Usage

#### Important: For this plugin to work you have to have an un-obfuscated or less obfuscated version of the game.

Select one of the options from the dropdown menu  
![](https://i.imgur.com/f2QnEdH.png)  

Its common for mobile games to be compiled with the gcc compiler while msvc is used for windows this causes compatability issues so you cannot use this plugin across compilers.  
This feature may come in the future.

If you select Il2Cpp make sure to select a un-obfuscated GameAssembly.dll and global-metadata.dat or an android Package file.  
If you select Mono make sure to select an un-obfuscated mono compiled Assembly-CSharp.dll located in the games Managed folder.  

The naming regex should correspond to the naming scheme of the Beebyte configuration.  
For example:  
NJGFKJNAEMN: `^[A-Z]{11}$` (Among Us)  
u091Eu091Fu0927u0924u0929u0927u0920u0928u0926u091Cu091D: `^[\u0900-\u097F]{11}$` (Phasmaphobia)  
UOiWPrj: `^[A-Za-\u0082\[\]\^]{7}$` (Garena Free Fire)

## Exports
Theres two different kinds of exports:  
Plain Text creates a output.txt file with every translation formatted in this way: `ObfuscatedName/DeobfuscatedName`.  
Classes for [Il2CppTranslator](https://github.com/OsOmE1/Il2CppTranslator) creates classes you can use in your own plugin project.  
If you use export Classes you can set your plugin name and it will create all the classes with the namespace `YourPluginName.Translators`.
