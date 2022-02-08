﻿using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Components.BankingAndCurrency;
using VRage.ModAPI;

namespace StationFramework
{
    public class StationGenerator
    {
        #region vars
        public int seed;
        public int height, width, levels;
        public StationTileType[,,] data;
        public IMyCubeGrid startBlock;
        public short sx, sy, sl;//last spawn location
        public byte[,,] currentAccessData;
        Random random;

        public const byte ALVL_OUTSIDE = 0;
        public const byte ALVL_INACCESSIBLE = 1;
        public const byte ALVL_MAIN = 2;
        public const byte ALVL_SECRET = 3;

        public int showoffwait = 12;

        int currentRoomX, currentRoomY;
        int currentEntranceX, currentEntranceY;
        int currentRoomTiles = 0;

        public int[] scx, scy; //Showoff Current Tiles

        readonly int sic = 5; //Showoff Indices Count

        public int stationTileCount = 0;

        List<Vector3D> lastAddedRooms;

        bool currentlyProcessingTriggers;

        List<PGTrigger> pgtriggers;
        List<PGTrigger> pgtriggersProcessingList;

        int cnc = 0;
        int cdnc = 0;
        int gcnc = 0;

        //Planar avoidance lists, disallowing connection to planar staircases
        //NO AVOIDANCE = Can connect to existing corridors of same type
        readonly StationTileType[] corridorPlanarNoAvoidanceList = { StationTileType.Entrance2, StationTileType.Entrance, StationTileType.Room2, StationTileType.Room, StationTileType.EntranceCloset, StationTileType.RoomCloset, StationTileType.BossRoom, StationTileType.BossRoom, StationTileType.SecretCorridor, StationTileType.SecretEntrance, StationTileType.Stairs, StationTileType.Staircase };
        readonly StationTileType[] corridorPlanarAvoidanceList = { StationTileType.Entrance2, StationTileType.Entrance, StationTileType.Corridor, StationTileType.Room2, StationTileType.Room, StationTileType.EntranceCloset, StationTileType.RoomCloset, StationTileType.BossRoom, StationTileType.BossRoom, StationTileType.SecretCorridor, StationTileType.SecretEntrance, StationTileType.Stairs, StationTileType.Staircase };


        //Non Planar avoidance lists, allowing connection to planar staircases
        //NO AVOIDANCE = Can connect to existing corridors of same type
        readonly StationTileType[] corridorNoAvoidanceList = { StationTileType.ToiletRoom, StationTileType.Room2, StationTileType.Room, StationTileType.EntranceCloset, StationTileType.RoomCloset, StationTileType.BossRoom, StationTileType.BossRoom, StationTileType.SecretCorridor, StationTileType.SecretEntrance };
        readonly StationTileType[] corridorAvoidanceList = { StationTileType.ToiletRoom, StationTileType.Corridor, StationTileType.Room2, StationTileType.Room, StationTileType.EntranceCloset, StationTileType.RoomCloset, StationTileType.BossRoom, StationTileType.BossRoom, StationTileType.SecretCorridor, StationTileType.SecretEntrance };

        StationModel[] availableModelArray;

        StationTileNeighborhood currentNeighborhood;
        StationTileNeighborhood goalNeighborhood;
        IgnoreNeighborhood currentIgnore;

        public static StationGenerator genObj;

        public short inaccessibleTileCount = 0;

        public StationTileType endType;
        static StationModel currentModel;
        static bool modelChosen;
        static int currentModelCount, currentModelMax;
        static PGTrigger currentModelTrigger;

        static int modelLoopMaxIterations = 100;

        static float mn;

        public GeneratedStationInfo currentMapInfo;
        public GenerationRequirements requirements;
        public List<StationModel> availableModels;

        int originFlr;
        public CorridorInfo originalInfo;

        int odh;
        CorridorInfo premadeInfo;
        bool useRequirements = false;
        bool useSavedStation = false;
        public bool randomTriggerIExec = false;
        public bool cleanDeadends = true;


        //public GameObject[,,] nodes;

        #endregion vars

        public StationGenerator(int s, int w, int h, int l, List<StationModel> models)
        {
            seed = s;
            this.height = h;
            this.width = w;
            this.levels = l;
            random = new System.Random(s);
            data = getEmptyMap(w, h, l);
            scx = new int[sic];
            scy = new int[sic];

            if (models != null)
                availableModels = models;



            pgtriggers = new List<PGTrigger>();

        }

        public void SetSeed(int s)
        {
            seed = s;
            random = new System.Random(s);
        }

