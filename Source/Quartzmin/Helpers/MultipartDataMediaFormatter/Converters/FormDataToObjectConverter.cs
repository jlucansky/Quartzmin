#if NETFRAMEWORK

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using MultipartDataMediaFormatter.Infrastructure;
using MultipartDataMediaFormatter.Infrastructure.Extensions;
using MultipartDataMediaFormatter.Infrastructure.Logger;

namespace MultipartDataMediaFormatter.Converters
{
    public class FormDataToObjectConverter
    {
        private readonly FormData SourceData;
        private readonly IFormDataConverterLogger Logger;
        private readonly MultipartFormatterSettings Settings;

        public FormDataToObjectConverter(FormData sourceData, IFormDataConverterLogger logger, MultipartFormatterSettings settings) 
        {
            if (sourceData == null)
                throw new ArgumentNullException("sourceData");
            if (logger == null)
                throw new ArgumentNullException("logger");
            if (settings == null)
                throw new ArgumentNullException("settings");

            Settings = settings;
            SourceData = sourceData;
            Logger = logger;
        }

        public object Convert(Type destinationType) 
        {
            if (destinationType == null)
                throw new ArgumentNullException("destinationType");

            if (destinationType == typeof(FormData))
                return SourceData;

            var objResult = CreateObject(destinationType);
            return objResult;
        } 

        private object CreateObject(Type destinationType, string propertyName = "")
        {
            object propValue = null;

            if (propertyName == null)
            {
                propertyName = "";
            }

            object buf;
            if (TryGetAsNotIndexedListOrArray(destinationType, propertyName, out buf)
                || TryGetFromFormData(destinationType, propertyName, out buf)
                || TryGetAsGenericDictionary(destinationType, propertyName, out buf)
                || TryGetAsIndexedGenericListOrArray(destinationType, propertyName, out buf)
                || TryGetAsCustomType(destinationType, propertyName, out buf))
            {
                propValue = buf;
            }
            else if (IsNotNullableValueType(destinationType)
                && IsNeedValidateMissedProperty(propertyName))
            {
                Logger.LogError(propertyName, "The value is required.");
            }
            else if (!IsFileOrConvertableFromString(destinationType))
            {
                Logger.LogError(propertyName, String.Format("Cannot parse type \"{0}\".", destinationType.FullName));
            }

            return propValue;
        }

        private bool TryGetAsNotIndexedListOrArray(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;

            Type genericListItemType;            
            if (IsGenericEnumerable(destinationType, out genericListItemType))
            {
                var items = GetNotIndexedListItems(propertyName, genericListItemType);
                propValue = MakeList(genericListItemType, destinationType, items, propertyName);
            }

            return propValue != null;
        }

        private List<object> GetNotIndexedListItems(string propertyName, Type genericListItemType)
        {
            List<object> res;
            if (!TryGetListFromFormData(genericListItemType, propertyName, out res))
            {
                TryGetListFromFormData(genericListItemType, propertyName + "[]", out res);
            }

            return res ?? new List<object>();
        }

        private bool TryGetFromFormData(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            List<object> values;
            if (TryGetListFromFormData(destinationType, propertyName, out values))
            {
                propValue = values.FirstOrDefault();
                return true;
            }
            return false;
        }

        private bool TryGetListFromFormData(Type destinationType, string propertyName, out List<object> propValue)
        {
            bool existsInFormData = false;
            propValue = null;

            if (destinationType == typeof(HttpFile) || destinationType == typeof(byte[]))
            {
                var files = SourceData.GetFiles(propertyName, Settings.CultureInfo);
                if (files.Any())
                {
                    existsInFormData = true;
                    propValue = new List<object>();

                    foreach (var httpFile in files)
                    {
                        var item = destinationType == typeof(byte[])
                            ? httpFile.Buffer
                            : (object)httpFile;

                        propValue.Add(item);
                    }
                }
            }
            else
            {
                var values = SourceData.GetValues(propertyName, Settings.CultureInfo);
                if (values.Any())
                {
                    existsInFormData = true;
                    propValue = new List<object>();

                    foreach (var value in values)
                    {
                        object val;
                        if(TryConvertFromString(destinationType, propertyName, value, out val))
                        {
                            propValue.Add(val);
                        }
                    }
                }
            }

            return existsInFormData;
        }

        private bool TryConvertFromString(Type destinationType, string propertyName, string val, out object propValue)
        {
            propValue = null;
            var typeConverter = destinationType.GetFromStringConverter();
            if (typeConverter == null)
            {
                Logger.LogError(propertyName, "Cannot find type converter for field - " + propertyName);
            }
            else
            {
                try
                {
                    propValue = typeConverter.ConvertFromString(val, Settings.CultureInfo);
                    return true;
                }
                catch (Exception ex)
                {
                    Logger.LogError(propertyName, String.Format("Error parsing field \"{0}\": {1}", propertyName, ex.Message));
                }
            }
            return false;
        }

        private bool TryGetAsGenericDictionary(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            Type keyType, valueType;            
            if (IsGenericDictionary(destinationType, out keyType, out valueType))
            {
                var dictType = typeof(Dictionary<,>).MakeGenericType(new[] { keyType, valueType });
                var add = dictType.GetMethod("Add");

                var pValue = Activator.CreateInstance(dictType);

                int index = 0;
                string origPropName = propertyName;
                bool isFilled = false;
                while (true)
                {
                    string propertyKeyName = String.Format("{0}[{1}].Key", origPropName, index);
                    var objKey = CreateObject(keyType, propertyKeyName);
                    if (objKey != null)
                    {
                        string propertyValueName = String.Format("{0}[{1}].Value", origPropName, index);
                        var objValue = CreateObject(valueType, propertyValueName);

                        if (objValue != null)
                        {
                            add.Invoke(pValue, new[] { objKey, objValue });
                            isFilled = true;
                        }
                    }
                    else
                    {
                        break;
                    }
                    index++;
                }

                if (isFilled || IsRootProperty(propertyName))
                {
                    propValue = pValue;
                }

                return true;
            }            
            return false;
        }

