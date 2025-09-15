namespace SqlHelper.Models
{
    /// <summary>
    /// Resultado de stored procedure con parámetros de salida
    /// </summary>
    public class SqlOutputResult<T>
    {
        public List<T> Data { get; set; } = new();
        public Dictionary<string, object> OutputParameters { get; set; } = new();
    }
}
