using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;

namespace IngameScript
{
    partial class Program
    {
        struct AirlockStatus
        {
            public AirlockAirventStatus AirVents;
            public AirlockDoorStatus ExternalDoors;
            public AirlockDoorStatus InternalDoors;
            public bool IsObstructed;
        }

        struct AirlockAirventStatus
        {
            public float OxygenLevel;
            public VentStatus Status;
            public int WorkingCount;
            public int DamagedCount;
            public bool IsDepressurized;
            public bool IsAirtight;
            public bool IsPressurized;

            public void Print(Printer sb, string title)
            {
                sb.Title(title);
                switch (Status)
                {
                    case VentStatus.Depressurized:
                        sb.Value(CHAR_RED, "Вакуум");
                        break;
                    case VentStatus.Depressurizing:
                        sb.ValuePressure(CHAR_CYAN, OxygenLevel, "сброс");
                        break;
                    case VentStatus.Pressurized:
                        sb.ValuePressure(CHAR_GREEN, OxygenLevel);
                        break;
                    case VentStatus.Pressurizing:
                        sb.ValuePressure(CHAR_YELLOW, OxygenLevel, "набор");
                        break;
                }

                if (DamagedCount > 0)
                {
                    if (WorkingCount == 0)
                    {
                        sb.Value(CHAR_RED, "Необходим ремонт");
                    }
                    else
                    {
                        sb.Value(CHAR_RED, "Неисправность");
                    }
                }
            }
        }

        struct AirlockDoorStatus
        {
            public float OpenRatio;
            public DoorStatus Status;
            public bool IsOpen;
            public bool IsClosed;
            public int WorkingCount;
            public int DamagedCount;

            public void Print(Printer sb, string title)
            {
                sb.Title(title);
                switch (Status)
                {
                    case DoorStatus.Opening:
                        sb.ValueProgress(CHAR_YELLOW, "Открывается", OpenRatio);
                        break;
                    case DoorStatus.Open:
                        sb.Value(CHAR_RED, "Открыта");
                        break;
                    case DoorStatus.Closing:
                        sb.ValueProgress(CHAR_YELLOW, "Закрывается", 1 - OpenRatio);
                        break;
                    case DoorStatus.Closed:
                        sb.Value(CHAR_GREEN, "Закрыта");
                        break;
                }

                if (DamagedCount > 0)
                {
                    if (WorkingCount == 0)
                    {
                        sb.Value(CHAR_RED, "Необходим ремонт");
                    }
                    else
                    {
                        sb.Value(CHAR_RED, "Неисправность");
                    }
                }
            }
        }

        private void GetStatus(out AirlockStatus status)
        {
            status = new AirlockStatus();

            status.IsObstructed = GetObstructionStatus();
            GetStatus(AL_DOOR_INNER, ref status.InternalDoors);
            GetStatus(AL_DOOR_OUTER, ref status.ExternalDoors);
            GetStatus(AL_AIRVENTS, ref status.AirVents);
        }

        private bool GetObstructionStatus()
        {
            var group = GridTerminalSystem.GetBlockGroupWithName(AL_SENSORS);
            if (group == null)
            {
                return false;
            }
            group.GetBlocks(blocks);

            try
            {
                for (var i = 0; i < blocks.Count; i++)
                {
                    var sensor = blocks[i] as IMySensorBlock;
                    if (sensor != null)
                    {
                        if (sensor.IsActive)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
            finally
            {
                blocks.Clear();
            }
        }

        private void GetStatus(string groupName, ref AirlockAirventStatus status)
        {
            status.WorkingCount = 0;
            status.DamagedCount = 0;
            status.OxygenLevel = 0;
            status.Status = VentStatus.Pressurized;

            var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group == null)
            {
                return;
            }
            group.GetBlocks(blocks);

            for (var i = 0; i < blocks.Count; i++)
            {
                var airvent = blocks[i] as IMyAirVent;
                if (airvent == null)
                {
                    continue;
                }

                status.Status = airvent.Status;
                status.OxygenLevel = Math.Max(status.OxygenLevel, airvent.GetOxygenLevel());
                status.IsAirtight = airvent.CanPressurize;

                if (airvent.IsFunctional)
                {
                    status.WorkingCount++;
                }
                else
                {
                    status.DamagedCount++;
                }
            }

            blocks.Clear();

            status.IsPressurized = status.OxygenLevel >= PRESSURIZE_THRESHOLD;
            status.IsDepressurized = status.OxygenLevel <= DEPRESSURIZE_THRESHOLD;
        }

        private void GetStatus(string groupName, ref AirlockDoorStatus status)
        {
            status.WorkingCount = 0;
            status.DamagedCount = 0;
            status.OpenRatio = 0;
            status.Status = DoorStatus.Closed;

            var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group == null)
            {
                return;
            }
            group.GetBlocks(blocks);

            for (var i = 0; i < blocks.Count; i++)
            {
                var door = blocks[i] as IMyDoor;
                if (door == null)
                {
                    continue;
                }

                status.Status = door.Status;
                status.OpenRatio = door.OpenRatio;

                if (door.IsFunctional)
                {
                    status.WorkingCount++;
                }
                else
                {
                    status.DamagedCount++;
                }
            }
            blocks.Clear();

            status.IsOpen = status.OpenRatio >= 1;
            status.IsClosed = status.OpenRatio <= 0;
        }
    }
}
