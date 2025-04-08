using System;
using System.Linq;
using System.Linq.Expressions;
using System.Collections.Generic;
using TabletopTunes.Core.Entities;

namespace TabletopTunes.Core.Common
{
    public class TrackQueryParser
    {
        private int position = 0;
        private List<QueryToken> tokens = new();

        public Expression<Func<TrackEntity, bool>> ParseQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return track => true;

            tokens = Tokenize(query);
            position = 0;

            var parameter = Expression.Parameter(typeof(TrackEntity), "track");
            var expression = ParseExpression(parameter);
            
            return Expression.Lambda<Func<TrackEntity, bool>>(expression, parameter);
        }

        private List<QueryToken> Tokenize(string query)
        {
            var tokens = new List<QueryToken>();
            var currentToken = "";
            var i = 0;

            while (i < query.Length)
            {
                var c = query[i];

                switch (c)
                {
                    case '#':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        i++;
                        while (i < query.Length && !char.IsWhiteSpace(query[i]) && !"&|!()".Contains(query[i]))
                        {
                            currentToken += query[i];
                            i++;
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.Tag, Value = currentToken.Trim() });
                        currentToken = "";
                        continue;

                    case '&':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.And });
                        break;

                    case '|':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.Or });
                        break;

                    case '!':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.Not });
                        break;

                    case '(':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.OpenParen });
                        break;

                    case ')':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        tokens.Add(new QueryToken { Type = QueryToken.TokenType.CloseParen });
                        break;

                    case ' ':
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
                            currentToken = "";
                        }
                        break;

                    default:
                        currentToken += c;
                        break;
                }
                i++;
            }

            if (currentToken.Length > 0)
            {
                tokens.Add(new QueryToken { Type = QueryToken.TokenType.Text, Value = currentToken.Trim() });
            }

            return tokens;
        }

        private Expression ParseExpression(ParameterExpression parameter)
        {
            var expr = ParseTerm(parameter);

            while (position < tokens.Count && tokens[position].Type == QueryToken.TokenType.Or)
            {
                position++;
                var right = ParseTerm(parameter);
                expr = Expression.OrElse(expr, right);
            }

            return expr;
        }

        private Expression ParseTerm(ParameterExpression parameter)
        {
            var expr = ParseFactor(parameter);

            while (position < tokens.Count && tokens[position].Type == QueryToken.TokenType.And)
            {
                position++;
                var right = ParseFactor(parameter);
                expr = Expression.AndAlso(expr, right);
            }

            return expr;
        }

        private Expression ParseFactor(ParameterExpression parameter)
        {
            if (position >= tokens.Count)
                return Expression.Constant(true);

            var token = tokens[position];
            position++;

            switch (token.Type)
            {
                case QueryToken.TokenType.Tag:
                    {
                        var tagName = token.Value!;
                        var trackTags = Expression.Property(parameter, "TrackTags");
                        var tt = Expression.Parameter(typeof(TrackTag), "tt");
                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(typeof(TrackTag));
                        
                        var tagNameEquals = Expression.Equal(
                            Expression.Property(Expression.Property(tt, "Tag"), "Name"),
                            Expression.Constant(tagName)
                        );
                        var lambda = Expression.Lambda<Func<TrackTag, bool>>(tagNameEquals, tt);
                        
                        return Expression.Call(null, anyMethod, trackTags, lambda);
                    }

                case QueryToken.TokenType.Text:
                    {
                        var searchText = token.Value!.ToLower();
                        var title = Expression.Property(parameter, "Title");
                        var toLowerCase = Expression.Call(title, typeof(string).GetMethod("ToLower", Type.EmptyTypes)!);
                        var contains = Expression.Call(toLowerCase, 
                            typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                            Expression.Constant(searchText));
                            
                        // Also search in tags
                        var tt = Expression.Parameter(typeof(TrackTag), "tt");
                        var trackTags = Expression.Property(parameter, "TrackTags");
                        var anyMethod = typeof(Enumerable).GetMethods()
                            .First(m => m.Name == "Any" && m.GetParameters().Length == 2)
                            .MakeGenericMethod(typeof(TrackTag));

                        var tagSearch = Expression.Call(null, anyMethod, trackTags,
                            Expression.Lambda<Func<TrackTag, bool>>(
                                Expression.Call(
                                    Expression.Call(
                                        Expression.Property(Expression.Property(tt, "Tag"), "Name"),
                                        typeof(string).GetMethod("ToLower", Type.EmptyTypes)!
                                    ),
                                    typeof(string).GetMethod("Contains", new[] { typeof(string) })!,
                                    Expression.Constant(searchText)
                                ),
                                tt
                            ));
                            
                        return Expression.OrElse(contains, tagSearch);
                    }

                case QueryToken.TokenType.Not:
                    {
                        var expr = ParseFactor(parameter);
                        return Expression.Not(expr);
                    }

                case QueryToken.TokenType.OpenParen:
                    {
                        var subExpr = ParseExpression(parameter);
                        if (position < tokens.Count && tokens[position].Type == QueryToken.TokenType.CloseParen)
                            position++;
                        return subExpr;
                    }

                default:
                    return Expression.Constant(true);
            }
        }
    }
}