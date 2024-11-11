namespace Assets.Scripts.SpaceRace.Projects
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using ModApi.Common.UI;
    using ModApi.Ui.Inspector;
    using UnityEngine;
    using UnityEngine.PlayerLoop;

    public interface IProjectScript<T> : IProgramChild where T : ProjectData
    {
        public T Data {get;}
    }



    public interface IHardwareDevelopmentScript : IProjectScript<PartDevelopmentData>
    {

    }

    public interface IStageDevelopmentScript : IProjectScript<StageDevelopmentData>
    {


    }

    public interface IConstructionScript : IProjectScript<ConstructionData>
    {


    }
}