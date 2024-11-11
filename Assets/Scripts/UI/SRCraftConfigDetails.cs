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
    using Assets.Scripts.SpaceRace.Hardware;
    using Assets.Scripts.SpaceRace.Formulas;

    public class SRCraftConfigDetails
    {
        private IProgramManager _pm;
        private DetailsPropertyScript _mass;
        private DetailsPropertyScript _height;
        private DetailsPropertyScript _width;
        private DetailsPropertyScript _integrations;
        private DetailsPropertyScript _simIntegrations;
        private DetailsPropertyScript _undevelopedPrice;
        private DetailsPropertyScript _basePrice;
        private DetailsTextScript _priceDesc;
        private DetailsPropertyScript _maxTechnicians;
        private DetailsPropertyScript _integrationTime;
        private DetailsPropertyScript _integrationCost;
        private DetailsTextScript _fuels;
        private DetailsTextScript _comment;
        private DetailsTextScript _efficiencyFactors;
        private DetailsTextScript _orders;
        private DetailsPropertyScript _ordersPrice;

        public SRCraftConfigDetails(ListViewDetailsScript listViewDetails, IProgramManager pm)
        {
            _pm = pm;
            //_image = listViewDetails.Widgets.AddImage();
            //_image.SetSize(200);
            //if (Game.IsCareer)
            //{
            //    _career = listViewDetails.Widgets.AddPropertyPair("Mass", "Size");
            //}
            //_productionTime = listViewDetails.Widgets.AddProperty("Production Time");
            _mass = listViewDetails.Widgets.AddProperty("Mass");
            _height = listViewDetails.Widgets.AddProperty("Height");
            _width = listViewDetails.Widgets.AddProperty("Width");
            _integrations = listViewDetails.Widgets.AddProperty("Previous Integrations");
            _simIntegrations = listViewDetails.Widgets.AddProperty("Similar Integrations (Weighted and Capped)");
            listViewDetails.Widgets.AddSpacer();
            _ordersPrice = listViewDetails.Widgets.AddProperty("Orders");
            _ordersPrice.Tooltip = "The price of parts and stages produced by our contractors"; 
            _orders = listViewDetails.Widgets.AddText(string.Empty);
            listViewDetails.Widgets.AddSpacer();
            _undevelopedPrice = listViewDetails.Widgets.AddProperty("Other Parts"); 
            _undevelopedPrice.Tooltip = "The price of parts that were not developed by a contractor";
            _basePrice = listViewDetails.Widgets.AddProperty("Base Stacking Cost");
            _basePrice.Tooltip = "The cost of stacking this vehicle at 100% efficiency";
            _priceDesc = listViewDetails.Widgets.AddText(string.Empty);
            listViewDetails.Widgets.AddText("Efficiency Factors");
            _efficiencyFactors = listViewDetails.Widgets.AddText(string.Empty);  
            _integrationCost = listViewDetails.Widgets.AddProperty("Expected Stacking Cost");
            _integrationCost.Tooltip = "The expected cost of stacking this vehicle given the current efficiency factors";
            _maxTechnicians = listViewDetails.Widgets.AddProperty("Max Technicians");
            _maxTechnicians.Tooltip = "The number of technicians that can work on this vehicle at once";
            _integrationTime = listViewDetails.Widgets.AddProperty("Integration Time (Fully Staffed)");
            listViewDetails.Widgets.AddSpacer();
            listViewDetails.Widgets.AddText("Fuel Required");
            _fuels = listViewDetails.Widgets.AddText("Fuel Required"); 
            _comment = listViewDetails.Widgets.AddText("Comments");
        }

        public void UpdateDetails(SRCraftConfig config)
        {
            _mass.ValueText = Units.GetMassString(config.Mass);
            _height.ValueText = Units.GetDistanceString(config.Height);
            _width.ValueText = Units.GetDistanceString(config.Width);
            _integrations.ValueText = config.Integrations.ToString();
            _simIntegrations.ValueText = string.Format("{0:F1}", config.SimilarIntegrations);
            _basePrice.ValueText = Units.GetMoneyString((long)config.ProductionRequired);
            _priceDesc.Text = config.ProductionDescription;
            _maxTechnicians.ValueText = _pm.CostCalculator.GetMaxTechnicians(config).ToString();
            _integrationTime.ValueText = Units.GetRelativeTimeString(Formulas.SRFormulas.SecondsPerDay * config.ProductionRequired * _pm.CostCalculator.IntegrationEfficiencyFactors(config).Sum(ef => 1.0F + ef.Penalty) / SRFormulas.TechProgress / _pm.CostCalculator.GetMaxTechnicians(config));
            _integrationCost.ValueText = Units.GetMoneyString((long)(config.ProductionRequired * _pm.CostCalculator.IntegrationEfficiencyFactors(config).Sum(ef => 1.0F + ef.Penalty)));
            _fuels.Text=string.Join("\n", config.Fuel.Select(v => $"{v.Item1} {Units.GetVolumeString((float)v.Item2)} {Units.GetMoneyString(v.Item3)}"));
            _ordersPrice.ValueText = Units.GetMoneyString(config.Orders.Sum(or => or.Price));
            _orders.Text=string.Join("\n", config.Orders.Select(or => or.ToString()));  
            _undevelopedPrice.ValueText = Units.GetMoneyString(config.UndevelopedPartPrice);
            _efficiencyFactors.Text = string.Join("\n", _pm.CostCalculator.IntegrationEfficiencyFactors(config).Select(ef => ef.ToString()));
            _comment.Text = string.Empty;
            _comment.Color = "#ff9a30";
        }

    }

}
