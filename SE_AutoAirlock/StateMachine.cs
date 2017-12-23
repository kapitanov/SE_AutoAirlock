using System;
using System.Collections.Generic;

namespace IngameScript
{
    partial class Program
    {
        enum Event
        {
            INIT,
            TIMER,
            RESET,

            ENTER_SHIP_APPROACHED,
            ENTER_SHIP_INSIDE,
            ENTER_SHIP_LEFT,

            EXIT_SHIP_APPROACHED,
            EXIT_SHIP_INSIDE,
            EXIT_SHIP_LEFT,
        }

        enum StateCategory
        {
            Ready,
            Locked,
            Transition,
            InUse
        }

        sealed class FSM
        {
            public struct StateCtx
            {
                public Event Event;
                public AirlockStatus Status;
                public double Time;
                public IController Control;
            }

            public struct StateResult
            {
                public StateFunc NextState;
                public string Description;
            }

            private static StateResult ToState(StateFunc state, string description = null)
            {
                return new StateResult
                {
                    NextState = state,
                    Description = description
                };
            }

            public delegate StateResult? StateFunc(ref StateCtx ctx);

            // Шлюзы закрыты
            public static StateResult? IDLE(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: false);
                        ctx.Control.InternalDoor(open: false);
                        ctx.Control.AuxDoors(open: false);
                        ctx.Control.Airvents(pressurize: true);
                        ctx.Control.Lights(LightMode.Off);
                        break;

                    case Event.ENTER_SHIP_APPROACHED:
                        return ToState(ENTER_DEPRESSURIZING);

                    case Event.EXIT_SHIP_APPROACHED:
                        return ToState(EXIT_INNER_OPENING);

                    case Event.TIMER:
                        if (ctx.Status.IsObstructed)
                        {
                            return ToState(OBSTRUCTED);
                        }
                        break;

                }

                return null;
            }

