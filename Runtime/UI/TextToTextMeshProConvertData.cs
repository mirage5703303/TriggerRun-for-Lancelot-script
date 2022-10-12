using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VLB;

namespace IEdgeGames
{
    public class TextToTextMeshProConvertData : SerializedMonoBehaviour
    {
        public class ReflectionData
        {
            [NonSerialized, OdinSerialize] public string FullTypeName;
            [NonSerialized, OdinSerialize] public string FieldName;
            [NonSerialized, OdinSerialize] public GameObject TextGameObject;
            [NonSerialized, OdinSerialize] public object Instance;
        }

        [NonSerialized, OdinSerialize] public List<ReflectionData> ReflectionDataList = null;

        [Button]
        public void StoreReferences()
        {
            ReflectionDataList = new List<ReflectionData>();
            var behaviours = GetComponentsInChildren<MonoBehaviour>();
            Dictionary<Type, KeyValuePair<MemberInfo, object>[]> typeCache =
                new Dictionary<Type, KeyValuePair<MemberInfo, object>[]>();
            foreach (var behaviour in behaviours)
            {
                var type = behaviour.GetType();

                if (!typeCache.TryGetValue(type, out KeyValuePair<MemberInfo, object>[] memberInfos))
                {
                    var members = type.GetMemberInfosFromType(behaviour, false,
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    memberInfos = members
                        .Where(info =>
                        {
                            if (info.Key is FieldInfo fieldInfo)
                            {
                                // if private, and there is no serializable + odinserialize attribute, return false;
                                if (fieldInfo.IsPrivate &&
                                    (fieldInfo.GetCustomAttribute<SerializableAttribute>() == null ||
                                     fieldInfo.GetCustomAttribute<OdinSerializeAttribute>() == null))
                                    return false;
                            }

                            // Serializable value
                            return true;
                        })
                        .Where(info =>
                        {
                            var memberType = info.Key.GetTypeFromMember();

                            // Array of text
                            if (memberType.IsArray && (memberType.GetElementType() == typeof(Text) ||
                                                       memberType.GetElementType() == typeof(TextMeshProUGUI)))
                            {
                                return true;
                            }

                            // List of text
                            if (typeof(IList).IsAssignableFrom(memberType) &&
                                memberType.GenericTypeArguments.Any(t =>
                                {
                                    return (t == typeof(Text) ||
                                            t == typeof(TextMeshProUGUI));
                                }))
                            {
                                return true;
                            }

                            // Single text
                            return (memberType == typeof(Text) ||
                                    memberType == typeof(TextMeshProUGUI));
                        }).ToArray();

                    typeCache.Add(type, memberInfos);
                }

                foreach (KeyValuePair<MemberInfo, object> info in memberInfos)
                {
                    var value = info.Key.GetValueFromMember(info.Value) as Text;
                    if (value == null) continue;
                    ReflectionDataList.Add(new ReflectionData()
                    {
                        FullTypeName = info.Key.DeclaringType?.FullName ?? info.Key.ReflectedType?.FullName,
                        FieldName = info.Key.Name,
                        Instance = info.Value,
                        TextGameObject = value != null ? value.gameObject : null
                    });
                }

            }
        }

        [Button]
        public void ResolveReferences()
        {
            if (ReflectionDataList == null) return;

            for (var i = 0; i < ReflectionDataList.Count; i++)
            {
                ReflectionData reflectionData = ReflectionDataList[i];
                var type = Type.GetType(reflectionData.FullTypeName, false, true);
                if (type == null)
                {
                    Debug.LogError($"[type] {reflectionData.FullTypeName} is null");
                    continue;
                }

                var memberInfo = type.GetField(reflectionData.FieldName,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (memberInfo == null)
                {
                    Debug.LogError($"[member] {reflectionData.FullTypeName}->{reflectionData.FieldName} is null");
                    continue;
                }

                var fromMember = memberInfo.GetTypeFromMember();
                // Wait until user changes this to TextMeshProUGUI
                if (fromMember == typeof(Text)) continue;

                if (fromMember == typeof(TextMeshProUGUI))
                {
                    var component = reflectionData.TextGameObject.GetComponent(fromMember);
                    if (component != null)
                    {
                        memberInfo.SetValueToMember(reflectionData.Instance, component);
                        ReflectionDataList.RemoveAt(i);
                        i--;
                    }
                }
                else
                {
                    Debug.LogError("Unhandled type: " + fromMember);
                }
            }

#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(gameObject);
#endif

            if (ReflectionDataList.Count == 0)
            {
                DestroyImmediate(this, true);
            }
        }

        public void OnEnable()
        {
            ResolveReferences();
        }
    }
    
    
    public static class ReflectionHelperExtension
    {
        public static object ResolveInstanceFromMemberInfo(this object instance, MemberInfo targetMemberInfo, out bool result)
        {
            if (instance == null)
            {
                result = false;
                return null;
            }

            var list = instance.GetMemberInfosFromInstance(true);
            var found = list.FirstOrDefault(pair => pair.Key == targetMemberInfo);
            if (found.Key != null)
            {
                result = true;
                return found.Value;
            }

            foreach (KeyValuePair<MemberInfo, object> pair in list)
            {
                var newInstance = ResolveInstanceFromMemberInfo(pair.Key.GetValueFromMember(pair.Value), targetMemberInfo, out result);
                if (result) return newInstance;
            }

            result = false;
            return null;
        }

        public static string GetJsonPropertyName(this MemberInfo member)
        {
            JsonPropertyAttribute jsonProperty = member.GetAttribute<JsonPropertyAttribute>();
            return jsonProperty != null ? jsonProperty.PropertyName : member.Name;
        }

        public static KeyValuePair<MemberInfo, object>[] GetMemberInfosFromInstance(this object instance, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return instance == null ? new KeyValuePair<MemberInfo, object>[0] : GetMemberInfosFromType(instance.GetType(), instance, includeProperty, flags);
        }
    
        public static KeyValuePair<MemberInfo, object>[] GetMemberInfosFromType(this Type type, object instance, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            List<KeyValuePair<MemberInfo, object>> list = type.GetMembers(flags)
                .Where(info => info.MemberType == MemberTypes.Field || includeProperty && info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(true);
                    if (attris.Any(o => o is IncludeEditorMemberAttribute)) return true;
                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo)info;
                        res = includeProperty && prop.GetGetMethod() != null;
                    }

                    return res && (attris.Length == 0 || 
#if !NO_UNITY
                                   attris.Any(o => o is MinMaxRangeAttribute) ||
 #endif
                                   attris.All(o => !(o is NonSerializedAttribute) && !(o is JsonIgnoreAttribute)));
                }).Select((info, i) => new KeyValuePair<MemberInfo, object>(info, instance)).ToList();


