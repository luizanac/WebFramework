using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Luizanac.Infra.Http.Abstractions;

namespace Luizanac.Infra.Http
{
    public abstract class ControllerBase
    {
        protected HttpListenerContext HttpContext { get; private set; }

        public ControllerBase()
        {
            
        }

        /// <summary>
        ///  Method to get a html file relative to the controller and action names.
        /// </summary>
        /// <param name="actionName">Param to get the action name that called this method</param>
        /// <param name="fileRelativePath">If especified, will override method default behavior. Can pass the file extension or not.</param>
        /// <param name="fileExtension">The file extension of the file that will be returned. default is html</param>
        /// <returns>Returns the html in string format</returns>
        protected async Task<string> View([CallerMemberName] string actionName = null, string fileRelativePath = null)
        {
            var controllerName = GetType().Name.Replace("Controller", "");

            string filePath = string.Empty;
            var baseDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, DefaultDirectories.Views);
            if (fileRelativePath != null && Uri.IsWellFormedUriString(fileRelativePath, UriKind.Relative))
            {
                var extension = Path.GetExtension(fileRelativePath);
                if (!string.IsNullOrEmpty(extension))
                    extension = extension.Equals($".{FileExtensions.Html}") ? extension : throw new ArgumentException("Only html files can be used");

                filePath = Path.Combine(baseDirectory, $"{fileRelativePath}.{(string.IsNullOrEmpty(extension) ? FileExtensions.Html : "")}");
            }
            else if (fileRelativePath == null)
                filePath = Path.Combine(baseDirectory, controllerName, $"{actionName}.{FileExtensions.Html}");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Doesn't exist a html file in path {filePath}");

            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        /// <summary>
        /// Convert view marked props with model props
        /// </summary>
        /// <param name="model">Model that will be used to process the view</param>
        /// <param name="actionName">Param to get the action name that called this method</param>
        /// <param name="fileRelativePath">If especified, will override method default behavior. Can pass the file extension or not.</param>
        /// <returns>Returns the html in string format</returns>
        protected async Task<string> View(object model, [CallerMemberName] string actionName = null, string fileRelativePath = null)
        {
            var content = await View(actionName, fileRelativePath);
            var modelProperties = model.GetType().GetProperties();

            var regex = new Regex("\\{(.*?)\\}");
            var matches = regex.Matches(content);
            content = regex.Replace(content, (match) =>
            {
                var prop = match.Groups[1].Value;
                var modelProp = modelProperties.Single(x => x.Name.Equals(prop, StringComparison.InvariantCultureIgnoreCase));
                return modelProp.GetValue(model)?.ToString();
            });
            return content;
        }

    }
}