//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#include <chrono>
#include <memory>
#include <functional>
#include "gtest/gtest.h"
#include "libworld.h"
#include "clearanceTypes.hpp"
#include "simpleRunwayMutex.hpp"
#include "libworld_test.h"
#include "mutexTestCase.hpp"
#include "mutexLongRunningTestCase.hpp"

using namespace std;
using namespace world;
using namespace ai;

TEST(RunwayMutexTest, waiting_at_holding_point)
{
    MutexLongRunningTestCase t;

    t.COL_ARRIVAL(101, "B738");
    t.COL_DEPARTURE(102, "A320");
    t.COL_DEPARTURE(103, "B747");
    t.COL_DEPARTURE(104, "A319");
    t.COL_DEPARTURE(105, "B777");
    t.COL_DEPARTURE(106, "A380");

    const auto& C = t.CELL;

    // 101 crossing the runway
    t.ROW(  0, { C.HSCRS_CLR()             , C.GND_GATE()             , C.GND_GATE()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(  5, { C.CROSSING()              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 10, { C.CROSSING()              , C.HS(1)                   , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    //                                       102 on holding point is cleared for LUAW but the taxiway between the HP and the RW is pretty long
    t.ROW( 15, { C.CROSSING()              , C.HS_CHK_LUAW(1, C.TA_CRSRWY()) , C.NOTHING()         , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 20, { C.CROSSING()              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 25, { C.CROSSING()              , C.NOTHING()               , C.HS(1)                   , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    //                                                                 103 checks in with tower on holding point and has to wait
    t.ROW( 30, { C.CROSSING()              , C.NOTHING()               , C.HS_CHK_HLD_DRLINE(1, true) , C.NOTHING()            , C.NOTHING()               , C.NOTHING()                });
    // The second part of PR #248 fixes a problem that appeared here : 103 was cleared for takeoff here (just as 101 vacated the runway)
    t.ROW( 35, { C.CRS_VACATED()           , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 40, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    //                                       102 arrives on the runway and is cleared for takeoff
    t.ROW( 45, { C.NOTHING()               , C.LUAW_CLRTO()            , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 50, { C.NOTHING()               , C.TO_ROLL(1)              , C.LUAW()                  , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 55, { C.NOTHING()               , C.TO_ROLL(2)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 60, { C.NOTHING()               , C.TO_ROLL(3)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 65, { C.NOTHING()               , C.TO_ROLL(4)              , C.LINEDUP()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 70, { C.NOTHING()               , C.TO_ROLL(5)              , C.LINEDUP()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 75, { C.NOTHING()               , C.TO_VACATED()            , C.LUAW_CLRTO()            , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 80, { C.NOTHING()               , C.NOTHING()               , C.TO_ROLL(1)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 85, { C.NOTHING()               , C.NOTHING()               , C.TO_ROLL(2)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 90, { C.NOTHING()               , C.NOTHING()               , C.TO_ROLL(3)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW( 95, { C.NOTHING()               , C.NOTHING()               , C.TO_ROLL(4)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    t.ROW(100, { C.NOTHING()               , C.NOTHING()               , C.TO_ROLL(5)              , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(105, { C.NOTHING()               , C.NOTHING()               , C.TO_VACATED()            , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(110, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(115, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(120, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(125, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(130, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(135, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(140, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(145, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(150, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(155, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(160, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(165, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(170, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(175, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(180, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(185, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(190, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(195, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    t.ROW(200, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(205, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(210, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(215, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(220, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(225, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(230, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(235, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(240, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(245, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(250, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(255, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(260, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(265, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(270, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(275, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(280, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(285, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(290, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(295, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    t.ROW(300, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(305, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(310, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(315, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(320, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(325, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(330, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(335, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(340, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(345, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(350, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(355, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(360, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(365, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(370, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(375, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(380, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(385, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(390, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(395, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    t.ROW(400, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(405, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(410, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(415, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(420, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(425, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(430, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(435, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(440, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(445, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(450, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(455, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(460, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(465, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(470, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(475, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(480, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(485, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(490, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });
    t.ROW(495, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    t.ROW(500, { C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()               , C.NOTHING()                });

    //    EXPECT_TRUE(
    //        t.run(0, 500, 5)
    //    );
}
