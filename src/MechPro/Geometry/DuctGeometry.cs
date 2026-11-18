using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;

namespace MechPro.Geometry
{
    public sealed class LabelPlacement
    {
        public Point3d Position { get; set; }
        public double Rotation { get; set; }
        public double Height { get; set; }
    }

    public static class DuctGeometry
    {
        private const double DirectionTolerance = 1e-5;
        private const double MinimumLength = 1e-6;
        private const double QuarterCircleBulge = 0.4142135623730950;

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

        public static Polyline BuildOutline(
            IList<Point3d> route, double width, double requestedRadius, double elevation)
        {
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
                    corners[i] = false;
                else if (Math.Abs(dot) < DirectionTolerance &&
                    Math.Abs(Math.Abs(cross) - 1.0) < DirectionTolerance)
                {
                    corners[i] = true;
                    hasCorner = true;
                }
                else
                    return null;
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
                    if (segmentIndex + 1 < pointCount - 1 && corners[segmentIndex + 1])
                        adjacentCorners++;

                    if (adjacentCorners > 0)
                        radius = Math.Min(radius,
                            lengths[segmentIndex] * 0.98 / adjacentCorners);
                }

                if (radius <= halfWidth + 1e-4)
                    return null;
            }

            List<OutlineVertex> left = BuildOffsetSide(
                route, directions, corners, halfWidth, radius, 1.0);
            List<OutlineVertex> right = BuildOffsetSide(
                route, directions, corners, halfWidth, radius, -1.0);
            if (left == null || right == null)
                return null;

            Polyline outline = new Polyline(left.Count + right.Count);
            int index = 0;
            foreach (OutlineVertex vertex in left)
                outline.AddVertexAt(index++, vertex.Point, vertex.Bulge, 0.0, 0.0);

            for (int i = right.Count - 1; i >= 0; i--)
            {
                double reverseBulge = i > 0 ? -right[i - 1].Bulge : 0.0;
                outline.AddVertexAt(index++, right[i].Point, reverseBulge, 0.0, 0.0);
            }

