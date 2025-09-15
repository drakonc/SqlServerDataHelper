using System.Data;

namespace SqlServerHelper.Extensions
{
    public static class DataRowExtensions
    {
        public static T GetFieldValue<T>(this DataRow row, string columnName, T defaultValue = default!)
        {
            if (row.Table.Columns.Contains(columnName) && row[columnName] != DBNull.Value)
            {
                return (T)Convert.ChangeType(row[columnName], typeof(T));
            }
            return defaultValue;
        }
    }
}
