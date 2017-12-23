using System;

namespace IngameScript
{
    partial class Program
    {
        enum Event
        {
            ENTER_1,
            ENTER_2,
            ENTER_3,
            EXIT_1,
            EXIT_2,
            EXIT_3,
        }

        enum State
        {
            // Шлюзы закрыты
            IDLE,
            // Шлюзы закрыты, кулдаун
            COOLDOWN,

            // сброс
            ENTER_DEPRESSURIZING,
            // открытие двери (наружу)
            ENTER_DEPRESSURIZED,
            // закрытие двери (наружу)
            ENTER_OUTER_CLOSING,
            // набор давления
            ENTER_PRESSURIZING,
            // открытие двери (внутрь)
            ENTER_PRESSURIZED,


            // открытие двери (внутрь)
            EXIT_INNER_OPEN,
            // закрытие двери (внутрь)
            EXIT_INNER_CLOSING,
            // сброс давления
            EXIT_DEPRESSURIZING,
            // открытие двери (наружу)
            EXIT_OUTER_OPEN,
        }

        private void HandleEvent(Event e)
        {
            switch (e)
            {
                case Event.ENTER_1:
                    switch (state)
                    {
                        case State.IDLE:
                            EnterState(State.ENTER_DEPRESSURIZING);
                            break;
                    }
                    break;
                case Event.ENTER_2:
                    switch (state)
                    {
                        case State.ENTER_DEPRESSURIZED:
                            EnterState(State.ENTER_OUTER_CLOSING);
                            break;
                    }
                    break;
                case Event.ENTER_3:
                    switch (state)
                    {
                        case State.ENTER_PRESSURIZED:
                            descr = "";
                            EnterState(State.COOLDOWN);
                            break;
                    }
                    break;

                case Event.EXIT_1:
                    switch (state)
                    {
                        case State.IDLE:
                            EnterState(State.EXIT_INNER_OPEN);
                            break;
                    }
                    break;
                case Event.EXIT_2:
                    switch (state)
                    {
                        case State.EXIT_INNER_OPEN:
                            EnterState(State.EXIT_INNER_CLOSING);
                            break;
                    }
                    break;
                case Event.EXIT_3:
                    switch (state)
                    {
                        case State.EXIT_OUTER_OPEN:
                            descr = "";
                            EnterState(State.COOLDOWN);
                            break;
                    }
                    break;
            }
        }

        private void HandleTimer(ref AirlockStatus status)
        {
            var forceReset = (DateTime.UtcNow - lastTime).TotalSeconds >= MAX_WAIT_TIME;
            if (forceReset && state != State.IDLE && state != State.COOLDOWN)
            {
                EnterState(State.IDLE);
                descr = "Время ожидания истекло";
                LcdText(ref status);
                return;
            }

            switch (state)
            {
                case State.ENTER_DEPRESSURIZING:
                    {
                        var force = (DateTime.UtcNow - lastTime).TotalSeconds >= MAX_DEPRESSURIZE_TIME;
                        if (status.AirVents.IsDepressurized || force)
                        {
                            descr = force ? "Принуд. сброс давления" : "";
                            EnterState(State.ENTER_DEPRESSURIZED);
                        }
                    }
                    break;

                case State.ENTER_OUTER_CLOSING:
                    if (status.ExternalDoors.IsClosed)
                    {
                        EnterState(State.ENTER_PRESSURIZING);
                    }
                    break;

                case State.ENTER_PRESSURIZING:
                    {
                        var force = (DateTime.UtcNow - lastTime).TotalSeconds >= MAX_PRESSURIZE_TIME;
                        if (status.AirVents.IsPressurized || force)
                        {
                            descr = force ? "Принуд. набор давления" : "";
                            EnterState(State.ENTER_PRESSURIZED);
                        }
                    }
                    break;

                case State.EXIT_INNER_CLOSING:
                    if (status.InternalDoors.IsClosed)
                    {
                        EnterState(State.EXIT_DEPRESSURIZING);
                    }
                    break;

                case State.EXIT_DEPRESSURIZING:
                    {
                        var force = (DateTime.UtcNow - lastTime).TotalSeconds >= MAX_DEPRESSURIZE_TIME;
                        if (status.AirVents.IsDepressurized || force)
                        {
                            descr = force ? "Принуд. сброс давления" : "";
                            EnterState(State.EXIT_OUTER_OPEN);
                        }
                    }
                    break;

                case State.COOLDOWN:
                    var cooldownOver = (DateTime.UtcNow - lastTime).TotalSeconds >= COOLDOWN_TIME;
                    if (cooldownOver)
                    {
                        EnterState(State.IDLE);
                    }
                    break;
            }

            LcdText(ref status);
        }

        private void EnterState(State s)
        {
            switch (s)
            {
                case State.IDLE:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;
                case State.COOLDOWN:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;

                case State.ENTER_DEPRESSURIZING:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: false);
                    break;
                case State.ENTER_DEPRESSURIZED:
                    ExternalDoor(open: true);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: false);
                    break;
                case State.ENTER_OUTER_CLOSING:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: false);
                    break;
                case State.ENTER_PRESSURIZING:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;
                case State.ENTER_PRESSURIZED:
                    ExternalDoor(open: false);
                    InternalDoor(open: true);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;


                case State.EXIT_INNER_OPEN:
                    ExternalDoor(open: false);
                    InternalDoor(open: true);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;
                case State.EXIT_INNER_CLOSING:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: true);
                    break;
                case State.EXIT_DEPRESSURIZING:
                    ExternalDoor(open: false);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: false);
                    break;
                case State.EXIT_OUTER_OPEN:
                    ExternalDoor(open: true);
                    InternalDoor(open: false);
                    AuxDoors(open: false);
                    Airvents(pressurize: false);
                    break;
            }

            if (state != s)
            {
                lastTime = DateTime.UtcNow;
                state = s;
            }

            Echo($"{state}");
        }
    }
}
