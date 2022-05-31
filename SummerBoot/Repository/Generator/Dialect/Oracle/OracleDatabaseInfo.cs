﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using SummerBoot.Core;
using SummerBoot.Repository.Generator.Dto;

namespace SummerBoot.Repository.Generator.Dialect.Oracle
{
    public class OracleDatabaseInfo : IDatabaseInfo
    {
        private readonly IDbFactory dbFactory;

        public OracleDatabaseInfo(IDbFactory dbFactory)
        {
            this.dbFactory = dbFactory;
        }

        public GenerateDatabaseSqlResult CreateTable(DatabaseTableInfoDto tableInfo)
        {
            var tableName = tableInfo.Name;
            var schemaTableName = GetSchemaTableName(tableInfo.Schema, tableName);

            var fieldInfos = tableInfo.FieldInfos;

            var body = new StringBuilder();
            body.AppendLine($"CREATE TABLE {schemaTableName} (");
            //主键
            var keyField = "";
            var hasKeyField = fieldInfos.Any(it => it.IsKey);
            //数据库注释
            var databaseDescriptions = new List<string>();
            if (tableInfo.Description.HasText())
            {
                var tableDescriptionSql = CreateTableDescription(tableInfo.Schema, tableName, tableInfo.Description);
                databaseDescriptions.Add(tableDescriptionSql);
            }

            for (int i = 0; i < fieldInfos.Count; i++)
            {
                var fieldInfo = fieldInfos[i];

                //行末尾是否有逗号
                var lastComma = "";
                if (i != fieldInfos.Count - 1)
                {
                    lastComma = ",";
                }
                else
                {
                    lastComma = hasKeyField ? "," : "";
                }

                body.AppendLine($"    {GetCreateFieldSqlByFieldInfo(fieldInfo)}{lastComma}");
                if (fieldInfo.IsKey)
                {
                    keyField = fieldInfo.ColumnName;
                }

                //添加行注释
                if (fieldInfo.Description.HasText())
                {
                    var tableFieldDescription = CreateTableFieldDescription(tableInfo.Schema, tableName, fieldInfo);
                    databaseDescriptions.Add(tableFieldDescription);
                }
            }

            if (keyField.HasText())
            {
                body.AppendLine($"    CONSTRAINT \"PK_{tableName}\" PRIMARY KEY (\"{keyField}\")");
            }

            body.AppendLine($")");

            var result = new GenerateDatabaseSqlResult()
            {
                Body = body.ToString(),
                Descriptions = databaseDescriptions,
                FieldModifySqls = new List<string>()
            };

            return result;
        }

        /// <summary>
        /// 通过字段信息生成生成表的sql
        /// </summary>
        /// <param name="fieldInfo"></param>
        /// <returns></returns>
        private string GetCreateFieldSqlByFieldInfo(DatabaseFieldInfoDto fieldInfo)
        {
            var identityString = fieldInfo.IsAutoCreate ? " GENERATED BY DEFAULT ON NULL AS IDENTITY MINVALUE 1 MAXVALUE 9999999999999999999999999999 INCREMENT BY 1 START WITH 1 CACHE 20 NOORDER  NOCYCLE  NOKEEP  NOSCALE" : "";
            var nullableString = fieldInfo.IsNullable ? "" : " NOT NULL";
            var columnDataType = fieldInfo.ColumnDataType;
            //string类型默认长度2000，也可自定义
            if (fieldInfo.ColumnDataType == "NVARCHAR2")
            {
                columnDataType = fieldInfo.StringMaxLength.HasValue && fieldInfo.StringMaxLength.Value != int.MaxValue
                    ? $"NVARCHAR2({fieldInfo.StringMaxLength.Value})"
                    : $"NVARCHAR2(2000)";
            }
            //自定义NUMBER精度类型
            if (fieldInfo.ColumnDataType == "NUMBER")
            {
                var precision = fieldInfo.Precision;
                var scale = fieldInfo.Scale;

                if (fieldInfo.ColumnType.GetUnderlyingType() == typeof(int))
                {
                    precision = 10;
                    scale = 0;
                }

                if (fieldInfo.ColumnType.GetUnderlyingType() == typeof(long))
                {
                    precision = 19;
                    scale = 0;
                }
                if (fieldInfo.ColumnType.GetUnderlyingType() == typeof(bool))
                {
                    precision = 1;
                    scale = 0;
                }
                if (fieldInfo.ColumnType.GetUnderlyingType() == typeof(short))
                {
                    precision = 5;
                    scale = 0;
                }
                if (fieldInfo.ColumnType.GetUnderlyingType() == typeof(byte))
                {
                    precision = 3;
                    scale = 0;
                }
                columnDataType =
                    $"NUMBER({precision},{scale})";
            }
            //guid类型，默认16位
            if (fieldInfo.ColumnDataType == "RAW")
            {
                columnDataType = $"RAW(16)";
            }
            //datetime类型，默认7位
            if (fieldInfo.ColumnDataType == "TIMESTAMP")
            {
                columnDataType = $"TIMESTAMP(7)";
            }

            if (fieldInfo.SpecifiedColumnDataType.HasText())
            {
                columnDataType = fieldInfo.SpecifiedColumnDataType;
            }

            var columnName = BoxTableNameOrColumnName(fieldInfo.ColumnName);
            var result = $"{columnName} {columnDataType}{identityString}{nullableString}";
            return result;
        }

