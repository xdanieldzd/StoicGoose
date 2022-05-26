# StoicGoose

<img src="Goose-Logo-Web.png" alt="Ze Goose" align="right" width="128">

![GitHub release (latest by date)](https://img.shields.io/github/v/release/xdanieldzd/StoicGoose)
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/xdanieldzd/StoicGoose/Build)
![GitHub](https://img.shields.io/github/license/xdanieldzd/StoicGoose)

StoicGoose is a work-in-progress Bandai WonderSwan and WonderSwan Color emulator. It is written in C# via Visual Studio Community 2022 under Windows 10 Pro 21H2, and uses .NET 6.0 along with the following NuGet packages:

* [OpenTK](https://www.nuget.org/packages/OpenTK) 4.7.2 (for OpenGL rendering, OpenAL sound, etc.)
* [OpenTK.WinForms](https://www.nuget.org/packages/OpenTK.WinForms) 4.0.0-pre.6 (for WinForms OpenGL control)
* [Newtonsoft.Json](https://www.nuget.org/packages/Newtonsoft.Json) 13.0.1 (for configuration, cheats and breakpoint storage)
* [GitInfo](https://www.nuget.org/packages/GitInfo) 2.2.0 (for versioning information)
* [ImGui.NET](https://www.nuget.org/packages/ImGui.NET) 1.87.3 (for debugger UI)
* [Iced](https://www.nuget.org/packages/Iced) 1.17.0 (for x86 disassembly)
* [Microsoft.CodeAnalysis.CSharp.Scripting](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp.Scripting) 4.2.0 (for breakpoint support)

Two flavors are available: a Windows Forms application meant for playing games (StoicGoose), and a debugger with a user interface created with Dear ImGui (StoicGoose.GLWindow).

## Requirements

* A GPU supporting OpenGL 4.6 (ex. Nvidia GeForce 400 series or later, Radeon HD 7000 series or later) and appropriate drivers
* [.NET 6.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0/runtime)
* A working implementation of OpenAL
  * If the emulator fails to run because **openal32.dll** is missing, get the [OpenAL Soft](https://www.openal-soft.org/) binaries, extract the correct DLL file to the emulator directory and name it openal32.dll
* Optionally, copies of the WonderSwan and WonderSwan Color bootstrap ROMs (supported but not required)

## Screenshots (v000.6 WIP)
WonderSwan and WonderSwan Color Bootstraps, using Dot-Matrix and Dot-Matrix Color shaders:

<img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-Bootstrap-Logo.png" alt="Screenshot Bootstraps 1" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-Bootstrap-Menu.png" alt="Screenshot Bootstraps 2" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-Bootstrap-Logo.png" alt="Screenshot Bootstraps 3" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-Bootstrap-Menu.png" alt="Screenshot Bootstraps 4" width="50%">

Various WonderSwan games, using Dot-Matrix shader:

<img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-DigiAnodeTamer.png" alt="Screenshot WS Games 1" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-FinalLap2000.png" alt="Screenshot WS Games 2" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-MedarotKabuto.png" alt="Screenshot WS Games 3" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WS-RockmanForte.png" alt="Screenshot WS Games 4" width="50%">

Various WonderSwan Color games, using Dot-Matrix Color shader:

<img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-DigiD1Tamers.png" alt="Screenshot WSC Games 1" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-FinalFantasy.png" alt="Screenshot WSC Games 2" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-MrDriller.png" alt="Screenshot WSC Games 3" width="50%"><img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/WSC-Riviera.png" alt="Screenshot WSC Games 4" width="50%">
<!--
Various debugging features (**outdated!**):

<img src="https://raw.githubusercontent.com/xdanieldzd/StoicGoose/master/Screenshots/Debugger.png" alt="Screenshot Debugger">
-->
## Acknowledgements & Attribution
* The XML data files in `Assets\No-Intro` were created by the [No-Intro](http://www.no-intro.org) project; see the [DAT-o-MATIC website](https://datomatic.no-intro.org) for official downloads.
* The file `WS-Icon.ico` is derived from "[WonderSwan-Black-Left.jpg](https://en.wikipedia.org/wiki/File:WonderSwan-Black-Left.jpg)" on [Wikipedia](https://en.wikipedia.org), in revision from 25 May 2014 by [Evan-Amos](https://commons.wikimedia.org/wiki/User:Evan-Amos), used as public domain.
* My personal thanks and gratitude to the late Near, who has always been encouraging and inspiring on my amateur emulator developer journey. They are sorely missed.
