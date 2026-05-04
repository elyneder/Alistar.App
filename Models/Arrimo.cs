using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Alistar.App.Models
{
    public class Arrimo
    {
        public string SituacaoArrimo { get; set; } = string.Empty;
        public string EstudaAtualmente { get; set; } = string.Empty;

        [JsonPropertyName("Education")]
        public string AnoQueEstaCursandoOuUltimoAnoQueCursou { get; set; } = string.Empty;
    }
}