        public string CreateTableDescription(string schema, string tableName, string description)
        {
            var schemaTableName = GetSchemaTableName(schema, tableName);
            var sql =
                $"COMMENT ON TABLE {schemaTableName} IS '{description}'";
            return sql;
        }

        public string UpdateTableDescription(string schema, string tableName, string description)
        {
            var sql = CreateTableDescription(schema, tableName, description);
            return sql;
        }

        public string CreateTableField(string schema, string tableName, DatabaseFieldInfoDto fieldInfo)
        {
            var schemaTableName = GetSchemaTableName(schema, tableName);
            var sql = $"ALTER TABLE {schemaTableName} ADD {GetCreateFieldSqlByFieldInfo(fieldInfo)}";
            return sql;
        }

        public string CreateTableFieldDescription(string schema, string tableName, DatabaseFieldInfoDto fieldInfo)
        {
            var schemaTableName = GetSchemaTableName(schema, tableName);
            var columnName = BoxTableNameOrColumnName(fieldInfo.ColumnName);
            var sql =
                $"COMMENT ON COLUMN {schemaTableName}.{columnName} IS '{fieldInfo.Description}'";
            return sql;
        }

        public DatabaseTableInfoDto GetTableInfoByName(string schema, string tableName)
        {
            schema = GetDefaultSchema(schema);
            var dbConnection = dbFactory.GetDbConnection();
            var sql = @"   select c.*,d.comments Description from (select a.column_name AS ColumnName , a.data_type AS ColumnDataType,a.DATA_PRECISION AS Precision,a.DATA_SCALE AS Scale,  a.data_length ,CASE when a.nullable='Y' THEN 1 ELSE 0 end as IsNullable,CASE when  b.column_name is not null then 1 else 0 END IsKey  from all_tab_columns a left join 
                (select cu.* from all_cons_columns cu, all_constraints au where cu.constraint_name = au.constraint_name and au.constraint_type = 'P') b on 
                b.table_name = a.Table_Name and a.column_name = b.column_name where a.Table_Name=:tableName and a.owner=:schemaName ORDER BY a.column_id) c left join all_col_comments d on c.ColumnName = d.column_name where d.table_name =:tableName
                ";
            var fieldInfos = dbConnection.Query<DatabaseFieldInfoDto>(sql, new { tableName, schemaName=schema }).ToList();

            var tableDescriptionSql = @"	SELECT comments FROM all_tab_comments WHERE table_name =:tableName AND TABLE_TYPE ='TABLE' and owner=:schemaName";

            var tableDescription = dbConnection.QueryFirstOrDefault<string>(tableDescriptionSql, new { tableName, schemaName = schema });

            var result = new DatabaseTableInfoDto()
            {
                Name = tableName,
                Description = tableDescription,
                FieldInfos = fieldInfos
            };

            return result;
        }

        public string GetSchemaTableName(string schema, string tableName)
        {
            tableName = BoxTableNameOrColumnName(tableName);
            tableName = schema.HasText() ? schema + "." + tableName : tableName;
            return tableName;
        }

        public string CreatePrimaryKey(string schema, string tableName, DatabaseFieldInfoDto fieldInfo)
        {
            var schemaTableName = GetSchemaTableName(schema, tableName);
            var sql =
                $"ALTER TABLE {schemaTableName} ADD CONSTRAINT {tableName}_PK PRIMARY KEY({fieldInfo.ColumnName}) ENABLE";

            return sql;
        }

        public string BoxTableNameOrColumnName(string tableNameOrColumnName)
        {
            return "\"" + tableNameOrColumnName + "\"";
        }

        public string GetDefaultSchema(string schema)
        {
            if (schema.HasText())
            {
                return schema;
            }

            var dbConnection = dbFactory.GetDbConnection();
            
            var result = dbConnection.QueryFirstOrDefault<string>("select USERNAME  from user_users");
            return result;
        }
    }
}