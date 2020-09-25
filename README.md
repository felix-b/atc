# Welcome to virtual world of air traffic and control simulation [early development]

Our goal is to create the most comprehensive and most accurate ATC (air traffic control) simulation. We simulate both air traffic and the ATC units as two halves of a whole. The platform for user interaction is the [X-Plane flight simulator](https://www.x-plane.com/), where AT&C loads as a plugin and lets the user choose between 1st-person pilot or 1st-person controller experience. The project aims at adding both educational and entertainment value for X-Plane users. **[See demo video](https://youtu.be/o0yAqO8ZdUo)**.

# How to use

We provide the ["bleeding edge" build](https://github.com/felix-b/atc/releases) for download, which we automatically update with every addition or bugfix. As the name suggests, it isn't thoroughly tested. 

## We value early testing by potential users

No two computers are the same, and we want to make sure the plugin successfully runs on as many different installations as possible. If the plugin doesn't work on *your* machine, it is good for *you* to let us know, so we can fix it early. 

We appreciate feedback and suggestions. If you would like certain features to be implemented early or in a certain way, letting us know increases the chances of this happening.

## Getting started

1. Go to the [bleeding edge build](https://github.com/felix-b/atc/releases) and download **airTrafficAndControl.buildNN.zip**
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
1. Start X-Plane and start a flight at an airport or your choice
1. In the main menu, select **Plugins** -> **Air Traffic and Control** -> **Start World**

### What to expect by far

- Switch to an external camera. You should see some AI aircraft at the gates.
- Back to your flight deck, turn on radios and tune COM1 to Clearance Delivery, Ground, or Tower
- Shortly you should hear chatter of AI pilots with AI controllers
- Observe some AI aircraft landing, and other AI aircraft departing

# How to give feedback

### General feedback or suggestions

Please discuss with us! and with other potential users! Visit
[Developing a comprehensive ATC - thoughts and demos](https://forums.x-plane.org/index.php?/forums/topic/224703-developing-a-comprehensive-atc-thoughts-and-demos/) thread on X-Plane.org forum.

## If you found a bug

- First look through the exisging issues here: https://github.com/felix-b/atc/labels/bug
- If it's already reported, see if you can add helpful information to the existing issue
- If not already reported, create a new issue here: https://github.com/felix-b/atc/issues/new
  - Attach **Log.txt** from the X-Plane directory (note: it's overwritten every time you start X-Plane).
  - State expected behavior, step by step
  - State actual behavior, step by step
  
Note: in order to comment or create issues, you need a free [GitHub account](https://github.com/join). If you don't have a GitHub account and you don't want to sign up, please just post [in the forum thread](https://forums.x-plane.org/index.php?/forums/topic/224703-developing-a-comprehensive-atc-thoughts-and-demos/) instead.

# Current status and roadmap

**[UPDATE 25-Sep-2020] We are currently working on the minimal viable product (MVP) release, which we expect to happen in upcoming week(s). You can track the progress in [this Kanban board](https://github.com/felix-b/atc/projects/1).**

AT&C is a long term project in the early development phase. Reaching the ultimate goal will take time, and our strategy is to continuously provide the most valuable functions through frequent incremental releases. 

### Essential roadmap highlights in no particular order

- Cross-platform: Windows, macOS, Linux
- Player experience: 1-st person pilot, 1-st person controller
- Operations: IFR/airline, VFR/GA, IFR/GA, Cargo, Helicopters, Military 
- Controller positions: CLRDEL, GND, TWR, APP, DEP, CTR, oceanic operations
- ATC interface: radio (speech synthesis and recognition), assistance mode (text transcript, non-speech inputs), CPDLC 
- Human pilot AI assistance: PF/PM
- AI ground handling (preferably, integration with existing ground handling solutions)
- Offline mode, MMO mode
- Flight schedules: real-life or fine-tuned by the user
- World console (flight tracker/planner): in X-Plane, external desktop app, website

We manage the complete roadmap in the [GitHub issues](https://github.com/felix-b/atc/issues), where you can find info on every planned feature.

*DISCLAIMER: While we all want to have this plugin working as soon as possible, we do NOT commit to any specific priorities or timeframes.*

# How to contribute

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

