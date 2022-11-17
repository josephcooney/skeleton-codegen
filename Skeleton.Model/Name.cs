using System;
using System.Collections.Generic;
using System.Linq;
using Pluralize.NET;
using Serilog;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Model
{
    public class Name
    {
        private static IPluralize _pluralize = new Pluralizer();
        private INamingConvention _namingConvention;
        private readonly Func<SimpleType> _relatedTypeFunc;
        private List<string> _parts = new List<string>();
        private string _originalName;
        
        public Name(string originalName, INamingConvention namingConvention, Func<SimpleType> relatedTypeFunc)
        {
            _originalName = originalName;
            _namingConvention = namingConvention;
            _relatedTypeFunc = relatedTypeFunc;
            _parts = _namingConvention.GetNameParts(originalName).ToList();
        }

        public List<string> Parts => new List<string>(_parts);

        public override string ToString()
        {
            return _originalName;
        }
        
        

        public Name BareName
        {
            get
            {
                var relatedType = _relatedTypeFunc();
                if (relatedType != null)
                {
                    var bareName = _originalName.Replace(relatedType.Name.ToString(), "").Trim('_').Replace("__", "_");
                    Log.Debug("Bare Name of {BareName} was determined from original name {OriginalName} with related type {RelatedTypeName}", bareName, _originalName, relatedType.Name);
                    return new Name(bareName, _namingConvention, _relatedTypeFunc);
                }
                
                Log.Warning("Unable to determine bare name for original name {OperationName} , because it has no related type", _originalName);
                return null;
            }
        }

        public string Humanized
        {
            get
            {
                try
                {
                    return string.Join(" ", _parts.Select(p => char.ToUpperInvariant(p[0]) + p.Substring(1)));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unable to humanize name {Name}", _originalName);
                    throw;
                }
            }
        }

        public string HumanizedPlural => _pluralize.Pluralize(Humanized);

        public string TypescriptFileName => CamelCase;
        
        public string CamelCase
        {
            get
            {
                if (string.IsNullOrEmpty(_originalName))
                {
                    return _originalName;
                }

                var name = _originalName;
                if (_originalName.IndexOf('_') > -1)
                {
                    name = CSharpName;
                }

                if (name.ToUpperInvariant() == name)
                {
                    return name.ToLowerInvariant();
                }
            
                return Char.ToLowerInvariant(name[0]) + name.Substring(1);
            }
        }
        
        public string CSharpName
        {
            get
            {
                if (_parts.Count == 0)
                {
                    return _originalName;
                }

                return PascalCaseNamingConvention.PascalCaseName(_parts);    
            }
        }
        
        public string KebabCase => string.Join('-', _parts);

        public string GetAlias()
        {
            return _originalName[0].ToString().ToLowerInvariant();
        }

        public string SqlEscaped => _namingConvention.EscapeSqlReservedWord(_originalName);

        public string Canonical => _originalName.ToLowerInvariant().Replace(" ", "").Replace("-", "").Replace("_", "");
    }
}