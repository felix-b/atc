using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;

namespace Just.Cli
{
    public class CommandLineParser
    {
        private readonly CommandParser _rootParser;

        private CommandLineParser(CommandParser rootParser)
        {
            _rootParser = rootParser;
        }
        
        public bool Parse(string[] args)
        {
            return _rootParser.Parse(args);
        }

        public string GetSyntaxHelpText(bool recursive, int widthChars = 80, int indentChars = 3, string? newLine = null)
        {
            var builder = new HelpTextBuilder();
            _rootParser.ContributeHelp(builder, recursive);
            return builder.GetSyntaxHelpText(newLine ?? Environment.NewLine, widthChars, indentChars);
        }

        public string GetFullHelpText(bool recursive, int widthChars = 80, int indentChars = 3, string? newLine = null)
        {
            var builder = new HelpTextBuilder();
            _rootParser.ContributeHelp(builder, recursive);
            return builder.GetFullHelpText(newLine ?? Environment.NewLine, widthChars, indentChars);
        }

        public static Combinator NewBuilder()
        {
            return new Combinator();
        }

        public interface IWithHelp
        {
            Combinator WithHelp(string help);
            Combinator WithHelp(string alias, string help);
        }
        
        public class Combinator : IWithHelp
        {
            private readonly Combinator? _parent;
            private readonly List<TokenState> _tokens;
            private readonly List<Action> _parseCompletedCallbacks;

            public Combinator()
                : this(parent: null)
            {
            }

            private Combinator(Combinator? parent)
            {
                _parent = parent;
                _tokens = new();
                _parseCompletedCallbacks = new();
            }

            public IWithHelp NamedFlag(string longVariant, Action<bool> onMatch)
            {
                return NamedFlag(longVariant, shortVariant: null, onMatch);
            }

            public IWithHelp NamedFlag(string longVariant, string? shortVariant, Action<bool> onMatch)
            {
                ValidateNameVariants(
                    longVariant, 
                    shortVariant, 
                    out var validLongVariant, 
                    out var validShortVariant);
                
                var token = new NamedFlagTokenState(
                    owner: this, 
                    isRequired: false, 
                    validLongVariant, 
                    validShortVariant, 
                    onMatch);
                
                _tokens.Add(token);
                return token;
            }

            public IWithHelp NamedValue<T>(string longVariant, Action<T> onMatch)
            {
                return NamedValue<T>(longVariant, shortVariant: null, onMatch);
            }

            public IWithHelp NamedValue<T>(string longVariant, string? shortVariant, Action<T> onMatch)
            {
                ValidateNameVariants(
                    longVariant, 
                    shortVariant, 
                    out var validLongVariant, 
                    out var validShortVariant);

                var token = new NamedValueTokenState<T>(
                    owner: this, 
                    isRequired: false, 
                    isList: false, 
                    validLongVariant, 
                    validShortVariant, 
                    onMatch);
                
                _tokens.Add(token);
                return token;
            }
            
            public IWithHelp NamedValueList<T>(string longVariant, Action<T> onMatch)
            {
                return NamedValueList<T>(longVariant, shortVariant: null, onMatch);
            }

            public IWithHelp NamedValueList<T>(string longVariant, string? shortVariant, Action<T> onMatch)
            {
                ValidateNameVariants(
                    longVariant, 
                    shortVariant, 
                    out var validLongVariant, 
                    out var validShortVariant);

                var token = new NamedValueTokenState<T>(
                    owner: this, 
                    isRequired: false, 
                    isList: true, 
                    validLongVariant, 
                    validShortVariant, 
                    onMatch);
                
                _tokens.Add(token);
                return token;
            }

            public IWithHelp Value<T>(Action<T> onMatch)
            {
                var token = new ValueTokenState<T>(this, isRequired: true, onMatch);
                _tokens.Add(token);
                return token;
            }

            public IWithHelp RequiredKeyword(string keyword, Action? onMatch = null)
            {
                var token = new KeywordTokenState(this, isRequired: true, keyword, isPositional: true, onMatch);
                _tokens.Add(token);
                return token;
            }

            public IWithHelp OptionalKeyword(string keyword, Action? onMatch = null)
            {
                var token = new KeywordTokenState(this, isRequired: false, keyword, isPositional: true, onMatch);
                _tokens.Add(token);
                return token;
            }

