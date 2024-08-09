using CefSharp;
using System;
using System.IO;
using System.Linq;

namespace Youtube_Live_Chat_Reformat
{
    internal class ResourceHandlerService : ISchemeHandlerFactory
    {
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            string fullPath = Path.GetFullPath(Path.Combine("Assets", string.Join("\\", new Uri(request.Url).Segments.Where(x => x != "/"))));
            try
            {
                return ResourceHandler.FromFilePath(fullPath);
            }
            catch
            {
                return ResourceHandler.FromByteArray(new byte[] { 0, 0 });
            }
        }
    }
}
