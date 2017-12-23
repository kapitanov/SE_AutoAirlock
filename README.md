# SE_AutoAirlock

Airlock controller script for Space Engineers

## Features

* Controls an airlock with sensors
* Doesn't require to interact with any buttons
* Properly handles airlock pressurization and depressurization
* Correctly handles exceptional cases like two ships trying to pass though an airlock at the same time
* Deletects airlock hull breaches and hardware malfunctions

## Setup

1. Add a programmable block and load the program.
2. Add two groups of doors (airtight doors, sliding doors or any other). Name one group `AL_DOOR_OUTER` and the other `AL_DOOR_INNER`.
3. (Optionally) Add some custom doors and combine them into a group named `AL_DOOR_AUX`.
4. Add some airvents inside the airlock and group them with name `AL_AIRVENTS`.
5. (Optionally) Add some LCD screns into group `AL_LCD`.
6. (Optionally) Add some lights into group `AL_LIGHTS`.
7. Add 5 sensors:

   * One sensor near outer door at its outer side (outside an airlock). It should trigger programmable block with argument `ENTER_1` on enter and with `EXIT_3` - on exit.
   * One sensor near outer door at its inner side (inside an airlock). It should trigger programmable block with argument `EXIT_2` on enter.
   * One sensor near inner door at its inner side (inside an airlock). It should trigger programmable block with argument `ENTER_2`  on enter.
   * One sensor near inner door at its outer side (outside an airlock). It should trigger programmable block with argument `EXIT_1` on enter and with `ENTER_3` - on exit.
   * One sensor than covers airlock's internal space. Add it into `AL_SENSORS` group.

```
| OUTSIDE ----S1----   OUTER_DOOR   ----S2---- ----S5----- ----S3----   INNER_DOOR  ----S4---- INSIDE
          |        |     | | |      |        | |         | |        |      | | |    |         |
          |        |     | | |      |        | |         | |        |      | | |    |         |  
          |        |     | | |      |        | |         | |        |      | | |    |         |
          |        |     | | |      |        | |         | |        |      | | |    |         |
          |        |     | | |      |        | |         | |        |      | | |    |         |
```