            public Combinator Command(string keyword, Action onMatch, Action? onParseCompleted = null)
            {
                var subCombinator = new Combinator(parent: this);
                var token = new CommandTokenState(
                    owner: this, 
                    keyword, 
                    onMatch,
                    subParserFactory: () => subCombinator.BuildParser());
                
                _tokens.Add(token);

                if (onParseCompleted != null)
                {
                    AddParseCompleteCallback(onParseCompleted);
                }
                
                return subCombinator;
            }

            public Combinator WithHelp(string help)
            {
                return this;
            }

            public Combinator WithHelp(string alias, string help)
            {
                return this;
            }

            public CommandLineParser Build()
            {
                return new CommandLineParser(BuildParser());
            }
             
            internal CommandParser BuildParser()
            {
                return new CommandParser(GetAllTokensWithParents(), _parseCompletedCallbacks);
            }

            internal IEnumerable<TokenState> GetAllTokensWithParents()
            {
                return _parent != null
                    ? _tokens.Concat(_parent.GetAllTokensWithParents().Where(c => !(c is CommandTokenState)))
                    : _tokens;
            }

            private void ValidateNameVariants(
                string inLongVariant, 
                string? inShortVariant, 
                out string outLongVariant,
                out string? outShortVariant)
            {
                if (inLongVariant.Length < 4 || inLongVariant[0] != '-' || inLongVariant[1] != '-' || inLongVariant[3] == '-')
                {
                    throw new ArgumentException("longVariant must follow the format '--name'", "longVariant");
                }

                if (inShortVariant != null && (inShortVariant.Length != 2 || inShortVariant[0] != '-' || inShortVariant[1] == '-'))
                {
                    throw new ArgumentException("shortVariant must follow the format '-c'", "shortVariant");
                }

                outLongVariant = inLongVariant.Substring(2);
                outShortVariant = inShortVariant?.Substring(1);
            }

            private void AddParseCompleteCallback(Action callback)
            {
                if (_parent != null)
                {
                    _parent.AddParseCompleteCallback(callback);   
                }
                else
                {
                    _parseCompletedCallbacks.Add(callback);
                }
            }
        }

        internal record HelpRecord(string Alias, string Help);

        internal interface ICommandParserContext
        {
            void ExpectNextToken(TokenState token);
            string[] TakeInputSuffix();
        }
        
        internal class CommandParser : ICommandParserContext
        {
            private readonly List<Action> _parseCompletedCallbacks;
            private readonly List<TokenState> _positionalTokens;
            private readonly List<TokenState> _randomTokens;
            private TokenState? _nextExpectedToken;
            private Queue<string> _args = new();

            public CommandParser(IEnumerable<TokenState> tokens, IEnumerable<Action> parseCompletedCallbacks)
            {
                _parseCompletedCallbacks = parseCompletedCallbacks.ToList();
                _positionalTokens = new List<TokenState>(tokens.Where(t => t.IsPositional));
                _randomTokens = new List<TokenState>(tokens.Where(t => !t.IsPositional));
                _nextExpectedToken = null;
            }

            public bool Parse(string[] args)
            {
                _args = new Queue<string>(args);
                
                while (_args.Count > 0)
                {
                    var arg = _args.Dequeue();

                    if (TryMatchExplicitlyExpectedToken(arg, out var failure) && !failure)
                    {
                        continue;
                    }
                    if (failure)
                    {
                        return false;
                    }

                    if (!TryMatchRandomToken(arg))
                    {
                        if (!TryMatchPositionalToken(arg))
                        {
                            return false;
                        }
                    }
                }
                
                var success = VerifyRequiredTokensMatched();
                if (success)
                {
                    InvokeParseCompletedCallbacks();
                }

                return success;

                bool VerifyRequiredTokensMatched()
                {
                    if (_nextExpectedToken != null)
                    {
                        return false;
                    }
                    
                    var allRequiredPositionalTokensMatched = (
                        _positionalTokens.Count == 0 || 
                        _positionalTokens.All(t => t.Occurrences >= t.MinOccurrences));

                    var allRequiredRandomTokensMatched = (
                        _randomTokens.Count == 0 || 
                        _randomTokens.All(t => t.Occurrences >= t.MinOccurrences));

                    return allRequiredPositionalTokensMatched && allRequiredRandomTokensMatched;
                }

                void InvokeParseCompletedCallbacks()
                {
                    _parseCompletedCallbacks.ForEach(callback => callback());
                }
                
                bool TryMatchExplicitlyExpectedToken(string arg, out bool failure)
                {
                    var explicitlyExpectedToken = TakeNextExptectedToken();
                    if (explicitlyExpectedToken != null)
                    {
                        var matched = explicitlyExpectedToken.Match(arg, this);
                        failure = !matched;
                        return matched;
                    }

                    failure = false;
                    return false;
                }
                
                bool TryMatchPositionalToken(string arg)
                {
                    var result = false;
                    
                    for (int i = 0; i < _positionalTokens.Count; i++)
                    {
                        var token = _positionalTokens[i];
                        result = token.Match(arg, this); 
                        if (result)
                        {
                            var removeCount = token.Occurrences >= token.MaxOccurrences ? i + 1 : i;
                            _positionalTokens.RemoveRange(0, removeCount);
                            break;
                        }

                        if (token.Occurrences < token.MinOccurrences)
                        {
                            break;
                        }
                    }

                    return result;
                }

                bool TryMatchRandomToken(string arg)
                {
                    var result = false;
                    var syntaxKind = GetArgSyntaxKind(arg);
                        
                    for (int i = 0 ; i < _randomTokens.Count ; i++)
                    {
                        var token = _randomTokens[i];
                        var matched = token.Match(arg, this);
                            
                        if (matched)
                        {
                            result = true;
                                
                            if (token.Occurrences >= token.MaxOccurrences)
                            {
                                _randomTokens.RemoveAt(i);
                                i--;
                            }

                            if (syntaxKind != ArgSyntaxKind.NamedOptionShortMultiple)
                            {
                                break;
                            }
                        }
                    }

                    return result;
                }
            }

