﻿using System;
using System.Collections.Generic;
using Skeleton.Model.NamingConventions;
using Skeleton.Model.Operations;

namespace Skeleton.Model
{
    public interface ITypeProvider
    {
        Domain GetDomain(Settings settings);

        void GetOperations(Domain domain);

        void AddGeneratedOperation(string text);

        void DropGenerated(Domain domain);

        CodeFile GenerateDropStatements(Domain oldDomain, Domain newDomain);
        
        string EscapeReservedWord(string name);

        string EscapeSqlName(string name); // like the above but ignores reserved words

        public string GetCsDbTypeFromDbType(string dbTypeName);

        public string GetSqlName(string entityName);

        public bool CustomTypeExists(string customTypeName);

        public bool IsDateOnly(string typeName);

        public bool IsTimeOnly(string typeName);

        public void AddTestData(List<CodeFile> scripts);
        
        public string DefaultNamespace { get; }

        public string GetTemplate(string templateName);
        
        bool GenerateCustomTypes { get; }

        string FormatOperationParameterName(string operationName, string name);

        string OperationTimestampFunction();

        IParamterPrototype CreateFieldAdapter(Field field, IOperationPrototype operationPrototype);
        
        bool IncludeIdentityFieldsInInsertStatements { get; }

        string GetProviderTypeForClrType(Type type);
        ISortField CreateSortField(IPseudoField field, IOperationPrototype operationPrototype);

        IPseudoField CreateSortParameter(INamingConvention namingConvention);
    }
}
