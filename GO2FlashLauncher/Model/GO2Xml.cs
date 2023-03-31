using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace GO2FlashLauncher.Model
{
    [XmlRoot(ElementName = "resource")]
    public class Resource
    {

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "src")]
        public string Src { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "resources")]
    public class Resources
    {

        [XmlElement(ElementName = "resource")]
        public List<Resource> Resource { get; set; }

        [XmlAttribute(AttributeName = "path")]
        public string Path { get; set; }

        [XmlAttribute(AttributeName = "gMap")]
        public string GMap { get; set; }

        [XmlAttribute(AttributeName = "res")]
        public string Res { get; set; }

        [XmlAttribute(AttributeName = "client")]
        public string Client { get; set; }

        [XmlAttribute(AttributeName = "galaxyAssetPath")]
        public string GalaxyAssetPath { get; set; }
    }

    [XmlRoot(ElementName = "audio")]
    public class Audio
    {

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "src")]
        public string Src { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }
    }

    [XmlRoot(ElementName = "music")]
    public class Music
    {

        [XmlElement(ElementName = "audio")]
        public List<Audio> Audio { get; set; }

        [XmlAttribute(AttributeName = "path")]
        public string Path { get; set; }

        [XmlAttribute(AttributeName = "res")]
        public string Res { get; set; }
    }

    [XmlRoot(ElementName = "Msg")]
    public class Msg
    {

        [XmlAttribute(AttributeName = "name")]
        public string Name { get; set; }

        [XmlAttribute(AttributeName = "src")]
        public string Src { get; set; }
    }

    [XmlRoot(ElementName = "Note")]
    public class Note
    {

        [XmlElement(ElementName = "Msg")]
        public List<Msg> Msg { get; set; }
    }

    [XmlRoot(ElementName = "config")]
    public class GO2Xml
    {

        [XmlElement(ElementName = "resources")]
        public Resources Resources { get; set; }

        [XmlElement(ElementName = "music")]
        public Music Music { get; set; }

        [XmlElement(ElementName = "Note")]
        public Note Note { get; set; }
    }
}
