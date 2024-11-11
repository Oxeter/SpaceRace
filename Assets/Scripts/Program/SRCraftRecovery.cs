
using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.State;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Flight.Sim;
using ModApi.Math;
using ModApi.Scripts.State;
using ModApi.State;
using UnityEngine;
using Assets.Scripts.Flight;
using Assets.Scripts.SpaceRace.Hardware;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.SpaceRace.Collections;
using Assets.Scripts.SpaceRace.UI;

namespace Assets.Scripts.SpaceRace.Projects
{
    public class SRCraftRecovery
    {
        private IProgramManager _pm;
        private CraftNode _node;

        private VerboseCraftConfiguration _vconfig;

        public SRCraftConfig Config;
        private float _undevelopedRecoveryFactor;

        public double ClosestDistance { get; } = -1.0;
        public LaunchLocation ClosestLocation { get; }
        public double WaterFactor = 1.0;
        public float MaxRecoverableMass = 300F;
        public bool IsDestroyed= false;

        public string FailMessage = string.Empty;
        public string StageDescription => _pm.HM.Description(Config.Stages);
        public string PartDescription => Config.Parts.Count > 2 ? $"{Config.Parts.TotalOccurences()} developed parts and {Units.GetMoneyString((long)(Config.UndevelopedPartPrice * _undevelopedRecoveryFactor))} of undeveloped parts." : _pm.HM.Description(Config.Parts) + $" {Units.GetMoneyString((long)(Config.UndevelopedPartPrice * _undevelopedRecoveryFactor))} of undeveloped parts.";
        public string FuelDescription => string.Join(", ", _vconfig.Fuels.Where(x => x.Key.Price > 0 && x.Value > 0.0).Select(x => $"{x.Key.Name}: {Units.GetMoneyString((long)(x.Key.Density * x.Key.Price * x.Value))}" ));
        public bool HumansOnBoard => _vconfig.Crew.Count > 0;
        public int NumAstronauts => _vconfig.Crew.Count;

        public bool CanRecover => FailMessage == string.Empty;

        public string Hardware => string.Join("\n", Config.Orders.Select(order => order.Description));

        public bool CanRecoverWhole => ClosestDistance <= ClosestLocation.FreeRecoveryRadius && Config.Id > 0;

        public long CrewRecoveryPrice => (long)(ClosestDistance * WaterFactor * _vconfig.Crew.Count * 100 * 0.02);
        
        public bool RecoverFuel =>  _vconfig.MassWet <= MaxRecoverableMass && ClosestDistance * WaterFactor * 100 * 0.02 * (_vconfig.MassWet - _vconfig.MassDry) <= _vconfig.FuelPrice;
        public long PartRecoveryPrice => (long)(ClosestDistance * WaterFactor * 100 * 0.02 * (RecoverFuel ? _vconfig.MassWet : _vconfig.MassDry ));
        