            void ICommandParserContext.ExpectNextToken(TokenState token)
            {
                _nextExpectedToken = token;
            }

            string[] ICommandParserContext.TakeInputSuffix()
            {
                var suffix = _args.ToArray();
                _args.Clear();
                return suffix;
            }

            public void ContributeHelp(HelpTextBuilder builder, bool recursive)
            {
                foreach (var token in _positionalTokens.Concat(_randomTokens))
                {
                    token.ContributeHelp(builder, recursive);
                }
            }
            
            private TokenState? TakeNextExptectedToken()
            {
                var result = _nextExpectedToken;
                _nextExpectedToken = null;
                return result;
            }

            internal static ArgSyntaxKind GetArgSyntaxKind(string arg)
            {
                if (arg.Length >= 4 && arg[0] == '-' && arg[1] == '-' && arg[2] != '-')
                {
                    return ArgSyntaxKind.NamedOptionLong;
                }

                if (arg.Length >= 2 && arg[0] == '-' && arg[1] != '-')
                {
                    var optionCount = 0;

                    for (int i = 0; i < arg.Length && arg[i] != '='; i++)
                    {
                        if (arg[i] != '-' && arg[i] != '+')
                        {
                            optionCount++;
                        }
                    }

                    return optionCount > 1 ? ArgSyntaxKind.NamedOptionShortMultiple : ArgSyntaxKind.NamedOptionShortSingle;
                }

                return ArgSyntaxKind.Other;
            }

            internal enum ArgSyntaxKind
            {
                Other,
                NamedOptionLong,
                NamedOptionShortSingle,
                NamedOptionShortMultiple
            }
        }
        
        internal abstract class TokenState : IWithHelp
        {
            protected TokenState(Combinator owner, int minOccurrences = 0, int maxOccurrences = Int32.MaxValue, bool isPositional = false)
            {
                this.Owner = owner;
                this.Help = new HelpRecord(string.Empty, string.Empty);
                this.MinOccurrences = minOccurrences;
                this.MaxOccurrences = maxOccurrences;
                this.IsPositional = isPositional;
                this.Occurrences = 0;
            }

            Combinator IWithHelp.WithHelp(string help)
            {
                this.Help = new HelpRecord(string.Empty, help);
                return Owner;
            }

            Combinator IWithHelp.WithHelp(string alias, string help)
            {
                this.Help = new HelpRecord(alias, help);
                return Owner;
            }
            
            public abstract bool Match(string arg, ICommandParserContext context);

            public virtual void ContributeHelp(HelpTextBuilder builder, bool recursive)
            {
                builder.AddOption(this.Help);
            }

            public Combinator Owner { get; }

            public bool IsPositional { get; }
            
            public int MinOccurrences { get; }

            public int MaxOccurrences { get; }

            public int Occurrences { get; protected set; }

            public HelpRecord Help { get; protected set; }
        }

        private class KeywordTokenState : TokenState
        {
            private readonly string _keyword;
            private readonly Action? _onMatch;

