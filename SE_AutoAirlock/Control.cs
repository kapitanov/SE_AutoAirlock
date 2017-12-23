using Sandbox.ModAPI.Ingame;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();

        interface IController
        {
            void ExternalDoor(bool open);
            void InternalDoor(bool open);
            void AuxDoors(bool open);
            void Airvents(bool pressurize);
            void Lights(LightMode mode);
            void LcdText(string text);
        }

        enum LightMode { Off, Blink, On, Red }

        class Controller : IController
        {
            private readonly List<IMyTerminalBlock> blocks = new List<IMyTerminalBlock>();
            private readonly IMyGridTerminalSystem GridTerminalSystem;

            public Controller(IMyGridTerminalSystem grid)
            {
                GridTerminalSystem = grid;
            }

            public void Airvents(bool pressurize)
            {
                var group = GridTerminalSystem.GetBlockGroupWithName(AL_AIRVENTS);
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

                    airvent.Depressurize = !pressurize;
                }
                blocks.Clear();
            }

            public void AuxDoors(bool open)
            {
                ControlDoors(AL_DOOR_AUX, open);
            }

            public void ExternalDoor(bool open)
            {
                ControlDoors(AL_DOOR_OUTER, open);
            }

            public void InternalDoor(bool open)
            {
                ControlDoors(AL_DOOR_INNER, open);
            }

            public void LcdText(string text)
            {
                var group = GridTerminalSystem.GetBlockGroupWithName(AL_LCD);
                if (group == null)
                {
                    return;
                }

                group.GetBlocks(blocks);
                for (var i = 0; i < blocks.Count; i++)
                {
                    var panel = blocks[i] as IMyTextPanel;
                    if (panel == null)
                    {
                        continue;
                    }

                    panel.WritePublicText(text);
                    panel.ShowPublicTextOnScreen();
                    panel.FontSize = 1.0f;
                    panel.Font = "Monospace";
                }
                blocks.Clear();
            }

            public void Lights(LightMode mode)
            {
                var group = GridTerminalSystem.GetBlockGroupWithName(AL_LIGHTS);
                if (group == null)
                {
                    return;
                }

                group.GetBlocks(blocks);
                for (var i = 0; i < blocks.Count; i++)
                {
                    var light = blocks[i] as IMyLightingBlock;
                    if (light == null)
                    {
                        continue;
                    }

                    switch (mode)
                    {
                        case LightMode.Off:
                            light.Enabled = false;
                            break;
                        case LightMode.Blink:
                            light.Enabled = true;
                            light.BlinkIntervalSeconds = 0.75f;
                            light.BlinkLength = 50f;
                            light.Color = Color.Yellow;
                            break;
                        case LightMode.On:
                            light.Enabled = true;
                            light.BlinkIntervalSeconds = 0;
                            light.Color = Color.White;
                            break;
                        case LightMode.Red:
                            light.Enabled = true;
                            light.BlinkIntervalSeconds = 0;
                            light.Color = Color.Red;
                            break;
                    }
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
                    var door = blocks[i] as IMyDoor;
                    if (door == null)
                    {
                        continue;
                    }

                    if (open)
                    {
                        door.OpenDoor();
                    }
                    else
                    {
                        door.CloseDoor();
                    }
                }
                blocks.Clear();
            }
        }
    }
}

