using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Craft;
using Assets.Scripts.Craft.Parts.Modifiers.Eva;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.Sim;
using Assets.Scripts.Flight.UI;
using Assets.Scripts.State;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Planet;
using ModApi.Scripts.State;
using ModApi.Ui;
using ModApi.Flight;
using UnityEngine;
using Assets.Scripts.Menu.ListView;
using ModApi.CelestialData;
using HarmonyLib;

namespace Assets.Scripts.SpaceRace.Ui
{

    //
    // Summary:
    //     View model class for the active crafts list view.
    public class SimpleActiveCraftsViewModel : ListViewModel
    {
        //
        // Summary:
        //     The craft data
        private CraftData _craftData;

        //
        // Summary:
        //     The craft script
        private CraftScript _craftScript;

        //
        // Summary:
        //     The crew members
        private List<CrewMember> _crewMembers = new List<CrewMember>();

        //
        // Summary:
        //     The details.
        private ActiveCraftsDetails _details;

        //
        // Summary:
        //     The flight state data
        private FlightStateData _flightStateData;

        //
        // Summary:
        //     The planet nodes
        private List<PlanetNode> _planetNodes;

        //
        // Summary:
        //     The solar system data.
        private SolarSystemDataScript _solarSystemData;

        //
        // Summary:
        //     Initializes a new instance of the Assets.Scripts.Menu.ListView.ActiveCraftsViewModel
        //     class.
        //
        // Parameters:
        //   flightStateData:
        //     The flight state data.
        //
        //   solarSystemData:
        //     The solar system data.
        //
        //   planetNodes:
        //     The planet nodes.
        public SimpleActiveCraftsViewModel(FlightStateData flightStateData, SolarSystemDataScript solarSystemData, List<PlanetNode> planetNodes)
        {
            _flightStateData = flightStateData;
            _solarSystemData = solarSystemData;
            _planetNodes = planetNodes;
        }

        public SimpleActiveCraftsViewModel()
        {
            _flightStateData = Traverse.Create(Game.Instance.FlightScene.FlightState).Field("_data").GetValue<FlightStateData>();
            CelestialDatabase celestialDatabase = Game.Instance.CelestialDatabase;
            PlanetarySystemFileData planetarySystem = _flightStateData.PlanetarySystem;
            CelestialFile file = celestialDatabase.GetFile(_flightStateData.PlanetarySystemFileReference);
            if (planetarySystem == null || file == null)
            {
                Debug.LogError("The current flight state's planetary system could not be found.");
            }
            else if (celestialDatabase.IsMissingFiles(planetarySystem))
            {
                Debug.LogError("The current flight state's planetary system '" + planetarySystem.Name + "' (" + planetarySystem.FileId.ToString() + ") is missing required files.");
                celestialDatabase.LogMissingFiles(CelestialFileType.PlanetarySystem, _flightStateData.PlanetarySystemFileReference);
            }
            else
            {
                _solarSystemData = SolarSystemDataScript.CreateFromFile(file, createTerrainData: false, applyScaleAndOverrides: true);
            }
            _planetNodes = FlightState.LoadPlanetNodes(_flightStateData, _solarSystemData, includeLockedPlanets: true);
        }

        //
        // Summary:
        //     Loads the items.
        //
        // Returns:
        //     Unity enumerator so the view model can optionally split the loading up over multiple
        //     frames.
        public override IEnumerator LoadItems()
        {
            _details = new ActiveCraftsDetails(base.ListView.ListViewDetails);
            List<ICraftNodeData> craftNodes = new List<ICraftNodeData>();
            foreach (ICraftNodeData craftNodeXml2 in _flightStateData.CraftNodes)
            {
                craftNodes.Add(craftNodeXml2);
            }

            List<ICraftNodeData> sortedNodes = craftNodes.OrderBy((ICraftNodeData x) => x.Name).ToList();
            foreach (ICraftNodeData craftNodeXml in sortedNodes)
            {
                base.ListView.CreateItem(craftNodeXml.Name, craftNodeXml.ParentName, craftNodeXml, null, ListViewScript.SpriteLoadLocation.Resources);
            }

            yield return new WaitForEndOfFrame();
        }

