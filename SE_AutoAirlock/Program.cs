using Sandbox.ModAPI.Ingame;
using System;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string AL_DOOR_OUTER = "AL_DOOR_OUTER";
        const string AL_DOOR_INNER = "AL_DOOR_INNER";
        const string AL_DOOR_AUX = "AL_DOOR_AUX";
        const string AL_AIRVENTS = "AL_AIRVENTS";
        const string AL_LCD = "AL_LCD";

        const float PRESSURIZE_THRESHOLD = 0.75f;
        const float DEPRESSURIZE_THRESHOLD = 0.01f;

        const int MAX_DEPRESSURIZE_TIME = 20;
        const int MAX_PRESSURIZE_TIME = 20;
        const int COOLDOWN_TIME = 10;
        const int MAX_WAIT_TIME = 30;


        State state;
        DateTime lastTime;
        string descr = "";

        public Program()
        {
            try
            {
                lastTime = DateTime.UtcNow;
                EnterState(State.IDLE);

                Runtime.UpdateFrequency = UpdateFrequency.Update10;
            }
            catch (Exception e)
            {
                Echo($"ERROR! {e.Message}\n{e.StackTrace}");
            }
        }

        public void Save() { }

        public void Main(string argument, UpdateType updateType)
        {
            try
            {
                AirlockStatus status;
                GetStatus(out status);

                if ((updateType & UpdateType.Update10) != 0)
                {
                    HandleTimer(ref status);
                }
                else
                {
                    argument = (argument ?? "").ToUpper();
                    switch (argument)
                    {
                        case "ENTER_1":
                            HandleEvent(Event.ENTER_1);
                            break;
                        case "ENTER_2":
                            HandleEvent(Event.ENTER_2);
                            break;
                        case "ENTER_3":
                            HandleEvent(Event.ENTER_3);
                            break;

                        case "EXIT_1":
                            HandleEvent(Event.EXIT_1);
                            break;
                        case "EXIT_2":
                            HandleEvent(Event.EXIT_2);
                            break;
                        case "EXIT_3":
                            HandleEvent(Event.EXIT_3);
                            break;

                        case "":
                            EnterState(State.IDLE);
                            break;

                        default:
                            Echo($"Bad command: \"{argument}\"");
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Echo($"ERROR! {e.Message}\n{e.StackTrace}");
            }
        }
    }
}