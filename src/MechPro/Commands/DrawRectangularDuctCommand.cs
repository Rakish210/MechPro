using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace MechPro.Commands
{
    public class DrawRectangularDuctCommand
    {
        private const double DirectionTolerance = 1e-6;

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
                new PromptPointOptions("\nKanal güzergâhının başlangıç noktasını seçin: "));
            if (startResult.Status != PromptStatus.OK)
                return;

            List<Point3d> route = new List<Point3d>();
            route.Add(startResult.Value);

            while (true)
            {
                PromptPointOptions options = new PromptPointOptions(
                    "\nSonraki güzergâh noktasını seçin (bitirmek için Enter): ");
                options.BasePoint = route[route.Count - 1];
                options.UseBasePoint = true;
                options.UseDashedLine = true;
                options.AllowNone = true;

                PromptPointResult pointResult = editor.GetPoint(options);
                if (pointResult.Status == PromptStatus.None)
                    break;
                if (pointResult.Status != PromptStatus.OK)
                    return;

                Point3d previous = route[route.Count - 1];
                Point3d next = pointResult.Value;

                if (Math.Abs(next.Z - startResult.Value.Z) > Tolerance.Global.EqualPoint)
                {
                    editor.WriteMessage("\nBu sürümde güzergâh XY düzleminde olmalıdır. Noktayı aynı Z seviyesinde seçin.");
                    continue;
                }

                Vector2d newDirection = new Vector2d(next.X - previous.X, next.Y - previous.Y);
                if (newDirection.Length < Tolerance.Global.EqualPoint)
                {
                    editor.WriteMessage("\nAynı noktayı tekrar seçemezsiniz.");
                    continue;
                }

                if (route.Count >= 2)
                {
                    Point3d beforePrevious = route[route.Count - 2];
                    Vector2d previousDirection = new Vector2d(
                        previous.X - beforePrevious.X, previous.Y - beforePrevious.Y).GetNormal();
                    Vector2d candidateDirection = newDirection.GetNormal();
                    double dot = previousDirection.DotProduct(candidateDirection);

                    bool straight = dot > 1.0 - DirectionTolerance;
                    bool rightAngle = Math.Abs(dot) < DirectionTolerance;
                    if (!straight && !rightAngle)
                    {
                        editor.WriteMessage("\nİlk sürümde yalnızca düz devam veya 90 derece dönüş desteklenir. Yeni nokta kabul edilmedi.");
                        continue;
                    }
                }

                route.Add(next);
            }

            if (route.Count < 2)
            {
                editor.WriteMessage("\nKanal oluşturmak için en az iki nokta seçmelisiniz.");
                return;
            }

            List<Point2d> leftSide = BuildOffsetSide(route, width / 2.0, 1.0);
            List<Point2d> rightSide = BuildOffsetSide(route, width / 2.0, -1.0);
            if (leftSide == null || rightSide == null)
            {
                editor.WriteMessage("\nKanal geometrisi oluşturulamadı. Güzergâh noktalarını kontrol edin.");
                return;
            }

            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                using (Polyline outline = new Polyline(leftSide.Count + rightSide.Count))
                {
                    int index = 0;
                    foreach (Point2d point in leftSide)
                        outline.AddVertexAt(index++, point, 0, 0, 0);

                    for (int i = rightSide.Count - 1; i >= 0; i--)
                        outline.AddVertexAt(index++, rightSide[i], 0, 0, 0);

                    outline.Closed = true;
                    outline.Elevation = startResult.Value.Z;
                    modelSpace.AppendEntity(outline);
                    transaction.AddNewlyCreatedDBObject(outline, true);
                }

                transaction.Commit();
            }

            editor.WriteMessage(
                "\nMechPro: {0} noktalı güzergâh, {1:0.##} mm genişliğinde gönye kesimli dirseklerle oluşturuldu. Kanal yüksekliği: {2:0.##} mm.",
                route.Count, width, height);
        }

        private static List<Point2d> BuildOffsetSide(
            IList<Point3d> route, double distance, double side)
        {
            List<Point2d> result = new List<Point2d>();
            int count = route.Count;

            for (int i = 0; i < count; i++)
            {
                Vector2d incoming;
                Vector2d outgoing;

                if (i == 0)
                {
                    outgoing = Direction(route[0], route[1]);
                    Vector2d normal = new Vector2d(-outgoing.Y, outgoing.X) * side;
                    result.Add(ToPoint2d(route[i]) + normal * distance);
                }
                else if (i == count - 1)
                {
                    incoming = Direction(route[i - 1], route[i]);
                    Vector2d normal = new Vector2d(-incoming.Y, incoming.X) * side;
                    result.Add(ToPoint2d(route[i]) + normal * distance);
                }
                else
                {
                    incoming = Direction(route[i - 1], route[i]);
                    outgoing = Direction(route[i], route[i + 1]);

                    Vector2d normalIn = new Vector2d(-incoming.Y, incoming.X) * side;
                    Vector2d normalOut = new Vector2d(-outgoing.Y, outgoing.X) * side;
                    Point2d linePoint1 = ToPoint2d(route[i]) + normalIn * distance;
                    Point2d linePoint2 = ToPoint2d(route[i]) + normalOut * distance;

                    double denominator = Cross(incoming, outgoing);
                    if (Math.Abs(denominator) < DirectionTolerance)
                    {
                        result.Add(linePoint1);
                    }
                    else
                    {
                        Vector2d between = linePoint2 - linePoint1;
                        double parameter = Cross(between, outgoing) / denominator;
                        result.Add(linePoint1 + incoming * parameter);
                    }
                }
            }

            return result;
        }

        private static Vector2d Direction(Point3d from, Point3d to)
        {
            return new Vector2d(to.X - from.X, to.Y - from.Y).GetNormal();
        }

        private static Point2d ToPoint2d(Point3d point)
        {
            return new Point2d(point.X, point.Y);
        }

        private static double Cross(Vector2d first, Vector2d second)
        {
            return first.X * second.Y - first.Y * second.X;
        }
    }
}