        //
        // Summary:
        //     Called when the delete button is clicked.
        //
        // Parameters:
        //   selectedItem:
        //     The selected item.
        public override void OnDeleteButtonClicked(ListViewItemScript selectedItem)
        {
            if (_craftScript == null)
            {
                DeleteNodeItem(selectedItem, saveGameState: true);
                return;
            }

            ICraftNodeData craftNodeData = selectedItem.ItemModel as ICraftNodeData;
            PlanetNode planet = _planetNodes.Where((PlanetNode x) => x.Name == craftNodeData.ParentName).First();
            CraftRecovery recovery = new CraftRecovery(Game.Instance.GameState, _craftScript.Data, _craftScript.Mass, craftNodeData, planet);
            RecoverCraftDialogScript recoverCraftDialogScript = RecoverCraftDialogScript.Create(recovery);
            recoverCraftDialogScript.CraftDestroyed = delegate
            {
                DeleteNodeItem(selectedItem, saveGameState: true);
            };
            recoverCraftDialogScript.CraftRecovered = delegate
            {
                DeleteNodeItem(selectedItem, saveGameState: true);
            };
        }

        //
        // Summary:
        //     Called when the ListView is initialized.
        //
        // Parameters:
        //   listView:
        //     The list view.
        public override void OnListViewInitialized(ListViewScript listView)
        {
            base.OnListViewInitialized(listView);
            listView.DisplayType = ListViewScript.ListViewDisplayType.SmallDialog;
            listView.Title = "Resume Flight";
            listView.CanDelete = true;
            listView.PrimaryButtonText = "RESUME";
            listView.CreateContextMenuItem("Remove All Debris", OnRemoveAllDebris, string.Empty);
        }

        //
        // Summary:
        //     Called when the primary button is clicked.
        //
        // Parameters:
        //   selectedItem:
        //     The selected item.
        public override void OnPrimaryButtonClicked(ListViewItemScript selectedItem)
        {
            if (_details.IsResumable)
            {
                IFlightScene flightSceneScript = Game.Instance.FlightScene;
                ICraftNodeData craftNodeData = selectedItem.ItemModel as ICraftNodeData;
                ICraftNode node = flightSceneScript.FlightState.CraftNodes.First(cr=> cr.NodeId == craftNodeData.NodeId);
                if (node.CraftScript != null && flightSceneScript.ChangePlayersActiveCommandPodImmediate(node.CraftScript.ActiveCommandPod, node))
                {
                    Game.Instance.GameState.Save();
                    Game.Instance.GameStateManager.CopyGameStateTag(Game.Instance.GameState.Id, "Active", "Preflight");
                    return;
                }
                flightSceneScript.ChangePlayersActiveCraftNode(node);
                Debug.Log("Reloading scene to switch craft.");
                return;
            }
            else if (!_details.IsPlayerAllowed)
            {
                MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog();
                messageDialogScript.OkayButtonText = "OK";
                ICraftNodeData craftNodeData2 = selectedItem.ItemModel as ICraftNodeData;
                messageDialogScript.MessageText = "You are not allowed to take control of " + craftNodeData2.Name + " from this menu.";
                messageDialogScript.UseDangerButtonStyle = false;
            }
            else
            {
                MessageDialogScript messageDialogScript2 = Game.Instance.UserInterface.CreateMessageDialog();
                messageDialogScript2.OkayButtonText = "OK";
                ICraftNodeData craftNodeData3 = selectedItem.ItemModel as ICraftNodeData;
                messageDialogScript2.MessageText = craftNodeData3.Name + " is not resumable because it does not have a control unit.";
                messageDialogScript2.UseDangerButtonStyle = false;
            }
        }

        //
        // Summary:
        //     Called when the view model should update the details for the selected item.
        //
        // Parameters:
        //   item:
        //     The item.
        //
        //   completeCallback:
        //     The callback to invoke when the action is complete.
        public override void UpdateDetails(ListViewItemScript item, Action completeCallback)
        {
            if (item != null)
            {
                ICraftNodeData craftNodeData = item.ItemModel as ICraftNodeData;
                _details.UpdateDetails(craftNodeData, _solarSystemData);
                if (_details.IsResumable)
                {
                    base.ListView.PrimaryButtonText = "RESUME";
                }
                else
                {
                    base.ListView.PrimaryButtonText = "NOT RESUMABLE";
                }
            }

            completeCallback?.Invoke();
        }