            outline.Closed = true;
            outline.Elevation = elevation;
            return outline;
        }

        public static Polyline BuildRectangle(
            Point3d start, Point3d end, double width, double elevation)
        {
            Vector2d direction = new Vector2d(
                end.X - start.X, end.Y - start.Y);
            if (direction.Length < MinimumLength || width <= 0.0)
                return null;

            direction = direction.GetNormal();
            Vector2d normal = new Vector2d(-direction.Y, direction.X) * (width / 2.0);

            Polyline polyline = new Polyline(4);
            polyline.AddVertexAt(0,
                new Point2d(start.X + normal.X, start.Y + normal.Y), 0, 0, 0);
            polyline.AddVertexAt(1,
                new Point2d(end.X + normal.X, end.Y + normal.Y), 0, 0, 0);
            polyline.AddVertexAt(2,
                new Point2d(end.X - normal.X, end.Y - normal.Y), 0, 0, 0);
            polyline.AddVertexAt(3,
                new Point2d(start.X - normal.X, start.Y - normal.Y), 0, 0, 0);
            polyline.Closed = true;
            polyline.Elevation = elevation;
            return polyline;
        }

        public static Polyline BuildReducer(
            Point3d start, Vector2d direction, double inletWidth,
            double outletWidth, double length, double elevation)
        {
            if (direction.Length < MinimumLength || inletWidth <= 0.0 ||
                outletWidth <= 0.0 || length <= 0.0)
                return null;

            Vector2d u = direction.GetNormal();
            Vector2d n = new Vector2d(-u.Y, u.X);
            Point2d p0 = new Point2d(start.X, start.Y);
            Point2d p1 = p0 + u * length;

            Polyline polyline = new Polyline(4);
            polyline.AddVertexAt(0,
                p0 + n * (inletWidth / 2.0), 0, 0, 0);
            polyline.AddVertexAt(1,
                p1 + n * (outletWidth / 2.0), 0, 0, 0);
            polyline.AddVertexAt(2,
                p1 - n * (outletWidth / 2.0), 0, 0, 0);
            polyline.AddVertexAt(3,
                p0 - n * (inletWidth / 2.0), 0, 0, 0);
            polyline.Closed = true;
            polyline.Elevation = elevation;
            return polyline;
        }

        public static LabelPlacement GetLabelPlacement(
            IList<Point3d> route, double width, double height)
        {
            if (route == null || route.Count < 2)
                return null;

            int best = 0;
            double bestLength = -1.0;
            for (int i = 0; i < route.Count - 1; i++)
            {
                double dx = route[i + 1].X - route[i].X;
                double dy = route[i + 1].Y - route[i].Y;
                double length = Math.Sqrt(dx * dx + dy * dy);
                if (length > bestLength)
                {
                    bestLength = length;
                    best = i;
                }
            }

            Point3d a = route[best];
            Point3d b = route[best + 1];
            double angle = Math.Atan2(b.Y - a.Y, b.X - a.X);
            double textHeight = Math.Max(2.5, Math.Min(width, height) * 0.16);

            return new LabelPlacement
            {
                Position = new Point3d(
                    (a.X + b.X) / 2.0,
                    (a.Y + b.Y) / 2.0,
                    route[0].Z),
                Rotation = angle,
                Height = textHeight
            };
        }

        public static Point3d Offset(Point3d origin, Vector2d axis, double along, double side)
        {
            Vector2d u = axis.GetNormal();
            Vector2d n = new Vector2d(-u.Y, u.X);
            return new Point3d(
                origin.X + u.X * along + n.X * side,
                origin.Y + u.Y * along + n.Y * side,
                origin.Z);
        }

        public static Vector2d Direction(Point3d from, Point3d to)
        {
            return new Vector2d(to.X - from.X, to.Y - from.Y).GetNormal();
        }

        public static double DistancePointToSegment(Point3d point, Point3d start, Point3d end)
        {
            Vector2d segment = new Vector2d(end.X - start.X, end.Y - start.Y);
            double lengthSquared = segment.DotProduct(segment);
            if (lengthSquared < MinimumLength)
                return point.DistanceTo(start);

            Vector2d offset = new Vector2d(point.X - start.X, point.Y - start.Y);
            double t = Math.Max(0.0, Math.Min(1.0,
                offset.DotProduct(segment) / lengthSquared));
            Point3d projection = new Point3d(
                start.X + segment.X * t,
                start.Y + segment.Y * t,
                start.Z);
            return point.DistanceTo(projection);
        }

        private static List<OutlineVertex> BuildOffsetSide(
            IList<Point3d> route, Vector2d[] directions, bool[] corners,
            double halfWidth, double radius, double side)
        {
            List<OutlineVertex> result = new List<OutlineVertex>();
            Vector2d firstNormal = LeftNormal(directions[0]) * side;
            result.Add(new OutlineVertex(ToPoint2d(route[0]) + firstNormal * halfWidth, 0.0));

            for (int i = 1; i < route.Count - 1; i++)
            {
                Vector2d incoming = directions[i - 1];
                Vector2d outgoing = directions[i];
                Vector2d incomingNormal = LeftNormal(incoming);
                Vector2d outgoingNormal = LeftNormal(outgoing);

                if (!corners[i])
                {
                    result.Add(new OutlineVertex(
                        ToPoint2d(route[i]) + incomingNormal * (side * halfWidth), 0.0));
                    continue;
                }

                double turn = Cross(incoming, outgoing) >= 0.0 ? 1.0 : -1.0;
                Point2d tangentIn = ToPoint2d(route[i]) -
                    incoming * radius + incomingNormal * (side * halfWidth);
                Point2d tangentOut = ToPoint2d(route[i]) +
                    outgoing * radius + outgoingNormal * (side * halfWidth);

                result.Add(new OutlineVertex(tangentIn, turn * QuarterCircleBulge));
                result.Add(new OutlineVertex(tangentOut, 0.0));
            }

            Vector2d lastNormal = LeftNormal(directions[directions.Length - 1]) * side;
            result.Add(new OutlineVertex(
                ToPoint2d(route[route.Count - 1]) + lastNormal * halfWidth, 0.0));
            return result;
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