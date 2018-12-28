using Quartzmin.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Quartzmin.Helpers
{
    public static class ModelValidator
    {
        public static void ValidateObject<T>(IEnumerable<T> collection, ICollection<ValidationError> errors, string ownerField = null)
        {
            foreach (var item in collection)
            {
                ValidateObject(item, errors, ownerField);
            }
        }

        public static void Validate<T>(IEnumerable<T> collection, ICollection<ValidationError> errors)
            where T: IHasValidation
        {
            foreach (var item in collection)
            {
                item.Validate(errors);
            }
        }

        public static void ValidateObject(object obj, ICollection<ValidationError> errors, params string[] ownerField)
        {
            ValidateObject(obj, errors, true, ownerField);
        }

        public static void ValidateObject(object obj, ICollection<ValidationError> errors, bool camelCase, params string[] ownerField)
        {
            var members = obj.GetType().GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);

            foreach (var p in members.OfType<PropertyInfo>())
            {
                if (p.GetCustomAttribute<SkipValidationAttribute>() != null)
                    continue;

                var required = p.GetCustomAttribute<RequiredAttribute>();

                if (required?.IsValid(p.GetValue(obj)) == false)
                {
                    errors.Add(ValidationError.EmptyField(GetFieldName(p.Name, camelCase, ownerField)));
                }

                if (p.GetValue(obj) is IHasValidation nestedValidation)
                {
                    nestedValidation.Validate(errors);
                }
            }
        }

        public static string GetFieldName(string field, bool camelCase, params string[] ownerField)
        {
            if (camelCase)
            {
                field = FirstCharToLower(field);
                ownerField = FirstCharToLower(ownerField);
            }

            if (ownerField == null || ownerField.Length == 0)
                return field;

            var path = string.Join(".", ownerField.Skip(1));

            if (!string.IsNullOrEmpty(path))
                path += ".";

            return $"{ownerField[0]}[{path}{field}]";
        }

        static string[] FirstCharToLower(params string[] inputs)
        {
            if (inputs == null)
                return null;
            return inputs.Select(x => FirstCharToLower(x)).ToArray();
        }

        static string FirstCharToLower(string input)
        {
            if (String.IsNullOrEmpty(input))
                return input;

            return input[0].ToString().ToLower() + input.Substring(1);
        }


    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field , AllowMultiple = false)]
    public class SkipValidationAttribute : Attribute
    {

    }
}