        private bool TryGetAsIndexedGenericListOrArray(Type destinationType, string propertyName, out object propValue)
        {            
            Type genericListItemType;            
            if (IsGenericEnumerable(destinationType, out genericListItemType))
            {
                var items = GetIndexedListItems(propertyName, genericListItemType);
                propValue =  MakeList(genericListItemType, destinationType, items, propertyName);
                return true;
            }

            propValue = null;
            return false;
        }

        private object MakeList(Type genericListItemType, Type destinationType, List<object> listItems, string propertyName)
        {
            object result = null;

            if (listItems.Any() || IsRootProperty(propertyName))
            {
                var listType = typeof(List<>).MakeGenericType(genericListItemType);

                var add = listType.GetMethod("Add");
                var pValue = Activator.CreateInstance(listType);

                foreach (var listItem in listItems)
                {
                    add.Invoke(pValue, new[] { listItem });
                }

                if (destinationType.IsArray)
                {
                    var toArrayMethod = listType.GetMethod("ToArray");
                    result = toArrayMethod.Invoke(pValue, new object[0]);
                }
                else
                {
                    result = pValue;
                }
            }

            return result;
        }

        private List<object> GetIndexedListItems(string origPropName, Type genericListItemType)
        {
            var res = new List<object>();
            int index = 0;
            while (true)
            {
                var propertyName = String.Format("{0}[{1}]", origPropName, index);
                var objValue = CreateObject(genericListItemType, propertyName);
                if (objValue != null)
                {
                    res.Add(objValue);
                }
                else
                {
                    break;
                }

                index++;
            }
            return res;
        }

        private bool TryGetAsCustomType(Type destinationType, string propertyName, out object propValue)
        {
            propValue = null;
            bool isCustomNonEnumerableType = destinationType.IsCustomNonEnumerableType();
            if (isCustomNonEnumerableType && IsRootPropertyOrAnyChildPropertiesExistsInFormData(propertyName))
            {
                propValue = Activator.CreateInstance(destinationType);
                foreach (PropertyInfo propertyInfo in destinationType.GetProperties().Where(m => m.SetMethod != null))
                {
                    var propName = (!String.IsNullOrEmpty(propertyName) ? propertyName + "." : "") + propertyInfo.Name;

                    var objValue = CreateObject(propertyInfo.PropertyType, propName);
                    if (objValue != null)
                    {
                        propertyInfo.SetValue(propValue, objValue);
                    }
                }
            }
            return isCustomNonEnumerableType;
        }


        private bool IsGenericDictionary(Type type, out Type keyType, out Type valueType)
        {
            var iDictType = GetGenericType(type, typeof(IDictionary<,>));
            if (iDictType != null)
            {
                var types = iDictType.GetGenericArguments();
                if (types.Length == 2)
                {
                    keyType = types[0];
                    valueType = types[1];
                    return true;
                }
            }

            keyType = null;
            valueType = null;
            return false;
        }

        private bool IsGenericEnumerable(Type type, out Type itemType)
        {
            if (GetGenericType(type, typeof(IDictionary<,>)) == null //not a dictionary
                && !type.Equals(typeof(string))) //not a string
            {
                var enumerType = GetGenericType(type, typeof(IEnumerable<>));
                if (enumerType != null) 
                {
                    Type[] genericArguments = enumerType.GetGenericArguments();
                    if (genericArguments.Length == 1)
                    {
                        itemType = genericArguments[0];
                        return true;
                    }
                }
            }
          
            itemType = null;
            return false;
        }

        private Type GetGenericType(Type type, Type genericTypeDefinition)
        {            
            return type.IsGenericType && genericTypeDefinition.Equals(type.GetGenericTypeDefinition())
                ? type
                : type.GetInterface(genericTypeDefinition.Name);
        }

        private bool IsFileOrConvertableFromString(Type type)
        {
            if (type == typeof (HttpFile))
                return true;

            return type.GetFromStringConverter() != null;
        }

        private bool IsNotNullableValueType(Type type)
        {
            if (!type.IsValueType)
                return false;

            return Nullable.GetUnderlyingType(type) == null;
        }

        private bool IsNeedValidateMissedProperty(string propertyName)
        {
            return Settings.ValidateNonNullableMissedProperty
                    && !IsIndexedProperty(propertyName)
                    && IsRootPropertyOrAnyParentsPropertyExistsInFormData(propertyName);
        }

        private bool IsRootPropertyOrAnyParentsPropertyExistsInFormData(string propertyName)
        {
            string parentName = "";
            if (propertyName != null)
            {
                int lastDotIndex = propertyName.LastIndexOf('.');
                if (lastDotIndex >= 0)
                {
                    parentName = propertyName.Substring(0, lastDotIndex);
                }
            }

            bool result = IsRootPropertyOrAnyChildPropertiesExistsInFormData(parentName);
            return result;
        }

        private bool IsRootPropertyOrAnyChildPropertiesExistsInFormData(string propertyName)
        {
            if (IsRootProperty(propertyName))
                return true;

            string prefixWithDot = propertyName + ".";
            bool result = SourceData.GetAllKeys().Any(m => m.StartsWith(prefixWithDot, true, Settings.CultureInfo));
            return result;
        }

        private bool IsRootProperty(string propertyName)
        {
            return propertyName == "";
        }

        private bool IsIndexedProperty(string propName)
        {
            return propName != null && propName.EndsWith("]");
        }
    }
}
#endif