        public StationTileType[,,] getEmptyMap(int width, int height, int levels)
        {
            StationTileType[,,] map = new StationTileType[width, height, levels];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {
                        map[i, j, l] = StationTileType.Empty;
                    }

                }
            }
            return map;
        }

        #region Generation
        public void GeneratedStation()
        {
            if(!useSavedStation)
            {
                if(useRequirements)
                {

                } else
                {
                    bool v2genNorth = true, v2genSouth = true, v2genEast = true, v2genWest = true, v2Priority = true, v2Planar = false;
                    float v2Branch = 0.125f;
                    float v2Turn = 0.3f;
                    float v2End = 0.005f;
                    float v2Room = 0.25f;
                    float v2Stairs = 0.03f;
                    float v2Model = 0.195f;
                    float v2Csavoid = 0.5f;
                    float v2EndRooms = 0.15f;
                    float v2TallRooms = 0.45f;
                    float v2LargeCorridors = 0.5f;
                    float v2SpawnCorridorsFromRooms = 0.65f;
                    int v2TallRoomMaxHeight = 3;
                    int v2TallRoomMinHeight = 3;
                    int roommin = 1;
                    int roommax = 2;
                    int v2CorridorMaxLength = 40;
                    int v2CountOffset = 0;
                    int v2MaxLengthOffset = 0;
                    GenerateStation(v2genNorth, v2genSouth, v2genEast, v2genWest, width / 2, height / 2, v2Branch, v2Turn, v2End, v2Room, v2Stairs, v2Model, v2Priority, false, true, true, true, v2Planar, v2Csavoid, v2CorridorMaxLength, StationTileType.Ending, v2CountOffset, v2MaxLengthOffset, v2EndRooms, v2TallRooms, v2TallRoomMaxHeight, v2TallRoomMinHeight, roommin, roommax, v2LargeCorridors, v2SpawnCorridorsFromRooms);
                }

                if(cleanDeadends)
                {
                    removeAllDeadends();
                }

                clearInaccessibleAreas();
            }
        }

        public void GenerateStation(bool no, bool so, bool ea, bool we, int x, int y, float b, float t, float e, float r, float s, float m, bool p, bool showoff, bool singleBranchMode, bool singleBranchChanceMode, bool useModels, bool planar, float csavoid, int maxl, StationTileType endType, int cto, int mlo, float endRooms, float tallRooms, int tallRoomMaxHeight, int tallRoomMinHeight, int roomsize, int roomrand, float largeCorridors, float growFromRooms)
        {
            CorridorInfo info = new CorridorInfo();
            originalInfo = new CorridorInfo();
            data = getEmptyMap(width, height, levels);
            pgtriggers = new List<PGTrigger>();
            pgtriggersProcessingList = new List<PGTrigger>();
            currentlyProcessingTriggers = false;

            if (requirements == null)
            {
                requirements = new GenerationRequirements(availableModels);
            }

            int l = originFlr = (levels == 1) ? 0 : (levels == 2) ? 1 : (levels >= 3) ? levels / 2 : 0;

            if (levels == 0)
                return;

            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            sx = (short)x;
            sy = (short)y;
            sl = (short)l;

            //Reset the random number generator in case the seed has been changed by the Generation Requirement loop
            random = new System.Random(seed);

            stationTileCount = 0;

            data[x, y, l] = StationTileType.Spawn; stationTileCount++;

            //Setting starting corridor info and using originalInfo obj to store its original state

            info.currentCount = originalInfo.currentCount = 0;
            info.branch = originalInfo.branch = b;
            info.turn = originalInfo.turn = t;
            info.end = originalInfo.end = e;
            info.room = originalInfo.room = r;
            info.stairs = originalInfo.stairs = s;
            info.model = originalInfo.model = m;
            info.priority = originalInfo.priority = p;
            info.justBranched = originalInfo.justBranched = true;
            info.showoffmode = originalInfo.showoffmode = showoff;
            info.singleBranchMode = originalInfo.singleBranchMode = singleBranchMode;
            info.singleBranchChanceMode = originalInfo.singleBranchChanceMode = singleBranchChanceMode;
            info.useModels = originalInfo.useModels = useModels;
            info.planeMode = originalInfo.planeMode = planar;
            info.csavoid = originalInfo.csavoid = csavoid;
            info.secret = originalInfo.secret = false;
            info.mlo = originalInfo.mlo = mlo;
            info.cto = originalInfo.cto = cto;
            info.endRooms = originalInfo.endRooms = endRooms;
            info.growFromRooms = originalInfo.growFromRooms = growFromRooms;
            info.maxlength = originalInfo.maxlength = maxl;
            info.tallRooms = tallRooms;
            info.tallRoomMaxHeight = tallRoomMaxHeight;
            info.tallRoomMinHeight = tallRoomMinHeight;
            info.roommin = roomsize;
            info.roommax = roomrand;
            info.largeCorridors = largeCorridors;

            

            currentMapInfo = new GeneratedStationInfo(availableModels);


            if (no)
            {
                info.x = x;
                info.y = y + 1;
                info.l = l;
                info.dh = 0;
                info.dv = 1;
                info.maxlength = height / 2;
                info.currentCount = 0;
                addCorridor(info);//Upwards
            }
            if (so)
            {
                info.x = x;
                info.y = y - 1;
                info.l = l;
                info.dh = 0;
                info.dv = -1;
                info.maxlength = height / 2;
                info.currentCount = 0;
                addCorridor(info);//Downwards
            }
            if (ea)
            {
                info.x = x + 1;
                info.y = y;
                info.l = l;
                info.dh = 1;
                info.dv = 0;
                info.maxlength = width / 2;
                info.currentCount = 0;
                addCorridor(info);//Right
            }
            if (we)
            {
                info.x = x - 1;
                info.y = y;
                info.l = l;
                info.dh = -1;
                info.dv = 0;
                info.maxlength = width / 2;
                info.currentCount = 0;
                addCorridor(info);//Left
            }


            //Process PGTriggers
            if (pgtriggers != null)
            {
                int cx, cy, cl, dh, dv;

                CorridorInfo newInfo = info.Clone();

                do
                {


                    foreach (PGTrigger tg in pgtriggers)
                    {
                        currentlyProcessingTriggers = true;

                        //Skip trigger if it has already been executed
                        if (tg.iexec)
                            continue;

                        cx = info.x = (int)tg.pos.X;
                        cy = info.y = (int)tg.pos.Y;
                        cl = info.l = (int)tg.pos.Z;
                        dh = info.dh = (int)toDxDy(tg.orientation).X;
                        dv = info.dv = (int)toDxDy(tg.orientation).Y;


                        switch (tg.type)
                        {

                            case PGTrigger.TYPE_NOP: break;

                            case PGTrigger.TYPE_ADDCORRIDOR:

                                if (!outOfBounds(cx + dh, cy + dv, cl) && data[cx + dh, cy + dv, cl] == StationTileType.Empty)
                                {
                                    data[cx, cy, cl] = StationTileType.Entrance; stationTileCount++;
                                    newInfo = info.Clone();
                                    newInfo.x += dh;
                                    newInfo.y += dv;
                                    addCorridor(newInfo);

                                }
                                break;

                            case PGTrigger.TYPE_ADDRANDROOM:

                                if (!outOfBounds(cx + dh, cy + dv, cl) && data[cx + dh, cy + dv, cl] == StationTileType.Empty)
                                {
                                    data[cx, cy, cl] = StationTileType.Entrance; stationTileCount++;
                                    data[cx + dh, cy + dv, cl] = StationTileType.Corridor; stationTileCount++;
                                    placeRandomRoom((int)tg.pos.X + dh, (int)tg.pos.Y + dv, (int)tg.pos.Z, dh, dv, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound) ? StationTileType.Room2 : StationTileType.Room, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound) ? StationTileType.Entrance2 : StationTileType.Entrance, 2, 5, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound), showoff, false, info.planeMode, tg.iexec, info.secret, info.growFromRooms);

                                }
                                break;

                            case PGTrigger.TYPE_ADDRANDMODL:

                                if (!outOfBounds(cx, cy, cl) && data[cx, cy, cl] == StationTileType.Empty)
                                {
                                    if (availableModelArray != null && availableModelArray.Length != 0)
                                    {
                                        //Gets a random model
                                        modelChosen = false;
                                        currentModel = null;
                                        mn = 0;
                                        do
                                        {
                                            currentModel = StationModel.pickRandomChance(availableModels.ToArray(), (float)random.NextDouble(), random);


                                            //End loop if no models are available
                                            if (currentModel == null)
                                                break;

                                            currentModel = StationModel.copy(currentModel);//Dereference arrays

                                            //Get the current model count from dictionary
                                            requirements.maximumModelRequirements.TryGetValue(currentModel.type, out currentModelCount);

                                            if (currentModelCount <= -1)
                                            {
                                                modelChosen = true;
                                            }
                                            else
                                            {
                                                modelChosen = (currentMapInfo.modelTypeCount[currentModel.type] < currentModelCount);
                                            }

                                            mn++;//Increment maximum try counter to avoid looping forever
                                        } while (!modelChosen && mn < modelLoopMaxIterations);

                                        if (modelChosen)
                                            placeModelV2(info, currentModel);
                                    }

                                }
                                break;

                            case PGTrigger.TYPE_ADDSECRETCORRIDOR:

                                if (!outOfBounds(cx + dh, cy + dv, cl) && data[cx + dh, cy + dv, cl] == StationTileType.Empty)
                                {
                                    data[cx, cy, cl] = StationTileType.SecretEntrance; stationTileCount++;
                                    newInfo = info.Clone();
                                    newInfo.x += dh;
                                    newInfo.y += dv;
                                    newInfo.secret = true;
                                    addCorridor(newInfo);

                                }
                                break;

                            default: break;
                        }


                    }

                    pgtriggers.Clear();//Clear trigger list

                    pgtriggers = pgtriggersProcessingList;//Put the newly added triggers into the list
                    pgtriggersProcessingList = new List<PGTrigger>();

                } while (pgtriggers.Count > 0);


            }

            currentMapInfo.tilecount = stationTileCount;

            data[(int)currentMapInfo.lastCorridor.X, (int)currentMapInfo.lastCorridor.Y, (int)currentMapInfo.lastCorridor.Z] = endType;

        }

        public void SpawnStationTiles()
        {
            if (startBlock == null)
                return;

            List<IMySlimBlock> blocks = new List<IMySlimBlock>();
            startBlock.GetBlocks(blocks);

            if (blocks.Count == 0)
                return;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {

                        PlacePrefabOnGrid(i, j, l, blocks[0]);

                    }
                }
            }
        }

        public void PlacePrefabOnGrid(int i, int j, int l, IMySlimBlock start)
        {

        }

        public void addCorridor(CorridorInfo info)
        {



            //Out of bounds
            if (info.x < 0 || info.x >= width || info.y < 0 || info.y >= height)
                return;

            //Exceeding max length ( 0 means infinite )
            if (info.currentCount >= info.maxlength && info.maxlength != 0)
            {
                return;
            }

            //Tile not empty and cannot overwrite
            if (data[info.x, info.y, info.l] != StationTileType.Empty && !info.priority)
            {

                if (!(data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs))
                {
                    return;
                }
                else if ((data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs) && info.planeMode)
                {
                    return;
                }

            }


            CorridorInfo newInfo = info.Clone();

            //Place corridor tile

            //Not using planar mode
            if (!info.planeMode)
            {
                //Use corridor self avoidance
                if (info.csavoid < random.NextDouble())
                {

                    currentIgnore = IgnoreNeighborhood.CorridorSelfAvoidIgnore(toOrientation(info.dh, info.dv));//Use non planar self avoid ignore

                    if (checkIncompatibleTileNeighbors(info.x, info.y, info.l, corridorAvoidanceList, currentIgnore))
                    {

                        if (!(data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs))
                            data[info.x, info.y, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor; stationTileCount++; info.currentCount++;


                    }
                    else return;

                }
                else//Allow corridor to connect
                {
                    IgnoreNeighborhood.CorridorSelfAvoidIgnore(toOrientation(info.dh, info.dv));//Use non planar non self avoid ignore

                    if (checkIncompatibleTileNeighbors(info.x, info.y, info.l, corridorNoAvoidanceList, currentIgnore))
                    {

                        if (!(data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs))
                            data[info.x, info.y, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor; stationTileCount++; info.currentCount++;

                    }
                    else return;

                }


            }
            else//Planar mode is active
            {
                //Use self avoidance
                if (info.csavoid < random.NextDouble())
                {

                    currentIgnore = IgnoreNeighborhood.CorridorSelfAvoidPlanarIgnore(toOrientation(info.dh, info.dv));

                    if (checkIncompatibleTileNeighbors3D(info.x, info.y, info.l, corridorPlanarAvoidanceList, currentIgnore))
                    {

                        if (!(data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs))
                            data[info.x, info.y, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor; stationTileCount++; info.currentCount++;


                    }
                    else
                    {
                        return;
                    }

                }
                else//Allow corridor to connect
                {

                    currentIgnore = IgnoreNeighborhood.CorridorSelfAvoidPlanarIgnore(toOrientation(info.dh, info.dv));

                    if (checkIncompatibleTileNeighbors3D(info.x, info.y, info.l, corridorPlanarNoAvoidanceList, currentIgnore))
                    {

                        if (!(data[info.x, info.y, info.l] == StationTileType.Staircase || data[info.x, info.y, info.l] == StationTileType.Stairs))
                            data[info.x, info.y, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor; stationTileCount++; info.currentCount++;

                    }
                    else
                    {
                        return;
                    }


                }


            }


            //Check if the corridor has been added
            if (data[info.x, info.y, info.l] == StationTileType.Corridor || data[info.x, info.y, info.l] == StationTileType.SecretCorridor)
            {
                //If the corridor is furthest from the spawn point, use it as current furthest path
                if (info.currentCount > currentMapInfo.longestPath)
                {
                    currentMapInfo.longestPath = info.currentCount;
                    currentMapInfo.lastCorridor = new Vector3(info.x, info.y, info.l);
                }

                //Large corridor mode active
                if (info.large)
                {

                    if (info.dh == 0)
                    {

                        if (data[info.x - info.prevdh, info.y, info.l] == StationTileType.Empty)
                        {
                            if (checkIncompatibleTileNeighbors3D(info.x - info.prevdh, info.y, info.l, corridorPlanarNoAvoidanceList, IgnoreNeighborhood.NO_IGNORE))
                                data[info.x - info.prevdh, info.y, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor;
                        }

                    }
                    else
                    {


                        if (data[info.x, info.y - info.prevdv, info.l] == StationTileType.Empty)
                        {
                            if (checkIncompatibleTileNeighbors3D(info.x, info.y - info.prevdv, info.l, corridorPlanarNoAvoidanceList, IgnoreNeighborhood.NO_IGNORE))
                                data[info.x, info.y - info.prevdv, info.l] = (info.secret) ? StationTileType.SecretCorridor : StationTileType.Corridor;
                        }

                    }


                }

            }



            //RNG module...

            //End the corridor randomly
            if (random.NextDouble() < info.end)
                return;

            //Branch into a perpendicular corridor
            if (random.NextDouble() < info.branch && !info.justBranched)
            {
                //Currently vertical
                if (info.dh == 0)
                {

                    //Special condition
                    if (random.Next(2) == 0 && random.Next(2) == 0)
                    {
                        newInfo = info.Clone();
                        newInfo.x += 1;
                        newInfo.dh = 1;
                        newInfo.dv = 0;
                        newInfo.justBranched = true;
                        newInfo.prevdh = info.dh;
                        newInfo.prevdv = info.dv;
                        newInfo.large = random.NextDouble() < info.largeCorridors;

                        addCorridor(newInfo);

                        newInfo = info.Clone();
                        newInfo.x -= 1;
                        newInfo.dh = -1;
                        newInfo.dv = 0;
                        newInfo.justBranched = true;
                        newInfo.prevdh = info.dh;
                        newInfo.prevdv = info.dv;
                        newInfo.large = random.NextDouble() < info.largeCorridors;

                        addCorridor(newInfo);
                    }
                    else
                    {

                        //Branch X positive
                        if (random.Next(2) == 0)
                        {
                            newInfo = info.Clone();
                            newInfo.x += 1;
                            newInfo.dh = 1;
                            newInfo.dv = 0;
                            newInfo.justBranched = true;
                            newInfo.large = random.NextDouble() < info.largeCorridors;
                            newInfo.prevdh = info.dh;
                            newInfo.prevdv = info.dv;
                            addCorridor(newInfo);
                        }
                        else
                        {
                            newInfo = info.Clone();
                            newInfo.x -= 1;
                            newInfo.dh = -1;
                            newInfo.dv = 0;
                            newInfo.justBranched = true;
                            newInfo.large = random.NextDouble() < info.largeCorridors;
                            newInfo.prevdh = info.dh;
                            newInfo.prevdv = info.dv;
                            addCorridor(newInfo);
                        }
                    }

                }//Currently Horizontal
                else
                {

                    //Special condition
                    if (random.Next(2) == 0 && random.Next(2) == 0)
                    {
                        newInfo = info.Clone();
                        newInfo.y += 1;
                        newInfo.dv = 1;
                        newInfo.dh = 0;
                        newInfo.justBranched = true;
                        newInfo.large = random.NextDouble() < info.largeCorridors;
                        newInfo.prevdh = info.dh;
                        newInfo.prevdv = info.dv;
                        addCorridor(newInfo);

                        newInfo = info.Clone();
                        newInfo.y -= 1;
                        newInfo.dv = -1;
                        newInfo.dh = 0;
                        newInfo.justBranched = true;
                        newInfo.prevdh = info.dh;
                        newInfo.prevdv = info.dv;
                        newInfo.large = random.NextDouble() < info.largeCorridors;
                        addCorridor(newInfo);
                    }
                    else
                    {

                        //Branch Y positive
                        if (random.Next(2) == 0)
                        {
                            newInfo = info.Clone();
                            newInfo.y += 1;
                            newInfo.dv = 1;
                            newInfo.dh = 0;
                            newInfo.justBranched = true;
                            newInfo.large = random.NextDouble() < info.largeCorridors;
                            newInfo.prevdh = info.dh;
                            newInfo.prevdv = info.dv;
                            addCorridor(newInfo);
                        }
                        else
                        {
                            newInfo = info.Clone();
                            newInfo.y -= 1;
                            newInfo.dv = -1;
                            newInfo.dh = 0;
                            newInfo.justBranched = true;
                            newInfo.large = random.NextDouble() < info.largeCorridors;
                            newInfo.prevdh = info.dh;
                            newInfo.prevdv = info.dv;
                            addCorridor(newInfo);
                        }
                    }
                }


                if (info.singleBranchMode && !info.singleBranchChanceMode)
                    return;

                if (info.singleBranchChanceMode && info.singleBranchMode && random.NextDouble() < info.turn)
                    return;


            }

            //Place entrance
            if (random.NextDouble() < info.room)
            {

                //Place room in front of corridor

                if (random.NextDouble() > info.endRooms)
                {
                    if (random.NextDouble() < info.tallRooms)
                        placeRandomTallRoom(info.x, info.y, info.l, info.dh, info.dv, random.Next(info.tallRoomMinHeight, info.tallRoomMaxHeight), (info.dh == 0) ? StationTileType.Room : StationTileType.Room2, (info.dh == 0) ? StationTileType.Entrance : StationTileType.Entrance2, info.roommin, info.roommax, (info.dh != 0), info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                    else
                    {
                        placeRandomRoom(info.x, info.y, info.l, info.dh, info.dv, (info.dh == 0) ? StationTileType.Room : StationTileType.Room2, (info.dh == 0) ? StationTileType.Entrance : StationTileType.Entrance2, info.roommin, info.roommax, true, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                    }

                }//Otherwise place it perpendicular to corridor
                else
                {
                    //Currently vertical
                    if (info.dh == 0)
                    {
                        //Room Horizontal Positive
                        if (random.Next(2) == 0)
                        {
                            if (random.NextDouble() < info.tallRooms)
                            {
                                placeRandomTallRoom((info.large && info.prevdh == -1) ? info.x + 1 : info.x, info.y, info.l, 1, 0, random.Next(info.tallRoomMinHeight, info.tallRoomMaxHeight), StationTileType.Room, StationTileType.Entrance, info.roommin, info.roommax, true, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }
                            else
                            {
                                placeRandomRoom((info.large && info.prevdh == -1) ? info.x + 1 : info.x, info.y, info.l, 1, 0, StationTileType.Room, StationTileType.Entrance, info.roommin, info.roommax, true, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }
                        }

                        else
                        {
                            if (random.NextDouble() < info.tallRooms)
                            {
                                placeRandomTallRoom((info.large && info.prevdh == 1) ? info.x - 1 : info.x, info.y, info.l, -1, 0, random.Next(info.tallRoomMinHeight, info.tallRoomMaxHeight), StationTileType.Room, StationTileType.Entrance, info.roommin, info.roommax, true, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);

                            }
                            else
                            {
                                placeRandomRoom((info.large && info.prevdh == 1) ? info.x - 1 : info.x, info.y, info.l, -1, 0, StationTileType.Room, StationTileType.Entrance, info.roommin, info.roommax, true, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }


                        }
                        //Currently horizontal
                    }
                    else
                    {
                        //Room Vertical Positive
                        if (random.Next(2) == 0)
                        {
                            if (random.NextDouble() < info.tallRooms)
                            {
                                placeRandomTallRoom(info.x, (info.large && info.prevdv == -1) ? info.y + 1 : info.y, info.l, 0, 1, random.Next(info.tallRoomMinHeight, info.tallRoomMaxHeight), StationTileType.Room2, StationTileType.Entrance2, info.roommin, info.roommax, false, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }
                            else
                            {
                                placeRandomRoom(info.x, (info.large && info.prevdv == -1) ? info.y + 1 : info.y, info.l, 0, 1, StationTileType.Room2, StationTileType.Entrance2, info.roommin, info.roommax, false, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }


                        }
                        else
                        {

                            if (random.NextDouble() < info.tallRooms)
                            {
                                placeRandomTallRoom(info.x, (info.large && info.prevdv == 1) ? info.y - 1 : info.y, info.l, 0, -1, random.Next(info.tallRoomMinHeight, info.tallRoomMaxHeight), StationTileType.Room2, StationTileType.Entrance2, info.roommin, info.roommax, false, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }
                            else
                            {
                                placeRandomRoom(info.x, (info.large && info.prevdv == 1) ? info.y - 1 : info.y, info.l, 0, -1, StationTileType.Room2, StationTileType.Entrance2, info.roommin, info.roommax, false, info.showoffmode, true, info.planeMode, randomTriggerIExec, info.secret, info.growFromRooms);
                            }

                        }

                    }
                }




            }

            //Place staircase
            if (random.NextDouble() < info.stairs)
            {

                int lld = 0;

                //Towards top since we are at bottom
                if (info.l == 0) lld = 1;

                //Towards bottom since we are at top
                else if (info.l >= levels - 1) lld = -1;
                //Randomly since we are in between
                else if (random.Next(2) == 0) lld = 1;
                else lld = -1;


                if (!info.planeMode)
                {
                    if (placeStaircase(info.x, info.y, info.l, lld, info.showoffmode))
                    {
                        newInfo = info.Clone();
                        newInfo.l += lld;
                        newInfo.dh = 1;
                        newInfo.dv = 0;
                        addCorridor(newInfo);
                        newInfo.dh = -1;
                        newInfo.dv = 0;
                        addCorridor(newInfo);
                        newInfo.dh = 0;
                        newInfo.dv = 1;
                        addCorridor(newInfo);
                        newInfo.dh = 0;
                        newInfo.dv = -1;
                        addCorridor(newInfo);

                        return;
                    }
                }
                else//Planar mode staircase
                {

                    //TRI JUNCTION
                    if (random.Next(2) == 0 && random.Next(2) == 0)
                    {
                        //First normal stairs
                        if (addStaircaseV2(info.x + info.dh, info.y + info.dv, info.l, info.dh, info.dv, lld))
                        {
                            newInfo = info.Clone();
                            newInfo.x = info.x + info.dh * 3;
                            newInfo.y = info.y + info.dv * 3;
                            newInfo.l = info.l + lld;

                            addCorridor(newInfo);



                        }

                        //Switching directions for first extra branch

                        newInfo = info.Clone();

                        odh = newInfo.dh;

                        newInfo.dh = newInfo.dv;
                        newInfo.dv = odh;

                        if (addStaircaseV2(newInfo.x + newInfo.dh, newInfo.y + newInfo.dv, newInfo.l, newInfo.dh, newInfo.dv, lld))
                        {
                            newInfo.x = newInfo.x + newInfo.dh * 3;
                            newInfo.y = newInfo.y + newInfo.dv * 3;
                            newInfo.l = newInfo.l + lld;

                            addCorridor(newInfo);



                        }

                        //Switching & inverting for second extra branch

                        newInfo = info.Clone();

                        odh = newInfo.dh;

                        newInfo.dh = -newInfo.dv;
                        newInfo.dv = -odh;

                        if (addStaircaseV2(newInfo.x + newInfo.dh, newInfo.y + newInfo.dv, newInfo.l, newInfo.dh, newInfo.dv, lld))
                        {
                            newInfo.x = newInfo.x + newInfo.dh * 3;
                            newInfo.y = newInfo.y + newInfo.dv * 3;
                            newInfo.l = newInfo.l + lld;

                            addCorridor(newInfo);


                        }

                        return;


                    }

                    //T JUNCTION
                    else if (random.Next(2) == 0)
                    {
                        //Switching directions for first extra branch

                        newInfo = info.Clone();

                        odh = newInfo.dh;

                        newInfo.dh = newInfo.dv;
                        newInfo.dv = odh;

                        if (addStaircaseV2(newInfo.x + newInfo.dh, newInfo.y + newInfo.dv, newInfo.l, newInfo.dh, newInfo.dv, lld))
                        {
                            newInfo.x = newInfo.x + newInfo.dh * 3;
                            newInfo.y = newInfo.y + newInfo.dv * 3;
                            newInfo.l = newInfo.l + lld;

                            addCorridor(newInfo);



                        }

                        //Switching & inverting for second extra branch

                        newInfo = info.Clone();

                        odh = newInfo.dh;

                        newInfo.dh = -newInfo.dv;
                        newInfo.dv = -odh;

                        if (addStaircaseV2(newInfo.x + newInfo.dh, newInfo.y + newInfo.dv, newInfo.l, newInfo.dh, newInfo.dv, lld))
                        {
                            newInfo.x = newInfo.x + newInfo.dh * 3;
                            newInfo.y = newInfo.y + newInfo.dv * 3;
                            newInfo.l = newInfo.l + lld;

                            addCorridor(newInfo);


                        }

                        return;
                    }

                    //REGULAR FRONT FACING STAIRCASE
                    else if (addStaircaseV2(info.x + info.dh, info.y + info.dv, info.l, info.dh, info.dv, lld))
                    {
                        newInfo = info.Clone();
                        newInfo.x = info.x + info.dh * 3;
                        newInfo.y = info.y + info.dv * 3;
                        newInfo.l = info.l + lld;

                        addCorridor(newInfo);

                        return;

                    }

                }

            }

            //Model placement
            if (random.NextDouble() < info.model && info.useModels && availableModels.Count > 0)
            {

                //Gets a random model
                modelChosen = false;
                currentModel = null;
                mn = 0;
                do
                {
                    currentModel = StationModel.pickRandomChance(availableModels.ToArray(), (float)random.NextDouble(), random);


                    //End loop if no models are available
                    if (currentModel == null)
                        break;

                    currentModel = StationModel.copy(currentModel);//Dereference arrays

                    //Get the current model count from dictionary
                    requirements.maximumModelRequirements.TryGetValue(currentModel.type, out currentModelCount);

                    if (currentModelCount <= -1)
                    {
                        modelChosen = true;
                    }
                    else
                    {
                        modelChosen = (currentMapInfo.modelTypeCount[currentModel.type] < currentModelCount);
                    }

                    mn++;//Increment maximum try counter to avoid looping forever
                } while (!modelChosen && mn < modelLoopMaxIterations);

                //Try adding the chosen model, if any
                if (modelChosen)
                {

                    if (currentModel.spawnMode == StationModel.SPAWNMODE_PERP)
                    {
                        //Spawnmode set to PERPENDICULAR
                        //currently horizontal
                        if (info.dv == 0)
                        {

                            //Place northbound
                            if (random.Next(2) == 0)
                            {

                                newInfo = info.Clone();
                                newInfo.y = (info.large) ? info.y + (info.prevdv > 0 ? 1 : 2) : info.y + 1;
                                newInfo.dh = 0;
                                newInfo.dv = 1;


                                placeModelV2(newInfo, currentModel);
                            }
                            else
                            {
                                newInfo = info.Clone();
                                newInfo.y = (info.large) ? info.y - (info.prevdv < 0 ? 1 : 2) : info.y - 1;
                                newInfo.dh = 0;
                                newInfo.dv = -1;

                                placeModelV2(newInfo, currentModel);
                            }

                        }
                        else
                        {

                            //Place eastbound
                            if (random.Next(2) == 0)
                            {

                                newInfo = info.Clone();
                                newInfo.x = (info.large) ? info.x + (info.prevdh > 0 ? 1 : 2) : info.x + 1;
                                newInfo.dh = 1;
                                newInfo.dv = 0;

                                placeModelV2(newInfo, currentModel);
                            }
                            else
                            {
                                newInfo = info.Clone();
                                newInfo.x = (info.large) ? info.x - (info.prevdh < 0 ? 1 : 2) : info.x - 1;
                                newInfo.dh = -1;
                                newInfo.dv = 0;


                                placeModelV2(newInfo, currentModel);
                            }

                        }

                    }//Spawnmode set to FRONT
                    else if (currentModel.spawnMode == StationModel.SPAWNMODE_FRONT)
                    {

                        newInfo = info.Clone();
                        newInfo.x = info.x + info.dh;
                        newInfo.y = info.y + info.dv;

                        placeModelV2(newInfo, currentModel);
                    }
                    else
                    {
                        //Spawnmode set to RANDOM, using endRooms parameter to pick add style
                        if (random.NextDouble() < info.endRooms)
                        {
                            newInfo = info.Clone();
                            newInfo.x = info.x + info.dh;
                            newInfo.y = info.y + info.dv;

                            placeModelV2(newInfo, currentModel);
                        }
                        else
                        {
                            if (info.dv == 0)
                            {

                                //Place northbound
                                if (random.Next(2) == 0)
                                {

                                    newInfo = info.Clone();
                                    newInfo.y = (info.large) ? info.y + (info.prevdv > 0 ? 1 : 2) : info.y + 1;
                                    newInfo.dh = 0;
                                    newInfo.dv = 1;


                                    placeModelV2(newInfo, currentModel);
                                }
                                else
                                {
                                    newInfo = info.Clone();
                                    newInfo.y = (info.large) ? info.y + (info.prevdv < 0 ? 1 : 2) : info.y - 1;
                                    newInfo.dh = 0;
                                    newInfo.dv = -1;

                                    placeModelV2(newInfo, currentModel);
                                }

                            }
                            else
                            {

                                //Place eastbound
                                if (random.Next(2) == 0)
                                {

                                    newInfo = info.Clone();
                                    newInfo.x = (info.large) ? info.x + (info.prevdh > 0 ? 1 : 2) : info.x + 1;
                                    newInfo.dh = 1;
                                    newInfo.dv = 0;

                                    placeModelV2(newInfo, currentModel);
                                }
                                else
                                {
                                    newInfo = info.Clone();
                                    newInfo.x = (info.large) ? info.x - (info.prevdh < 0 ? 1 : 2) : info.x - 1;
                                    newInfo.dh = -1;
                                    newInfo.dv = 0;


                                    placeModelV2(newInfo, currentModel);
                                }

                            }
                        }
                    }

                }


            }



            //Step to the next corridor
            newInfo = info.Clone();
            newInfo.x += info.dh;
            newInfo.y += info.dv;
            newInfo.justBranched = false;
            addCorridor(newInfo);



        }

        public void processTrigger(PGTrigger tg, CorridorInfo info)
        {

            currentlyProcessingTriggers = true;

            CorridorInfo newInfo;

            int cx, cy, cl, dh, dv;

            cx = info.x = (int)tg.pos.X;
            cy = info.y = (int)tg.pos.Y;
            cl = info.l = (int)tg.pos.Z;
            dh = info.dh = (int)toDxDy(tg.orientation).X;
            dv = info.dv = (int)toDxDy(tg.orientation).Y;


            switch (tg.type)
            {

                case PGTrigger.TYPE_NOP: break;

                case PGTrigger.TYPE_ADDCORRIDOR:

                    if (!outOfBounds(cx + dh, cy + dv, cl) && data[cx + dh, cy + dv, cl] == StationTileType.Empty)
                    {
                        data[cx, cy, cl] = StationTileType.Entrance; stationTileCount++;
                        newInfo = info.Clone();
                        newInfo.x += dh;
                        newInfo.y += dv;
                        addCorridor(newInfo);

                    }
                    break;

                case PGTrigger.TYPE_ADDRANDROOM:

                    if (!outOfBounds(cx, cy, cl) && data[cx, cy, cl] == StationTileType.Empty)
                    {
                        placeRandomRoom(cx - dh, cy - dv, cl, dh, dv, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound) ? StationTileType.Room2 : StationTileType.Room, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound) ? StationTileType.Entrance2 : StationTileType.Entrance, 2, 5, (tg.orientation == Orientation.Northbound || tg.orientation == Orientation.Southbound), false, false, info.planeMode, tg.iexec, info.secret, info.growFromRooms);

                    }
                    break;

                case PGTrigger.TYPE_ADDRANDMODL:

                    if (!outOfBounds(cx, cy, cl) && data[cx, cy, cl] == StationTileType.Empty)
                    {
                        if (availableModelArray != null && availableModelArray.Length != 0)
                        {
                            //Gets a random model
                            modelChosen = false;
                            currentModel = null;
                            mn = 0;
                            do
                            {
                                currentModel = StationModel.pickRandomChance(availableModels.ToArray(), (float)random.NextDouble(), random);


                                //End loop if no models are available
                                if (currentModel == null)
                                    break;

                                currentModel = StationModel.copy(currentModel);//Dereference arrays

                                //Get the current model count from dictionary
                                requirements.maximumModelRequirements.TryGetValue(currentModel.type, out currentModelCount);

                                if (currentModelCount <= -1)
                                {
                                    modelChosen = true;
                                }
                                else
                                {
                                    modelChosen = (currentMapInfo.modelTypeCount[currentModel.type] < currentModelCount);
                                }

                                mn++;//Increment maximum try counter to avoid looping forever
                            } while (!modelChosen && mn < modelLoopMaxIterations);

                            if (modelChosen)
                                placeModelV2(info, currentModel);
                        }

                    }
                    break;

                case PGTrigger.TYPE_ADDSECRETCORRIDOR:

                    if (!outOfBounds(cx + dh, cy + dv, cl) && data[cx + dh, cy + dv, cl] == StationTileType.Empty)
                    {
                        data[cx, cy, cl] = StationTileType.SecretEntrance; stationTileCount++;
                        newInfo = info.Clone();
                        newInfo.x += dh;
                        newInfo.y += dv;
                        newInfo.secret = true;
                        addCorridor(newInfo);

                    }
                    break;

                default: break;
            }
        }

        #endregion

        #region Neighbor

        public StationTileNeighborhood getNeighbors(int x, int y, int l)
        {
            StationTileNeighborhood currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;
                return currentNeighborhood;
            }


            try
            {
                currentNeighborhood.center = data[x, y, l];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.north = data[x, y + 1, l];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[x, y - 1, l];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.east = data[x + 1, y, l];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.west = data[x - 1, y, l];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }

            return currentNeighborhood;
        }

        public StationTileNeighborhood getNeighbors3D(int x, int y, int l)
        {
            StationTileNeighborhood currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;
                currentNeighborhood.above = StationTileType.ERROR;
                currentNeighborhood.below = StationTileType.ERROR;
                return currentNeighborhood;
            }

            try
            {
                currentNeighborhood.center = data[x, y, l];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.north = data[x, y + 1, l];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[x, y - 1, l];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.east = data[x + 1, y, l];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.west = data[x - 1, y, l];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.above = data[x, y, l + 1];
            }
            catch
            {
                currentNeighborhood.above = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.below = data[x, y, l - 1];
            }
            catch
            {
                currentNeighborhood.below = StationTileType.ERROR;
            }


            return currentNeighborhood;

        }

        public StationTileNeighborhood getNeighborsFull(int x, int y, int l)
        {
            StationTileNeighborhood currentNeighborhood = new StationTileNeighborhood();

            if (data == null)
            {
                currentNeighborhood.north = StationTileType.ERROR;
                currentNeighborhood.northeast = StationTileType.ERROR;
                currentNeighborhood.northwest = StationTileType.ERROR;

                currentNeighborhood.east = StationTileType.ERROR;
                currentNeighborhood.west = StationTileType.ERROR;

                currentNeighborhood.south = StationTileType.ERROR;
                currentNeighborhood.southeast = StationTileType.ERROR;
                currentNeighborhood.southwest = StationTileType.ERROR;

                currentNeighborhood.above = StationTileType.ERROR;
                currentNeighborhood.below = StationTileType.ERROR;

                return currentNeighborhood;
            }

            try
            {
                currentNeighborhood.center = data[x, y, l];
            }
            catch
            {
                currentNeighborhood.center = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.north = data[x, y + 1, l];
            }
            catch
            {
                currentNeighborhood.north = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.northeast = data[x + 1, y + 1, l];
            }
            catch
            {
                currentNeighborhood.northeast = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.northwest = data[x - 1, y + 1, l];
            }
            catch
            {
                currentNeighborhood.northwest = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.south = data[x, y - 1, l];
            }
            catch
            {
                currentNeighborhood.south = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.southwest = data[x - 1, y - 1, l];
            }
            catch
            {
                currentNeighborhood.southwest = StationTileType.ERROR;
            }

            try
            {
                currentNeighborhood.southeast = data[x + 1, y - 1, l];
            }
            catch
            {
                currentNeighborhood.southeast = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.east = data[x + 1, y, l];
            }
            catch
            {
                currentNeighborhood.east = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.west = data[x - 1, y, l];
            }
            catch
            {
                currentNeighborhood.west = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.above = data[x, y, l + 1];
            }
            catch
            {
                currentNeighborhood.above = StationTileType.ERROR;
            }
            try
            {
                currentNeighborhood.below = data[x, y, l - 1];
            }
            catch
            {
                currentNeighborhood.below = StationTileType.ERROR;
            }

            return currentNeighborhood;

        }

        public bool checkEntranceNeighbors(int x, int y, int l, StationTileType roomType)
        {
            currentNeighborhood = getNeighbors(x, y, l);

            if (!(currentNeighborhood.north == StationTileType.Empty || currentNeighborhood.north == roomType || currentNeighborhood.north == StationTileType.Corridor || currentNeighborhood.north == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.south == StationTileType.Empty || currentNeighborhood.south == roomType || currentNeighborhood.south == StationTileType.Corridor || currentNeighborhood.south == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.east == StationTileType.Empty || currentNeighborhood.east == roomType || currentNeighborhood.east == StationTileType.Corridor || currentNeighborhood.east == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.west == StationTileType.Empty || currentNeighborhood.west == roomType || currentNeighborhood.west == StationTileType.Corridor || currentNeighborhood.west == StationTileType.SecretCorridor))
                return false;

            return true;
        }

        public bool checkEntranceNeighbors3D(int x, int y, int l, StationTileType roomType)
        {
            currentNeighborhood = getNeighbors3D(x, y, l);

            if (!(currentNeighborhood.north == StationTileType.Empty || currentNeighborhood.north == roomType || currentNeighborhood.north == StationTileType.Corridor || currentNeighborhood.north == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.south == StationTileType.Empty || currentNeighborhood.south == roomType || currentNeighborhood.south == StationTileType.Corridor || currentNeighborhood.south == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.east == StationTileType.Empty || currentNeighborhood.east == roomType || currentNeighborhood.east == StationTileType.Corridor || currentNeighborhood.east == StationTileType.SecretCorridor))
                return false;

            if (!(currentNeighborhood.west == StationTileType.Empty || currentNeighborhood.west == roomType || currentNeighborhood.west == StationTileType.Corridor || currentNeighborhood.west == StationTileType.SecretCorridor))
                return false;

            if (!((currentNeighborhood.above == StationTileType.Empty || currentNeighborhood.above == StationTileType.ERROR)))
                return false;

            if (!((currentNeighborhood.below == StationTileType.Empty || currentNeighborhood.below == StationTileType.ERROR)))
                return false;

            return true;
        }

        public bool checkRoomNeighbors(int x, int y, int l, StationTileType roomType, StationTileType entranceType)
        {
            currentNeighborhood = getNeighbors(x, y, l);

            if (!(currentNeighborhood.north == StationTileType.Empty || currentNeighborhood.north == roomType || currentNeighborhood.north == entranceType) || (currentNeighborhood.north == StationTileType.Corridor))
                return false;

            if (!(currentNeighborhood.south == StationTileType.Empty || currentNeighborhood.south == roomType || currentNeighborhood.south == entranceType) || (currentNeighborhood.south == StationTileType.Corridor))
                return false;

            if (!(currentNeighborhood.east == StationTileType.Empty || currentNeighborhood.east == roomType || currentNeighborhood.east == entranceType) || (currentNeighborhood.east == StationTileType.Corridor))
                return false;

            if (!(currentNeighborhood.west == StationTileType.Empty || currentNeighborhood.west == roomType || currentNeighborhood.west == entranceType) || (currentNeighborhood.west == StationTileType.Corridor))
                return false;

            return true;
        }

        public bool checkRoomNeighbors3D(int x, int y, int l, StationTileType roomType, StationTileType entranceType, bool tallCeilingMode)
        {
            currentNeighborhood = getNeighbors3D(x, y, l);

            if (tallCeilingMode)
            {
                if (!(currentNeighborhood.north == StationTileType.Empty || currentNeighborhood.north == StationTileType.ERROR || currentNeighborhood.north == roomType))
                    return false;

                if (!(currentNeighborhood.south == StationTileType.Empty || currentNeighborhood.south == StationTileType.ERROR || currentNeighborhood.south == roomType))
                    return false;

                if (!(currentNeighborhood.east == StationTileType.Empty || currentNeighborhood.east == StationTileType.ERROR || currentNeighborhood.east == roomType))
                    return false;

                if (!(currentNeighborhood.west == StationTileType.Empty || currentNeighborhood.west == StationTileType.ERROR || currentNeighborhood.west == roomType))
                    return false;

                if (!((currentNeighborhood.above == StationTileType.Empty || currentNeighborhood.above == StationTileType.ERROR)))
                    return false;

                if (!((currentNeighborhood.below == StationTileType.Empty || currentNeighborhood.below == StationTileType.ERROR || currentNeighborhood.below == roomType)))
                    return false;
            }
            else
            {

                if (!(currentNeighborhood.north == StationTileType.Empty || currentNeighborhood.north == roomType || currentNeighborhood.north == entranceType) || (currentNeighborhood.north == StationTileType.Corridor))
                    return false;

                if (!(currentNeighborhood.south == StationTileType.Empty || currentNeighborhood.south == roomType || currentNeighborhood.south == entranceType) || (currentNeighborhood.south == StationTileType.Corridor))
                    return false;

                if (!(currentNeighborhood.east == StationTileType.Empty || currentNeighborhood.east == roomType || currentNeighborhood.east == entranceType) || (currentNeighborhood.east == StationTileType.Corridor))
                    return false;

                if (!(currentNeighborhood.west == StationTileType.Empty || currentNeighborhood.west == roomType || currentNeighborhood.west == entranceType) || (currentNeighborhood.west == StationTileType.Corridor))
                    return false;

                if (!((currentNeighborhood.above == StationTileType.Empty || currentNeighborhood.above == StationTileType.ERROR)))
                    return false;

                if (!((currentNeighborhood.below == StationTileType.Empty || currentNeighborhood.below == StationTileType.ERROR)))
                    return false;

            }



            return true;
        }

        public bool checkIncompatibleTileNeighbors(int x, int y, int l, StationTileType[] incompatible, IgnoreNeighborhood ignore)
        {
            currentNeighborhood = getNeighbors(x, y, l);

            bool ok = true;

            foreach (StationTileType t in incompatible)
            {
                ok = !((currentNeighborhood.north == t && !ignore.north) || (currentNeighborhood.south == t && !ignore.south) || (currentNeighborhood.east == t && !ignore.east) || (currentNeighborhood.west == t && !ignore.west));
                if (!ok)
                    return ok;
            }

            return ok;
        }

        public bool checkIncompatibleTileNeighbors3D(int x, int y, int l, StationTileType[] incompatible, IgnoreNeighborhood ignore)
        {
            currentNeighborhood = getNeighbors3D(x, y, l);

            bool ok = true;

            foreach (StationTileType t in incompatible)
            {
                ok = !((currentNeighborhood.north == t && !ignore.north) || (currentNeighborhood.south == t && !ignore.south) || (currentNeighborhood.east == t && !ignore.east) || (currentNeighborhood.west == t && !ignore.west) || (!(currentNeighborhood.above == StationTileType.Empty || currentNeighborhood.above == StationTileType.ERROR) && !ignore.above) || (!(currentNeighborhood.below == StationTileType.Empty || currentNeighborhood.below == StationTileType.ERROR) && !ignore.below));
                if (!ok)
                {
                    return ok;
                }

            }

            return ok;
        }

        public bool checkCompatibleNeighbors(int x, int y, int l, StationTileType[] compatible, IgnoreNeighborhood ignore)
        {

            bool northok = false, southok = false, eastok = false, westok = false;

            currentNeighborhood = getNeighbors(x, y, l);

            foreach (StationTileType t in compatible)
            {

                if (!northok)
                    northok = (currentNeighborhood.north == t || ignore.north);

                if (!southok)
                    southok = (currentNeighborhood.south == t || ignore.south);

                if (!eastok)
                    eastok = (currentNeighborhood.east == t || ignore.east);

                if (!westok)
                    westok = (currentNeighborhood.west == t || ignore.west);

            }


            return (northok && southok && westok && eastok);

        }

        public bool staircaseCheck(int x, int y, int l)
        {
            if (outOfBounds(x, y, l)) return false;
            return ((data[x, y, l] == StationTileType.Corridor || data[x, y, l] == StationTileType.Empty));
        }

        public bool canPlaceStaircase(int x, int y, int l, int dl)
        {

            if (!staircaseCheck(x - 1, y + 1, l)) return false;
            if (!staircaseCheck(x, y + 1, l)) return false;
            if (!staircaseCheck(x + 1, y + 1, l)) return false;
            if (!staircaseCheck(x - 1, y, l)) return false;
            if (!staircaseCheck(x, y, l)) return false;
            if (!staircaseCheck(x + 1, y, l)) return false;
            if (!staircaseCheck(x - 1, y - 1, l)) return false;
            if (!staircaseCheck(x, y - 1, l)) return false;
            if (!staircaseCheck(x + 1, y - 1, l)) return false;

            if (!staircaseCheck(x - 1, y + 1, l + dl)) return false;
            if (!staircaseCheck(x, y + 1, l + dl)) return false;
            if (!staircaseCheck(x + 1, y + 1, l + dl)) return false;
            if (!staircaseCheck(x - 1, y, l + dl)) return false;
            if (!staircaseCheck(x, y, l + dl)) return false;
            if (!staircaseCheck(x + 1, y, l + dl)) return false;
            if (!staircaseCheck(x - 1, y - 1, l + dl)) return false;
            if (!staircaseCheck(x, y - 1, l + dl)) return false;
            if (!staircaseCheck(x + 1, y - 1, l + dl)) return false;

            return true;

        }

        public bool staircaseCheckV2(int x, int y, int l, IgnoreNeighborhood ignore)
        {

            currentNeighborhood = getNeighbors3D(x, y, l);

            //Correct 3D neighbor checking to make sure staircases do not break the layout
            //Checks above and below the current floor before being added.
            if ((currentNeighborhood.north != StationTileType.Empty && !ignore.north) ||
                (currentNeighborhood.south != StationTileType.Empty && !ignore.south) ||
                (currentNeighborhood.west != StationTileType.Empty && !ignore.west) ||
                (currentNeighborhood.east != StationTileType.Empty && !ignore.east) ||
                ((currentNeighborhood.above != StationTileType.Empty && currentNeighborhood.above != StationTileType.ERROR) && !ignore.above) ||
                ((currentNeighborhood.below != StationTileType.Empty && currentNeighborhood.below != StationTileType.ERROR) && !ignore.below))
            {
                return false;
            }

            return (data[x, y, l] == StationTileType.Empty);
        }


        #endregion

        #region Placing
        public void placeRandomRoom(int x, int y, int l, int dh, int dv, StationTileType roomType, StationTileType entranceType, int minsize, int maxsize, bool messyMode, bool showoffmode, bool addPassages, bool planeMode, bool trigExec, bool secret, float growFromRoom)
        {

            bool reachedDoor = false, passageAdded = false;
            lastAddedRooms = new List<Vector3D>();

            currentRoomTiles = 0;


            //Out of bounds
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            //Assign direction bound variables
            int d = (dh == 0) ? dv : dh;
            bool zy = (d == dv);

            //Place initial entrance
            currentRoomX = x + dh;
            currentRoomY = y + dv;

            currentEntranceX = currentRoomX;
            currentEntranceY = currentRoomY;

            if (!planeMode)
            {
                if (checkEntranceNeighbors(currentEntranceX, currentEntranceY, l, roomType) && checkRoomNeighbors(currentRoomX + dh, currentRoomY + dv, l, roomType, entranceType))
                {
                    data[currentEntranceX, currentEntranceY, l] = (secret) ? StationTileType.SecretEntrance : entranceType;
                    lastAddedRooms.Add(new Vector3(currentEntranceX, currentEntranceY, l));
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (checkEntranceNeighbors3D(currentEntranceX, currentEntranceY, l, roomType) && checkRoomNeighbors3D(currentRoomX + dh, currentRoomY + dv, l, roomType, entranceType, false))
                {
                    data[currentEntranceX, currentEntranceY, l] = (secret) ? StationTileType.SecretEntrance : entranceType;
                    lastAddedRooms.Add(new Vector3(currentEntranceX, currentEntranceY, l));
                }
                else
                {
                    return;
                }
            }


            currentRoomX += dh;
            currentRoomY += dv;



            

            int ymin = -MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int ymax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int xmin = -MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int xmax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);

            for (int j = ymin; j < ymax; j++)
            {

                //Generate room line
                for (int i = xmin; i < xmax; i++)
                {

                    currentRoomX = (zy) ? x + i * d : currentRoomX;
                    currentRoomY = (!zy) ? y + i * d : currentRoomY;


                    if (!planeMode)
                    {
                        if (checkRoomNeighbors(currentRoomX, currentRoomY, l, roomType, (secret) ? StationTileType.SecretEntrance : entranceType))
                        {
                            data[currentRoomX, currentRoomY, l] = roomType;
                            stationTileCount++;
                            currentRoomTiles++;


                            //Check if room reached door
                            if (zy && currentRoomX == currentEntranceX && currentRoomY == currentEntranceY + dv)
                            {
                                reachedDoor = true;
                            }

                            else if (!zy && currentRoomY == currentEntranceY && currentRoomX == currentEntranceX + dh)
                            {
                                reachedDoor = true;
                            }


                            if (addPassages && !passageAdded)
                            {
                                //Restrict passages to the end of rooms depending on direction
                                if ((j == ymax - 1 && dv == 1) || (i == xmax - 1 && dh == 1) || (j == ymin && dv == -1) || (i == xmin && dh == -1))
                                {
                                    if ((float)random.NextDouble() < growFromRoom)
                                    {

                                        if (!zy)
                                        {


                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }
                                        else
                                        {

                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }



                                        //passageAdded = true;
                                    }
                                }

                            }



                            lastAddedRooms.Add(new Vector3(currentRoomX, currentRoomY, l));


                        }
                        else
                        {

                            if (currentRoomTiles == 0)
                            {
                                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
                            }


                            if (!reachedDoor)
                                clearInvalidRoom();

                            return;

                        }
                    }
                    else
                    {
                        if (checkRoomNeighbors3D(currentRoomX, currentRoomY, l, roomType, (secret) ? StationTileType.SecretEntrance : entranceType, false))
                        {
                            data[currentRoomX, currentRoomY, l] = roomType;
                            stationTileCount++;
                            currentRoomTiles++;


                            //Check if room reached door
                            if (zy && currentRoomX == currentEntranceX && currentRoomY == currentEntranceY + dv)
                            {
                                reachedDoor = true;
                            }

                            else if (!zy && currentRoomY == currentEntranceY && currentRoomX == currentEntranceX + dh)
                            {
                                reachedDoor = true;
                            }


                            if (addPassages && !passageAdded)
                            {
                                //Restrict passages to the end of rooms depending on direction
                                if ((j == ymax - 1))
                                {
                                    if ((float)random.NextDouble() < growFromRoom)
                                    {

                                        if (!zy)
                                        {


                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }
                                        else
                                        {

                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }



                                        //passageAdded = true;
                                    }
                                }

                            }


                            lastAddedRooms.Add(new Vector3(currentRoomX, currentRoomY, l));


                        }
                        else
                        {

                            if (currentRoomTiles == 0)
                            {
                                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
                            }


                            if (!reachedDoor)
                                clearInvalidRoom();

                            return;

                        }
                    }

                    if (messyMode)
                    {
                        xmin = -MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                        xmax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                    }



                }

                currentRoomX += dh;
                currentRoomY += dv;

                if (messyMode)
                {

                    ymax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                }

            }

            if (currentRoomTiles == 0)
            {
                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
            }

        }

        public void placeRandomTallRoom(int x, int y, int l, int dh, int dv, int ceilingHeight, StationTileType roomType, StationTileType entranceType, int minsize, int maxsize, bool messyMode, bool showoffmode, bool addPassages, bool planeMode, bool trigExec, bool secret, float growFromRoom)
        {

            bool reachedDoor = false, passageAdded = false, endLayout = false;
            lastAddedRooms = new List<Vector3D>();

            currentRoomTiles = 0;

            List<Vector3D> firstFloor = new List<Vector3D>();

            //Out of bounds
            if (x < 0 || x >= width || y < 0 || y >= height)
                return;

            //Assign direction bound variables
            int d = (dh == 0) ? dv : dh;
            bool zy = (d == dv);

            //Place initial entrance
            currentRoomX = x + dh;
            currentRoomY = y + dv;

            currentEntranceX = currentRoomX;
            currentEntranceY = currentRoomY;

            if (!planeMode)
            {
                if (checkEntranceNeighbors(currentEntranceX, currentEntranceY, l, roomType) && checkRoomNeighbors(currentRoomX + dh, currentRoomY + dv, l, roomType, entranceType))
                {
                    data[currentEntranceX, currentEntranceY, l] = (secret) ? StationTileType.SecretEntrance : entranceType;
                    lastAddedRooms.Add(new Vector3(currentEntranceX, currentEntranceY, l));
                }
                else
                {
                    return;
                }
            }
            else
            {
                if (checkEntranceNeighbors3D(currentEntranceX, currentEntranceY, l, roomType) && checkRoomNeighbors3D(currentRoomX + dh, currentRoomY + dv, l, roomType, entranceType, false))
                {
                    data[currentEntranceX, currentEntranceY, l] = (secret) ? StationTileType.SecretEntrance : entranceType;
                    lastAddedRooms.Add(new Vector3(currentEntranceX, currentEntranceY, l));
                }
                else
                {
                    return;
                }
            }


            currentRoomX += dh;
            currentRoomY += dv;

            int ymin = -MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int ymax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int xmin = -MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
            int xmax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);

            for (int j = ymin; j < ymax; j++)
            {
                //Generate room line
                for (int i = xmin; i < xmax; i++)
                {

                    currentRoomX = (zy) ? x + i * d : currentRoomX;
                    currentRoomY = (!zy) ? y + i * d : currentRoomY;


                    if (!planeMode)
                    {
                        if (checkRoomNeighbors(currentRoomX, currentRoomY, l, roomType, (secret) ? StationTileType.SecretEntrance : entranceType))
                        {
                            //Room tile added
                            data[currentRoomX, currentRoomY, l] = roomType;
                            stationTileCount++;
                            currentRoomTiles++;
                            firstFloor.Add(new Vector3(currentRoomX, currentRoomY, l));

                            //Check if room reached door
                            if (zy && currentRoomX == currentEntranceX && currentRoomY == currentEntranceY + dv)
                            {
                                reachedDoor = true;
                            }

                            else if (!zy && currentRoomY == currentEntranceY && currentRoomX == currentEntranceX + dh)
                            {
                                reachedDoor = true;
                            }


                            //Add secret passages
                            if (addPassages && !passageAdded)
                            {

                                //Restrict passages to the end of rooms depending on direction
                                if ((j == ymax - 1 && dv == 1) || (i == xmax - 1 && dh == 1) || (j == ymin && dv == -1) || (i == xmin && dh == -1))
                                {
                                    if ((float)random.NextDouble() < growFromRoom)
                                    {

                                        if (!zy)
                                        {


                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }
                                        else
                                        {

                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }



                                        //passageAdded = true;
                                    }
                                }

                            }


                            lastAddedRooms.Add(new Vector3(currentRoomX, currentRoomY, l));


                        }
                        else
                        {

                            if (currentRoomTiles == 0)
                            {
                                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
                            }


                            if (!reachedDoor)
                                clearInvalidRoom();

                            endLayout = true;
                            break;


                        }
                    }
                    else
                    {
                        if (checkRoomNeighbors3D(currentRoomX, currentRoomY, l, roomType, (secret) ? StationTileType.SecretEntrance : entranceType, false))
                        {
                            data[currentRoomX, currentRoomY, l] = roomType;
                            stationTileCount++;
                            currentRoomTiles++;
                            firstFloor.Add(new Vector3(currentRoomX, currentRoomY, l));

                            //Check if room reached door
                            if (zy && currentRoomX == currentEntranceX && currentRoomY == currentEntranceY + dv)
                            {
                                reachedDoor = true;
                            }

                            else if (!zy && currentRoomY == currentEntranceY && currentRoomX == currentEntranceX + dh)
                            {
                                reachedDoor = true;
                            }


                            //Add secret passages
                            if (addPassages && !passageAdded)
                            {

                                //Restrict passages to the end of rooms depending on direction
                                if ((j == ymax - 1))
                                {
                                    if ((float)random.NextDouble() < growFromRoom)
                                    {

                                        if (!zy)
                                        {


                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX + d, currentRoomY, l), (dh > 0) ? Orientation.Eastbound : Orientation.Westbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }
                                        else
                                        {

                                            if (!currentlyProcessingTriggers)
                                                pgtriggers.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));
                                            else
                                                pgtriggersProcessingList.Add(new PGTrigger(new Vector3(currentRoomX, currentRoomY + d, l), (dv > 0) ? Orientation.Northbound : Orientation.Southbound, PGTrigger.TYPE_ADDSECRETCORRIDOR, trigExec));


                                            passageAdded = true;


                                        }



                                        //passageAdded = true;
                                    }
                                }

                            }

                            lastAddedRooms.Add(new Vector3(currentRoomX, currentRoomY, l));


                        }
                        else
                        {

                            if (currentRoomTiles == 0)
                            {
                                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
                            }


                            if (!reachedDoor)
                                clearInvalidRoom();

                            endLayout = true;
                            break;

                        }
                    }


                    if (messyMode)
                    {
                        xmin = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                        xmax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                    }


                }

                if (endLayout)
                    break;

                currentRoomX += dh;
                currentRoomY += dv;

                if (messyMode)
                {
                    ymax = MathHelper.Clamp(random.Next(maxsize) + minsize, minsize, maxsize);
                }

            }

            if (currentRoomTiles == 0)
            {
                data[currentEntranceX, currentEntranceY, l] = StationTileType.Empty;
            }

            //Attempt to create tall ceiling
            foreach (Vector3D v in firstFloor)
            {

                for (int flr = l + 1; flr < l + ceilingHeight; flr++)
                {

                    if (!outOfBounds((int)v.X, (int)v.Y, flr))
                    {
                        if (checkRoomNeighbors3D((int)v.X, (int)v.Y, flr, roomType, (secret) ? StationTileType.SecretEntrance : entranceType, true))
                        {
                            currentNeighborhood = getNeighbors3D((int)v.X, (int)v.Y, flr);

                            if (currentNeighborhood.above == StationTileType.Empty || currentNeighborhood.above == StationTileType.ERROR)
                            {
                                data[(int)v.X, (int)v.Y, flr] = roomType;
                            }
                            else
                            {
                                break;
                            }

                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }




                }

            }

        }

        public bool placeStaircase(int x, int y, int l, int dl, bool showoff)
        {


            //Check if placing staircase is possible
            if (x + 1 > width || x - 1 < 0 || y + 1 > height || y - 1 < 0)
                return false;

            if (((dl > 0) && (l >= levels - 1)) || (dl < 0 && (l <= 0)))
                return false;

            if (!canPlaceStaircase(x, y, l, dl))
                return false;

            applyStaircase(x, y, l, dl, showoff);
            return true;

        }

        public void applyStaircase(int x, int y, int l, int dl, bool showoff)
        {

            data[x - 1, y + 1, l] = StationTileType.Staircase;
            data[x, y + 1, l] = StationTileType.Staircase;
            data[x + 1, y + 1, l] = StationTileType.Staircase;
            data[x - 1, y, l] = StationTileType.Staircase;
            data[x, y, l] = (dl < 0) ? StationTileType.Staircase : StationTileType.Stairs;
            data[x + 1, y, l] = StationTileType.Staircase;
            data[x - 1, y - 1, l] = StationTileType.Staircase;
            data[x, y - 1, l] = StationTileType.Staircase;
            data[x + 1, y - 1, l] = StationTileType.Staircase;

            data[x - 1, y + 1, l + dl] = StationTileType.Staircase;
            data[x, y + 1, l + dl] = StationTileType.Staircase;
            data[x + 1, y + 1, l + dl] = StationTileType.Staircase;
            data[x - 1, y, l + dl] = StationTileType.Staircase;
            data[x, y, l + dl] = (dl < 0) ? StationTileType.Stairs : StationTileType.Staircase;
            data[x + 1, y, l + dl] = StationTileType.Staircase;
            data[x - 1, y - 1, l + dl] = StationTileType.Staircase;
            data[x, y - 1, l + dl] = StationTileType.Staircase;
            data[x + 1, y - 1, l + dl] = StationTileType.Staircase;

            stationTileCount += 18;

        }

        public bool addStaircaseV2(int x, int y, int l, int dh, int dv, int dl)
        {

            if ((x + dh) < 0 || (x + dh) > width || (y + dv) < 0 || (y + dv) > height || (l + dl) < 0 || (l + dl) > levels)
            {
                return false;
            }

            if (staircaseCheckV2(x, y, l, IgnoreNeighborhood.CorridorSelfAvoidPlanarIgnore(toOrientation(dh, dv))) && staircaseCheckV2(x + dh, y + dv, l, IgnoreNeighborhood.NO_IGNORE) && staircaseCheckV2(x, y, l + dl, IgnoreNeighborhood.NO_IGNORE) && staircaseCheckV2(x + dh, y + dv, l + dl, IgnoreNeighborhood.NO_IGNORE))
            {
                if (dl > 0)
                {
                    data[x, y, l] = StationTileType.Staircase;
                    data[x + dh, y + dv, l] = StationTileType.Stairs;
                    data[x, y, l + dl] = StationTileType.Staircase;
                    data[x + dh, y + dv, l + dl] = StationTileType.Staircase;
                }
                else
                {
                    data[x, y, l] = StationTileType.Staircase;
                    data[x + dh, y + dv, l] = StationTileType.Staircase;
                    data[x, y, l + dl] = StationTileType.Stairs;
                    data[x + dh, y + dv, l + dl] = StationTileType.Staircase;
                }
                return true;
            }
            return false;

        }

        public void placeModelV2(CorridorInfo info, StationModel m)
        {
            //Dereference model info to avoid corruption.
            StationModel model = StationModel.copy(m);

            int sx, sy, sl, cx = 0, cy = 0, cl = 0, tx, ty, tl; //indices

            //SX,SY,SL are where the 0,0,0 point of the model are on the data
            //CX,CY,CL are where the current X,Y,Z of the model are on the data
            //TX,TY,TL are where the current X,Y,Z of a TRIGGER are on the data



            Orientation modelOrientation = toOrientation(info.dh, info.dv);
            Orientation triggerOrientation;

            //Process model tile groups if any are present
            if (model.tileGroups.groupCount > 0)
                model.model = StationModel.getTGProcessedLayout(model, random);

            model = StationModel.rotate(model, modelOrientation);

            currentModelCount = currentMapInfo.GetStationModelCount(model.type);

            bool isBoss = false;

            sx = info.x - model.ox;
            sy = info.y - model.oy;
            sl = info.l - model.ol;

            //First loop to make sure the space is available in non priority mode



            for (int i = 0; i < model.width; i++)
            {
                for (int j = 0; j < model.height; j++)
                {
                    for (int k = 0; k < model.depth; k++)
                    {



                        cx = sx + i;
                        cy = sy + j;
                        cl = sl + k;

                        //Return if out of bounds
                        if (cx < 0 || cx >= width || cy < 0 || cy >= height || cl < 0 || cl >= levels)
                        {
                            return;
                        }



                        //Get neighbors
                        currentNeighborhood = getNeighbors3D(cx, cy, cl);

                        //Return if space check is false and priority mode isnt on
                        if (info.planeMode)
                        {
                            if (!model.spaceCheck3D(data[cx, cy, cl], currentNeighborhood.above, currentNeighborhood.below) && !info.priority)
                            {
                                return;
                            }
                        }
                        else
                        {
                            if (data[cx, cy, cl] != StationTileType.Empty && !info.priority)
                                return;
                        }


                        //Finally check all neighbors if required by model
                        foreach (StationTileType tt in model.typesToCheck)
                        {
                            if (model.model[i, j, k] == tt)
                            {

                                if (!info.planeMode)
                                {
                                    if (!model.typeToCheck(currentNeighborhood.north, currentNeighborhood.south, currentNeighborhood.east, currentNeighborhood.west))
                                        return;
                                }
                                else
                                {
                                    if (!model.typeToCheck3D(currentNeighborhood.north, currentNeighborhood.south, currentNeighborhood.east, currentNeighborhood.west, currentNeighborhood.above, currentNeighborhood.below))
                                        return;
                                }


                            }
                        }


                    }
                }
            }

            //Model will be placed, increment count value

            currentMapInfo.IncrementStationModelCount(model.type);


            //Second loop to place the model, if the space is fully available ( or if priority mode is on )
            for (int i = 0; i < model.width; i++)
            {
                for (int j = 0; j < model.height; j++)
                {
                    for (int k = 0; k < model.depth; k++)
                    {

                        cx = sx + i;
                        cy = sy + j;
                        cl = sl + k;

                        if (!isBoss && model.model[i, j, k] == StationTileType.BossRoom)
                            isBoss = true;

                        data[cx, cy, cl] = model.model[i, j, k];

                    }
                }
            }

            //New trigger processing algorithm
            //Generalized for all trigger type, orientation and position

            if (model.triggers != null)
            {

                //Debug.Log("Triggers Found!");

                foreach (PGTrigger trig in model.triggers)
                {

                    //Initialize by setting trigger position in data
                    tx = sx + (int)trig.pos.X;
                    ty = sy + (int)trig.pos.Y;
                    tl = sl + (int)trig.pos.Z;

                    //Debug.Log("Trigger Position On Map : " + tx + "," + ty + "," + tl);

                    //First calculate the correct orientation
                    triggerOrientation = rotateOrientation(trig.orientation, modelOrientation);

                    //Create trigger obj
                    currentModelTrigger = new PGTrigger(new Vector3(tx, ty, tl), triggerOrientation, trig.type, trig.iexec);

                    //Then add the new trigger with it's data on the map if it's intended for Post Generation Execution
                    if (!trig.iexec && !currentlyProcessingTriggers)
                        pgtriggers.Add(currentModelTrigger);
                    else if (!trig.iexec && currentlyProcessingTriggers)
                    {
                        pgtriggersProcessingList.Add(currentModelTrigger);
                    }
                    else
                    {//Otherwise execute the trigger immediately
                        //Debug.Log("Trigger Immediately Executed!");
                        processTrigger(currentModelTrigger, info);
                    }



                }

            }
            else
            {
                if (model.triggers == null)
                {
                    //Debug.Log("Model Array is NULL!");
                }


                if (currentlyProcessingTriggers)
                {
                    //Debug.Log("Currently Processing Triggers!");
                }


            }




        }
        #endregion

        #region Cleanup

        public void clearInaccessibleAreas()
        {

            currentAccessData = getAccessMap(new Vector3(sx, sy, sl));

            inaccessibleTileCount = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {
                        if (currentAccessData[i, j, l] == ALVL_INACCESSIBLE)
                        {
                            data[i, j, l] = StationTileType.Empty;
                            inaccessibleTileCount++;
                        }
                    }
                }
            }

        }

        public byte[,,] getAccessMap(Vector3D start)
        {
            byte[,,] aMap = new byte[width, height, levels];

            //Finalized list of tiles that should be set to ENABLED
            List<Vector3D> finalTiles = new List<Vector3D>();//Consider making this list STATIC to save some RAM

            //Reference to tiles found in last step, used to process next step faster
            List<Vector3D> lastStepTiles = new List<Vector3D>();

            List<Vector3D> nextStepTiles = new List<Vector3D>();

            Vector3D current;

            StationTileNeighborhood neighbors;

            finalTiles.Add(start);//Add origin point to both lists
            lastStepTiles.Add(start);

            //Loop for every distance value possible
            do
            {


                foreach (Vector3D tile in lastStepTiles)
                {
                    neighbors = getNeighbors3D((int)tile.X, (int)tile.Y, (int)tile.Z);

                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.north))
                    {
                        current = new Vector3D(tile.X, tile.Y + 1, tile.Z);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }

                    }
                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.south))
                    {
                        current = new Vector3D(tile.X, tile.Y - 1, tile.Z);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }
                    }
                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.west))
                    {
                        current = new Vector3D(tile.X - 1, tile.Y, tile.Z);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }
                    }
                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.east))
                    {
                        current = new Vector3D(tile.X + 1, tile.Y, tile.Z);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }
                    }
                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.above))
                    {
                        current = new Vector3D(tile.X, tile.Y, tile.Z + 1);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }
                    }
                    if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.NONEMPTY, neighbors.below))
                    {
                        current = new Vector3D(tile.X, tile.Y, tile.Z - 1);
                        if (!finalTiles.Contains(current))
                        {
                            finalTiles.Add(current);
                            nextStepTiles.Add(current);
                        }
                    }


                }

                //Swap and clean nextStepTiles list
                lastStepTiles = nextStepTiles;
                nextStepTiles = new List<Vector3D>();
            } while (lastStepTiles.Count > 0);


            //Create access level map
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {

                        if (data[i, j, l] == StationTileType.Empty)
                        {
                            aMap[i, j, l] = ALVL_OUTSIDE;
                            continue;
                        }


                        if (finalTiles.Contains(new Vector3D(i, j, l)))
                        {
                            aMap[i, j, l] = ALVL_MAIN;
                        }
                        else
                        {
                            aMap[i, j, l] = ALVL_INACCESSIBLE;
                        }

                    }
                }
            }

            return aMap;


        }

        public void removeFoundDeadends(Vector3D[] deadends)
        {
            foreach (Vector3D v in deadends)
            {
                data[(int)v.X, (int)v.Y, (int)v.Z] = StationTileType.Empty;
                stationTileCount--;
            }
        }

        public void removeAllDeadends()
        {

            Vector3D[] cd;

            while ((cd = findDeadends()).Length > 0)
            {
                removeFoundDeadends(cd);
            }

            while (findAndFixSecretEntrances() > 0) ;

        }

        public int findAndFixSecretEntrances()
        {
            StationTileType current;
            StationTileType roomType = StationTileType.Empty;
            int fix = 0;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {

                        int cbnc = 0, cgnc = 0;

                        current = data[i, j, l];

                        if (current == StationTileType.SecretCorridor || current == StationTileType.SecretEntrance)
                        {
                            currentNeighborhood = getNeighborsFull(i, j, l);

                            //Checking room neighbors
                            if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.ANYROOM, currentNeighborhood.north))
                            {
                                cbnc++;
                                roomType = currentNeighborhood.north;
                            }

                            if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.ANYROOM, currentNeighborhood.south))
                            {
                                cbnc++;
                                roomType = currentNeighborhood.south;
                            }

                            if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.ANYROOM, currentNeighborhood.east))
                            {
                                cbnc++;
                                roomType = currentNeighborhood.east;
                            }

                            if (StationTileTypeUtils.IsTileTypeMatching(StationTileType.ANYROOM, currentNeighborhood.west))
                            {
                                cbnc++;
                                roomType = currentNeighborhood.west;
                            }



                            //Turn secret into roomtype if more than 1 are found
                            if (cbnc > 1)
                            {
                                data[i, j, l] = roomType;
                                fix++;
                                continue;
                            }

                            //Now checking for secret corridor that could be turned into an entrance
                            if (current == StationTileType.SecretCorridor && cbnc > 0)
                            {
                                data[i, j, l] = StationTileType.SecretEntrance;
                                fix++;
                            }


                        }
                    }
                }
            }
            return fix;
        }

        public Vector3D[] findDeadends()
        {

            List<Vector3D> deadends = new List<Vector3D>();
            StationTileType current;


            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    for (int l = 0; l < levels; l++)
                    {

                        current = data[i, j, l];

                        if (!(current == StationTileType.Corridor || current == StationTileType.SecretCorridor || current == StationTileType.Room || current == StationTileType.Stairs || current == StationTileType.Staircase || current == StationTileType.Room2 || current == StationTileType.Entrance || current == StationTileType.Entrance2 || current == StationTileType.SecretEntrance || current == StationTileType.EntranceBoss || current == StationTileType.EntranceCloset))
                            continue;


                        cnc = 0;//Current direct count
                        cdnc = 0;//Current diagonal count
                        gcnc = 0;//Goal neighbor count

                        if (current != StationTileType.Stairs && current != StationTileType.Staircase)
                        {
                            currentNeighborhood = getNeighborsFull(i, j, l);

                            if (currentNeighborhood.north != StationTileType.Empty && currentNeighborhood.north != StationTileType.ERROR)
                                cnc++;
                            if (currentNeighborhood.south != StationTileType.Empty && currentNeighborhood.south != StationTileType.ERROR)
                                cnc++;
                            if (currentNeighborhood.east != StationTileType.Empty && currentNeighborhood.east != StationTileType.ERROR)
                                cnc++;
                            if (currentNeighborhood.west != StationTileType.Empty && currentNeighborhood.west != StationTileType.ERROR)
                                cnc++;



                            if (cnc < 2 && (current == StationTileType.Corridor || current == StationTileType.SecretCorridor || current == StationTileType.Entrance || current == StationTileType.Entrance2 || current == StationTileType.EntranceBoss || current == StationTileType.SecretEntrance))
                                deadends.Add(new Vector3D(i, j, l));
                            else if (cnc < 1 && (current == StationTileType.Room || current == StationTileType.Room2))
                            {
                                deadends.Add(new Vector3D(i, j, l));
                            }
                            else if (cnc != 2 && (current == StationTileType.Entrance || current == StationTileType.Entrance2 || current == StationTileType.SecretEntrance || current == StationTileType.EntranceBoss))
                            {
                                //deadends.Add(new Vector3(i, j, l));
                            }

                            //Special clause to find large corridor deadends
                            //THE && INSTEAD OF || IS TO MAKE SURE WE ARE FINDING LARGE CORRIDORS AND NOT TURNING THIN CORRIDORS
                            if ((current == StationTileType.Corridor || current == StationTileType.SecretCorridor) && cnc == 2)
                            {
                                //First step: identify a possible large corridor deadend by counting connected diagonals

                                if (currentNeighborhood.northwest != StationTileType.Empty && currentNeighborhood.northwest != StationTileType.ERROR)
                                {
                                    if ((currentNeighborhood.north != StationTileType.Empty && currentNeighborhood.north != StationTileType.ERROR) && (currentNeighborhood.west != StationTileType.Empty && currentNeighborhood.west != StationTileType.ERROR))
                                        cdnc++;
                                }

                                if (currentNeighborhood.northeast != StationTileType.Empty && currentNeighborhood.northeast != StationTileType.ERROR)
                                {
                                    if ((currentNeighborhood.north != StationTileType.Empty && currentNeighborhood.north != StationTileType.ERROR) && (currentNeighborhood.east != StationTileType.Empty && currentNeighborhood.east != StationTileType.ERROR))
                                        cdnc++;
                                }

                                if (currentNeighborhood.southwest != StationTileType.Empty && currentNeighborhood.southwest != StationTileType.ERROR)
                                {
                                    if ((currentNeighborhood.south != StationTileType.Empty && currentNeighborhood.south != StationTileType.ERROR) && (currentNeighborhood.west != StationTileType.Empty && currentNeighborhood.west != StationTileType.ERROR))
                                        cdnc++;
                                }

                                if (currentNeighborhood.southeast != StationTileType.Empty && currentNeighborhood.southeast != StationTileType.ERROR)
                                {
                                    if ((currentNeighborhood.south != StationTileType.Empty && currentNeighborhood.south != StationTileType.ERROR) && (currentNeighborhood.east != StationTileType.Empty && currentNeighborhood.east != StationTileType.ERROR))
                                        cdnc++;
                                }

                                //If only a single connected diagonal is found this means we found a possible deadend
                                if (cdnc == 1)
                                {

                                    //Next we find in which direction the diagonal was

                                    //Diagonal is NORTHWEST so the goal tile is WEST of ORIGINAL
                                    if (currentNeighborhood.northwest != StationTileType.Empty && currentNeighborhood.northwest != StationTileType.ERROR)
                                        goalNeighborhood = getNeighborsFull(i - 1, j, l);

                                    //Diagonal is NORTHEAST so goal tile is NORTH of ORIGINAL
                                    if (currentNeighborhood.northeast != StationTileType.Empty && currentNeighborhood.northeast != StationTileType.ERROR)
                                        goalNeighborhood = getNeighborsFull(i, j + 1, l);

                                    //Diagonal is SOUTHWEST so goal tile is SOUTH of ORIGINAL
                                    if (currentNeighborhood.southwest != StationTileType.Empty && currentNeighborhood.southwest != StationTileType.ERROR)
                                        goalNeighborhood = getNeighborsFull(i, j - 1, l);

                                    //Diagonal is SOUTHEAST so goal tile IS EAST of ORIGINAL
                                    if (currentNeighborhood.southeast != StationTileType.Empty && currentNeighborhood.southeast != StationTileType.ERROR)
                                        goalNeighborhood = getNeighborsFull(i + 1, j, l);

                                    //Make sure the goal tile is not empty
                                    if (goalNeighborhood.center != StationTileType.Empty && goalNeighborhood.center != StationTileType.ERROR)
                                    {
                                        //Start counting goal direct neighbors

                                        if (goalNeighborhood.north != StationTileType.Empty && goalNeighborhood.north != StationTileType.ERROR)
                                            gcnc++;

                                        if (goalNeighborhood.south != StationTileType.Empty && goalNeighborhood.south != StationTileType.ERROR)
                                            gcnc++;

                                        if (goalNeighborhood.east != StationTileType.Empty && goalNeighborhood.east != StationTileType.ERROR)
                                            gcnc++;

                                        if (goalNeighborhood.west != StationTileType.Empty && goalNeighborhood.west != StationTileType.ERROR)
                                            gcnc++;

                                        //Remove the large corridor deadend tile
                                        if (gcnc == 2)
                                        {
                                            deadends.Add(new Vector3D(i, j, l));
                                        }

                                    }
                                }


                            }

                        }
                        else
                        {

                            currentNeighborhood = getNeighborsFull(i, j, l);

                            if (current == StationTileType.Staircase)
                            {
                                if (currentNeighborhood.north != StationTileType.Empty && currentNeighborhood.north != StationTileType.ERROR)
                                    cnc++;
                                if (currentNeighborhood.south != StationTileType.Empty && currentNeighborhood.south != StationTileType.ERROR)
                                    cnc++;
                                if (currentNeighborhood.east != StationTileType.Empty && currentNeighborhood.east != StationTileType.ERROR)
                                    cnc++;
                                if (currentNeighborhood.west != StationTileType.Empty && currentNeighborhood.west != StationTileType.ERROR)
                                    cnc++;
                                if (currentNeighborhood.above != StationTileType.Empty && currentNeighborhood.above != StationTileType.ERROR)
                                    cnc++;
                                if (currentNeighborhood.below != StationTileType.Empty && currentNeighborhood.below != StationTileType.ERROR)
                                    cnc++;

                                if (cnc >= 3)
                                {
                                    continue;
                                }
                                else
                                {

                                    cnc = 0;

                                    if (currentNeighborhood.north == StationTileType.Staircase)
                                        cnc++;
                                    if (currentNeighborhood.south == StationTileType.Staircase)
                                        cnc++;
                                    if (currentNeighborhood.east == StationTileType.Staircase)
                                        cnc++;
                                    if (currentNeighborhood.west == StationTileType.Staircase)
                                        cnc++;
                                    if (currentNeighborhood.above == StationTileType.Staircase)
                                        cnc++;
                                    if (currentNeighborhood.below == StationTileType.Staircase)
                                        cnc++;

                                    if (cnc >= 2)
                                        continue;
                                    else
                                        deadends.Add(new Vector3D(i, j, l));

                                }


                            }
                            else if (current == StationTileType.Stairs)
                            {

                                if (currentNeighborhood.north == StationTileType.Staircase)
                                    cnc++;
                                if (currentNeighborhood.south == StationTileType.Staircase)
                                    cnc++;
                                if (currentNeighborhood.east == StationTileType.Staircase)
                                    cnc++;
                                if (currentNeighborhood.west == StationTileType.Staircase)
                                    cnc++;
                                if (currentNeighborhood.above == StationTileType.Staircase)
                                    cnc++;
                                if (currentNeighborhood.below == StationTileType.Staircase)
                                    cnc++;

                                if (cnc >= 2)
                                    continue;
                                else
                                    deadends.Add(new Vector3D(i, j, l));

                            }


                        }
                    }
                }
            }


            return deadends.ToArray();

        }

        #endregion

        #region Utility

        public void clearInvalidRoom()
        {
            foreach (Vector3D v in lastAddedRooms)
            {
                data[(int)v.X, (int)v.Y, (int)v.Z] = StationTileType.Empty;
            }
        }

        public bool outOfBounds(int x, int y, int l)
        {
            return (x >= width || x < 0 || y >= height || y < 0 || l >= levels || l < 0);
        }

        public static Orientation toOrientation(int dx, int dy)
        {
            return (dy == 0) ? ((dx > 0) ? Orientation.Eastbound : Orientation.Westbound) : (dy > 0) ? Orientation.Northbound : Orientation.Southbound;
        }

        static Orientation rotateOrientation(Orientation original, Orientation rotation)
        {

            Orientation ret = original;

            if (rotation == Orientation.Northbound) return original;

            switch (original)
            {
                case Orientation.Northbound:
                    ret = rotation;
                    break;

                case Orientation.Eastbound:
                    ret = (rotation == Orientation.Southbound) ? Orientation.Westbound : (rotation == Orientation.Eastbound) ? Orientation.Southbound : (rotation == Orientation.Westbound) ? Orientation.Northbound : original;
                    break;

                case Orientation.Southbound:
                    ret = (rotation == Orientation.Southbound) ? Orientation.Northbound : (rotation == Orientation.Eastbound) ? Orientation.Westbound : (rotation == Orientation.Westbound) ? Orientation.Eastbound : original;
                    break;

                case Orientation.Westbound:
                    ret = (rotation == Orientation.Southbound) ? Orientation.Eastbound : (rotation == Orientation.Eastbound) ? Orientation.Northbound : (rotation == Orientation.Westbound) ? Orientation.Southbound : original;
                    break;

                default:
                    ret = original;
                    break;


            }

            return ret;
        }

        public static Vector3D toDxDy(Orientation o)
        {
            switch (o)
            {
                case Orientation.Northbound: return new Vector3D(0, 1, 0);
                case Orientation.Eastbound: return new Vector3D(1, 0, 0);
                case Orientation.Westbound: return new Vector3D(-1, 0, 0);
                case Orientation.Southbound: return new Vector3D(0, -1, 0);
                default: return new Vector3D(0, 0, 0);
            }
        }

        #endregion

        #region corridorInfo
        public class CorridorInfo
        {
            public int x, y, l, dh, dv, maxlength, currentCount, cto, mlo, tallRoomMaxHeight, tallRoomMinHeight, roommin, roommax, prevdh, prevdv;
            public float branch, turn, end, room, stairs, model, csavoid, endRooms, tallRooms, largeCorridors, growFromRooms;
            public bool priority, justBranched, showoffmode, singleBranchMode, singleBranchChanceMode, useModels, secret, planeMode, large;

            public CorridorInfo Clone()
            {
                CorridorInfo toret = new CorridorInfo();
                toret.x = x;
                toret.y = y;
                toret.l = l;
                toret.dh = dh;
                toret.dv = dv;
                toret.maxlength = maxlength;
                toret.currentCount = currentCount;
                toret.cto = cto;
                toret.mlo = mlo;
                toret.tallRoomMaxHeight = tallRoomMaxHeight;
                toret.tallRoomMinHeight = tallRoomMinHeight;
                toret.roommin = roommin;
                toret.roommax = roommax;
                toret.prevdh = prevdh;
                toret.prevdv = prevdv;
                toret.branch = branch;
                toret.turn = turn;
                toret.end = end;
                toret.room = room;
                toret.stairs = stairs;
                toret.model = model;
                toret.csavoid = csavoid;
                toret.endRooms = endRooms;
                toret.tallRooms = tallRooms;
                toret.largeCorridors = largeCorridors;
                toret.growFromRooms = growFromRooms;
                toret.priority = priority;
                toret.justBranched = justBranched;
                toret.showoffmode = showoffmode;
                toret.singleBranchMode = singleBranchMode;
                toret.singleBranchChanceMode = singleBranchChanceMode;
                toret.useModels = useModels;
                toret.secret = secret;
                toret.planeMode = planeMode;
                toret.large = large;
                return toret;
            }

        }

        #endregion
    }



}

