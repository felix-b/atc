// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <string>
#include <sstream>
#include "libworld.h"
#include "intentTypes.hpp"

using namespace std;

static const int PHONETIC_ALPHABET_SIZE = 26;
static const char* PHONETIC_ALPHABET[PHONETIC_ALPHABET_SIZE] = {
    "Alpha",
    "Bravo",
    "Charlie",
    "Delta",
    "Echo",
    "Foxtrot",
    "Golf",
    "Hotel",
    "India",
    "Juliett",
    "Kilo",
    "Lima",
    "Mike",
    "November",
    "Oscar",
    "Papa",
    "Quebec",
    "Romeo",
    "Sierra",
    "Tango",
    "Uniform",
    "Victor",
    "Whiskey",
    "X-ray",
    "Yankee",
    "Zulu"
};

namespace world
{

    class SimplePhraseologyService : public PhraseologyService
    {
    private:
        shared_ptr<HostServices> m_host;
        unordered_map<int, function<void(UtteranceBuilder& builder, shared_ptr<Intent> intent)>> m_verbalizerByIntentCode;
    public:
        SimplePhraseologyService(shared_ptr<HostServices> _host) :
            m_host(_host)
        {
            buildVerbalizerMap();
        }
    public:
        shared_ptr<Utterance> verbalizeIntent(shared_ptr<Intent> intent) override
        {
            auto found = m_verbalizerByIntentCode.find(intent->code());
            if (found == m_verbalizerByIntentCode.end())
            {
                throw runtime_error(
                    "SimplePhraseologyService has no verbalizer for intent code " + 
                    to_string(intent->code()));
            }
            
            const auto& verbalizer = found->second;

            UtteranceBuilder builder;
            verbalizer(builder, intent);

            return builder.getUtterance();
        }

    private:

