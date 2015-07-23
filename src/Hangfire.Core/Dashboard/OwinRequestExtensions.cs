using System.IO;
using System.Threading.Tasks;
using Microsoft.Owin;

namespace Hangfire.Dashboard
{
    internal static class OwinRequestExtensions
    {
        /// <summary>
        /// Hack to prevent "Unable to cast object of type 'Microsoft.Owin.FormCollection' 
        /// to type 'Microsoft.Owin.IFormCollection'" exception, when internalized version
        /// does not match the current project's one.
        /// </summary>
        public static async Task<IFormCollection> ReadFormSafeAsync(this IOwinContext context)
        {
            // hack to clear a possible cached type from Katana in environment
            context.Environment.Remove("Microsoft.Owin.Form#collection");

            var form = await context.Request.ReadFormAsync();

            // hack to prevent caching of an internalized type from Katana in environment
            context.Environment.Remove("Microsoft.Owin.Form#collection");

            return form;
        } 
    }
}