        //
        // Summary:
        //     Determines whether the specified item matches the search text.
        //
        // Parameters:
        //   item:
        //     The item to test.
        //
        //   searchTextLower:
        //     The search text in lower case format.
        //
        // Returns:
        //     true if the item matches the search text, false otherwise.
        protected override bool MatchesSearchCriteria(ListViewItemScript item, string searchTextLower)
        {
            ICraftNodeData craftNodeData = (ICraftNodeData)item.ItemModel;
            return craftNodeData.Name.ToLower().Contains(searchTextLower) || craftNodeData.ParentName.ToLower().Contains(searchTextLower);
        }

        //
        // Summary:
        //     Deletes the craft node.
        //
        // Parameters:
        //   flightStateData:
        //     The flight state data.
        //
        //   craftNodeData:
        //     The craft node data.
        private static void DeleteCraftNode(FlightStateData flightStateData, ICraftNodeData craftNodeData)
        {
            try
            {
                XElement craftXml = flightStateData.LoadCraftXml(craftNodeData.NodeId);
                CraftData craftData = Game.Instance.CraftLoader.LoadCraftImmediate(craftXml);
                List<CrewMember> crewMembers = GetCrewMembers(craftData);
                if (crewMembers.Count > 0)
                {
                    foreach (CrewMember item in crewMembers)
                    {
                        if (item.State == CrewMemberState.InFlight)
                        {
                            item.State = CrewMemberState.Deceased;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            flightStateData.RemoveCraftNode(craftNodeData);
            flightStateData.Save();
        }

        //
        // Summary:
        //     Gets the crew members.
        //
        // Parameters:
        //   craftData:
        //     The craft data.
        //
        // Returns:
        //     The list of crew members.
        private static List<CrewMember> GetCrewMembers(CraftData craftData)
        {
            List<CrewMember> list = new List<CrewMember>();
            foreach (PartData part in craftData.Assembly.Parts)
            {
                EvaData modifier = part.GetModifier<EvaData>();
                if (modifier?.CrewMember != null)
                {
                    list.Add(modifier.CrewMember);
                }
            }

            return list;
        }

        //
        // Summary:
        //     Called when the user confirms deleting a craft node.
        //
        // Parameters:
        //   item:
        //     The item.
        //
        //   saveGameState:
        //     If set to true, the game state will be saved.
        private void DeleteNodeItem(ListViewItemScript item, bool saveGameState)
        {
            DeleteCraftNode(_flightStateData, item.ItemModel as ICraftNodeData);
            _crewMembers.Clear();
            _craftData = null;
            _craftScript = null;
            base.ListView.DeleteItem(item);
            Items.Remove(item);
            base.ListView.SelectedItem = null;
            if (saveGameState)
            {
                Game.Instance.GameState.Save();
            }
        }

        //
        // Summary:
        //     Called when the remove all debris menu item is clicked.
        //
        // Parameters:
        //   contextMenuItem:
        //     The context menu item.
        private void OnRemoveAllDebris(ContextMenuItemScript contextMenuItem)
        {
            List<ListViewItemScript> debris = new List<ListViewItemScript>();
            foreach (ListViewItemScript item in Items)
            {
                ICraftNodeData craftNodeData = item.ItemModel as ICraftNodeData;
                if ((!craftNodeData.HasCommandPod && craftNodeData.ContractTrackingId == null) || craftNodeData.CraftPartCount == 0)
                {
                    debris.Add(item);
                }
            }

            if (debris.Count > 0)
            {
                MessageDialogScript messageDialogScript = Game.Instance.UserInterface.CreateMessageDialog(MessageDialogType.OkayCancel);
                messageDialogScript.MessageText = $"There are {debris.Count} non-resumable craft(s). Confirm that you wish to remove them.";
                messageDialogScript.OkayClicked += delegate (MessageDialogScript d)
                {
                    d.Close();
                    foreach (ListViewItemScript item2 in debris)
                    {
                        DeleteNodeItem(item2, saveGameState: false);
                    }

                    Game.Instance.GameState.Save();
                };
            }
            else
            {
                MessageDialogScript messageDialogScript2 = Game.Instance.UserInterface.CreateMessageDialog();
                messageDialogScript2.MessageText = "There is no more debris to remove.";
            }
        }
    }
}