            public KeywordTokenState(Combinator owner, bool isRequired, string keyword, bool isPositional, Action? onMatch) 
                : base(owner, minOccurrences: isRequired ? 1 : 0, maxOccurrences: 1, isPositional)
            {
                _keyword = keyword;
                _onMatch = onMatch;
            }

            public override bool Match(string arg, ICommandParserContext context)
            {
                var matched = (arg == _keyword);
                
                if (matched)
                {
                    Occurrences++;
                    _onMatch?.Invoke();
                }

                return matched;
            }

            public override void ContributeHelp(HelpTextBuilder builder, bool recursive)
            {
                // do nothing
            }

            public override string ToString()
            {
                return _keyword;
            }
        }

        private class CommandTokenState : KeywordTokenState
        {
            private readonly Action? _onMatch;
            private readonly Func<CommandParser> _subParserFactory;

            public CommandTokenState(Combinator owner, string keyword, Action? onMatch, Func<CommandParser> subParserFactory) 
                : base(owner, isRequired: false, keyword, isPositional: false, onMatch: null)
            {
                _onMatch = onMatch;
                _subParserFactory = subParserFactory;
            }

            public override bool Match(string arg, ICommandParserContext context)
            {
                var keywordMatched = base.Match(arg, context);

                if (keywordMatched)
                {
                    var suffix = context.TakeInputSuffix();
                    var subParser = _subParserFactory();
                    var commandParsed = subParser.Parse(suffix);

                    if (commandParsed)
                    {
                        _onMatch?.Invoke();
                    }
                }

                return keywordMatched;
            }

            public override void ContributeHelp(HelpTextBuilder builder, bool recursive)
            {
                builder.AddCommand(this.Help, buildNestedHelp: nestedBuilder => {
                    if (recursive)
                    {
                        var subParser = _subParserFactory();
                        subParser.ContributeHelp(nestedBuilder, recursive);
                    }
                });
            }
        }

        private class ValueTokenState<T> : TokenState
        {
            private readonly Action<T>? _onMatch;

            public ValueTokenState(Combinator owner, bool isRequired, Action<T>? onMatch)
                : base(owner, minOccurrences: isRequired ? 1 : 0, maxOccurrences: 1, isPositional: true)
            {
                _onMatch = onMatch;
            }

            public override bool Match(string arg, ICommandParserContext context)
            {
                var matched = TryParse(arg, out var value);

                if (matched)
                {
                    Occurrences++;
                    _onMatch?.Invoke(value);
                }

                return matched;
            }

            public override string ToString()
            {
                return $"value<{typeof(T).Name}>";
            }

            private bool TryParse(string s, out T value)
            {
                if (typeof(T) == typeof(string))
                {
                    value = (T) (object) s;
                    return true;
                }

                if (typeof(T).IsEnum)
                {
                    var result = Enum.TryParse(typeof(T), s, ignoreCase: true, out var enumValue);
                    value = result ? (T) enumValue : default;
                    return result;
                }

                if (ValueTokenState.TryGetParser<T>(out var tryParse) && tryParse != null)
                {
                    return tryParse(s, out value);
                }

                value = default;
                return false;
            }
        }
        
        private static class ValueTokenState
        {
            public delegate bool TryParseDelegate<T>(string s, out T value);

            private static readonly Dictionary<Type, Delegate> _tryParseByType = new() {
                { 
                    typeof(Int32), 
                    new TryParseDelegate<Int32>(Int32.TryParse)
                },
                { 
                    typeof(Decimal), 
                    new TryParseDelegate<Decimal>(Decimal.TryParse)
                },
                { 
                    typeof(DateTime), 
                    new TryParseDelegate<DateTime>(TryParseUtcDateTime)
                },
                { 
                    typeof(TimeSpan), 
                    new TryParseDelegate<TimeSpan>(TimeSpan.TryParse)
                },
                { 
                    typeof(Boolean), 
                    new TryParseDelegate<Boolean>(Boolean.TryParse)
                },
            };

            public static bool TryGetParser<T>(out TryParseDelegate<T>? parser)
            {
                var result = _tryParseByType.TryGetValue(typeof(T), out var tryParseUntyped);
                parser = result ? (TryParseDelegate<T>)tryParseUntyped : default;
                return result;
            }

            public static bool TryParseUtcDateTime(string s, out DateTime value)
            {
                return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out value);
            }
        }
        
        private class NamedFlagTokenState : TokenState
        {
            private readonly string _longVariant;
            private readonly string? _shortVariant;
            private readonly Action<bool>? _onMatch;

