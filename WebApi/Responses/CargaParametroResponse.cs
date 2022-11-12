﻿using System;
using Dominio.Administrador;
using System.Net;
using Dominio;

namespace WebApi.Responses
{
    public class CargaParametroResponse : GenericResponse
    {
        public List<string> errores { get; set; }
        public int registros { get; set; }



        public CargaParametroResponse(HttpStatusCode codigo, string titulo, string mensaje, List<string> errores, int registros) : base(codigo, titulo, mensaje)
        {
            this.errores = errores;
            this.registros = registros;
        }

        //public CargaParametroResponse(HttpStatusCode codigo, string titulo, string mensaje, ParametroRequest error, int registros) : base(codigo, titulo, mensaje)
        //{
        //}
    }
}

