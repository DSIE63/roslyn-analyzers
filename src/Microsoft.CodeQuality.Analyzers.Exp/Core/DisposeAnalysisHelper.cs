﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Analyzer.Utilities.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using Microsoft.CodeAnalysis.Operations.ControlFlow;
using Microsoft.CodeAnalysis.Operations.DataFlow;
using Microsoft.CodeAnalysis.Operations.DataFlow.DisposeAnalysis;
using Microsoft.CodeAnalysis.Operations.DataFlow.PointsToAnalysis;

namespace Microsoft.CodeQuality.Analyzers.Exp
{
    /// <summary>
    /// Helper for <see cref="DisposeAnalysis"/>.
    /// </summary>
    internal class DisposeAnalysisHelper
    {
        private static readonly string[] s_disposeOwnershipTransferLikelyTypes = new string[]
            {
                "System.IO.Stream",
                "System.IO.TextReader",
                "System.IO.TextWriter",
                "System.Resources.IResourceReader",
            };
        private static readonly ConditionalWeakTable<Compilation, DisposeAnalysisHelper> s_DisposeHelperCache =
            new ConditionalWeakTable<Compilation, DisposeAnalysisHelper>();
        private static readonly ConditionalWeakTable<Compilation, DisposeAnalysisHelper>.CreateValueCallback s_DisposeHelperCacheCallback =
            new ConditionalWeakTable<Compilation, DisposeAnalysisHelper>.CreateValueCallback(compilation => new DisposeAnalysisHelper(compilation));

        private static ImmutableHashSet<OperationKind> s_DisposableCreationKinds => ImmutableHashSet.Create(
            OperationKind.ObjectCreation,
            OperationKind.TypeParameterObjectCreation,
            OperationKind.DynamicObjectCreation,
            OperationKind.Invocation);

        private readonly WellKnownTypeProvider _wellKnownTypeProvider;
        private readonly ImmutableHashSet<INamedTypeSymbol> _disposeOwnershipTransferLikelyTypes;
        private ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>> _lazyDisposableFieldsMap;
        public INamedTypeSymbol IDisposable => _wellKnownTypeProvider.IDisposable;
        public INamedTypeSymbol Task => _wellKnownTypeProvider.Task;

        private DisposeAnalysisHelper(Compilation compilation)
        {
            _wellKnownTypeProvider = WellKnownTypeProvider.GetOrCreate(compilation);
            if (IDisposable != null)
            {
                _disposeOwnershipTransferLikelyTypes = GetDisposeOwnershipTransferLikelyTypes(compilation);
            }
        }        

        private static ImmutableHashSet<INamedTypeSymbol> GetDisposeOwnershipTransferLikelyTypes(Compilation compilation)
        {
            var builder = ImmutableHashSet.CreateBuilder<INamedTypeSymbol>();
            foreach (var typeName in s_disposeOwnershipTransferLikelyTypes)
            {
                INamedTypeSymbol typeSymbol = compilation.GetTypeByMetadataName(typeName);
                if (typeSymbol != null)
                {
                    builder.Add(typeSymbol);
                }
            }

            return builder.ToImmutable();
        }

        private void EnsureDisposableFieldsMap()
        {
            if (_lazyDisposableFieldsMap == null)
            {
                Interlocked.CompareExchange(ref _lazyDisposableFieldsMap, new ConcurrentDictionary<INamedTypeSymbol, ImmutableHashSet<IFieldSymbol>>(), null);
            }
        }

        public static bool TryGetOrCreate(Compilation compilation, out DisposeAnalysisHelper disposeHelper)
        {
            disposeHelper = s_DisposeHelperCache.GetValue(compilation, s_DisposeHelperCacheCallback);
            if (disposeHelper.IDisposable == null)
            {
                disposeHelper = null;
                return false;
            }

            return true;
        }

        public bool TryGetOrComputeResult(
            ImmutableArray<IOperation> operationBlocks,
            IMethodSymbol containingMethod,
            out DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> disposeAnalysisResult)
        {
            return TryGetOrComputeResult(operationBlocks, containingMethod, out disposeAnalysisResult, out var _);
        }

        public bool TryGetOrComputeResult(
            ImmutableArray<IOperation> operationBlocks,
            IMethodSymbol containingMethod,
            out DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> disposeAnalysisResult,
            out DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResult)
        {
            return TryGetOrComputeResult(operationBlocks, containingMethod, trackInstanceFields: false,
                disposeAnalysisResult: out disposeAnalysisResult, pointsToAnalysisResult: out pointsToAnalysisResult, trackedInstanceFieldPointsToMap: out var _);
        }

