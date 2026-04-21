using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Reflection;

namespace SchoolManager.Attributes
{
    /// <summary>
    /// Atributo que convierte automáticamente los DateTime a UTC en los parámetros de los controladores.
    /// Afecta tanto a parámetros simples (DateTime/DateTime?) como a propiedades de tipos complejos.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DateTimeConversionAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.ActionDescriptor is not ControllerActionDescriptor descriptor)
            {
                base.OnActionExecuting(context);
                return;
            }

            var parameters = descriptor.MethodInfo.GetParameters();
            var arguments = context.ActionArguments;

            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (!arguments.TryGetValue(param.Name!, out var value) || value == null)
                    continue;

                var paramType = param.ParameterType;

                if (paramType == typeof(DateTime))
                {
                    var dateTime = (DateTime)value;
                    if (dateTime.Kind != DateTimeKind.Utc)
                    {
                        var utc = ToUtc(dateTime);
                        arguments[param.Name!] = utc;
                    }
                }
                else if (paramType == typeof(DateTime?))
                {
                    var nullableDateTime = (DateTime?)value;
                    if (nullableDateTime.HasValue && nullableDateTime.Value.Kind != DateTimeKind.Utc)
                    {
                        var utc = ToUtc(nullableDateTime.Value);
                        arguments[param.Name!] = utc;
                    }
                }
                else if (paramType.IsClass && paramType != typeof(string))
                {
                    ConvertDateTimesToUtc(value);
                }
            }

            base.OnActionExecuting(context);
        }

        private static DateTime ToUtc(DateTime dateTime)
        {
            return dateTime.Kind == DateTimeKind.Local
                ? dateTime.ToUniversalTime()
                : DateTime.SpecifyKind(dateTime, DateTimeKind.Local).ToUniversalTime();
        }

        /// <summary>
        /// Convierte recursivamente todos los DateTime a UTC en las propiedades de un objeto.
        /// </summary>
        private static void ConvertDateTimesToUtc(object obj)
        {
            if (obj == null) return;

            var type = obj.GetType();
            if (type == typeof(string)) return;

            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return; // Ya manejados como parámetros simples

            if (type.IsClass && !type.IsPrimitive)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var property in properties)
                {
                    if (!property.CanRead || !property.CanWrite) continue;

                    if (property.PropertyType == typeof(DateTime))
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            var dateTime = (DateTime)value;
                            if (dateTime.Kind != DateTimeKind.Utc)
                                property.SetValue(obj, ToUtc(dateTime));
                        }
                    }
                    else if (property.PropertyType == typeof(DateTime?))
                    {
                        var value = property.GetValue(obj);
                        if (value != null)
                        {
                            var nullableDateTime = (DateTime?)value;
                            if (nullableDateTime.HasValue && nullableDateTime.Value.Kind != DateTimeKind.Utc)
                                property.SetValue(obj, ToUtc(nullableDateTime.Value));
                        }
                    }
                }
            }
        }
    }
}
