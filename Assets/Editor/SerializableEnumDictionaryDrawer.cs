using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using SerializableTable;
using System.Collections;
using UnityEditorInternal;
using System.Reflection;

namespace SerializableTable
{
    [CustomPropertyDrawer(typeof(SerializableEnumDictionary<>), true)] 
	public class SerializableEnumDictionaryDrawer : PropertyDrawer
	{
		static readonly Lazy<GUIContent> EMPTY = new Lazy<GUIContent>(() => new GUIContent(string.Empty));
		static readonly Lazy<GUIStyle> RITCH = new Lazy<GUIStyle>(() => new GUIStyle(GUI.skin.label){richText = true});

        ReorderableList m_RList;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if(m_RList == null)
            {
                m_RList = CreateRList(property);
            }

			var instance = property.GetInstance<IDictionary>();
			return EditorGUIUtility.singleLineHeight + m_RList.GetHeight();
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if(m_RList == null)
            {
                m_RList = CreateRList(property);
            }

            using (new EditorGUI.PropertyScope(position, label, property))
			{
				EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
				var contentsRect = 
					string.IsNullOrEmpty(label.text)
					? position
					: new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height);

				m_RList.DoList(contentsRect);
			}
		}

        ReorderableList CreateRList(SerializedProperty property)
        {
            var defaultKeyProp = property.FindPropertyRelative(SerializableEnumDictionary<object>.SERIALIZE_PATH_DEFAULT_KEY);
            var addKeyProp = property.FindPropertyRelative(SerializableEnumDictionary<object>.SERIALIZE_PATH_NEW_KEY);
            var keysProp = property.FindPropertyRelative(SerializableEnumDictionary<object>.SERIALIZE_PATH_KEYS);
            var valuesProp = property.FindPropertyRelative(SerializableEnumDictionary<object>.SERIALIZE_PATH_VALUES);

            var lastSelectIndex = 0;

            return new ReorderableList(property.serializedObject, keysProp, true, true, true, true)
            {
                onSelectCallback = _ => lastSelectIndex = m_RList.index,
                drawHeaderCallback = pos =>
                {
                    var keys = keysProp.GetInstance<IList<SerializableEnum>>();
                    var isDuplicated = keys.GroupBy(k => k).Any(g => g.Count() > 1);
                    var defaultKey = defaultKeyProp.GetInstance<SerializableEnum>();
                    GUI.skin.label.richText = true;
                    EditorGUI.LabelField(pos, $"key: {((Enum)defaultKey)?.GetType().Name} {(isDuplicated ? "<color=#FF0000>Duplicated keys</color>" : "")}", RITCH.Value);
				
					Event current = Event.current;
					if(pos.Contains(current.mousePosition) &&  Event.current.type == EventType.ContextClick)
					{
						var instance = property.GetInstance<IDictionary>();
						GenericMenu menu = new GenericMenu();
						menu.AddItem(new GUIContent("Fill"), false, () =>
						{
							var more = Enum.GetValues(((Enum)defaultKey).GetType()).OfType<Enum>()
								.Select(e => new SerializableEnum(e))
								.Where(se => !instance.Contains(se));
							foreach (var key in more)
							{
								instance[key] = null;
							}
                    		property.serializedObject.ApplyModifiedProperties();
						});
						menu.AddItem(new GUIContent("Sort"), false, () =>
						{
							var kvs = instance.Keys.OfType<SerializableEnum>()
								.Select(k => (k, instance[k]))
								.OrderBy(kv => (UInt64)kv.k)
								.ToArray();
							instance.Clear();
							foreach (var (key, value) in kvs)
							{
								instance[key] = value;
							}
							property.serializedObject.ApplyModifiedProperties();
						});
						menu.ShowAsContext();
						current.Use(); 
					}
                },
                footerHeight = EditorGUIUtility.singleLineHeight * 2,
                drawFooterCallback = pos =>
                {
					var footerRectEx = new Rect(pos.x, pos.y, pos.width, EditorGUIUtility.singleLineHeight * 2);
					GUI.Box(footerRectEx, string.Empty, ReorderableList.defaultBehaviours.headerBackground);
                    var left = new Rect(pos.x, pos.y, pos.width - 50, pos.height);
                    var right = new Rect(pos.x + left.width, pos.y, 50, pos.height);
                    if (EditorGUI.PropertyField(left, defaultKeyProp, new GUIContent("NewKey")))
                    {
                        property.serializedObject.ApplyModifiedProperties();
                    }

					var newKey = defaultKeyProp.GetInstance<SerializableEnum>();
					var instance = property.GetInstance<IDictionary>();
					EditorGUI.BeginDisabledGroup(instance.Contains(newKey));
					if (GUI.Button(right, "ADD"))
					{
						var index = m_RList.index < 0 ? m_RList.count : Mathf.Clamp(m_RList.index, 0, m_RList.count);
						var defaultKey = defaultKeyProp.GetInstance<SerializableEnum>();
						property.GetInstance<IDictionary>()[defaultKey] = null;
						property.serializedObject.ApplyModifiedProperties();
					}
					EditorGUI.EndDisabledGroup();
                },
                onRemoveCallback = _ =>
                {
                    var index = m_RList.index < 0 ? m_RList.count : Mathf.Clamp(m_RList.index, 0, m_RList.count);
                    var instance = property.GetInstance<IDictionary>();
                    var key = instance.Keys.OfType<SerializableEnum>().ElementAt(index);
                    instance.Remove(key);
                    property.serializedObject.ApplyModifiedProperties();
                },
                onReorderCallbackWithDetails = (_, old, @new) =>
                {
					valuesProp.MoveArrayElement(old, @new);
                    property.serializedObject.ApplyModifiedProperties();
                },
                elementHeightCallback = i =>
                {
                    var keyHeight = EditorGUI.GetPropertyHeight(keysProp.GetArrayElementAtIndex(i));
                    var valueHeight = EditorGUI.GetPropertyHeight(valuesProp.GetArrayElementAtIndex(i));
                    return Mathf.Max(keyHeight, valueHeight);
                },
                onCanAddCallback = _ => true,
                drawElementCallback = (pos, i, _, __) =>
                {
                    var left = new Rect(pos.x, pos.y, pos.width / 2, pos.height);
                    var right = new Rect(pos.x + pos.width / 2, pos.y, pos.width / 2, pos.height);
                    EditorGUI.PropertyField(left, keysProp.GetArrayElementAtIndex(i), EMPTY.Value);
                    EditorGUI.PropertyField(right, valuesProp.GetArrayElementAtIndex(i), EMPTY.Value);
                },
            };
        }
	}
}
