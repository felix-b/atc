// 
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
// 
#pragma once

#include <iostream>
#include <functional>

// SDK
#include "XPLMPlugin.h"
#if !XPLM300
#error This plugin requires version 300 of the SDK
#endif

// PPL 
#include "log.h"
#include "logwriter.h"
#include "menuitem.h"
#include "action.h"
#include "pluginpath.h"

// tnc
#include "utils.h"
#include "poc.h"
#include "libworld.h"
#include "multiplayerInitializer.hpp"

using namespace std;
using namespace PPL;

//const char* poc_resource_directory = "/Users/felixb/Desktop/xp/Resources/plugins/tnc/Resources";
//static string resourceDirectory = "/Users/felixb/Desktop/xp/Resources/plugins/tnc/Resources";//PluginPath::prependPluginResourcesPath("");

class PluginInstance
{
private:
    class DelegatingAction : public Action
    {
    private:
        string m_Name;
        function<void()> m_Callback;
    public:
        DelegatingAction()
            : m_Name(""), m_Callback([]() {})
        {
        }
        DelegatingAction(const string& name, function<void()> callback)
            : m_Name(name), m_Callback(callback)
        {
        }
        virtual ~DelegatingAction() = default;
        virtual const string name() const
        {
            return m_Name;
        }
        virtual void doAction()
        {
            m_Callback();
        }
    };
private:
    MultiplayerInitializer m_multiplayer;
    MenuItem m_menu;
    DelegatingAction m_startWorldAction;
    DelegatingAction m_stopWorldAction;
    DelegatingAction m_resetWorldAction;
    DelegatingAction m_reloadPluginsAction;
    DelegatingAction m_TimeX10Action;
    DelegatingAction m_TimeX20Action;
    DelegatingAction m_TimeX1Action;
    shared_ptr<TncPoc> m_currentPoc;
public:
    PluginInstance() :
        m_multiplayer(getPluginResourcesDirectory()),
        m_menu("Traffic & Control")
    {
        XPLMDebugString("TNCPOC0> PluginInstance::PluginInstance()\n");
        
        m_reloadPluginsAction = DelegatingAction("RELOAD plugins", [this]() {
            XPLMReloadPlugins();
        });

        m_startWorldAction = DelegatingAction("Start World", [this]() {
            switchToPoc(createPoc1());
        });

        m_stopWorldAction = DelegatingAction("Stop World", [this]() {
            switchToPoc(nullptr);
        });

        m_resetWorldAction = DelegatingAction("Reset World", [this]() {
            switchToPoc(nullptr);
            switchToPoc(createPoc1());
        });

        m_TimeX20Action = DelegatingAction("Time x 20", [this]() {
            if (m_currentPoc)
            {
                m_currentPoc->setTimeFactor(20);
            }
        });

        m_TimeX10Action = DelegatingAction("Time x 10", [this]() {
            if (m_currentPoc)
            {
                m_currentPoc->setTimeFactor(10);
            }
        });

        m_TimeX1Action = DelegatingAction("Time x 1", [this]() {
            if (m_currentPoc)
            {
                m_currentPoc->setTimeFactor(1);
            }
        });

        m_menu.addSubItem(&m_reloadPluginsAction);
        m_menu.addSubItem(&m_startWorldAction);
        m_menu.addSubItem(&m_stopWorldAction);
        m_menu.addSubItem(&m_resetWorldAction);
        m_menu.addSubItem(&m_TimeX10Action);
        m_menu.addSubItem(&m_TimeX20Action);
        m_menu.addSubItem(&m_TimeX1Action);
    }

    ~PluginInstance()
    {
        XPLMDebugString("TNCPOC0> PluginInstance::~PluginInstance()\n");
        switchToPoc(nullptr);
    }

private:
    void switchToPoc(shared_ptr<TncPoc> newPoc)
    {
        if (m_currentPoc)
        {
            try 
            {
                m_currentPoc->stop();
            }
            catch (const exception& e)
            {
                PrintDebugString("TNC> POC CRASHED while stopping!!! %s\r\n", e.what());
            }
        }
        
        m_currentPoc = newPoc;
        
        if (newPoc)
        {
            try 
            {
                newPoc->start();
            }
            catch (const exception& e)
            {
                PrintDebugString("TNC> POC CRASHED while starting!!! %s\r\n", e.what());
            }
        }
    }
private:
    static string getPluginResourcesDirectory()
    {
        string result = getPluginDirectory();
        result.append(XPLMGetDirectorySeparator());
        result.append("Resources");
        return result;
    }
};
