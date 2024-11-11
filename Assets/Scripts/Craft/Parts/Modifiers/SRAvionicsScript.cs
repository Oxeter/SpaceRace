namespace Assets.Scripts.SpaceRace.Modifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using ModApi.Flight;
    using ModApi.Craft.Parts;
    using ModApi.GameLoop.Interfaces;
    using ModApi.GameLoop;
    using Assets.Scripts.Flight;
    using UnityEngine;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Craft;
    using ModApi.Math;
    using ModApi.Ui.Inspector;
    using ModApi.State;
    using Assets.Scripts.Flight.UI;
    using ModApi.Flight.Sim;
    using ModApi.Ui;
    using Assets.Scripts.SpaceRace.UI;
    using ModApi.Flight.Events;
    using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.SpaceRace.Formulas;

    public class SRAvionicsScript : PartModifierScript<SRAvionicsData>, IFlightUpdate, IFlightFixedUpdate, IAnalyzePerformance
    {
        public bool UsesMachNumber => false;
        private double _distance = 0.0;
        private bool _above = true;
        private bool _hasRCS = false;
        private ICraftNode _thisCraft;
        private LaunchLocation _launchLocation;
        private IPlanetNode _launchPlanet;
        private Vector3d _launchSurfacePosition;
        private bool _globalRadio;
        private List<SRAvionicsData> _avionics;
        private float _craftWideMaxThrust = 0f;
        private double _craftWideMaxComRange = 0f;
        public TextModel PowerConsumptionModel {get; private set;}
        public TextModel ComRangeModel {get; private set;}
        public TextModel MaxThrustModel {get; private set;}
        public TextModel ShutDownModel {get; private set;}
        public TextModel RadioDistanceModel {get; private set;}
        public TextModel RadioAboveModel {get; private set;}
        public TextModel FlightTimeModel {get; private set;}
        public CommandPodScript CommandModifier {get; private set;}

        public CommandPodScript OriginalCommand {get; set;}
        void Start()
        {
            if (Game.InFlightScene) 
            {
                _thisCraft = PartScript.CraftScript.CraftNode;
                Data.LaunchLocationName ??= PartScript.CraftScript.CraftNode.InitialCraftNodeData.First().LaunchLocationName;
                _launchLocation ??= Game.Instance.GameState.LaunchLocations.First(loc => loc.Name == Data.LaunchLocationName);
                _launchPlanet = FlightSceneScript.Instance.FlightState.RootNode.FindPlanet(_launchLocation.PlanetName);
                _launchSurfacePosition = _launchPlanet.GetSurfacePosition(_launchLocation.Latitude * 0.01745329238474369, _launchLocation.Longitude * 0.01745329238474369, AltitudeType.AboveSeaLevel, 0.0);
                _globalRadio = Game.IsCareer? Game.Instance.GameState.Career.TechTree.GetItemValue("SpaceRace.GlobalRadio").ValueAsBool : true;
                PartScript.CraftScript.ActiveCommandPodChanged += CheckAvionics;
                CommandModifier = Data.Part.GetModifier<CommandPodData>().Script;
                if (Data.OriginalCommandId >= 0) 
                {
                    OriginalCommand = PartScript.CraftScript.Data.Assembly.Parts.FirstOrDefault(p => p.Id == Data.OriginalCommandId)?.GetModifier<CommandPodData>().Script;
                }
                CheckAvionics(PartScript.CraftScript);
                CheckRCS(PartScript.CraftScript);
                FlightSceneScript.Instance.TimeManager.TimeMultiplierModeChanged += OnTimeMultiplierModeChanged;
                FlightSceneScript.Instance.FlightEnded += OnFlightEnded;
            }
        }

        public override void OnCraftStructureChanged(ICraftScript craftScript)
        {
            base.OnCraftStructureChanged(craftScript);
            _thisCraft = PartScript.CraftScript.CraftNode;
            CheckAvionics(craftScript);
            CheckRCS(craftScript);
        }
        private void CheckRCS(ICraftScript craftScript)
        {
            _hasRCS = craftScript.Data.Assembly.Parts.Count(p => p.GetModifier<SRRCSData>() !=null) >= 4;
        }

        public void OnFlightEnded(object sender, FlightEndedEventArgs e)
        {
            FlightSceneScript.Instance.TimeManager.TimeMultiplierModeChanged -= OnTimeMultiplierModeChanged;
        }

        private void OnTimeMultiplierModeChanged(TimeMultiplierModeChangedEvent e)
        {
            if (!e.EnteredWarpMode || !_thisCraft.IsPlayer)
            {
                return;
            }
            if (!_hasRCS)
            {
                FlightSceneScript.Instance.FlightSceneUI.ShowMessage("Craft has fewer than 4 RCS thrusters. Cannot hold heading during warp.");
                FlightSceneScript.Instance.FlightControls.NavSphere.UnlockHeading();
                return;
            }
            Vector3 forward = PartScript.CraftScript.CenterOfMass.forward;
            Vector3 toDirection = FlightSceneScript.Instance.ViewManager.GameView.ReferenceFrame.PlanetToFrameVector(CommandModifier.Controls.TargetDirection ?? new Vector3(0,0,0));
            if (Quaternion.Angle(Quaternion.FromToRotation(forward, toDirection), Quaternion.identity) > 5)
            {
                FlightSceneScript.Instance.FlightSceneUI.ShowMessage("Craft is too far from target heading. Cannot hold heading during warp.");
                FlightSceneScript.Instance.FlightControls.NavSphere.UnlockHeading();
                return;
            }
        }
      
        private void CheckAvionics()
        {
            CheckAvionics(PartScript.CraftScript);
        }
        private void CheckAvionics(ICraftScript craft, ICommandPod commandPod, ICommandPod oldcommandPod)
        {
            CheckAvionics(craft);
        }

        private void CheckAvionics(ICraftScript craft)
        {
            _avionics = PartScript.CraftScript.Data.Assembly.Parts.Select(p => p.GetModifier<SRAvionicsData>()).Where(x => x != null).ToList();
            _craftWideMaxThrust = _avionics.Max(x => x.MaxThrust);
            MaxThrustModel?.Update();
            _craftWideMaxComRange = _avionics.Max(x => x.MaxComDistance);
            ComRangeModel?.Update();
            if (Game.InFlightScene && craft.CraftNode.IsPlayer)
            {
                if (PartScript.CraftScript.FlightData.MaxActiveEngineThrust <= Data.MaxThrust)
                {
                    if (Data.TooMuchThrust)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"Craft thrust is now {Units.GetForceString(PartScript.CraftScript.FlightData.MaxActiveEngineThrust)}. {Data.Part.Name} avionics activated.");
                    }
                    Data.TooMuchThrust = false;
                }
                else 
                {
                    if (!Data.TooMuchThrust)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} system cannot handle {Units.GetForceString(PartScript.CraftScript.FlightData.MaxActiveEngineThrust)} of thrust. Avionics shutting down.");
                        if (PartScript.Data.CommandPodId == PartScript.Data.Id && _craftWideMaxThrust >= PartScript.CraftScript.FlightData.MaxActiveEngineThrust)
                        {
                            CommandModifier.ReplicateActivationGroups = ActivationGroupReplicationMode.All;
                            CommandModifier.ReplicateControls = true;
                            CommandModifier.ReplicateStageActivations = true;
                            SRAvionicsData capable = _avionics.FirstOrDefault(x => x.MaxThrust >= PartScript.CraftScript.FlightData.MaxActiveEngineThrust);
                            capable.OriginalCommandId = Data.Part.Id;
                            capable.Script.OriginalCommand = CommandModifier;
                            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} system cannot handle {Units.GetForceString(PartScript.CraftScript.FlightData.MaxActiveEngineThrust)} of thrust. Switching to {capable.Part.Name}");
                            FlightSceneScript.Instance.ChangePlayersActiveCommandPodImmediate(capable.Script.CommandModifier, base.PartScript.CraftScript.CraftNode);
                        }
                        else
                        {
                            Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} system cannot handle {Units.GetForceString(PartScript.CraftScript.FlightData.MaxActiveEngineThrust)} of thrust. Avionics shutting down.");
                        }

                    }
                    Data.TooMuchThrust = true;
                }
                if (Data.OriginalCommandId >= 0 )
                {
                    if (OriginalCommand?.PartScript.CraftScript != PartScript.CraftScript)
                    {
                        Debug.Log($"Switching to command pod {OriginalCommand.Part.Name}");
                        FlightSceneScript.Instance.ChangePlayersActiveCommandPodImmediate(OriginalCommand, OriginalCommand.PartScript.CraftScript.CraftNode);
                        Data.OriginalCommandId = 0;
                        OriginalCommand = null;
                    }
                }
            }
        }

        void IFlightFixedUpdate.FlightFixedUpdate(in FlightFrameData frame)
        {
            if (Data.FlightTime <= SRFormulas.MechanicalGuidanceTime && !_thisCraft.InContactWithPlanet)
            {
                Data.FlightTime += frame.DeltaTime;
                FlightTimeModel?.Update();
            }
            if (CommandModifier.IsPlayerControlled)
            {
                if (PartScript.CraftScript.NumAstronauts == 0 && PeriodicRadioDistance() > Data.MaxComDistance)
                {
                    OnOutOfRange();
                }
                else if (Data.Guidance == "Mechanical")
                {
                    if (Data.InCommunication && Data.FlightTime > SRFormulas.MechanicalGuidanceTime)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} has exceeded its {Units.GetRelativeTimeString(SRFormulas.MechanicalGuidanceTime)} operating limit.");
                    }
                    Data.InCommunication = Data.FlightTime <= SRFormulas.MechanicalGuidanceTime;
                }
                else
                {
                    bool incoms = Data.InCommunication;
                    _distance = RadioDistance();
                    _above = RadioLineOfSight();
                    RadioDistanceModel?.Update();
                    RadioAboveModel?.Update();
                    Data.InCommunication = _distance <= Data.MaxComDistance && _above;
                    if (incoms && !Data.InCommunication)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} guidance system has lost radio contact.");
                    }
                    else if (!incoms && Data.InCommunication)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage($"{Data.Part.Name} guidance system has regained radio contact.");
                    }
                }
                if (Data.TooMuchThrust || (PartScript.CraftScript.NumAstronauts == 0 && !Data.InCommunication && Data.Guidance != "Electronic")) 
                {
                    CraftControls.ZeroControls(CommandModifier.Controls, false);
                }
                //if (!Data.InCommunication)
                //{
                    // This is used to allow contracts to indirectly require the craft be in communication range
                    // Can technically be bypassed with crew, if players want to send crew on the first mars flyby...
                  //  _thisCraft.AllowPlayerControl = false;
                //}
            }
        }
        void IFlightUpdate.FlightUpdate(in FlightFrameData frame)
        {
            if (CommandModifier.BatteryFuelSource.TotalFuel >0)
            {
                CommandModifier.BatteryFuelSource.RemoveFuel(ActualPowerConsumption() * (float)frame.DeltaTimeWorld);
            }
            PowerConsumptionModel?.Update();
        }
        /// <summary>
        /// Sets active avionics to use 10% power when autopilot has no heading.
        /// </summary>
        /// <returns>The actual power consumption rate</returns>
        private float ActualPowerConsumption()
        {
            return Data.PowerConsumption * (CommandModifier.Controls.TargetHeading.HasValue && Data.Guidance != "Unguided" ? 1f : 0.1f);
        } 

        public override void OnGenerateInspectorModel(PartInspectorModel model)
        {
            GroupModel groupModel = new GroupModel("Avionics");
            model.AddGroup(groupModel);
            CreateInspectorModel(groupModel, flight: true);
        }
        public void OnGeneratePerformanceAnalysisModel(GroupModel groupModel)
        {
            CreateInspectorModel(groupModel, false);
        }
        private void CreateInspectorModel(GroupModel model, bool flight)
        {
            if (flight)
            {
                PowerConsumptionModel = model.Add(new TextModel("Power Consumption", ()=> Units.GetPowerString(ActualPowerConsumption() * 1000f)));
                model.Add(new TextModel("Max Thrust(part)", ()=> Units.GetForceString(Data.MaxThrust)));
                MaxThrustModel = model.Add(new TextModel("Max Thrust(craft)", ()=> Units.GetForceString(_craftWideMaxThrust)));
                ShutDownModel = model.Add(new TextModel("Shutdown", ()=> Data.TooMuchThrust.ToString()));   
                ComRangeModel = model.Add(new TextModel("Com Range", ()=> Units.GetDistanceString((float)_craftWideMaxComRange)));
                if (Data.Guidance == "Mechanical")
                {
                    FlightTimeModel = model.Add(new TextModel("Flight Time", ()=> Units.GetRelativeTimeString(Data.FlightTime)));
                }
                if (Data.Guidance == "Radio")
                {
                    RadioDistanceModel = model.Add(new TextModel("Radio Distance", ()=> Units.GetDistanceString((float)_distance)));
                    RadioAboveModel = model.Add(new TextModel("Above Horizon", ()=> _above.ToString()));
                }
            }
            else
            {
                PowerConsumptionModel = model.Add(new TextModel("Avionics Power", ()=> Units.GetPowerString(Data.PowerConsumption* 1000f)));
                FlightTimeModel = model.Add(new TextModel("Control Time", ()=> Data.MaxFlightTime == float.MaxValue ? "Unlimited" : Units.GetRelativeTimeString(Data.MaxFlightTime)));
            }
        }
        private double RadioDistance()
        {
            return _thisCraft.Parent == _launchPlanet ? (_globalRadio ? _thisCraft.Altitude : (_launchPlanet.SurfaceVectorToPlanetVector(_launchSurfacePosition) - _thisCraft.Position).magnitude): (_launchPlanet.SolarPosition - _thisCraft.SolarPosition).magnitude;
        }
        private double PeriodicRadioDistance()
        {
            return _thisCraft.Parent == _launchPlanet ? _thisCraft.Periapsis.Position.magnitude - _launchPlanet.PlanetData.Radius : RadioDistance();
        }
        private bool RadioLineOfSight()
        { 
            if (_thisCraft.Parent == _launchPlanet)
            {
                if (_globalRadio) 
                {
                    return true;
                }
                else
                {
                    Vector3d launch = _launchPlanet.SurfaceVectorToPlanetVector(0.99999*_launchSurfacePosition);
                    //Debug.Log(launch.ToString() + (_thisCraft.Position - launch).ToString());
                    return Vector3d.Dot(launch, _thisCraft.Position - launch) > 0.0;
                }
            }
            if (Vector3d.Cross(Vector3d.Normalize(_launchPlanet.SolarPosition -_thisCraft.SolarPosition), _thisCraft.Position).magnitude < _thisCraft.Parent.PlanetData.Radius)
            {
                return false;
            }
            if (_globalRadio)
            {
                return true;
            }
            Vector3d launch2 = _launchPlanet.SurfaceVectorToPlanetVector(0.99999*_launchSurfacePosition);
            return Vector3d.Dot(launch2, _thisCraft.SolarPosition - _launchPlanet.SolarPosition) > 0;
        }
        private void OnOutOfRange()
        {
            Game.Instance.FlightScene.TimeManager.RequestPauseChange(true, false);
            MessageDialogScript dialog = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel, Game.Instance.UserInterface.Transform, false);
            dialog.MessageText = "The current command pod is out of communications range and is not on a trajectory to return anytime soon.";
            dialog.CancelButtonText = "Try another pod";
            dialog.OkayButtonText = "End Flight";
            dialog.CancelClicked += RangeCancelClicked;
            dialog.OkayClicked += RangeOkayClicked;
        }

        private void RangeCancelClicked(MessageDialogScript script)
        {
            script.Close();
        }

        private void RangeOkayClicked(MessageDialogScript script)
        {
            script.Close();
            GameStateType type = Game.Instance.GameState.Type;
            if (type == GameStateType.Default)
            {
                SREndFlightDialogScript.Create(Game.Instance.FlightScene.FlightSceneUI.Transform);   
            }
            else if (type == GameStateType.Simulation)
            {
                SRRetryFlightDialogScript.Create(Game.Instance.FlightScene.FlightSceneUI.Transform);  
            }
            else
            {  
                RetryFlightDialogScript.Create(Game.Instance.FlightScene.FlightSceneUI.Transform);    
            }
        }



    }
}