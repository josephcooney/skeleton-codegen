using System;
using System.Collections.Generic;
using System.Linq;
using Serilog;
using Skeleton.Model;
using Skeleton.Templating.Classes.Adapters;

namespace Skeleton.Templating.ReactClient.Adapters
{
    public class ClientApiOperationAdapter : OperationAdapter
    {
        public ClientApiOperationAdapter(Operation op, Domain domain, ApplicationType type) : base(op, domain, type)
        {
        }

        public bool HasCustomType => UsesModel || Parameters.Any(p => p.IsCustomTypeOrCustomArray);
        
        public string ModelTypeName => UsesModel ? $"{Util.CSharpNameFromName(_op.Name)}{NamingConventions.ModelClassNameSuffix}" : null;
        
        public ClientApiAdapter ClientApi => new ClientApiAdapter(_type, _domain);
        
        public List<Field> EditableLinkingFields
        {
            get
            {
                var fields = UserEditableParameters
                    .Where(p => p.RelatedTypeField != null && p.RelatedTypeField.HasReferenceType &&
                                !p.RelatedTypeField.ReferencesType.IsReferenceData).Select(p => p.RelatedTypeField).ToList();
                
                fields.AddRange(UserEditableParameters.Where(p => p.IsCustomTypeOrCustomArray).SelectMany(p => p.CustomType.Fields.Where(f => f.HasReferenceType && !f.ReferencesType.IsReferenceData)));

                return fields;
            }
        }

        public bool HasEditableLinkingFields => EditableLinkingFields.Any();

        public string StatePath => UsesModel ? "state.data" : "state";

        public List<UserInputFieldModel> UserInputFields {
            get
            {
                var fields = new List<UserInputFieldModel>();
                foreach (var parameter in UserEditableParameters)
                {
                    if (parameter.IsCustomTypeOrCustomArray)
                    {
                        foreach (var field in parameter.ClientCustomType.Fields)
                        {
                            // this only handles 1 level of nesting of fields
                            if (field is Field)
                            {
                                if (IsLinkingItemIdList(field))
                                {
                                    var otherSideOfLink = GetOtherSideOfLinkingType(field);
                                    
                                    fields.Add(new UserInputFieldModel()
                                        {IsLinkedItemIds = true, LinkedItemType = otherSideOfLink, Field = field as Field, Name = field.Name, RelativeStatePath = parameter.Name + "."});
                                    
                                }
                                else
                                {
                                    fields.Add(new UserInputFieldModel()
                                        {Field = field as Field, Name = field.Name, RelativeStatePath = parameter.Name + "."});
                                }
                            }
                            else
                            {
                                // handle parameters that don't match to underlying type fields
                                fields.Add(new UserInputFieldModel(){Parameter = field as Parameter, Name = parameter.Name, RelativeStatePath = parameter.Name + "."});
                            }
                        }
                    }
                    else
                    {
                        if (parameter.RelatedTypeField == null && IsLinkingItemIdList(parameter))
                        {
                            var otherSideOfLink = GetOtherSideOfLinkingType(parameter);
                            fields.Add(new UserInputFieldModel{Name = parameter.Name, Parameter = parameter, IsLinkedItemIds = true, LinkedItemType = otherSideOfLink});
                        }
                        else
                        {
                            fields.Add(new UserInputFieldModel{Field = parameter.RelatedTypeField, Name = parameter.Name, Parameter = parameter});
                        }
                    }
                }

                if (_op.ChangesData && !_op.CreatesNew)
                {
                    return fields.Where(f => !(f.IsKey && f.Parameter?.RelatedTypeField?.ReferencesType == _type)).ToList();
                }

                return fields;
            }
        }

        private ApplicationType GetOtherSideOfLinkingType(TypedValue field)
        {
            var isArrayOrList = field.ClrType.IsArray || (field.ClrType.IsGenericType && (field.ClrType.GetGenericTypeDefinition() == typeof(List<>)));
            if (isArrayOrList)
            {
                try
                {
                    Type itemType = field.ClrType.IsArray
                        ? field.ClrType.GetElementType()
                        : field.ClrType.GetGenericArguments()[0];

                    var linkingType = _type.LinkedTypes.Where(t => t.IsLink).Single(t =>
                        t.Fields.Any(f => Util.PluraliseParameterName(f.Name) == field.Name && f.ClrType == itemType));
                    // get the 'other side' of the linking type
                    return linkingType.Fields.Single(f => f.HasReferenceType && f.ReferencesType != _type && f.ReferencesType != _domain.UserType).ReferencesType;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unexpected error getting linking type for field {Field} on type {Type}", field.Name, _type.Name);
                    return null;
                }
            }

            Log.Warning("Unable to find other side of link on field {FieldName} on type {TypeName}", field.Name, _type.Name);
            return null;
        }

