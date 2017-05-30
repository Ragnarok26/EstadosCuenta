using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;

namespace EstadosCuenta.Models
{
    public class Territorio
    {
        public int TerritoryId { get; set; }
        public string Territory { get; set; }
        public string Empresa { get; set; }
    }
}