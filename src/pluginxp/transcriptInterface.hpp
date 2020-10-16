//
// This file is part of AT&C project which simulates virtual world of air traffic and ATC.
// Code licensing terms are available at https://github.com/felix-b/atc/blob/master/LICENSE
//
#pragma once

#include <string>
#include <utility>
#include <vector>
#include <algorithm>
#include <functional>
#include <utility>

// SDK
#include "XPLMPlugin.h"
#include "XPLMDisplay.h"
#include "XPLMMenus.h"
#include "XPWidgets.h"
#include "XPStandardWidgets.h"

#include "utils.h"
#include "libworld.h"
#include "pluginMenu.hpp"

using namespace std;
using namespace world;

class TranscriptInterface
{
public:
    class TransmissionOption
    {
    public:
        string label;
        function<void()> callback;
    };
protected:
    TranscriptInterface() = default;
public:
    virtual void setTransmissionOptions(const vector<TransmissionOption>& options) = 0;
};

class MenuBasedTranscriptInterface : public TranscriptInterface
{
private:
    shared_ptr<HostServices> m_host;
    PluginMenu& m_menu;
    vector<shared_ptr<PluginMenu::Item>> m_items;
public:
    explicit MenuBasedTranscriptInterface(shared_ptr<HostServices> _host, PluginMenu& _menu) :
        m_host(_host),
        m_menu(_menu)
    {
    }
public:

    void setTransmissionOptions(const vector<TransmissionOption>& options) override
    {
        m_host->writeLog("MenuBasedTranscriptInterface::setTransmissionOptions:1");

        m_items.clear();

        for (const auto& option : options)
        {
            auto item = shared_ptr<PluginMenu::Item>(new PluginMenu::Item(m_menu, option.label, option.callback));
            m_items.push_back(item);
        }

        m_host->writeLog("MenuBasedTranscriptInterface::setTransmissionOptions:2");
    }
};
