using System;

namespace GO2FlashLauncher.Script
{
    public class ImageNotFound : Exception
    {
        public ImageNotFound(string file) : base("Image not found! File: " + file)
        {

        }
    }
}
