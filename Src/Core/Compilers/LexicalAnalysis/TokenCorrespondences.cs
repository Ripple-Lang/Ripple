using System.Collections.Generic;
using System.Collections.ObjectModel;
using Ripple.Compilers.Tokens;

namespace Ripple.Compilers.LexicalAnalysis
{
    public partial class Lexer
    {
        /// <summary>
        /// 一文字で表されるシンボルとその文字との対応
        /// </summary>
        private static readonly ReadOnlyDictionary<char, SymbolToken.SymbolType> OneLenSymbols =
            new ReadOnlyDictionary<char, SymbolToken.SymbolType>(
                new Dictionary<char, SymbolToken.SymbolType> {
                    { '.', SymbolToken.SymbolType.Dot },
                    { ',', SymbolToken.SymbolType.Comma },
                    { ':', SymbolToken.SymbolType.Colon },
                    { ';', SymbolToken.SymbolType.Semicolon },
                    { '?', SymbolToken.SymbolType.Question },
                    { '{', SymbolToken.SymbolType.OpeningBrace },
                    { '}', SymbolToken.SymbolType.ClosingBrace },

                    { '(', SymbolToken.SymbolType.OpeningBracket },
                    { ')', SymbolToken.SymbolType.ClosingBracket },

                    { '[', SymbolToken.SymbolType.OpeningSquare },
                    { ']', SymbolToken.SymbolType.ClosingSquare },

                    { '+', SymbolToken.SymbolType.Plus },
                    { '-', SymbolToken.SymbolType.Minus },
                    { '*', SymbolToken.SymbolType.Mult },
                    { '/', SymbolToken.SymbolType.Div },		
                    { '<', SymbolToken.SymbolType.LessThan },
                    { '>', SymbolToken.SymbolType.GreaterThan },
		
                    { '!', SymbolToken.SymbolType.Exclamation },
                    { '=', SymbolToken.SymbolType.Equals },
			    });

        /// <summary>
        /// 二文字で表されるシンボルとその文字列との対応
        /// </summary>
        private static readonly ReadOnlyDictionary<string, SymbolToken.SymbolType> TwoLenSymbols =
            new ReadOnlyDictionary<string, SymbolToken.SymbolType>(
                new Dictionary<string, SymbolToken.SymbolType> {
                    { "++", SymbolToken.SymbolType.PlusPlus },
                    { "--", SymbolToken.SymbolType.MinusMinus },
												
                    { "+=", SymbolToken.SymbolType.AddAssign },
                    { "-=", SymbolToken.SymbolType.SubAssign },
                    { "*=", SymbolToken.SymbolType.MultAssign },
                    { "/=", SymbolToken.SymbolType.DivAssign },
				
                    { "<=", SymbolToken.SymbolType.LessOrEquals },
                    { ">=", SymbolToken.SymbolType.GreaterOrEquals },
                    { "!=", SymbolToken.SymbolType.NotEquals },
			    });

        /// <summary>
        /// 三文字で表されるシンボルとその文字列との対応
        /// </summary>
        private static readonly ReadOnlyDictionary<string, SymbolToken.SymbolType> ThreeLenSymbols =
            new ReadOnlyDictionary<string, SymbolToken.SymbolType>(
                new Dictionary<string, SymbolToken.SymbolType> {
                    { "+<=", SymbolToken.SymbolType.AddStageAssign },
                    { "-<=", SymbolToken.SymbolType.SubStageAssign },
                    { "*<=", SymbolToken.SymbolType.MultStageAssign },
                    { "/<=", SymbolToken.SymbolType.DivStageAssign },
			    });

