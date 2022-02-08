using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace StationFramework
{
    public enum StationTileType
    {

        //Actual Tile Types
        Empty, ERROR, Corridor, Spawn, Entrance, Entrance2, Room, Room2, Stairs,
        Staircase, Ending, Mobspawn, EntranceCloset, RoomCloset, BossRoom, EntranceBoss,
        CFEntrance,

        //Global Type Overrides
        NOREQUIREMENT, ANYROOM, WALKABLE, ANYENTRANCE, NOTLOADED, NONEMPTY,

        //New types
        SecretCorridor, SecretEntrance, BossRaised, BossStairs,

        WALKABLE_NOT_ROOM, WALKABLE_NOT_CORRIDOR, WALKABLE_NOT_ENTRANCE, WALKABLE_NOT_CORRIDORSTAIRS,

        //Boss / Special room types
        BossBridge, SpikeFloor, Room_RaisedFloor, Room_RaisedStairs, Room_Stairs,

        //New overrides
        WALKABLE_RAISED, ANYSTAIRS, ANYROOM_STAIRS, ANYROOM_NOT_RAISED, WALKABLE_NOT_SECRET, ANYENTRANCE_NOT_SECRET, WALKABLE_SECRET,

        //NOT Types ( Can be replaced with another feature )
        NOT_SECRET, NOT_WALKABLE, NOT_ROOM,

        //Custom types
        ToiletRoom, BedRoom

    }

    public static class StationTileTypeUtils
    {
        public static bool IsTileTypeMatching(StationTileType requirement, StationTileType tile)
        {
            bool requirementsMet = false;
            if (requirement == StationTileType.ANYROOM) requirementsMet = (tile == StationTileType.ToiletRoom || tile == StationTileType.Room || tile == StationTileType.Room2 || tile == StationTileType.BossRoom || tile == StationTileType.BossBridge || tile == StationTileType.BossStairs || tile == StationTileType.Room_RaisedFloor || tile == StationTileType.Room_RaisedStairs || tile == StationTileType.Room_Stairs);
            else if (requirement == StationTileType.ANYENTRANCE) requirementsMet = (tile == StationTileType.Entrance || tile == StationTileType.Entrance2 || tile == StationTileType.EntranceBoss || tile == StationTileType.SecretEntrance);
            else if (requirement == StationTileType.ANYENTRANCE_NOT_SECRET) requirementsMet = (tile == StationTileType.Entrance || tile == StationTileType.Entrance2 || tile == StationTileType.EntranceBoss);
            else if (requirement == StationTileType.ANYSTAIRS) requirementsMet = (tile == StationTileType.Stairs || tile == StationTileType.BossStairs || tile == StationTileType.Room_Stairs || tile == StationTileType.Room_RaisedStairs);
            else if (requirement == StationTileType.ANYROOM_STAIRS) requirementsMet = (tile == StationTileType.BossStairs || tile == StationTileType.Room_Stairs || tile == StationTileType.Room_RaisedStairs);
            else if (requirement == StationTileType.ANYROOM_NOT_RAISED) requirementsMet = (tile == StationTileType.BossStairs || tile == StationTileType.Room_Stairs || tile == StationTileType.Room || tile == StationTileType.Room2 || tile == StationTileType.BossRoom);
            else if (requirement == StationTileType.NONEMPTY) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR);
            else if (requirement == StationTileType.WALKABLE) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised);
            else if (requirement == StationTileType.WALKABLE_RAISED) requirementsMet = (tile == StationTileType.BossRaised || tile == StationTileType.Room_RaisedFloor || tile == StationTileType.Room_RaisedStairs || tile == StationTileType.BossBridge);
            else if (requirement == StationTileType.WALKABLE_NOT_ROOM) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised || tile == StationTileType.Room || tile == StationTileType.Room2 || tile == StationTileType.BossRoom || tile == StationTileType.BossBridge || tile == StationTileType.BossStairs || tile == StationTileType.Room_RaisedFloor || tile == StationTileType.Room_RaisedStairs || tile == StationTileType.BossRaised || tile == StationTileType.Room_Stairs);
            else if (requirement == StationTileType.WALKABLE_NOT_CORRIDOR) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised || tile == StationTileType.Corridor || tile == StationTileType.SecretCorridor);
            else if (requirement == StationTileType.WALKABLE_NOT_CORRIDORSTAIRS) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised || tile == StationTileType.Corridor || tile == StationTileType.SecretCorridor || tile == StationTileType.Stairs || tile == StationTileType.Staircase);
            else if (requirement == StationTileType.WALKABLE_NOT_ENTRANCE) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised || tile == StationTileType.Entrance || tile == StationTileType.Entrance2 || tile == StationTileType.EntranceBoss);
            else if (requirement == StationTileType.WALKABLE_NOT_SECRET) requirementsMet = !(tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised || tile == StationTileType.SecretCorridor || tile == StationTileType.SecretEntrance);
            else if (requirement == StationTileType.WALKABLE_SECRET) requirementsMet = (tile == StationTileType.SecretCorridor || tile == StationTileType.SecretEntrance);
            else if (requirement == StationTileType.Empty) requirementsMet = (tile == StationTileType.Empty || tile == StationTileType.ERROR || tile == StationTileType.BossRaised);
            else if (requirement != StationTileType.NOREQUIREMENT) requirementsMet = (tile == requirement);
            else if (requirement == StationTileType.NOREQUIREMENT) requirementsMet = true;


            return requirementsMet;
        }
    }

    public class StationTileNeighborhood
    {

        public StationTileType north;
        public StationTileType northeast;
        public StationTileType northwest;

        public StationTileType south;
        public StationTileType southeast;
        public StationTileType southwest;

        public StationTileType center;
        public StationTileType east;
        public StationTileType west;

        public StationTileType above;
        public StationTileType below;

        public StationTileNeighborhood()
        {
            north = StationTileType.Empty;
            northeast = StationTileType.Empty;
            northwest = StationTileType.Empty;

            south = StationTileType.Empty;
            southeast = StationTileType.Empty;
            southwest = StationTileType.Empty;

            center = StationTileType.Empty;
            east = StationTileType.Empty;
            west = StationTileType.Empty;

            above = StationTileType.Empty;
            below = StationTileType.Empty;
        }

    }

    public enum Orientation
    {
        Northbound, Southbound, Eastbound, Westbound

    }

    public class IgnoreNeighborhood
    {

        public bool north;
        public bool northeast;
        public bool northwest;
        public bool south;
        public bool southeast;
        public bool southwest;
        public bool east;
        public bool west;
        public bool above;
        public bool below;

        public static IgnoreNeighborhood NO_IGNORE = new IgnoreNeighborhood();



        public IgnoreNeighborhood()
        {
            north = false;
            northeast = false;
            northwest = false;
            south = false;
            southeast = false;
            southwest = false;
            east = false;
            west = false;
            above = false;
            below = false;
        }

        //Self avoidance & Planar ignore neighborhood, ignores only the previous tile and considers 3D tiles as well as the tile in front of the corridor
        public static IgnoreNeighborhood CorridorSelfAvoidPlanarIgnore(Orientation currentDirection)
        {
            IgnoreNeighborhood toret = new IgnoreNeighborhood();
            switch (currentDirection)
            {
                case Orientation.Northbound: toret.south = true; break;
                case Orientation.Southbound: toret.north = true; break;
                case Orientation.Westbound: toret.east = true; break;
                case Orientation.Eastbound: toret.west = true; break;
            }

            return toret;
        }

        //Default ignore for Planar mode corridor without self avoidance, ignore both previous and next tile
        public static IgnoreNeighborhood CorridorPlanarIgnore(Orientation currentDirection)
        {
            IgnoreNeighborhood toret = new IgnoreNeighborhood();
            switch (currentDirection)
            {
                case Orientation.Northbound: toret.south = true; toret.north = true; break;
                case Orientation.Southbound: toret.north = true; toret.south = true; break;
                case Orientation.Westbound: toret.east = true; toret.west = true; break;
                case Orientation.Eastbound: toret.west = true; toret.east = true; break;
            }

            return toret;
        }

        //Default ignore for non planar layout featuring self avoidance
        public static IgnoreNeighborhood CorridorDefaultIgnore(Orientation currentDirection)
        {
            IgnoreNeighborhood toret = new IgnoreNeighborhood();
            switch (currentDirection)
            {
                case Orientation.Northbound: toret.south = true; toret.north = true; break;
                case Orientation.Southbound: toret.north = true; toret.south = true; break;
                case Orientation.Westbound: toret.east = true; toret.west = true; break;
                case Orientation.Eastbound: toret.west = true; toret.east = true; break;
            }

            toret.above = true;
            toret.below = true;

            return toret;
        }

        //Default corridor ignore for non planar, non self avoid corridor.
        public static IgnoreNeighborhood CorridorSelfAvoidIgnore(Orientation currentDirection)
        {
            IgnoreNeighborhood toret = new IgnoreNeighborhood();
            switch (currentDirection)
            {
                case Orientation.Northbound: toret.south = true; break;
                case Orientation.Southbound: toret.north = true; break;
                case Orientation.Westbound: toret.east = true; break;
                case Orientation.Eastbound: toret.west = true; break;
            }

            toret.above = true;
            toret.below = true;

            return toret;
        }
    }

    public class PGTrigger
    {

        //PGTrigger Global Types
        public const short TYPE_NOP = 0;
        public const short TYPE_ADDCORRIDOR = 1;
        public const short TYPE_ADDRANDROOM = 2;
        public const short TYPE_ADDRANDMODL = 3;
        public const short TYPE_ADDSECRETCORRIDOR = 4;


        public Vector3D pos;
        public Orientation orientation;
        public short type;
        public bool iexec;

        public PGTrigger(Vector3D p, Orientation o, short t, bool ie)
        {
            pos = p;
            orientation = o;
            type = t;
            iexec = ie;

        }


    }

    public struct RandomTileGroups
    {
        public struct GroupParams
        {
            public bool sharedroll;
            public byte srgroupID;
            public float chance;
            public byte groupID;
            public bool linked;
            public StationTileType replaceType;
        }

        public int groupCount;

        public int mwidth, mheight, mdepth;

        public byte[,,] layout;

        public List<GroupParams> groupParams;

        public bool init;

    }

}
