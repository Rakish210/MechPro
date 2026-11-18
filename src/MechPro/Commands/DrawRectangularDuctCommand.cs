using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;

namespace MechPro.Commands
{
    public class DrawRectangularDuctCommand
    {
        [CommandMethod("MECHPRO")]
        public void ShowMechPro()
        {
            UI.MechProPalette.Show();
        }

        [CommandMethod("MECHPRODRAW")]
        public void DrawRectangularDuct()
        {
            Document document = Application.DocumentManager.MdiActiveDocument;
            if (document == null)
                return;

            Editor editor = document.Editor;
            Database database = document.Database;

            double width = UI.MechProPalette.DuctWidth;
            double height = UI.MechProPalette.DuctHeight;

            PromptPointResult startResult = editor.GetPoint(
                new PromptPointOptions("\nKanal başlangıç noktasını seçin: "));
            if (startResult.Status != PromptStatus.OK)
                return;

            PromptPointOptions endOptions = new PromptPointOptions(
                "\nKanal bitiş noktasını seçin: ");
            endOptions.BasePoint = startResult.Value;
            endOptions.UseBasePoint = true;

            PromptPointResult endResult = editor.GetPoint(endOptions);
            if (endResult.Status != PromptStatus.OK)
                return;

            Vector3d direction = endResult.Value - startResult.Value;
            if (direction.Length < Tolerance.Global.EqualPoint)
            {
                editor.WriteMessage("\nBaşlangıç ve bitiş noktaları aynı olamaz.");
                return;
            }

            Vector3d axis = new Vector3d(direction.X, direction.Y, 0.0);
            if (axis.Length < Tolerance.Global.EqualPoint)
            {
                editor.WriteMessage("\nKanal ekseni XY düzleminde olmalıdır.");
                return;
            }

            axis = axis.GetNormal();
            Vector3d perpendicular = new Vector3d(-axis.Y, axis.X, 0.0);
            Vector3d offset = perpendicular * (width / 2.0);

            Point3d p1 = new Point3d(startResult.Value.X, startResult.Value.Y, startResult.Value.Z) + offset;
            Point3d p2 = new Point3d(endResult.Value.X, endResult.Value.Y, endResult.Value.Z) + offset;
            Point3d p3 = new Point3d(endResult.Value.X, endResult.Value.Y, endResult.Value.Z) - offset;
            Point3d p4 = new Point3d(startResult.Value.X, startResult.Value.Y, startResult.Value.Z) - offset;

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                using (Polyline outline = new Polyline(4))
                {
                    outline.AddVertexAt(0, new Point2d(p1.X, p1.Y), 0, 0, 0);
                    outline.AddVertexAt(1, new Point2d(p2.X, p2.Y), 0, 0, 0);
                    outline.AddVertexAt(2, new Point2d(p3.X, p3.Y), 0, 0, 0);
                    outline.AddVertexAt(3, new Point2d(p4.X, p4.Y), 0, 0, 0);
                    outline.Closed = true;

                    modelSpace.AppendEntity(outline);
                    transaction.AddNewlyCreatedDBObject(outline, true);
                }

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nMechPro: {0:0.##} x {1:0.##} mm kanal izi oluşturuldu. Yükseklik bu aşamada parametre olarak saklanır; plan geometrisine uygulanmaz.",
                width, height);
        }
    }
}