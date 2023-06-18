using CefSharp;
using CefSharp.DevTools.Page;
using GO2FlashLauncher.Script.GameLogic.Constellation;
using System;

namespace GO2FlashLauncher.Script.GameLogic
{
    internal class ConstellationCreator
    {
        public AbstractConstellation Create(Constellations constellation, PageClient pageClient, IBrowserHost host)
        {
            AbstractConstellation c;
            switch (constellation)
            {
                case Constellations.Aries:
                    c = new Aries(pageClient, host);
                    break;
                case Constellations.Aquarius:
                    c = new Aquarius(pageClient, host);
                    break;
                case Constellations.Cancer:
                    c = new Cancer(pageClient, host);
                    break;
                case Constellations.Capricorn:
                    c = new Capricorn(pageClient, host);
                    break;
                case Constellations.Leo:
                    c = new Leo(pageClient, host);
                    break;
                case Constellations.Libra:
                    c = new Libra(pageClient, host);
                    break;
                case Constellations.Pisces:
                    c = new Pisces(pageClient, host);
                    break;
                case Constellations.Sagitarius:
                    c = new Sagitarius(pageClient, host);
                    break;
                case Constellations.Scorpio:
                    c = new Scorpio(pageClient, host);
                    break;
                case Constellations.Taurus:
                    c = new Taurus(pageClient, host);
                    break;
                case Constellations.Virgo:
                    c = new Virgo(pageClient, host);
                    break;
                default:
                    throw new NotImplementedException();
            }
            return c;
        }
    }
}
