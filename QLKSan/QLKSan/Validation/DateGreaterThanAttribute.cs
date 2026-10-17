using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QLKSan.Validation
{
    public class DateGreaterThanAttribute : ValidationAttribute, IClientModelValidator
    {
        private readonly string _comparisonProperty;

        public DateGreaterThanAttribute(string comparisonProperty)
        {
            _comparisonProperty = comparisonProperty;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime currentDate)
            {
                var property = validationContext.ObjectType.GetProperty(_comparisonProperty);
                if (property == null)
                {
                    return new ValidationResult($"Property with name {_comparisonProperty} not found");
                }

                var comparisonValue = property.GetValue(validationContext.ObjectInstance);
                if (comparisonValue is DateTime comparisonDate)
                {
                    // Ngày hiện tại phải lớn hơn ngày dùng để so sánh
                    if (currentDate <= comparisonDate)
                    {
                        return new ValidationResult(ErrorMessage ?? $"Ngày phải lớn hơn {_comparisonProperty}.");
                    }
                }
            }

            return ValidationResult.Success;
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-dategreaterthan", ErrorMessage ?? $"Ngày phải lớn hơn {_comparisonProperty}.");
            context.Attributes.Add("data-val-dategreaterthan-property", _comparisonProperty);
        }
    }
}
