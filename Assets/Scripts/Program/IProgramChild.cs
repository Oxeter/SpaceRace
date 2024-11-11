namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using ModApi.Common.UI;
    using ModApi.Ui.Inspector;
    using UnityEngine;


    public interface IProgramChild
    {
        public ProjectCategory Category {get;}
        public int Id {get;}
        public string Name {get;}
        public long PricePerDay {get;}
        public bool Active {get;}
        public bool Completed {get;}
        public virtual string Tooltip => $"{Name} {Category}";
        public string PricePerDayString();
        public string PricePerDayTooltip();
        public bool PricePerDayVisible();
        public void UpdateProgress(double dT);
        public void UpdateRates(bool efficiencies);
        public void Initialize();
        public int ContractorId {get;}
        public GroupModel GetGroupModel();

    }
}