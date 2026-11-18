using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace MechPro.Domain
{
    public sealed class MechProConnector
    {
        public Point3d Position { get; set; }
        public string NodeId { get; set; }

        public MechProConnector()
        {
            NodeId = Guid.NewGuid().ToString("N");
        }

        public MechProConnector(Point3d position)
            : this()
        {
            Position = position;
        }
    }

    public sealed class MechProObjectData
    {
        public string EntityId { get; set; }
        public string NetworkId { get; set; }
        public string Kind { get; set; }
        public string Subtype { get; set; }
        public string DisplayName { get; set; }
        public string LayerName { get; set; }
        public string SystemId { get; set; }
        public string MaterialId { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
        public double Thickness { get; set; }
        public string LabelHandle { get; set; }
        public List<Point3d> Route { get; set; }
        public List<MechProConnector> Connectors { get; set; }

        public MechProObjectData()
        {
            EntityId = Guid.NewGuid().ToString("N");
            NetworkId = Guid.NewGuid().ToString("N");
            Route = new List<Point3d>();
            Connectors = new List<MechProConnector>();
        }
    }

    public static class CadObjectDataStore
    {
        private const string DictionaryKey = "MECHPRO_DATA";
        private const string SchemaMarker = "MECHPRO_OBJECT_V1";

        public static void Write(Entity entity, MechProObjectData data, Transaction transaction)
        {
            if (entity == null || data == null || transaction == null)
                throw new ArgumentNullException();

            if (entity.ExtensionDictionary.IsNull)
                entity.CreateExtensionDictionary();

            DBDictionary dictionary = (DBDictionary)transaction.GetObject(
                entity.ExtensionDictionary, OpenMode.ForWrite);

            Xrecord record;
            if (dictionary.Contains(DictionaryKey))
            {
                record = (Xrecord)transaction.GetObject(
                    dictionary.GetAt(DictionaryKey), OpenMode.ForWrite);
            }
            else
            {
                record = new Xrecord();
                dictionary.SetAt(DictionaryKey, record);
                transaction.AddNewlyCreatedDBObject(record, true);
            }

            List<TypedValue> values = new List<TypedValue>();
            values.Add(new TypedValue((int)DxfCode.Text, SchemaMarker));

            AddText(values, "EntityId", data.EntityId);
            AddText(values, "NetworkId", data.NetworkId);
            AddText(values, "Kind", data.Kind);
            AddText(values, "Subtype", data.Subtype);
            AddText(values, "DisplayName", data.DisplayName);
            AddText(values, "LayerName", data.LayerName);
            AddText(values, "SystemId", data.SystemId);
            AddText(values, "MaterialId", data.MaterialId);
            AddNumber(values, "Width", data.Width);
            AddNumber(values, "Height", data.Height);
            AddNumber(values, "Thickness", data.Thickness);
            AddText(values, "LabelHandle", data.LabelHandle);

            values.Add(new TypedValue((int)DxfCode.Text, "ROUTE"));
            values.Add(new TypedValue((int)DxfCode.Int32, data.Route.Count));
            foreach (Point3d point in data.Route)
                AddPoint(values, point);

            values.Add(new TypedValue((int)DxfCode.Text, "CONNECTORS"));
            values.Add(new TypedValue((int)DxfCode.Int32, data.Connectors.Count));
            foreach (MechProConnector connector in data.Connectors)
            {
                AddPoint(values, connector.Position);
                values.Add(new TypedValue((int)DxfCode.Text, connector.NodeId ?? string.Empty));
            }

            record.Data = new ResultBuffer(values.ToArray());
        }

        public static MechProObjectData Read(Entity entity, Transaction transaction)
        {
            if (entity == null || transaction == null || entity.ExtensionDictionary.IsNull)
                return null;

            DBDictionary dictionary = transaction.GetObject(
                entity.ExtensionDictionary, OpenMode.ForRead, false) as DBDictionary;
            if (dictionary == null || !dictionary.Contains(DictionaryKey))
                return null;

            Xrecord record = transaction.GetObject(
                dictionary.GetAt(DictionaryKey), OpenMode.ForRead, false) as Xrecord;
            if (record == null || record.Data == null)
                return null;

            MechProObjectData data = new MechProObjectData();
            using (ResultBuffer buffer = record.Data)
            {
                TypedValue[] values = buffer.AsArray();
                if (values.Length == 0 ||
                    !string.Equals(Convert.ToString(values[0].Value),
                        SchemaMarker, StringComparison.Ordinal))
                    return null;

                int index = 1;
                while (index < values.Length)
                {
                    string key = Convert.ToString(values[index].Value);
                    index++;

                    if (key == "ROUTE")
                    {
                        int count = ReadInt(values, ref index);
                        data.Route.Clear();
                        for (int i = 0; i < count && index + 2 < values.Length; i++)
                            data.Route.Add(ReadPoint(values, ref index));
                    }
                    else if (key == "CONNECTORS")
                    {
                        int count = ReadInt(values, ref index);
                        data.Connectors.Clear();
                        for (int i = 0; i < count && index + 3 < values.Length; i++)
                        {
                            Point3d point = ReadPoint(values, ref index);
                            string nodeId = Convert.ToString(values[index].Value);
                            index++;
                            data.Connectors.Add(new MechProConnector(point)
                            {
                                NodeId = nodeId
                            });
                        }
                    }
                    else if (key.StartsWith("T:", StringComparison.Ordinal) &&
                        index < values.Length)
                    {
                        string value = Convert.ToString(values[index].Value);
                        index++;
                        SetText(data, key.Substring(2), value);
                    }
                    else if (key.StartsWith("N:", StringComparison.Ordinal) &&
                        index < values.Length)
                    {
                        double value = Convert.ToDouble(
                            values[index].Value, CultureInfo.InvariantCulture);
                        index++;
                        SetNumber(data, key.Substring(2), value);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return data;
        }

        private static void AddText(List<TypedValue> values, string key, string value)
        {
            values.Add(new TypedValue((int)DxfCode.Text, "T:" + key));
            values.Add(new TypedValue((int)DxfCode.Text, value ?? string.Empty));
        }

        private static void AddNumber(List<TypedValue> values, string key, double value)
        {
            values.Add(new TypedValue((int)DxfCode.Text, "N:" + key));
            values.Add(new TypedValue((int)DxfCode.Real, value));
        }

        private static void AddPoint(List<TypedValue> values, Point3d point)
        {
            values.Add(new TypedValue((int)DxfCode.Real, point.X));
            values.Add(new TypedValue((int)DxfCode.Real, point.Y));
            values.Add(new TypedValue((int)DxfCode.Real, point.Z));
        }

        private static int ReadInt(TypedValue[] values, ref int index)
        {
            if (index >= values.Length)
                return 0;
            int result = Convert.ToInt32(values[index].Value, CultureInfo.InvariantCulture);
            index++;
            return Math.Max(0, Math.Min(result, 10000));
        }

        private static Point3d ReadPoint(TypedValue[] values, ref int index)
        {
            double x = Convert.ToDouble(values[index++].Value, CultureInfo.InvariantCulture);
            double y = Convert.ToDouble(values[index++].Value, CultureInfo.InvariantCulture);
            double z = Convert.ToDouble(values[index++].Value, CultureInfo.InvariantCulture);
            return new Point3d(x, y, z);
        }

        private static void SetText(MechProObjectData data, string key, string value)
        {
            switch (key)
            {
                case "EntityId": data.EntityId = value; break;
                case "NetworkId": data.NetworkId = value; break;
                case "Kind": data.Kind = value; break;
                case "Subtype": data.Subtype = value; break;
                case "DisplayName": data.DisplayName = value; break;
                case "LayerName": data.LayerName = value; break;
                case "SystemId": data.SystemId = value; break;
                case "MaterialId": data.MaterialId = value; break;
                case "LabelHandle": data.LabelHandle = value; break;
            }
        }

        private static void SetNumber(MechProObjectData data, string key, double value)
        {
            switch (key)
            {
                case "Width": data.Width = value; break;
                case "Height": data.Height = value; break;
                case "Thickness": data.Thickness = value; break;
            }
        }
    }
}