using System;
using System.Collections.Generic;
using SerializableTable;
using UnityEngine;

enum Hoge
{
    A, B, C
}
[Flags]
enum Huga
{
    A = 1 << 0,
    B = 1 << 1,
    C = 1 << 2,
}

class Test : MonoBehaviour
{
    [SerializeField] SerializableEnum m_Enum = new SerializableEnum(Hoge.A);
    [Serializable] class Dictionary : SerializableEnumDictionary<Sprite> { }
    [SerializeField] Dictionary m_dic = new Dictionary { DefaultKey = new SerializableEnum(Huga.A) };
    IReadOnlyDictionary<SerializableEnum, Sprite> Dic => m_dic;

    void Start()
    {
        foreach (var kv in Dic)
        {
            Debug.Log($"[{kv.Key}]{kv.Value}", this);
        }
    }
}
