﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SmartAddresser.Editor.Core.Models.Shared.AssetGroups.AssetFilterImpl
{
    /// <summary>
    ///     Filter to pass assets if matches the type.
    /// </summary>
    [Serializable]
    [AssetFilter("Type Filter", "Type Filter")]
    public sealed class TypeBasedAssetFilter : AssetFilterBase
    {
        [SerializeField] private TypeReferenceListableProperty _type = new TypeReferenceListableProperty();
        [SerializeField] private bool _matchWithDerivedTypes = true;
        private List<string> _invalidAssemblyQualifiedNames = new List<string>();

        private Dictionary<Type, bool> _resultCache = new Dictionary<Type, bool>();
        private object _resultCacheLocker = new object();
        private List<Type> _types = new List<Type>();

        /// <summary>
        ///     Type.
        /// </summary>
        public TypeReferenceListableProperty Type => _type;

        public bool MatchWithDerivedTypes
        {
            get => _matchWithDerivedTypes;
            set => _matchWithDerivedTypes = value;
        }

        public override void SetupForMatching()
        {
            _types.Clear();
            _invalidAssemblyQualifiedNames.Clear();
            foreach (var typeRef in _type)
            {
                if (typeRef == null)
                    continue;

                if (!typeRef.IsValid())
                    continue;

                var assemblyQualifiedName = typeRef.AssemblyQualifiedName;
                var type = System.Type.GetType(assemblyQualifiedName);
                if (type == null)
                    _invalidAssemblyQualifiedNames.Add(assemblyQualifiedName);
                else
                    _types.Add(type);
            }

            _resultCache.Clear();
        }

        public override bool Validate(out string errorMessage)
        {
            if (_invalidAssemblyQualifiedNames.Count >= 1)
            {
                var sb = new StringBuilder();
                sb.Append($"[{GetType().Name}] Unknown types: ");
                foreach (var invalidAssemblyQualifiedNames in _invalidAssemblyQualifiedNames)
                {
                    sb.Append(invalidAssemblyQualifiedNames);
                    sb.Append(" / ");
                }

                // Remove the last " / ".
                sb.Remove(sb.Length - 3, 3);

                errorMessage = sb.ToString();
                return false;
            }

            errorMessage = null;
            return true;
        }

        /// <inheritdoc />
        public override bool IsMatch(string assetPath, Type assetType, bool isFolder)
        {
            if (assetType == null)
                return false;

            if (_resultCache.TryGetValue(assetType, out var result))
                return result;

            foreach (var type in _types)
            {
                if (type == assetType)
                {
                    result = true;
                    break;
                }

                if (_matchWithDerivedTypes && assetType.IsSubclassOf(type))
                {
                    result = true;
                    break;
                }
            }

            lock (_resultCacheLocker)
            {
                _resultCache.Add(assetType, result);
            }

            return result;
        }

        public override string GetDescription()
        {
            var result = new StringBuilder();
            var elementCount = 0;
            foreach (var type in _type)
            {
                if (type == null || string.IsNullOrEmpty(type.Name))
                    continue;

                if (elementCount >= 1)
                    result.Append(" || ");

                result.Append(type.Name);
                elementCount++;
            }

            if (result.Length >= 1)
            {
                if (elementCount >= 2)
                {
                    result.Insert(0, "( ");
                    result.Append(" )");
                }

                result.Insert(0, "Type: ");
            }

            if (MatchWithDerivedTypes)
                result.Append(" and derived types");

            return result.ToString();
        }
    }
}