            for (int i = 0; i < list.Count; i++)
            {
                KeyValuePair<MemberInfo, object> pair = list[i];

                var attris = pair.Key.GetCustomAttributes(true);
                if (attris.Any(o => o is IncludeEditorMemberAttribute))
                {
                    var filter = (IncludeEditorMemberAttribute)attris.First(o => o is IncludeEditorMemberAttribute);

                    switch (filter.IncludeType)
                    {
                        case IncludeObjectType.MemberSelf:
                        {
                            // Do nothing
                        }
                            break;
                        case IncludeObjectType.MemberContent:
                        {
                            list.RemoveAt(i);
                            if (filter.SortingType == SortType.Insert)
                                list.InsertRange(i, GetMemberInfosFromType(pair.Key.GetTypeFromMember(), instance != null ? pair.Key.GetValueFromMember(pair.Value) : null, filter.IncludeProperty));
                            else
                                list.AddRange(GetMemberInfosFromType(pair.Key.GetTypeFromMember(), instance != null ? pair.Key.GetValueFromMember(pair.Value) : null, filter.IncludeProperty));
                            i--;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return list.ToArray();
        }

        public static MemberInfo[] GetMemberInfosByTypeOnly(this Type type, bool includeProperty)
        {
            List<MemberInfo> list = type.GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Where(info => info.MemberType == MemberTypes.Field ||
                               info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(true);
                    if (attris.Any(o => o is IncludeEditorMemberAttribute)) return true;

                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo) info;
                        res = includeProperty && prop.GetGetMethod() != null;
                    }

                    return res && (attris.Length == 0 || 
#if !NO_UNITY
                                   attris.Any(o => o is MinMaxRangeAttribute) ||
#endif
                                   attris.All(o => !(o is NonSerializedAttribute) && !(o is JsonIgnoreAttribute)));
                }).ToList();

            for (int i = 0; i < list.Count; i++)
            {
                MemberInfo member = list[i];

                var attris = member.GetCustomAttributes(true);
                if (attris.Any(o => o is IncludeEditorMemberAttribute))
                {
                    var filter = (IncludeEditorMemberAttribute) attris.First(o => o is IncludeEditorMemberAttribute);

                    switch (filter.IncludeType)
                    {
                        case IncludeObjectType.MemberSelf:
                        {
                            // Do nothing
                        }
                            break;
                        case IncludeObjectType.MemberContent:
                        {
                            list.RemoveAt(i);
                            if (filter.SortingType == SortType.Insert)
                                list.InsertRange(i, GetMemberInfosByTypeOnly(member.GetTypeFromMember(), filter.IncludeProperty));
                            else
                                list.AddRange(GetMemberInfosByTypeOnly(member.GetTypeFromMember(), filter.IncludeProperty));
                            i--;
                        }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return list.ToArray();
        }

        public static T GetAttribute<T>(this MemberInfo memberInfo) where T : Attribute
        {
            return memberInfo != null ? memberInfo.GetCustomAttributes(false).OfType<T>().FirstOrDefault() : default(T);
        }

        static List<Type> GetBaseTypes(object instance)
        {
            List<Type> list = new List<Type>();
            Type t = instance.GetType();
            while (t != null)
            {
                list.Add(t);
                t = t.BaseType;
                if (t == typeof(object)) break;
            }
            return list;
        }
        public static MemberInfo[] GetMemberInfos(this object instance, bool includeProperty, bool includeBaseType, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            if (instance == null)
                return new MemberInfo[0];

            if (!includeBaseType) return GetMemberInfoFromType(instance.GetType(), includeProperty, flags);
            
            List<MemberInfo> infos = new List<MemberInfo>();
            List<Type> types = GetBaseTypes(instance);
            foreach (var type in types)
            {
                infos.AddRange(GetMemberInfoFromType(type, includeProperty, flags));
            }
            return infos.GroupBy(info => info.Name).Select(group => group.Last()).ToArray();
        }

        public static MemberInfo[] GetMemberInfoFromType(this Type type, bool includeProperty, BindingFlags flags = BindingFlags.Instance | BindingFlags.Public)
        {
            return type.GetMembers(flags)
                .Where(info => info.MemberType == MemberTypes.Field ||
                               info.MemberType == MemberTypes.Property).Where(info =>
                {
                    var attris = info.GetCustomAttributes(false);
                    bool res = true;

                    if (info.MemberType == MemberTypes.Property)
                    {
                        PropertyInfo prop = (PropertyInfo)info;
                        res = includeProperty && prop.GetGetMethod() != null && prop.GetSetMethod() != null;
                    }

                    return res && (attris.Length == 0 || attris.All(o => !(o is NonSerializedAttribute) && !(o is JsonIgnoreAttribute)));
                }).ToArray();
        }

        public static IEnumerable<Type> GetDerivedTypes(this Type type)
        {
            return Assembly.GetAssembly(type).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && !myType.IsInterface && myType.IsSubclassOf(type));
        }
        
        public static Type GetTypeFromMember(this MemberInfo member)
        {
            if (member == null) return null;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).FieldType;
                case MemberTypes.Property:
                    return ((PropertyInfo)member).PropertyType;
            }

            return null;
        }

