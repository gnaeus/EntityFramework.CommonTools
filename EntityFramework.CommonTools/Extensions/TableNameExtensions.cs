using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Core.Metadata.Edm;
using System.Data.Entity.Core.Mapping;
using System.Data.Entity.Infrastructure;
using System.Linq;

namespace EntityFramework.CommonTools
{
	 public struct TableAndSchema
    {
        public string TableName;
        public string Schema;

        public TableAndSchema(string table, string schema)
        {
            TableName = table;
            Schema = schema;
        }

        public void Deconstruct(out string table, out string schema)
        {
            table = TableName;
            schema = Schema;
        }
    }
	
    public static partial class DbContextExtensions
    {
        /// <summary>
        /// Get corresponding table name and schema by <paramref name="entityType"/>
        /// </summary>
        public static TableAndSchema GetTableAndSchemaName(this DbContext context, Type entityType)
        {
            return context.GetTableAndSchemaNames(entityType).Single();
        }

        /// <summary>
        /// Get corresponding table name and schema by <paramref name="entityType"/>.
        /// Use it if entity is splitted between multiple tables.
        /// </summary>
        public static TableAndSchema[] GetTableAndSchemaNames(this DbContext context, Type entityType)
        {
            return _tableNames.GetOrAdd(new ContextEntityType(context.GetType(), entityType), _ =>
            {
                return GetTableAndSchemaNames(entityType, context).ToArray();
            });
        }
        
        private struct ContextEntityType
        {
            public Type ContextType;
            public Type EntityType;

            public ContextEntityType(Type contextType, Type entityType)
            {
                ContextType = contextType;
                EntityType = entityType;
            }

            public override int GetHashCode()
            {
                return ContextType.GetHashCode() ^ EntityType.GetHashCode();
            }
        }

        private static readonly ConcurrentDictionary<ContextEntityType, TableAndSchema[]> _tableNames
            = new ConcurrentDictionary<ContextEntityType, TableAndSchema[]>();

        /// <summary>
        /// https://romiller.com/2014/04/08/ef6-1-mapping-between-types-tables/
        /// </summary>
        private static IEnumerable<TableAndSchema> GetTableAndSchemaNames(Type type, DbContext context)
        {
            var metadata = ((IObjectContextAdapter)context).ObjectContext.MetadataWorkspace;

            // Get the part of the model that contains info about the actual CLR types
            var objectItemCollection = ((ObjectItemCollection)metadata.GetItemCollection(DataSpace.OSpace));

            // Get the entity type from the model that maps to the CLR type
            var entityType = metadata
                .GetItems<EntityType>(DataSpace.OSpace)
                .Single(e => objectItemCollection.GetClrType(e) == type);

            // Get the entity set that uses this entity type
            var entitySet = metadata
                .GetItems<EntityContainer>(DataSpace.CSpace)
                .Single()
                .EntitySets
                .Single(s => s.ElementType.Name == entityType.Name);

            // Find the mapping between conceptual and storage model for this entity set
            var mapping = metadata.GetItems<EntityContainerMapping>(DataSpace.CSSpace)
                    .Single()
                    .EntitySetMappings
                    .Single(s => s.EntitySet == entitySet);

            // Find the storage entity sets (tables) that the entity is mapped
            var tables = mapping
                .EntityTypeMappings.Single()
                .Fragments;

            // Return the table name from the storage entity set
            return tables.Select(f => new TableAndSchema(
                (string)f.StoreEntitySet.MetadataProperties["Table"].Value ?? f.StoreEntitySet.Name,
                (string)f.StoreEntitySet.MetadataProperties["Schema"].Value ?? f.StoreEntitySet.Schema));
        }
    }
}
