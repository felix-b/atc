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

using namespace std;
using namespace world;

class PluginMenu
{
public:
    class Item
    {
    public:
        friend class PluginMenu;
    private:
        string m_text;
        PluginMenu& m_menu;
        function<void()> m_action;
    public:
        Item(PluginMenu& _menu, string  _text, function<void()> _action) :
            m_menu(_menu),
            m_text(std::move(_text)),
            m_action(std::move(_action))
        {
            m_menu.addSubItem(*this);
        }
        ~Item()
        {
            m_menu.removeSubItem(*this);
        }
    public:
        const string& text() const { return m_text; }
    private:
        void handler()
        {
            PrintDebugString("MENU  |item[%s] selected", m_text.c_str());
            try
            {
                m_action();
            }
            catch (const exception& e)
            {
                PrintDebugString("MENU  |item[%s] handler CRASHED!!! %s", m_text.c_str(), e.what());
            }
        }
    };
private:
    int m_itemId;
    XPLMMenuID m_subMenuId;
    vector<Item*> m_subItemIndex;
public:
    PluginMenu(const string& text)
    {
        m_itemId = XPLMAppendMenuItem(XPLMFindPluginsMenu(), text.c_str(), nullptr, 1);
        m_subMenuId = XPLMCreateMenu(text.c_str(), XPLMFindPluginsMenu(), m_itemId, menuHandler, this);
    }
    ~PluginMenu()
    {
        XPLMClearAllMenuItems(m_subMenuId);
        XPLMEnableMenuItem(XPLMFindPluginsMenu(), m_itemId, 0);
        XPLMRemoveMenuItem(XPLMFindPluginsMenu(), m_itemId);
        XPLMDestroyMenu(m_subMenuId);
    }
private:
    void addSubItem(Item& item)
    {
        int index = XPLMAppendMenuItem(m_subMenuId, item.text().c_str(), (void*)&item, 1);
        while (m_subItemIndex.size() < index + 1)
        {
            m_subItemIndex.push_back(nullptr);
        }
        m_subItemIndex[index] = &item;
        PrintDebugString("MENU  |item[%s] added", item.text().c_str());
    }
    void removeSubItem(const Item& item)
    {
        auto found = std::find(m_subItemIndex.begin(), m_subItemIndex.end(), &item);
        if (found != m_subItemIndex.end())
        {
            int index = std::distance(m_subItemIndex.begin(), found);
            if (index >= 0)
            {
                XPLMRemoveMenuItem(m_subMenuId, index);
                m_subItemIndex.erase(found);
                PrintDebugString("MENU  |item[%s] removed", item.text().c_str());
                return;
            }
        }
        PrintDebugString("MENU  |WARNING: cannot remove item[%s], index not found", item.text().c_str());
    }
private:
    static void menuHandler(void*, void *iRef)
    {
        if (iRef)
        {
            Item *item = static_cast<Item*>(iRef);
            item->handler();
        }
    }
};
