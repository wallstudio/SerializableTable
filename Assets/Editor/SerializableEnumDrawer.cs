using System;
using System.Linq;
using System.Collections;
using SerializableTable;
using UnityEditor;
using UnityEngine;

namespace SerializableTable
{
    [CustomPropertyDrawer(typeof(SerializableEnum))]
	public class SerializableEnumDrawer : PropertyDrawer
	{
		static readonly Lazy<GUIStyle> RITCH = new Lazy<GUIStyle>(() => new GUIStyle(GUI.skin.label){richText = true, wordWrap  = true});

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) => EditorGUIUtility.singleLineHeight * 2;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var typeProp = property.FindPropertyRelative(SerializableEnum.SERIALIZE_PATH_TYPE);
			var namesProp = property.FindPropertyRelative(SerializableEnum.SERIALIZE_PATH_NAMES);
			var isAllProp = property.FindPropertyRelative(SerializableEnum.SERIALIZE_PATH_ISALL);
			var instance = property.GetInstance<SerializableEnum>();
			var value = (Enum)instance;

			var type = value?.GetType();
			var isFlag = type?.IsDefined(typeof(FlagsAttribute), false) == true;
			Enum newValue = null;

			using(new EditorGUI.PropertyScope(position, label, property))
			{
				var contentsRect = string.IsNullOrEmpty(label.text)
					? position
					: EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), new GUIContent($"{label.text} <{(isFlag ? "[Flags]" : "")}{type?.Name}>"));

				var valueRect = new Rect(contentsRect.x, contentsRect.y, contentsRect.width, EditorGUIUtility.singleLineHeight);
				if(type != null)
				{
					newValue = isFlag ? EditorGUI.EnumFlagsField(valueRect, value) : EditorGUI.EnumPopup(valueRect, value);
					if(value != newValue)
					{
						property.SetInstance(new SerializableEnum(newValue));
						property.serializedObject.ApplyModifiedProperties();
					}
				}
				else
				{
					// EditorGUI.LabelField(valueRect, "Typeが設定されされていません");
					EditorGUI.LabelField(valueRect, "Typeが設定されされていません");
				}

				var dumpRect = new Rect(contentsRect.x, contentsRect.y + valueRect.height, contentsRect.width, EditorGUIUtility.singleLineHeight);
				EditorGUI.LabelField(dumpRect, $"<size={GUI.skin.label.fontSize / 2}>{value}</size>", RITCH.Value);
			}			
		}
	}
}