        public bool TryGetOrComputeResult(
            ImmutableArray<IOperation> operationBlocks,
            IMethodSymbol containingMethod,
            bool trackInstanceFields,
            out DataFlowAnalysisResult<DisposeBlockAnalysisResult, DisposeAbstractValue> disposeAnalysisResult,
            out DataFlowAnalysisResult<PointsToBlockAnalysisResult, PointsToAbstractValue> pointsToAnalysisResult,
            out ImmutableDictionary<IFieldSymbol, PointsToAbstractValue> trackedInstanceFieldPointsToMap)
        {
            foreach (var operationRoot in operationBlocks)
            {
                IBlockOperation topmostBlock = operationRoot.GetTopmostParentBlock();
                if (topmostBlock != null)
                {
                    var cfg = ControlFlowGraph.Create(topmostBlock);

                    // Invoking an instance method may likely invalidate all the instance field analysis state, i.e.
                    // reference type fields might be re-assigned to point to different objects in the called method.
                    // An optimistic points to analysis assumes that the points to values of instance fields don't change on invoking an instance method.
                    // A pessimistic points to analysis resets all the instance state and assumes the instance field might point to any object, hence has unknown state.
                    // For dispose analysis, we want to perform an optimistic points to analysis as we assume a disposable field is not likely to be re-assigned to a separate object in helper method invocations in Dispose.
                    pointsToAnalysisResult = PointsToAnalysis.GetOrComputeResult(cfg, containingMethod, _wellKnownTypeProvider, pessimisticAnalysis: false);
                    disposeAnalysisResult = DisposeAnalysis.GetOrComputeResult(cfg, containingMethod, _wellKnownTypeProvider, _disposeOwnershipTransferLikelyTypes, pointsToAnalysisResult,
                        trackInstanceFields, out trackedInstanceFieldPointsToMap);
                    return true;
                }
            }

            disposeAnalysisResult = null;
            pointsToAnalysisResult = null;
            trackedInstanceFieldPointsToMap = null;
            return false;
        }

        private bool HasDisposableOwnershipTransferForParameter(IMethodSymbol containingMethod) =>
            containingMethod.MethodKind == MethodKind.Constructor &&
            containingMethod.Parameters.Any(p => _disposeOwnershipTransferLikelyTypes.Contains(p.Type));

        private bool IsDisposableCreation(IOperation operation)
            => (s_DisposableCreationKinds.Contains(operation.Kind) ||
                operation.Parent is IArgumentOperation argument && argument.Parameter.RefKind == RefKind.Out) &&
               operation.Type?.IsDisposable(IDisposable) == true;

        public bool HasAnyDisposableCreationDescendant(ImmutableArray<IOperation> operationBlocks, IMethodSymbol containingMethod)
        {
            return operationBlocks.HasAnyOperationDescendant(IsDisposableCreation) ||
                HasDisposableOwnershipTransferForParameter(containingMethod);
        }

        public ImmutableHashSet<IFieldSymbol> GetDisposableFields(INamedTypeSymbol namedType)
        {
            EnsureDisposableFieldsMap();
            if (_lazyDisposableFieldsMap.TryGetValue(namedType, out ImmutableHashSet<IFieldSymbol> disposableFields))
            {
                return disposableFields;
            }

            if (!namedType.IsDisposable(IDisposable))
            {
                disposableFields = ImmutableHashSet<IFieldSymbol>.Empty;
            }
            else
            {
                disposableFields = namedType.GetMembers()
                    .OfType<IFieldSymbol>()
                    .Where(f => f.Type.IsDisposable(IDisposable) && !f.Type.DerivesFrom(_wellKnownTypeProvider.Task))
                    .ToImmutableHashSet();
            }

            return _lazyDisposableFieldsMap.GetOrAdd(namedType, disposableFields);
        }

        /// <summary>
        /// Returns true if the given <paramref name="location"/> was created for an allocation in the <paramref name="containingMethod"/>
        /// or represents a location created for a constructor parameter whose type indicates dispose ownership transfer.
        /// </summary>
        public bool IsDisposableCreationOrDisposeOwnershipTransfer(AbstractLocation location, IMethodSymbol containingMethod)
        {
            if (location.CreationOpt == null)
            {
                return location.SymbolOpt?.Kind == SymbolKind.Parameter &&
                    HasDisposableOwnershipTransferForParameter(containingMethod);
            }

            return IsDisposableCreation(location.CreationOpt);
        }
    }
}
