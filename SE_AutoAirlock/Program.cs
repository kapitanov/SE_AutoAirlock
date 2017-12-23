using Sandbox.ModAPI.Ingame;
using System;
using System.Text;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        const string AL_DOOR_OUTER = "AL_DOOR_OUTER";
        const string AL_DOOR_INNER = "AL_DOOR_INNER";
        const string AL_DOOR_AUX = "AL_DOOR_AUX";
        const string AL_AIRVENTS = "AL_AIRVENTS";
        const string AL_LCD = "AL_LCD";
        const string AL_LIGHTS = "AL_LIGHTS";
        const string AL_SENSORS = "AL_SENSORS";

        const float PRESSURIZE_THRESHOLD = 0.75f;
        const float DEPRESSURIZE_THRESHOLD = 0.01f;

        const int MAX_DEPRESSURIZE_TIME = 20;
        const int MAX_PRESSURIZE_TIME = 20;
        const int MAX_WAIT_TIME = 30;


        Controller controller;
        FSM fsm;

        public Program()
        {
            try
            {
                controller = new Controller(GridTerminalSystem);
                fsm = new FSM();

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
                Event e;

                if ((updateType & UpdateType.Update10) != 0)
                {
                    e = Event.TIMER;
                }
                else
                {
                    argument = (argument ?? "").ToUpper();
                    switch (argument)
                    {
                        case "ENTER_1":
                            e = Event.ENTER_SHIP_APPROACHED;
                            break;
                        case "ENTER_2":
                            e = Event.ENTER_SHIP_INSIDE;
                            break;
                        case "ENTER_3":
                            e = Event.ENTER_SHIP_LEFT;
                            break;

                        case "EXIT_1":
                            e = Event.EXIT_SHIP_APPROACHED;
                            break;
                        case "EXIT_2":
                            e = Event.EXIT_SHIP_INSIDE;
                            break;
                        case "EXIT_3":
                            e = Event.EXIT_SHIP_LEFT;
                            break;

                        case "":
                            e = Event.RESET;
                            break;

                        default:
                            Echo($"Bad command: \"{argument}\"");
                            return;
                    }
                }

                fsm.Update(controller, e, ref status);
                controller.LcdText(GetStatusText(ref status));
                Echo($"State: {fsm.StateName}");
            }
            catch (Exception e)
            {
                Echo($"ERROR! {e.Message}\n{e.StackTrace}");
            }
        }

        Printer sb = new Printer();

        private string GetStatusText(ref AirlockStatus status)
        {
            sb.Begin();

            sb.Header("ГЕРМОШЛЮЗ");
            sb.Title("СОСТОЯНИЕ");

            char icon = ' ';
            switch (fsm.StateCat)
            {
                case StateCategory.Ready:
                    icon = CHAR_GREEN;
                    break;
                case StateCategory.Locked:
                    icon = CHAR_RED;
                    break;
                case StateCategory.Transition:
                    icon = CHAR_YELLOW;
                    break;
                case StateCategory.InUse:
                    icon = CHAR_CYAN;
                    break;
            }
            sb.Value(icon, fsm.State);

            status.ExternalDoors.Print(sb, "ВНЕШ. ДВЕРЬ");
            status.InternalDoors.Print(sb, "ВНУТР. ДВЕРЬ");
            status.AirVents.Print(sb, "ДАВЛЕНИЕ");

            if (!string.IsNullOrEmpty(fsm.Description))
            {
                sb.Title("ВНИМАНИЕ");
                sb.Value(fsm.Description);
            }

            sb.End();

            return sb.ToString();
        }

        const char CHAR_RED = '\uE200';
        const char CHAR_YELLOW = '\uE220';
        const char CHAR_GREEN = '\uE120';
        const char CHAR_CYAN = '\uE124';


        class Printer
        {
            const char CHAR_FRAME = ' ';
            const char CHAR_SEPARATOR = ' ';
            const char CHAR_PROGRESS_EMPTY = '_';
            const char CHAR_PROGRESS_FILL = '#';

            const string PROGRESS_100 = "\uE118\uE118\uE118\uE118\uE118";
            const string PROGRESS_80 = "\uE118\uE118\uE118\uE118\uE149";
            const string PROGRESS_60 = "\uE118\uE118\uE118\uE149\uE149";
            const string PROGRESS_40 = "\uE118\uE118\uE149\uE149\uE149";
            const string PROGRESS_20 = "\uE118\uE149\uE149\uE149\uE149";
            const string PROGRESS_0 = "\uE149\uE149\uE149\uE149\uE149";

            const int WIDTH = 26;
            const int HEIGHT = 18;

            StringBuilder sb = new StringBuilder();
            int lines;

            public void Begin()
            {
                sb.Clear();
                lines = 0;
                Separator();
            }

            public void Header(string text)
            {
                sb.Append(CHAR_FRAME);
                var i = 1;

                var len = Math.Min(WIDTH - i - 1, text.Length);
                var offset = (WIDTH - 2 - len) / 2;

                for (; i < offset; i++)
                {
                    sb.Append(' ');
                }

                PrintTrimmed(text, ref i);

                sb.Append(CHAR_FRAME);
                sb.AppendLine();
                lines++;

                Separator();
            }

            public void Separator()
            {
                for (int i = 0; i < WIDTH; i++)
                {
                    sb.Append(CHAR_SEPARATOR);
                }
                sb.AppendLine();
                lines++;
            }

            public void Title(string text)
            {
                sb.Append(CHAR_FRAME);
                var i = 1;

                PrintTrimmed(text, ref i);

                sb.Append(CHAR_FRAME);
                sb.AppendLine();
                lines++;
            }

            public void Value(string text)
            {
                sb.Append(CHAR_FRAME);
                sb.Append("  ");

                var i = 3;

                PrintTrimmed(text, ref i);

                sb.Append(CHAR_FRAME);
                sb.AppendLine();
                lines++;
            }

            public void ValuePressure(char icon, float pressure, string text = null)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    Value(icon, $"{pressure,-4:F2} атм ({text})");
                }
                else
                {
                    Value(icon, $"{pressure,-4:F2} атм");
                }
            }

            public void ValueProgress(char icon, string text, float progress)
            {
                string bar = "";
                if (progress >= 0.9)
                {
                    bar = PROGRESS_100;
                }
                else if (progress >= 0.8)
                {
                    bar = PROGRESS_80;
                }
                else if (progress >= 0.6)
                {
                    bar = PROGRESS_60;
                }
                else if (progress >= 0.4)
                {
                    bar = PROGRESS_40;
                }
                else if (progress >= 0.2)
                {
                    bar = PROGRESS_20;
                }
                else
                {
                    bar = PROGRESS_0;
                }

                Value(icon, $"{text} {bar}");
            }

            public void Value(char icon, string text)
            {
                sb.Append(CHAR_FRAME);
                sb.Append("  ");
                sb.Append(icon);
                sb.Append(' ');

                var i = 5;

                PrintTrimmed(text, ref i);

                sb.Append(CHAR_FRAME);
                sb.AppendLine();
                lines++;
            }

            public void End()
            {
                while (lines < HEIGHT - 1)
                {
                    Value("");
                }
                Separator();
            }

            public override string ToString()
            {
                return sb.ToString();
            }

            private void PrintTrimmed(string text, ref int i)
            {
                var max = WIDTH - 1;
                var len = Math.Min(max - i, text.Length);
                for (var j = 0; j < len; j++, i++)
                {
                    sb.Append(text[j]);
                }
                for (; i < max; i++)
                {
                    sb.Append(' ');
                }
            }
        }
    }
}