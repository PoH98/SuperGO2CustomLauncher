using CefSharp;
using CefSharp.DevTools;
using GO2FlashLauncher.Script.GameLogic.Constellation;
using System;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class ConstellationCreator
    {
        public AbstractConstellation Create(Constellations constellation, DevToolsClient devtools, IBrowserHost host)
        {
            AbstractConstellation c;
            switch (constellation)
            {
                case Constellations.Aries:
                    c = new Aries(devtools, host);
                    break;
                case Constellations.Aquarius:
                    c = new Aquarius(devtools, host);
                    break;
                case Constellations.Cancer:
                    c = new Cancer(devtools, host);
                    break;
                case Constellations.Capricorn:
                    c = new Capricorn(devtools, host);
                    break;
                case Constellations.Leo:
                    c = new Leo(devtools, host);
                    break;
                case Constellations.Libra:
                    c = new Libra(devtools, host);
                    break;
                case Constellations.Pisces:
                    c = new Pisces(devtools, host);
                    break;
                case Constellations.Sagitarius:
                    c = new Sagitarius(devtools, host);
                    break;
                case Constellations.Scorpio:
                    c = new Scorpio(devtools, host);
                    break;
                case Constellations.Taurus:
                    c = new Taurus(devtools, host);
                    break;
                case Constellations.Virgo:
                    c = new Virgo(devtools, host);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return c;
        }
    }
}
