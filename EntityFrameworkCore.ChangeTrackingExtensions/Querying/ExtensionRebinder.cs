using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

#if EF_CORE
namespace EntityFrameworkCore.ChangeTrackingExtensions
#elif EF_6
namespace EntityFramework.ChangeTrackingExtensions
#else
namespace QueryableExtensions
#endif
{
    internal class ExtensionRebinder : ExpressionVisitor
    {
        readonly object _originalQueryable;
        readonly Expression _replacementQueryable;
        readonly List<KeyValuePair<string, Expression>> _argumentReplacements;

        public ExtensionRebinder(
            object originalQueryable, Expression replacementQueryable,
            List<KeyValuePair<string, Expression>> argumentReplacements)
        {
            _originalQueryable = originalQueryable;
            _replacementQueryable = replacementQueryable;
            _argumentReplacements = argumentReplacements;
        }

        protected override Expression VisitConstant(ConstantExpression node)
        {
            return node.Value == _originalQueryable ? _replacementQueryable : node;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.NodeType == ExpressionType.MemberAccess
                && node.Expression.NodeType == ExpressionType.Constant
                && node.Expression.Type.GetTypeInfo().IsDefined(typeof(CompilerGeneratedAttribute)))
            {
                string argumentName = node.Member.Name;

                Expression replacement = _argumentReplacements
                    .Where(p => p.Key == argumentName)
                    .Select(p => p.Value)
                    .FirstOrDefault();

                if (replacement != null)
                {
                    return replacement;
                }
            }
            return base.VisitMember(node);
        }
    }
}
