using System;
using Microsoft.CognitiveServices.Speech;

namespace Atc.Speech.AzurePlugin
{
    public static class SpeechConfigFactory
    {
        public const string SubscriptionKeyVariableName = "ATC_AZ_SPC_SUB_KEY";
        public const string RegionVariableName = "ATC_AZ_SPC_RGN";
        
        public static SpeechConfig SubscriptionFromEnvironment()
        {
            var subscriptionKey = "f2d847ff725c467a854350823aa1021c"; //Environment.GetEnvironmentVariable(SubscriptionKeyVariableName);
            var region = "westeurope"; //Environment.GetEnvironmentVariable(RegionVariableName);

            if (string.IsNullOrWhiteSpace(subscriptionKey) || string.IsNullOrWhiteSpace(region))
            {
                //TODO find right exception type
                throw new Exception(
                    $"Azure subscription is not configured, use env vars: '{SubscriptionKeyVariableName}' and '{RegionVariableName}'");
            }
            
            return SpeechConfig.FromSubscription(subscriptionKey, region);
        }
    }
}
