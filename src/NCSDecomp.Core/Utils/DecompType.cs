// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS Type.java. Stack footprint matches KPatcher.Core NCSTypeCode byte values.

using System;
using KPatcher.Core.Formats.NCS;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// NWScript value type and stack footprint (4-byte slots), using nwnnsscomp-style byte codes.
    /// Use <see cref="ToNCSType"/> to bridge to <see cref="NCSType"/> where needed.
    /// </summary>
    public class DecompType
    {
        public const byte VtNone = 0;
        public const byte VtStack = 1;
        public const byte VtInteger = 3;
        public const byte VtFloat = 4;
        public const byte VtString = 5;
        public const byte VtObject = 6;
        public const byte VtEffect = 16;
        public const byte VtEvent = 17;
        public const byte VtLocation = 18;
        public const byte VtTalent = 19;
        public const byte VtIntint = 32;
        public const byte VtFloatfloat = 33;
        public const byte VtObjectobject = 34;
        public const byte VtStringstring = 35;
        public const byte VtStructstruct = 36;
        public const byte VtIntfloat = 37;
        public const byte VtFloatint = 38;
        public const byte VtEffecteffect = 48;
        public const byte VtEventevent = 49;
        public const byte VtLocloc = 50;
        public const byte VtTaltal = 51;
        public const byte VtVectorvector = 58;
        public const byte VtVectorfloat = 59;
        public const byte VtFloatvector = 60;
        public const byte VtVector = unchecked((byte)-16);
        public const byte VtStruct = unchecked((byte)-15);
        public const byte VtInvalid = unchecked((byte)-1);

        protected byte _type;
        protected int _size;

        public DecompType(byte type)
        {
            _type = type;
            _size = 1;
        }

        public DecompType(string str)
        {
            _type = Decode(str);
            _size = TypeSize(_type) / 4;
        }

        public static DecompType ParseType(string str)
        {
            return new DecompType(str);
        }

        public virtual void Close()
        {
        }

        public byte ByteValue()
        {
            return _type;
        }

        public override string ToString()
        {
            return ToString(_type);
        }

        public virtual string ToDeclString()
        {
            return ToString();
        }

        public int Size()
        {
            return _size;
        }

        public virtual bool IsTyped()
        {
            return _type != VtInvalid;
        }

        public string ToValueString()
        {
            return _type.ToString();
        }

        public static string ToString(DecompType atype)
        {
            return atype != null ? ToString(atype._type) : "";
        }

        protected static string ToString(byte type)
        {
            switch (type)
            {
                case unchecked((byte)-16):
                    return "vector";
                case unchecked((byte)-15):
                    return "struct";
                case unchecked((byte)-1):
                    return "invalid";
                case 0:
                    return "void";
                case 1:
                    return "stack";
                case 3:
                    return "int";
                case 4:
                    return "float";
                case 5:
                    return "string";
                case 6:
                    return "object";
                case 16:
                    return "effect";
                case 18:
                    return "location";
                case 19:
                    return "talent";
                case 32:
                    return "intint";
                case 33:
                    return "floatfloat";
                case 34:
                    return "objectobject";
                case 35:
                    return "stringstring";
                case 36:
                    return "structstruct";
                case 37:
                    return "intfloat";
                case 38:
                    return "floatint";
                case 48:
                    return "effecteffect";
                case 49:
                    return "eventevent";
                case 50:
                    return "locloc";
                case 51:
                    return "taltal";
                case 58:
                    return "vectorvector";
                case 59:
                    return "vectorfloat";
                case 60:
                    return "floatvector";
                default:
                    return "unknown";
            }
        }

        private static byte Decode(string type)
        {
            if (type == "void")
            {
                return 0;
            }

            if (type == "int")
            {
                return 3;
            }

            if (type == "float")
            {
                return 4;
            }

            if (type == "string")
            {
                return 5;
            }

            if (type == "object")
            {
                return 6;
            }

            if (type == "effect")
            {
                return 16;
            }

            if (type == "event")
            {
                return 17;
            }

            if (type == "location")
            {
                return 18;
            }

            if (type == "talent")
            {
                return 19;
            }

            if (type == "vector")
            {
                return unchecked((byte)-16);
            }

            if (type == "action")
            {
                return 0;
            }

            if (type == "INT")
            {
                return 3;
            }

            if (type == "OBJECT_ID")
            {
                return 6;
            }

            throw new InvalidOperationException("Attempted to get unknown type " + type);
        }

        public int TypeSize()
        {
            return TypeSize(_type);
        }

        public static int TypeSize(string type)
        {
            return TypeSize(Decode(type));
        }

        private static int TypeSize(byte type)
        {
            switch (type)
            {
                case unchecked((byte)-16):
                    return 12;
                case 0:
                    return 0;
                case 3:
                case 4:
                case 5:
                case 6:
                case 16:
                case 17:
                case 18:
                case 19:
                    return 4;
                default:
                    throw new InvalidOperationException("Unknown type code: " + type);
            }
        }

        public virtual DecompType GetElement(int pos)
        {
            if (pos != 1)
            {
                throw new InvalidOperationException("Position > 1 for type, not struct");
            }

            return this;
        }

        public NCSType ToNCSType()
        {
            return new NCSType((NCSTypeCode)_type);
        }

        public bool Equals(byte type)
        {
            return _type == type;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }

            DecompType other = obj as DecompType;
            return other != null && _type == other._type;
        }

        public override int GetHashCode()
        {
            return _type;
        }
    }
}
