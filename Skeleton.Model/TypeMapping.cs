using System;
using System.Collections.Generic;
using System.Data;

namespace Skeleton.Model
{
    public class TypeMapping
    {
        private static Dictionary<System.Type, DbType> map;

        private static Dictionary<System.Type, string> clrAliasMap;

        public static string GetCSharpShortTypeName(System.Type clrType)
        {
            if (clrAliasMap.ContainsKey(clrType))
            {
                return clrAliasMap[clrType];
            }

            return clrType.Name;
        }

        static TypeMapping()
        {
            map = new Dictionary<System.Type, DbType>
            {
                [typeof(byte)] = DbType.Byte,
                [typeof(sbyte)] = DbType.SByte,
                [typeof(short)] = DbType.Int16,
                [typeof(ushort)] = DbType.UInt16,
                [typeof(int)] = DbType.Int32,
                [typeof(uint)] = DbType.UInt32,
                [typeof(long)] = DbType.Int64,
                [typeof(ulong)] = DbType.UInt64,
                [typeof(float)] = DbType.Single,
                [typeof(double)] = DbType.Double,
                [typeof(decimal)] = DbType.Decimal,
                [typeof(bool)] = DbType.Boolean,
                [typeof(string)] = DbType.String,
                [typeof(char)] = DbType.StringFixedLength,
                [typeof(Guid)] = DbType.Guid,
                [typeof(DateTime)] = DbType.DateTime,
                [typeof(DateTimeOffset)] = DbType.DateTimeOffset,
                [typeof(TimeSpan)] = DbType.Time,
                [typeof(byte[])] = DbType.Binary,
                [typeof(byte?)] = DbType.Byte,
                [typeof(sbyte?)] = DbType.SByte,
                [typeof(short?)] = DbType.Int16,
                [typeof(ushort?)] = DbType.UInt16,
                [typeof(int?)] = DbType.Int32,
                [typeof(uint?)] = DbType.UInt32,
                [typeof(long?)] = DbType.Int64,
                [typeof(ulong?)] = DbType.UInt64,
                [typeof(float?)] = DbType.Single,
                [typeof(double?)] = DbType.Double,
                [typeof(decimal?)] = DbType.Decimal,
                [typeof(bool?)] = DbType.Boolean,
                [typeof(char?)] = DbType.StringFixedLength,
                [typeof(Guid?)] = DbType.Guid,
                [typeof(DateTime?)] = DbType.DateTime,
                [typeof(DateTimeOffset?)] = DbType.DateTimeOffset,
                [typeof(TimeSpan?)] = DbType.Time,
                [typeof(object)] = DbType.Object
            };

            clrAliasMap = new Dictionary<System.Type, string>
            {
                [typeof(System.Boolean)] =    "bool",
                [typeof(System.Byte)]   = "byte",
                [typeof(System.SByte)] = "sbyte",
                [typeof(System.Char)] = "char",
                [typeof(System.Decimal)] = "decimal",   
                [typeof(System.Double)] = "double",
                [typeof(System.Single)] = "float",
                [typeof(System.Int32)] = "int",
                [typeof(System.UInt32)] = "uint",
                [typeof(System.Int64)] = "long",
                [typeof(System.UInt64)] = "ulong",
                [typeof(System.Object)] = "object",
                [typeof(System.Int16)] = "short",
                [typeof(System.UInt16)] = "ushort",
                [typeof(System.String)] = "string",
            };
        }
    }
}
