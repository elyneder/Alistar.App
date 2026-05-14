using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Alistar.App.Models
{
    public class VidaPessoal
    {
        
        [JsonPropertyName("Address")]
        public string Endereco { get; set; } = string.Empty;

        public string Bairro { get; set; } = string.Empty;
        public string CEP { get; set; } = string.Empty;
        public string Telefone { get; set; } = string.Empty;

        [JsonPropertyName("City")]
        public string Municipio { get; set; } = string.Empty;    
        public string Email { get; set; } = string.Empty;
        public string Ocupacao { get; set; } = string.Empty;
        public string MoraCom { get; set; } = string.Empty;
        public string EstadoCivil { get; set; } = string.Empty;
        public string PossuiFilhos { get; set; } = string.Empty;
        public string QuantidadeFilhos { get; set; } = string.Empty;
        public string QuemTrabalhaNaFamilia { get; set; } = string.Empty;
        public string QuemSustentaAFamilia { get; set; } = string.Empty;
        public string RecebeAuxilioGovernamental { get; set; } = string.Empty;
        public string QualAuxilioGovernamental { get; set; } = string.Empty;

       
    }
}
