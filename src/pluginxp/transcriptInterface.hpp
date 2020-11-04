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
    class TransmissionOption;
    typedef function<void()> OptionSelectedCallback;
    typedef function<void(vector<TransmissionOption>& optionList)> OptionListLoadCallback;
    class TransmissionOption
    {
    public:
        string label;
        OptionSelectedCallback callback = noopOptionCallback;
        bool hasListLoader = false;
        OptionListLoadCallback listLoader = noopOptionListLoader;
        bool refreshable = false;
    };
protected:
    TranscriptInterface() = default;
public:
    virtual void setUserActionsMenu(shared_ptr<PluginMenu> menu) = 0;
    virtual void setTransmissionOptions(const vector<TransmissionOption>& options) = 0;
public:
    static void noopOptionCallback() { }
    static void noopOptionListLoader(vector<TransmissionOption>&) { }
};

class MenuBasedTranscriptInterface : public TranscriptInterface
{
private:
    shared_ptr<HostServices> m_host;
    shared_ptr<PluginMenu> m_userActionsMenu;
    vector<shared_ptr<PluginMenu::Item>> m_items;
    vector<shared_ptr<PluginMenu>> m_subMenus;
public:
    explicit MenuBasedTranscriptInterface(shared_ptr<HostServices> _host) :
        m_host(_host)
    {
    }
public:
    void setUserActionsMenu(shared_ptr<PluginMenu> _menu) override
    {
        setTransmissionOptions({});
        m_userActionsMenu = _menu;
    }

    void setTransmissionOptions(const vector<TransmissionOption>& options) override
    {
        if (!m_userActionsMenu)
        {
            return;
        }

        m_host->writeLog("MenuBasedTranscriptInterface::setTransmissionOptions:1");
        m_items.clear();
        m_subMenus.clear();

        for (const auto& option : options)
        {
            if (option.hasListLoader)
            {
                auto subMenu = shared_ptr<PluginMenu>(new PluginMenu(option.label, *m_userActionsMenu));
                m_subMenus.push_back(subMenu);

                vector<TransmissionOption> listOptions;
                option.listLoader(listOptions);
                for (const auto& listOption : listOptions)
                {
                    auto listItem = shared_ptr<PluginMenu::Item>(new PluginMenu::Item(*subMenu, listOption.label, listOption.callback));
                    m_items.push_back(listItem);
                }

                if (option.refreshable)
                {
                    vector<TransmissionOption> copyOfOptions = options;
                    auto refreshItem = shared_ptr<PluginMenu::Item>(new PluginMenu::Item(*subMenu, "(refresh list)", [=]{
                        setTransmissionOptions(copyOfOptions);
                    }));
                    m_items.push_back(refreshItem);
                }
            }
            else
            {
                auto item = shared_ptr<PluginMenu::Item>(new PluginMenu::Item(*m_userActionsMenu, option.label, option.callback));
                m_items.push_back(item);
            }
        }

        m_host->writeLog("MenuBasedTranscriptInterface::setTransmissionOptions:2");
    }
};
