using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using AutoCadPolyline = Autodesk.AutoCAD.DatabaseServices.Polyline;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;

namespace MechPro.Commands
{
    public class DrawRectangularDuctCommand
    {
        private const double DirectionTolerance = 1e-5;
        private const double MinimumLength = 1e-6;
        private const double QuarterCircleBulge = 0.4142135623730950;

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

            editor.WriteMessage(
                "\nMechPro: İmleci hareket ettirerek kanal önizlemesini izleyin. Noktaları seçin; Enter ile bitirin.");

            while (true)
            {
                RoutePointJig jig = new RoutePointJig(route, width, width, startResult.Value.Z);
                PromptResult dragResult = editor.Drag(jig);

                if (jig.FinishRequested)
                    break;

                if (dragResult.Status != PromptStatus.OK)
                    return;

                if (!jig.HasCandidate)
                {
                    editor.WriteMessage(
                        "\nGeçersiz yön veya dirsek için yetersiz mesafe. Noktayı yeniden seçin.");
                    continue;
                }

                Point3d next = jig.CandidatePoint;
                List<Point3d> testRoute = new List<Point3d>(route);
                testRoute.Add(next);

                double effectiveRadius;
                using (AutoCadPolyline testOutline = BuildOutline(
                    testRoute, width, width, startResult.Value.Z, out effectiveRadius))
                {
                    if (testOutline == null)
                    {
                        editor.WriteMessage(
                            "\nBu dönüş için yeterli düz kanal boyu yok. Dirsek iç yarıçapının pozitif kalması için güzergâhı uzatın.");
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

            double finalRadius;
            AutoCadPolyline finalOutline = BuildOutline(
                route, width, width, startResult.Value.Z, out finalRadius);

            if (finalOutline == null)
            {
                editor.WriteMessage(
                    "\nKanal geometrisi oluşturulamadı. Güzergâh uzunluklarını ve dönüşleri kontrol edin.");
                return;
            }

            using (finalOutline)
            using (Transaction transaction = database.TransactionManager.StartTransaction())
            {
                BlockTable blockTable = (BlockTable)transaction.GetObject(
                    database.BlockTableId, OpenMode.ForRead);
                BlockTableRecord modelSpace = (BlockTableRecord)transaction.GetObject(
                    blockTable[BlockTableRecord.ModelSpace], OpenMode.ForWrite);

                modelSpace.AppendEntity(finalOutline);
                transaction.AddNewlyCreatedDBObject(finalOutline, true);
                transaction.Commit();
            }

            editor.WriteMessage(
                "\nMechPro: {0} noktalı güzergâh, {1:0.##} mm genişliğinde yuvarlatılmış 90° dirseklerle oluşturuldu. Merkez hat dirsek yarıçapı: {2:0.##} mm. Kanal yüksekliği: {3:0.##} mm (2D plan görünüşü).",
                route.Count, width, finalRadius, height);
        }

        private sealed class RoutePointJig : DrawJig
        {
            private readonly List<Point3d> _fixedRoute;
            private readonly double _width;
            private readonly double _requestedRadius;
            private readonly double _elevation;
            private Point3d _candidatePoint;
            private bool _hasCandidate;

            public bool FinishRequested { get; private set; }
            public bool HasCandidate { get { return _hasCandidate; } }
            public Point3d CandidatePoint { get { return _candidatePoint; } }

            public RoutePointJig(
                IList<Point3d> fixedRoute,
                double width,
                double requestedRadius,
                double elevation)
            {
                _fixedRoute = new List<Point3d>(fixedRoute);
                _width = width;
                _requestedRadius = requestedRadius;
                _elevation = elevation;
            }

            protected override SamplerStatus Sampler(JigPrompts prompts)
            {
                JigPromptPointOptions options = new JigPromptPointOptions(
                    "\nSonraki kanal noktasını seçin (Enter ile bitir): ");

                Point3d lastPoint = _fixedRoute[_fixedRoute.Count - 1];
                options.BasePoint = lastPoint;
                options.UseBasePoint = true;
                options.UseDashedLine = true;
                options.UserInputControls =
                    UserInputControls.NullResponseAccepted |
                    UserInputControls.Accept3dCoordinates;

                PromptPointResult result = prompts.AcquirePoint(options);

                if (result.Status == PromptStatus.None)
                {
                    FinishRequested = true;
                    _hasCandidate = false;
                    return SamplerStatus.Cancel;
                }

                if (result.Status != PromptStatus.OK)
                {
                    _hasCandidate = false;
                    return SamplerStatus.Cancel;
                }

                Point3d rawPoint = new Point3d(
                    result.Value.X, result.Value.Y, _elevation);

                Point3d snappedPoint;
                bool valid = TrySnapNextPoint(_fixedRoute, rawPoint, out snappedPoint);

                if (valid)
                {
                    bool unchanged = _hasCandidate &&
                        _candidatePoint.DistanceTo(snappedPoint) < Tolerance.Global.EqualPoint;
                    _candidatePoint = snappedPoint;
                    _hasCandidate = true;
                    return unchanged ? SamplerStatus.NoChange : SamplerStatus.OK;
                }

                if (_hasCandidate)
                {
                    _hasCandidate = false;
                    return SamplerStatus.OK;
                }

                return SamplerStatus.NoChange;
            }

            protected override bool WorldDraw(WorldDraw draw)
            {
                List<Point3d> previewRoute = new List<Point3d>(_fixedRoute);
                if (_hasCandidate)
                    previewRoute.Add(_candidatePoint);

                if (previewRoute.Count >= 2)
                {
                    double ignoredRadius;
                    using (AutoCadPolyline preview = BuildOutline(
                        previewRoute, _width, _requestedRadius, _elevation, out ignoredRadius))
                    {
                        if (preview != null)
                        {
                            preview.Color = Color.FromColorIndex(ColorMethod.ByAci, 4);
                            draw.Geometry.Draw(preview);
                        }
                    }
                }

                return true;
            }
        }

        private sealed class OutlineVertex
        {
            public Point2d Point;
            public double Bulge;

            public OutlineVertex(Point2d point, double bulge)
            {
                Point = point;
                Bulge = bulge;
            }
        }

        private static bool TrySnapNextPoint(
            IList<Point3d> route, Point3d rawPoint, out Point3d snappedPoint)
        {
            snappedPoint = rawPoint;
            Point3d last = route[route.Count - 1];
            Vector2d offset = new Vector2d(
                rawPoint.X - last.X, rawPoint.Y - last.Y);

            if (offset.Length < MinimumLength)
                return false;

            if (route.Count == 1)
                return true;

            Vector2d previousDirection = Direction(
                route[route.Count - 2], last);
            Vector2d rawDirection = offset.GetNormal();
            double dot = previousDirection.DotProduct(rawDirection);
            double cross = Cross(previousDirection, rawDirection);

            Vector2d snappedDirection;
            if (Math.Abs(dot) >= Math.Abs(cross))
            {
                if (dot <= 0.0)
                    return false;

                snappedDirection = previousDirection;
            }
            else
            {
                double side = cross > 0.0 ? 1.0 : -1.0;
                snappedDirection = LeftNormal(previousDirection) * side;
            }

            double distance = offset.DotProduct(snappedDirection);
            if (distance < MinimumLength)
                return false;

            snappedPoint = new Point3d(
                last.X + snappedDirection.X * distance,
                last.Y + snappedDirection.Y * distance,
                last.Z);
            return true;
        }

        private static AutoCadPolyline BuildOutline(
            IList<Point3d> route,
            double width,
            double requestedRadius,
            double elevation,
            out double effectiveRadius)
        {
            effectiveRadius = 0.0;

            if (route == null || route.Count < 2 || width <= 0.0)
                return null;

            int pointCount = route.Count;
            Vector2d[] directions = new Vector2d[pointCount - 1];
            double[] lengths = new double[pointCount - 1];
            bool[] corners = new bool[pointCount];

            for (int i = 0; i < pointCount - 1; i++)
            {
                Vector2d segment = new Vector2d(
                    route[i + 1].X - route[i].X,
                    route[i + 1].Y - route[i].Y);

                lengths[i] = segment.Length;
                if (lengths[i] < MinimumLength)
                    return null;

                directions[i] = segment / lengths[i];
            }

            bool hasCorner = false;
            for (int i = 1; i < pointCount - 1; i++)
            {
                double dot = directions[i - 1].DotProduct(directions[i]);
                double cross = Cross(directions[i - 1], directions[i]);

                if (dot > 1.0 - DirectionTolerance)
                {
                    corners[i] = false;
                }
                else if (Math.Abs(dot) < DirectionTolerance &&
                    Math.Abs(Math.Abs(cross) - 1.0) < DirectionTolerance)
                {
                    corners[i] = true;
                    hasCorner = true;
                }
                else
                {
                    return null;
                }
            }

            double halfWidth = width / 2.0;
            double radius = requestedRadius > 0.0 ? requestedRadius : width;

            if (hasCorner)
            {
                for (int segmentIndex = 0; segmentIndex < lengths.Length; segmentIndex++)
                {
                    int adjacentCorners = 0;
                    if (segmentIndex > 0 && corners[segmentIndex])
                        adjacentCorners++;
                    if (segmentIndex + 1 < pointCount - 1 &&
                        corners[segmentIndex + 1])
                        adjacentCorners++;

                    if (adjacentCorners > 0)
                    {
                        double allowed = lengths[segmentIndex] * 0.98 / adjacentCorners;
                        radius = Math.Min(radius, allowed);
                    }
                }

                if (radius <= halfWidth + 1e-4)
                    return null;
            }

            effectiveRadius = hasCorner ? radius : 0.0;

            List<OutlineVertex> leftSide = BuildOffsetSide(
                route, directions, corners, halfWidth, radius, 1.0);
            List<OutlineVertex> rightSide = BuildOffsetSide(
                route, directions, corners, halfWidth, radius, -1.0);

            if (leftSide == null || rightSide == null)
                return null;

            AutoCadPolyline outline = new AutoCadPolyline(leftSide.Count + rightSide.Count);
            int index = 0;

            foreach (OutlineVertex vertex in leftSide)
                outline.AddVertexAt(index++, vertex.Point, vertex.Bulge, 0.0, 0.0);

            for (int i = rightSide.Count - 1; i >= 0; i--)
            {
                double reverseBulge = i > 0 ? -rightSide[i - 1].Bulge : 0.0;
                outline.AddVertexAt(
                    index++, rightSide[i].Point, reverseBulge, 0.0, 0.0);
            }

            outline.Closed = true;
            outline.Elevation = elevation;
            return outline;
        }

        private static List<OutlineVertex> BuildOffsetSide(
            IList<Point3d> route,
            Vector2d[] directions,
            bool[] corners,
            double halfWidth,
            double radius,
            double side)
        {
            List<OutlineVertex> result = new List<OutlineVertex>();
            Vector2d firstNormal = LeftNormal(directions[0]) * side;
            result.Add(new OutlineVertex(
                ToPoint2d(route[0]) + firstNormal * halfWidth, 0.0));

            for (int i = 1; i < route.Count - 1; i++)
            {
                Vector2d incoming = directions[i - 1];
                Vector2d outgoing = directions[i];
                Vector2d incomingNormal = LeftNormal(incoming);
                Vector2d outgoingNormal = LeftNormal(outgoing);

                if (!corners[i])
                {
                    result.Add(new OutlineVertex(
                        ToPoint2d(route[i]) + incomingNormal * (side * halfWidth),
                        0.0));
                    continue;
                }

                double turn = Cross(incoming, outgoing) >= 0.0 ? 1.0 : -1.0;
                Point2d tangentIn = ToPoint2d(route[i]) -
                    incoming * radius + incomingNormal * (side * halfWidth);
                Point2d tangentOut = ToPoint2d(route[i]) +
                    outgoing * radius + outgoingNormal * (side * halfWidth);

                result.Add(new OutlineVertex(
                    tangentIn, turn * QuarterCircleBulge));
                result.Add(new OutlineVertex(tangentOut, 0.0));
            }

            Vector2d lastNormal = LeftNormal(directions[directions.Length - 1]) * side;
            result.Add(new OutlineVertex(
                ToPoint2d(route[route.Count - 1]) + lastNormal * halfWidth,
                0.0));

            return result;
        }

        private static Vector2d Direction(Point3d from, Point3d to)
        {
            return new Vector2d(to.X - from.X, to.Y - from.Y).GetNormal();
        }

        private static Vector2d LeftNormal(Vector2d direction)
        {
            return new Vector2d(-direction.Y, direction.X);
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