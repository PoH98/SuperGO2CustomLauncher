using System.IO;
using System.Linq;
using System.Reflection;

namespace GalaxyOrbit.Resources
{
    public class EmbededResourceHandler
    {
        readonly Assembly Ass;
        public EmbededResourceHandler()
        {
            Ass = Assembly.GetExecutingAssembly();
        }

        public Stream GetResources(string url)
        {
            var fileName = url.Split('/').Last();
            var info = Ass.GetManifestResourceInfo(fileName);
            if(info != null)
            {
                return Ass.GetManifestResourceStream(fileName);
            }
            return null;
        }
    }
}
