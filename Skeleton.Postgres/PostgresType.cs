using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using NpgsqlTypes;

namespace Skeleton.Postgres
{
    public class PostgresType
    {
        private static Dictionary<string, System.Type> _postgresClrTypes;
        
        private const string ArraySuffix = "[]";
        private const string VoidKeyword = "void";
        private readonly string _typeDescription;

        private const string date = "date";
        private const string time_with_timezone = "time with time zone";
        private const string time = "time";
        
        static PostgresType()
        {
            // from here https://www.npgsql.org/doc/types/basic.html
            _postgresClrTypes = new Dictionary<string, System.Type>
            {
                ["boolean"] = typeof(bool),
                ["smallint"] = typeof(short),
                ["integer"] = typeof(int),
                ["bigint"] = typeof(long),
                ["real"] = typeof(float),
                ["double precision"] = typeof(double),
                ["numeric"] = typeof(decimal),
                ["money"] = typeof(decimal),
                ["text"] = typeof(string),
                ["character varying"] = typeof(string),
                ["character"] = typeof(string),
                ["citext"] = typeof(string),
                ["json"] = typeof(string),
                ["jsonb"] = typeof(string),
                ["xml"] = typeof(string),
                ["point"] = typeof(NpgsqlPoint),
                ["lseg"] = typeof(NpgsqlLSeg),
                ["path"] = typeof(NpgsqlPath),
                ["polygon"] = typeof(NpgsqlPolygon),
                ["line"] = typeof(NpgsqlLine),
                ["circle"] = typeof(NpgsqlCircle),
                ["box"] = typeof(NpgsqlBox),
                ["bit(1)"] = typeof(bool),
                ["bit(n)"] = typeof(BitArray),
                ["bit varying"] = typeof(BitArray),
                ["hstore"] = typeof(IDictionary<string, string>),
                ["uuid"] = typeof(Guid),
                ["cidr"] = typeof(ValueTuple<IPAddress, int>),
                ["inet"] = typeof(IPAddress),
                ["macaddr"] = typeof(PhysicalAddress),
                ["tsquery"] = typeof(NpgsqlTsQuery),
                ["tsvector"] = typeof(NpgsqlTsVector),
                [date] = typeof(DateTime),
                ["interval"] = typeof(TimeSpan),
                ["timestamp"] = typeof(DateTime),
                ["timestamp without time zone"] = typeof(DateTime),
                ["timestamp with time zone"] = typeof(DateTime),
                [time] = typeof(TimeSpan),
                [time_with_timezone] = typeof(DateTimeOffset),
                ["bytea"] = typeof(byte[]),
                ["oid"] = typeof(uint),
                ["xid"] = typeof(uint),
                ["cid"] = typeof(uint),
                ["oidvector"] = typeof(uint[]),

            };
        }
        
        public PostgresType(string typeDescription)
        {
            _typeDescription = typeDescription;
        }

        public bool IsArray => _typeDescription.EndsWith(ArraySuffix);

        public string Name
        {
            get
            {
                if (!IsArray)
                {
                    return _typeDescription;
                }

                return _typeDescription.Replace(ArraySuffix, String.Empty);
            }
        }

        public bool IsVoid => _typeDescription == VoidKeyword;

        public Type ClrType
        {
            get
            {
                if (_postgresClrTypes.ContainsKey(Name))
                {
                    var type = _postgresClrTypes[Name];
                    if (IsArray)
                    {
                        return type.MakeArrayType();
                    }

                    return type;
                }
            
                return null;
            }
        }

        public static bool IsDateOnly(string typeName)
        {
            return typeName.ToLowerInvariant() == date;
        }

        public static bool IsTimeOnly(string typeName)
        {
            var nameLower = typeName.ToLowerInvariant();
            return (nameLower == time || nameLower == time_with_timezone);
        }
    }
}