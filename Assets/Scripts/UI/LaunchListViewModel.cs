namespace Assets.Scripts.SpaceRace.Ui
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Assets.Scripts.State;
    using ModApi.Craft;
    using ModApi.Math;
    using ModApi.Services.Purchasing;
    using ModApi.State;
    using ModApi.Ui;
    using UnityEngine;
    using Assets.Scripts.Menu.ListView;
    using Assets.Scripts.SpaceRace.Projects;
    using Assets.Scripts.SpaceRace.Hardware;

    public class LaunchListViewModel : ListViewModel
    {  
        private IProgramManager _pm;
        private List<ILaunchable> _craft;
        private VerboseCraftConfiguration _vconfig;
        private SRCraftConfigDetails _details;
        //
        // Summary:
        //     Gets or sets the action to execute when the craft has been selected.
        //
        //
        // Value:
        //     The offer selected action.
        public Action<SRCraftConfig> CraftSelected { get; set; }
        //
        // Summary:
        //     Gets or sets the primary button text.
        //
        // Value:
        //     The primary button text.
        public string PrimaryButtonText { get; set; } = "BEGIN INTEGRATION";


        //
        // Summary:
        //     Gets or sets the title.
        //
        // Value:
        //     The title.
        public string Title { get; set; } = "CRAFT MANAGER";
        
        public LaunchListViewModel(List<ILaunchable> integrations, List<ILaunchable> configs, IProgramManager pm, VerboseCraftConfiguration vconfig = null)
        {
            _pm = pm;
            _craft = integrations;
            if (vconfig != null)
            {
                _vconfig = vconfig;
                SRCraftConfig config = new SRCraftConfig(vconfig, Game.Instance.Designer.CraftScript.Data.Name, IntegrationStatus.Designer);
                string desc = string.Empty;
                config.SimilarIntegrations = pm.HM.GetStartingSimilarIntegrations(config);
                config.ProductionRequired = pm.CostCalculator.GetIntegrationProduction(config, ref desc);
                config.ProductionDescription = desc;
                foreach (HardwareOrder order in config.Orders)
                {
                    order.Description = pm.HM.GetDescription(order.HardwareRequired);
                }
                _craft.Insert(0, config);
            }
            _craft.AddRange(configs);
        }
        private string Subtitle(ILaunchable craft)
        {
            switch (craft.Status)
            {
                case IntegrationStatus.Designer:
                    goto case IntegrationStatus.Configuration;
                case IntegrationStatus.Configuration: 
                    return string.Format("Estimated cost: {0}", Units.GetMoneyString(_pm.CostCalculator.GetIntegrationCost(craft.Config)));
                case IntegrationStatus.Waiting:
                    goto case IntegrationStatus.Rollback;
                case IntegrationStatus.Rollout:
                    goto case IntegrationStatus.Rollback;
                 case IntegrationStatus.Stacking:
                    goto case IntegrationStatus.Rollback;                   
                case IntegrationStatus.Rollback:
                    return $"{craft.Status} time remaining: {Units.GetRelativeTimeString(craft.TimeToCompletion)}";
                default:
                    return craft.Status.ToString();
            }
        }
        public override IEnumerator LoadItems()
        {
            _details = new SRCraftConfigDetails(base.ListView.ListViewDetails, _pm);
            ListView.SelectedItem = null;
            for (int i = 0; i < _craft.Count(); i++)
            {
                ListViewItemScript script = ListView.CreateItem(_craft[i].Name, Subtitle(_craft[i]), _craft[i]);
                if (_vconfig != null && i==0)
                {
                    script.Title += " (Designer Craft)";
                    ListView.SelectedItem = script;
                }
            }
            yield return new WaitForEndOfFrame();
        }
        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.Title = Title;
            listView.PrimaryButtonText = PrimaryButtonText;
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
        }
        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            ILaunchable craft = selectedItem?.ItemModel as ILaunchable;
            IntegrationScript script = null;
            ListView.Close();
            switch (craft.Status)
            { 
                case IntegrationStatus.Designer:
                    InputDialogScript dialog = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.UserInterface.Transform);
                    dialog.MessageText = "Name this craft.";
                    dialog.InputPlaceholderText = "Name";
                    dialog.InputText = craft.Name;
                    dialog.OkayClicked += (s) => {s.Close(); _pm.NewIntegrationFromDesigner(_vconfig,s.InputText);};
                    return;
                case IntegrationStatus.Configuration:
                    InputDialogScript dialog2 = Game.Instance.UserInterface.CreateInputDialog(Game.Instance.UserInterface.Transform);
                    dialog2.MessageText = "Name this craft.";
                    dialog2.InputPlaceholderText = "Name";
                    dialog2.InputText = craft.Name;
                    dialog2.OkayClicked += (s) => {s.Close(); _pm.RepeatIntegration(craft.Config,s.InputText);};
                    return;
                case IntegrationStatus.Stacked:
                    script = craft as IntegrationScript;
                    script.OnRolloutButtonClicked(null);
                    return;
                case IntegrationStatus.Ready:
                    script = craft as IntegrationScript;
                    script.OnLaunchButtonClicked(null);
                    return;
                default:
                    return;
            }
        }

        public override void OnDeleteButtonClicked(ListViewItemScript selectedItem)
        {
            ILaunchable craft = selectedItem?.ItemModel as ILaunchable;
            switch (craft.Status)
            {
                case IntegrationStatus.Configuration:
                    craft.Config.Active = false;
                    break;
                case IntegrationStatus.Ready:
                    goto case IntegrationStatus.Stacked;
                case IntegrationStatus.Stacked:
                    _pm.Integrations[craft.Id].Cancel();
                    break;
            }
            selectedItem.Visible = false;
            selectedItem.Selected = false;
        }

        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                ILaunchable craft = item.ItemModel as ILaunchable;
                _details.UpdateDetails(craft.Config);
                switch (craft.Status)
                {
                    case IntegrationStatus.Designer:
                        ListView.CanDelete = false;
                        ListView.PrimaryButtonText = "INTEGRATE";
                        break;
                    case IntegrationStatus.Configuration:
                        ListView.CanDelete = true;
                        ListView.PrimaryButtonText = "INTEGRATE";
                        break;
                    case IntegrationStatus.Stacked:
                        ListView.CanDelete = true;
                        ListView.PrimaryButtonText = "ROLLOUT";
                        break;
                    case IntegrationStatus.Ready:
                        ListView.CanDelete = true;
                        ListView.PrimaryButtonText = "LAUNCH";
                        break;
                    default:
                        ListView.PrimaryButtonText = "CLOSE";
                        break;
                }
            }
            completeCallback?.Invoke();
        }

    }
}