        void buildVerbalizerMap()
        {
            //TODO: define a macro???

            m_verbalizerByIntentCode.insert({ PilotAffirmationIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizePilotAffirmation(builder, dynamic_pointer_cast<PilotAffirmationIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotHandoffReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizePilotHandoffReadback(builder, dynamic_pointer_cast<PilotHandoffReadbackIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotIfrClearanceRequestIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeIfrClearanceRequest(builder, dynamic_pointer_cast<PilotIfrClearanceRequestIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ DeliveryIfrClearanceReplyIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeIfrClearanceReply(builder, dynamic_pointer_cast<DeliveryIfrClearanceReplyIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotIfrClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeIfrClearanceReadback(builder, dynamic_pointer_cast<PilotIfrClearanceReadbackIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ DeliveryIfrClearanceReadbackCorrectIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeIfrClearanceReadbackCorrect(builder, dynamic_pointer_cast<DeliveryIfrClearanceReadbackCorrectIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotPushAndStartRequestIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizePushAndStartRequest(builder, dynamic_pointer_cast<PilotPushAndStartRequestIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ GroundPushAndStartReplyIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizePushAndStartReply(builder, dynamic_pointer_cast<GroundPushAndStartReplyIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotDepartureTaxiRequestIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeDepartureTaxiRequest(builder, dynamic_pointer_cast<PilotDepartureTaxiRequestIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ GroundDepartureTaxiReplyIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeDepartureTaxiReply(builder, dynamic_pointer_cast<GroundDepartureTaxiReplyIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotDepartureTaxiReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeDepartureTaxiReadback(builder, dynamic_pointer_cast<PilotDepartureTaxiReadbackIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotReportHoldingShortIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeHoldingShort(builder, dynamic_pointer_cast<PilotReportHoldingShortIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ GroundRunwayCrossClearanceIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) {
                verbalizeRunwayCrossClearance(builder, dynamic_pointer_cast<GroundRunwayCrossClearanceIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ GroundSwitchToTowerIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeSwitchToTower(builder, dynamic_pointer_cast<GroundSwitchToTowerIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotCheckInWithTowerIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeCheckInWithTower(builder, dynamic_pointer_cast<PilotCheckInWithTowerIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ TowerLineUpIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLineUp(builder, dynamic_pointer_cast<TowerLineUpIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotLineUpReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLineUpReadback(builder, dynamic_pointer_cast<PilotLineUpReadbackIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ TowerClearedForTakeoffIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeTakeoffClearance(builder, dynamic_pointer_cast<TowerClearedForTakeoffIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotTakeoffClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeTakeoffClearanceReadback(builder, dynamic_pointer_cast<PilotTakeoffClearanceReadbackIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotTakeoffClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeReportFinal(builder, dynamic_pointer_cast<PilotReportFinalIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotTakeoffClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLandingClearance(builder, dynamic_pointer_cast<TowerClearedForLandingIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotTakeoffClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLandingClearanceReadback(builder, dynamic_pointer_cast<PilotLandingClearanceReadbackIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotReportFinalIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeReportFinal(builder, dynamic_pointer_cast<PilotReportFinalIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ TowerClearedForLandingIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLandingClearance(builder, dynamic_pointer_cast<TowerClearedForLandingIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotLandingClearanceReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) { 
                verbalizeLandingClearanceReadback(builder, dynamic_pointer_cast<PilotLandingClearanceReadbackIntent>(intent));
            }});


            m_verbalizerByIntentCode.insert({ PilotArrivalCheckInWithGroundIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) {
                verbalizeArrivalCheckInWithGround(builder, dynamic_pointer_cast<PilotArrivalCheckInWithGroundIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ GroundArrivalTaxiReplyIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) {
                verbalizeArrivalTaxiReply(builder, dynamic_pointer_cast<GroundArrivalTaxiReplyIntent>(intent));
            }});

            m_verbalizerByIntentCode.insert({ PilotArrivalTaxiReadbackIntent::IntentCode, [this](UtteranceBuilder& builder, shared_ptr<Intent> intent) {
                verbalizeArrivalTaxiReadback(builder, dynamic_pointer_cast<PilotArrivalTaxiReadbackIntent>(intent));
            }});
        }

        void verbalizePilotAffirmation(UtteranceBuilder& builder, shared_ptr<PilotAffirmationIntent> intent)
        {
            builder.addAffirmation(isHeads(intent) ? "Roger" : (isMale(intent) ? "copy that" : "copy"));
            if (isHeads(intent))
            {
                builder.addPunctuation();
                builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
            }
        }

        void verbalizePilotHandoffReadback(UtteranceBuilder& builder, shared_ptr<PilotHandoffReadbackIntent> intent)
        {
            if (isTails(intent))
            {
                builder.addData(spellFrequency(intent->newFrequencyKhz()));
                builder.addFarewell("thank you");
            }
            else
            {
                builder.addFarewell("Roger");
            }
        }

        void verbalizeIfrClearanceRequest(UtteranceBuilder& builder, shared_ptr<PilotIfrClearanceRequestIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("with");
            builder.addData(spellPhoneticString(intent->atisLetter()), isHeads(intent));
            builder.addText("at gate");
            builder.addData(intent->subjectFlight()->plan()->departureGate());
            builder.addText(isHeads(intent) ? "ready to copy IFR clearance" : "requesting IFR clearance");
        }

        void verbalizeIfrClearanceReply(UtteranceBuilder& builder, shared_ptr<DeliveryIfrClearanceReplyIntent> intent)
        {
            auto flightPlan = intent->subjectFlight()->plan();
            stringstream text;

            if (intent->cleared())
            {
                auto clearance = intent->clearance();
                auto departureGround = 
                    m_host->getWorld()->getAirport(flightPlan->departureAirportIcao())->groundAt(intent->subjectFlight()->aircraft()->location());

                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addPunctuation();
                builder.addText(isHeads(intent) ? "cleared to" : "you are cleared to");
                builder.addData(spellPhoneticString(clearance->limit()));
                builder.addText("via", isHeads(intent));
                builder.addDisfluency("uhm", isHeads(intent));
                builder.addData(clearance->sid(), true);
                builder.addText("departure and");
                builder.addData(clearance->transition(), true);
                builder.addText("transition then as filed");
                builder.addPunctuation();
                builder.addText("altimeter");
                builder.addData("two niner niner eight");
                builder.addText("squawk", isTails(intent));
                builder.addDisfluency("uhm", isTails(intent));
                builder.addData(spellSquawk(clearance->squawk()), true);
                builder.addText("initial climb");
                builder.addData(spellAltitude(clearance->initialAltitudeFeet()));
                builder.addText("expect further clearance in");
                builder.addData(to_string(clearance->furtherClearanceInMinutes()) + " minutes");
            }
            else
            {
                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addPunctuation();
                builder.addText("your flight plan was not filed in the system.");
            }
        }

        void verbalizeIfrClearanceReadback(UtteranceBuilder& builder, shared_ptr<PilotIfrClearanceReadbackIntent> intent)
        {
            auto clearance = intent->clearance();

            builder.addText("To");
            builder.addData(spellPhoneticString(clearance->limit()));
            builder.addText("via");
            builder.addData(clearance->sid());
            builder.addText("and");
            builder.addData(clearance->transition());
            builder.addText("squawk");
            builder.addData(spellSquawk(clearance->squawk()));
            builder.addText("climb");
            builder.addData(spellAltitude(clearance->initialAltitudeFeet()));
            builder.addPunctuation();
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

        void verbalizeIfrClearanceReadbackCorrect(UtteranceBuilder& builder, shared_ptr<DeliveryIfrClearanceReadbackCorrectIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("readback correct");
            builder.addPunctuation();
            builder.addText("Contact ground on");
            builder.addData(spellFrequency(intent->groundKhz()), true);
            builder.addText(isHeads(intent) ? "when ready" : "for taxi");
            builder.addFarewell(isHeads(intent) ? "have a good flight" : "have a good one");
        }

        void verbalizePushAndStartRequest(UtteranceBuilder& builder, shared_ptr<PilotPushAndStartRequestIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("at gate");
            builder.addData(intent->subjectFlight()->plan()->departureGate());
            builder.addText(isHeads(intent) ? "push with Quebec" : "information Quebec requesting push and start");
        }

        void verbalizePushAndStartReply(UtteranceBuilder& builder, shared_ptr<GroundPushAndStartReplyIntent> intent)
        {
            stringstream text;

            if (intent->approved())
            {
                auto approval = intent->approval();

                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addText(isHeads(intent) ? "push approved, tail north south line" : "push tail north southline");
                builder.addPunctuation();
                builder.addText(isHeads(intent) ? "expect runway" : "active runway is");
                builder.addData(spellRunway(approval->departureRunway()));
                builder.addPunctuation();
                builder.addText(isHeads(intent) ? "advise when ready to taxi" : "call back for taxi");
            }
            else
            {
                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addPunctuation();
                builder.addText("stay on my frequency, expect delay of thirty minutes, we're at peak load");
            }
        }

        void verbalizeDepartureTaxiRequest(UtteranceBuilder& builder, shared_ptr<PilotDepartureTaxiRequestIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("requesting taxi to active");
        }

        void verbalizeDepartureTaxiReply(UtteranceBuilder& builder, shared_ptr<GroundDepartureTaxiReplyIntent> intent)
        {
            stringstream text;

            if (intent->cleared())
            {
                auto clearance = intent->clearance();

                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addText("taxi to runway");
                builder.addData(spellRunway(clearance->departureRunway()));
                builder.addPunctuation();
                builder.addText("via", isHeads(intent));
                builder.addDisfluency("uhm", isHeads(intent));
                builder.addData(spellTaxiPath(clearance->taxiPath()), true);
            }
            else
            {
                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addPunctuation();
                builder.addText("hold your position, I'll be back with you in a few minutes");
            }
        }

        void verbalizeDepartureTaxiReadback(UtteranceBuilder& builder, shared_ptr<PilotDepartureTaxiReadbackIntent> intent)
        {
            auto clearance = intent->clearance();

            builder.addText("Taxi to");
            builder.addData(spellRunway(clearance->departureRunway()));
            builder.addPunctuation();
            builder.addText("via");
            builder.addData(spellTaxiPath(clearance->taxiPath()), isHeads(intent));
            builder.addPunctuation();
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

        void verbalizeHoldingShort(UtteranceBuilder& builder, shared_ptr<PilotReportHoldingShortIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("holding short");

            if (intent->runway().length() > 0)
            {
                builder.addData("runway " + spellRunway(intent->runway()));
            }

            if (intent->holdingPoint().length() > 0)
            {
                builder.addData("at " + spellPhoneticString(intent->holdingPoint()));
            }
        }

        void verbalizeRunwayCrossClearance(UtteranceBuilder& builder, shared_ptr<GroundRunwayCrossClearanceIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addPunctuation();
            builder.addText("Ground");
            builder.addPunctuation();
            builder.addText("Cross runway");
            builder.addData(spellRunway(intent->clearance()->runwayName()));
            builder.addText("continue taxiing");
        }

        void verbalizeSwitchToTower(UtteranceBuilder& builder, shared_ptr<GroundSwitchToTowerIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("contact tower on");
            builder.addData(spellFrequency(intent->towerKhz()));
            builder.addFarewell("have a good one");
        }

        void verbalizeCheckInWithTower(UtteranceBuilder& builder, shared_ptr<PilotCheckInWithTowerIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            if (intent->haveNumbers())
            {
                builder.addText("have numbers");
            }
            if (intent->holdingPoint().length() > 0)
            {
                builder.addText("holding short at");
                builder.addData(spellPhoneticString(intent->holdingPoint()));
            }
            else
            {
                builder.addText("holding short of runway");
                builder.addData(spellRunway(intent->runway()));
            }
        }

        void verbalizeLineUp(UtteranceBuilder& builder, shared_ptr<TowerLineUpIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("line up runway");
            builder.addData(spellRunway(intent->runway()));
        }

        void verbalizeLineUpReadback(UtteranceBuilder& builder, shared_ptr<PilotLineUpReadbackIntent> intent)
        {
            builder.addText("lining up");
            builder.addData(spellRunway(intent->runway()));
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

        void verbalizeTakeoffClearance(UtteranceBuilder& builder, shared_ptr<TowerClearedForTakeoffIntent> intent)
        {
            auto clearance = intent->clearance();
            const auto& runwayEnd = m_host->getWorld()->getRunwayEnd(
                intent->subjectFlight()->plan()->departureAirportIcao(), 
                clearance->departureRunway());

            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addPunctuation();
            builder.addText("winds calm"); //TODO add winds data

            float turnDegrees = GeoMath::getTurnDegrees(runwayEnd.heading(), clearance->initialHeading());
            if (abs(turnDegrees) < 1)
            {
                builder.addText("fly runway heading");
            }
            else
            {
                builder.addText(turnDegrees > 0 ? "turn right" : "turn left");
                builder.addText("heading");
                builder.addData(spellHeading(clearance->initialHeading()));
            }

            builder.addText("contact departure on");
            builder.addData(spellFrequency(intent->departureKhz()));
            builder.addText("runway");
            builder.addData(spellRunway(intent->clearance()->departureRunway()));
            builder.addText("cleared for takeoff.");
        }

        void verbalizeTakeoffClearanceReadback(UtteranceBuilder& builder, shared_ptr<PilotTakeoffClearanceReadbackIntent> intent)
        {
            auto clearance = intent->clearance();

            builder.addText("heading");
            builder.addData(spellHeading(clearance->initialHeading()));
            builder.addText("departure on");
            builder.addData(spellFrequency(intent->departureKhz()));
            builder.addText("cleared for takeoff");
            builder.addData(spellRunway(intent->clearance()->departureRunway()));
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

        void verbalizeReportFinal(UtteranceBuilder& builder, shared_ptr<PilotReportFinalIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("final runway");
            builder.addData(spellRunway(intent->runway()));
        }

        void verbalizeLandingClearance(UtteranceBuilder& builder, shared_ptr<TowerClearedForLandingIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addText("winds calm cleared to land runway");
            builder.addData(spellRunway(intent->clearance()->runway()));
            builder.addPunctuation();
            builder.addText("when vacated contact ground on");
            builder.addData(spellFrequency(intent->clearance()->groundKhz()));
        }

        void verbalizeLandingClearanceReadback(UtteranceBuilder& builder, shared_ptr<PilotLandingClearanceReadbackIntent> intent)
        {
            builder.addText("cleared to land");
            builder.addData(spellRunway(intent->clearance()->runway()));
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

        void verbalizeArrivalCheckInWithGround(UtteranceBuilder& builder, shared_ptr<PilotArrivalCheckInWithGroundIntent> intent)
        {
            builder.addData(spellCallsign(intent->subjectControl()->callSign()));
            builder.addPunctuation();
            builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
            builder.addPunctuation();
            builder.addText("vacated runway");
            builder.addData(spellRunway(intent->runway()));
            builder.addText("at");
            builder.addData(spellPhoneticString(intent->exitName()));

            if (isHeads(intent))
            {
                builder.addPunctuation();
                builder.addText("request taxi instructions to terminal");
            }
        }

        void verbalizeArrivalTaxiReply(UtteranceBuilder& builder, shared_ptr<GroundArrivalTaxiReplyIntent> intent)
        {
            stringstream text;

            if (intent->cleared())
            {
                auto clearance = intent->clearance();

                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addText("your gate is");
                builder.addData(clearance->parkingStand());
                builder.addPunctuation();
                builder.addText("taxi via");
                builder.addDisfluency("uhm", isHeads(intent));
                builder.addData(spellTaxiPath(clearance->taxiPath()), true);
            }
            else
            {
                builder.addData(spellCallsign(intent->subjectFlight()->callSign()));
                builder.addPunctuation();
                builder.addText("hold your position, I'll be back with you");
            }
        }

        void verbalizeArrivalTaxiReadback(UtteranceBuilder& builder, shared_ptr<PilotArrivalTaxiReadbackIntent> intent)
        {
            auto clearance = intent->clearance();

            builder.addText("Gate");
            builder.addData(spellRunway(clearance->parkingStand()));
            builder.addPunctuation();
            builder.addText("taxi via");
            builder.addData(spellTaxiPath(clearance->taxiPath()), isHeads(intent));
            builder.addPunctuation();
            builder.addFarewell(spellCallsign(intent->subjectFlight()->callSign()));
        }

    public:

        static string spellPhoneticString(string s)
        {
            stringstream result;

            for (int i = 0 ; i < s.length() ; i++)
            {
                if (i > 0)
                {
                    result << " ";
                }

                char c = s[i];
                if (c >= '0' && c <= '9')
                {
                    result << c;
                }
                else
                {
                    result << spellPhoneticChar(c);
                }
            }

            return result.str();
        }

        static string spellPhoneticChar(char c)
        {
            int index = toupper(c) - 'A';
            return index >= 0 && index < PHONETIC_ALPHABET_SIZE
                   ? PHONETIC_ALPHABET[index]
                   : "?";
        }

        static string spellFrequencyKhz(int khz)
        {
            return to_string(khz / 1000) + " point " + to_string(khz % 1000);
        }

        static string spellTaxiPath(shared_ptr<TaxiPath> taxiPath)
        {
            stringstream text;
            auto steps = taxiPath->toHumanFriendlySteps();

            for (int i = 0 ; i < steps.size() ; i++)
            {
                if (i > 0)
                {
                    text << " ";
                }
                text << spellPhoneticString(steps[i]);
            }

            return text.str();
        }

        static string spellFrequency(int khz)
        {
            stringstream text;

            int mhzOnly = khz / 1000;
            int khzOnly = khz % 1000;

            int mhzDigits[3] = { mhzOnly / 100, (mhzOnly / 10) % 10, mhzOnly % 10 };
            int khzDigits[3] = { khzOnly / 100, (khzOnly / 10) % 10, khzOnly % 10 };

            text << mhzDigits[0] << ' ' << mhzDigits[1] << ' ' << mhzDigits[2]
                 << " point "
                 << khzDigits[0];

            if (khzDigits[1] != 0 || khzDigits[2] != 0)
            {
                text << ' ' << khzDigits[1];
            }

            if (khzDigits[2] != 0)
            {
                text << ' ' << khzDigits[2];
            }

            return text.str();
        }

        static string spellRunway(const string& name)
        {
            stringstream text;

            for (int i = 0 ; i < name.length() ; i++)
            {
                if (i > 0)
                {
                    text << ' ';
                }
                
                char c = name[i];
                switch (c)
                {
                case 'L':
                    text << "Left";
                    break;
                case 'R':
                    text << "Right";
                    break;
                case 'C':
                    text << "Center";
                    break;
                default:
                    text << c;
                }
            }

            return text.str();
        }


        static string spellSquawk(const string& squawk)
        {
            stringstream text;

            for (int i = 0 ; i < squawk.length() ; i++)
            {
                if (i > 0)
                {
                    text << ' ';
                }
                text << squawk[i];
            }

            return text.str();
        }

        static string spellIcaoCode(const string& icaoCode)
        {
            stringstream text;

            for (int i = 0 ; i < icaoCode.length() ; i++)
            {
                if (i > 0)
                {
                    text << ' ';
                }
                text << icaoCode[i];
            }

            return text.str();
        }

        static string spellHeading(float heading)
        {
            string digits = to_string((int)heading);
            stringstream text;

            for (int i = 0 ; i < digits.length() ; i++)
            {
                if (i > 0)
                {
                    text << ' ';
                }
                text << digits[i];
            }

            return text.str();
        }

        static string spellCallsign(const string& callsign)
        {
            stringstream text;

            for (int i = 0 ; i < callsign.length() ; i++)
            {
                if (i > 0 && (isdigit(callsign[i]) || isdigit(callsign[i-1])))
                {
                    text << ' ';
                }
                text << callsign[i];
            }

            return text.str();
        }

        static string spellAltitude(float feet)
        {
            return to_string((int)feet);
        }

        static bool isHeads(const shared_ptr<Intent>& intent)
        {
            return ((intent->subjectFlight()->pilot()->id() % 2) == 0);
        }

        static bool isTails(const shared_ptr<Intent>& intent)
        {
           return !isHeads(intent);
        }

        static bool isMale(const shared_ptr<Intent>& intent)
        {
            return (intent->subjectFlight()->pilot()->gender() == Actor::Gender::Male);
        }
    };
}