            public NamedFlagTokenState(Combinator owner, bool isRequired, string longVariant, string? shortVariant, Action<bool>? onMatch) 
                : base(owner, minOccurrences: isRequired ? 1 : 0, maxOccurrences: 1, isPositional: false)
            {
                _longVariant = longVariant;
                _shortVariant = shortVariant;
                _onMatch = onMatch;
            }

            public override bool Match(string arg, ICommandParserContext context)
            {
                var syntaxKind = CommandParser.GetArgSyntaxKind(arg);
                var isLongSyntax = syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionLong;
                var isShortSyntax = ( 
                    syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionShortSingle || 
                    syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionShortMultiple);
                if (!isShortSyntax && !isLongSyntax)
                {
                    return false;
                }
                
                var equalityIndex = arg.IndexOf('=');
                var prefixDashCount = isLongSyntax ? 2 : 1;
                string optionName = equalityIndex > 0 
                    ? arg.Substring(prefixDashCount, equalityIndex - prefixDashCount)
                    : arg.Substring(prefixDashCount);
                var shortVariantOptionNameCharIndex = isShortSyntax && _shortVariant != null
                    ? optionName.IndexOf(_shortVariant)
                    : -1;
                var suffixDashCount = isLongSyntax && (optionName[^1] == '-' || optionName[^1] == '+') ? 1 : 0;  

                var matched = isLongSyntax
                    ? (_longVariant.Length == optionName.Length - suffixDashCount && optionName.StartsWith(_longVariant))
                    : (_shortVariant != null && optionName.Contains(_shortVariant));
                
                if (matched)
                {
                    Occurrences++;
                    var isOn = !OptionHasOffSwitch();
                    _onMatch?.Invoke(isOn);
                }

                return matched;

                bool OptionHasOffSwitch()
                {
                    if (isLongSyntax)
                    {
                        return arg[^1] == '-';
                    }

                    return (
                        shortVariantOptionNameCharIndex < optionName.Length - 1 &&
                        optionName[shortVariantOptionNameCharIndex + 1] == '-');
                }
            }
            
            public override string ToString()
            {
                return $"--{_longVariant}{(_shortVariant != null ? "|-" : string.Empty)}{(_shortVariant ?? string.Empty)}";
            }
        }
        
        private class NamedValueTokenState<T> : TokenState
        {
            private readonly string _longVariant;
            private readonly string? _shortVariant;
            private readonly Action<T>? _onMatch;

            public NamedValueTokenState(Combinator owner, bool isRequired, bool isList, string longVariant, string? shortVariant, Action<T>? onMatch) 
                : base(
                    owner, 
                    minOccurrences: isRequired ? 1 : 0, 
                    maxOccurrences: isList ? 4096 : 1, 
                    isPositional: false)
            {
                _longVariant = longVariant;
                _shortVariant = shortVariant;
                _onMatch = onMatch;
            }

            public override bool Match(string arg, ICommandParserContext context)
            {
                var syntaxKind = CommandParser.GetArgSyntaxKind(arg);
                var isLongSyntax = syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionLong;
                var isShortSyntax = ( 
                    syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionShortSingle || 
                    syntaxKind == CommandParser.ArgSyntaxKind.NamedOptionShortMultiple);
                if (!isShortSyntax && !isLongSyntax)
                {
                    return false;
                }
                
                var equalityIndex = arg.IndexOf('=');
                var prefixDashCount = isLongSyntax ? 2 : 1;
                var optionName = equalityIndex > 0 
                    ? arg.Substring(prefixDashCount, equalityIndex - prefixDashCount)
                    : arg.Substring(prefixDashCount);

                var matched = isLongSyntax
                    ? (optionName == _longVariant)
                    : (_shortVariant != null && optionName.Contains(_shortVariant));
                
                if (matched)
                {
                    Occurrences++;

                    var valueToken = new ValueTokenState<T>(Owner, isRequired: MinOccurrences > 0, _onMatch);
                    if (equalityIndex >= 0)
                    {
                        return valueToken.Match(arg.Substring(equalityIndex + 1), context);
                    }
                    
                    context.ExpectNextToken(valueToken);
                    return true;
                }

                return matched;
            }

            public override string ToString()
            {
                return $"--{_longVariant}{(_shortVariant != null ? "|-" : string.Empty)}{(_shortVariant ?? string.Empty)}=<{typeof(T).Name}>";
            }
        }
    }
}
