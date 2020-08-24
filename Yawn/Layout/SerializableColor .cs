//  Copyright (c) 2020 Jeff East
//
//  Licensed under the Code Project Open License (CPOL) 1.02
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace Yawn
{
    [Serializable]
    public struct SerializableColor : IEquatable<SerializableColor>, ISerializable
    {
        [XmlAttribute]
        public byte R;
        [XmlAttribute]
        public byte G;
        [XmlAttribute]
        public byte B;
        [XmlAttribute]
        public bool IsNull;

        public Color? Get()
        {
            if (IsNull)
            {
                return null;
            }
            else
            {
                return Color.FromRgb(R, G, B);
            }
        }

        public bool HasValue { get { return !IsNull; } }

        public void Set(Color? c)
        {
            if (c.HasValue)
            {
                IsNull = false;
                R = c.Value.R;
                G = c.Value.G;
                B = c.Value.B;
            }
            else
            {
                IsNull = true;
            }
        }

        public SerializableColor(Color c)
        {
            R = c.R;
            G = c.G;
            B = c.B;
            IsNull = false;
        }

        public SerializableColor(SerializationInfo info, StreamingContext context)
        {
            R = info.GetByte("R");
            G = info.GetByte("G");
            B = info.GetByte("B");
            IsNull = info.GetBoolean("IsNull");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("R", R);
            info.AddValue("G", G);
            info.AddValue("B", B);
            info.AddValue("IsNull", IsNull);
        }

        public static implicit operator SerializableColor(Color c)
        {
            return new SerializableColor(c);
        }

        public static implicit operator Color(SerializableColor sc)
        {
            if (sc.Get().HasValue)
            {
                return sc.Get().Value;
            }
            else
            {
                return default(Color);
            }
        }

        public bool Equals(SerializableColor other)
        {
            return (IsNull == other.IsNull) &&
                (IsNull ||
                 (R == other.R && G == other.G && B == other.B));
        }
    }
}
