﻿using System.Collections.Generic;
using System.Reflection;

namespace SummerBoot.Repository.ExpressionParser.Parser
{
    public class DbQueryResult
    {
        /// <summary>
        /// 执行的sql
        /// </summary>
        public string Sql { get; set; }
        /// <summary>
        /// 计算总数的sql
        /// </summary>
        public string CountSql { get; set; }
        /// <summary>
        /// 参数
        /// </summary>
        public List<SqlParameter> SqlParameters { get; set; }
        /// <summary>
        /// 插入数据库后获取ID的sql
        /// </summary>
        public string LastInsertIdSql { get; set; }
        /// <summary>
        /// Id 字段的反射属性信息
        /// </summary>
        public PropertyInfo IdKeyPropertyInfo { get; set; }
        /// <summary>
        /// id字段的名称，有些数据库大小写敏感
        /// </summary>
        public string IdName { get; set; }
    }
}