        /// <summary>
        /// キーワードとその文字列との対応
        /// </summary>
        private static readonly ReadOnlyDictionary<string, KeywordToken.KeywordType> KeywordTypes =
            new ReadOnlyDictionary<string, KeywordToken.KeywordType>(
                new Dictionary<string, KeywordToken.KeywordType>{
                    { "func", KeywordToken.KeywordType.Func },
                    { "as", KeywordToken.KeywordType.As },
                    { "and", KeywordToken.KeywordType.And },
                    { "or", KeywordToken.KeywordType.Or },
                    { "not", KeywordToken.KeywordType.Not },
                    { "bool", KeywordToken.KeywordType.Bool },
                    { "int", KeywordToken.KeywordType.Int },
                    { "long", KeywordToken.KeywordType.Long },
                    { "float", KeywordToken.KeywordType.Float },
                    { "idiv", KeywordToken.KeywordType.Idiv },
                    { "mod" , KeywordToken.KeywordType.Mod },
                    { "true", KeywordToken.KeywordType.True },
                    { "false", KeywordToken.KeywordType.False },
                    { "nothing", KeywordToken.KeywordType.Nothing },
                    { "var", KeywordToken.KeywordType.Var },
                    { "if", KeywordToken.KeywordType.If },
                    { "else", KeywordToken.KeywordType.Else },
                    { "while", KeywordToken.KeywordType.While },
                    { "do", KeywordToken.KeywordType.Do },
                    { "for", KeywordToken.KeywordType.For },
                    { "return", KeywordToken.KeywordType.Return },
                    { "operation", KeywordToken.KeywordType.Operation },
                    { "stage", KeywordToken.KeywordType.Stage },
                    { "now", KeywordToken.KeywordType.Now },
                    { "next", KeywordToken.KeywordType.Next },
                    { "param", KeywordToken.KeywordType.Param },
                    { "init", KeywordToken.KeywordType.Init },
                    { "holds", KeywordToken.KeywordType.Holds },
                    { "each", KeywordToken.KeywordType.Each },
                    { "at", KeywordToken.KeywordType.At },
                    { "in", KeywordToken.KeywordType.In },
                    { "struct", KeywordToken.KeywordType.Struct },
                    { "to", KeywordToken.KeywordType.To },
                    { "sbyte", KeywordToken.KeywordType.SByte },
                    { "ubyte", KeywordToken.KeywordType.UByte },
                    { "break", KeywordToken.KeywordType.Break },
                    { "continue", KeywordToken.KeywordType.Continue },
                    { "parallel", KeywordToken.KeywordType.Parallel },
                });

        private static readonly ReadOnlyDictionary<SymbolToken.SymbolType, string> SymbolOriginalTokens =
            CreateSymbolOriginalTokens();

        private static readonly ReadOnlyDictionary<KeywordToken.KeywordType, string> KeywordOriginalTokens =
            CreateKeywordOriginalTokens();

        /// <summary>
        /// SymbolToken.SymbolTypeをKeyとして、それに対応するstringをvalueに持つ
        /// ReadOnlyDictionaryを生成します。
        /// </summary>
        private static ReadOnlyDictionary<SymbolToken.SymbolType, string> CreateSymbolOriginalTokens()
        {
            var symbolOriginalTokens = new Dictionary<SymbolToken.SymbolType, string>();

            foreach (var keyValuePair in OneLenSymbols)
            {
                symbolOriginalTokens.Add(keyValuePair.Value, keyValuePair.Key.ToString());
            }

            foreach (var keyValuePair in TwoLenSymbols)
            {
                symbolOriginalTokens.Add(keyValuePair.Value, keyValuePair.Key);
            }

            return new ReadOnlyDictionary<SymbolToken.SymbolType, string>(symbolOriginalTokens);
        }

        /// <summary>
        /// KeywordToken.KeywordTypeをKeyとして、それに対応するstringをvalueに持つ
        /// ReadOnlyDictionaryを生成します。
        /// </summary>
        private static ReadOnlyDictionary<KeywordToken.KeywordType, string> CreateKeywordOriginalTokens()
        {
            var keywordOriginalTokens = new Dictionary<KeywordToken.KeywordType, string>();

            foreach (var keyValuePair in KeywordTypes)
            {
                keywordOriginalTokens.Add(keyValuePair.Value, keyValuePair.Key);
            }

            return new ReadOnlyDictionary<KeywordToken.KeywordType, string>(keywordOriginalTokens);
        }

        internal static string GetSymbolOriginalToken(SymbolToken.SymbolType type)
        {
            try
            {
                return SymbolOriginalTokens[type];
            }
            catch (KeyNotFoundException)
            {
                return "@Unknown";
            }
        }

        internal static string GetKeywordOriginalToken(KeywordToken.KeywordType type)
        {
            try
            {
                return KeywordOriginalTokens[type];
            }
            catch (KeyNotFoundException)
            {
                return "@Unknown";
            }
        }
    }
}
