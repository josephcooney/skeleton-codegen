using System;
using System.Collections.Generic;
using System.Linq;
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
                                fields.Add(new UserInputFieldModel()
                                    {Field = field as Field, Name = field.Name, RelativeStatePath = parameter.Name + "."});
                                
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
                        fields.Add(new UserInputFieldModel{Field = parameter.RelatedTypeField, Name = parameter.Name, Parameter = parameter});
                    }
                }

                return fields;
            }
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

        public bool IsDateTime => Field?.IsDateTime ?? Parameter.IsDateTime;

        public bool IsLargeTextContent => Field?.IsLargeTextContent ?? Parameter.IsLargeTextContent;

        public bool IsFile => Field?.IsFile ?? Parameter.IsFile;

        public bool IsRating => Field?.IsRating ?? Parameter.IsRating;

        public bool IsColor => Field?.IsColor ?? false;

        public Type ClrType => Field?.ClrType ?? Parameter?.ClrType;

        public bool IsHtml => Field?.IsHtml ?? Parameter.IsHtml;
    }
}