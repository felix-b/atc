using System.Globalization;
using Atc.Data.Primitives;
using Atc.Speech.Abstractions;

namespace Atc.World.Abstractions
{
    public interface IVerbalizationService
    {
        IVerbalizer GetVerbalizer(Party speaker, LanguageCode language);
    }
    
    public interface IVerbalizer
    {
        UtteranceDescription VerbalizeIntent(Party speaker, Intent intent);
        LanguageCode Language { get; }
    }
}
