namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.SpaceRace.Collections;
    using UnityEngine;
    using ModApi.Ui.Inspector;
    using ModApi.Math;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.Ui.Inspector;
    using ModApi.Flight.Sim;
    using System.Linq;
    using ModApi.Flight.UI;
    using ModApi.Design;
    using ModApi.Flight;
    //using System.Runtime.InteropServices;
    using ModApi.State;
    using System.Xml.Linq;
    using Assets.Scripts.Flight;
    using Assets.Scripts.Craft;
    using ModApi.Craft;
    using ModApi.Craft.Parts;
    using Assets.Scripts.Craft.Parts.Modifiers;
    using ModApi.Scenes.Parameters;
    using Assets.Scripts.Flight.Sim;
    using Assets.Scripts.Craft.Fuel;


    public interface IIntegrationScript : IProjectScript<IntegrationData>
    {
        public bool Launching {get;}
    }

    public interface ILaunchable
    {
        public int Id {get;}
        public string Name {get;}
        public SRCraftConfig Config {get;}
        public IntegrationStatus Status {get;}
        public double TimeToCompletion {get;}
    }

    public class IntegrationScript : ProjectScript<IntegrationData>, IIntegrationScript, ILaunchable
    {
        public bool Launching {get; private set;}
        public bool HighlightLauchButton => ShouldHightlightLaunchButton.Contains(_data.Status);
        private SliderModel technicianSlider;
        private TextModel _statusModel;
        private TextModel _launchPadModel;
        public SRCraftConfig Config => _hm.GetCraftConfig(conf => conf.Id == _data.ConfigId);
        public IntegrationStatus Status => _data.Status;
        public string Tooltip => $"{Name} {Status}";
        public HardwareOrder LongestOrder;
        private static List<IntegrationStatus> ShouldHightlightLaunchButton = new List<IntegrationStatus>()
        {
            IntegrationStatus.Ready,
            IntegrationStatus.Stacked
        };
        private static List<IntegrationStatus> CanCancelStatus = new List<IntegrationStatus>()
        {
            IntegrationStatus.Waiting,
            IntegrationStatus.Stacking,
            IntegrationStatus.Stacked
        };
        private static List<IntegrationStatus> CanRolloutStatus = new List<IntegrationStatus>()
        {
            IntegrationStatus.Stacked
        };
        private static List<IntegrationStatus> CanRollbackStatus = new List<IntegrationStatus>()
        {
            IntegrationStatus.Ready,
            IntegrationStatus.Rollout,
        };

        private static List<IntegrationStatus> CanSetTechniciansStatus = new List<IntegrationStatus>()
        {
            IntegrationStatus.Rollout,
            IntegrationStatus.Stacking,
            IntegrationStatus.Rollback,
            IntegrationStatus.Recovery,
            IntegrationStatus.Repairs
        };
        public override ProjectCategory Category {get {return ProjectCategory.Integration;}}
        public override double TimeToCompletion => LongestOrder?.TimeToCompletion ?? _data.TimeToCompletion;
        public override void Initialize()
        {
            base.Initialize();
            Launching = false;
            PlaceOrders();
            SetRush(_data.Rush); //this sets max technicians and progress per day correctly
            UpdateRates(true);
        }
        private void AssignLongestOrder()
        {
            LongestOrder = null;
            foreach (HardwareOrder order in _data.Orders)
            {
                if (LongestOrder == null || LongestOrder.TimeToCompletion <= order.TimeToCompletion)
                {
                    LongestOrder = order;
                }
            }
        }
        protected override void SetRush(bool rush)
        {
            _data.Rush = rush;
            _data.MaxTechnicians = (int)(_data.BaseMaxTechnicians * Formulas.SRFormulas.RushPrice(rush));
            if (Status == IntegrationStatus.Waiting)
            {
                _data.Orders.RemoveAll(x => x.CurrentProduction >= x.TotalProduction);
                if (_data.Orders.Count == 0) 
                {
                    Complete();
                    return;
                }
                if (rush)
                {
                    LongestOrder.RushRequested = true;
                    _pm.Contractor(LongestOrder.ContractorId).UpdateRates(false);
                    foreach (HardwareOrder order in _data.Orders.Where(or => or.TimeToCompletion > LongestOrder.TimeToCompletion))
                    {
                        order.RushRequested = true;
                        _pm.Contractor(order.ContractorId).UpdateRates(false);
                    }
                }
                else
                {
                    foreach (HardwareOrder order in _data.Orders)
                    {
                        order.RushRequested = false;
                        _pm.Contractor(order.ContractorId).UpdateRates(false);
                    }
                    AssignLongestOrder();
                }
            }
            else if (technicianSlider != null)
            {
                technicianSlider.MaxValue = _data.MaxTechnicians;
            }
            SetTechnicians(_data.Technicians); //reduces to max if max was reduced below current value
        }
        public void PlaceOrders()
        {
            foreach (HardwareOrder order in Data.Orders.Where(or => or.TotalProduction > or.CurrentProduction))
            {
                order.Priority = Id;
                order.ProjectId = Id;
                order.ProjectName = Name;
                _pm.Contractor(order.ContractorId).PlaceOrder(order);
            }
            AssignLongestOrder();
        }       
        public void Prioritize(ItemModel b)
        {
            _data.Orders.RemoveAll(x => x.CurrentProduction >= x.TotalProduction);
            int priority = -_pm.Prioritize();
            foreach (HardwareOrder order in _data.Orders)
            {
                order.Priority = priority;
                _pm.Contractor(order.ContractorId).UpdateRates(false);
                _pm.ReplaceInspectorGroup(order.ContractorId);
            }
            AssignLongestOrder();
            //_pm.ReplaceInspectorGroup(0);
        }
        public IntegrationScript(IntegrationData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
            _hm = pm.HM;
        }
        protected override void Complete()
        {
            bool flag = CanSetTechniciansStatus.Contains(Status);
            switch (_data.Status)
            {
                case IntegrationStatus.Waiting:
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Ready to Stack");
                    LongestOrder = null;
                    _pm.SetWarp(false);
                    foreach (HardwareOrder order in _data.Orders)
                    {
                        _pm.Contractor(order.ContractorId).CancelOrder(Id); // only required for cheaters
                    }
                    _data.Status = IntegrationStatus.Stacking;
                    base.UpdateRates(true);
                    SetTechnicians(_data.MaxTechnicians);
                    _pm.UpdateAllRates(false);
                    _pm.ReplaceInspectorGroup(Id);
                    break;
                case IntegrationStatus.Repairs:
                    goto case IntegrationStatus.Stacking;    
                case IntegrationStatus.Stacking:
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Ready to Roll Out");
                    _pm.SetWarp(false);
                    _pm.Career.SpendMoney((long)(_data.PricePerDay * (_data.TotalProgress- _data.CurrentProgress)/_data.ProgressPerDay));
                    _data.CurrentProgress = _data.TotalProgress;
                    _data.Status = IntegrationStatus.Stacked;
                    break;
                case IntegrationStatus.Rollout:
                    Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Ready to Launch");
                    _pm.SetWarp(false);
                    _pm.Career.SpendMoney((long)(_data.PricePerDay * (_data.TotalProgress- _data.CurrentProgress)/_data.ProgressPerDay));
                    _data.CurrentProgress = _data.TotalProgress;
                    _data.Status = IntegrationStatus.Ready;
                    break;
                case IntegrationStatus.Rollback:
                    if (Game.InFlightScene)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Rollback Complete");
                        _pm.SetWarp(false);
                        _pm.Career.SpendMoney((long)(_data.PricePerDay * (_data.TotalProgress- _data.CurrentProgress)/_data.ProgressPerDay));
                        _data.CurrentProgress = _data.TotalProgress;
                        _data.Status = IntegrationStatus.Stacked;
                        RemoveCraftFromPad();
                        break;
                    }
                    throw new Exception("Attempted to complete rollback outside the flightscene.");
                case IntegrationStatus.Recovery:
                    if (Game.InFlightScene)
                    {
                        Game.Instance.FlightScene.FlightSceneUI.ShowMessage(Name + " Recovery Complete");
                        _pm.SetWarp(false);
                        _pm.Career.SpendMoney((long)(_data.PricePerDay * (_data.TotalProgress- _data.CurrentProgress)/_data.ProgressPerDay));
                        _data.CurrentProgress = 0.0;
                        _data.TotalProgress = _data.RepairProgress;
                        _data.Status = IntegrationStatus.Repairs;
                        RemoveCraftFromPad();
                        break;
                    }
                    throw new Exception("Attempted to complete recovery outside the flightscene.");
                default:
                    break;
            }
            if (!CanSetTechniciansStatus.Contains(Status))
            {
                SetRush(false);
                SetTechnicians(0);
            }          
            else if (!flag)
            {
                SetTechnicians(_data.MaxTechnicians);
            }
            UpdateRates(false);
            _launchPadModel.Update();
            if (_statusModel != null)
            {
                _statusModel.Value = _data.Status.ToString();
            }
        }
        private bool Rushing()
        {
            return _data.Rush;
        }
        private string TimeToCompletionString()
        {   
            switch (TimeToCompletion)
            {
                case 0.0 : return "Complete";
                case double.NaN: return "Complete";
                case double.PositiveInfinity: return "No Progress";
                default: return Units.GetRelativeTimeString((float)TimeToCompletion);
            }
        }
        private float ProgressBarValue()
        {
            return LongestOrder == null ? (float)(_data.CurrentProgress / _data.TotalProgress) : (float)(LongestOrder.CurrentProduction /LongestOrder.TotalProduction);
        }
        public override GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _statusModel = _gModel.Add(new TextModel("Status",_data.Status.ToString));
            _launchPadModel = _gModel.Add(new TextModel("Pad", ()=> _data.LaunchLocation,null,null, () => _data.LaunchLocation != null));
            _gModel.Add(new ProgressBarModel(
                TimeToCompletionString, 
                ProgressBarValue));
            _gModel.Add(new ToggleModel("Rush", Rushing, SetRush));
            foreach (HardwareOrder order in _data.Orders)
            {
                _gModel.Add(new TextModel(order.ContractorName,order.TTCString, null, order.Description, order.IsOpen));
            }
            _gModel.Add(new TextModel("Price/Day", PricePerDayString));
            technicianSlider = _gModel.Add(new SliderModel("Technicians", ()=>_data.Technicians, num => SetTechnicians((int)num), 0, _data.MaxTechnicians, true)
                {
                    ValueFormatter = (num) => string.Format("{0:F0}", num),
                    DetermineVisibility = () => CanSetTechniciansStatus.Contains(_data.Status)
                }
                );
            if (_data.EfficiencyFactors.Count >0)
            {
                _gModel.Add(new LabelModel("Efficiency Factors", ElementAlignment.Center));
                foreach (EfficiencyFactor fac in _data.EfficiencyFactors)
                {
                    _gModel.Add(new TextModel(fac.Text,fac.PenaltyString, null, fac.ToolTip));
                }
                _gModel.Add(new SpacerModel());
            }
            _gModel.Add(new TextButtonModel(
                "Prioritize", 
                Prioritize, 
                null, 
                ()=>_data.Status == IntegrationStatus.Waiting));
            _gModel.Add(new TextButtonModel(
                "Rollout", 
                OnRolloutButtonClicked, 
                null, 
                CanRollout));
            _gModel.Add(new TextButtonModel(
                "Rollback", 
                OnRollbackButtonClicked, 
                null, 
                CanRollback));
            _gModel.Add(new TextButtonModel(
                "Launch", 
                OnLaunchButtonClicked, 
                null, 
                ()=> _data.Status == IntegrationStatus.Ready));
            _gModel.Add(new TextButtonModel(
                "Cancel", 
                OnCancelClicked, 
                null, 
                CanCancel));
            _gModel.Add(new TextButtonModel(
                "Complete", 
                (b) => Complete(),
                null,
                ()=> _pm.Career.TechTree.GetItemValue("Cheats.SpaceRaceCheats")?.ValueAsBool ?? false
                )); 
            return _gModel;
        }
        protected override void AddEfficiencyFactors()
        {
            base.AddEfficiencyFactors();
            if (_data.Status == IntegrationStatus.Stacking)
            _data.EfficiencyFactors = _pm.CostCalculator.IntegrationEfficiencyFactors(_hm.Get<SRCraftConfig>(config => config.Id == _data.ConfigId));
        }
        private bool CanCancel()
        {
            return CanCancelStatus.Contains(_data.Status);
        }
        private bool CanRollback()
        {
            return CanRollbackStatus.Contains(_data.Status);
        }
        private bool CanRollout()
        {
            return CanRolloutStatus.Contains(_data.Status);
        }

        public override void Cancel()
        {
            SetTechnicians(0);
            if (_data.LaunchLocation != null) _pm.ReleaseLaunchLocation(_data.LaunchLocation);
            foreach (HardwareOrder order in _data.Orders)
            {
                _pm.Contractor(order.ContractorId).CancelOrder(Id);
            }
            base.Cancel();
        }
        public void OnRolloutButtonClicked(ItemModel b)
        {
            Assets.Scripts.Menu.ListView.LaunchLocationsViewModel locationMenu = new Assets.Scripts.Menu.ListView.LaunchLocationsViewModel
            {
                PrimaryButtonText = "Rollout",
                Title = "Select Launch Pad",
                LaunchLocationSelected = loc => RollOut(loc)
            };
            Game.Instance.UserInterface.CreateListView(locationMenu);
        }
        public void OnRollbackButtonClicked(ItemModel b)
        {
            _data.Status = IntegrationStatus.Rollback;
            _data.CurrentProgress = 0.0;
            _data.TotalProgress = _data.RolloutProgress;
            SetTechnicians(_data.MaxTechnicians);
            _pm.UpdateAllRates(false);
            _pm.ReplaceInspectorGroup(Id);
        }
        public void OnLaunchButtonClicked(ItemModel b)
        {
            Launching = true;
            _pm.LaunchingId = Id;
            if (Game.InFlightScene)
            {   
                IFlightScene flightSceneScript = Game.Instance.FlightScene;
                ICraftNode node = flightSceneScript.FlightState.CraftNodes.First(cr=> cr.NodeId == _data.NodeId);
                if (!_pm.ContractsChanged && node.CraftScript != null && flightSceneScript.ChangePlayersActiveCommandPodImmediate(node.CraftScript.ActiveCommandPod, node))
                {
                    string text = $"Launching {Name}.";
                    flightSceneScript.FlightSceneUI.FlightLog.AddLog(text, FlightLogEntryCategory.Default);
                    flightSceneScript.FlightSceneUI.ShowMessage(text);
                    FlightSceneScript.Instance.FlightState.Save();
                    Game.Instance.GameState.Save();
                    Game.Instance.GameStateManager.CopyGameStateTag(Game.Instance.GameState.Id, "Active", "Preflight");
                    _pm.CheckForLaunch();
                    return;
                }
                flightSceneScript.ChangePlayersActiveCraftNode(node);
                Debug.Log("Reloading scene to launch.");
                return;
            }
            if (Game.InDesignerScene)
            {
                string id = Game.Instance.GameState.Id;
                string gameStateTag = "Active";
                IDesigner designer = Game.Instance.Designer;
                designer.SaveCraft(CraftDesigns.EditorCraftId);
                designer.PerformanceAnalysis.Visible = false; 
                CraftScript craft = designer.CraftScript as CraftScript;
                craft.Unload();
                Game.Instance.LoadGameStateOrDefault(id, gameStateTag);
            }
            FlightSceneLoadParameters parameters = FlightSceneLoadParameters.ResumeCraft(_data.NodeId);
            Game.Instance.StartFlightScene(parameters);
            return;
        }
        public void SetTechnicians(int i)
        {
            if (i > _data.Technicians + _pm.AvailableTechnicians) i = _data.Technicians + _pm.AvailableTechnicians;
            if (i > _data.MaxTechnicians) i = _data.MaxTechnicians;
            _pm.AvailableTechnicians += _data.Technicians - i;
            _data.Technicians = i;
            _data.BaseProgressPerDay = _data.Technicians > _data.BaseMaxTechnicians ? SRFormulas.TechProgress * (_data.BaseMaxTechnicians +  (0.5 * (_data.Technicians - _data.BaseMaxTechnicians))) : SRFormulas.TechProgress * _data.Technicians;
            _pm.SetNextCompletion(TimeToCompletion, this);
        }
        public override void UpdateRates(bool efficiencies)
        {
            switch (_data.Status)
            {
                case IntegrationStatus.Waiting: 
                    
                    List<HardwareOrder> list = new List<HardwareOrder>(_data.Orders.Where(or => or.CurrentProduction >= or.TotalProduction));
                    if (list.Count > 0)
                    {
                        _data.Orders.RemoveAll(x => list.Contains(x));
                        foreach (HardwareOrder order in list)
                        {
                            _pm.Contractor(order.ContractorId).UpdateRates(false);
                        }
                        _pm.ReplaceInspectorGroup(Id);
                    }
                    if (_data.Orders.Count == 0)
                    {
                        Complete();
                    }
                    else
                    {
                        _pm.Contractor(LongestOrder.ContractorId).UpdateRates(false);
                        _pm.SetNextCompletion(TimeToCompletion, this);
                    }
                    return;
                default:
                    base.UpdateRates(efficiencies);
                    return;
            }
        }
        private void EmptyTanks(CraftData craft)
        {
            foreach (PartData part in craft.Assembly.Parts)
            {
                FuelTankData tank = part.GetModifier<FuelTankData>();
                if (tank != null && tank.FuelType != ModApi.Craft.Propulsion.FuelType.Battery) tank.Fuel = 0.0;
            }
        }

        private static bool IsCraftTooCloseToLaunchPosition(Vector3d existingCraftPosition, Vector3d launchPosition)
        {
            Vector3d lhs = existingCraftPosition - launchPosition;
            Vector3d normalized = launchPosition.normalized;
            double num = Mathd.Abs(Vector3d.Dot(lhs, normalized));
            double num2 = lhs.sqrMagnitude - num * num;
            return num2 < 400.0 && num < 250.0;
        }
        private void ClearStartLocation(CraftNode craftNode)
        {
        CraftNode[] array = FlightSceneScript.Instance.FlightState.CraftNodes.ToArray();
        CraftNode[] array2 = array;
        foreach (CraftNode craftNode2 in array2)
        {
            if (craftNode2 != craftNode && IsCraftTooCloseToLaunchPosition(craftNode2.Position, craftNode.Position))
            {
                try
                {
                    SRCraftRecovery craftRecovery = new SRCraftRecovery(_pm, craftNode2, new CraftNodeDataDynamic(craftNode2));
                    craftRecovery.RecoverParts();
                    craftNode2.DestroyCraft();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Unable to recover craft.\n" + ex.ToString());
                }
            }
        }
    }
        private void RollOut(LaunchLocation location)
        {
            if (!_pm.LocationAvailable(location.Name))
            throw new Exception("Launchpad is occupied.  You shouldn't have been able to select it.");
            if (_pm.HM.Craft[_data.ConfigId].Height > location.MaxCraftHeight)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Craft is too tall for this lauchpad.");
                return;
            }
            if (_pm.HM.Craft[_data.ConfigId].Width > location.MaxCraftDiameter)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Craft is too wide for this lauchpad.");
                return;
            }
            if (_pm.HM.Craft[_data.ConfigId].Mass > location.MaxCraftMass)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Craft is too heavy for this lauchpad.");
                return;
            }
            FlightSceneScript fs = FlightSceneScript.Instance;
            if (fs == null)
            {
                return;
            }
            _pm.GoToSpaceCenter();
            _data.LaunchLocation = location.Name;
            _pm.ReserveLaunchLocation(location.Name);
            _pm.SwitchToNearestSpaceCenter(location);
            XElement craftXml = _pm.Designs.GetCraftDesign(string.Format("Integration{0:D}", _data.ConfigId));
            CraftData craftData = Game.Instance.CraftLoader.LoadCraftImmediate(craftXml);
            EmptyTanks(craftData);
            CraftNode node = fs.SpawnCraft(Name, craftData, location);
            ClearStartLocation(node);
            node.FlightStart();
            _data.NodeId = node.NodeId;
            _data.Status = IntegrationStatus.Rollout;
            _data.CurrentProgress = 0.0;
            _data.TotalProgress = _data.RolloutProgress * _pm.Career?.TechTree.GetItemValue("SpaceRace.GroundLightReliability")?.ValueAsFloat ?? _data.RolloutProgress;
            SetTechnicians(_data.MaxTechnicians);
            UpdateRates(true);
            _pm.UpdateAllRates(false);
            _pm.ReplaceInspectorGroup(Id);
        }
        private void RemoveCraftFromPad()
        {
            _pm.ReleaseLaunchLocation(_data.LaunchLocation);
            _data.LaunchLocation = null;
            CraftNode craftNode = Game.Instance.FlightScene.FlightState.CraftNodes.FirstOrDefault(node => node.NodeId == _data.NodeId) as CraftNode;
            if (craftNode.IsPlayer) 
            {
                _pm.GoToSpaceCenter(true);
            }
            else
            {
                craftNode.Enabled = false;
                craftNode.DestroyCraft();
            }
        }
    }
    public class IntegrationData : ProjectData
    {
        [XmlAttribute] 
        public IntegrationStatus Status = IntegrationStatus.Waiting;
        public List<HardwareOrder> Orders = null;
        [XmlAttribute]
        public int Technicians = 0;
        [XmlAttribute]
        public int BaseMaxTechnicians = 10;
        [XmlAttribute]
        public int MaxTechnicians = 10;
        [XmlAttribute]
        public int ConfigId = 0;
        public override long PricePerDay => Status == IntegrationStatus.Waiting? 0 : SRFormulas.TechProgress * Technicians;
        public override double ProgressPerDay => Status == IntegrationStatus.Waiting? 0.0 : BaseProgressPerDay * Efficiency;
        [XmlAttribute]
        public bool Repeat = false;
        [XmlAttribute]
        public bool Recovered = false;
        [XmlAttribute]
        public double RolloutProgress = 0.0;
        [XmlAttribute]
        public double RepairProgress = 0.0;
        [XmlAttribute]
        public int NodeId = 0;
        [XmlAttribute]
        public string LaunchLocation;
    }

    public enum IntegrationStatus
    {
        Designer,
        Configuration,
        Waiting,
        Stacking,
        Stacked,
        Rollout,
        Rollback,
        Ready,
        Recovery,
        Repairs

    }

}