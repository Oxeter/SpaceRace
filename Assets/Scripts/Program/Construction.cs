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
    using System.Linq;
    using Assets.Scripts.Ui.Inspector;
    using Assets.Scripts.Flight;

    public class ConstructionScript : ProjectScript<ConstructionData>
    {
        public override ProjectCategory Category {get {return ProjectCategory.Construction;}}

        public ConstructionScript(ConstructionData data, IProgramManager pm)
        {
            _data = data;
            _pm = pm;
            _hm = pm.HM;
        }

        protected override void Complete()
        {
            base.Complete();
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == Data.ContractId);
            if (contract != null)
            {
                contract.Status = Career.Contracts.ContractStatus.Complete;
                _pm.Career.Contracts.CloseContract(contract, Game.Instance.GameState.LoadFlightStateData());
                _pm.Data.MaxTechnicians += _data.AdditionalTechnicians;
            }
            _pm.EndProject(Id);
        }



        public override GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _gModel.Add(new ProgressBarModel(
                () => Units.GetRelativeTimeString(_data.TimeToCompletion), 
                () => (float)(_data.CurrentProgress / _data.TotalProgress)));
            _gModel.Add(new ToggleModel("Rush", () => _data.Rush, (f) => _data.Rush=f));
            _gModel.Add(new TextModel("Price/Day", () => Units.GetMoneyString(PricePerDay)));
            _gModel.Add(new TextModel("Technician Capacity", () => _data.AdditionalTechnicians.ToString(), null, "The additional technicians that this launchpad can support"));
            _gModel.Add(new TextButtonModel(
                "Pause", 
                OnPauseClicked, null, ()=> !_data.Completed));
            _gModel.Add(new TextButtonModel(
                "Resume", 
                OnResumeClicked, null, ()=> _data.Completed));               
            _gModel.Add(new TextButtonModel(
                "Cancel", 
                OnCancelClicked));
            _gModel.Add(new TextButtonModel(
                "Complete", 
                (b) => Complete(),
                null,
                ()=> _pm.Career.TechTree.GetItemValue("Cheats.SpaceRaceCheats")?.ValueAsBool ?? false
                )); 
            return _gModel;
        }

        public override void Cancel()
        {
            Assets.Scripts.Career.Contracts.Contract contract = _pm.Career.Contracts.Active.FirstOrDefault(con => con.Id == Data.ContractId);
            if (contract != null)
            {
                contract.Status = Career.Contracts.ContractStatus.Generated;
            }
            base.Cancel();
        }

        public override void Initialize()
        {

        }

        public override void UpdateRates(bool efficiencies)
        {

        }
        public void OnPauseClicked(ItemModel m)
        {
            _data.Completed = true;
            UpdateRates(false);
        }
        public void OnResumeClicked(ItemModel m)
        {
            _data.Completed = false;
            UpdateRates(false);
        }
    }

    public class ConstructionData : ProjectData
    {
        [XmlAttribute]
        public string ContractId;
        //public bool Pause = false;
        //public override double ProgressPerDay => Pause? 0.0 : base.ProgressPerDay;
        //public override long PricePerDay => Pause? 0 : base.PricePerDay;
        [XmlAttribute]
        public int AdditionalTechnicians;
    }




}