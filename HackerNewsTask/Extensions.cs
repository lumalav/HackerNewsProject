using System;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace HackerNewsTask
{
    public static class Extensions
    {
        /// <summary>
        /// Generic parser
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="request"></param>
        /// <param name="paramName"></param>
        /// <returns></returns>
        public static T Parse<T>(this HttpRequest request, string paramName)
        {
            try
            {
                var converter = TypeDescriptor.GetConverter(typeof(T));
                return (T)converter.ConvertFromString(request.Query[paramName]);
            }
            catch (NotSupportedException)
            {
                throw new Exception($"{paramName} is not a valid {typeof(T)}");
            }
        }
    }
}
