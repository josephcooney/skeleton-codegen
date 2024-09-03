using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Serilog;
using Skeleton.Model;
using Skeleton.Model.Operations;

namespace Skeleton.Templating.DatabaseFunctions.Adapters
{
    public class DbTypeAdapter : IOperationPrototype
    {
        protected readonly ApplicationType _applicationType;
        private readonly string[] _operation;
        private readonly Domain _domain;


        public DbTypeAdapter(ApplicationType applicationType, string[] operation, OperationType operationType, Domain domain)
        {
            _applicationType = applicationType;
            _operation = operation;
            _domain = domain;
            OperationType = operationType;
        }

        public Domain Domain => _domain;

        public virtual IPseudoField UserIdField
        {
            get
            {
                if (_domain.UserIdentity != null)
                {
                    return new UserIdField(_domain);
                }

                return null;
            }
        }

        public virtual DbTypeAdapter UserType
        {
            get
            {
                if (_domain.UserIdentity != null)
                {
                    return new DbTypeAdapter((ApplicationType)_domain.UserIdentity.Type, null, OperationType.None, _domain);
                }

                return null;
            }
        }

        public List<IPseudoField> NonPrimaryKeyFields
        {
            get
            {
                var list = new List<IPseudoField>();
                list.AddRange(_applicationType.Fields.Where(a => !a.IsKey).Select(a => _domain.TypeProvider.CreateFieldAdapter(a, this)).OrderBy(f => f.Order));
                return list;
            }
        }

        public List<IPseudoField> PrimaryKeyFields
        {
            get
            {
                var list = new List<IPseudoField>();
                list.AddRange(_applicationType.Fields.Where(a => a.IsKey).Select(a => _domain.TypeProvider.CreateFieldAdapter(a, this)).OrderBy(f => f.Order));
                return list;
            }
        }

        public IPseudoField PrimaryKeyField => PrimaryKeyFields.FirstOrDefault();

        public List<IPseudoField> UserEditableFields
        {
            get
            {
                var list = new List<IPseudoField>();
                list.AddRange(_applicationType.Fields.Where(a => a.IsCallerProvided).Select(a => _domain.TypeProvider.CreateFieldAdapter(a, this)).OrderBy(f => f.Order));
                return list;
            }
        }

        public IParamterPrototype CreatedByField
        {
            get
            {
                var field = _applicationType.Fields.FirstOrDefault(f => f.IsTrackingUser && _domain.NamingConvention.IsCreatedByFieldName(f.Name));
                if (field != null)
                {
                    return _domain.TypeProvider.CreateFieldAdapter(field, this);
                }
                return null;
            }
        }

        public bool HasCreatedByField => CreatedByField != null;

        public IPseudoField ModifiedByField
        {
            get
            {
                var field = _applicationType.Fields.FirstOrDefault(f => f.IsTrackingUser && _domain.NamingConvention.IsModifiedByFieldName(f.Name));
                if (field != null)
                {
                    return _domain.TypeProvider.CreateFieldAdapter(field, this);
                }
                return null;
            }
        }

        public bool HasModifiedByField => ModifiedByField != null;

        public virtual List<IPseudoField> SelectInputFields
        {
            get
            {
                var fields = new List<IPseudoField>();

                if (UserIdField != null)
                {
                    fields.Add(UserIdField);
                }

                return fields;
            }
        }

