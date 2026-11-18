using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using MechPro.Domain;
using MechPro.Geometry;
using MechPro.UI;
using Polyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MechPro.Commands
{
    public class MechProCommands
    {
        private const double MinimumLength = 1e-6;

        [CommandMethod("MECHPRO")]
        public void ShowMechPro()
        {
            try
            {
                MechProPalette.Show();
            }
            catch (System.Exception ex)
            {
                Application.DocumentManager.MdiActiveDocument.Editor.WriteMessage(
                    "\nMechPro paneli açılamadı: " + ex.Message);
            }
        }

        [CommandMethod("MECHPRODRAW")]
        public void DrawRectangularDuct()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            Database database = document.Database;

            try
            {
                CatalogEntry system = CatalogService.FindById(
                    CatalogService.Current.Systems, MechProPalette.SystemId);
                CatalogEntry material = CatalogService.FindById(
                    CatalogService.Current.Materials, MechProPalette.MaterialId);
                if (system == null || material == null)
                {
                    editor.WriteMessage("\nMechPro: Malzeme veya kanal sistemi katalogda bulunamadı.");
                    return;
                }

                double width = MechProPalette.DuctWidth;
                double height = MechProPalette.DuctHeight;
                double thickness = MechProPalette.Thickness;

                PromptPointResult startResult = editor.GetPoint(
                    new PromptPointOptions("\nKanal güzergâhının başlangıç noktasını seçin: "));
                if (startResult.Status != PromptStatus.OK)
                    return;

                List<Point3d> route = new List<Point3d> { startResult.Value };
                editor.WriteMessage(
                    "\nMechPro: İmleç kanal konturunu canlı takip eder. Dönüş noktalarını seçin; Enter ile bitirin.");

                while (true)
                {
                    RoutePointJig jig = new RoutePointJig(
                        route, width, width, startResult.Value.Z);
                    PromptResult dragResult = editor.Drag(jig);

                    if (jig.FinishRequested)
                        break;
                    if (dragResult.Status != PromptStatus.OK)
                        return;
                    if (!jig.HasCandidate)
                    {
                        editor.WriteMessage("\nGeçersiz yön veya kısa segment. Noktayı yeniden seçin.");
                        continue;
                    }

                    List<Point3d> testRoute = new List<Point3d>(route)
                    {
                        jig.CandidatePoint
                    };
                    using (Polyline test = DuctGeometry.BuildOutline(
                        testRoute, width, width, startResult.Value.Z))
                    {
                        if (test == null)
                        {
                            editor.WriteMessage(
                                "\nDirsek için yeterli mesafe yok veya dönüş 90° değil. Güzergâhı uzatın.");
                            continue;
                        }
                    }

                    route.Add(jig.CandidatePoint);
                }

                if (route.Count < 2)
                {
                    editor.WriteMessage("\nKanal oluşturmak için en az iki nokta seçin.");
                    return;
                }

                using (Polyline outline = DuctGeometry.BuildOutline(
                    route, width, width, startResult.Value.Z))
                {
                    if (outline == null)
                    {
                        editor.WriteMessage("\nKanal geometrisi oluşturulamadı.");
                        return;
                    }

                    using (Transaction transaction = database.TransactionManager.StartTransaction())
                    {
                        ObjectId layerId = EnsureLayer(database, transaction, system);
                        outline.SetDatabaseDefaults(database);
                        outline.LayerId = layerId;
                        outline.ColorIndex = 256;

                        BlockTableRecord modelSpace = GetModelSpace(database, transaction);
                        modelSpace.AppendEntity(outline);
                        transaction.AddNewlyCreatedDBObject(outline, true);

                        LabelPlacement placement = DuctGeometry.GetLabelPlacement(
                            route, width, height);
                        DBText label = CreateText(
                            database, layerId,
                            string.Format(CultureInfo.InvariantCulture,
                                "{0:0.##}x{1:0.##} / {2:0.##}", width, height, thickness),
                            placement.Position, placement.Height, placement.Rotation);
                        modelSpace.AppendEntity(label);
                        transaction.AddNewlyCreatedDBObject(label, true);
                        label.AdjustAlignment(database);

                        MechProObjectData data = new MechProObjectData
                        {
                            Kind = "Duct",
                            Subtype = "Rectangular",
                            DisplayName = "Dikdörtgen kanal",
                            LayerName = system.Layer,
                            SystemId = system.Id,
                            MaterialId = material.Id,
                            Width = width,
                            Height = height,
                            Thickness = thickness,
                            LabelHandle = label.Handle.ToString()
                        };
                        data.Route.AddRange(route);
                        data.Connectors.Add(new MechProConnector(route[0]));
                        data.Connectors.Add(new MechProConnector(route[route.Count - 1]));
                        CadObjectDataStore.Write(outline, data, transaction);

                        transaction.Commit();
                    }
                }

                editor.WriteMessage(
                    "\nMechPro: {0} x {1} mm kanal oluşturuldu. Sac: {2:0.##} mm, Malzeme: {3}, Sistem: {4}.",
                    width, height, thickness, material.Name, system.Name);
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage("\nMechPro kanal çizim hatası: " + ex.Message);
            }
        }

        [CommandMethod("MECHPROEDIT")]
        public void EditSelectedDuct()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            PromptEntityOptions options = new PromptEntityOptions(
                "\nÖzelliklerini düzenlemek istediğiniz MechPro kanalını seçin: ");
            options.SetRejectMessage("\nBir kanal polyline nesnesi seçmelisiniz.");
            options.AddAllowedClass(typeof(Polyline), false);

            PromptEntityResult result = document.Editor.GetEntity(options);
            if (result.Status != PromptStatus.OK)
                return;

            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                Entity entity = transaction.GetObject(result.ObjectId, OpenMode.ForRead) as Entity;
                MechProObjectData data = CadObjectDataStore.Read(entity, transaction);
                if (data == null || data.Kind != "Duct")
                {
                    document.Editor.WriteMessage(
                        "\nSeçilen nesnede MechPro kanal verisi bulunamadı.");
                    return;
                }

                MechProPalette.Show();
                MechProPalette.LoadDuctForEdit(data, entity.Handle.ToString());
                document.Editor.WriteMessage(
                    "\nMechPro: Kanal özellikleri palete yüklendi. Değerleri düzenleyip 'SEÇİLİ KANALI GÜNCELLE' düğmesine basın.");
                transaction.Commit();
            }
        }

        [CommandMethod("MECHPROAPPLY")]
        public void ApplyDuctChanges()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null || string.IsNullOrWhiteSpace(MechProPalette.EditingHandle))
                return;

            Editor editor = document.Editor;
            Database database = document.Database;

            try
            {
                Handle handle = ParseHandle(MechProPalette.EditingHandle);
                ObjectId objectId = database.GetObjectId(false, handle, 0);

                CatalogEntry system = CatalogService.FindById(
                    CatalogService.Current.Systems, MechProPalette.SystemId);
                CatalogEntry material = CatalogService.FindById(
                    CatalogService.Current.Materials, MechProPalette.MaterialId);
                if (system == null || material == null)
                {
                    editor.WriteMessage("\nMechPro: Seçilen malzeme veya sistem bulunamadı.");
                    return;
                }

                using (Transaction transaction = database.TransactionManager.StartTransaction())
                {
                    Polyline entity = transaction.GetObject(objectId, OpenMode.ForWrite) as Polyline;
                    if (entity == null)
                    {
                        editor.WriteMessage("\nDüzenlenecek kanal bulunamadı. MECHPROEDIT komutunu yeniden çalıştırın.");
                        return;
                    }

                    MechProObjectData data = CadObjectDataStore.Read(entity, transaction);
                    if (data == null || data.Kind != "Duct" || data.Route.Count < 2)
                    {
                        editor.WriteMessage("\nSeçilen nesnenin MechPro kanal verisi eksik.");
                        return;
                    }

                    double width = MechProPalette.DuctWidth;
                    double height = MechProPalette.DuctHeight;
                    double thickness = MechProPalette.Thickness;

                    using (Polyline geometry = DuctGeometry.BuildOutline(
                        data.Route, width, width, data.Route[0].Z))
                    {
                        if (geometry == null)
                        {
                            editor.WriteMessage("\nYeni ölçülerle kanal geometrisi oluşturulamadı.");
                            return;
                        }

                        while (entity.NumberOfVertices > 0)
                            entity.RemoveVertexAt(entity.NumberOfVertices - 1);

                        for (int i = 0; i < geometry.NumberOfVertices; i++)
                        {
                            entity.AddVertexAt(i, geometry.GetPoint2dAt(i),
                                geometry.GetBulgeAt(i), 0.0, 0.0);
                        }
                        entity.Closed = true;
                        entity.Elevation = geometry.Elevation;
                        entity.LayerId = EnsureLayer(database, transaction, system);
                        entity.ColorIndex = 256;
                    }

                    data.Width = width;
                    data.Height = height;
                    data.Thickness = thickness;
                    data.SystemId = system.Id;
                    data.MaterialId = material.Id;
                    data.LayerName = system.Layer;

                    ObjectId labelId = ObjectId.Null;
                    if (!string.IsNullOrWhiteSpace(data.LabelHandle))
                    {
                        try { labelId = database.GetObjectId(false, ParseHandle(data.LabelHandle), 0); }
                        catch { labelId = ObjectId.Null; }
                    }

                    LabelPlacement placement = DuctGeometry.GetLabelPlacement(
                        data.Route, width, height);
                    string labelText = string.Format(CultureInfo.InvariantCulture,
                        "{0:0.##}x{1:0.##} / {2:0.##}", width, height, thickness);

                    DBText label = null;
                    if (!labelId.IsNull && labelId.IsValid)
                        label = transaction.GetObject(labelId, OpenMode.ForWrite, false) as DBText;

                    if (label == null)
                    {
                        BlockTableRecord modelSpace = GetModelSpace(database, transaction);
                        label = CreateText(database, entity.LayerId, labelText,
                            placement.Position, placement.Height, placement.Rotation);
                        modelSpace.AppendEntity(label);
                        transaction.AddNewlyCreatedDBObject(label, true);
                        data.LabelHandle = label.Handle.ToString();
                    }
                    else
                    {
                        label.TextString = labelText;
                        label.Position = placement.Position;
                        label.AlignmentPoint = placement.Position;
                        label.Height = placement.Height;
                        label.Rotation = placement.Rotation;
                        label.LayerId = entity.LayerId;
                        label.AdjustAlignment(database);
                    }

                    CadObjectDataStore.Write(entity, data, transaction);
                    transaction.Commit();
                }

                MechProPalette.ClearEditMode();
                editor.WriteMessage("\nMechPro: Kanal özellikleri ve geometri güncellendi.");
            }
            catch (System.Exception ex)
            {
                editor.WriteMessage("\nMechPro kanal güncelleme hatası: " + ex.Message);
            }
        }

        [CommandMethod("MECHPROFITTING")]
        public void PlaceFitting()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            Database database = document.Database;

            CatalogEntry definition = CatalogService.FindById(
                CatalogService.Current.Fittings, MechProPalette.FittingId);
            if (definition == null)
            {
                editor.WriteMessage("\nMechPro: Bağlantı elemanı katalogda bulunamadı.");
                return;
            }

            CatalogEntry system = CatalogService.FindById(
                CatalogService.Current.Systems, MechProPalette.SystemId);
            CatalogEntry material = CatalogService.FindById(
                CatalogService.Current.Materials, MechProPalette.MaterialId);
            if (system == null || material == null)
            {
                editor.WriteMessage("\nÖnce geçerli kanal sistemi ve malzeme seçin.");
                return;
            }

            PromptPointResult pointResult = editor.GetPoint(
                new PromptPointOptions("\nBağlantı elemanının referans noktasını seçin: "));
            if (pointResult.Status != PromptStatus.OK)
                return;

            PromptPointOptions directionOptions = new PromptPointOptions(
                "\nBağlantı yönünü belirleyin: ");
            directionOptions.BasePoint = pointResult.Value;
            directionOptions.UseBasePoint = true;
            PromptPointResult directionResult = editor.GetPoint(directionOptions);
            if (directionResult.Status != PromptStatus.OK)
                return;

            Vector2d direction = new Vector2d(
                directionResult.Value.X - pointResult.Value.X,
                directionResult.Value.Y - pointResult.Value.Y);
            if (direction.Length < MinimumLength)
            {
                editor.WriteMessage("\nGeçerli bir yön seçilmedi.");
                return;
            }
            direction = direction.GetNormal();

            List<Polyline> shapes = new List<Polyline>();
            List<Point3d> connectors = new List<Point3d>();
            Point3d origin = pointResult.Value;
            double width = MechProPalette.DuctWidth;
            double elevation = origin.Z;

            string kind = (definition.Kind ?? string.Empty).ToUpperInvariant();
            if (kind == "ELBOW90")
            {
                Vector2d normal = new Vector2d(-direction.Y, direction.X);
                List<Point3d> route = new List<Point3d>
                {
                    Move(origin, direction, -2.5 * width),
                    origin,
                    Move(origin, normal, 2.5 * width)
                };
                Polyline elbow = DuctGeometry.BuildOutline(route, width, width, elevation);
                if (elbow != null) shapes.Add(elbow);
                connectors.Add(route[0]);
                connectors.Add(route[2]);
            }
            else if (kind == "PANTS")
            {
                double half = width / 2.0;
                double branchLength = 2.8 * width;
                Vector2d mainNormal = new Vector2d(-direction.Y, direction.X);
                Vector2d branchA = direction.RotateBy(Math.PI / 6.0);
                Vector2d branchB = direction.RotateBy(-Math.PI / 6.0);
                Point3d inlet = Move(origin, direction, -2.2 * width);
                Point3d outletA = Move(origin, branchA, branchLength);
                Point3d outletB = Move(origin, branchB, branchLength);

                Vector2d normalA = new Vector2d(-branchA.Y, branchA.X);
                Vector2d normalB = new Vector2d(-branchB.Y, branchB.X);
                List<Point3d> pantsPoints = new List<Point3d>
                {
                    DuctGeometry.Offset(origin, direction, -2.2 * width, -half),
                    DuctGeometry.Offset(origin, direction, -0.25 * width, -half),
                    DuctGeometry.Offset(origin, branchB, 0, -half),
                    DuctGeometry.Offset(origin, branchB, branchLength, -half),
                    DuctGeometry.Offset(origin, branchB, branchLength, half),
                    DuctGeometry.Offset(origin, branchB, 0, half),
                    DuctGeometry.Offset(origin, branchA, 0, -half),
                    DuctGeometry.Offset(origin, branchA, branchLength, -half),
                    DuctGeometry.Offset(origin, branchA, branchLength, half),
                    DuctGeometry.Offset(origin, branchA, 0, half),
                    DuctGeometry.Offset(origin, direction, -0.25 * width, half),
                    DuctGeometry.Offset(origin, direction, -2.2 * width, half)
                };
                Polyline pants = DuctGeometry.BuildPolygon(pantsPoints, elevation);
                if (pants != null) shapes.Add(pants);
                connectors.Add(inlet);
                connectors.Add(outletA);
                connectors.Add(outletB);
            }
            else if (kind == "TEE")
            {
                Vector2d normal = new Vector2d(-direction.Y, direction.X);
                Point3d mainStart = Move(origin, direction, -2.5 * width);
                Point3d mainEnd = Move(origin, direction, 2.5 * width);
                Point3d branchEnd = Move(origin, normal, 2.5 * width);

                double half = width / 2.0;
                List<Point3d> teePoints = new List<Point3d>
                {
                    DuctGeometry.Offset(origin, direction, -2.5 * width, -half),
                    DuctGeometry.Offset(origin, direction,  2.5 * width, -half),
                    DuctGeometry.Offset(origin, direction,  2.5 * width,  half),
                    DuctGeometry.Offset(origin, direction,  half, half),
                    DuctGeometry.Offset(origin, direction,  half, 2.5 * width),
                    DuctGeometry.Offset(origin, direction, -half, 2.5 * width),
                    DuctGeometry.Offset(origin, direction, -half, half),
                    DuctGeometry.Offset(origin, direction, -2.5 * width, half)
                };
                Polyline tee = DuctGeometry.BuildPolygon(teePoints, elevation);
                if (tee != null) shapes.Add(tee);
                connectors.Add(mainStart);
                connectors.Add(mainEnd);
                connectors.Add(branchEnd);
            }
            else if (kind == "REDUCER")
            {
                Point3d start = origin;
                double outletWidth = MechProPalette.ReducerOutletWidth;
                double length = Math.Max(width * 1.5, Math.Abs(width - outletWidth) * 3.0);
                Polyline reducer = DuctGeometry.BuildReducer(
                    start, direction, width, outletWidth, length, elevation);
                if (reducer != null) shapes.Add(reducer);
                connectors.Add(start);
                connectors.Add(Move(start, direction, length));
            }
            else
            {
                editor.WriteMessage("\nBu bağlantı türü için henüz çizim üreticisi tanımlanmamış.");
                return;
            }

            if (shapes.Count == 0)
            {
                editor.WriteMessage("\nBağlantı geometrisi oluşturulamadı.");
                return;
            }

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                ObjectId layerId = EnsureLayer(database, transaction, system);
                BlockTableRecord modelSpace = GetModelSpace(database, transaction);

                for (int i = 0; i < shapes.Count; i++)
                {
                    Polyline shape = shapes[i];
                    shape.SetDatabaseDefaults(database);
                    shape.LayerId = layerId;
                    shape.ColorIndex = 256;
                    modelSpace.AppendEntity(shape);
                    transaction.AddNewlyCreatedDBObject(shape, true);

                    if (i == 0)
                    {
                        MechProObjectData data = new MechProObjectData
                        {
                            Kind = "Fitting",
                            Subtype = definition.Id,
                            DisplayName = definition.Name,
                            LayerName = system.Layer,
                            SystemId = system.Id,
                            MaterialId = material.Id,
                            Width = width,
                            Height = MechProPalette.DuctHeight,
                            Thickness = MechProPalette.Thickness
                        };
                        foreach (Point3d connector in connectors)
                            data.Connectors.Add(new MechProConnector(connector));
                        CadObjectDataStore.Write(shape, data, transaction);
                    }
                }

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nMechPro: {0} bağlantı elemanı yerleştirildi. Bu sürümde 2D yerleşim geometrisi ve bağlantı portları oluşturuldu.",
                definition.Name);
        }

        [CommandMethod("MECHPROEQUIPMENT")]
        public void PlaceEquipment()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            Database database = document.Database;
            CatalogEntry definition = CatalogService.FindById(
                CatalogService.Current.Equipment, MechProPalette.EquipmentId);
            CatalogEntry system = CatalogService.FindById(
                CatalogService.Current.Systems, MechProPalette.SystemId);
            CatalogEntry material = CatalogService.FindById(
                CatalogService.Current.Materials, MechProPalette.MaterialId);

            if (definition == null || system == null || material == null)
            {
                editor.WriteMessage("\nMechPro ekipman veya kanal katalog tanımı bulunamadı.");
                return;
            }

            PromptPointResult pointResult = editor.GetPoint(
                new PromptPointOptions("\nEkipman yerleştirme merkezini seçin: "));
            if (pointResult.Status != PromptStatus.OK)
                return;

            PromptPointOptions directionOptions = new PromptPointOptions(
                "\nEkipmanın yönünü belirleyin: ");
            directionOptions.BasePoint = pointResult.Value;
            directionOptions.UseBasePoint = true;
            PromptPointResult directionResult = editor.GetPoint(directionOptions);
            if (directionResult.Status != PromptStatus.OK)
                return;

            Vector2d axis = new Vector2d(
                directionResult.Value.X - pointResult.Value.X,
                directionResult.Value.Y - pointResult.Value.Y);
            if (axis.Length < MinimumLength)
                return;
            axis = axis.GetNormal();

            Point3d center = pointResult.Value;
            double bodyLength = Math.Max(250.0, MechProPalette.DuctWidth * 1.4);
            double bodyWidth = Math.Max(150.0, MechProPalette.DuctWidth * 0.85);
            Point3d start = Move(center, axis, -bodyLength / 2.0);
            Point3d end = Move(center, axis, bodyLength / 2.0);
            Point3d connectorStart = Move(center, axis, -bodyLength * 0.65);
            Point3d connectorEnd = Move(center, axis, bodyLength * 0.65);

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                ObjectId layerId = EnsureLayer(database, transaction, system);
                BlockTableRecord modelSpace = GetModelSpace(database, transaction);

                Polyline body = DuctGeometry.BuildRectangle(
                    start, end, bodyWidth, center.Z);
                if (body == null)
                    return;

                body.SetDatabaseDefaults(database);
                body.LayerId = layerId;
                body.ColorIndex = 256;
                modelSpace.AppendEntity(body);
                transaction.AddNewlyCreatedDBObject(body, true);

                string symbolKind = (definition.Kind ?? string.Empty).ToUpperInvariant();
                AddEquipmentDetails(database, transaction, modelSpace, layerId,
                    center, axis, bodyLength, bodyWidth, symbolKind, definition.Name);

                MechProObjectData data = new MechProObjectData
                {
                    Kind = "Equipment",
                    Subtype = definition.Id,
                    DisplayName = definition.Name,
                    LayerName = system.Layer,
                    SystemId = system.Id,
                    MaterialId = material.Id,
                    Width = bodyWidth,
                    Height = bodyLength,
                    Thickness = MechProPalette.Thickness
                };
                data.Connectors.Add(new MechProConnector(connectorStart));
                data.Connectors.Add(new MechProConnector(connectorEnd));
                CadObjectDataStore.Write(body, data, transaction);

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nMechPro: {0} ekipman sembolü yerleştirildi ve iki bağlantı portu kaydedildi.",
                definition.Name);
        }

        [CommandMethod("MECHPRONETWORK")]
        public void AuditNetwork()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            Database database = document.Database;
            List<NetworkItem> items = new List<NetworkItem>();

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTableRecord modelSpace = GetModelSpace(database, transaction);
                foreach (ObjectId id in modelSpace)
                {
                    Entity entity = transaction.GetObject(id, OpenMode.ForRead, false) as Entity;
                    if (entity == null)
                        continue;

                    MechProObjectData data = CadObjectDataStore.Read(entity, transaction);
                    if (data != null && data.Connectors.Count > 0)
                        items.Add(new NetworkItem { Data = data, Handle = entity.Handle.ToString() });
                }
                transaction.Commit();
            }

            int totalPorts = items.Sum(item => item.Data.Connectors.Count);
            int connectedPorts = 0;
            double tolerance = MechProPalette.ConnectionTolerance;

            foreach (NetworkItem item in items)
            {
                foreach (MechProConnector connector in item.Data.Connectors)
                {
                    bool connected = false;

                    foreach (NetworkItem other in items)
                    {
                        if (ReferenceEquals(item, other))
                            continue;

                        foreach (MechProConnector otherConnector in other.Data.Connectors)
                        {
                            if ((!string.IsNullOrWhiteSpace(connector.NodeId) &&
                                string.Equals(connector.NodeId, otherConnector.NodeId,
                                    StringComparison.OrdinalIgnoreCase)) ||
                                connector.Position.DistanceTo(otherConnector.Position) <= tolerance)
                            {
                                connected = true;
                                break;
                            }
                        }
                        if (connected)
                            break;

                        if (string.Equals(other.Data.Kind, "Duct", StringComparison.OrdinalIgnoreCase))
                        {
                            for (int i = 0; i < other.Data.Route.Count - 1; i++)
                            {
                                double snapTolerance = Math.Max(tolerance,
                                    Math.Min(other.Data.Width * 0.08, 40.0));
                                if (DuctGeometry.DistancePointToSegment(
                                    connector.Position, other.Data.Route[i],
                                    other.Data.Route[i + 1]) <= snapTolerance)
                                {
                                    connected = true;
                                    break;
                                }
                            }
                        }
                        if (connected)
                            break;
                    }

                    if (connected)
                        connectedPorts++;
                }
            }

            int freePorts = Math.Max(0, totalPorts - connectedPorts);
            int[] parents = Enumerable.Range(0, items.Count).ToArray();
            for (int i = 0; i < items.Count; i++)
            {
                for (int j = i + 1; j < items.Count; j++)
                {
                    if (ItemsAreConnected(items[i].Data, items[j].Data, tolerance))
                        Union(parents, i, j);
                }
            }

            int networkCount = 0;
            HashSet<int> roots = new HashSet<int>();
            for (int i = 0; i < parents.Length; i++)
                roots.Add(FindRoot(parents, i));
            networkCount = roots.Count;

            editor.WriteMessage(
                "\nMECHPRO AĞ ANALİZİ: {0} MechPro elemanı, {1} bağlantı portu, {2} bağlı port, {3} açık port, {4} bağlantılı ağ grubu. Yakınlık toleransı: {5:0.##} mm.",
                items.Count, totalPorts, connectedPorts, freePorts, networkCount, tolerance);

            foreach (NetworkItem item in items)
            {
                editor.WriteMessage("\n - {0} | {1} | {2} port",
                    item.Handle, item.Data.DisplayName ?? item.Data.Kind,
                    item.Data.Connectors.Count);
            }
        }

        [CommandMethod("MECHPROCONNECT")]
        public void ConnectSelectedObjects()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            PromptEntityResult firstSelection = editor.GetEntity(
                new PromptEntityOptions("\nBirinci MechPro elemanını seçin: "));
            if (firstSelection.Status != PromptStatus.OK)
                return;

            PromptEntityResult secondSelection = editor.GetEntity(
                new PromptEntityOptions("\nİkinci MechPro elemanını seçin: "));
            if (secondSelection.Status != PromptStatus.OK)
                return;

            using (Transaction transaction = document.Database.TransactionManager.StartTransaction())
            {
                Entity firstEntity = transaction.GetObject(
                    firstSelection.ObjectId, OpenMode.ForWrite) as Entity;
                Entity secondEntity = transaction.GetObject(
                    secondSelection.ObjectId, OpenMode.ForWrite) as Entity;
                MechProObjectData first = CadObjectDataStore.Read(firstEntity, transaction);
                MechProObjectData second = CadObjectDataStore.Read(secondEntity, transaction);

                if (first == null || second == null ||
                    first.Connectors.Count == 0 || second.Connectors.Count == 0)
                {
                    editor.WriteMessage("\nHer iki nesnede de MechPro bağlantı portu bulunmalıdır.");
                    return;
                }

                int firstIndex = 0;
                int secondIndex = 0;
                double minimum = double.MaxValue;
                for (int i = 0; i < first.Connectors.Count; i++)
                {
                    for (int j = 0; j < second.Connectors.Count; j++)
                    {
                        double distance = first.Connectors[i].Position.DistanceTo(
                            second.Connectors[j].Position);
                        if (distance < minimum)
                        {
                            minimum = distance;
                            firstIndex = i;
                            secondIndex = j;
                        }
                    }
                }

                double maximum = Math.Max(MechProPalette.ConnectionTolerance, 1.0);
                if (minimum > maximum)
                {
                    editor.WriteMessage(
                        "\nEn yakın bağlantı portları {0:0.##} mm uzakta. İzin verilen tolerans {1:0.##} mm.",
                        minimum, maximum);
                    return;
                }

                if (firstEntity.ObjectId == secondEntity.ObjectId)
                {
                    editor.WriteMessage("\nBir nesneyi kendi üzerine bağlayamazsınız.");
                    return;
                }

                string oldFirstNetwork = first.NetworkId;
                string oldSecondNetwork = second.NetworkId;
                string mergedNetwork = Guid.NewGuid().ToString("N");
                string nodeId = Guid.NewGuid().ToString("N");
                first.Connectors[firstIndex].NodeId = nodeId;
                second.Connectors[secondIndex].NodeId = nodeId;

                BlockTableRecord modelSpace = GetModelSpace(
                    document.Database, transaction);
                foreach (ObjectId itemId in modelSpace)
                {
                    Entity itemEntity = transaction.GetObject(
                        itemId, OpenMode.ForRead, false) as Entity;
                    if (itemEntity == null)
                        continue;

                    MechProObjectData itemData;
                    if (itemEntity.ObjectId == firstEntity.ObjectId)
                        itemData = first;
                    else if (itemEntity.ObjectId == secondEntity.ObjectId)
                        itemData = second;
                    else
                        itemData = CadObjectDataStore.Read(itemEntity, transaction);

                    if (itemData == null)
                        continue;

                    bool belongsToMergedNetwork =
                        (!string.IsNullOrWhiteSpace(oldFirstNetwork) &&
                         string.Equals(itemData.NetworkId, oldFirstNetwork,
                             StringComparison.OrdinalIgnoreCase)) ||
                        (!string.IsNullOrWhiteSpace(oldSecondNetwork) &&
                         string.Equals(itemData.NetworkId, oldSecondNetwork,
                             StringComparison.OrdinalIgnoreCase)) ||
                        itemEntity.ObjectId == firstEntity.ObjectId ||
                        itemEntity.ObjectId == secondEntity.ObjectId;

                    if (!belongsToMergedNetwork)
                        continue;

                    if (itemEntity.ObjectId != firstEntity.ObjectId &&
                        itemEntity.ObjectId != secondEntity.ObjectId)
                        itemEntity.UpgradeOpen();

                    itemData.NetworkId = mergedNetwork;
                    CadObjectDataStore.Write(itemEntity, itemData, transaction);
                }

                transaction.Commit();

                editor.WriteMessage(
                    "\nMechPro: {0} mm mesafedeki iki bağlantı portu ortak düğüm kimliğiyle ilişkilendirildi.",
                    minimum);
            }
        }

        [CommandMethod("MECHPRORELOADCATALOG")]
        public void ReloadCatalog()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            try
            {
                CatalogService.Reload();
                MechProPalette.RefreshCatalogLists();
                if (document != null)
                    document.Editor.WriteMessage(
                        "\nMechPro: Malzeme, sistem, bağlantı ve ekipman katalogları yeniden yüklendi.");
            }
            catch (System.Exception ex)
            {
                if (document != null)
                    document.Editor.WriteMessage("\nKatalog yükleme hatası: " + ex.Message);
            }
        }

        private static void AddEquipmentDetails(
            Database database, Transaction transaction, BlockTableRecord modelSpace,
            ObjectId layerId, Point3d center, Vector2d axis,
            double length, double width, string kind, string displayName)
        {
            Vector2d normal = new Vector2d(-axis.Y, axis.X);
            double halfLength = length / 2.0;
            double halfWidth = width / 2.0;

            if (kind == "GRILLE")
            {
                for (int i = -2; i <= 2; i++)
                    AddLine(database, transaction, modelSpace, layerId,
                        Move(Move(center, axis, i * length * 0.13), normal, -halfWidth),
                        Move(Move(center, axis, i * length * 0.13), normal, halfWidth));
            }
            else if (kind == "DAMPER" || kind == "FIREDAMPER")
            {
                AddLine(database, transaction, modelSpace, layerId,
                    Move(Move(center, axis, -halfLength * 0.8), normal, -halfWidth * 0.8),
                    Move(Move(center, axis, halfLength * 0.8), normal, halfWidth * 0.8));
                if (kind == "FIREDAMPER")
                    AddLine(database, transaction, modelSpace, layerId,
                        Move(Move(center, axis, -halfLength * 0.8), normal, halfWidth * 0.8),
                        Move(Move(center, axis, halfLength * 0.8), normal, -halfWidth * 0.8));
            }
            else if (kind == "AHU")
            {
                AddLine(database, transaction, modelSpace, layerId,
                    center, Move(center, normal, width * 0.75));
                AddLabel(database, transaction, modelSpace, layerId,
                    "AHU", center, Math.Max(10, width * 0.14), Math.Atan2(axis.Y, axis.X));
            }
            else if (kind == "FAN")
            {
                Circle circle = new Circle(center, Vector3d.ZAxis, width * 0.25);
                circle.SetDatabaseDefaults(database);
                circle.LayerId = layerId;
                modelSpace.AppendEntity(circle);
                transaction.AddNewlyCreatedDBObject(circle, true);
                AddLine(database, transaction, modelSpace, layerId,
                    Move(center, axis, -width * 0.22),
                    Move(center, axis, width * 0.22));
                AddLine(database, transaction, modelSpace, layerId,
                    Move(center, normal, -width * 0.22),
                    Move(center, normal, width * 0.22));
            }
            else if (kind == "VAV")
            {
                AddLine(database, transaction, modelSpace, layerId,
                    Move(Move(center, axis, -halfLength * 0.35), normal, -halfWidth * 0.7),
                    Move(Move(center, axis, halfLength * 0.35), normal, halfWidth * 0.7));
            }
            else if (kind == "SILENCER")
            {
                for (int i = -1; i <= 1; i++)
                    AddLine(database, transaction, modelSpace, layerId,
                        Move(Move(center, normal, i * width * 0.2), axis, -halfLength * 0.65),
                        Move(Move(center, normal, i * width * 0.2), axis, halfLength * 0.65));
            }
            else if (kind == "FILTER" || kind == "COIL")
            {
                AddLine(database, transaction, modelSpace, layerId,
                    Move(Move(center, axis, -halfLength * 0.25), normal, -halfWidth),
                    Move(Move(center, axis, halfLength * 0.25), normal, halfWidth));
                AddLine(database, transaction, modelSpace, layerId,
                    Move(Move(center, axis, -halfLength * 0.25), normal, halfWidth),
                    Move(Move(center, axis, halfLength * 0.25), normal, -halfWidth));
            }
            else
            {
                AddLine(database, transaction, modelSpace, layerId,
                    Move(Move(center, axis, -halfLength * 0.3), normal, -halfWidth * 0.7),
                    Move(Move(center, axis, halfLength * 0.3), normal, halfWidth * 0.7));
            }
        }

        private static void AddRectangle(
            List<Polyline> shapes, Point3d start, Point3d end,
            double width, double elevation)
        {
            Polyline rectangle = DuctGeometry.BuildRectangle(start, end, width, elevation);
            if (rectangle != null)
                shapes.Add(rectangle);
        }

        private static Point3d Move(Point3d point, Vector2d direction, double distance)
        {
            return new Point3d(
                point.X + direction.X * distance,
                point.Y + direction.Y * distance,
                point.Z);
        }

        private static BlockTableRecord GetModelSpace(
            Database database, Transaction transaction)
        {
            BlockTable blockTable = (BlockTable)transaction.GetObject(
                database.BlockTableId, OpenMode.ForRead);
            return (BlockTableRecord)transaction.GetObject(
                blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);
        }

        private static ObjectId EnsureLayer(
            Database database, Transaction transaction, CatalogEntry system)
        {
            string layerName = string.IsNullOrWhiteSpace(system.Layer)
                ? "MP_" + system.Id.ToUpperInvariant()
                : system.Layer;

            LayerTable layerTable = (LayerTable)transaction.GetObject(
                database.LayerTableId, OpenMode.ForRead);
            if (layerTable.Has(layerName))
            {
                ObjectId existingId = layerTable[layerName];
                LayerTableRecord existingLayer = (LayerTableRecord)transaction.GetObject(
                    existingId, OpenMode.ForWrite);
                existingLayer.Color = ParseColor(system.Color);
                return existingId;
            }

            layerTable.UpgradeOpen();
            LayerTableRecord layer = new LayerTableRecord
            {
                Name = layerName,
                Color = ParseColor(system.Color)
            };
            ObjectId id = layerTable.Add(layer);
            transaction.AddNewlyCreatedDBObject(layer, true);
            return id;
        }

        private static Autodesk.AutoCAD.Colors.Color ParseColor(string htmlColor)
        {
            try
            {
                string hex = (htmlColor ?? "#172B64").Trim().TrimStart('#');
                if (hex.Length == 6)
                {
                    byte red = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
                    byte green = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
                    byte blue = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
                    return Autodesk.AutoCAD.Colors.Color.FromRgb(red, green, blue);
                }
            }
            catch { }
            return Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 5);
        }

        private static DBText CreateText(
            Database database, ObjectId layerId, string text,
            Point3d position, double height, double rotation)
        {
            DBText label = new DBText();
            label.SetDatabaseDefaults(database);
            label.TextString = text;
            label.Height = height;
            label.Position = position;
            label.AlignmentPoint = position;
            label.HorizontalMode = TextHorizontalMode.TextCenter;
            label.VerticalMode = TextVerticalMode.TextVerticalMid;
            label.Rotation = rotation;
            label.LayerId = layerId;
            label.ColorIndex = 256;
            return label;
        }

        private static void AddLabel(
            Database database, Transaction transaction, BlockTableRecord modelSpace,
            ObjectId layerId, string text, Point3d position, double height, double rotation)
        {
            DBText label = CreateText(database, layerId, text, position, height, rotation);
            modelSpace.AppendEntity(label);
            transaction.AddNewlyCreatedDBObject(label, true);
            label.AdjustAlignment(database);
        }

        private static void AddLine(
            Database database, Transaction transaction, BlockTableRecord modelSpace,
            ObjectId layerId, Point3d start, Point3d end)
        {
            Line line = new Line(start, end);
            line.SetDatabaseDefaults(database);
            line.LayerId = layerId;
            line.ColorIndex = 256;
            modelSpace.AppendEntity(line);
            transaction.AddNewlyCreatedDBObject(line, true);
        }

        private static Handle ParseHandle(string value)
        {
            return new Handle(long.Parse(value, NumberStyles.HexNumber,
                CultureInfo.InvariantCulture));
        }


        private static bool ItemsAreConnected(
            MechProObjectData first, MechProObjectData second, double tolerance)
        {
            if (!string.IsNullOrWhiteSpace(first.NetworkId) &&
                string.Equals(first.NetworkId, second.NetworkId,
                    StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (MechProConnector a in first.Connectors)
            {
                foreach (MechProConnector b in second.Connectors)
                {
                    if ((!string.IsNullOrWhiteSpace(a.NodeId) &&
                        string.Equals(a.NodeId, b.NodeId, StringComparison.OrdinalIgnoreCase)) ||
                        a.Position.DistanceTo(b.Position) <= tolerance)
                        return true;
                }

                if (IsOnDuct(a.Position, second, tolerance))
                    return true;
            }

            foreach (MechProConnector b in second.Connectors)
            {
                if (IsOnDuct(b.Position, first, tolerance))
                    return true;
            }
            return false;
        }

        private static bool IsOnDuct(
            Point3d point, MechProObjectData candidate, double tolerance)
        {
            if (!string.Equals(candidate.Kind, "Duct", StringComparison.OrdinalIgnoreCase) ||
                candidate.Route == null || candidate.Route.Count < 2)
                return false;

            double snapTolerance = Math.Max(tolerance,
                Math.Min(candidate.Width * 0.08, 40.0));
            for (int i = 0; i < candidate.Route.Count - 1; i++)
            {
                if (DuctGeometry.DistancePointToSegment(
                    point, candidate.Route[i], candidate.Route[i + 1]) <= snapTolerance)
                    return true;
            }
            return false;
        }

        private static int FindRoot(int[] parents, int value)
        {
            while (parents[value] != value)
            {
                parents[value] = parents[parents[value]];
                value = parents[value];
            }
            return value;
        }

        private static void Union(int[] parents, int first, int second)
        {
            int rootA = FindRoot(parents, first);
            int rootB = FindRoot(parents, second);
            if (rootA != rootB)
                parents[rootB] = rootA;
        }

        private sealed class NetworkItem
        {
            public MechProObjectData Data;
            public string Handle;
        }

        private sealed class RoutePointJig : Autodesk.AutoCAD.EditorInput.DrawJig
        {
            private readonly List<Point3d> _fixedRoute;
            private readonly double _width;
            private readonly double _radius;
            private readonly double _elevation;
            private Point3d _candidatePoint;
            private bool _hasCandidate;

            public bool FinishRequested { get; private set; }
            public bool HasCandidate { get { return _hasCandidate; } }
            public Point3d CandidatePoint { get { return _candidatePoint; } }

            public RoutePointJig(
                IList<Point3d> route, double width, double radius, double elevation)
            {
                _fixedRoute = new List<Point3d>(route);
                _width = width;
                _radius = radius;
                _elevation = elevation;
            }

            protected override Autodesk.AutoCAD.EditorInput.SamplerStatus Sampler(Autodesk.AutoCAD.EditorInput.JigPrompts prompts)
            {
                Autodesk.AutoCAD.EditorInput.JigPromptPointOptions options = new Autodesk.AutoCAD.EditorInput.JigPromptPointOptions(
                    "\nSonraki kanal noktasını seçin (Enter ile bitir): ");
                Point3d last = _fixedRoute[_fixedRoute.Count - 1];
                options.BasePoint = last;
                options.UseBasePoint = true;
                options.UserInputControls =
                    UserInputControls.NullResponseAccepted |
                    UserInputControls.Accept3dCoordinates;

                PromptPointResult result = prompts.AcquirePoint(options);
                if (result.Status == PromptStatus.None)
                {
                    FinishRequested = true;
                    _hasCandidate = false;
                    return Autodesk.AutoCAD.EditorInput.SamplerStatus.Cancel;
                }
                if (result.Status != PromptStatus.OK)
                {
                    _hasCandidate = false;
                    return Autodesk.AutoCAD.EditorInput.SamplerStatus.Cancel;
                }

                Point3d raw = new Point3d(result.Value.X, result.Value.Y, _elevation);
                Point3d snapped;
                if (TrySnap(_fixedRoute, raw, out snapped))
                {
                    bool unchanged = _hasCandidate &&
                        _candidatePoint.DistanceTo(snapped) < Tolerance.Global.EqualPoint;
                    _candidatePoint = snapped;
                    _hasCandidate = true;
                    return unchanged ? Autodesk.AutoCAD.EditorInput.SamplerStatus.NoChange : Autodesk.AutoCAD.EditorInput.SamplerStatus.OK;
                }

                if (_hasCandidate)
                {
                    _hasCandidate = false;
                    return Autodesk.AutoCAD.EditorInput.SamplerStatus.OK;
                }
                return Autodesk.AutoCAD.EditorInput.SamplerStatus.NoChange;
            }

            protected override bool WorldDraw(Autodesk.AutoCAD.GraphicsInterface.WorldDraw draw)
            {
                List<Point3d> previewRoute = new List<Point3d>(_fixedRoute);
                if (_hasCandidate)
                    previewRoute.Add(_candidatePoint);

                if (previewRoute.Count >= 2)
                {
                    using (Polyline preview = DuctGeometry.BuildOutline(
                        previewRoute, _width, _radius, _elevation))
                    {
                        if (preview != null)
                        {
                            preview.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(
                                ColorMethod.ByAci, 4);
                            draw.Geometry.Draw(preview);
                        }
                    }
                }
                return true;
            }

            private static bool TrySnap(
                IList<Point3d> route, Point3d raw, out Point3d snapped)
            {
                snapped = raw;
                Point3d last = route[route.Count - 1];
                Vector2d offset = new Vector2d(raw.X - last.X, raw.Y - last.Y);
                if (offset.Length < MinimumLength)
                    return false;
                if (route.Count == 1)
                    return true;

                Vector2d previous = DuctGeometry.Direction(
                    route[route.Count - 2], last);
                Vector2d rawDirection = offset.GetNormal();
                double dot = previous.DotProduct(rawDirection);
                double cross = previous.X * rawDirection.Y - previous.Y * rawDirection.X;
                Vector2d chosen;

                if (Math.Abs(dot) >= Math.Abs(cross))
                {
                    if (dot <= 0.0)
                        return false;
                    chosen = previous;
                }
                else
                {
                    chosen = new Vector2d(-previous.Y, previous.X) *
                        (cross > 0.0 ? 1.0 : -1.0);
                }

                double distance = offset.DotProduct(chosen);
                if (distance < MinimumLength)
                    return false;

                snapped = new Point3d(
                    last.X + chosen.X * distance,
                    last.Y + chosen.Y * distance,
                    last.Z);
                return true;
            }
        }
    }
}