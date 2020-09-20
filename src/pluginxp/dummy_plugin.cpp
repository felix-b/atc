#include "XPLMPlugin.h"
#include "XPLMUtilities.h"
#include <cstring>

PLUGIN_API int XPluginStart(char * outName, char * outSig, char * outDesc) {
    // Plugin details
	strcpy(outName, "Air Traffic & Control Plugin");
	strcpy(outSig, "felix-b.atc");
	strcpy(outDesc, "More information https://github.com/felix-b/atc");

    // You probably want this on
	XPLMEnableFeature("XPLM_USE_NATIVE_PATHS", 1);
	XPLMDebugString("ATC> XPluginStart\n");

	return 1;
}

PLUGIN_API void	XPluginStop(void) {
	XPLMDebugString("ATC> XPluginStop\n");
}

PLUGIN_API void XPluginDisable(void) {
	XPLMDebugString("ATC> XPluginDisable\n");
}

PLUGIN_API int XPluginEnable(void) {
	XPLMDebugString("ATC> XPluginEnable\n");
	return 1;
}

PLUGIN_API void XPluginReceiveMessage(XPLMPluginID, intptr_t inMessage, void * inParam) {
	XPLMDebugString("ATC> XPluginReceiveMessage\n");
}
