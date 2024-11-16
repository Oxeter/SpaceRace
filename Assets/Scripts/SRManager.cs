using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ModApi.Flight;
using System.Xml.Linq;

using ModApi;
//using ModApi.Common;
using ModApi.Mods;
using ModApi.Flight.UI;
using Assets.Scripts.Flight.Sim;
using ModApi.Flight.Sim;
using Assets.Scripts.Flight;
using Assets.Scripts.Flight.UI;   
using Assets.Scripts.Design;
using Assets.Scripts.Craft.Fuel;
using Assets.Scripts.State; 
using ModApi.Craft;
using ModApi.Ui;
using ModApi.Math;
using ModApi.Scenes.Parameters;
using Assets.Scripts.Craft.Parts.Modifiers.Propulsion;
using ModApi.Design.PartProperties;
using Assets.Scripts.SpaceRace.Projects;
using Assets.Scripts.Craft.Parts;
using Jundroo.ModTools;   
using UnityEngine;
using Assets.Scripts.Career;
using Assets.Scripts.Craft.Parts.Modifiers;
using ModApi.Craft.Parts;
using ModApi.Craft.Propulsion;


using HarmonyLib;
using Assets.Scripts.Ui; 
using Assets.Scripts;
using Assets.Scripts.SpaceRace.Hardware;
using Assets.Scripts.SpaceRace.Modifiers;
using Assets.Scripts.SpaceRace.Collections;
using Assets.Scripts.SpaceRace.Ui;
using Assets.Scripts.Career.Research;
using Assets.Scripts.Menu;
using ModApi.State;
using Assets.Scripts.Menu.ListView;
using UI.Xml;
using ModApi.CelestialData;
using Assets.Scripts.SpaceRace.UI;
using Assets.Scripts.Craft;
using ModApi.Design;
using ModApi.Common.Extensions;
using Assets.Scripts.Career.Contracts;
using Assets.Scripts.Menu.ListView.Career;
using ModApi.Scenes.Events;
using Assets.Scripts.Career.Contracts.Requirements;
using System.Runtime.Remoting.Messaging;


namespace Assets.Scripts.SpaceRace

{


    public partial class SRManager : MonoBehaviour
    {
        public static FuelTankConstruction[] TankConstructions = new FuelTankConstruction[]
        {
            new FuelTankConstruction("Steel Wall", 1500f, 5f, null, true, 2500f),
            new FuelTankConstruction("Aluminium Wall", 1300f, 10f, null, true, 2000f),
            new FuelTankConstruction("Aluminium Alloy Wall", 1800f, 15f, "Fueltank.Alloy", true, 1150f),
            new FuelTankConstruction("Steel Balloon", 750f, 100f),
            new FuelTankConstruction("Aluminium Stringers", 1200f, 25f),
            new FuelTankConstruction("Aluminium Alloy Stringers", 1000f, 30f, "Fueltank.Alloy"),
            new FuelTankConstruction("Aluminium Isogrid", 1000f, 45f),
            new FuelTankConstruction("Aluminium Alloy Isogrid", 850f, 50f, "Fueltank.Alloy")
        };
        public static SRManager Instance {get; private set;}     
        public IProgramManager pm {get; private set;}
        private GameObject _program;
        /// <summary>
        /// Called after a new program manager is loaded.  
        /// This is a good time to register part mods and subscribe to the NewSpaceProgram event to add or modify contractors.
        /// </summary>
        public event SpaceProgramDelgate ProgramManagerLoaded;

        void Awake()
        {
            Instance = this;
            SRProjectsButtons.Initialize();
            ProgramManagerLoaded += OnProgramManagerLoaded;
        }
        private void OnSceneLoading(object sender, SceneEventArgs e)
        {
            pm?.OnSceneLoaded(sender, e);
        }
        private void OnProgramManagerLoaded(IProgramManager pm)
        {
            pm.RegisterPartModData(new SRRocketEngineData(pm));
            pm.RegisterPartModData(new SRCapsuleData(pm));
            pm.RegisterPartModData(new SRFuelTankData(pm));
            pm.RegisterPartModData(new SRRCSData(pm));
            pm.RegisterPartModData(new SRAvionicsData(pm));
        }
        void Start()
        { 
            StartProgramManager();
        }