        public List<IPseudoField> UpdateInputFields
        {
            get
            {
                var fields = PrimaryKeyFields.Union(UserEditableFields.Where(f => f.Edit)).ToList();
                if (UserIdField != null)
                {
                    fields.Add(UserIdField);
                }
                if (ModifiedByField != null)
                {
                    fields.Add(ModifiedByField);                    
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public List<IPseudoField> InsertInputFields
        {
            get
            {
                var fields = new List<IPseudoField>();
                if (!UsesCustomInsertType)
                {
                    // in this case there should probably only be 1 field.
                    // If there were more we would be using a custom insert type
                    fields.AddRange(InsertTypeFields);
                }
                if (UserIdField != null)
                {
                    fields.Add(UserIdField);
                }
                if (CreatedByField != null)
                {
                    fields.Add(CreatedByField);
                }

                if (Domain.LogOperation != null)
                {
                    AddLogFields(fields, Domain.LogOperation);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        private void AddLogFields(List<IPseudoField> fields, Operation domainLogOperation)
        {
        }

        public bool HasInsertInputFields => InsertInputFields.Any();

        public List<IPseudoField> InsertTypeFields
        {
            get
            {
                var fields = UserEditableFields.Where(f => f.Add).ToList();
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public List<IPseudoField> DeleteInputFields
        {
            get
            {
                var fields = PrimaryKeyFields.ToList();
                if (UserIdField != null)
                {
                    fields.Add(UserIdField);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }


        public List<IPseudoField> InsertFields
        {
            get
            {
                var fields  = UserEditableFields.Where(f => f.Add).ToList();
                if (!fields.Any(f => f.IsKey) && _domain.TypeProvider.IncludeIdentityFieldsInInsertStatements)
                {
                    fields.AddRange(PrimaryKeyFields);
                }
                
                var createdDateTrackingField = _applicationType.Fields.FirstOrDefault(a => a.IsCreatedDate);
                if (createdDateTrackingField != null)
                {
                    fields.Add(_domain.TypeProvider.CreateFieldAdapter(createdDateTrackingField, this));
                }
                var searchField = _applicationType.Fields.FirstOrDefault(f => f.IsSearch);
                if (searchField != null)
                {
                    fields.Add(_domain.TypeProvider.CreateFieldAdapter(searchField, this));
                }
                if (CreatedByField != null)
                {
                    fields.Add(CreatedByField);
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public List<IPseudoField> UpdateFields
        {
            get
            {
                var fields = UserEditableFields.Where(f => f.Edit).ToList();
                var updatedDateTrackingField = _applicationType.Fields.FirstOrDefault(a => a.IsModifiedDate);
                if (updatedDateTrackingField != null)
                {
                    fields.Add(_domain.TypeProvider.CreateFieldAdapter(updatedDateTrackingField, this));
                }
                if (ModifiedByField != null)
                {
                    fields.Add(ModifiedByField);
                }
                var searchField = _applicationType.Fields.FirstOrDefault(f => f.IsSearch);
                if (searchField != null)
                {
                    fields.Add(_domain.TypeProvider.CreateFieldAdapter(searchField, this));
                }
                return fields.OrderBy(f => f.Order).ToList();
            }
        }

        public string Name => _applicationType.Name;

        public string Namespace => _applicationType.Namespace;

        public List<IParamterPrototype> Fields => _applicationType.Fields.Where(f => (!f.IsExcludedFromResults)).Select(a => _domain.TypeProvider.CreateFieldAdapter(a, this)).ToList();

        public bool HasExcludedFields => _applicationType.Fields.Any(f => f.IsExcludedFromResults);

        public virtual string FunctionName
        {
            get
            {
                var fragments = new List<string>() { _applicationType.Name };
                fragments.AddRange(_operation);
                return _domain.NamingConvention.CreateNameFromFragments(fragments);
            }
        }
        
        public OperationType OperationType { get; }

        public string ShortName => _applicationType.Name[0].ToString().ToLowerInvariant();

        public bool SoftDelete => _applicationType.Fields.Any(a => a.IsDelete);

        public bool HardDelete => _applicationType.DeleteType == DeleteType.Hard;

        public bool NoAddUI => _applicationType.Attributes?.noAddUI == true;

        public bool NoEditUI => _applicationType.Attributes?.noEditUI == true;

        public ApplicationType UnderlyingType => _applicationType;

        public bool AllowAnonView 
        {
            get
            {
                var anon = _applicationType.Attributes?.security?.anon;
                return SecurityUtil.HasViewRights(anon);
            }
        }

        public bool AllowAnonList
        {
            get
            {
                var anon = _applicationType.Attributes?.security?.anon;
                return SecurityUtil.HasListRights(anon);
            }
        }

        public bool AllowAnonAdd
        {
            get
            {
                var anon = _applicationType.Attributes?.security?.anon;
                return SecurityUtil.HasAddRights(anon);
            }
        }

        public bool AllowAnonEdit
        {
            get
            {
                var anon = _applicationType.Attributes?.security?.anon;
                return SecurityUtil.HasEditRights(anon);
            }
        }

        public bool AllowAnonDelete
        {
            get
            {
                var anon = _applicationType.Attributes?.security?.anon;
                return SecurityUtil.HasDeleteRights(anon);
            }
        }

        public bool AllowUserView
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user == null || SecurityUtil.HasViewRights(user);
            }
        }

        public bool AllowUserList
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user == null || SecurityUtil.HasListRights(user);
            }
        }

        public bool AllowUserReadAll
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user != null && !SecurityUtil.Contains(user, SecurityRights.None) && SecurityUtil.Contains(user, SecurityRights.ReadAll);
            }
        }

        public bool AllowUserAdd
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user == null || SecurityUtil.HasAddRights(user);
            }
        }

        public bool AllowUserEditAll
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user != null && !SecurityUtil.Contains(user, SecurityRights.None) && SecurityUtil.Contains(user, SecurityRights.EditAll);
            }
        }

        public bool AllowUserEdit
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user == null || SecurityUtil.HasEditRights(user);
            }
        }

        public bool AllowUserDelete
        {
            get
            {
                var user = _applicationType.Attributes?.security?.user;
                return user == null || SecurityUtil.HasDeleteRights(user);
            }
        }

        protected void GetRelatedOwnershipExpression(string currentUserIdentifier, IEnumerable<Field> relatedIdentity, StringBuilder sb, Func<Field, string> getAliasFunc)
        {
            sb.AppendLine("\t(");
            foreach (var identityField in relatedIdentity)
            {
                
                sb.AppendLine(
                    $"\t\t{getAliasFunc(identityField)}.{Util.EscapeSqlReservedWord(identityField.Name)} = {currentUserIdentifier}");
                if (identityField != relatedIdentity.Last())
                {
                    sb.AppendLine("\t\t OR");
                }
            }
            sb.AppendLine("\t)");
        }

        // find a related type with a created by or modified by field
        public List<Field> LinkToOwershipType
        {
            get
            {
                var fields = new List<Field>();

                // TODO - this should be generalised to do something recursive, to find the path no matter how complicated the graph.

                // first level traversal 
                if (GenerateLinkToOwershipType(fields, _applicationType)) return fields;

                // second-level relationships
                var related = _applicationType.Fields.Where(f => f.HasReferenceType);
                foreach (var field in related)
                {
                    fields = new List<Field>();
                    fields.Add(field);
                    if (GenerateLinkToOwershipType(fields, field.ReferencesType))
                    {
                        return fields;
                    }
                }

                return null; // TODO
            }
        }

        public string AddManyArrayItemVariableName => "item";
        public string NewRecordParameterName =>  Util.MakeDbNameNotEscaped(new List<string> {Name, "to", "add"});

        public string NewTypeName => Util.MakeDbNameNotEscaped([Name, "new"]);

        public bool UsesCustomInsertType
        {
            get
            {
                return Fields.Count(f => f.IsUserEditable) > 1;
            }
        }

        public bool AddMany => _applicationType.Attributes?.addMany == true;

        private bool GenerateLinkToOwershipType(List<Field> fields, ApplicationType type)
        {
            var directlink = GetFieldsLinkingToOwnedType(type);
            if (directlink != null)
            {
                fields.Add(directlink);
                fields.Add(directlink.ReferencesType.Fields.SingleOrDefault(f => f.IsTrackingUser));
                return true;
            }

            return false;
        }

        private Field GetFieldsLinkingToOwnedType(ApplicationType type)
        {
            try
            {
                return type.Fields.FirstOrDefault(f =>
                    f.HasReferenceType && f.ReferencesType.Fields.Any(fld => fld.IsTrackingUser));
            }
            catch (InvalidOperationException opEx)
            {
                Log.Error(opEx, "Multiple linking fields to Owned Type for Type {ApplicationType}", _applicationType.Name);
                throw;
            }
        }

        private List<Field> GetRelatingFields(ApplicationType type)
        {
            return type.Fields.Where(f => f.HasReferenceType).ToList();
        }

        protected void GetDirectOwnershipExpression(string currentUserIdentifier, StringBuilder sb, string alias)
        {
            var aliasExp = alias;
            if (!string.IsNullOrEmpty(aliasExp))
            {
                aliasExp += ".";
            }

            if (HasCreatedByField)
            {
                sb.AppendLine($"{aliasExp}{CreatedByField.Name} = {_domain.TypeProvider.FormatOperationParameterName(FunctionName, currentUserIdentifier)}");
            }

            if (HasCreatedByField && HasModifiedByField)
            {
                sb.AppendLine("OR");
            }

            if (HasModifiedByField)
            {
                sb.AppendLine($"{aliasExp}{ModifiedByField.Name} = {_domain.TypeProvider.FormatOperationParameterName(FunctionName, currentUserIdentifier)}");
            }
        }
    }
}
