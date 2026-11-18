# MechPro

MechPro is a modular AutoCAD 2021 add-in for mechanical/HVAC design. The current milestone focuses on data-aware 2D rectangular duct routing, fittings, equipment symbols and connection management.

## Current capabilities

- Multi-point rectangular duct routing with a live cursor preview and automatic 90-degree rounded turns.
- Editable duct width, height and sheet thickness (0.1–20 mm; values such as 0.6, 0.8, 6 and 8 mm are supported).
- Material and duct-system selection.
- A dedicated AutoCAD layer and color for each duct system, driven by the external catalog.
- A duct label placed inside the footprint, for example 400x200 / 0.6.
- Persistent object metadata stored in each primary entity's extension dictionary/Xrecord: object ID, system, material, dimensions, thickness, route coordinates, layer, label handle and connector ports.
- MECHPROEDIT and MECHPROAPPLY to load and update the selected duct's properties and regenerate its 2D outline.
- Catalog-driven fitting choices: 90-degree elbow, pants/Y branch, reducer and T branch.
- Catalog-driven 2D HVAC equipment symbols: damper, fire damper, grille, AHU, fan, VAV, silencer, filter and coil.
- Connector port metadata, automatic proximity-based network audit and manual port connection between selected MechPro objects.
- Four palette sections: Duct, Fittings, Equipment and Network.

The current fitting and equipment footprints are 2D schematic geometry. They are not yet fabrication-ready sheet-metal assemblies or 3D solids. The airflow, velocity and pressure-loss calculation engine, AI recommendations, 3D modelling and report generation are intentionally deferred.

## Dynamic catalog

On first load, MechPro copies the embedded starter catalog to:

    %AppData%\MechPro\catalog.xml

The XML catalog is the runtime source for:

- Materials
- Duct systems, layer names and layer colors
- Fitting definitions
- Equipment definitions

To add a material, system or equipment entry, edit the XML file and run MECHPRORELOADCATALOG (or use the Network palette button). Existing drawing entities retain their saved IDs and technical properties. A system definition controls the AutoCAD layer and true color used for new entities; editing an existing duct and applying its properties moves it to the newly configured layer/color.

Example system entry:

~~~xml
<System id="return" name="Dönüş havası"
        kind="AirReturn" layer="MP_HAVA_DONUS" color="#B71C1C" />
~~~

The starter catalog contains two systems: supply air (navy) and exhaust air (red). Additional systems such as fresh air, return air, smoke exhaust and pressurization can be added through the catalog without changing the palette code.

Catalog kind values select a geometry handler. Catalog entries and labels are data-driven; a new fitting geometry family that has no existing handler still requires a corresponding geometry module to be added to the project. This keeps catalogue administration separate from the reusable geometry implementation.

## AutoCAD commands

| Command | Purpose |
|---|---|
| MECHPRO | Open the MechPro palette |
| MECHPRODRAW | Draw a data-aware rectangular duct route |
| MECHPROEDIT | Select a MechPro duct and load its properties into the palette |
| MECHPROAPPLY | Apply changed duct parameters and rebuild its 2D outline |
| MECHPROFITTING | Place the selected catalog fitting |
| MECHPROEQUIPMENT | Place the selected equipment symbol |
| MECHPRONETWORK | Audit MechPro objects, connector ports and network groups |
| MECHPROCONNECT | Assign a common node ID to the closest ports of two selected MechPro objects |
| MECHPRORELOADCATALOG | Reload the user-editable XML catalog |

## Notes on network connections

Each duct, fitting or equipment primary entity stores connector coordinates and node IDs. Network audit checks shared node IDs, connector proximity and equipment/fitting ports that lie on a duct centerline. Manual connection uses the closest port pair and the tolerance configured in the Network palette.

This is an initial 2D connectivity graph. It does not yet perform flow balancing, pressure calculations, topology validation against engineering codes or automatic fitting insertion into a selected duct run.

## Requirements

- AutoCAD 2021 (64-bit)
- .NET Framework 4.8 Developer Pack / targeting pack
- MSBuild (Visual Studio Build Tools is sufficient)
- AutoCAD managed API assemblies from the licensed local installation:
  - AcMgd.dll
  - AcDbMgd.dll
  - AcCoreMgd.dll

Autodesk assemblies are not included in this repository. They are referenced from the local AutoCAD installation when available.

## Build

~~~powershell
msbuild .\src\MechPro\MechPro.csproj /restore /m /p:Configuration=Release /p:Platform=x64
~~~

If AutoCAD is installed in a non-standard folder, set AutodeskManagedPath:

~~~powershell
msbuild .\src\MechPro\MechPro.csproj /restore /p:AutodeskManagedPath="D:\Autodesk\AutoCAD 2021" /p:Configuration=Release /p:Platform=x64
~~~

The resulting MechPro.dll can be loaded in AutoCAD with NETLOAD. The palette is opened with MECHPRO.

## Automated build

GitHub Actions builds the add-in on Windows for pushes to main, pull requests and manual dispatch. The CI build uses the AutoCAD 2021 API NuGet reference package when a local AutoCAD installation is not available. Successful workflow runs publish the MechPro-AutoCAD2021 artifact for 14 days.

## Roadmap

1. Continue validating 2D routes, fitting footprints, equipment symbols and network connectivity in AutoCAD 2021.
2. Add editing/connection grips, automatic branch insertion and stronger topology validation.
3. Add engineering calculation modules for airflow, velocity, friction and local pressure losses.
4. Add AI explanations and design alternatives grounded in the deterministic calculation engine.
5. Add 3D duct/equipment modelling, schedules and reports.
