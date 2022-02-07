using Sandbox.Definitions;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Session;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace StationFramework.Population
{
    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation)]
    public class PopulationManager: MySessionComponentBase
    {
        public long gridID = 0;
        public bool isStation = false;
        public IMyCubeGrid grid = null;
        public List<PopulationDef> populationDefs = new List<PopulationDef>();


        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            base.Init(sessionComponent);
            ListReader<MyComponentDefinitionBase> entComps = MyDefinitionManager.Static.GetEntityComponentDefinitions();
            foreach (var def in entComps)
            {
                if (def.Id.SubtypeId.String.Contains("[StationFramework]"))
                {
                    
                    string popdef = def.DescriptionString;
                    popdef = popdef.Replace("]", ">");
                    popdef = popdef.Replace("[", "<");
                    PopulationDef pdef = MyAPIGateway.Utilities.SerializeFromXML<PopulationDef>(popdef);
                    if(pdef != null)
                    {
                        MyAPIGateway.Utilities.ShowMessage("SF", "Station Definition: " + pdef.DefinitionName);
                        populationDefs.Add(pdef);
                    }

                }
            }
        }

    }

    [System.Serializable]
    public enum StationFrameworkType
    {
        DEFAULT,
        SHIPDEALER,
        BOUNTY,
        OTHER
    }

    [System.Serializable]
    public enum CultureRole
    {
        Adhocracy,
        Clan,
        Heirarchy,
        Market,
        Purpose,
        Learning,
        Enjoyment,
        Results,
        Authority,
        Safety,
        Order,
        Caring
    }

    [System.Serializable]
    public enum SituationalRole
    {
        Superior,
        Partner,
        Suboridinate
    }

    [System.Serializable]
    public enum SocialRole
    {
        Leader,
        Knowledge,
        Generator,
        Connector,
        Follower,
        Moralist,
        Enforcer,
        Observer
    }

    [System.Serializable]
    public enum PeronalityType
    {
        Architext,
        Logician,
        Commander,
        Debater,
        Advocate,
        Mediator,
        Protagonist,
        Campaigner,
        Logistician,
        Defender,
        Executive,
        Consul,
        Virtuoso,
        Adventurer,
        Entrepreneur,
        Entertainer,
        Animal
    }

    [System.Serializable]
    public class characterBehaviorBase
    {
        public PeronalityType personality = PeronalityType.Debater;
        public SocialRole socialRole = SocialRole.Follower;
        public CultureRole cultureRole = CultureRole.Market;
        public SituationalRole situationalRole = SituationalRole.Suboridinate;

    }

    [System.Serializable]
    public class PersonalityDef
    {
        public List<string> Text = new List<string>();
    }

    [System.Serializable]
    public class PopulationDef
    {
        public string DefinitionName = "Default Station";
        public string FactionContains = "";
        public int PopulationMin = 1;
        public int PopulationMax = 10;
        public StationFrameworkType StationType = StationFrameworkType.DEFAULT;
        public double Morale = 0.5;
        public List<PeronalityType> PeronalityTypes = new List<PeronalityType>();
    }

    [System.Serializable]
    public class PopulationData
    {
        public List<Character> characters = new List<Character>();
    }

    [System.Serializable]
    public class Character
    {
        public string name = "";
        public string description = "";
        public long factionID = 0;
        public long entityID = 0;
        public Vector3D position = Vector3D.Zero;
        

    }


}
