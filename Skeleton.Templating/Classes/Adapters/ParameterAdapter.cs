﻿using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.DatabaseFunctions.Adapters.Fields;

namespace Skeleton.Templating.Classes
{
    public class ParameterAdapter : Parameter
    {
        private readonly Parameter _parameter;

        public ParameterAdapter(Domain domain, Parameter parameter) : base(domain, parameter.Operation, parameter.Name, parameter.ClrType, parameter.ProviderTypeName)
        {
            _parameter = parameter;
        }

        public override string Name => _parameter.Name;

        public override int Order => _parameter.Order;

        public override Type ClrType => _parameter.ClrType;

        public override string ProviderTypeName => _parameter.ProviderTypeName;

        public override Field RelatedTypeField => _parameter.RelatedTypeField;

        public bool HasSize => _parameter.Size != null;

        public override int? Size => _parameter.Size;

        public override bool IsRequired => _parameter.IsRequired;

        public bool RelatedFieldHasReferenceType => _parameter.RelatedTypeField != null && _parameter.RelatedTypeField.HasReferenceType;

        public override bool IsInt => _parameter.ClrType == typeof(int) || _parameter.ClrType == typeof(int?); // needed in react template to determine if we need to parseInt or not

        public bool UserEditable
        {
            get
            {
                return _parameter?.Attributes?.userEditable == true || IsCustomType || IsCustomArrayType || (_parameter.RelatedTypeField != null &&
                                                                       _parameter.RelatedTypeField.IsCallerProvided);
            }
        }

        public string ResolvedClrType
        {
            get
            {
                if (IsCustomType)
                {
                    return Util.CSharpNameFromName(_parameter.ProviderTypeName);
                }

                if (IsCustomArrayType)
                {
                    return $"List<{Util.CSharpNameFromName(_parameter.ProviderTypeName)}>";
                }
                
                return Util.FormatClrType(_parameter.ClrType);
            }
        }
        
        public string ApiResolvedClrType
        {
            get
            {
                if (IsCustomType)
                {
                    return Util.CSharpNameFromName(_parameter.ProviderTypeName) + NamingConventions.ModelClassNameSuffix;
                }
                else
                {
                    return ResolvedClrType;
                }
            }
        }

        public string ResolvedTypescriptType
        {
            get
            {
                if (IsCustomType)
                {
                    return Util.CSharpNameFromName(_parameter.ProviderTypeName);                    
                }
                
                if (IsCustomArrayType)
                {
                    return $"{Util.CSharpNameFromName(_parameter.ProviderTypeName)}[]";
                }
                
                return Util.GetTypeScriptTypeForClrType(_parameter.ClrType);
            }
        }

        public string ResolvedDartType
        {
            get
            {
                if (IsCustomType)
                {
                    return Util.CSharpNameFromName(_parameter.ProviderTypeName);                    
                }
                
                if (IsCustomArrayType)
                {
                    return $"List<{Util.CSharpNameFromName(_parameter.ProviderTypeName)}>";
                }
                
                return Util.GetDartTypeForClrType(_parameter.ClrType);
            }
        }

        public string ResolvedTypescriptTypeUnderlying
        {
            get
            {
                if (IsCustomTypeOrCustomArray)
                {
                    return Util.CSharpNameFromName(_parameter.ProviderTypeName);                    
                }
                
                return Util.GetTypeScriptTypeForClrType(_parameter.ClrType);
            }
        }

        public bool IsCustomArrayType => ClrType == typeof(List<ResultType>);

        public bool IsCustomTypeOrCustomArray => IsCustomType || IsCustomArrayType;
        
        public ResultType CustomType
        {
            get
            {
                if (IsCustomTypeOrCustomArray)
                {
                    return _domain.ResultTypes.Single(rt => rt.Name == _parameter.ProviderTypeName && rt.Namespace == _parameter.Operation.Namespace);
                }

                return null;
            }
        }

        public ClientCustomTypeModel ClientCustomType
        {
            get
            {
                if (IsCustomTypeOrCustomArray)
                {
                    return new ClientCustomTypeModel(CustomType);
                }

                return null;
            }
        }

        private List<string> _pagingParameterNames = null;
        
        public bool IsPagingParameter
        {
            get
            {
                if (_pagingParameterNames == null)
                {
                    // lazy-load list of paging param names
                    var pageSizeParamName = PageSizeField.GetNameForNamingConvention(_domain.NamingConvention);
                    var pageNumParamName = PageNumberField.GetNameForNamingConvention(_domain.NamingConvention);
                    var sortParameterName = _domain.TypeProvider.CreateSortParameter(_domain.NamingConvention).Name;
                    var sortDescendingParameterName = SortDescendingParameter.GetNameForNamingConvention(_domain.NamingConvention);
                
                    _pagingParameterNames = new List<string> { pageSizeParamName, pageNumParamName, sortParameterName, sortDescendingParameterName };
                }
                
                return _pagingParameterNames.Contains(Name);
            }  
        }
    }
}