        public long RepairPrice => (long)(_pm.CostCalculator.GetIntegrationRepair(Config) + _pm.CostCalculator.GetIntegrationRollout(Config));
        public SRCraftRecovery(IProgramManager pm, CraftNode node, ICraftNodeData nodeData)
        {
            _pm = pm;
            _node = node;
            _vconfig = pm.GetVerboseCraftConfig(node.CraftScript.ActiveCommandPod);
            Config = new SRCraftConfig(_vconfig, node.Name);
            foreach ((int,int) x in pm.Data.Flights.Where(x => node.InitialCraftNodeIds.Contains(x.Item1)))
            {
                if (Config.Similarity(pm.HM.Craft[x.Item2]) == 1.0F)
                {
                    Config = pm.HM.Craft[x.Item2];
                }
            }         
            _undevelopedRecoveryFactor = pm.Career.TechTree.GetItemValue("SpaceRace.Recovery")?.ValueAsFloat ?? 1.0F;
            MaxRecoverableMass = _node.WaterDepth > 2.0 ? pm.Career.TechTree.GetItemValue("SpaceRace.Recovery")?.ValueAsFloat ?? 300000f : float.MaxValue;
            IPlanetNode planet = _node.Parent;
            Vector3d vector3d = planet.PlanetVectorToSurfaceVector(node.Position);

            Vector3d vector3d2 = planet.SurfaceVectorToPlanetVector(planet.CalculateSurfaceVelocity(vector3d));
            Vector3d vector3d3 = node.Velocity - vector3d2;
            Debug.Log(node.NodeId.ToString());
            Debug.Log(string.Join(" ", _pm.Integrations.Values.Select(integ => integ.Data.NodeId.ToString())));
            if (node.IsDestroyed)
            {
                IsDestroyed = true;
                FailMessage = "This craft does not have any parts to recover.";
            }
            else if (!node.InContactWithPlanet)
            {
                FailMessage = "The craft cannot be recovered because it is not on the ground.";
            }
            else if (vector3d3.magnitude > 5.0)
            {
                FailMessage = "The craft cannot be recovered because it is moving too fast.";
            }
            else if (_pm.Integrations.Values.Any(integ => integ.Data.NodeId == node.NodeId))
            {
                FailMessage = "The craft cannot be recovered because it is currently being rolled to/from the launchpad.";
            }
            string planetName = planet.Name;
            Vector3d? surfacePosition = nodeData.SurfacePosition;
            planet.GetSurfaceCoordinates(vector3d, out var latitude, out var longitude);
            List<LaunchLocation> list = pm.GameState.LaunchLocations.Where((LaunchLocation x) => x.PlanetName == planetName).ToList();
            List<LaunchLocation> list2 = new List<LaunchLocation>();
            foreach (LaunchLocation item in list)
            {
                if (!Game.Instance.GameState.Validator.IsLaunchLocationLocked(item.Name))
                {
                    list2.Add(item);
                }
            }

            LaunchLocation launchLocation = null;
            double num = double.MaxValue;
            foreach (LaunchLocation item2 in list2)
            {
                double num3 = MathUtils.Haversine(item2.Latitude * 0.01745329, item2.Longitude * 0.01745329, latitude, longitude, planet.PlanetData.Radius);
                num3 = Math.Max(0.0, num3 - item2.FreeRecoveryRadius);
                if (num3 < num)
                {
                    num = num3;
                    launchLocation = item2;
                }
            }

            if (launchLocation == null)
            {
                FailMessage = "There are no recovery locations available on " + planetName + ".";
                return;
            }
            ClosestDistance = num;
            ClosestLocation = launchLocation;
        }
        public void RecoverHumans()
        {
            _pm.Career.SpendMoney(CrewRecoveryPrice);
            foreach (CrewMember crew in _vconfig.Crew)
            {
                crew.State = CrewMemberState.Available;
            }
        }

        public void RecoverParts(int? contractorId = null)
        {
            _pm.Career.SpendMoney(PartRecoveryPrice - (long)(_vconfig.UndevelopedPartPrice * _undevelopedRecoveryFactor)  - (RecoverFuel? _vconfig.FuelPrice : 0));
            foreach (CrewMember crew in _vconfig.Crew)
            {
                crew.State = CrewMemberState.Available;
            }
            foreach (SRStage stage in _vconfig.Stages.Keys)
            {
                if (stage.Id != 0)
                {
                    _pm.Contractor(contractorId ?? stage.ContractorId).Data.RecoveredHardware.AddPair(stage.Id, 1);
                }
            }
            foreach (SRPart part in _vconfig.Parts.Values)
            {
                if (part.Id != 0)
                {
                    _pm.Contractor(contractorId ?? part.ContractorId).Data.RecoveredHardware.AddPair(part.Id, 1);
                }
            }
        }

        public void RecoverWhole()
        {
            Debug.Log($"Repeating integration {Config.Id}");
            foreach (CrewMember crew in _vconfig.Crew)
            {
                crew.State = CrewMemberState.Available;
            }
            _pm.RepeatIntegration(Config, _node.Name, _node, ClosestLocation);
        }

    }
}