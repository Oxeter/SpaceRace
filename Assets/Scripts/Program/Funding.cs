namespace Assets.Scripts.SpaceRace.Projects
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System.Xml;
    using System.Xml.Serialization;
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Collections;
    using ModApi.Math;
    using System;
    using Assets.Scripts.SpaceRace.Formulas;
    using System.Linq;
    using ModApi.Ui.Inspector;
    using HarmonyLib;

    public class FundingScript : ProjectScript<FundingData>, IProgramChild
    {
        private TextModel _statusModel;
        private LabelModel _contractsModel;
        public override ProjectCategory Category {get {return ProjectCategory.Funding;}}
        public List<Career.Contracts.Contract> Contracts = new List<Career.Contracts.Contract>();
        public void AddContract(Career.Contracts.Contract contract)
        {
            //Debug.Log($"{contract.Name} added {contract.RewardMoney} new funding to {Name}");
            _data.TotalProgress += contract.RewardMoney;
            Contracts.Add(contract);
            UpdateRates(true);
            _contractsModel?.Update();
        }
        public void CompleteContract(Career.Contracts.Contract contract)
        {
            _data.PermanentFunding += contract.RewardMoney;
            _data.TotalProgress += contract.RewardMoney;
            RemoveContract(contract);
        }
        public void CancelContract(Career.Contracts.Contract contract)
        {
            RemoveContract(contract);
        }
        public void RemoveContract(Career.Contracts.Contract contract)
        {
            _data.TotalProgress -= contract.RewardMoney;
            Contracts.Remove(contract);
            UpdateRates(true);
            _contractsModel?.Update();
        }
        public override GroupModel GetGroupModel()
        {
            GroupModel _gModel = new GroupModel(_data.Name, Id.ToString());
            _statusModel = _gModel.Add(new TextModel("Status", () => _data.ProgressPerDay == 0? "Depleted" : _data.ProgressPerDay > 0? "Disbursing" :"Clawback"));
            _gModel.Add(new ProgressBarModel(
                () => Units.GetRelativeTimeString(_data.TimeToCompletion), 
                () => (float)(_data.CurrentProgress / _data.TotalProgress)));
            _gModel.Add(new TextModel("Funding/Day", PricePerDayString));
            _gModel.Add(new TextModel("Funds Remaining", ()=> Units.GetMoneyString((long)((_data.TotalProgress-_data.CurrentProgress)* _data.Difficulty))));
            _gModel.Add(new LabelModel("Contracts", ElementAlignment.Center));
            _contractsModel = _gModel.Add(new LabelModel(string.Join("\n", Contracts.Select(con => con.Name))));
            return _gModel;
        }
        public override void UpdateProgress(double dT)
        {
            _data.CurrentProgress += _data.ProgressPerDay * dT * Formulas.SRFormulas.DaysPerSecond;
            if (_data.TimeToCompletion < 0) 
            {
                Complete();
            }
        }
        public override void UpdateRates(bool efficiencies)
        {
            _data.Clawback = _data.CurrentProgress > _data.TotalProgress;
            _data.Active = Contracts.Count > 0 || _data.Clawback;
            _data.Completed = _data.TotalProgress == _data.CurrentProgress;
            _statusModel?.Update();
        }
        public FundingScript(FundingData data, IProgramManager pm, PersistantFundingData pdata = null)
        {
            _data = data;
            _data.Difficulty = ModSettings.Instance.FundingMultiplier;
            _pm = pm;
            _data.PermanentFunding = 0;
            _data.TotalProgress = _data.PermanentFunding;
            _data.CurrentProgress = pdata?.CurrentProgress ?? 0;
            _data.Efficiency = pdata?.Efficiency ?? 1;
            _data.Active = false;
        }
        public override void Initialize()
        {
        }
        public override string PricePerDayString()
        {
            return (!_data.Completed && _data.Active)? Units.GetMoneyString(-_data.PricePerDay) : Units.GetMoneyString(0);
        }
        public override string PricePerDayTooltip()
        {
            return Units.GetMoneyString((long)((_data.TotalProgress-_data.CurrentProgress)*_data.Difficulty)) + "\n"
            +string.Join("\n", Contracts.Select(con=> con.Name));
        }
        protected override void Complete()
        {
            _pm.Career.SpendMoney((long)((_data.TotalProgress-_data.CurrentProgress)*_data.Difficulty)); 
            _data.CurrentProgress = _data.TotalProgress;
            UpdateRates(true);
        }
        public PersistantFundingData GetPersistantFundingData()
        {
            return new PersistantFundingData(){
                Name = _data.Name,
                Efficiency = _data.Efficiency,
                PermanentFunding = _data.PermanentFunding,
                CurrentProgress = _data.CurrentProgress,
            };
        }
    }
    public class FundingData : ProjectData
    {
        [XmlAttribute]
        public List<string> ContractIds;
        [XmlAttribute]
        public long PermanentFunding = 0;
        [XmlAttribute]
        public bool Clawback = false;
        [XmlIgnore]
        public float Difficulty = 1F;
        public override double ProgressPerDay => Completed ? 0.0 : (Clawback ? SRFormulas.ClawbackFactor : 1.0) * BaseProgressPerDay * Efficiency;
        public override long PricePerDay =>  -(long)(ProgressPerDay* Difficulty);
    }

    public class PersistantFundingData
    {
        [XmlAttribute]
        public string Name;
        [XmlAttribute]
        public double Efficiency = 1;
        [XmlAttribute]
        public long PermanentFunding = 0;
        [XmlAttribute]
        public double CurrentProgress = 0;
    }

}