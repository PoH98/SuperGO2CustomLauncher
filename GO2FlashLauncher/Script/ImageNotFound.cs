using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GO2FlashLauncher.Script
{
    public class ImageNotFound:Exception
    {
        public ImageNotFound(string file):base("Image not found! File: " + file)
        {

        }
    }
}
