namespace Api.Models
{
    public class CreateMedicoRequest
    {
        public required string Nombre { get; set; }
        public required string Especialidad { get; set; }
    }
}
