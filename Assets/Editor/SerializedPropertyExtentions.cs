using System;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;

namespace SerializableTable
{
    static class Extentions
	{
		static  readonly Regex GET_INDEX = new Regex(@"\[([0-9]+)\]");

	
    	public static T GetInstance<T> (this SerializedProperty property) => GetInstance<T>(property.serializedObject.targetObject, property.propertyPath.Replace(".Array.data[", ".[").Split('.'));
	
    
    	static T GetInstance<T> (object obj, string[] path)
		{
			foreach (var fieldName in path)
			{
				if(!fieldName.Contains("["))
				{
					var field = obj.GetType().SerchMember(f => f.Name.Equals(fieldName, StringComparison.OrdinalIgnoreCase)).First() as FieldInfo;
					obj = field.GetValue(obj);
				}
				else
				{
                    var index = GET_INDEX.Match(fieldName).Groups[1].Value;
					obj = (obj as IList).OfType<object>().ElementAtOrDefault(int.Parse(index));
				}

				if(obj == null)
				{
					break;
				}
			}
			return obj != null ? (T)obj : default;
		}
    

		public static bool SetInstance(this SerializedProperty property, object value)
		{
			var path = property.propertyPath.Replace(".Array.data[", ".[").Split('.');
			object obj = GetInstance<object>(property.serializedObject.targetObject, path.Take(path.Length - 1).ToArray());

			if(!path.Last().Contains("["))
			{
				var field = obj.GetType().SerchMember(filter: f => f.Name.Equals(path.Last(), StringComparison.OrdinalIgnoreCase)).First() as FieldInfo;
				field.SetValue(obj, value);
			}
			else
			{
				var index = GET_INDEX.Match(path.Last()).Groups[1].Value;
                IList list = (obj as IList);
                list[int.Parse(index)] = value;
			}

			return true;
		}
	

        public static IEnumerable<MemberInfo> SerchMember(this Type type, Predicate<MemberInfo> filter)
        {
            var members = type.GetMembers(BindingFlags.Instance | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public);
            foreach (var member in members)
            {
                if (filter?.Invoke(member) != false)
                {
                    yield return member;
                }
            }

            if (type.BaseType != null && type.BaseType != type)
            {
				foreach (var member in type.BaseType.SerchMember(filter))
				{
					yield return member;
				}
			}
        }
    }
}
