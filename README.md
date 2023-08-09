# FRML

Work in progress.
Generic unity mod loader made originally for Garfield Kart: Furious Racing.
Should work with any unity game.
Modifies game binary to register callbacks - Do not use with games with anticheat.

## installation

- Place patcher.exe, FRML.dll and Mono.Cecil.dll in the *_Data/Managed folder
- Run the patcher
- Assembly-CSharp.dll is modified after a backup is created
- All exposed classes are listed in the auto-generated "doc" folder
- Mods can be placed in the "Mods" folder 

## uninstall

- Simply delete the patched Assembly-CSharp.dll and rename Assembly-CSharp.dll.bak to Assembly-CSharp.dll

## FRML

Loads mods and provides methods to easily access classes that are exposed by the patcher.

## patcher

Modifies the game binary using Mono, exposing classes private/public members and allowing callbacks to be created by FRML
Auto-generates HTML documentation.

## TestMod

Simple example mod that generates usage of the mod loader.
