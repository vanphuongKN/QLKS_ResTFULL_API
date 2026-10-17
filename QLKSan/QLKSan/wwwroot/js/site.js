// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Custom validation for FutureDateAttribute
if ($.validator && $.validator.unobtrusive) {
    $.validator.unobtrusive.adapters.addBool('futuredate');
    $.validator.addMethod('futuredate', function (value, element, params) {
        if (!value) return true;
        
        var inputDate = new Date(value);
        var today = new Date();
        today.setHours(0, 0, 0, 0); // Ignore time part
        
        return inputDate >= today;
    });

    // Custom validation for DateGreaterThanAttribute
    $.validator.unobtrusive.adapters.add('dategreaterthan', ['property'], function (options) {
        options.rules['dategreaterthan'] = options.params;
        options.messages['dategreaterthan'] = options.message;
    });

    $.validator.addMethod('dategreaterthan', function (value, element, params) {
        if (!value) return true;

        // Find the element to compare with (assuming it has id matching the property name)
        var propertyName = params.property;
        
        // ASP.NET Core MVC usually prefixes inputs with parent property names, but typically id is generated as #PropertyName
        // This is a simple fallback that checks by id or name
        var compareElement = $('#' + propertyName);
        if (compareElement.length === 0) {
            compareElement = $('[name$="' + propertyName + '"]');
        }

        if (compareElement.length > 0 && compareElement.val()) {
            var inputDate = new Date(value);
            var compareDate = new Date(compareElement.val());
            return inputDate > compareDate;
        }

        return true;
    });
}
