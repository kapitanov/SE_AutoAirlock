using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Text;

namespace IngameScript
{
    partial class Program
    {
        private string GetStatusText(ref AirlockStatus status)
        {
            var sb = new StringBuilder();
            {
                sb.AppendLine("СОСТОЯНИЕ");
                var time = DateTime.UtcNow - lastTime;

                switch (state)
                {
                    case State.IDLE:
                        sb.AppendLine("  Готов");
                        break;
                    case State.COOLDOWN:
                        sb.AppendFormat("  Подождите ({0} сек)", COOLDOWN_TIME - (int)time.TotalSeconds);
                        sb.AppendLine();
                        break;
                    case State.ENTER_DEPRESSURIZING:
                        sb.AppendFormat("  Сброс давления ({0} сек)", (int)time.TotalSeconds);
                        sb.AppendLine();
                        break;
                    case State.ENTER_DEPRESSURIZED:
                        sb.AppendLine("  Внеш. шлюз открыт");
                        sb.AppendLine();
                        break;
                    case State.ENTER_OUTER_CLOSING:
                        sb.AppendLine("  Герметизация");
                        sb.AppendLine();
                        break;
                    case State.ENTER_PRESSURIZING:
                        sb.AppendFormat("  Набор давления ({0} сек)", (int)time.TotalSeconds);
                        sb.AppendLine();
                        break;
                    case State.ENTER_PRESSURIZED:
                        sb.AppendLine("  Внутр. шлюз открыт");
                        break;

                    case State.EXIT_INNER_OPEN:
                        sb.AppendLine("  Внутр. шлюз открыт");
                        break;
                    case State.EXIT_INNER_CLOSING:
                        sb.AppendLine("  Закрытие дверей");
                        break;
                    case State.EXIT_DEPRESSURIZING:
                        sb.AppendFormat("  Сброс давления ({0} сек)", (int)time.TotalSeconds);
                        sb.AppendLine();
                        break;
                    case State.EXIT_OUTER_OPEN:
                        sb.AppendLine("  Внеш. шлюз открыт");
                        break;
                }
            }

            status.ExternalDoors.Print(sb, "ВНЕШ. ДВЕРЬ");
            status.InternalDoors.Print(sb, "ВНУТР. ДВЕРЬ");
            status.AirVents.Print(sb, "ДАВЛЕНИЕ");

            if (!string.IsNullOrEmpty(descr))
            {
                sb.AppendLine("ВНИМАНИЕ");
                sb.Append("  ");
                sb.AppendLine(descr);
            }

            return sb.ToString();
        }


        struct AirlockStatus
        {
            public AirlockAirventStatus AirVents;
            public AirlockDoorStatus ExternalDoors;
            public AirlockDoorStatus InternalDoors;
        }

        struct AirlockAirventStatus
        {
            public float OxygenLevel;
            public VentStatus Status;
            public int WorkingCount;
            public int DamagedCount;
            public bool IsDepressurized;
            public bool IsPressurized;

            public void Print(StringBuilder sb, string title)
            {
                sb.AppendLine(title);
                switch (Status)
                {
                    case VentStatus.Depressurized:
                        sb.AppendLine("  Вакуум");
                        sb.AppendLine();
                        break;
                    case VentStatus.Depressurizing:
                        sb.AppendFormat("  {0:F2,-4} атм (сброс)", OxygenLevel);
                        sb.AppendLine();
                        break;
                    case VentStatus.Pressurized:
                        sb.AppendFormat("  {0:F2,-4} атм", OxygenLevel);
                        sb.AppendLine();
                        break;
                    case VentStatus.Pressurizing:
                        sb.AppendFormat("  {0:F2,-4} атм (набор)", OxygenLevel);
                        sb.AppendLine();
                        break;
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

            public void Print(StringBuilder sb, string title)
            {
                sb.AppendLine(title);
                switch (Status)
                {
                    case DoorStatus.Opening:
                        sb.AppendFormat("  Открывается {0:F0}%", OpenRatio * 100);
                        sb.AppendLine();
                        break;
                    case DoorStatus.Open:
                        sb.AppendLine("  Открыта");
                        break;
                    case DoorStatus.Closing:
                        sb.AppendFormat("  Закрывается {0:F0}%.", 100 - OpenRatio * 100);
                        sb.AppendLine();
                        break;
                    case DoorStatus.Closed:
                        sb.AppendLine("  Закрыта");
                        break;
                }

                if (DamagedCount > 0)
                {
                    if (WorkingCount == 0)
                    {
                        sb.AppendLine("  ! Необходим ремонт");
                    }
                    else
                    {
                        sb.AppendLine("  ! Неисправность");
                    }
                }
            }
        }

        private void GetStatus(out AirlockStatus status)
        {
            status = new AirlockStatus();

            GetStatus(AL_DOOR_INNER, ref status.InternalDoors);
            GetStatus(AL_DOOR_OUTER, ref status.ExternalDoors);
            GetStatus(AL_AIRVENTS, ref status.AirVents);
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
                var airvent = (IMyAirVent)blocks[i];

                status.Status = airvent.Status;
                status.OxygenLevel += airvent.GetOxygenLevel();

                if (airvent.IsFunctional)
                {
                    status.WorkingCount++;
                }
                else
                {
                    status.DamagedCount++;
                }
            }

            status.OxygenLevel /= blocks.Count;
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
                var door = (IMyDoor)blocks[i];

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
