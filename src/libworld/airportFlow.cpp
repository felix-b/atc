//
// AirportFlow implementation with default airport configurations
//
#include "airportFlow.hpp"

namespace world
{
    AirportFlowRegistry AirportFlowRegistry::createWithDefaultFlows()
    {
        AirportFlowRegistry registry;
        
        // ===== KSEA - Seattle-Tacoma International =====
        // North Flow: Wind from 310-050 (winds favor runways 16/17)
        {
            AirportFlow northFlow("NORTH", 310, 50);
            // 16L: Arrivals
            northFlow.addRunway("16L", true, false);
            // 16C: Arrivals
            northFlow.addRunway("16C", true, false);
            // 16R: Departures
            northFlow.addRunway("16R", false, true);
            // 17: Departures
            northFlow.addRunway("17", false, true);
            registry.registerFlow("KSEA", northFlow);
        }
        
        // South Flow: Wind from 130-230 (winds favor runways 34/35)
        {
            AirportFlow southFlow("SOUTH", 130, 230);
            // 34L: Arrivals
            southFlow.addRunway("34L", true, false);
            // 34C: Arrivals
            southFlow.addRunway("34C", true, false);
            // 34R: Departures
            southFlow.addRunway("34R", false, true);
            // 35: Departures
            southFlow.addRunway("35", false, true);
            registry.registerFlow("KSEA", southFlow);
        }
        
        // ===== KSFO - San Francisco International =====
        // West Flow (most common): Wind from 240-300 (winds favor runways 28)
        {
            AirportFlow westFlow("WEST", 240, 300);
            // 28L: Arrivals
            westFlow.addRunway("28L", true, false);
            // 28R: Arrivals + Departures (shared)
            westFlow.addRunway("28R", true, true);
            // 27L: Departures
            westFlow.addRunway("27L", false, true);
            // 27R: Departures
            westFlow.addRunway("27R", false, true);
            registry.registerFlow("KSFO", westFlow);
        }
        
        // Southeast Flow: Wind from 120-160 (winds favor runways 10)
        {
            AirportFlow seFlow("SOUTHEAST", 120, 160);
            // 10L: Departures
            seFlow.addRunway("10L", false, true);
            // 10R: Arrivals
            seFlow.addRunway("10R", true, false);
            registry.registerFlow("KSFO", seFlow);
        }
        
        // ===== LEMD - Madrid Barajas =====
        // West Flow: Wind from 240-300 (winds favor runways 32L/32R/36L/36R)
        {
            AirportFlow westFlow("WEST", 240, 300);
            // 32L: Arrivals
            westFlow.addRunway("32L", true, false);
            // 32R: Arrivals + Departures
            westFlow.addRunway("32R", true, true);
            // 36L: Departures
            westFlow.addRunway("36L", false, true);
            // 36R: Departures
            westFlow.addRunway("36R", false, true);
            registry.registerFlow("LEMD", westFlow);
        }
        
        // East Flow: Wind from 060-120 (winds favor runways 14L/14R/18L/18R)
        {
            AirportFlow eastFlow("EAST", 60, 120);
            // 14L: Departures
            eastFlow.addRunway("14L", false, true);
            // 14R: Arrivals
            eastFlow.addRunway("14R", true, false);
            // 18L: Arrivals
            eastFlow.addRunway("18L", true, false);
            // 18R: Departures
            eastFlow.addRunway("18R", false, true);
            registry.registerFlow("LEMD", eastFlow);
        }
        
        // ===== LEBL - Barcelona El Prat =====
        // Northeast Flow: Wind from 030-090 (winds favor runways 25L/25R)
        {
            AirportFlow neFlow("NORTHEAST", 30, 90);
            // 25L: Arrivals
            neFlow.addRunway("25L", true, false);
            // 25R: Arrivals + Departures
            neFlow.addRunway("25R", true, true);
            // 20: Departures (secondary)
            neFlow.addRunway("20", false, true);
            registry.registerFlow("LEBL", neFlow);
        }
        
        // Southwest Flow: Wind from 210-270 (winds favor runways 07L/07R)
        {
            AirportFlow swFlow("SOUTHWEST", 210, 270);
            // 07L: Arrivals
            swFlow.addRunway("07L", true, false);
            // 07R: Arrivals + Departures
            swFlow.addRunway("07R", true, true);
            // 02: Departures (secondary)
            swFlow.addRunway("02", false, true);
            registry.registerFlow("LEBL", swFlow);
        }
        
        // ===== KLAX - Los Angeles International =====
        // West Flow (most common): Wind from 240-300
        {
            AirportFlow westFlow("WEST", 240, 300);
            // 24L: Arrivals
            westFlow.addRunway("24L", true, false);
            // 24R: Arrivals
            westFlow.addRunway("24R", true, false);
            // 25L: Departures
            westFlow.addRunway("25L", false, true);
            // 25R: Departures
            westFlow.addRunway("25R", false, true);
            // 06L: Secondary arrivals (inland)
            westFlow.addRunway("06L", true, false);
            registry.registerFlow("KLAX", westFlow);
        }
        
        // East Flow: Wind from 060-120
        {
            AirportFlow eastFlow("EAST", 60, 120);
            // 06L: Arrivals
            eastFlow.addRunway("06L", true, false);
            // 06R: Arrivals
            eastFlow.addRunway("06R", true, false);
            // 07L: Departures
            eastFlow.addRunway("07L", false, true);
            // 07R: Departures
            eastFlow.addRunway("07R", false, true);
            registry.registerFlow("KLAX", eastFlow);
        }
        
        // ===== KORD - Chicago O'Hare =====
        // West Flow: Wind from 240-300 (favors runways 27/28)
        {
            AirportFlow westFlow("WEST", 240, 300);
            // 27L: Arrivals
            westFlow.addRunway("27L", true, false);
            // 27R: Arrivals
            westFlow.addRunway("27R", true, false);
            // 28L: Departures
            westFlow.addRunway("28L", false, true);
            // 28R: Departures
            westFlow.addRunway("28R", false, true);
            // 22L: Secondary arrivals
            westFlow.addRunway("22L", true, false);
            registry.registerFlow("KORD", westFlow);
        }
        
        // East Flow: Wind from 060-120 (favors runways 09/10)
        {
            AirportFlow eastFlow("EAST", 60, 120);
            // 09L: Arrivals
            eastFlow.addRunway("09L", true, false);
            // 09R: Arrivals
            eastFlow.addRunway("09R", true, false);
            // 10L: Departures
            eastFlow.addRunway("10L", false, true);
            // 10R: Departures
            eastFlow.addRunway("10R", false, true);
            // 04L: Secondary arrivals
            eastFlow.addRunway("04L", true, false);
            registry.registerFlow("KORD", eastFlow);
        }
        
        // ===== EGLL - London Heathrow =====
        // Westerly Operations (most common): Wind from 190-280
        {
            AirportFlow westerlyFlow("WESTERLY", 190, 280);
            // 27L: Arrivals
            westerlyFlow.addRunway("27L", true, false);
            // 27R: Arrivals + Departures
            westerlyFlow.addRunway("27R", true, true);
            registry.registerFlow("EGLL", westerlyFlow);
        }
        
        // Easterly Operations: Wind from 010-100
        {
            AirportFlow easterlyFlow("EASTERLY", 10, 100);
            // 09L: Arrivals + Departures
            easterlyFlow.addRunway("09L", true, true);
            // 09R: Arrivals
            easterlyFlow.addRunway("09R", true, false);
            registry.registerFlow("EGLL", easterlyFlow);
        }
        
        return registry;
    }
}
