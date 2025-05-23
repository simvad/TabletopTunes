using System;

namespace TabletopTunes.Core.Common
{
    public class QueryToken
    {
        public enum TokenType
        {
            Tag,
            Text,
            And,
            Or,
            Not,
            OpenParen,
            CloseParen
        }

        public TokenType Type { get; set; }
        public string? Value { get; set; }
    }
}