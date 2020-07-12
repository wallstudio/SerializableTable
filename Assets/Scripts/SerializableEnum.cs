using System;
using System.Globalization;
using UnityEngine;

namespace SerializableTable
{
    public class EnumTypeMissMatchException : Exception
    {
        public EnumTypeMissMatchException() : base() {}
        public EnumTypeMissMatchException(string message) : base(message) {}
    }


    [Serializable]
    public class SerializableEnum<TEnum> : SerializableEnum where TEnum : unmanaged, Enum
    {
        public SerializableEnum(TEnum value) : base(value) {}
    }


    [Serializable]
    public class SerializableEnum : ISerializationCallbackReceiver, IComparable<SerializableEnum>, IComparable, IEquatable<SerializableEnum>
    {
        Type m_Type;
        UInt64 m_Bits;

        SerializableEnum() {}
        public SerializableEnum(Enum value)
        {
            if(value == null)
            {
                throw new ArgumentNullException();
            }

            m_Type = value.GetType();
            m_Bits = ToUInt64(value);

            // Srialize時まで遅延
            m_IsAll = default;
            m_TypeName = default;
            m_Names = default;
        }

        static Enum ToEnum(SerializableEnum value)
        {
            if(value.m_Type == default)
            {
                return null;
            }
            return (Enum)Enum.ToObject(value.m_Type, value.m_Bits);
        }

        static UInt64 ToUInt64(Enum value)
        {
            // https://referencesource.microsoft.com/#mscorlib/system/enum.cs,209

            // Helper function to silently convert the value to UInt64 from the other base types for enum without throwing an exception.
            // This is need since the Convert functions do overflow checks.
            switch(value.GetTypeCode())
            {
                case TypeCode.SByte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                    return (UInt64)Convert.ToInt64(value, CultureInfo.InvariantCulture);
                case TypeCode.Byte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Boolean:
                case TypeCode.Char:
                    return Convert.ToUInt64(value, CultureInfo.InvariantCulture);
                default:
                    return 0UL;
            }
        }


        #region Operators
		public override string ToString() => ToEnum(this)?.ToString();
		public override bool Equals(object obj) => (obj is IComparable<SerializableEnum> other) && other.Equals(this);
		public override int GetHashCode() => (m_Type?.GetHashCode() ?? 0) ^ m_Bits.GetHashCode();
		bool IEquatable<SerializableEnum>.Equals(SerializableEnum other) => m_Type == other.m_Type && m_Bits == other.m_Bits;
        int IComparable<SerializableEnum>.CompareTo(SerializableEnum other) => m_Type.Name.CompareTo(other.m_Type.Name) << 8 + m_Bits.CompareTo(other.m_Bits);
        int IComparable.CompareTo(object obj) => obj is SerializableEnum other ? ((IComparable<SerializableEnum>)this).CompareTo(other) : GetHashCode().CompareTo(obj.GetHashCode());
        public static explicit operator Enum (SerializableEnum other) => ToEnum(other);
        public static explicit operator UInt64 (SerializableEnum other) => other.m_Bits;
		public static SerializableEnum operator & (SerializableEnum a, Enum b) => Operate(a, b, (_a, _b) => _a & _b);
		public static SerializableEnum operator | (SerializableEnum a, Enum b) => Operate(a, b, (_a, _b) => _a | _b);
		public static SerializableEnum operator ^ (SerializableEnum a, Enum b) => Operate(a, b, (_a, _b) => _a ^ _b);
		static SerializableEnum Operate(SerializableEnum a, Enum b, Func<UInt64, UInt64, UInt64> op) => Operate(a, new SerializableEnum(b), op);
		public static SerializableEnum operator & (SerializableEnum a, SerializableEnum b) => Operate(a, b, (_a, _b) => _a & _b);
		public static SerializableEnum operator | (SerializableEnum a, SerializableEnum b) => Operate(a, b, (_a, _b) => _a | _b);
		public static SerializableEnum operator ^ (SerializableEnum a, SerializableEnum b) => Operate(a, b, (_a, _b) => _a ^ _b);
		static SerializableEnum Operate(SerializableEnum a, SerializableEnum b, Func<UInt64, UInt64, UInt64> op) => Operate(a, b.m_Type, b.m_Bits, op);
		public static SerializableEnum operator & (SerializableEnum a, UInt64 b) => Operate(a, a.m_Type, b, (_a, _b) => _a & _b);
		public static SerializableEnum operator | (SerializableEnum a, UInt64 b) => Operate(a, a.m_Type, b, (_a, _b) => _a | _b);
		public static SerializableEnum operator ^ (SerializableEnum a, UInt64 b) => Operate(a, a.m_Type, b, (_a, _b) => _a ^ _b);
		static SerializableEnum Operate(SerializableEnum a, Type bType, UInt64 b, Func<UInt64, UInt64, UInt64> op)
        {
            if(a.m_Type != bType)
            {
                throw new EnumTypeMissMatchException($"{a}({a.m_Type?.Name}) vs {b}({bType?.Name})");
            }

            return new SerializableEnum()
            {
                m_Type = a.m_Type,
                m_Bits = op(a.m_Bits, b),
            };
        }
        #endregion // Operators


        #region Serialization
        public const string SERIALIZE_PATH_TYPE = nameof(m_TypeName);
        public const string SERIALIZE_PATH_NAMES = nameof(m_Names);
        public const string SERIALIZE_PATH_ISALL= nameof(m_IsAll);
        [SerializeField] string m_TypeName;
        [SerializeField] string m_Names;
        [SerializeField] bool m_IsAll;
        
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_IsAll = false;
            m_TypeName = null;
            m_Names = null;

            if(m_Type == default)
            {
                return;
            }
            
            var value = ToEnum(this);
            m_TypeName = value.GetType().AssemblyQualifiedName;
            m_Names = value.ToString();
            m_IsAll = m_Bits == ~0UL;
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            m_Type = default;
            m_Bits = default;

            if(string.IsNullOrEmpty(m_TypeName))
            {
                return;
            }

            m_Type = Type.GetType(m_TypeName);
            try
            {
                var value = (Enum)Enum.Parse(m_Type, m_Names);
                m_Bits = m_IsAll ? ~0UL : ToUInt64(value);
            }
            catch(Exception)
            {
                m_Bits = m_IsAll ? ~0UL : 0UL;
            }
        }
        #endregion // Serialization
    }
}
