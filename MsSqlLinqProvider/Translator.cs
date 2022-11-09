using System.Linq.Expressions;
using System.Text;
namespace MsSqlLinqProvider
{
    public class Translator : ExpressionVisitor
    {
        private readonly StringBuilder _resultStringBuilder = new StringBuilder();
        public string Translate(Expression exp)
        {
            Visit(exp);
            return _resultStringBuilder.ToString();
        }
        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            _resultStringBuilder.Append($"SELECT * FROM {node.Arguments[0]} WHERE ");
            return base.VisitMethodCall(node);
        }
        protected override Expression VisitBinary(BinaryExpression node)
        {
            if (node.NodeType == ExpressionType.Equal)
            {
                Visit(node.Left);
                _resultStringBuilder.Append("=");
                if (node.Left.Type == typeof(string))
                {
                    _resultStringBuilder.Append("'");
                }
                Visit(node.Right);
                if (node.Left.Type == typeof(string))
                {
                    _resultStringBuilder.Append("'");
                }
            }
            else if (node.NodeType == ExpressionType.GreaterThan || node.NodeType == ExpressionType.LessThan)
            {
                Visit(node.Left);
                if (node.NodeType == ExpressionType.GreaterThan)
                {
                    _resultStringBuilder.Append(">");
                }
                else if (node.NodeType == ExpressionType.LessThan)
                {
                    _resultStringBuilder.Append("<");
                }
                Visit(node.Right);
            }           
            else if (node.NodeType == ExpressionType.AndAlso)
            {
                Visit(node.Left);
                var left = _resultStringBuilder.ToString();
                _resultStringBuilder.Clear();
                Visit(node.Right);
                var right = _resultStringBuilder.ToString();
                _resultStringBuilder.Clear();
                _resultStringBuilder.Append($"{left} AND {right}");
            }
            else
            {
                throw new NotSupportedException($"Operation '{node.NodeType}' is not supported");
            }
            return node;
        }
        protected override Expression VisitMember(MemberExpression node)
        {
            _resultStringBuilder.Append(node.Member.Name);
            return base.VisitMember(node);
        }
        protected override Expression VisitConstant(ConstantExpression node)
        {
            _resultStringBuilder.Append(node.Value);
            return base.VisitConstant(node);
        }
    }
}
