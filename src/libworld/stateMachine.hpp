//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <memory>
#include <string>
#include <algorithm>
#include <unordered_map>
#include "libworld.h"
#include "intentTypes.hpp"
#include "intentFactory.hpp"

namespace world
{
    template<class TStateId, class TTriggerId>
    class StateMachine
    {
    public:

        class State
        {
        private:
            TStateId m_id;
            string m_name;
        protected:
            State(TStateId _id, const string &_name) :
                m_id(_id),
                m_name(_name)
            {
            }

        public:

            TStateId id() const { return m_id; }
            const string &name() const { return m_name; }

        public:
            virtual void enter()
            {
            }

            virtual void exit()
            {
            }

            virtual void ping()
            {
            }

            virtual void receiveTrigger(StateMachine &machine, TTriggerId triggerId)
            {
            }
        };

        class Transition
        {
        private:
            function<bool(TTriggerId triggerId)> m_predicate;
            function<void(StateMachine &machine)> m_execute;
        public:
            Transition(
                function<bool(TTriggerId triggerId)> _predicate,
                function<void(StateMachine &machine)> _execute
            ) : m_predicate(_predicate),
                m_execute(_execute)
            {
            }
        public:
            bool apply(StateMachine &machine, TTriggerId triggerId) const
            {
                if (m_predicate(triggerId))
                {
                    m_execute(machine);
                    return true;
                }
                return false;
            }
        };

        class DeclarativeState : public State
        {
        private:
            function<void()> m_onEnter;
            function<void()> m_onExit;
            function<void()> m_onPing;
            vector<Transition> m_transitions;
        public:
            DeclarativeState(
                TStateId _id,
                const string& _name,
                const vector<Transition>& _transitions = {},
                function<void()> _onEnter = noop,
                function<void()> _onExit = noop,
                function<void()> _onPing = noop
            ) : State(_id, _name),
                m_transitions(_transitions),
                m_onEnter(std::move(_onEnter)),
                m_onExit(std::move(_onExit)),
                m_onPing(std::move(_onPing))
            {
            }
        public:
            void enter() override
            {
                m_onEnter();
            }
            void exit() override
            {
                m_onExit();
            }
            void ping() override
            {
                m_onPing();
            }
            void receiveTrigger(StateMachine &machine, TTriggerId triggerId) override
            {
                for (const auto& transition : m_transitions)
                {
                    if (transition.apply(machine, triggerId))
                    {
                        break;
                    }
                }
            }
        public:
            static void noop() { }
        };

    private:

        shared_ptr<HostServices> m_host;
        unordered_map<TStateId, function<shared_ptr<State>()>> m_stateFactories;
        shared_ptr<State> m_currentState;
        string m_nameForLog;

    protected:

        StateMachine(shared_ptr<HostServices> _host, const string& _nameForLog) :
            m_host(_host),
            m_nameForLog(_nameForLog)
        {
        }

    public:

        const string& nameForLog() const { return m_nameForLog; }
        shared_ptr<State> currentState() const { return m_currentState; }

    public:

        void addState(TStateId id, function<shared_ptr<State>()> factory)
        {
            m_stateFactories.insert({id, factory});
        }

        void receiveTrigger(TTriggerId triggerId)
        {
            if (m_currentState)
            {
                try
                {
                    m_host->writeLog(
                        "%s|STATEM state[%s] receiving trigger[%d]",
                        m_nameForLog.c_str(), m_currentState->name().c_str(), (int) triggerId);
                    m_currentState->receiveTrigger(*this, triggerId);
                }
                catch (const exception &e)
                {
                    m_host->writeLog(
                        "%s|STATEM CRASHED at state[%s] while receiving trigger!!! %s",
                        m_nameForLog.c_str(), m_currentState->name().c_str(), e.what());
                    throw;
                }
            }
            else
            {
                m_host->writeLog(
                    "%s|STATEM WARNING: received trigger[%d], ignoring due no current state",
                    m_nameForLog.c_str(), (int) triggerId);
            }
        }

        void ping()
        {
            if (m_currentState)
            {
                try
                {
                    m_host->writeLog(
                        "%s|STATEM state[%s] receiving ping",
                        m_nameForLog.c_str(), m_currentState->name().c_str());

                    m_currentState->ping();
                }
                catch (const exception &e)
                {
                    m_host->writeLog(
                        "%s|STATEM CRASHED at state[%s] while receiving ping!!! %s",
                        m_nameForLog.c_str(), m_currentState->name().c_str(), e.what());
                    throw;
                }
            }
            else
            {
                m_host->writeLog(
                    "%s|STATEM WARNING: received ping, ignoring due no current state",
                    m_nameForLog.c_str());
            }
        }

        void transitionToState(TStateId stateId)
        {
            auto factoryIt = m_stateFactories.find(stateId);
            if (factoryIt == m_stateFactories.end())
            {
                throw runtime_error(
                    "State machine [" + m_nameForLog + "] has no state with id [" + to_string((int) stateId) + "]");
            }

            const auto& factory = factoryIt->second;
            exitCurrentState();

            shared_ptr<State> newState;

            try
            {
                newState = factory();
            }
            catch (const exception &e)
            {
                m_host->writeLog(
                    "%s|STATEM CRASHED while instantiating state [%d]!!! %s",
                    m_nameForLog.c_str(), (int) stateId, e.what());
                throw;
            }

            enterNewState(newState);
        }

    protected:

        shared_ptr<HostServices> host() const { return m_host; }

    private:

        void exitCurrentState()
        {
            if (m_currentState)
            {
                try
                {
                    m_host->writeLog(
                        "%s|STATEM exiting state[%s]",
                        m_nameForLog.c_str(), m_currentState->name().c_str());

                    m_currentState->exit();
                    m_currentState.reset();
                }
                catch (const exception &e)
                {
                    m_host->writeLog(
                        "%s|STATEM CRASHED while transitioning state (exit)!!! %s",
                        m_nameForLog.c_str(), e.what());
                    throw;
                }
            }
        }

        void enterNewState(shared_ptr<State> newState)
        {
            try
            {
                m_currentState = newState;

                if (m_currentState)
                {
                    m_host->writeLog(
                        "%s|STATEM entering state[%s]",
                        m_nameForLog.c_str(), m_currentState->name().c_str());

                    m_currentState->enter();

                    m_host->writeLog(
                        "%s|STATEM transitioned to state[%s]",
                        m_nameForLog.c_str(), m_currentState->name().c_str());
                }
            }
            catch (const exception &e)
            {
                m_host->writeLog(
                    "%s|STATEM CRASHED while transitioning state (enter)!!! %s",
                    m_nameForLog.c_str(), e.what());
                throw;
            }
        }
    };
}
