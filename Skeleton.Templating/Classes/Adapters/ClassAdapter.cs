using System;
using System.Collections.Generic;
using System.Linq;
using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;
using Skeleton.Templating.DatabaseFunctions;

namespace Skeleton.Templating.Classes
{
    public class ClassAdapter
    {
        protected readonly SimpleType _type;
        protected readonly Domain _domain;
        protected readonly SecurityRoles _securityRoles;

        public ClassAdapter(SimpleType type, Domain domain)
        {
            _type = type;
            _domain = domain;
            _securityRoles = new SecurityRoles(domain.Settings);
        }

        public string Name => _type.Name;

        public List<ClassFieldAdapter> Fields => _type.Fields.Select(f => new ClassFieldAdapter(f)).ToList();
        
        public List<ClassFieldAdapter> NonExcludedFields => _type.NonExcludedFields.Select(f => new ClassFieldAdapter(f)).ToList();
        
        public virtual bool GenerateConstructor
        {
            get
            {
                return !Operations.Any(o =>
                    o.ChangesOrCreatesData && o.Parameters.Any(p => p.ProviderTypeName == _type.Name));
            }
        }
        
        public string Namespace
        {
            get
            {
                if (!string.IsNullOrEmpty(_domain.DefaultNamespace) && (string.IsNullOrEmpty(_type.Namespace) || _type.Namespace == _domain.TypeProvider.DefaultNamespace))
                {
                    return _domain.DefaultNamespace;
                }

                return _type.Namespace;
            }
        }

        public OperationAdapter InsertOperation
        {
            get { return Operations.FirstOrDefault(o => o.IsInsert); }
        }

        public OperationAdapter UpdateOperation
        {
            get { return Operations.FirstOrDefault(o => o.IsUpdate); }
        }

        public OperationAdapter DeleteOperation
        {
            get { return CanDelete ? Operations.FirstOrDefault(o => o.IsDelete) : null; }
        }

        public Field IdentityField
        {
            get
            {
                var fields = _type.Fields.Where(a => a.IsKey);
                if (fields.Count() == 1)
                {
                    return fields.First();
                }
                if (fields.Count() > 1)
                {
                    throw new InvalidOperationException($"this can't handle composite keys yet, but type {_type.Name} has multiple key fields.");
                }
                if (fields.Count() == 0)
                {
                    throw new InvalidOperationException($"Type {_type.Name} doesn't have any primary key fields.");
                }
                return null;
            }
        }

        public List<Field> KeyFields => _type.Fields.Where(a => a.IsKey).ToList();

        public bool HasMultipleKeys => KeyFields.Count > 1;

        public bool CanDelete => _type is ApplicationType && ((ApplicationType)_type).DeleteType != DeleteType.None;

        public List<ClassFieldAdapter> UserEditableFields => Fields.Where(f => f.IsCallerProvided).ToList();

        public List<ClassFieldAdapter> UserEditableReferenceFields => UserEditableFields
            .Where(a => a.ReferencesType != null && !a.ReferencesType.IsSearchable && a.ReferencesType.DisplayField != null && !a.ReferencesType.Ignore)
            .ToList();

        public List<ApplicationType> UserEditableReferencedTypes => UserEditableReferenceFields.Select(f => f.ReferencesType).Distinct().ToList();

        public bool CanSearch => _type is ApplicationType && ((ApplicationType) _type).Fields.Any(f => f.IsSearch);

        public bool HasCustomResultType
        {
            get { return _domain.Operations.Any(o => !o.Ignore && o.Returns.SimpleReturnType != null && (o.Returns.SimpleReturnType is ResultType) && !((ResultType)o.Returns.SimpleReturnType).Ignore); }
        }

        public string DomainNamespace
        {
            get
            {
                if (!string.IsNullOrEmpty(_domain.Settings.DomainNamespace))
                {
                    return _domain.Settings.DomainNamespace;
                }
                else
                {
                    return $"{Util.CSharpNameFromName(Namespace)}.Data.Domain";
                }
            }
        }
        
        public virtual List<OperationAdapter> Operations
        {
            get
            {
                if (_type is ApplicationType)
                {
                    return _domain.Operations.Where(o => !o.Ignore && (o.Returns?.SimpleReturnType == _type || o.RelatedType == _type)).OrderBy(o => o.Name).Select(o => new OperationAdapter(o, _domain, (ApplicationType)_type)).ToList();
                }
                else
                {
                    var resultType = _type as ResultType;
                    return resultType.Operations.Select(o => new OperationAdapter(o, _domain, resultType.RelatedType)) // getting the related type feels wierd and hacky here
                        .ToList();
                }
            }
        }

        public Field DisplayField => _type.DisplayField;

        public bool IsAttachment => _type is ApplicationType && ((ApplicationType) _type).IsAttachment;

        public bool HasDetails
        {
            get
            {
                var t = _type as ApplicationType;
                if (t == null)
                {
                    return false;
                }

                return !t.IsReferenceData && !t.IsLink && !t.Ignore && !t.IsAttachment;
            }
        }

        public SecurityRoles SecurityRoles => _securityRoles;

        public bool Paged => (_type as ApplicationType).Paged;

        public bool HasHelp => _domain.HasHelpType;
    }
}
