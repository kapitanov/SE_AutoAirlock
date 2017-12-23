using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

        private void ExternalDoor(bool open)
        {
            ControlDoors(AL_DOOR_OUTER, open);
        }

        private void InternalDoor(bool open)
        {
            ControlDoors(AL_DOOR_INNER, open);
        }

        private void AuxDoors(bool open)
        {
            ControlDoors(AL_DOOR_AUX, open);
        }

        private void Airvents(bool pressurize)
        {
            var group = GridTerminalSystem.GetBlockGroupWithName(AL_AIRVENTS);
            if (group == null)
            {
                return;
            }

            group.GetBlocks(blocks);
            for (var i = 0; i < blocks.Count; i++)
            {
                ((IMyAirVent)blocks[i]).Depressurize = !pressurize;
            }
            blocks.Clear();
        }

        private void LcdText(ref AirlockStatus status)
        {
            var text = GetStatusText(ref status);

            var group = GridTerminalSystem.GetBlockGroupWithName(AL_LCD);
            if (group == null)
            {
                return;
            }

            group.GetBlocks(blocks);
            for (var i = 0; i < blocks.Count; i++)
            {
                var panel = (IMyTextPanel)blocks[i];
                panel.WritePublicText(text);
                panel.ShowPublicTextOnScreen();
                panel.FontSize = 1.0f;
                panel.Font = "Monospace";
            }
            blocks.Clear();
        }


        private void ControlDoors(string groupName, bool open)
        {
            var group = GridTerminalSystem.GetBlockGroupWithName(groupName);
            if (group == null)
            {
                return;
            }

            group.GetBlocks(blocks);
            for (var i = 0; i < blocks.Count; i++)
            {
                if (open)
                {
                    ((IMyDoor)blocks[i]).OpenDoor();
                }
                else
                {
                    ((IMyDoor)blocks[i]).CloseDoor();
                }
            }
            blocks.Clear();
        }
    }
}
