using CefSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Youtube_Live_Chat_Reformat
{
    internal class ResourceHandlerService : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            var fullPath = Path.GetFullPath(Path.Combine("Assets", string.Join("\\",new Uri(request.Url).Segments.Where(x => x != "/"))));
            return ResourceHandler.FromFilePath(fullPath);
        }
    }
}
