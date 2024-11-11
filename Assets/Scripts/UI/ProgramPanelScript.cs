namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Linq;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using System.IO;
    using UnityEngine;

    using ModApi.Craft.Parts;
    using ModApi.Common.Extensions;
    using ModApi.Flight;
    using ModApi.Scenes.Events;
    using ModApi.Flight.Events;
    using Assets.Scripts.SpaceRace.Hardware;
    using ModApi.Ui.Inspector;
    using Assets.Scripts.SpaceRace.Collections;
    using Assets.Scripts.SpaceRace.Modifiers;
    using Assets.Scripts.SpaceRace.Formulas;
    using Assets.Scripts.Craft;
    using Assets.Scripts.Career.Contracts.Requirements;
    using ModApi.Craft;
    using Assets.Scripts.Design.Staging;
    using Assets.Scripts.Craft.Parts;
    /// <summary>
    /// currently defunct
    /// </summary>
    public class ProgramPanelScript
    {
        private IProgramManager _pm;
        private IInspectorPanel _panel;
        private InspectorModel _inspectorModel;
        public Dictionary<int,GroupModel> InspectorGroups = new Dictionary<int, GroupModel>();

        public void Inititalize()
        {

        }
        public void ToggleProjectPanel()
        {
            _panel.Visible = !_panel.Visible;
        }
    }
}