using System.Globalization;
using Atc.Data.Primitives;

namespace Atc.World.Abstractions
{
    public interface IVerbalizationService
    {
        IVerbalizer GetVerbalizer(PartyDescription speaker, LanguageCode? language = null);
    }
    
    public interface IVerbalizer
    {
        UtteranceDescription VerbalizeIntent(PartyDescription speaker, Intent intent);
        LanguageCode Language { get; }
    }
}
