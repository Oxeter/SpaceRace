using UI.Xml;
namespace Assets.Scripts.SpaceRace.Program
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Ui;
    using System.Xml.Serialization;
    using System.IO;
    using System;
    using System.Linq;
   
    using System.Xml.Linq;
    using Assets.Scripts.State;
    using Assets.Scripts.Flight;
    using System.Xml;
    using ModApi.Flight.Sim;
    using ModApi.Craft;
    using Assets.Scripts.Menu.ListView;
    using ModApi.Ui;
    using ModApi.State;
    using Unity.Mathematics;
    using Assets.Scripts.Career.Research;
    using Assets.Scripts.Flight.Sim;
    using ModApi.Planet;

    public class SpaceCenterManager
    {
        private IProgramManager _pm;
        public List<SpaceCenterLocation> Locations {get; private set;}

        public List<SpaceCenterStructure> Structures {get; private set;}
        public List<SpaceCenterLocation> UnlockedLocations => Locations.Where(loc=> loc.LaunchPads.Any(pad => Game.Instance.GameState.Career.UnlockedLocations.Contains(pad))).ToList();
        
        public IEnumerable<SpaceCenterStructure> UnlockedStructures => Structures.Where(st=> st.Unlocked);
        public SpaceCenterManager(IProgramManager pm,  List<SpaceCenterLocation> locs, List<SpaceCenterStructure> structs)
        {
            _pm=pm;
            Locations = locs;
            Structures = structs;
        }
        public void ResetUnlockedStatus()
        {
            foreach (SpaceCenterStructure structure in Structures)
            {
                structure.Unlocked = false;
            }
        }
        public void SpawnFromLaunchLocations(List<string> unlockedLocations)
        {
            foreach (SpaceCenterStructure structure in Structures)
            {
                if (unlockedLocations.Any(loc2 => structure.LaunchPads.Contains(loc2)))
                {
                   Unlock(structure);
                }
            }
        }

        public void OnContractCompleted(Career.Contracts.Contract contract)
        {
            foreach (SpaceCenterStructure structure in Structures.Where(loc2 => !loc2.Unlocked))
            {
                if (contract.UnlockLocations.Any(loc3 => structure.LaunchPads.Contains(loc3)))
                {
                   Unlock(structure);
                }
            }
        }

        public void SpawnSpaceCenter(SpaceCenterLocation loc)
        {
            if (!Game.InFlightScene) return;
            string scPath = Path.Combine(CareerState.CheckOverridePath(_pm.Career.Contracts.ResourcesPath, "Crafts/"), ModSettings.Instance.VisibleSpaceCenters ? "Space-Center-Visible.xml" : "Space-Center.xml");
            CraftData scData = Game.Instance.CraftLoader.LoadCraftImmediate(XDocument.Load(scPath).Root);
            CraftNode node = FlightSceneScript.Instance.SpawnCraft("Space Center", scData, loc.Location);
            node.NodeId = - 1;
        }

        private void Unlock(SpaceCenterStructure structure)
        {
            if (structure.Unlocked)
            {
                return;
            }
            structure.Unlocked = true;
            SpawnStructure(structure);
        }
        private string SpawnStructure(SpaceCenterStructure structure)
        {
            if (!Game.InFlightScene) 
            {
                return null;
            }
            IPlanetNode planet = FlightSceneScript.Instance.FlightState.RootNode.FindPlanet(structure.Planet);
            StructureNodeData data = new StructureNodeData(structure.Xml);
            StructureNode node=new StructureNode(data, planet);
            planet.AddChildNode(node);
            if (planet.IsTerrainDataLoaded)
            {
                node.OnTerrainDataLoaded();
            }
            return structure.Id;
        }


        public void OnFlightStart()
        {
            RemovePlanetaryStructures("Earth");
            ResetUnlockedStatus();
            SpawnFromLaunchLocations(_pm.Career.UnlockedLocations);
        }
        private void RemovePlanetaryStructures(string planet)
        {
            IPlanetNode planetNode = FlightSceneScript.Instance.FlightState.RootNode.FindPlanet(planet);
            while (true)
            {
                StructureNode structureNode = planetNode.DynamicNodes.FirstOrDefault(node => node is StructureNode) as StructureNode;
                if (structureNode ==null)
                { 
                    break;
                }
                if (structureNode.IsLoadedInGameView)
                    {
                        structureNode.UnloadFromGameView(false);
                    }
                planetNode.RemoveChildNode(structureNode);
            }
        }
    }

    public class SpaceCenterLocation
    {
        public List<string> LaunchPads = new List<string>();
        public LaunchLocation Location;
        public SpaceCenterLocation(XElement xml)
        {
            LaunchPads = ((string)xml.Attribute("pads")).Split(';').ToList();
            Location = new LaunchLocation(xml);
        }
    }

    public class SpaceCenterStructure
    {
        public string Id;
        [XmlIgnore]
        public bool Unlocked = false;
        public string Planet = "Earth";
        public List<string> LaunchPads = new List<string>();
        public XElement Xml;
    }
}