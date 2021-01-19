# Welcome to virtual world of air traffic and control simulation [early development]

Our goal is to create a comprehensive and accurate ATC (air traffic control) simulation. We simulate both air traffic and the ATC units as two halves of a whole. The platform for user interaction is the [X-Plane flight simulator](https://www.x-plane.com/), where AT&C loads as a plugin and lets the user choose between 1st-person pilot or 1st-person controller experience. The project aims at adding both educational and entertainment value for X-Plane users. **[See demo video](https://youtu.be/VeK6mcrzLWk)**.

# Getting started

## Where to download

   - The last released version (Alpha 1) is available at [AT&C Download Page at X-Plane.org](https://forums.x-plane.org/index.php?/files/file/67391-atc-air-traffic-control-plugin/)
   - To get the very latest changes, download the [beeding-edge build](https://github.com/felix-b/atc/releases)

## Installation

1. Download **airTrafficAndControl.buildNN.zip** from one of the above sources
1. Make sure X-Plane is not running
1. Unpack **airTrafficAndControl.buildNN.zip** under the X-Plane **plugins** folder. Your directory structure should be like this:
   ```
   X-Plane 11
     |
     +-- Resources
           |
           +-- plugins
                 |
                 +-- airTrafficAndControl
                       |
                       +-- 64
                       +-- Resources
                       +-- speech
                       +-- sounds
   ```
1. Start X-Plane

## How to Use

See detailed instructions in the [Alpha 1 Release Notes](https://github.com/felix-b/atc/wiki/Alpha-1-Release-Notes)

1. Start a flight at a towered airport with taxiways and at least 3 ATC frequencies (CLR/DEL, GND, and TWR).
1. Observe AI aircraft parking at some of the gates
1. Power on the radios, and tune COM1 to CLR/DEL, GND, or TWR frequency; listen to chatter of AI pilots with AI controllers
1. Use menus to file a flight plan and transmit requests to ATC
1. Talk to CLR/DEL to get IFR clearance
1. When ready, talk to GND to get pushback approval
1. When ready to taxi, talk to GND again to get the taxi clearance
1. Taxi to departure runway as instructed; beware of AI aircraft taxiing around
1. Report to GND when holding short of runways
1. GND will hand you off to TWR; comply with TWR instructions
1. Take off according to the clearance

# We <3 Feedback!

## General feedback or suggestions

Please discuss with us! and with other potential users! Visit
[Developing a comprehensive ATC - thoughts and demos](https://forums.x-plane.org/index.php?/forums/topic/224703-developing-a-comprehensive-atc-thoughts-and-demos/) thread on X-Plane.org forum.

## If you found a bug

- First look through the exisging issues here: https://github.com/felix-b/atc/labels/bug
- If it's already reported, see if you can add helpful information to the existing issue
- If not already reported, create a new issue here: https://github.com/felix-b/atc/issues/new
  - Attach **Log.txt** from the X-Plane directory (note: it's overwritten every time you start X-Plane).
  - State expected behavior, step by step
  - State actual behavior, step by step
  
Note: in order to comment or create issues on GitHub, you need a free [GitHub account](https://github.com/join). 

If you don't have a GitHub account and you don't want to sign up, please just post [in the forum thread](https://forums.x-plane.org/index.php?/forums/topic/224703-developing-a-comprehensive-atc-thoughts-and-demos/).

# Status and roadmap

AT&C is a long term project in the early development phase. Reaching the ultimate goal will take time, and our strategy is to continuously provide the most valuable functions through frequent incremental releases. 

**[UPDATE 28-Dec-2020] After evaluating initial success of the Alpha 1, we updated the roadmap, and now working on Alpha 2. 
You can track the progress in [our Kanban board](https://github.com/felix-b/atc/projects/1). Latest changes are available immediately in the [beeding-edge builds](https://github.com/felix-b/atc/releases).**

**[UPDATE 11-Nov-2020] Alpha 1 was released for preview and testing.**

## Alpha 1

The currently available Alpha 1 version partially covers airline IFR operations at the departure airport, including:

- Partially implemented Clearance Delivery, Ground, and Tower controllers
- Arriving and departing AI traffic, limited to B738 aircraft, 3 airline liveries, and randomly generated routes

## Alpha 2

Available in the [bleeding-edge builds](https://github.com/felix-b/atc/releases):

- Load airport data from custom scenery
- Use historical real life routes from [OpenFlights.org](https://openflights.org/data.html) (temporarily, this creates airline/livery mismatch because now there are a lot of airlines, but still only 3 liveries).

The upcoming Alpha 2 release is planned to:

- Fix bugs found during Alpha 1 testing
- Use complete BlueBell CSL package (all AI aircraft types, all available liveries)
- Add Cessna 172 AI aircraft doing IFR (the first step into GA)
- Add experimental speech recognition (Windows only)
- Run on macOS

## Roadmap 

Essential highlights in no particular order:

- Cross-platform: Windows, macOS, Linux
- Player experience: 1-st person pilot, 1-st person controller
- Operations: IFR/airline, VFR/GA, IFR/GA, Cargo, Helicopters, Military 
- Controller positions: CLRDEL, GND, TWR, APP, DEP, Center, oceanic operations
- ATC interface: radio (speech synthesis and recognition), assistance mode (text transcript, mouse inputs), CPDLC 
- Workload assistance: User Pilot-Flying & AI Pilot-Monitoring
- AI ground handling (preferably, integration with existing ground handling solutions)
- Offline mode, possibly MMO mode in the future
- AI flight routes and schedules: real-life or fine-tuned by the user
- World console (flight tracker/planner): in X-Plane, external desktop app, website

We manage the complete roadmap in the [GitHub issues](https://github.com/felix-b/atc/issues), where you can find info on every planned feature.

*DISCLAIMER: While we all want to have this plugin working as soon as possible, we do NOT commit to any specific priorities or timeframes.*

# How to help

## We appreciate any help, not only programming!

If you like the idea of this project, if you would like to include a realistic ATC in your flying simulation experience as soon as possible, you can actually help this happen faster! 

Creating the most comprehensive and accurate ATC simulation involves a diversity of effort. You can help in any of these ways:

- Playing, testing, and elaborating on ideas
- Providing insights into aviation procedures and phraseology
- Providing communication transcripts, hypothetical and real
- [*not yet possible*] Configuring flight model of AI aircraft types
- [*not yet possible*] Writing workflows for AI pilots and AI controllers
- [*not yet possible*] Writing airport-specific or airspace-specific procedures
- Improving taxi network in WED airports
- [*not yet possible*] Specifying testing scenarios (this is **not** programming) 
- Writing documentation
- C++ programming (g++/CMake) for the plugin
- Lua programming for automated testing
- Front-end programming (TypeScript) for the external world console

Bullets marked as [*not yet possible*] require developing some underlying functionality first. 

## If you'd like to help

Please make sure you read these guides:
- [CODE_of_CONDUCT](https://github.com/felix-b/atc/blob/master/CODE_of_CONDUCT.md) - mandatory
- [CONTRIBUTING](https://github.com/felix-b/atc/blob/master/CONTRIBUTING.md) - how we organize our work

Should you have any questions, please don't hesitate to contact us on the X-Plane.org forum - either post to [our thread](https://forums.x-plane.org/index.php?/forums/topic/224703-developing-a-comprehensive-atc-thoughts-and-demos/) or PM felix-b or togfox.


# Open Source Acknowledgements

AT&C includes open source software, namely these awesome libraries and tools:

- [PPL library](https://github.com/PhilippMuenzel/PPL) licensed under [BSD-3-Clause](https://github.com/PhilippMuenzel/PPL/blob/master/LICENSE)
- [XPMP2 library](https://github.com/TwinFan/XPMP2) licensed under [MIT](https://github.com/TwinFan/XPMP2/blob/master/LICENSE)
- [moodycamel::ConcurrentQueue](https://github.com/cameron314/concurrentqueue) library licensed under [Simplified BSD License](https://github.com/cameron314/concurrentqueue/blob/master/LICENSE.md)
- [Google Test library](https://github.com/google/googletest) licensed under [BSD-3-Clause](https://github.com/google/googletest/blob/master/LICENSE) license.