        public static object GetValueFromMember(this MemberInfo member, object instance)
        {
            if (member == null) return null;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                    return ((FieldInfo)member).GetValue(instance);
                case MemberTypes.Property:
                    if (((PropertyInfo) member) is {CanRead: true} prop)
                    {
                        return prop.GetValue(instance, null);
                    }
                    break;
            }

            return null;
        }


        public static bool SetValueToMember(this MemberInfo member, object instance, object value)
        {
            if (member == null) return false;

            switch (member.MemberType)
            {
                case MemberTypes.Field:
                {
                        FieldInfo field = (FieldInfo)member;
                        //Type nullableType = Nullable.GetUnderlyingType(field.FieldType);
                        //Type fieldType = nullableType != null ? nullableType : field.FieldType;
                        Type fieldType = field.FieldType;

                        if (fieldType == typeof(short)) field.SetValue(instance, Convert.ToInt16(value));
                        else if (fieldType == typeof(byte)) field.SetValue(instance, Convert.ToByte(value));
                        else if (fieldType == typeof(sbyte)) field.SetValue(instance, Convert.ToSByte(value));
                        else if (fieldType == typeof(int)) field.SetValue(instance, Convert.ToInt32(value));
                        else if (fieldType == typeof(long)) field.SetValue(instance, Convert.ToInt64(value));
                        else if (fieldType == typeof(ushort)) field.SetValue(instance, Convert.ToUInt16(value));
                        else if (fieldType == typeof(uint)) field.SetValue(instance, Convert.ToUInt32(value));
                        else if (fieldType == typeof(ulong)) field.SetValue(instance, Convert.ToUInt64(value));
                        else if (fieldType == typeof(float)) field.SetValue(instance, Convert.ToSingle(value));
                        else if (fieldType == typeof(short?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt16(value));
                        else if (fieldType == typeof(byte?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToByte(value));
                        else if (fieldType == typeof(sbyte?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToSByte(value));
                        else if (fieldType == typeof(int?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt32(value));
                        else if (fieldType == typeof(long?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt64(value));
                        else if (fieldType == typeof(ushort?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt16(value));
                        else if (fieldType == typeof(uint?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt32(value));
                        else if (fieldType == typeof(ulong?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt64(value));
                        else if (fieldType == typeof(float?)) field.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToSingle(value));
                        else if (fieldType == typeof(DBNull)) field.SetValue(instance, null);
                        else if (fieldType.IsEnum)
                        {
                            object enumValue = value != null ? Enum.Parse(fieldType, $"{value}", true) : null;
                            field.SetValue(instance, enumValue);
                        }
                        else field.SetValue(instance, value == DBNull.Value ? null : value);
                        return true;
                    }
                case MemberTypes.Property:
                {
                        PropertyInfo property = (PropertyInfo)member;
                        if (!property.CanWrite) return false;
                        //Type nullableType = Nullable.GetUnderlyingType(property.PropertyType);
                        //Type propertyType = nullableType != null ? nullableType : property.PropertyType;
                        Type propertyType = property.PropertyType;

                        if (propertyType == typeof(short)) property.SetValue(instance, Convert.ToInt16(value), null);
                        else if (propertyType == typeof(byte)) property.SetValue(instance, Convert.ToByte(value), null);
                        else if (propertyType == typeof(sbyte)) property.SetValue(instance, Convert.ToSByte(value), null);
                        else if (propertyType == typeof(int)) property.SetValue(instance, Convert.ToInt32(value), null);
                        else if (propertyType == typeof(long)) property.SetValue(instance, Convert.ToInt64(value), null);
                        else if (propertyType == typeof(ushort)) property.SetValue(instance, Convert.ToUInt16(value), null);
                        else if (propertyType == typeof(uint)) property.SetValue(instance, Convert.ToUInt32(value), null);
                        else if (propertyType == typeof(ulong)) property.SetValue(instance, Convert.ToUInt64(value), null);
                        else if (propertyType == typeof(float)) property.SetValue(instance, Convert.ToSingle(value), null);
                        else if (propertyType == typeof(short?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt16(value), null);
                        else if (propertyType == typeof(byte?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToByte(value), null);
                        else if (propertyType == typeof(sbyte?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToSByte(value), null);
                        else if (propertyType == typeof(int?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt32(value), null);
                        else if (propertyType == typeof(long?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToInt64(value), null);
                        else if (propertyType == typeof(ushort?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt16(value), null);
                        else if (propertyType == typeof(uint?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt32(value), null);
                        else if (propertyType == typeof(ulong?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToUInt64(value), null);
                        else if (propertyType == typeof(float?)) property.SetValue(instance, value == DBNull.Value || value == null ? (object)null : Convert.ToSingle(value));
                        else if (propertyType == typeof(DBNull)) property.SetValue(instance, null, null);
                        else if (propertyType.IsEnum)
                        {
                            object enumValue = value != null ? Enum.Parse(propertyType, $"{value}", true) : null;
                            property.SetValue(instance, enumValue, null);
                        }
                        else property.SetValue(instance, value == DBNull.Value ? null : value, null);
                        return true;
                    }
            }

            return false;
        }
    }
    
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public class IncludeEditorMemberAttribute : Attribute
    {
        public SortType SortingType = SortType.Insert;
        public IncludeObjectType IncludeType = IncludeObjectType.MemberSelf;
        public bool IncludeProperty = true;
    }

    public enum IncludeObjectType
    {
        /// <summary>
        /// Only show the member itself
        /// </summary>
        MemberSelf,
        /// <summary>
        /// Only show the contents of the member
        /// </summary>
        MemberContent,
    }

    public enum SortType
    {
        /// <summary>
        /// Insert into the collection using the current index
        /// </summary>
        Insert,
        /// <summary>
        /// Add to the end of the collection
        /// </summary>
        Add,
    }
}