        public void StartProgramManager(GameState state = null)
        {
            state ??= Game.Instance.GameState;
            if (state.Mode == GameStateMode.Career) 
            {
                _program = new GameObject("Program");
                pm = _program.AddComponent<ProgramManager>();
                DontDestroyOnLoad(_program);
                ProgramManagerLoaded?.Invoke(pm);
            }
        }

        public void KillProgramManager()
        {
            pm?.CloseProgramManager();
            if (_program != null && _program.activeInHierarchy)
            {
                Debug.Log("destroying program object");
                Destroy(_program);
                _program = null;
                Debug.Log("object destroyed");
            }
            pm = null;
        }
        public static bool IsSpaceCenter(ICraftNode node)
        {
            return node.NodeId < 0;
        }


    }

    /// <summary>
    /// Patch of istrackingpayload to return false if the tracker and the payload tracking number are both null.
    /// </summary>
    [HarmonyPatch(typeof(PayloadRequirement), "IsTrackingPayload")] 
    class IsTrackingPayload_Patch
    {
        static bool Prefix(string payloadTrackingId, ref bool __result)
        {
            if (payloadTrackingId==null)
            {
                __result = false;
                return false;
            }
            return true;
        }
        
    }

    /// <summary>
    /// Lets spacerace parts past the career validator
    /// </summary>
    [HarmonyPatch(typeof(TechTree), "IsDesignerPartAvailable")]
    class IsDesignerPartAvailable_Patch
    {
        static bool Prefix(DesignerPart designerPart, ref bool __result)
        {
            XElement part = designerPart.AssemblyElement.Element("Parts").Element("Part");
            if (SRUtilities.IsSpaceRacePart(part) || designerPart.Mod?.ModInfo.Name == "SpaceRace"){
                __result = true;
                //Debug.Log("Passed a SpaceRace part");
                return false;
            };
            return true;
        }
    }

    [HarmonyPatch(typeof(TechTree), "IsPartTypeAvailable")]
    class IsPartTypeAvailable_Patch
    {
        static bool Prefix(PartType partType, ref bool __result)
        {
            if (partType.Mod?.ModInfo.Name == "SpaceRace"){
                __result = true;
                //Debug.Log("Passed a SpaceRace part");
                return false;
            };
            return true;
        }
    }




    [HarmonyPatch(typeof(GameStateManager), "LoadGameState")]
    class LoadGameState_Patch
    {
        static void Postfix(string id, string tag, ref GameState __result)
        {   
            if (__result.Mode == GameStateMode.Career) 
            {
                if (SRManager.Instance.pm == null)
                {
                    SRManager.Instance.StartProgramManager(__result);
                }
                else
                {
                    SRManager.Instance.pm?.Load(__result);
                }
                
            }
            else
            {
                SRManager.Instance.KillProgramManager();
            }
            return;
        }
    }

    [HarmonyPatch(typeof(GameState), "Save")]
    class Save_Patch
    {
        static bool Prefix(GameState __instance)
        {
            if (Game.IsCareer) 
            {
                try
                {
                    SRManager.Instance.pm?.Save(__instance.Id);
                }
                catch
                {
                    Debug.Log("SpaceRace save failed");
                }
            }
            return true;
        }
    }


