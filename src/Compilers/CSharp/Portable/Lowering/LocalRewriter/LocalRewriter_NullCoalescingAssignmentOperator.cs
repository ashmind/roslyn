// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Immutable;
using System.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Symbols;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.CSharp
{
    internal sealed partial class LocalRewriter
    {
        public override BoundNode VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node)
        {
            return VisitNullCoalescingAssignmentOperator(node, true);
        }

        private BoundExpression VisitNullCoalescingAssignmentOperator(BoundNullCoalescingAssignmentOperator node, bool used)
        {
            BoundExpression rewrittenRight = VisitExpression(node.Right);

            var temps = ArrayBuilder<LocalSymbol>.GetInstance();
            var stores = ArrayBuilder<BoundExpression>.GetInstance();
            
            // This will be filled in with the LHS that uses temporaries to prevent
            // double-evaluation of side effects.
            var transformedLeft = TransformCompoundAssignmentLHS(node.Left, stores, temps, false);

            CSharpSyntaxNode syntax = node.Syntax;

            // OK now we have all the temporaries.
            // What we want to generate is:
            //
            // xlhs = xlhs ?? xrhs;
            //
            // TODO (coalesce-assignment): this is of course a bit lazy, what we might want is
            // (xlhs != null ? xlhs : (xlhs = MakeConversion(xhrs)));

            var readLeft = MakeRValue(transformedLeft);
            var coalesce = MakeNullCoalescingOperator(syntax, readLeft, rewrittenRight, node.LeftConversion, node.Type);
            var assignment = MakeAssignmentOperator(syntax, transformedLeft, coalesce, node.Left.Type, used: used, isChecked: false, isCompoundAssignment: true);

            // OK, at this point we have:
            //
            // * temps evaluating and storing portions of the LHS that must be evaluated only once.
            // * the "transformed" left hand side, rebuilt to use temps where necessary
            // * the assignment "xlhs = ((LEFT)xlhs ?? rhs)"
            // 
            // Notice that we have recursively rewritten the bound nodes that are things stored in
            // the temps, and by calling the "Make" methods we have rewritten the conversions and
            // assignments too, if necessary.

            BoundExpression result = (temps.Count == 0 && stores.Count == 0) ?
                assignment :
                new BoundSequence(
                    syntax,
                    temps.ToImmutable(),
                    stores.ToImmutable(),
                    assignment,
                    assignment.Type);

            temps.Free();
            stores.Free();
            return result;
        }
    }
}
