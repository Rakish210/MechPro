# MechPro

MechPro is an AutoCAD 2021 add-in for mechanical installation design. Development starts with the **rectangular ventilation duct** module and will expand incrementally.

## First milestone

- Load as a .NET Framework 4.8 add-in in AutoCAD 2021.
- Provide the `MECHPRO` command.
- Draw a rectangular duct footprint between two picked points using a width input; height is currently collected for information only.
- Keep geometry and AutoCAD interaction separated so later modules can reuse the core.

## Requirements

- AutoCAD 2021 (64-bit)
- .NET Framework 4.8 Developer Pack / targeting pack
- MSBuild (Visual Studio Build Tools is sufficient; the full Visual Studio IDE is not required)
- AutoCAD managed API assemblies from the local AutoCAD installation:
  - `AcMgd.dll`
  - `AcDbMgd.dll`
  - `AcCoreMgd.dll`

The Autodesk assemblies are not included in this repository. They are proprietary components and should be referenced from the licensed local installation.

## Build

Open a Developer Command Prompt or PowerShell with MSBuild available, then run:

```powershell
msbuild .\src\MechPro\MechPro.csproj /p:Configuration=Release
```

If AutoCAD is installed in a non-standard folder, set the `AutodeskManagedPath` MSBuild property to the folder containing the three DLLs:

```powershell
msbuild .\src\MechPro\MechPro.csproj /p:AutodeskManagedPath="D:\\Autodesk\\AutoCAD 2021" /p:Configuration=Release
```

The resulting add-in DLL can be loaded in AutoCAD with `NETLOAD`. The first command is `MECHPRO`.

## Automated build

GitHub Actions builds the project on Windows for pushes to `main`, pull requests, and manual workflow dispatch. The CI build uses Autodesk's official AutoCAD 2021 API NuGet package (`AutoCAD.NET 24.0.0`) when the local AutoCAD installation folder is unavailable. After a successful run, download the `MechPro-AutoCAD2021` artifact from the run's **Actions** page. The artifact is a build output; it still needs to be loaded and tested in AutoCAD 2021.

## Development notes

VS Code can be used as the editor. It does not itself provide the .NET Framework build toolchain; install the .NET Framework 4.8 targeting pack and MSBuild/Visual Studio Build Tools. Do not commit Autodesk DLLs.

## Roadmap

1. Rectangular duct drawing (initial module)
2. Duct properties, layers, and system assignment
3. Fittings and connections
4. Sizing and pressure-loss calculations
5. Further HVAC, plumbing, and heating/cooling modules
