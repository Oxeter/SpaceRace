namespace Assets.Scripts.SpaceRace.Ui
{
    using ModApi.Ui.Inspector;
    using UnityEngine;
    using SpaceRace;
    using SpaceRace.Projects;
    using SpaceRace.Collections;
    using ModApi.Math;
    using ModApi;
    //using ModApi;

    public class ProjectsInspectorScript : MonoBehaviour
    {
    //
        // Summary:
        //     The inspector panel
        private IInspectorPanel _inspectorPanel;

        //
        // Summary:
        //     The view model
        //private FlightInspectorViewModel _viewModel;

        //
        // Summary:
        //     The visible flag.
        private bool _visible;


        public bool Visible
        {
            get
            {
                return _visible;
            }
            set
            {
                
                Game.Instance.Settings.Game.Flight.ShowFlightViewInspector.UpdateAndCommit(value);
                _visible = value;
            }
        }

        public static ProjectsInspectorScript Create()
        {
            GameObject gameObject = new GameObject("ProjectInspector");
            gameObject.transform.SetParent(Game.Instance.UserInterface.Transform, worldPositionStays: false);
            return gameObject.AddComponent<ProjectsInspectorScript>();
        }

        public void ShowMessage(string message)
        {
            if (Game.InFlightScene)
            Game.Instance.FlightScene.FlightSceneUI.ShowMessage(message);
            else if (Game.InDesignerScene)
            Game.Instance.Designer.DesignerUi.ShowMessage(message);
        }

        protected virtual void Start()
        {
            InspectorModel inspectorModel = new InspectorModel("FlightView", "Flight Info");
            //_viewModel = new ProjectsInspectorViewModel();

            //foreach (Project proj in SRManager.Instance.ProjectDB.Developments)
            {
            //    GroupModel groupModel = new GroupModel(proj.Name);
           //     groupModel.Add(new TextModel("Battery", () => Units.GetPercentageString(_viewModel.FuelBatteryPercentage)));
            }

            InspectorPanelCreationInfo inspectorPanelCreationInfo = new InspectorPanelCreationInfo()
            {
                StartPosition = InspectorPanelCreationInfo.InspectorStartPosition.UpperRight,
                StartOffset = new Vector2(-170f, -90f),
                Resizable = !Device.IsMobileBuild
            };

            _inspectorPanel = Game.Instance.UserInterface.CreateInspectorPanel(inspectorModel, inspectorPanelCreationInfo);
            _inspectorPanel.CloseButtonClicked += delegate
            {
                Visible = false;
            };
        }
    }

}

