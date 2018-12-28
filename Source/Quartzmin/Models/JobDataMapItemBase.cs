using Quartzmin.TypeHandlers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Quartzmin.Models
{
    public class JobDataMapItemBase : IHasValidation
    {
        [Required]
        public string Name { get; set; }

        public object Value { get; set; }

        [Required]
        public TypeHandlerBase SelectedType { get; set; }

        public bool IsLast { get; set; }

        public string RowId { get; set; }

        const string NameField = "data-map[name]";
        const string HandlerField = "data-map[handler]";
        const string TypeField = "data-map[type]";
        const string IndexField = "data-map[index]";
        const string ValueField = "data-map[value]";
        const string LastItemField = "data-map[lastItem]";

        public static JobDataMapItemBase FromDictionary(Dictionary<string, object> formData, Services services)
        {
            var valueFormData = new Dictionary<string, object>();

            var result = new JobDataMapItemBase();

            foreach (var item in formData)
            {
                if (item.Key == NameField)
                {
                    result.Name = (string)item.Value;
                    continue;
                }
                if (item.Key == HandlerField)
                {
                    result.SelectedType = services.TypeHandlers.Deserialize((string)item.Value);
                    continue;
                }
                if (item.Key == TypeField)
                {
                    continue;
                }
                if (item.Key == IndexField)
                {
                    result.RowId = (string)item.Value;
                    continue;
                }
                if (item.Key == LastItemField)
                {
                    result.IsLast = Convert.ToBoolean(item.Value);
                    continue;
                }

                valueFormData.Add(item.Key, item.Value);
            }

            if (result.SelectedType != null)
                result.Value = result.SelectedType.ConvertFrom(valueFormData);

            return result;
        }

        public override string ToString()
        {
            if (Name != null)
            {
                if (Value != null)
                    return $"{Name} = {Value}";
                else
                    return Name;
            }

            return base.ToString();
        }

        public void Validate(ICollection<ValidationError> errors)
        {
            if (string.IsNullOrEmpty(Name))
                AddValidationError(NameField, errors);

            if (SelectedType == null)
                AddValidationError(TypeField, errors);

            if (SelectedType?.IsValid(Value) == false)
                AddValidationError(ValueField, errors);
        }

        void AddValidationError(string field, ICollection<ValidationError> errors)
        {
            errors.Add(ValidationError.EmptyField(field + ":" + RowId));
        }
    }
}
