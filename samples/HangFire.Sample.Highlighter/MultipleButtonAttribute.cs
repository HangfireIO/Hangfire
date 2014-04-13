using System;
using System.Reflection;
using System.Web.Mvc;

namespace HangFire.Sample.Highlighter
{
    /// <summary>
    /// Attribute-based solution to the multiple submit button issue based 
    /// heavily on the post and comments from Maartin Balliauw.
    /// http://stackoverflow.com/a/7111222/1317575
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class MultipleButtonAttribute : ActionNameSelectorAttribute
    {
        public string Name { get; set; }
        public string Argument { get; set; }

        public override bool IsValidName(ControllerContext controllerContext, string actionName, MethodInfo methodInfo)
        {
            var isValidName = false;
            var keyValue = string.Format("{0}:{1}", Name, Argument);
            var value = controllerContext.Controller.ValueProvider.GetValue(keyValue);
            
            if (value != null)
            {
                controllerContext.Controller.ControllerContext.RouteData.Values[Name] = Argument;
                isValidName = true;
            }

            return isValidName;
        }
    }
}