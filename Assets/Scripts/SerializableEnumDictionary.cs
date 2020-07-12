using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SerializableTable
{
    public class SerializableEnumDictionary<TValue> : Dictionary<SerializableEnum, TValue>, ISerializationCallbackReceiver
    {
        public SerializableEnum DefaultKey { get => m_DefaultKey; set => m_DefaultKey = value; }


        public const string SERIALIZE_PATH_DEFAULT_KEY = nameof(m_DefaultKey);
        public const string SERIALIZE_PATH_NEW_KEY = nameof(m_NewKey);
        public const string SERIALIZE_PATH_KEYS = nameof(m_Keys);
        public const string SERIALIZE_PATH_VALUES = nameof(m_Values);
        [SerializeField] SerializableEnum m_DefaultKey;
        [SerializeField] SerializableEnum m_NewKey;
        [SerializeField] List<SerializableEnum> m_Keys;
        [SerializeField] List<TValue> m_Values;


        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            m_Keys = new List<SerializableEnum>();
            m_Values = new List<TValue>();

            foreach (var kv in this)
            {
                m_Keys.Add(kv.Key);
                m_Values.Add(kv.Value);
            }
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            Clear();

            foreach (var (key, value) in m_Keys.Zip(m_Values, (k, v) => (k, v)))
            {
                this[key] = value;
            }

            DefaultKey = m_DefaultKey;
        }
    }
}
