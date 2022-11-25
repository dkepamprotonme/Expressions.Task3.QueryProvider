using Expressions.Task3.E3SQueryProvider.Models.Request;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace Expressions.Task3.E3SQueryProvider
{
    public class ExpressionToFtsRequestTranslator : ExpressionVisitor
    {
        readonly StringBuilder _resultStringBuilder;

        public ExpressionToFtsRequestTranslator()
        {
            _resultStringBuilder = new StringBuilder();
        }

        public string Translate(Expression exp)
        {
            Visit(exp);

            return _resultStringBuilder.ToString();
        }

        #region protected methods

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Method.DeclaringType == typeof(Queryable)
                && node.Method.Name == nameof(Queryable.Where))
            {
                var predicate = node.Arguments[1];
                Visit(predicate);

                return node;
            }

            if (node.Method.DeclaringType == typeof(string)
                && (node.Method.Name == nameof(string.Equals)
                || node.Method.Name == nameof(string.Contains)
                || node.Method.Name == nameof(string.StartsWith)
                || node.Method.Name == nameof(string.EndsWith)))
            {
                Visit(node.Object);
                _resultStringBuilder.Append("(");
                if (node.Method.Name == nameof(string.Contains) || node.Method.Name == nameof(string.EndsWith))
                {
                    _resultStringBuilder.Append("*");
                }
                var predicate = node.Arguments[0];
                Visit(predicate);
                if (node.Method.Name == nameof(string.Contains) || node.Method.Name == nameof(string.StartsWith))
                {
                    _resultStringBuilder.Append("*");
                }
                _resultStringBuilder.Append(")");
                return node;
            }

            return base.VisitMethodCall(node);
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Equal:

                    Expression memberNode;
                    Expression constantNode;

                    if (node.Left.NodeType == ExpressionType.MemberAccess && node.Right.NodeType == ExpressionType.Constant)
                    {
                        memberNode = node.Left;
                        constantNode = node.Right;
                    }

                    else if (node.Left.NodeType == ExpressionType.Constant && node.Right.NodeType == ExpressionType.MemberAccess)
                    {
                        memberNode = node.Right;
                        constantNode = node.Left;
                    }

                    else
                    {
                        var leftMessage = $"Left operand should be property or field: {node.Left.NodeType}";
                        var rightMessage = $"Right operand should be constant: {node.Right.NodeType}";
                        var message = $"{leftMessage} and {rightMessage} or vice versa.";
                        throw new NotSupportedException(message);
                    }

                    Visit(memberNode);
                    _resultStringBuilder.Append("(");
                    Visit(constantNode);
                    _resultStringBuilder.Append(")");

                    break;

                case ExpressionType.AndAlso:

                    Visit(node.Left);
                    var left = _resultStringBuilder.ToString();
                    _resultStringBuilder.Clear();

                    Visit(node.Right);
                    var right = _resultStringBuilder.ToString();
                    _resultStringBuilder.Clear();

                    var ftsQueryRequest = new FtsQueryRequest()
                    {
                        Statements = new List<Statement>
                        {
                            new Statement() { Query = left},
                            new Statement() { Query = right}
                        }
                    };

                    var ftsQueryRequestString = JsonConvert.SerializeObject(ftsQueryRequest,
                        Formatting.None, new JsonSerializerSettings()
                        {
                            NullValueHandling = NullValueHandling.Ignore,
                            DefaultValueHandling = DefaultValueHandling.Ignore
                        });

                    _resultStringBuilder.Append(ftsQueryRequestString);
 
                    break;

                default:
                    throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            };

            return node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name).Append(":");

            return base.VisitMember(node);
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);

            return node;
        }

        #endregion
    }
}
