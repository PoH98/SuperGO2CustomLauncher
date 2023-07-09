using CefSharp;
using GalaxyOrbit.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GalaxyOrbit4Launcher.Service
{
    internal class ResourceHandlerService : ISchemeHandlerFactory
    {
        private readonly HttpClient hc;
        private readonly EmbededResourceHandler rh;
        private readonly string HostName;
        public ResourceHandlerService(string hostName)
        {
            hc = new HttpClient();
            rh = new EmbededResourceHandler();
            HostName = hostName;
        }
        public IResourceHandler Create(IBrowser browser, IFrame frame, string schemeName, IRequest request)
        {
            var localResource = rh.GetResources(request.Url);
            if(localResource == null)
            {
                var result = hc.GetAsync(request.Url).Result;
                if (!result.IsSuccessStatusCode)
                {
                    return ResourceHandler.ForErrorMessage(result.ReasonPhrase, result.StatusCode);
                }
                localResource = result.Content.ReadAsStreamAsync().Result;
            }
            return ResourceHandler.FromStream(localResource);
        }
    }
}