    [HarmonyPatch(typeof(NavPanelController), "LayoutRebuilt")]
    class NavPanelLayoutRebuilt_Patch
    {
        static bool Prefix(NavPanelController __instance)
        {
            if (Game.IsCareer)
            {
                __instance.xmlLayout.GetElementById(SRProjectsButtons.flightProjectToggleButtonId).AddOnClickEvent(SRManager.Instance.pm.ToggleProgramPanel);
                __instance.xmlLayout.GetElementById(SRProjectsButtons.flightCrewButtonId).AddOnClickEvent(SRManager.Instance.pm.OnClickCrewButton);
                if (ModSettings.Instance.ShowHelp)
                {
                    __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).AddOnClickEvent(SRManager.Instance.pm.Tutorial.ShowMessage);
                }
                else
                {
                    __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).SetAndApplyAttribute("active", "false");
                }
            }
            else 
            {
                __instance.xmlLayout.GetElementById(SRProjectsButtons.flightProjectToggleButtonId).SetAndApplyAttribute("active", "false");
                __instance.xmlLayout.GetElementById(SRProjectsButtons.flightCrewButtonId).SetAndApplyAttribute("active", "false");
                __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).SetAndApplyAttribute("active", "false");
            }
            return true;
        }
    }
    [HarmonyPatch(typeof(DesignerUiController), "LayoutRebuilt")]
    class DesignerLayoutRebuilt_Patch
    {
        static bool Prefix(DesignerUiController __instance)
        {
            if (Game.IsCareer)
            {
                __instance.xmlLayout.GetElementById(SRProjectsButtons.designProjectToggleButtonId).AddOnClickEvent(SRManager.Instance.pm.ToggleProgramPanel);
                if (ModSettings.Instance.ShowHelp)
                {
                    __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).AddOnClickEvent(SRManager.Instance.pm.Tutorial.ShowMessage);
                }
                else
                {
                    __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).SetAndApplyAttribute("active", "false");
                }
            }
            else 
            {
                __instance.xmlLayout.GetElementById(SRProjectsButtons.designProjectToggleButtonId).SetAndApplyAttribute("active", "false");
                __instance.xmlLayout.GetElementById(SRProjectsButtons.helpButtonId).SetAndApplyAttribute("active", "false");
            }

            return true;
        }
    }

    /// <summary>
    /// Remove the IsPimped check on engines.  All our engines are pimped B)
    /// </summary>
    [HarmonyPatch(typeof(RocketEngineScript), "ValidatePart")]
    class DesignerValidatePart_Patch
    {
        static bool Prefix(ModApi.Scripts.State.Validation.ValidationResult result, RocketEngineScript __instance)
        {   
            return true;
        }

        static void Postfix(ModApi.Scripts.State.Validation.ValidationResult result, RocketEngineScript __instance)
        {
            ModApi.Scripts.State.Validation.ValidationMessage message = result.Messages.Find(mes => mes.Message == "One or more XML edits have been detected.");
            if (message != null) result.Messages.Remove(message);
        }

    }

    /// <summary>
    /// Allows the NewGameDialogScript to load a custom planetary system when starting a career game.
    /// </summary>
    [HarmonyPatch(typeof(NewGameDialogScript), "GetPlanetarySystemFileReference")]
    class GetPlanetarySystemFileReference_Patch
    {
        static bool Prefix(ref string careerFolder, NewGameDialogScript __instance)
        {
            careerFolder = null;
            return true;
        }
    }

    /// <summary>
    /// Allows the NewGameDialogScript to retain knowledge of the selected planetary system when switching to career mode.
    /// </summary>
    [HarmonyPatch(typeof(NewGameDialogScript), "SetDefaultPlanetarySystem")]
    class SetDefaultPlanetarySystem_Patch
    {
        static bool Prefix(NewGameDialogScript __instance)
        {
            return false;
        }
    }

    [HarmonyPatch(typeof(NewGameDialogScript), "OnOkayButtonClicked")]
    class OnOkayButtonClicked_Patch
    {
        static bool Prefix(NewGameDialogScript __instance)
        {
            SRManager.Instance.KillProgramManager();
            return true;
        }
        static void Postfix(NewGameDialogScript __instance)
        {
            SRManager.Instance.StartProgramManager();
        }
    }



    [HarmonyPatch(typeof(LoadGameViewModel), "LoadGame")]
    class LoadGameViewModel_Patch
    {
        static bool Prefix(LoadGameViewModel __instance)
        {
            SRManager.Instance.KillProgramManager();
            return true;
        }
        static void Postfix(LoadGameViewModel __instance)
        {
            SRManager.Instance.StartProgramManager();
        }
    }



    /// <summary>
    /// Forces the game into simulation mode while in designer
    /// </summary>
    [HarmonyPatch(typeof(DesignerUiController), "OnPlayButtonClicked")]
    class DesignerScriptStart_Patch
    {
        static void Postfix()
        {
            GameState gameState = Game.Instance.GameState;
            SRManager.Instance.pm.LastCraftConfig = new SRCraftConfig(SRManager.Instance.pm.GetVerboseCraftConfig(Game.Instance.Designer.CraftScript.PrimaryCommandPod), "designer");
            if (gameState.Type != GameStateType.Simulation && gameState.Mode == GameStateMode.Career)
            {
                GameStateManager gsm = Game.Instance.GameStateManager;
                CelestialFileReference cfs = gameState.LoadFlightStateData().PlanetarySystemFileReference;
                string id = gameState.Id;
                string gameStateTag = "Simulation.Active";
                string gameStateTagPath = gsm.GetGameStateTagPath(id);
                gsm.CopyGameStateTagFromDirectory(id, gameStateTagPath, "Simulation.Active");
                GameState newGameState = new GameState(id, gsm.GetGameStateTagPath(id, "Simulation.Active"));
                newGameState.Type = GameStateType.Simulation;
                newGameState.Career.TechTree.GetNode("undo").Researched = true;
                newGameState.Career.TechTree.GetNode("cheater").Researched = true;
                //CelestialDatabase celestialDatabase = Game.Instance.CelestialDatabase;
                FlightStateData flightStateData = newGameState.LoadFlightStateData();
                flightStateData.ChangePlanetarySystem(cfs);
                flightStateData.Save();
                newGameState.Save();
                Game.Instance.LoadGameStateOrDefault(id, gameStateTag);
                Debug.Log("Entering Simulation GameState");
            }
        }
    }
    /// <summary>
    /// Extend exit flight scene to accept the tech tree as a destination.  Basically a rewrite of the method for that case.
    /// </summary>
    [HarmonyPatch(typeof(FlightSceneScript), "ExitFlightScene")]
    public class ExitFlightScene_Patch
    {
        static bool Prefix(FlightSceneScript __instance, bool saveFlightState, FlightSceneExitReason exitReason, string sceneName)
        {
            if (sceneName == "TechTree")
            {
                //These traverses don't seem to work, but I don't know that it matters for gameplay.
                //Traverse.Create(__instance).Field("_saveFlightStateOnExit").SetValue(saveFlightState); 
                //Traverse.Create(__instance).Field("_flightSceneExitReason").SetValue(exitReason);
                FlightSceneScript.Instance.FlightState.Save();
                Game.Instance.GameState.Save();
                Time.timeScale = 1f;
                Game.Instance.SceneManager.LoadTechTree();
                return false;
            }
            return true;
        }
    }
    /// <summary>
    /// Substitutes a rewritten EndFlightDialog script.
    /// </summary>
    [HarmonyPatch(typeof(FlightSceneUiController), "OnExitButtonClicked")]
    public class FlightSceneOnExitButtonClicked_Patch
    {
        static bool Prefix()
        {
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
            return false;
        }
    }

    /// <summary>
    /// Hides the navball if we load into the space center.
    /// </summary>
    [HarmonyPatch(typeof(FlightSceneInterfaceScript), "Start")]
    class Start_Patch
    {
        static void Postfix()
        {
            if (SRManager.IsSpaceCenter(Game.Instance.FlightScene.CraftNode))
            {
                Game.Instance.FlightScene.FlightSceneUI.SetNavSphereVisibility(false, false);
            }
        }
    }

    /// <summary>
    /// Prevents the navball from being made visible during warp.  This is because SRAvionics shuts down the navsphere for craft that cannot maneuver and we don't want players turning it back on.
    /// </summary>
    [HarmonyPatch(typeof(FlightSceneInterfaceScript), "SetNavSphereVisibility")]
    class SetNavSphereVisibility_Patch
    {
        static bool Prefix(bool visible, bool updateSettings)
        {
            if (visible && Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Exit warp before enabiling NavSphere");
                return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Prevents the craft from locking onto one of the heading indicators during warp.  
    /// Free maneuver during warp is limiting to holding to a preselected indicator.
    /// </summary>
    [HarmonyPatch(typeof(NavSphereScript), "LockedIndicator", MethodType.Setter)]
    class LockedIndicator_Patch
    {
        static bool Prefix(object value)
        {
            if (value != null && Game.Instance.FlightScene.TimeManager.CurrentMode.WarpMode)
            {
                Game.Instance.FlightScene.FlightSceneUI.ShowMessage("Exit warp before setting a new heading");
                return false;
            }
            return true;
        }
    }



    /// <summary>
    /// Calls a method in the program manager when contracts are accepted.  
    /// At time of writing, this adds funding and may trigger historical events or construction projects.
    /// </summary>
    [HarmonyPatch(typeof(ContractContext), "AcceptContract")]
    class AcceptContract_Patch
    {
        static void Postfix(Contract contract, double currentTime)
        {
            SRManager.Instance.pm.OnContractAccepted(contract);
        }
    }

    /// <summary>
    /// Calls a method in the program manager when contracts are added to the context.  
    /// At time of writing, this checks for whether a historical event blocks the contract.
    /// </summary>
    [HarmonyPatch(typeof(ContractContext), "AddNewContract")]
    class AddNewContract_Patch
    {
        static void Prefix(ref Contract contract)
        {
            SRManager.Instance.pm.OnNewContract(contract);
        }
    }

    /// <summary>
    /// Calls a method in the program manager when contracts are cancelled.  
    /// At time of writing this is to cancel the corresponding construction project and corresponding funding.
    /// </summary>
    [HarmonyPatch(typeof(ContractContext), "CancelContract")]
    class CancelContract_Patch
    {
        static void Postfix(Contract contract, FlightStateData flightStateData)
        {
            SRManager.Instance.pm.OnContractCancelled(contract);
        }
    } 

    [HarmonyPatch(typeof(ContractContext), "CloseContract")]
    class CloseContract_Patch
    {
        static void Postfix(Contract contract, FlightStateData flightStateData)
        {
            if (contract.Status == ContractStatus.Complete)
            {
                SRManager.Instance.pm.OnContractCompleted(contract);
            }
            if (contract.Status == ContractStatus.Terminated)
            {
                SRManager.Instance.pm.OnContractCancelled(contract);
            }
        }
    }

    /// <summary>
    /// Makes ablative heatshields remove heat as they ablate (which is the whole point of ablative heatshields)
    /// Absolutely necessary for reentries at real solar system speeds.
    /// </summary>
    [HarmonyPatch(typeof(BodyScript), "UpdatePartTemperatures")]
    class UpdatePartTemperatures_Patch
    {
        static void Postfix(BodyScript __instance)
        {
            foreach (PartData part in __instance.Data.Parts)
            {
                PartScript partScript = (PartScript)part.PartScript;
                if (partScript.Temperature > part.Config.MaxTemperature && part.Config.HeatShield >0f)
                {
                    partScript.Temperature += ModSettings.Instance.AblatorEffectiveness *(part.Config.MaxTemperature - partScript.Temperature);
                }
            }
        }
    }

    /// <summary>
    /// Update SRRocketenginedata when changes are made to Rocketenginedata.
    /// </summary>
    [HarmonyPatch(typeof(RocketEngineData), "UpdateAndSyncComponents")]
    class UpdateAndSyncComponents_Patch
    {
        static void Postfix(RocketEngineData __instance, bool refreshTextureStyles, IDesignerPartPropertiesModifierInterface d = null)
        {
            __instance.Part.GetModifier<SRRocketEngineData>().UpdateInfo(ProgramManager.Instance);
        }
    }

    /// <summary>
    /// Update SRCapsuledata when changes are made to CommandPodData.
    /// </summary>
    [HarmonyPatch(typeof(CommandPodData), "UpdateOtherModifiersAndStuff")]
    class UpdateOtherModifiersAndStuff_Patch
    {
        static void Postfix(CommandPodData __instance)
        {
            SRCapsuleData modifier =  __instance.Part.GetModifier<SRCapsuleData>();
            if (modifier != null)
            {
                modifier.UpdateOtherModifiersAndStuff();
            }
        }
    }


    /// <summary>
    /// An attempt at creating fuel sources for alternate fuel types.  Didn't work well.
    /// </summary>
    //[HarmonyPatch(typeof(CraftFuelSources), "CreateFuelSourceForConnectedParts")]
    class CreateFuelSourceForConnectedParts_Patch
    {
        static void Postfix(CraftFuelSources __instance, IEnumerable<PartData> parts, bool removeDisconnectedCrossFeeds, List<CraftFuelSource> fuelSources)
        {
            Debug.Log("CreateFuelSourcePatch Firing");
            foreach (PartData part in parts)
            {
                FuelTankData ft = part.GetModifier<FuelTankData>();
                if (ft != null)
                {
                    CraftFuelSource craftFuelSource = null;
                    if (ft.FuelType.Id == "HP Nitrogen")
                    {
                        craftFuelSource = ft.Part.CommandPod?.GetModifier<SRCommandPodData>().Script.HPNitrogenFuelSource as CraftFuelSource;
                    }
                    if (ft.FuelType.Id == "HP Monopropellant")
                    {
                        craftFuelSource = ft.Part.CommandPod?.GetModifier<SRCommandPodData>().Script.HPMonoFuelSource as CraftFuelSource;
                    }
                    if (ft.FuelType.Id == "HP MMH/NTO")
                    {
                        craftFuelSource = ft.Part.CommandPod?.GetModifier<SRCommandPodData>().Script.HPMMHFuelSource as CraftFuelSource;
                    }
                    if (craftFuelSource != null)
                    {
                        craftFuelSource.AddFuelTank(ft.Script);
                    }
                }
            }
        }
    }
    //[HarmonyPatch(typeof(CraftFuelSources), "Rebuild")]
    class Rebuild_Patch
    {
        static void Postfix(CraftFuelSources __instance, ICraftScript craftScript, ref List<CraftFuelSource> ____fuelSources, ref IFuelTransferManager ____fuelTransferManager)
        {
            Debug.Log("Rebuild Patch Firing");
            foreach (ICommandPod commandPod in craftScript.CommandPods)
            {
                SRCommandPodScript srcommandPodScript = commandPod.Part.GetModifier<SRCommandPodData>().Script;
                FuelType nit = Game.Instance.PropulsionData.Fuels.First(x => x.Id == "HP Nitrogen");
                FuelType mon = Game.Instance.PropulsionData.Fuels.First(x => x.Id == "HP Monopropellant");
                FuelType mmh = Game.Instance.PropulsionData.Fuels.First(x => x.Id == "HP MMHNTO");
                CraftFuelSource craftFuelSource = new CraftFuelSource(____fuelTransferManager, ____fuelSources.Count, nit);
                craftFuelSource.ReverseSubPriority = true;
                ____fuelSources.Add(craftFuelSource);
                srcommandPodScript.HPNitrogenFuelSource = craftFuelSource;
                CraftFuelSource craftFuelSource2 = new CraftFuelSource(____fuelTransferManager, ____fuelSources.Count, mon);
                craftFuelSource2.ReverseSubPriority = true;
                ____fuelSources.Add(craftFuelSource2);
                srcommandPodScript.HPMonoFuelSource = craftFuelSource2;
                CraftFuelSource craftFuelSource3 = new CraftFuelSource(____fuelTransferManager, ____fuelSources.Count, mmh);
                craftFuelSource3.ReverseSubPriority = true;
                ____fuelSources.Add(craftFuelSource3);
                srcommandPodScript.HPMMHFuelSource = craftFuelSource3;
            }
        }
    }
    
    //[HarmonyPatch(typeof(ReactionControlNozzleScript), "OnCraftStructureChanged")]
    class OnCraftStructureChanged_Patch
    {
        static void Postfix(ReactionControlNozzleScript __instance, ref IFuelSource ____fuelSource, ICraftScript craftScript)
        {
            FuelType fuel = __instance.Part.GetModifier<SRRCSData>().Fuel;
            if (__instance.PartScript.CommandPod != null)
            {
                if (fuel.Id == "HP Nitrogen")
                ____fuelSource = __instance.Part.CommandPod.GetModifier<SRCommandPodData>().Script.HPNitrogenFuelSource;
                if (fuel.Id == "HP Monopropellant")
                ____fuelSource = __instance.Part.CommandPod.GetModifier<SRCommandPodData>().Script.HPMonoFuelSource;
                if (fuel.Id == "HP MMHNTO")
                ____fuelSource = __instance.Part.CommandPod.GetModifier<SRCommandPodData>().Script.HPMMHFuelSource;
            }
            else
            {
                ____fuelSource = EmptyFuelSource.GetOrCreate(FuelType.Monopropellant);
            }
        }
    }




}