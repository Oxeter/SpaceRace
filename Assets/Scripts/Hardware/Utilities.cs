namespace Assets.Scripts.SpaceRace.Hardware
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.IO;
    
    using ModApi.Common.Extensions;

    using ModApi.Craft.Parts;

    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Formulas;
    using UnityEngine;
    using Jundroo.ModTools;
    using Unity.Mathematics;
    using Assets.Scripts.Career.Research;
    using Assets.Scripts.State;
    using HarmonyLib;
    using Assets.Scripts.State.Validation;
    using Microsoft.SqlServer.Server;

    public static class SRUtilities
    {
        public static readonly List<string> Modifiers = new List<string>(){"SRRocketEngineData", "SRReactionEngineData", "SRFuelTankData", "SRCapsulesData", "SRAvionicsData"};
        public static XElement DesignerXml(XElement part)
        {
            XElement dp = new XElement
            (
                "DesignerPart",
                new XAttribute ("name", part.GetStringAttribute("name", "Rocket Engine")),
                new XAttribute ("spacerace", "true"),
                new XAttribute ("category", "Propulsion"),
                //new XAttribute ("description", Description),
                new XAttribute ("order", part.GetIntAttribute("id", 0)+100),
                new XAttribute ("showInDesigner", "true"),
                new XAttribute ("snapshotDistanceScaler", "1"),
                new XAttribute ("snapshotRotation", "-25,100,0"),
                new XElement(
                    "Assembly",
                    new XElement("Parts", part)
                )
            );
            return dp;
        }
        /// <summary>
        /// Removes symmetry and position references from a part's XML so it can be compared to other parts
        /// </summary>
        /// <param name="partXml"></param>
        public static void RemoveSymmetry(XElement partXml)
        {
            Debug.Log("About to remove symmetry");
            if (partXml.Attribute("symmetryId") != null) partXml.Attribute("symmetryId").Remove();
            if (partXml.Attribute("position") != null) partXml.Attribute("position").SetValue("0,0,0");
            if (partXml.Attribute("rotation") != null) partXml.Attribute("rotation").SetValue("0,0,0");
            if (partXml.Element("Config")?.Attribute("symmetryId") != null) partXml.Element("Config").Attribute("symmetryId").Remove();
            if (partXml.Element("RocketEngine")?.Attribute("symmetryId") != null) partXml.Element("RocketEngine").Attribute("symmetryId").Remove();
            if (partXml.Element("SpaceRace.SRRocketEngine")?.Attribute("symmetryId") != null) partXml.Element("SpaceRace.SRRocketEngine").Attribute("symmetryId").Remove();
            if (partXml.Element("InputController")?.Attribute("symmetryId") != null) partXml.Element("InputController").Attribute("symmetryId").Remove();
        }
        /// <summary>
        /// Decides whether two engines are equivalent using their part xml. 
        /// Note that this method requires symmetry to be stripped from the data first
        /// </summary>
        /// <param name="x">the first engine's part xml</param>
        /// <param name="y">the seconf engine's part xml</param>
        /// <returns>whether they are equal</returns>
        public static bool IsSameEngine(XElement x, XElement y)
        {
            return XNode.DeepEquals(x.Element("RocketEngine"), y.Element("RocketEngine"))
            && x.Element("SpaceRace.SRRocketEngine").GetBoolAttribute("airLight")==y.Element("SpaceRace.SRRocketEngine").GetBoolAttribute("airLight");
        }

        public static void CacheDesignerPart(XElement part)
        {
            Game.Instance.CachedDesignerParts.AddDesignerPart(DesignerXml(part));
        }
        public static bool IsSpaceRacePart(XElement xml)
        {
            return xml.Elements().Any(el => el.Name.ToString().StartsWith("SpaceRace."));
        }
        public static bool IsSpaceRacePart(PartData part)
        {
            return part.Modifiers.Any(data => Modifiers.Contains(data.Name));
        }

        public enum Usage
        {
        Unused,
        Part,
        Family,
        Tech
        }
        /// <summary>
        /// A dictionary to look up which attributes in a part's xml are relevant to its family clasififcation
        /// </summary>
        public static Dictionary<string, Dictionary<string,Usage>> XmlProperties = new Dictionary<string, Dictionary<string,Usage>>(){
            {"RocketEngine", new Dictionary<string,Usage>
                {
                    {"engineTypeId", Usage.Tech},
                    {"engineSubTypeId", Usage.Tech}, 
                    {"size", Usage.Family},
                    {"fuelType", Usage.Tech}, 
                    {"nozzleTypeId", Usage.Tech}
                }
            }
        };
        /// <summary>
        /// Produces the xml by which SpaceRace part families are classified.  Tanks are handled separately.
        /// </summary>
        /// <param name="xml">An xml from partdata</param>
        /// <returns>A stripped down xml with only relevant properties</returns>
        public static XElement HardwareXml(XElement xml, Usage usage)
        {
            if (!IsSpaceRacePart(xml)) return null;
            string srmodifier = xml.Elements().Select(el => el.Name.ToString()).First(name => name.StartsWith("SpaceRace."));
            switch (srmodifier)
            { 
                case "SpaceRace.SRFuelTank" : return null;
                case "SpaceRace.SRRocketEngine" : 
                {   
                    XElement famxml = new XElement("Part");
                    foreach (XElement modifier in xml.Elements().Where(elem => XmlProperties.Keys.Contains(elem.Name.LocalName)))
                    {
                        string name = modifier.Name.LocalName;
                        Debug.Log("found modifier "+name);
                        XElement newmodifier = new XElement(name);
                        foreach (string attr in XmlProperties[name].Keys)
                        {
                            if (XmlProperties[name][attr] >= usage)
                            {
                                List<string> value = new List<string>(){modifier.GetStringAttributeOrNullIfEmpty(attr)};
                                // is the list really necessary?  SetAttribute does not want to find the method in the base class
                                newmodifier.SetAttribute(attr, value);
                                Debug.Log(string.Format("Copied {0} in {1}", attr, name));
                            }
                        }
                        famxml.Add(modifier);
                    }
                    return famxml;
                }
                default :
                {
                    Debug.Log("Unrecognized SpaceRace modifier "+srmodifier);
                    return null;
                } 
            }
        }
        /// <summary>
        /// Merges an additional tech tree (containing only items and copies of the same nodes) into the base tech tree
        /// </summary>
        /// <param name="original">Xelement from the original tech tree</param>
        /// <param name="additions">Xeleemnt of the techs you want to merge</param>
        public static void TechTreeMerge(XElement original, XElement additions)
        {
            if (additions.Elements("Items").Any())
            {
                foreach (XElement item in additions.Element("Items").Elements("Item"))
                {
                    original.Element("Items").Add(item);
                }
            }

            if (additions.Elements("Nodes").Any())
            {
                foreach (XElement node in additions.Element("Nodes").Elements("Node"))
                {
                    XElement targetnode = original.Element("Nodes").Elements("Node").First(elem => elem.GetStringAttribute("id") == node.GetStringAttribute("id"));
                    foreach (XElement item in node.Elements("Item"))
                    {
                        targetnode.Add(item);
                    }
                }
            }
        }
        /// <summary>
        /// Not functional.  The Traverse fails to inject the tech tree.
        /// </summary>
        /// <param name="filename"></param>
        public static void InjectTechTreeAdditions(string filename)
        {
            ///
            /// traverse not working.  Instead: write a parallel validator??
            /// 
            /// long term: don't load the gamestate without the mod activated. this should solve everything
            ///
            CareerState career = Game.Instance.GameState.Career;
            string resourcesAbsolutePath = career.ResourcesAbsolutePath;
            Debug.Log(resourcesAbsolutePath+ "/SRTech.xml");
            if (!File.Exists(resourcesAbsolutePath + "/" + filename))
            {
                Debug.Log("SRTech.xml not found in this career folder");
                return;
            }

            string path2 = resourcesAbsolutePath + "/TechTree.xml";
            string path3 = resourcesAbsolutePath + "/" + filename;
            XElement ttxml = XElement.Parse(File.ReadAllText(path2));
            XElement srxml = XElement.Parse(File.ReadAllText(path3));
            TechTreeMerge(ttxml, srxml);
            TechTree newtree = new TechTree(ttxml, Game.Instance.CachedDesignerParts, false);
            Debug.Log(newtree.GetNode("rockets").Items.Count.ToString());

            string id = Game.Instance.GameState.Id;
            string path = Game.PersistentDataPath + 
                    "/UserData/GameStates/" + id + 
                    "/Active/GameState.xml";
            Debug.Log(path);
            if (File.Exists(path))
            {
                XElement careerxml = XElement.Parse(File.ReadAllText(path));
                newtree.LoadStatusFromXml(careerxml?.Element("TechTree"));
            }
            Debug.Log(career.TechTree.GetNode("rockets").Items.Count.ToString());
            Traverse.Create(Game.Instance.GameState.Career).Field("TechTree").SetValue(newtree);
            Debug.Log(career.TechTree.GetNode("rockets").Items.Count.ToString());
            CareerValidator validator = new CareerValidator(career);
            Traverse.Create(Game.Instance.GameState).Field("Validator").SetValue(validator);
            
        }
    }
}