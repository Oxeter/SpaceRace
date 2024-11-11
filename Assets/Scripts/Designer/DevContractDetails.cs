namespace Assets.Scripts.SpaceRace.Ui
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Assets.Scripts;
    using Assets.Scripts.Menu.ListView;
    using Assets.Scripts.SpaceRace.Projects;
    using System;
    using ModApi.Math;
    using System.Linq;
    using Assets.Scripts.SpaceRace.Collections;

    public class PartDevContractDetails
    {
        private DetailsPropertyScript _totalCapacity;
        private DetailsPropertyScript _newCapacity;
        private DetailsPropertyScript _productionTime;
        private DetailsPropertyScript _productionTimeNew;
        private DetailsPropertyScript _productionCost;
                private DetailsPropertyScript _productionPrice;
                private DetailsPropertyScript _developmentBaseTime;
        private DetailsPropertyScript _developmentTime;
        private DetailsPropertyScript _developemntCost;
        private DetailsPropertyScript _developemntPricePerDay;
        private DetailsTextScript _comment;

        private DetailsTextScript _efficiencyFactors;

        public PartDevContractDetails(ListViewDetailsScript listViewDetails)
        {

            DetailsTextScript production = listViewDetails.Widgets.AddText("Production");  
            production.Alignment = TMPro.TextAlignmentOptions.Center;
            _productionCost = listViewDetails.Widgets.AddProperty("Production Per Unit");
            _newCapacity = listViewDetails.Widgets.AddProperty("Added Capacity");
            _productionTimeNew = listViewDetails.Widgets.AddProperty("Days/Unit with added capacity");
            _totalCapacity = listViewDetails.Widgets.AddProperty("New Total Capacity");
            _productionTime = listViewDetails.Widgets.AddProperty("Days/Unit with full capacity");
            _productionPrice = listViewDetails.Widgets.AddProperty("Unit Price");
            DetailsTextScript development = listViewDetails.Widgets.AddText("Development");  
            development.Alignment = TMPro.TextAlignmentOptions.Center;
            _developmentBaseTime = listViewDetails.Widgets.AddProperty("Base Development Time");
            _efficiencyFactors = listViewDetails.Widgets.AddText("Efficiency Factors");  
            _developmentTime = listViewDetails.Widgets.AddProperty("Expected Development Time");
            _developemntPricePerDay = listViewDetails.Widgets.AddProperty("Price Per Day");
            _developemntCost = listViewDetails.Widgets.AddProperty("Total Development Price"); 
            DetailsTextScript comments = listViewDetails.Widgets.AddText("Comments");
            comments.Alignment = TMPro.TextAlignmentOptions.Center;
            _comment = listViewDetails.Widgets.AddText("comments", "#ff9a30");   
        }

        public void UpdateDetails(DevBid offer)
        {
            _totalCapacity.ValueText = Units.GetMoneyString((long)offer.ProductionCapacity);
            _productionTime.ValueText = Units.GetRelativeTimeString(offer.ProjectedProductionTime);
            _newCapacity.ValueText = Units.GetMoneyString((long)offer.NewProductionCapacity);
            _productionTimeNew.ValueText = Units.GetRelativeTimeString(offer.ProjectedProductionTimeNew);
            _productionCost.ValueText = Units.GetMoneyString((long)offer.ProductionRequired);
            _productionPrice.ValueText = Units.GetMoneyString(offer.ProductionCost);
            _developmentBaseTime.ValueText = Units.GetRelativeTimeString(offer.BaseDevTime);
            _efficiencyFactors.Text = String.Join("\n", offer.EfficiencyFactors.Select(ef => ef.ToString()));
            _developmentTime.ValueText = Units.GetRelativeTimeString(offer.ProjectedDevelopmentTime);
            _developemntPricePerDay.ValueText = Units.GetMoneyString(offer.DevCostPerDay);
            _developemntCost.ValueText = Units.GetMoneyString(offer.TotalProjectedCost);
            _comment.Text = String.Join("\n", offer.Comments);
        }

    }

    public class StageDevContractDetails
    {
        private DetailsPropertyScript _totalCapacity;
        private DetailsPropertyScript _newCapacity;
        private DetailsPropertyScript _productionTime;
        private DetailsPropertyScript _productionTimeNew;
        private DetailsPropertyScript _productionCost;
        private DetailsPropertyScript _productionPrice;
        private DetailsTextScript _productionDescription;

        private DetailsTextScript _comment;
        private DetailsPropertyScript _developmentBaseTime;
        private DetailsPropertyScript _developmentTime;
        private DetailsPropertyScript _developemntCost;
        private DetailsPropertyScript _developemntPricePerDay;
        private DetailsTextScript _efficiencyFactors;

        public StageDevContractDetails(ListViewDetailsScript listViewDetails)
        {
            DetailsTextScript production = listViewDetails.Widgets.AddText("Production"); 
            production.Alignment = TMPro.TextAlignmentOptions.Center; 
            _productionCost = listViewDetails.Widgets.AddProperty("Production Per Stage");
            _productionDescription = listViewDetails.Widgets.AddText(""); 
            _newCapacity = listViewDetails.Widgets.AddProperty("Added Capacity");
            _productionTimeNew = listViewDetails.Widgets.AddProperty("Days/Unit with added capacity");
            _totalCapacity = listViewDetails.Widgets.AddProperty("New Total Capacity");
            _productionTime = listViewDetails.Widgets.AddProperty("Days/Unit with full capacity");
            _productionPrice = listViewDetails.Widgets.AddProperty("Unit Price");
            listViewDetails.Widgets.AddSpacer();
            DetailsTextScript development = listViewDetails.Widgets.AddText("Development");
            development.Alignment = TMPro.TextAlignmentOptions.Center;
            _developmentBaseTime = listViewDetails.Widgets.AddProperty("Base Development Time");
            _efficiencyFactors = listViewDetails.Widgets.AddText("Efficiency Factors");  
            _developmentTime = listViewDetails.Widgets.AddProperty("Expected Development Time");
            _developemntPricePerDay = listViewDetails.Widgets.AddProperty("Price Per Day");
            _developemntCost = listViewDetails.Widgets.AddProperty("Total Development Price");
            DetailsTextScript comments = listViewDetails.Widgets.AddText("Comments");
            comments.Alignment = TMPro.TextAlignmentOptions.Center;
            _comment = listViewDetails.Widgets.AddText("Comments", "#ff9a30");
        }

        public void UpdateDetails(StageDevBid offer)
        {
            if (offer.ProductionCosts.Count > 1)
            {
                _productionCost.LabelText = "Production Per Stage Set";
            }
            else 
            {
                _productionCost.LabelText = "Production Per Stage";
            }
            _productionCost.ValueText = Units.GetMoneyString((long)offer.ProductionRequired);
            _totalCapacity.ValueText = Units.GetMoneyString((long)offer.ProductionCapacity);
            _productionTime.ValueText = Units.GetRelativeTimeString(offer.ProjectedProductionTime);
            _productionTimeNew.ValueText = Units.GetRelativeTimeString(offer.ProjectedProductionTimeNew);
            _newCapacity.ValueText = Units.GetMoneyString((long)offer.NewProductionCapacity);
            _productionPrice.ValueText = Units.GetMoneyString((long)offer.ProductionCost);
            _productionDescription.Text = offer.ProductionsDescription;
            _developmentBaseTime.ValueText = Units.GetRelativeTimeString(offer.BaseDevTime);
            _efficiencyFactors.Text = String.Join("\n", offer.EfficiencyFactors.Select(ef => ef.ToString()));
            _developmentTime.ValueText = Units.GetRelativeTimeString(offer.ProjectedDevelopmentTime);
            _developemntPricePerDay.ValueText = Units.GetMoneyString(offer.DevCostPerDay);
            _developemntCost.ValueText = Units.GetMoneyString(offer.TotalProjectedCost);
            _comment.Text = String.Join("\n", offer.Comments);
        }

    }
}
