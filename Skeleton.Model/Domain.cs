#nullable enable
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using Serilog;
using Skeleton.Model.NamingConventions;

namespace Skeleton.Model
{
    public class Domain
    {
        public ITypeProvider TypeProvider { get; }
        
        public INamingConvention NamingConvention { get; }
        
        public Settings Settings { get; }

        public Domain(Settings settings, ITypeProvider typeProvider, INamingConvention namingConvention)
        {
            Settings = settings;
            Types = new List<ApplicationType>();
            Operations = new List<Operation>();
            ResultTypes = new List<ResultType>();
            TypeProvider = typeProvider;
            NamingConvention = namingConvention;
            DefaultNamespace = settings.ApplicationName;
        }

        public List<ApplicationType> Types { get;  }

        public List<Operation> Operations { get; }

        public List<ResultType> ResultTypes { get; }

        public string DefaultNamespace { get; set; }

        public List<ApplicationType> FilteredTypes
        {
            get
            {
                if (Settings.TypeName != null)
                {
                    return Types.Where(t => t.Name == Settings.TypeName).ToList();
                }

                return Types;
            }
        }

        public List<string> ExcludedSchemas => Settings.ExcludedSchemas;

        public ApplicationType? UserType => Types.SingleOrDefault(t => t.IsSecurityPrincipal);

        public ApplicationType? HelpType => Types.SingleOrDefault(t => t.IsHelp);

        public bool HasHelpType => HelpType != null;

        public Operation? LogOperation => Operations.SingleOrDefault(o => o.IsLog);
        
        public Field? UserIdentity
        {
            get
            {
                if (UserType != null)
                {
                    var id = UserType.Fields.Single(f => f.IsKey);
                    return id;
                }

                return null;
            }
        }
        
        public SimpleType? FindTypeByFields(List<Field> fields, Operation operation, bool ignoreCase)
        {
            if (operation == null) throw new ArgumentNullException(nameof(operation));
            if (operation?.Attributes?.applicationtype != null)
            {
                var applicationType = operation.Attributes.applicationtype.ToString();
                var type = Types.FirstOrDefault(a => a.Name == applicationType && a.Namespace == operation.Namespace);
                if (type != null)
                {
                    if (FieldsMatch(fields, type.Fields, ignoreCase))
                    {
                        return type;
                    }
                }
            }

            var possibleName = FindPossibleNameForTypeFromOperationName(operation!.Name);
            if (!string.IsNullOrEmpty(possibleName))
            {
                var possibleMatch = Types.FirstOrDefault(a => a.Name == possibleName && a.Namespace == operation.Namespace);
                if (possibleMatch != null)
                {
                    if (FieldsMatch(fields, possibleMatch.Fields, ignoreCase))
                    {
                        return possibleMatch; 
                    }
                }
            }

            return null;
        }

        private bool FieldsMatch(List<Field> fields, List<Field> possibleMatchFields, bool ignoreCase)
        {
            if (fields.Count != possibleMatchFields.Count(f => !f.IsExcludedFromResults))
            {
                return false;
            }

            foreach (var field in fields)
            {
                if (!possibleMatchFields.Where(f => !f.IsExcludedFromResults).Any(f => ((f.Name == field.Name && !ignoreCase) || (ignoreCase && f.Name.ToLowerInvariant() == field.Name.ToLowerInvariant())) && f.ProviderTypeName == field.ProviderTypeName))
                {
                    return false;
                }
            }

            return true;
        }
        
        private string? FindPossibleNameForTypeFromOperationName(string operationName)
        {
            // try to find the underlying entity name so we can check that first - queries should be named <noun>_<verb>_<suffix> e.g. customer_address_select_by_customer -> we want customer_address
            if (operationName.IndexOf("_select_") > 0)
            {
                return operationName.Substring(0, operationName.IndexOf("_select_"));
            }

            return null;
        }
        
        public void UpdateResultFieldPropertiesFromApplicationType(Operation operation, ResultType result)
        {
            var appTypeName = operation.Attributes.applicationtype.ToString();
            var appType = Types.FirstOrDefault(t => t.Name == appTypeName && t.Namespace == operation.Namespace);
            if (appType == null)
            {
                appType = Types.FirstOrDefault(t => t.Name == appTypeName);
                if (appType == null)
                {
                    return;
                }
            }

            foreach (var resFld in result.Fields)
            {
                var fld = appType.Fields.FirstOrDefault(f => f.Name == resFld.Name);
                if (fld != null)
                {
                    if (resFld.ClrType == null)
                    {
                        Log.Warning("For Operation {OperationName} No CLR type specified for result field {FieldName} with provider type {ProviderTypeName}", operation.Name, resFld.Name, resFld.ProviderTypeName);
                    }
                    else
                    {
                        // e.g. fld is int? and resFld is int
                        if ((resFld.ClrType != fld.ClrType) && typeof(Nullable<>).MakeGenericType(resFld.ClrType) == fld.ClrType)
                        {
                            resFld.ClrType = fld.ClrType;
                        }    
                    }
                    
                    resFld.Size = fld.Size;

                    if (resFld.Attributes != null)
                    {
                        if (fld.Attributes != null)
                        {
                            // merge the two lists of attributes
                            resFld.Attributes = Combine(fld.Attributes, resFld.Attributes);
                        }
                    }
                    else
                    {
                        resFld.Attributes = fld.Attributes;
                    }

                    resFld.IsKey = fld.IsKey;
                    resFld.IsRequired = fld.IsRequired;
                    resFld.IsComputed = fld.IsComputed;
                    resFld.ReferencesType = fld.ReferencesType;
                    resFld.ReferencesTypeField = fld.ReferencesTypeField;
                }
            }
            
            static dynamic Combine(dynamic item1, dynamic item2)
            {
                var dictionary1 = (IDictionary<string, object>)item1;
                var dictionary2 = (IDictionary<string, object>)item2;
                var result = new ExpandoObject();
                var d = result as IDictionary<string, object>; //work with the Expando as a Dictionary

                foreach (var pair in dictionary1.Concat(dictionary2))
                {
                    d[pair.Key] = pair.Value;
                }

                return result;
            }
        }
    }
}
