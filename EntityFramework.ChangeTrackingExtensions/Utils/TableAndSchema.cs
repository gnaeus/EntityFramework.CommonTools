namespace EntityFramework.ChangeTrackingExtensions.Utils
{
    public struct TableAndSchema
    {
        public string Table;
        public string Schema;

        public TableAndSchema(string table, string schema)
        {
            Table = table;
            Schema = schema;
        }

        public void Deconstruct(out string table, out string schema)
        {
            table = Table;
            schema = Schema;
        }
    }
}
