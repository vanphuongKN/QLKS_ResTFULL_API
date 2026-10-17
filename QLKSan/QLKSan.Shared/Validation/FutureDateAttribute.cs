using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace QLKSan.Validation
{
    public class FutureDateAttribute : ValidationAttribute, IClientModelValidator
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime date)
            {
                // Cho phép ngày hôm nay hoặc tương lai (chỉ so sánh phần ngày, bỏ qua giờ)
                if (date.Date >= DateTime.Now.Date)
                {
                    return ValidationResult.Success;
                }
                return new ValidationResult(ErrorMessage ?? "Ngày không được nằm trong quá khứ.");
            }
            return ValidationResult.Success; // Bỏ qua nếu không phải kiểu DateTime hoặc null
        }

        public void AddValidation(ClientModelValidationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            context.Attributes.Add("data-val", "true");
            context.Attributes.Add("data-val-futuredate", ErrorMessage ?? "Ngày không được nằm trong quá khứ.");
        }
    }
}
