using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Reflection;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace FixPrefabPermissionsMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Session : MySessionComponentBase
    {
        HashSet<long> _nonPlayerFactionMembers = new HashSet<long>();
        HashSet<long> _trackedGrids = new HashSet<long>();

        public override void LoadData()
        {
            MyAPIGateway.Entities.OnEntityAdd += Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove += Entities_OnEntityRemove;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Entities.OnEntityAdd -= Entities_OnEntityAdd;
            MyAPIGateway.Entities.OnEntityRemove -= Entities_OnEntityRemove;
        }

        private void Entities_OnEntityAdd(VRage.ModAPI.IMyEntity obj)
        {
            if (IsNpcStation(obj))
            {
                TrackGrid(obj as IMyCubeGrid);
            }
        }

        private void Entities_OnEntityRemove(IMyEntity obj)
        {
            if (_trackedGrids.Contains(obj.EntityId))
            {
                UnTrackGrid(obj as IMyCubeGrid);
            }
        }

        private void UpdateNonPlayerFactionMembers()
        {
            foreach (var faction in MyAPIGateway.Session.Factions.Factions.Values)
            {
                if (faction.AcceptHumans)
                {
                    continue;
                }

                foreach (var member in faction.Members.Keys)
                {
                    if (_nonPlayerFactionMembers.Add(member))
                    {
                        Log("NPC faction " + faction.Name + ", member " + member);
                    }
                }
            }
        }

        private bool IsNpcStation(IMyEntity entity)
        {
            if (!(entity is IMyCubeGrid))
            {
                return false;
            }

            var grid = entity as IMyCubeGrid;
            if (!grid.IsStatic)
            {
                return false;
            }

            UpdateNonPlayerFactionMembers();
            foreach (var owner in grid.BigOwners)
            {
                var isNpcGrid = _nonPlayerFactionMembers.Contains(owner);
                if (!isNpcGrid)
                {
                    continue;
                }

                return HasStoreBlock(grid);
            }

            return false;
        }

        private bool HasStoreBlock(IMyCubeGrid grid)
        {
            foreach (var block in (grid as MyCubeGrid).GetFatBlocks())
            {
                if (block is IMyStoreBlock)
                {
                    return true;
                }
            }
            return false;
        }

        private void TrackGrid(IMyCubeGrid grid)
        {
            Log("Tracking grid " + grid.Name + " (" + grid.EntityId + ")");

            _trackedGrids.Add(grid.EntityId);
            grid.OnIsStaticChanged += OnIsStaticChanged;
        }

        private void UnTrackGrid(IMyCubeGrid grid)
        {
            Log("Untracking grid " + grid.Name + " (" + grid.EntityId + ")");

            _trackedGrids.Remove(grid.EntityId);
            grid.OnIsStaticChanged -= OnIsStaticChanged;
        }

        private void OnIsStaticChanged(IMyCubeGrid grid, bool isStatic)
        {
            if (!isStatic)
            {
                Log("Disallow convert to ship for grid " + grid.Name + " (" + grid.EntityId + ")");
                ((MyCubeGrid)grid).ConvertToStatic();
            }
        }

        private void Log(string s)
        {
            MyLog.Default.WriteLine("FixPrefabPermissions: " + s);
        }
    }
}