        private bool IsLinkingItemIdList(TypedValue field)
        {
            var isArrayOrList = field.ClrType.IsArray || (field.ClrType.IsGenericType && (field.ClrType.GetGenericTypeDefinition() == typeof(List<>)));
            if (isArrayOrList)
            {
                Type itemType = field.ClrType.IsArray
                    ? field.ClrType.GetElementType()
                    : field.ClrType.GetGenericArguments()[0];
                
                // check to see if the reference type has a linking type with one of these column ids
                var linkingTypes = _type.LinkedTypes.Where(t => t.IsLink);
                var result = linkingTypes.Any(t => t.Fields.Any(f => Util.PluraliseParameterName(f.Name) == field.Name && f.ClrType == itemType));
                return result;
            }

            return false;
        }

        public List<UserInputFieldModel> ClientSuppliedFields
        {
            get
            {
                var fields = UserInputFields;
                if (_op.ChangesData && !_op.CreatesNew)
                {
                    foreach (var parameter in Parameters)
                    {
                        if (parameter.IsCustomTypeOrCustomArray)
                        {
                            foreach (var field in parameter.CustomType.Fields.Where(f => f.IsKey))
                            {
                                // this only handles 1 level of nesting of fields
                                fields.Add(new UserInputFieldModel()
                                    {Field = field, Name = field.Name, RelativeStatePath = parameter.Name + "."});
                            }
                        }
                        else
                        {
                            if (parameter.RelatedTypeField?.IsKey == true)
                            {
                                fields.Add(new UserInputFieldModel{Field = parameter.RelatedTypeField, Name = parameter.Name, Parameter = parameter});
                            }
                        }
                    }
                }

                return fields;
            }
        }
    }

    public class UserInputFieldModel
    {
        public Field Field { get; set; }
        
        public Parameter Parameter { get; set; }
        
        public string RelativeStatePath { get; set; }
        
        public string Name { get; set; }

        public string NameWithPath => RelativeStatePath + Name;

        public string NameWithPathCamelCase => Util.CamelCase(RelativeStatePath) + Util.CamelCase(Name);

        public string NameWithPathSafeCamelCase => NameWithPathCamelCase.Replace(".", "?.");
        
        public string NameWithPathSafe => NameWithPath.Replace(".", "?.");

        public bool IsBoolean => Field?.IsBoolean ?? Parameter.IsBoolean;

        public bool IsDate => Field?.IsDate ?? Parameter.IsDate;

        public bool IsDateOrDateTime => Field?.IsDateOrDateTime ?? Parameter.IsDateOrDateTime;

        public bool IsDateTime => Field?.IsDateTime ?? Parameter.IsDateTime;

        public bool IsLargeTextContent => Field?.IsLargeTextContent ?? Parameter.IsLargeTextContent;

        public bool IsFile => Field?.IsFile ?? Parameter.IsFile;

        public bool IsRating => Field?.IsRating ?? Parameter.IsRating;

        public bool IsColor => Field?.IsColor ?? false;

        public Type ClrType => Field?.ClrType ?? Parameter?.ClrType;

        public bool IsHtml => Field?.IsHtml ?? Parameter.IsHtml;

        public bool IsKey => Field?.IsKey ?? Parameter.RelatedTypeField?.IsKey ?? false;
        
        public bool IsLinkedItemIds { get; set; }
        
        public ApplicationType LinkedItemType { get; set; }

        public string LabelText
        {
            get
            {
                if (IsLinkedItemIds)
                {
                    return $"Related {Util.Pluralize(Util.HumanizeName(LinkedItemType.Name))}";
                }
                
                if (Field != null)
                {
                    return Util.HumanizeName(Field);
                }

                return Util.HumanizeName(Name);
            }
        }
    }
}