            // Шлюзы закрыты, кулдаун
            public static StateResult? COOLDOWN(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: false);
                        ctx.Control.InternalDoor(open: false);
                        ctx.Control.AuxDoors(open: false);
                        ctx.Control.Airvents(pressurize: true);
                        ctx.Control.Lights(LightMode.Red);
                        break;
                    case Event.TIMER:
                        if (ctx.Status.IsObstructed)
                        {
                            return ToState(OBSTRUCTED);
                        }
                        if (ctx.Time >= MAX_PRESSURIZE_TIME)
                        {
                            return ToState(IDLE);
                        }
                        if (ctx.Status.AirVents.IsPressurized &&
                            ctx.Status.ExternalDoors.IsClosed &&
                            ctx.Status.InternalDoors.IsClosed)
                        {
                            return ToState(IDLE);
                        }
                        break;
                }

                return null;
            }

            // Обнаружено препятствие
            public static StateResult? OBSTRUCTED(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.Lights(LightMode.Red);
                        break;
                    case Event.TIMER:
                        if (!ctx.Status.IsObstructed)
                        {
                            return ToState(COOLDOWN);
                        }
                        break;
                }

                return null;
            }

            // Сброс давления
            public static StateResult? ENTER_DEPRESSURIZING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.Airvents(pressurize: false);
                        ctx.Control.Lights(LightMode.Blink);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.IsObstructed)
                        {
                            return ToState(OBSTRUCTED);
                        }
                        if (ctx.Status.AirVents.IsDepressurized)
                        {
                            return ToState(ENTER_OUTER_OPENING);
                        }
                        if (ctx.Time >= MAX_DEPRESSURIZE_TIME)
                        {
                            return ToState(ENTER_OUTER_OPENING, "Принуд. сброс давления");
                        }
                        break;
                }

                return null;
            }

            // Открытие двери (наружу)
            public static StateResult? ENTER_OUTER_OPENING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: true);
                        ctx.Control.Lights(LightMode.On);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.ExternalDoors.IsOpen)
                        {
                            return ToState(ENTER_OUTER_OPEN);
                        }
                        break;

                    case Event.ENTER_SHIP_INSIDE:
                        return ToState(ENTER_OUTER_CLOSING);
                }

                return null;
            }

            // Дверь открыта (наружу)
            public static StateResult? ENTER_OUTER_OPEN(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: true);
                        break;

                    case Event.ENTER_SHIP_INSIDE:
                        return ToState(ENTER_OUTER_CLOSING);

                    case Event.TIMER:
                        if (ctx.Time >= MAX_WAIT_TIME)
                        {
                            return ToState(WAIT_TIMED_OUT);
                        }
                        break;
                }

                return null;
            }

            // Закрытие двери (наружу)
            public static StateResult? ENTER_OUTER_CLOSING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: false);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.ExternalDoors.IsClosed)
                        {
                            return ToState(ENTER_PRESSURIZING);
                        }
                        break;
                }

                return null;
            }

            // Набор давления
            public static StateResult? ENTER_PRESSURIZING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.Airvents(pressurize: true);
                        ctx.Control.Lights(LightMode.Blink);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.AirVents.IsPressurized)
                        {
                            return ToState(ENTER_INNER_OPENING);
                        }
                        if (ctx.Time >= MAX_PRESSURIZE_TIME)
                        {
                            return ToState(ENTER_INNER_OPENING, "Принуд. набор давления");
                        }
                        break;
                }

                return null;
            }

            // Открытие двери (внутрь)
            public static StateResult? ENTER_INNER_OPENING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: true);
                        ctx.Control.Lights(LightMode.On);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.InternalDoors.IsOpen)
                        {
                            return ToState(ENTER_INNER_OPEN);
                        }
                        break;

                    case Event.ENTER_SHIP_LEFT:
                        return ToState(ENTER_INNER_CLOSING);
                }

                return null;
            }

            // Дверь открыта (внутрь)
            public static StateResult? ENTER_INNER_OPEN(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: true);
                        break;

                    case Event.ENTER_SHIP_LEFT:
                        return ToState(ENTER_INNER_CLOSING);
                }

                return null;
            }

            // Закрытие двери (внутрь)
            public static StateResult? ENTER_INNER_CLOSING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: false);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.IsObstructed)
                        {
                            return ToState(OBSTRUCTED);
                        }
                        if (ctx.Status.InternalDoors.IsClosed)
                        {
                            return ToState(COOLDOWN);
                        }
                        break;
                }

                return null;
            }



            // Открытие внутренней двери
            public static StateResult? EXIT_INNER_OPENING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: true);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.InternalDoors.IsOpen)
                        {
                            return ToState(EXIT_INNER_OPEN);
                        }
                        break;

                    case Event.EXIT_SHIP_INSIDE:
                        return ToState(EXIT_INNER_CLOSING);
                }

                return null;
            }

            // Внутренняя дверь открыта
            public static StateResult? EXIT_INNER_OPEN(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: true);
                        break;

                    case Event.EXIT_SHIP_INSIDE:
                        return ToState(EXIT_INNER_CLOSING);

                    case Event.TIMER:
                        if (ctx.Time >= MAX_WAIT_TIME)
                        {
                            return ToState(WAIT_TIMED_OUT);
                        }
                        break;
                }

                return null;
            }

            // Закрытие внутренней двери
            public static StateResult? EXIT_INNER_CLOSING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: false);
                        ctx.Control.AuxDoors(open: false);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.InternalDoors.IsClosed)
                        {
                            return ToState(EXIT_DEPRESSURIZING);
                        }
                        break;
                }

                return null;
            }

            // Сброс давления
            public static StateResult? EXIT_DEPRESSURIZING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.Airvents(pressurize: false);
                        ctx.Control.Lights(LightMode.Blink);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.AirVents.IsDepressurized)
                        {
                            return ToState(EXIT_OUTER_OPENING);
                        }
                        if (ctx.Time >= MAX_DEPRESSURIZE_TIME)
                        {
                            return ToState(EXIT_OUTER_OPENING, "Принуд. сброс давления");
                        }
                        break;
                }

                return null;
            }

            // Открытие внешней двери
            public static StateResult? EXIT_OUTER_OPENING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: true);
                        ctx.Control.Lights(LightMode.On);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.ExternalDoors.IsOpen)
                        {
                            return ToState(EXIT_OUTER_OPEN);
                        }
                        break;

                    case Event.EXIT_SHIP_LEFT:
                        return ToState(EXIT_OUTER_CLOSING);
                }

                return null;
            }

            // Внешняя дверь открыта
            public static StateResult? EXIT_OUTER_OPEN(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        break;

                    case Event.EXIT_SHIP_LEFT:
                        return ToState(EXIT_OUTER_CLOSING);

                    case Event.TIMER:
                        if (ctx.Time >= MAX_WAIT_TIME)
                        {
                            return ToState(WAIT_TIMED_OUT);
                        }
                        break;
                }

                return null;
            }

            // Закрытие внешней двери
            public static StateResult? EXIT_OUTER_CLOSING(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.ExternalDoor(open: false);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.IsObstructed)
                        {
                            return ToState(OBSTRUCTED);
                        }
                        if (ctx.Status.ExternalDoors.IsClosed)
                        {
                            return ToState(COOLDOWN);
                        }
                        break;
                }

                return null;
            }


            // Закрытие внешней двери
            public static StateResult? WAIT_TIMED_OUT(ref StateCtx ctx)
            {
                switch (ctx.Event)
                {
                    case Event.INIT:
                        ctx.Control.InternalDoor(open: false);
                        ctx.Control.ExternalDoor(open: false);
                        ctx.Control.Lights(LightMode.Off);
                        break;

                    case Event.TIMER:
                        if (ctx.Status.ExternalDoors.IsClosed && ctx.Status.InternalDoors.IsClosed)
                        {
                            return ToState(COOLDOWN);
                        }
                        break;
                }

                return null;
            }

            private StateFunc state = null;
            private DateTime stateTime;
            private string description = "";

            private readonly Dictionary<StateFunc, string> stateNames;
            private readonly Dictionary<StateFunc, string> stateDescriptions;
            private readonly Dictionary<StateFunc, StateCategory> stateCategories;

            public FSM()
            {
                stateNames = new Dictionary<StateFunc, string>
                {
                    {IDLE, nameof(IDLE) },
                    {COOLDOWN, nameof(COOLDOWN) },
                    {WAIT_TIMED_OUT, nameof(WAIT_TIMED_OUT) },
                    {OBSTRUCTED                    , nameof(OBSTRUCTED) },

                    {ENTER_DEPRESSURIZING, nameof(ENTER_DEPRESSURIZING) },
                    {ENTER_OUTER_OPENING, nameof(ENTER_OUTER_OPENING) },
                    {ENTER_OUTER_OPEN, nameof(ENTER_OUTER_OPEN) },
                    {ENTER_OUTER_CLOSING, nameof(ENTER_OUTER_CLOSING) },
                    {ENTER_PRESSURIZING, nameof(ENTER_PRESSURIZING) },
                    {ENTER_INNER_OPENING, nameof(ENTER_INNER_OPENING) },
                    {ENTER_INNER_OPEN, nameof(ENTER_INNER_OPEN) },
                    {ENTER_INNER_CLOSING, nameof(ENTER_INNER_CLOSING) },

                    {EXIT_INNER_OPENING, nameof(EXIT_INNER_OPENING) },
                    {EXIT_INNER_OPEN, nameof(EXIT_INNER_OPEN) },
                    {EXIT_INNER_CLOSING, nameof(EXIT_INNER_CLOSING) },
                    {EXIT_DEPRESSURIZING, nameof(EXIT_DEPRESSURIZING) },
                    {EXIT_OUTER_OPENING, nameof(EXIT_OUTER_OPENING) },
                    {EXIT_OUTER_OPEN, nameof(EXIT_OUTER_OPEN) },
                    {EXIT_OUTER_CLOSING, nameof(EXIT_OUTER_CLOSING) },
                };

                stateDescriptions = new Dictionary<StateFunc, string>
                {
                    { IDLE, "Готов" },
                    { COOLDOWN, "Ожидание" },
                    { WAIT_TIMED_OUT, "Время ожидания истекло" },
                    { OBSTRUCTED, "Обнаружено препятствие" },

                    { ENTER_DEPRESSURIZING, "Сброс давления" },
                    { ENTER_OUTER_OPENING, "Открытие внеш. двери" },
                    { ENTER_OUTER_OPEN, "Внеш. дверь открыта" },
                    { ENTER_OUTER_CLOSING, "Закрытие внеш. двери" },
                    { ENTER_PRESSURIZING, "Набор давления" },
                    { ENTER_INNER_OPENING, "Открытие внутр. двери" },
                    { ENTER_INNER_OPEN, "Внутр. дверь открыта" },
                    { ENTER_INNER_CLOSING, "Закрытие внутр. двери" },

                    { EXIT_INNER_OPENING, "Открытие внутр. двери" },
                    { EXIT_INNER_OPEN, "Внутр. дверь открыта" },
                    { EXIT_INNER_CLOSING, "Закрытие внутр. двери" },
                    { EXIT_DEPRESSURIZING, "Сброс давления" },
                    { EXIT_OUTER_OPENING, "Открытие внеш. двери" },
                    { EXIT_OUTER_OPEN, "Внеш. дверь открыта" },
                    { EXIT_OUTER_CLOSING, "Закрытие внеш. двери" },
                };

                stateCategories = new Dictionary<StateFunc, StateCategory>
                {
                    {IDLE,  StateCategory.Ready },

                    {COOLDOWN, StateCategory.Locked },
                    {WAIT_TIMED_OUT, StateCategory.Locked  },
                    {OBSTRUCTED, StateCategory.Locked  },

                    {ENTER_DEPRESSURIZING, StateCategory.Transition },
                    {ENTER_OUTER_OPENING, StateCategory.Transition },
                    {ENTER_OUTER_CLOSING, StateCategory.Transition },
                    {ENTER_PRESSURIZING, StateCategory.Transition},
                    {ENTER_INNER_OPENING, StateCategory.Transition },
                    {ENTER_INNER_CLOSING, StateCategory.Transition },
                    {EXIT_INNER_OPENING, StateCategory.Transition },
                    {EXIT_INNER_CLOSING, StateCategory.Transition },
                    {EXIT_DEPRESSURIZING, StateCategory.Transition },
                    {EXIT_OUTER_OPENING, StateCategory.Transition },
                    {EXIT_OUTER_CLOSING,StateCategory.Transition },

                    {ENTER_OUTER_OPEN, StateCategory.InUse },
                    {ENTER_INNER_OPEN, StateCategory.InUse},
                    {EXIT_INNER_OPEN, StateCategory.InUse },
                    {EXIT_OUTER_OPEN, StateCategory.InUse },
                };
            }

            public string State => stateDescriptions.GetValueOrDefault(state, "НЕИЗВЕСТНО");
            public string StateName => stateNames.GetValueOrDefault(state, "???");
            public StateCategory StateCat => stateCategories.GetValueOrDefault(state, StateCategory.InUse);


            public string Description => description;

            public void Update(IController control, Event e, ref AirlockStatus status)
            {
                if (state == null || e == Event.RESET)
                {
                    state = IDLE;
                    stateTime = DateTime.UtcNow;
                    Update(control, Event.INIT, ref status);

                    if (e == Event.RESET)
                    {
                        return;
                    }
                }

                var ctx = new StateCtx
                {
                    Event = e,
                    Status = status,
                    Control = control,
                    Time = (DateTime.UtcNow - stateTime).TotalSeconds
                };
                var result = state(ref ctx);
                if (result != null)
                {
                    description = result.Value.Description;

                    if (state != result.Value.NextState)
                    {
                        state = result.Value.NextState;
                        stateTime = DateTime.UtcNow;

                        Update(control, Event.INIT, ref status);
                    }
                }
            }
        }
    }
}
