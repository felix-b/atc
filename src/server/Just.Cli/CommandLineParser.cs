using System;
using System.Collections.Generic;

namespace Just.Cli
{
    public class CommandLineParser
    {
        private CommandLineParser()
        {
        }
        
        public bool Parse(string[] args)
        {
            throw new NotImplementedException();
        }

        public string GetSyntaxHelpText(bool recursive)
        {
            throw new NotImplementedException();
        }

        public string GetFullHelpText(int widthChars, bool recursive)
        {
            throw new NotImplementedException();
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

            public Combinator()
            {
                _parent = null;
            }

            private Combinator(Combinator parent)
            {
                _parent = parent;
            }

            public IWithHelp NamedFlag(string longVariant, Action<bool> onMatch) 
            {
                throw new NotImplementedException();
            }

            public IWithHelp NamedFlag(string longVariant, string shortVariant, Action<bool> onMatch) 
            {
                throw new NotImplementedException();
            }

            public IWithHelp NamedValue<T>(string longVariant, Action<T> onMatch)
            {
                throw new NotImplementedException();
            }

            public IWithHelp NamedValue<T>(string longVariant, string shortVariant, Action<T> onMatch)
            {
                throw new NotImplementedException();
            }
            
            public IWithHelp NamedValueList<T>(string longVariant, Action<T[]> onMatch)
            {
                throw new NotImplementedException();
            }

            public IWithHelp NamedValueList<T>(string longVariant, string shortVariant, Action<T> onMatch)
            {
                throw new NotImplementedException();
            }

            public IWithHelp Value<T>(Action<T> onMatch)
            {
                throw new NotImplementedException();
            }

            public IWithHelp RequiredKeyword(string keyword, Action? onMatch = null)
            {
                throw new NotImplementedException();
            }

            public IWithHelp OptionalKeyword(string keyword, Action? onMatch = null)
            {
                throw new NotImplementedException();
            }

            public Combinator Command(string keyword, Action onMatch, Action? onParseCompleted = null)
            {
                throw new NotImplementedException();
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
                throw new NotImplementedException();
            }
        }

        public class FluentHelp : IWithHelp
        {
            private readonly Combinator _owner;
            private readonly object _target;
            private readonly IDictionary<object, HelpRecord> _helpMap;

            public FluentHelp(Combinator owner, object target, IDictionary<object, HelpRecord> helpMap)
            {
                _owner = owner;
                _target = target;
                _helpMap = helpMap;
            }

            public Combinator WithHelp(string help)
            {
                _helpMap[_target] = new HelpRecord(string.Empty, help);
                return _owner;
            }

            public Combinator WithHelp(string alias, string help)
            {
                _helpMap[_target] = new HelpRecord(alias, help);
                return _owner;
            }
            
            public  IDictionary<object, HelpRecord> HelpMap => _helpMap;
        }

        public record HelpRecord(string Alias, string Help);

        private abstract class TokenState : IWithHelp
        {
            protected TokenState(Combinator owner, int minOccurrences = 0, int maxOccurrences = Int32.MaxValue)
            {
                this.Owner = owner;
                this.Help = new HelpRecord(string.Empty, string.Empty);
                this.MinOccurrences = minOccurrences;
                this.MaxOccurrences = maxOccurrences;
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
            
            public abstract bool Match(string arg);

            public Combinator Owner { get; }

            public int MinOccurrences { get; }

            public int MaxOccurrences { get; }

            public int Occurrences { get; protected set; }

            public HelpRecord Help { get; protected set; }
        }

        private class CommandTokenState : TokenState
        {
            private readonly List<TokenState> _positioinalTokens = new();
            private readonly List<TokenState> _randomTokens = new();
            
            public CommandTokenState(Combinator owner) 
                : base(owner, 1, Int32.MaxValue)
            {
            }

            public override bool Match(string arg)
            {
                
            }
        }
        
        private class KeywordTokenState : TokenState
        {
            private readonly string _keyword;
            private readonly Action? _onMatch;

            public KeywordTokenState(Combinator owner, bool isRequired, string keyword, Action? onMatch) 
                : base(owner, isRequired ? 1 : 0, 1)
            {
                _keyword = keyword;
                _onMatch = onMatch;
            }

            public override bool Match(string arg)
            {
                var matched = (arg == _keyword);
                
                if (matched)
                {
                    Occurrences++;
                    _onMatch?.Invoke();
                }

                return matched;
            }
        }
    }
}